from __future__ import annotations

import json
import os
import re
import time
from pathlib import Path

import requests
from playwright.sync_api import sync_playwright, Page, BrowserContext

from toolkit.logging_config import get_logger
from toolkit.types import AudioFormat

logger = get_logger("pristine")

HEADLESS: bool = os.getenv("PRISTINE_HEADLESS", "0") == "1"

BASE_OUT_DIR: str = os.getenv(
    "PRISTINE_OUT_DIR",
    r"C:\Users\Lance\My Drive\Sir Fapsalot\Pristine Classical",
)

TOSCANINI_BEETHOVEN = [
    "PASC552",
    "PASC553",
    "PASC554",
    "PASC555",
    "PASC556",
    "PASC557",
]

GENERAL = [
    "PASC762",
    "PASC648",
    "PASC313",
    "PASC003",
    "PASC040",
    "PASC393",
    "PASC626",
    "PASC653",
    "PASC246",
    "PASC619",
    "PASC731",
    "PASC760",
    "PACO180",
    "PASC569",
    "PASC669",
    "PASC633",
    "PASC655",
    "PASC741",
    "PASC736",
    "PASC131",
    "PASC006",
    "PASC759",
    "PASC764",
    "PASC486",
    "PASC450",
    "PASC443",
    "PASC447",
    "PAKM059",
]

STOKOWSKI = [
    "PASC591",
    "PASC596",
    "PASC531",
    "PASC609",
    "PASC625",
    "PASC379",
    "PASC161",
    "PASC133",
    "PASC182",
    "PASC602",
    "PASC536",
    "PASC587",
    "PASC629",
]

RELEASES: list[str] = TOSCANINI_BEETHOVEN + GENERAL + STOKOWSKI

S3_COVERS = "https://s3-eu-west-1.amazonaws.com/pristine-classical-storage/covers/"
PRISTINE_APP = "https://pristinestreaming.com/app/browse"
POLL_SLEEP = 1.0
POST_DL_WAIT = 2.0
MAX_STALL_COUNT = 60
AUTO_OVERWRITE = True

_READY_STATE = {0: "NOTHING", 1: "METADATA", 2: "CURRENT", 3: "FUTURE", 4: "ENOUGH"}
_NET_STATE = {0: "EMPTY", 1: "IDLE", 2: "LOADING", 3: "NO_SRC"}

_WIN_ILLEGAL_CHARS = re.compile(r'[<>:"/\\|?*\x00-\x1f]')
_TRAILING_DOTS_SPACES = re.compile(r"[\s.]+$")
_AUDIO_URL_RE = re.compile(r"\.(flac|mp3|wav|aac|ogg)(\?|$)", re.IGNORECASE)

_TIMESTAMP_PREFIX_RE = re.compile(r"^\s*\d{1,2}:\d{2}(?::\d{2})?\s*[-\u2013\u2014:.)]*\s*")
_MOVEMENT_PREFIX_RE = re.compile(
    r"""
    ^\s*(?:
        (?P<ord>\d{1,2})(?:st|nd|rd|th)?\s*(?:m(?:ovement|ov\.?|vt|vmt)\.?)?
      | (?P<roman>[ivxlcdm]{1,6})\s*(?:m(?:ovement|ov\.?|vt|vmt)\.?)?
      | (?P<word>first|second|third|fourth|fifth|sixth|seventh|eighth|ninth|tenth)\s+movement
    )\s*[-\u2013\u2014:.)]*\s*
    """,
    re.IGNORECASE | re.VERBOSE,
)
_WORD_TO_INT: dict[str, int] = {
    "first": 1, "second": 2, "third": 3, "fourth": 4, "fifth": 5,
    "sixth": 6, "seventh": 7, "eighth": 8, "ninth": 9, "tenth": 10,
}
_ROMAN_NUMERALS = [
    "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X",
    "XI", "XII", "XIII", "XIV", "XV",
]


def _roman(n: int) -> str:
    return _ROMAN_NUMERALS[n] if 0 < n < len(_ROMAN_NUMERALS) else str(n)


def _normalize_track_title(title: str) -> str:
    """Strip leading timestamps and normalize movement prefixes to canonical Roman numeral form.

    Examples:
        "1st.mov - Allegro"  -> "I. Allegro"
        "0:00 - 1st mvt"     -> "I."
        "II movement: Andante" -> "II. Andante"
        "Plain Title"          -> "Plain Title"
    """
    text = _TIMESTAMP_PREFIX_RE.sub("", title).strip()
    m = _MOVEMENT_PREFIX_RE.match(text)
    if not m:
        return text
    remainder = text[m.end():].strip()
    if m.group("ord"):
        num = int(m.group("ord"))
    elif m.group("roman"):
        roman_map = {"I": 1, "V": 5, "X": 10, "L": 50, "C": 100}
        val = 0
        prev = 0
        for ch in reversed(m.group("roman").upper()):
            curr = roman_map.get(ch, 0)
            val += curr if curr >= prev else -curr
            prev = curr
        num = val
    else:
        num = _WORD_TO_INT.get((m.group("word") or "").lower(), 0)
    prefix = f"{_roman(num)}."
    return f"{prefix} {remainder}" if remainder else prefix


_USER_DATA_DIR = os.path.join(
    os.getenv("LOCALAPPDATA", ""), "pristine-playwright-profile"
)
_AUTH_PATH = Path(__file__).parent.parent.parent / "state" / "pristine" / "auth.json"


def _sanitize_path_component(name: str) -> str:
    try:
        name = _WIN_ILLEGAL_CHARS.sub("-", name)
    except Exception as exc:
        logger.warning("sanitize sub failed: %s", exc)
    try:
        name = _TRAILING_DOTS_SPACES.sub("", name)
    except Exception as exc:
        logger.warning("sanitize trailing sub failed: %s", exc)
    try:
        return name.strip() or "Unknown"
    except Exception as exc:
        logger.warning("sanitize strip failed: %s", exc)
        return "Unknown"


_DL_MAX_ATTEMPTS = 3
_DL_RETRY_BASE_S = 2


def _download_file(
    url: str, dest: str, session: requests.Session | None = None
) -> bool:
    try:
        if session is None:
            try:
                session = requests.Session()
            except Exception as exc:
                logger.error("Session creation failed: %s", exc)
                return False
        if not AUTO_OVERWRITE:
            try:
                if os.path.exists(dest) and os.path.getsize(dest) > 0:
                    logger.info("SKIP (already exists): %s", os.path.basename(dest))
                    return True
            except Exception as exc:
                logger.debug("Existence check failed: %s", exc)
        part_dest = dest + ".part"
        last_exc: Exception | None = None
        for attempt in range(1, _DL_MAX_ATTEMPTS + 1):
            try:
                t0 = time.time()
                r = session.get(url, stream=True, timeout=60)
                if r.status_code != 200:
                    logger.warning("HTTP %s: %s", r.status_code, url)
                    return False
                downloaded_bytes = 0
                try:
                    with open(part_dest, "wb") as fp:
                        for chunk in r.iter_content(chunk_size=131072):
                            if chunk:
                                try:
                                    fp.write(chunk)
                                    downloaded_bytes += len(chunk)
                                except Exception as exc:
                                    logger.error("Write error: %s", exc)
                                    raise
                except Exception as exc:
                    logger.error("File open/write failed for %s: %s", dest, exc)
                    raise
                try:
                    os.replace(part_dest, dest)
                except Exception as exc:
                    logger.error("File replace failed: %s", exc)
                    return False
                try:
                    dt = time.time() - t0
                    speed = (downloaded_bytes / 1024 / 1024) / dt if dt > 0 else 0
                    logger.info(
                        "DL complete: %s  (%.2f MB/s)", os.path.basename(dest), speed
                    )
                except Exception as exc:
                    logger.debug("Speed logging failed for %s: %s", dest, exc)
                return True
            except (requests.RequestException, OSError) as exc:
                last_exc = exc
                try:
                    if os.path.exists(part_dest):
                        os.remove(part_dest)
                except Exception as exc2:
                    logger.debug("Cleanup of partial file failed: %s", exc2)
                if attempt < _DL_MAX_ATTEMPTS:
                    delay = _DL_RETRY_BASE_S * (2 ** (attempt - 1))
                    logger.warning(
                        "Download attempt %d/%d failed (%s) — retrying in %ds",
                        attempt, _DL_MAX_ATTEMPTS, exc, delay,
                    )
                    time.sleep(delay)
        logger.warning(
            "Failed to download %s after %d attempts: %s",
            os.path.basename(dest), _DL_MAX_ATTEMPTS, last_exc,
        )
        return False
    except Exception as exc:
        logger.error("_download_file unexpected error for %s: %s", dest, exc)
        return False


def _count_downloaded_audio_files(album_out: str) -> int:
    """Count downloaded audio files in the album output directory."""
    try:
        if not os.path.isdir(album_out):
            return 0
        return sum(
            1
            for name in os.listdir(album_out)
            if name.lower().endswith((".flac", ".mp3"))
            and os.path.getsize(os.path.join(album_out, name)) > 0
        )
    except Exception as exc:
        logger.debug("Audio file count failed for %s: %s", album_out, exc)
        return 0


def _create_browser_context(pw: object) -> BrowserContext:
    try:
        context = pw.chromium.launch_persistent_context(
            _USER_DATA_DIR,
            channel="msedge",
            headless=HEADLESS,
            accept_downloads=True,
            args=["--autoplay-policy=no-user-gesture-required"],
        )
        if _AUTH_PATH.exists():
            try:
                auth = json.loads(_AUTH_PATH.read_text(encoding="utf-8"))
                cookies = auth.get("cookies", [])
                if cookies:
                    context.add_cookies(cookies)
                    logger.info(
                        "Injected %d cookies from auth.json into Playwright context.",
                        len(cookies),
                    )
                origins = auth.get("origins", [])
                for origin_data in origins:
                    origin_url = origin_data.get("origin", "")
                    local_storage_items = origin_data.get("localStorage", [])
                    if local_storage_items:
                        js_lines = []
                        for item in local_storage_items:
                            escaped_name = json.dumps(item["name"])
                            escaped_value = json.dumps(item["value"])
                            js_lines.append(
                                f"localStorage.setItem({escaped_name}, {escaped_value});"
                            )
                        script = "\n".join(js_lines)
                        context.add_init_script(
                            f"if (window.location.origin === {json.dumps(origin_url)}) {{\n{script}\n}}"
                        )
                        logger.info(
                            "Queued %d localStorage item(s) for %s",
                            len(local_storage_items),
                            origin_url,
                        )
            except Exception as exc:
                logger.warning("Failed to inject auth cookies into context: %s", exc)
        return context
    except Exception as exc:
        logger.error("Failed to launch browser: %s", exc)
        raise


def _js(page: Page, script: str) -> object:
    try:
        return page.evaluate(f"() => {{\n{script}\n}}")
    except Exception as exc:
        logger.warning("JS execution failed: %s — script: %.80s...", exc, script)
        return None


def _setup_audio_capture(page: Page, captured_urls: list[str]) -> None:
    def on_request(request: object) -> None:
        try:
            url: str = getattr(request, "url", "")
            if _AUDIO_URL_RE.search(url):
                if url not in captured_urls:
                    captured_urls.append(url)
                    logger.debug("Captured audio URL: %s", url[:120])
        except Exception as exc:
            logger.debug("Request handler error: %s", exc)

    try:
        page.on("request", on_request)
    except Exception as exc:
        logger.warning("Failed to setup audio capture: %s", exc)


def _wait_for_login(page: Page, timeout_s: int = 180) -> bool:
    try:
        if "browse" in page.url and "login" not in page.url:
            logger.info("Already on browse page — session is active.")
            return True
    except Exception as exc:
        logger.debug("URL check error: %s", exc)
    try:
        page.wait_for_url(
            "**pristinestreaming.com/app/browse**", timeout=timeout_s * 1000
        )
        logger.info("Login confirmed — redirected to browse.")
        return True
    except Exception:
        logger.error("Login not detected within %ds.", timeout_s)
        return False


def _resolve_album_id(page: Page, code: str) -> int | None:
    """Search for album code and return the numeric album ID."""
    search_selector = ".pp-navbar__search__input"
    for attempt in range(3):
        logger.debug("resolve_album_id: attempt %d/3 for code '%s'", attempt + 1, code)
        try:
            page.click(search_selector)
        except Exception as exc:
            logger.debug("click search input failed (attempt %d): %s", attempt + 1, exc)
        _js(
            page,
            "var el=document.querySelector('.pp-navbar__search__input');"
            "if(el){el.value='';el.dispatchEvent(new Event('input',{bubbles:true}));}",
        )
        try:
            page.fill(search_selector, code)
        except Exception as exc:
            logger.debug(
                "fill failed (attempt %d): %s — using JS fallback", attempt + 1, exc
            )
            _js(
                page,
                "var el=document.querySelector('.pp-navbar__search__input');if(el){el.value='"
                + code
                + "';"
                + "el.dispatchEvent(new Event('input',{bubbles:true}));"
                + "el.dispatchEvent(new Event('change',{bubbles:true}));}",
            )
        _js(
            page,
            "var el=document.querySelector('.pp-navbar__search__input');if(el){"
            "el.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',keyCode:13,bubbles:true}));"
            "el.dispatchEvent(new KeyboardEvent('keyup',{key:'Enter',keyCode:13,bubbles:true}));}",
        )
        try:
            page.wait_for_load_state("networkidle", timeout=5000)
        except Exception:
            pass
        try:
            search_url = page.url
        except Exception as exc:
            logger.warning(
                "Could not read URL after search (attempt %d): %s", attempt + 1, exc
            )
            search_url = ""
        logger.debug("Search URL after submit: %s", search_url)
        if (
            code.lower() not in search_url.lower()
            and code[4:].lower() not in search_url.lower()
        ):
            logger.warning(
                "Search URL mismatch on attempt %d: %s — retrying...",
                attempt + 1,
                search_url,
            )
            try:
                page.goto(PRISTINE_APP, wait_until="domcontentloaded")
            except Exception as exc:
                logger.warning("goto PRISTINE_APP failed: %s", exc)
            continue
        clicked = False
        for sel in (
            "[href*='/albums/']",
            ".pp-browse-grid__item",
            ".pp-search-results__item",
        ):
            try:
                element = page.query_selector(sel)
            except Exception as exc:
                logger.debug("query_selector('%s') failed: %s", sel, exc)
                continue
            if element is None:
                continue
            logger.debug("Clicking selector '%s'", sel)
            try:
                page.click(sel)
            except Exception as exc:
                logger.warning("click('%s') raised %s — retrying...", sel, exc)
                try:
                    page.goto(PRISTINE_APP, wait_until="domcontentloaded")
                except Exception as nav_exc:
                    logger.debug("goto retry failed: %s", nav_exc)
                clicked = True
                break
            try:
                page.wait_for_load_state("domcontentloaded", timeout=10000)
            except Exception:
                pass
            try:
                current_url = page.url
            except Exception as exc:
                logger.warning("Could not read URL after click: %s", exc)
                current_url = ""
            logger.debug("Landed on URL: %s", current_url)
            if "/albums/" in current_url:
                try:
                    album_id = int(current_url.rstrip("/").split("/")[-1])
                except ValueError:
                    logger.debug(
                        "ValueError: could not parse album ID from '%s' — skipping",
                        current_url.rstrip("/").split("/")[-1],
                    )
                    clicked = True
                    break
                page_title: str = (
                    _js(
                        page,
                        "return document.querySelector('.pp-album-view__title')?.textContent?.trim()||''",
                    )
                    or ""
                )
                logger.debug("Page title: '%s' — checking for '%s'", page_title, code)
                if code.casefold() in page_title.casefold():
                    logger.debug("Resolved %s -> album_id=%d", code, album_id)
                    return album_id
                logger.warning(
                    "Album title '%s' does not match %s — retrying...",
                    page_title,
                    code,
                )
                try:
                    page.goto(PRISTINE_APP, wait_until="domcontentloaded")
                except Exception as exc:
                    logger.warning("goto PRISTINE_APP failed: %s", exc)
                clicked = True
                break
            clicked = True
            break
        if not clicked:
            logger.warning(
                "No clickable result found on attempt %d — retrying...", attempt + 1
            )
            try:
                page.goto(PRISTINE_APP, wait_until="domcontentloaded")
            except Exception as exc:
                logger.warning("goto PRISTINE_APP failed: %s", exc)
    return None


def _download_artwork_and_pdf(
    page: Page, album_out: str, album_title: str, session: requests.Session
) -> None:
    """Download album artwork and PDF booklet using URLs found on the page."""
    artwork_src: str = (
        _js(
            page,
            "return document.querySelector('.pp-album-view__artwork > img')?.src || ''",
        )
        or ""
    )
    if not artwork_src:
        logger.info("No artwork found — skipping.")
        return
    img_filename = artwork_src.rsplit("/", maxsplit=1)[-1]
    name_no_ext, img_ext = os.path.splitext(img_filename)
    img_dest = os.path.join(album_out, f"{album_title}{img_ext}")
    logger.info("Artwork -> %s", os.path.basename(img_dest))
    _download_file(artwork_src, img_dest, session)
    pdf_url = f"{S3_COVERS}{name_no_ext}.pdf"
    pdf_dest = os.path.join(album_out, f"{name_no_ext}.pdf")
    logger.info("PDF -> %s", os.path.basename(pdf_dest))
    if not _download_file(pdf_url, pdf_dest, session):
        logger.info("PDF not available.")


def _start_playback(page: Page) -> None:
    try:
        _debug_screenshot(page, "before_playback")
    except Exception as exc:
        logger.debug("debug screenshot failed: %s", exc)
    try:
        pp_classes = _js(
            page,
            "return [...new Set([...document.querySelectorAll('[class]')].flatMap(el=>[...el.classList]).filter(c=>c.startsWith('pp-')))].sort()",
        )
        logger.info("PP classes present: %s", pp_classes)
    except Exception as exc:
        logger.debug("pp class dump failed: %s", exc)
    try:
        play_buttons = _js(
            page,
            "return [...document.querySelectorAll('button,a,[role=button]')].filter(el=>el.offsetParent!==null).map(el=>({cls:el.className.substring(0,80),txt:el.textContent.trim().substring(0,30),tag:el.tagName}))",
        )
        logger.info("Visible buttons: %s", play_buttons)
    except Exception as exc:
        logger.debug("button dump failed: %s", exc)
    try:
        _js(
            page,
            "var t=document.querySelector('.pp-seekbar--togglebutton');if(t&&t.value!=='1')t.click();",
        )
    except Exception as exc:
        logger.debug("seekbar toggle failed: %s", exc)
    try:
        page.wait_for_selector(".pp-tracklist__item", timeout=15000)
    except Exception as exc:
        logger.warning("wait_for_selector .pp-tracklist__item failed: %s", exc)
    try:
        item_count = _js(
            page,
            "return document.querySelectorAll('.pp-tracklist__item').length",
        )
        playnow_count = _js(
            page,
            "return document.querySelectorAll('.pp-tracklist__item__playnow').length",
        )
        logger.debug("tracklist items=%s  playnow buttons=%s", item_count, playnow_count)
    except Exception as exc:
        logger.debug("tracklist count probe failed: %s", exc)
    clicked = False
    try:
        page.hover(".pp-tracklist__item")
        page.click(".pp-tracklist__item .pp-tracklist__item__playnow")
        clicked = True
        logger.debug("Clicked .pp-tracklist__item__playnow")
    except Exception as exc:
        logger.debug("playnow click failed: %s", exc)
    if not clicked:
        try:
            _js(
                page,
                "var btn=document.querySelector('.pp-tracklist__item .pp-tracklist__item__playnow');if(btn){btn.dispatchEvent(new MouseEvent('click',{bubbles:true,cancelable:true}));} else{var item=document.querySelector('.pp-tracklist__item');if(item)item.dispatchEvent(new MouseEvent('dblclick',{bubbles:true,cancelable:true}));}",
            )
            clicked = True
            logger.debug("JS dispatch click on playnow/item")
        except Exception as exc:
            logger.debug("JS dispatch click failed: %s", exc)
    try:
        page.wait_for_function(
            "!!document.querySelector('body > audio[src]')",
            timeout=5000,
        )
    except Exception:
        pass
    try:
        dur_info = _js(
            page,
            "var els=document.querySelectorAll('body > audio');"
            "return Array.from(els).map(function(e,i){"
            "return {index:i,src:e.getAttribute('src')||'(none)',paused:e.paused,"
            "duration:e.duration,readyState:e.readyState,networkState:e.networkState};});",
        )
        if dur_info:
            for elem in dur_info:
                dur = elem.get("duration")
                dur_str = f"{dur:.2f}s" if dur and dur == dur else "NaN"
                logger.debug(
                    "post-click audio[%s] src=%s paused=%s duration=%s rs=%s ns=%s",
                    elem.get("index"),
                    str(elem.get("src", ""))[:60],
                    elem.get("paused"),
                    dur_str,
                    elem.get("readyState"),
                    elem.get("networkState"),
                )
        else:
            logger.debug("post-click: no <audio> elements in DOM")
    except Exception as exc:
        logger.debug("post-click audio probe failed: %s", exc)
    try:
        _debug_screenshot(page, "after_playnow_click")
    except Exception:
        pass


def _parse_tracklist(page: Page) -> list[str]:
    """Return track titles visible in the album tracklist."""
    try:
        raw = _js(
            page,
            "return Array.from(document.querySelectorAll('.pp-tracklist__item__title')).map(function(el){return el.textContent.trim();})",
        )
        tracks: list[str] = list(raw) if raw else []
    except (TypeError, AttributeError, ValueError) as exc:
        logger.warning("parse_tracklist failed: %s", exc)
        tracks = []
    logger.debug("parse_tracklist: found %d track(s)", len(tracks))
    return tracks


def _debug_screenshot(page: Page, label: str) -> None:
    try:
        out_dir = os.path.join(
            os.getenv(
                "PRISTINE_OUT_DIR",
                r"C:\Users\Lance\Dev\Scripts\python\.tmp\pristine-live",
            ),
            "debug",
        )
        os.makedirs(out_dir, exist_ok=True)
        path = os.path.join(out_dir, f"{label}.png")
        page.screenshot(path=path, full_page=True)
        logger.info("Screenshot saved: %s", path)
    except Exception as exc:
        logger.debug("_debug_screenshot error: %s", exc)


def _debug_audio_state(page: Page) -> None:
    """Log the state of all audio elements on the page for diagnostics."""
    try:
        info = _js(
            page,
            "return Array.from(document.querySelectorAll('body > audio')).map(function(el,i){return {index:i,src:el.getAttribute('src')||'(none)',"
            + "paused:el.paused,currentTime:el.currentTime,duration:el.duration,readyState:el.readyState,networkState:el.networkState};});",
        )
    except Exception as exc:
        logger.debug("debug_audio_state failed: %s", exc)
        return
    if not info:
        logger.debug("No <audio> elements found in DOM")
        return
    try:
        for elem in info:
            src = str(elem["src"])
            file_id = src.rsplit("/", 1)[-1].split("?")[0] if "/" in src else src
            ready = _READY_STATE.get(int(elem["readyState"]), str(elem["readyState"]))
            net = _NET_STATE.get(int(elem["networkState"]), str(elem["networkState"]))
            dur = elem.get("duration")
            dur_str = f"{dur:.1f}s" if dur and dur == dur else "NaN"
            logger.debug(
                "audio[%d] id=%s paused=%s time=%.1f dur=%s ready=%s net=%s",
                elem["index"],
                file_id,
                elem["paused"],
                elem["currentTime"],
                dur_str,
                ready,
                net,
            )
    except (KeyError, TypeError, ValueError) as exc:
        logger.debug("debug_audio_state parse error: %s", exc)


def _get_active_audio_src(page: Page) -> str | None:
    """Return the src URL of the currently playing audio element."""
    result = _js(
        page,
        "var els=document.querySelectorAll('body > audio');for(var i=0;i<els.length;i++){var el=els[i];if(!el.paused&&el.hasAttribute('src'))return el.getAttribute('src');}return null;",
    )
    return str(result) if result else None


def _has_ready_paused_audio(page: Page) -> bool:
    """Return True when at least one audio element is fully buffered but still paused."""
    result = _js(
        page,
        "var els=document.querySelectorAll('body > audio');"
        "for(var i=0;i<els.length;i++){var el=els[i];"
        "if(el.paused&&el.readyState>=4&&el.hasAttribute('src'))return true;}"
        "return false;",
    )
    return bool(result)


def _get_track_title(page: Page, fallback: str) -> str:
    """Return the currently playing track title, or the fallback string."""
    t = _js(
        page,
        "return document.querySelector('.pp-playbar__now-playing__track')?.textContent?.trim()||''",
    )
    return str(t).strip() if t else fallback


def _click_forward(page: Page) -> None:
    """Click the next-track button."""
    _js(
        page,
        "var f=document.querySelector('.pp-play-controls__main__primary > li:nth-child(3) > button');if(f)f.click();",
    )


def _click_play(page: Page) -> None:
    """Click the play button."""
    _js(
        page,
        "var p=document.querySelector('.pp-play-controls__main__primary > li:nth-child(2) > button');if(p)p.click();",
    )


def _pause_all_audio(page: Page) -> None:
    """Pause all audio elements on the page."""
    _js(
        page,
        "document.querySelectorAll('body > audio').forEach(function(e){e.pause();});",
    )


def _download_single_album(context: BrowserContext, code: str, out_dir: str) -> None:
    """Download a single album: resolve, parse tracklist, capture streams, download files."""
    logger.info("--- %s ---", code)
    try:
        page = context.new_page()
    except Exception as exc:
        logger.error("Failed to open new page for %s: %s", code, exc)
        return
    try:
        try:
            page.goto(PRISTINE_APP, wait_until="domcontentloaded")
        except Exception as exc:
            logger.error("Navigation to PRISTINE_APP failed for %s: %s", code, exc)
            return
        if not _wait_for_login(page):
            logger.error("Skipping %s — could not log in.", code)
            return
        logger.info("Searching for %s...", code)
        album_id = _resolve_album_id(page, code)
        if album_id is None:
            logger.error("Could not resolve album ID for %s — skipping.", code)
            return
        logger.info("Resolved %s -> ID %d", code, album_id)
        try:
            page.goto(
                f"https://pristinestreaming.com/app/browse/albums/{album_id}",
                wait_until="domcontentloaded",
            )
        except Exception as exc:
            logger.error("Navigation to album page failed for %s: %s", code, exc)
            return
        try:
            page.wait_for_selector(".pp-album-view__title", timeout=30000)
        except Exception as exc:
            logger.warning("Timed out waiting for album title selector: %s", exc)
        raw_title: str = (
            _js(
                page,
                "return document.querySelector('.pp-album-view__title')?.textContent?.trim()||'Unknown Album'",
            )
            or "Unknown Album"
        )
        album_title = _sanitize_path_component(str(raw_title))
        logger.debug("Raw DOM title: '%s' -> sanitized: '%s'", raw_title, album_title)
        logger.info("Album: %s", album_title)
        try:
            album_out = os.path.join(out_dir, album_title)
            os.makedirs(album_out, exist_ok=True)
        except OSError as exc:
            logger.error("Cannot create album directory %s: %s", album_out, exc)
            return
        session = requests.Session()
        expected_tracks = _parse_tracklist(page)
        expected_count = len(expected_tracks)
        logger.info("Expected tracks: %d", expected_count)
        if expected_tracks:
            logger.debug(
                "Tracklist: %s",
                " | ".join(f"[{i:02d}] {t}" for i, t in enumerate(expected_tracks, 1)),
            )
        if expected_count == 0:
            logger.warning(
                "No tracks found in DOM tracklist — album may be empty or not loaded."
            )
        _download_artwork_and_pdf(page, album_out, album_title, session)
        captured_urls: list[str] = []
        _setup_audio_capture(page, captured_urls)
        logger.info("Starting playback...")
        _start_playback(page)
        _debug_audio_state(page)
        seen_urls: set[str] = set()
        seen_titles: set[str] = set()
        stall_count = 0
        track_num = 0
        logger.info(
            "Polling for audio streams (max wait per track: %.0fs)...",
            MAX_STALL_COUNT * POLL_SLEEP,
        )
        while stall_count < MAX_STALL_COUNT:
            src: str | None = None
            for candidate in captured_urls:
                if candidate not in seen_urls:
                    src = candidate
                    break
            if src is None:
                src = _get_active_audio_src(page)
            if src and src not in seen_urls:
                seen_urls.add(src)
                stall_count = 0
                track_num += 1
                raw_title_track = _get_track_title(page, f"Track {track_num:02d}")
                if raw_title_track in seen_titles:
                    logger.debug(
                        "Duplicate title '%s' (track %d) — all tracks complete",
                        raw_title_track,
                        track_num,
                    )
                    logger.info("Duplicate title '%s' — all tracks complete.", raw_title_track)
                    break
                seen_titles.add(raw_title_track)
                normalized_title = _normalize_track_title(raw_title_track)
                safe_title = _sanitize_path_component(normalized_title.title())
                ext = ".flac" if ".flac" in src else ".mp3"
                track_dest = os.path.join(album_out, f"{track_num:02d}. {safe_title}{ext}")
                logger.info("[%02d] %s%s", track_num, safe_title, ext)
                file_id = src.rsplit("/", 1)[-1].split("?")[0] if "/" in src else src
                logger.debug("New src: id=%s", file_id)
                _pause_all_audio(page)
                try:
                    success = _download_file(src, track_dest, session)
                    if not success:
                        logger.warning("Failed to download track: %s", track_dest)
                except Exception as exc:
                    logger.error("Download error for %s: %s", track_dest, exc)
                if expected_count > 0 and track_num >= expected_count:
                    logger.info(
                        "All %d expected tracks initiated — stopping.", expected_count
                    )
                    break
                time.sleep(POST_DL_WAIT)
                _click_forward(page)
                try:
                    page.wait_for_function(
                        "Array.from(document.querySelectorAll('body > audio')).some("
                        "a => a.hasAttribute('src') && a.readyState >= 2)",
                        timeout=4000,
                    )
                except Exception:
                    pass
                _click_play(page)
                try:
                    page.wait_for_function(
                        "Array.from(document.querySelectorAll('body > audio')).some("
                        "a => !a.paused)",
                        timeout=3000,
                    )
                except Exception:
                    pass
            else:
                stall_count += 1
                logger.debug(
                    "Stall %d/%d — no active src", stall_count, MAX_STALL_COUNT
                )
                _debug_audio_state(page)
                if _has_ready_paused_audio(page):
                    logger.debug("Audio buffered but paused — retrying play")
                    _click_play(page)
                elif stall_count == 5:
                    logger.debug(
                        "Stall 5 — audio still not loaded, retrying playnow click via JS"
                    )
                    try:
                        _js(
                            page,
                            "var btn=document.querySelector('.pp-tracklist__item .pp-tracklist__item__playnow');if(btn){btn.dispatchEvent(new MouseEvent('click',{bubbles:true,cancelable:true}));}",
                        )
                    except Exception as exc:
                        logger.debug("stall-retry playnow failed: %s", exc)
                time.sleep(POLL_SLEEP)
        downloaded_count = len(seen_titles)
        missing_on_disk: list[str] = []
        for title in seen_titles:
            normalized = _normalize_track_title(title)
            safe_check = _sanitize_path_component(normalized.title())
            has_file = any(
                os.path.exists(os.path.join(album_out, f"{track_num:02d}. {safe_check}{ext}"))
                and os.path.getsize(
                    os.path.join(album_out, f"{track_num:02d}. {safe_check}{ext}")
                ) > 0
                for ext in (".flac", ".mp3")
            )
            if not has_file:
                missing_on_disk.append(title)
        if missing_on_disk:
            logger.warning(
                "%d track(s) missing from disk after download:", len(missing_on_disk)
            )
            for m in missing_on_disk:
                logger.warning("MISSING: %s", m)
        if expected_count > 0 and downloaded_count < expected_count:
            missing_not_initiated = [t for t in expected_tracks if t not in seen_titles]
            logger.warning(
                "INCOMPLETE: initiated %d/%d tracks", downloaded_count, expected_count
            )
            for m in missing_not_initiated:
                logger.warning("NOT INITIATED: %s", m)
        elif expected_count > 0 and not missing_on_disk:
            logger.info(
                "OK %s complete — %d/%d tracks verified on disk in '%s'",
                code,
                downloaded_count,
                expected_count,
                album_out,
            )
        elif expected_count > 0:
            logger.warning(
                "%s — %d/%d tracks initiated but %d missing on disk",
                code,
                downloaded_count,
                expected_count,
                len(missing_on_disk),
            )
        else:
            logger.info(
                "OK %s complete — %d tracks in '%s'", code, downloaded_count, album_out
            )
        time.sleep(10)
    finally:
        try:
            page.close()
        except Exception as exc:
            logger.debug("page.close failed for %s: %s", code, exc)


def download_codes(
    codes: list[str] | None = None, out_dir: str | Path | None = None
) -> None:
    effective_codes = codes if codes else RELEASES
    dest = str(out_dir) if out_dir else BASE_OUT_DIR
    try:
        os.makedirs(dest, exist_ok=True)
    except OSError as exc:
        logger.error("Cannot create output directory %s: %s", dest, exc)
        return
    logger.info("download_codes: %d code(s), dest=%s", len(effective_codes), dest)
    with sync_playwright() as pw:
        try:
            context = _create_browser_context(pw)
        except Exception as exc:
            logger.error("PW cannot start browser — aborting: %s", exc)
            return
        try:
            seed_page = context.new_page()
            try:
                seed_page.goto(PRISTINE_APP, wait_until="domcontentloaded")
            except Exception as exc:
                logger.warning("seed_page goto failed: %s", exc)
            if not _wait_for_login(seed_page):
                logger.error("Could not log in to Pristine — aborting.")
                try:
                    context.close()
                except Exception:
                    pass
                return
            try:
                seed_page.close()
            except Exception as exc:
                logger.debug("seed_page.close failed: %s", exc)
        except Exception as exc:
            logger.error("PW seed page failed: %s", exc)
            try:
                context.close()
            except Exception:
                pass
            return
        if not _AUTH_PATH.exists():
            logger.error(
                "No session found at %s — run `pristine login` first.", _AUTH_PATH
            )
            try:
                context.close()
            except Exception:
                pass
            return
        total = len(effective_codes)
        try:
            for i, code in enumerate(effective_codes, 1):
                logger.info("Progress: %d/%d — %s", i, total, code)
                try:
                    _download_single_album(context, code, dest)
                except Exception as exc:
                    logger.error("PW album %s failed: %s", code, exc)
                time.sleep(3)
        except Exception as exc:
            logger.error("Download loop failed: %s", exc)
        finally:
            try:
                context.close()
            except Exception as exc:
                logger.debug("context.close failed: %s", exc)
    logger.info("ALL DOWNLOADS COMPLETE")


def login() -> None:
    logger.info(
        "Opening browser for Pristine login — log in then close or wait for auto-save."
    )
    with sync_playwright() as pw:
        try:
            context = pw.chromium.launch_persistent_context(
                _USER_DATA_DIR,
                channel="msedge",
                headless=False,
                accept_downloads=False,
            )
        except Exception as exc:
            logger.error("Cannot open browser: %s", exc)
            return
        try:
            page = context.new_page()
            page.goto(PRISTINE_APP, wait_until="domcontentloaded")
            already_in = False
            try:
                already_in = (
                    "login" not in page.url
                    and "browse" in page.url
                    and not page.locator("text=Browsing as guest").is_visible()
                )
            except Exception:
                pass
            if already_in:
                logger.info("Already logged in — saving session.")
            else:
                logger.info(
                    "Browser open — log in with your Pristine account (you have 5 minutes)."
                )
                page.goto("https://pristineclassical.com/pages/player-subscribe")
            if already_in or _wait_for_login(page, timeout_s=300):
                try:
                    _AUTH_PATH.parent.mkdir(parents=True, exist_ok=True)
                    context.storage_state(path=str(_AUTH_PATH))
                    logger.info("Session saved to %s", _AUTH_PATH)
                except Exception as exc:
                    logger.error("Failed to save session: %s", exc)
            else:
                logger.error("Login not completed — session NOT saved.")
        except Exception as exc:
            logger.error("login() failed: %s", exc)
        finally:
            try:
                context.close()
            except Exception:
                pass


if __name__ == "__main__":
    try:
        download_codes()
    except Exception as exc:
        logger.error("Top-level error: %s", exc)
