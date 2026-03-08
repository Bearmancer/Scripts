"""Pristine Classical streaming downloader.

Downloads FLAC albums from Pristine Classical via browser automation
(botasaurus + Chrome DevTools Protocol).

Environment variables:
    PRISTINE_HEADLESS     '1' for headless mode (default: 0 — visible browser).
    PRISTINE_BROWSER_EXE  Path to browser executable.  Defaults to Edge on Windows,
                          auto-detected on Linux.
    PRISTINE_OUT_DIR      Download destination directory.
"""
from __future__ import annotations

import os
import time
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

import requests
from botasaurus.browser import Driver, Wait, browser

from toolkit.logging_config import get_logger

logger = get_logger("pristine")

# ── Browser / env config ──────────────────────────────────────────────────────
# Vivaldi cannot be used: it redirects new launches to the existing running
# instance instead of spawning a separate CDP-controllable process.
_default_browser = (
    r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe"
    if os.name == "nt"
    else ""
)
BROWSER_EXE: str | None = os.getenv("PRISTINE_BROWSER_EXE", _default_browser) or None
BROWSER_ARGS: list[str] = []
HEADLESS: bool = os.getenv("PRISTINE_HEADLESS", "0") == "1"

# ── Download destination ──────────────────────────────────────────────────────
BASE_OUT_DIR: str = os.getenv(
    "PRISTINE_OUT_DIR",
    r"C:\Users\Lance\My Drive\Sir Fapsalot\Pristine Classical",
)

# ── Release codes ─────────────────────────────────────────────────────────────
TOSCANINI_BEETHOVEN = [
    "PASC552", "PASC553", "PASC554",
    "PASC555", "PASC556", "PASC557",
]

GENERAL = [
    "PASC762", "PASC648", "PASC313", "PASC003", "PASC040", "PASC393",
    "PASC626", "PASC653", "PASC246", "PASC619", "PASC731", "PASC760",
    "PACO180", "PASC569", "PASC669", "PASC633", "PASC655", "PASC741",
    "PASC736", "PASC131", "PASC006", "PASC759", "PASC764", "PASC486",
    "PASC450", "PASC443", "PASC447", "PAKM059",
]

STOKOWSKI = [
    "PASC591", "PASC596", "PASC531", "PASC609", "PASC625", "PASC379",
    "PASC161", "PASC133", "PASC182", "PASC602", "PASC536", "PASC587",
    "PASC629",
]

ALL_RELEASES: list[tuple[str, list[str]]] = [
    ("Toscanini Beethoven Cycles", TOSCANINI_BEETHOVEN),
    ("General", GENERAL),
    ("Stokowski", STOKOWSKI),
]

# ── Protocol constants ────────────────────────────────────────────────────────
S3_COVERS = "https://s3-eu-west-1.amazonaws.com/pristine-classical-storage/covers/"
PRISTINE_APP = "https://pristinestreaming.com/app/browse"
POLL_SLEEP = 1.0
POST_DL_WAIT = 2.0
MAX_STALL_COUNT = 60
AUTO_OVERWRITE = True


# ── Helpers ───────────────────────────────────────────────────────────────────
def _js(driver: Driver, script: str) -> object:
    """Run synchronous JS and return result."""
    return driver.run_js(script)


def _download_file(url: str, dest: str, session: requests.Session) -> bool:
    """Stream-download url → dest. Returns True on success."""
    if not AUTO_OVERWRITE and os.path.exists(dest) and os.path.getsize(dest) > 0:
        logger.info("SKIP (already exists): %s", os.path.basename(dest))
        return True

    part_dest = dest + ".part"
    try:
        import time as _time
        t0 = _time.time()
        r = session.get(url, stream=True, timeout=60)
        if r.status_code != 200:
            logger.warning("HTTP %s: %s", r.status_code, url)
            return False

        downloaded_bytes = 0
        with open(part_dest, "wb") as fp:
            for chunk in r.iter_content(chunk_size=131072):
                if chunk:
                    fp.write(chunk)
                    downloaded_bytes += len(chunk)

        os.replace(part_dest, dest)
        dt = _time.time() - t0
        speed = (downloaded_bytes / 1024 / 1024) / dt if dt > 0 else 0
        logger.info("DL complete: %s  (%.2f MB/s)", os.path.basename(dest), speed)
        return True
    except (requests.RequestException, OSError) as exc:
        logger.warning("Error downloading %s: %s", os.path.basename(dest), exc)
        if os.path.exists(part_dest):
            try:
                os.remove(part_dest)
            except OSError:
                logger.debug("Failed to clean up partial file: %s", part_dest)
        return False


def _wait_for_login(driver: Driver) -> bool:
    """Return True once browse is accessible, False on timeout."""
    url: str = _js(driver, "return window.location.href") or ""  # type: ignore[assignment]
    if "browse" in url and "login" not in url:
        return True
    logger.info("Not logged in — waiting up to 3 minutes for manual login...")
    for _ in range(90):
        time.sleep(2)
        url = _js(driver, "return window.location.href") or ""  # type: ignore[assignment]
        if "browse" in url and "login" not in url:
            logger.info("Login detected — continuing.")
            return True
    logger.error("Login timeout.")
    return False


def _resolve_album_id(driver: Driver, code: str) -> int | None:
    """Search for code in the UI, click first result, return numeric album ID."""
    for attempt in range(3):
        logger.debug("resolve_album_id: attempt %d/3 for code '%s'", attempt + 1, code)
        search_input = ".pp-navbar__search__input"
        (
            driver.triple_click(search_input)  # type: ignore[attr-defined]
            if hasattr(driver, "triple_click")
            else driver.click(search_input)
        )
        _js(
            driver,
            "var el=document.querySelector('.pp-navbar__search__input');"
            + "if(el){el.value='';el.dispatchEvent(new Event('input',{bubbles:true}));}",
        )
        driver.sleep(0)
        driver.type(search_input, code)
        _js(
            driver,
            "var el=document.querySelector('.pp-navbar__search__input');"
            + "if(el){"
            + "el.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',keyCode:13,bubbles:true}));"
            + "el.dispatchEvent(new KeyboardEvent('keyup',{key:'Enter',keyCode:13,bubbles:true}));}",
        )
        time.sleep(2.5)

        search_url: str = _js(driver, "return window.location.href") or ""  # type: ignore[assignment]
        logger.debug("Search URL after submit: %s", search_url)
        if (
            code.lower() not in search_url.lower()
            and code[4:].lower() not in search_url.lower()
        ):
            logger.warning("Search URL mismatch on attempt %d: %s — retrying...", attempt + 1, search_url)
            driver.get(PRISTINE_APP)
            time.sleep(2)
            continue

        clicked = False
        for sel in ["[href*='/albums/']", ".pp-browse-grid__item", ".pp-search-results__item"]:
            if driver.is_element_present(sel):
                logger.debug("Clicking selector '%s'", sel)
                driver.click(sel)
                time.sleep(3)
                url: str = _js(driver, "return window.location.href") or ""  # type: ignore[assignment]
                logger.debug("Landed on URL: %s", url)
                if "/albums/" in url:
                    try:
                        album_id = int(url.rstrip("/").split("/")[-1])
                    except ValueError:
                        logger.debug("ValueError: could not parse album ID from '%s' — skipping", url.rstrip("/").split("/")[-1])
                        clicked = True
                        break
                    page_title: str = (
                        _js(driver, "return document.querySelector('.pp-album-view__title')?.textContent?.trim()||''")  # type: ignore[assignment]
                        or ""
                    )
                    logger.debug("Page title: '%s' — checking for '%s'", page_title, code)
                    if code.upper() in page_title.upper():
                        logger.debug("Resolved %s → album_id=%d", code, album_id)
                        return album_id
                    logger.warning("Album title '%s' does not match %s — retrying...", page_title, code)
                    driver.get(PRISTINE_APP)
                    time.sleep(2)
                    clicked = True
                    break
                clicked = True
                break
        if not clicked:
            logger.warning("No clickable result found on attempt %d — retrying...", attempt + 1)
            driver.get(PRISTINE_APP)
            time.sleep(2)

    return None


def _download_artwork_and_pdf(
    driver: Driver,
    album_out: str,
    album_title: str,
    session: requests.Session,
) -> None:
    """Download cover image and PDF booklet."""
    artwork_src: str = (
        _js(driver, "return document.querySelector('.pp-album-view__artwork > img')?.src || ''")  # type: ignore[assignment]
        or ""
    )
    if not artwork_src:
        logger.info("No artwork found — skipping.")
        return

    img_filename = artwork_src.rsplit("/", maxsplit=1)[-1]
    name_no_ext, img_ext = os.path.splitext(img_filename)

    img_dest = os.path.join(album_out, f"{album_title}{img_ext}")
    logger.info("Artwork → %s", os.path.basename(img_dest))
    _download_file(artwork_src, img_dest, session)

    pdf_url = f"{S3_COVERS}{name_no_ext}.pdf"
    pdf_dest = os.path.join(album_out, f"{name_no_ext}.pdf")
    logger.info("PDF    → %s", os.path.basename(pdf_dest))
    if not _download_file(pdf_url, pdf_dest, session):
        logger.info("PDF not available.")


def _start_playback(driver: Driver) -> None:
    """Click Play if not already playing, enable FLAC toggle."""
    _js(
        driver,
        "var t=document.querySelector('.pp-seekbar--togglebutton');if(t&&t.value!=='1')t.click();",
    )
    _js(
        driver,
        "var tr=document.querySelector('.pp-playbar__now-playing__track');"
        + "if(!tr){var b=document.querySelector('.pp-album-view__action');if(b)b.click();}",
    )
    time.sleep(1.5)
    _js(
        driver,
        "var p=document.querySelector('.pp-play-controls__main__primary > li:nth-child(2) > button');"
        + "if(p){var i=p.querySelector('i');if(i&&i.classList.contains('fa-play'))p.click();}",
    )
    time.sleep(3)


def _parse_tracklist(driver: Driver) -> list[str]:
    """Return track title strings from .pp-tracklist__item__title elements."""
    try:
        raw = _js(
            driver,
            "return Array.from(document.querySelectorAll('.pp-tracklist__item__title'))"
            + ".map(function(el){return el.textContent.trim();})",
        )
        tracks: list[str] = list(raw) if raw else []  # type: ignore[arg-type]
    except (TypeError, AttributeError, ValueError) as exc:
        logger.warning("parse_tracklist failed: %s", exc)
        tracks = []
    logger.debug("parse_tracklist: found %d track(s)", len(tracks))
    return tracks


def _debug_audio_state(driver: Driver) -> None:
    """Log debug info about all audio elements in the DOM."""
    info = _js(
        driver,
        "return Array.from(document.querySelectorAll('body > audio')).map(function(el,i){"
        + "  return {index:i,src:el.getAttribute('src')||'(none)',"
        + "    paused:el.paused,currentTime:el.currentTime,"
        + "    readyState:el.readyState,networkState:el.networkState};});",
    )
    if not info:
        logger.debug("No <audio> elements found in DOM")
        return
    for elem in info:  # type: ignore[union-attr]
        src = str(elem["src"])
        logger.debug(
            "audio[%d] src=%s%s paused=%s time=%.1f ready=%s net=%s",
            elem["index"],
            src[:80],
            "…" if len(src) > 80 else "",
            elem["paused"],
            elem["currentTime"],
            elem["readyState"],
            elem["networkState"],
        )


def _get_active_audio_src(driver: Driver) -> str | None:
    """Return src of the currently active (not paused) audio element, or None."""
    result = _js(
        driver,
        "var els=document.querySelectorAll('body > audio');"
        + "for(var i=0;i<els.length;i++){"
        + "  var el=els[i];"
        + "  if(!el.paused&&el.hasAttribute('src'))return el.getAttribute('src');}"
        + "return null;",
    )
    return str(result) if result else None


def _get_track_title(driver: Driver, fallback: str) -> str:
    """Return the currently displayed track title."""
    t = _js(
        driver,
        "return document.querySelector('.pp-playbar__now-playing__track')?.textContent?.trim()||''",
    )
    return str(t).strip() if t else fallback


def _click_forward(driver: Driver) -> None:
    """Click the forward/next button."""
    _js(
        driver,
        "var f=document.querySelector('.pp-play-controls__main__primary > li:nth-child(3) > button');"
        + "if(f)f.click();",
    )


def _pause_all_audio(driver: Driver) -> None:
    """Pause all audio elements so they don't buffer during download."""
    _js(driver, "document.querySelectorAll('body > audio').forEach(function(e){e.pause();});")


# ── Core download task (botasaurus-decorated) ─────────────────────────────────
@browser(
    profile="pristine_account",
    headless=HEADLESS,
    chrome_executable_path=BROWSER_EXE,
    add_arguments=BROWSER_ARGS,
    remove_default_browser_check_argument=True,
)
def _download_single_album(driver: Driver, data: dict[str, str]) -> None:
    """Download one album end-to-end: search → artwork/PDF → all tracks."""
    code = data["code"]
    out_dir = data["out_dir"]
    group = data.get("group", "")

    prefix = f"[{group}] " if group else ""
    logger.info("─" * 20 + f"  {prefix}{code}  " + "─" * 20)

    driver.get(PRISTINE_APP)
    time.sleep(3)

    if not _wait_for_login(driver):
        logger.error("Skipping %s — could not log in.", code)
        return

    logger.info("Searching for %s...", code)
    album_id = _resolve_album_id(driver, code)

    if album_id is None:
        logger.error("Could not resolve album ID for %s — skipping.", code)
        return

    logger.info("Resolved %s → ID %d", code, album_id)

    driver.get(f"https://pristinestreaming.com/app/browse/albums/{album_id}")
    driver.wait_for_element(".pp-album-view__title", wait=Wait.LONG)

    raw_title: str = (
        _js(driver, "return document.querySelector('.pp-album-view__title')?.textContent?.trim()||'Unknown Album'")  # type: ignore[assignment]
        or "Unknown Album"
    )
    album_title = raw_title.replace(":", " -")
    logger.debug("Raw DOM title: '%s' → sanitized: '%s'", raw_title, album_title)
    logger.info("Album: %s", album_title)

    album_out = os.path.join(out_dir, album_title)
    os.makedirs(album_out, exist_ok=True)

    session = requests.Session()

    expected_tracks = _parse_tracklist(driver)
    expected_count = len(expected_tracks)
    logger.info("Expected tracks: %d", expected_count)
    for idx, track_name in enumerate(expected_tracks, 1):
        logger.info("  [%02d] %s", idx, track_name)

    if expected_count == 0:
        logger.warning("No tracks found in DOM tracklist — album may be empty or not loaded.")

    _download_artwork_and_pdf(driver, album_out, album_title, session)

    logger.info("Starting playback...")
    _start_playback(driver)
    _debug_audio_state(driver)

    seen_urls: set[str] = set()
    seen_titles: set[str] = set()
    stall_count = 0
    track_num = 0

    logger.info("Polling for audio streams (max wait per track: %.0fs)...", MAX_STALL_COUNT * POLL_SLEEP)

    with ThreadPoolExecutor(max_workers=5) as executor:
        while stall_count < MAX_STALL_COUNT:
            src = _get_active_audio_src(driver)

            if src and src not in seen_urls:
                seen_urls.add(src)
                stall_count = 0
                track_num += 1

                title = _get_track_title(driver, f"Track {track_num:02d}")

                if title in seen_titles:
                    logger.debug("Duplicate title '%s' (track %d) — all tracks complete", title, track_num)
                    logger.info("Duplicate title '%s' — all tracks complete.", title)
                    break
                seen_titles.add(title)

                ext = ".flac" if ".flac" in src else ".mp3"
                track_dest = os.path.join(album_out, f"{title}{ext}")

                logger.info("  [%02d] %s%s", track_num, title, ext)
                short = src[:25] + "..." + src[-22:] if len(src) > 50 else src
                logger.debug("New src: %s", short)

                _pause_all_audio(driver)
                executor.submit(_download_file, src, track_dest, session)

                if expected_count > 0 and track_num >= expected_count:
                    logger.info("All %d expected tracks initiated — stopping.", expected_count)
                    break

                time.sleep(4.0)
                _click_forward(driver)
                time.sleep(1.0)
            else:
                stall_count += 1
                if stall_count % 10 == 0:
                    logger.debug("Stall count: %d/%d — waiting for audio...", stall_count, MAX_STALL_COUNT)
                    _debug_audio_state(driver)
                time.sleep(POLL_SLEEP)

    # ── Completion verification ───────────────────────────────────────────────
    downloaded_count = len(seen_titles)
    missing_on_disk: list[str] = []

    for title in seen_titles:
        if not any(
            os.path.exists(os.path.join(album_out, f"{title}{ext}"))
            and os.path.getsize(os.path.join(album_out, f"{title}{ext}")) > 0
            for ext in (".flac", ".mp3")
        ):
            missing_on_disk.append(title)

    if missing_on_disk:
        logger.warning("%d track(s) missing from disk after download:", len(missing_on_disk))
        for m in missing_on_disk:
            logger.warning("  MISSING: %s", m)

    if expected_count > 0 and downloaded_count < expected_count:
        missing_not_initiated = [t for t in expected_tracks if t not in seen_titles]
        logger.warning("INCOMPLETE: initiated %d/%d tracks", downloaded_count, expected_count)
        for m in missing_not_initiated:
            logger.warning("  NOT INITIATED: %s", m)
    elif expected_count > 0 and not missing_on_disk:
        logger.info(
            "✓ %s complete — %d/%d tracks verified on disk in '%s'",
            code, downloaded_count, expected_count, album_out,
        )
    elif expected_count > 0:
        logger.warning(
            "%s — %d/%d tracks initiated but %d missing on disk",
            code, downloaded_count, expected_count, len(missing_on_disk),
        )
    else:
        logger.info(
            "✓ %s complete — %d tracks in '%s'",
            code, downloaded_count, album_out,
        )
    time.sleep(10)


# ── Public API ────────────────────────────────────────────────────────────────
def download_codes(codes: list[str], out_dir: str | Path | None = None, group: str = "") -> None:
    """Download specific release codes.

    Args:
        codes: List of release codes (e.g. ['PASC552', 'PASC553']).
        out_dir: Destination directory. Falls back to PRISTINE_OUT_DIR / BASE_OUT_DIR.
        group: Optional group label shown in log output.
    """
    dest = str(out_dir) if out_dir else BASE_OUT_DIR
    os.makedirs(dest, exist_ok=True)

    total = len(codes)
    for i, code in enumerate(codes, 1):
        logger.info("Progress: %d/%d — %s", i, total, code)
        _download_single_album(data={"code": code, "out_dir": dest, "group": group})  # type: ignore[call-arg]
        time.sleep(3)


def download_all(out_dir: str | Path | None = None) -> None:
    """Download all configured releases (Toscanini, General, Stokowski).

    Args:
        out_dir: Destination directory. Falls back to PRISTINE_OUT_DIR / BASE_OUT_DIR.
    """
    dest = str(out_dir) if out_dir else BASE_OUT_DIR
    os.makedirs(dest, exist_ok=True)

    total = sum(len(codes) for _, codes in ALL_RELEASES)
    done = 0

    for group_name, codes in ALL_RELEASES:
        logger.info("─" * 20 + f"  GROUP: {group_name}  ({len(codes)} albums)  " + "─" * 20)
        for code in codes:
            done += 1
            logger.info("Progress: %d/%d — %s", done, total, code)
            _download_single_album(data={"code": code, "out_dir": dest, "group": group_name})  # type: ignore[call-arg]
            time.sleep(3)

    logger.info("─" * 20 + "  ALL DOWNLOADS COMPLETE  " + "─" * 20)
