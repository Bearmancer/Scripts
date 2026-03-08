"""Last.fm scrobble sync to Google Sheets.

Syncs recent Last.fm scrobbles to a Google Sheets spreadsheet.
Requires the following environment variables:
    - LASTFM_API_KEY: Last.fm API key
    - LASTFM_API_SECRET: Last.fm API secret

Google Sheets authentication uses OAuth2 credentials stored at:
    ~/Services/last.fm Scrobble Updater/Google Sheets Credentials.json
    ~/Services/last.fm Scrobble Updater/token.json

Usage:
    toolkit lastfm
"""
import os
import pickle
from datetime import datetime
from pathlib import Path

import google.auth.exceptions
import gspread
import gspread.exceptions
import pylast
from google.auth.transport.requests import Request

from toolkit.logging_config import get_logger


SCOPES = ["https://www.googleapis.com/auth/spreadsheets"]
CREDS_FILE = (
    Path.home()
    / "Services"
    / "last.fm Scrobble Updater"
    / "Google Sheets Credentials.json"
)
TOKEN_FILE = Path.home() / "Services" / "last.fm Scrobble Updater" / "token.json"
SHEET_ID = "1scv0dBa7iGx0hQTqmMwvzceoZlyiRSjswz80FCO1cco"
SHEET_NAME = "last.fm scrobbles"
LASTFM_USERNAME = "kanishknishar"


def _get_api_key() -> str:
    """Get Last.fm API key from environment or raise error."""
    key = os.getenv("LASTFM_API_KEY")
    if not key:
        raise ValueError("LASTFM_API_KEY environment variable not set")
    return key


def _get_api_secret() -> str:
    """Get Last.fm API secret from environment or raise error."""
    secret = os.getenv("LASTFM_API_SECRET")
    if not secret:
        raise ValueError("LASTFM_API_SECRET environment variable not set")
    return secret


logger = get_logger("lastfm")


def authenticate_google_sheets() -> gspread.Client:
    """Authenticate with Google Sheets using stored credentials."""
    if not TOKEN_FILE.exists():
        raise FileNotFoundError(f"Google Sheets token not found: {TOKEN_FILE}")

    with open(TOKEN_FILE, "rb") as token:
        try:
            creds = pickle.load(token)
        except (pickle.UnpicklingError, EOFError) as e:
            raise ValueError(f"Corrupted token file: {TOKEN_FILE}") from e

        if creds and creds.expired and creds.refresh_token:
            try:
                creds.refresh(Request())
            except google.auth.exceptions.RefreshError as e:
                error_msg = str(e)
                logger.error(f"Google credentials refresh failed: {error_msg}")
                if "invalid_grant" in error_msg.lower() or "expired" in error_msg.lower():
                    logger.error("Refresh token expired — re-authenticate with OAuth flow")
                raise

        with open(TOKEN_FILE, "wb") as token_write:
            pickle.dump(creds, token_write)

    return gspread.authorize(creds)


def get_last_scrobble_timestamp(sheet: gspread.Worksheet) -> datetime:
    """Get the timestamp of the most recent scrobble in the sheet."""
    try:
        values = sheet.get(range_name="A2:A2")
    except gspread.exceptions.APIError as e:
        logger.error(f"Google Sheets API error (HTTP {e.response.status_code}): {e.error.get('message', str(e))}")
        raise

    if not values or not values[0]:
        raise ValueError("No existing scrobbles found.")

    last_timestamp_str = str(values[0][0])
    logger.info(f"Last scrobble timestamp: {last_timestamp_str}")

    return datetime.strptime(last_timestamp_str, "%Y-%m-%d %H:%M:%S")


def prepare_track_data(tracks: list[pylast.PlayedTrack]) -> list[list[str]]:
    """Convert track objects to row data for the sheet."""
    values: list[list[str]] = []
    for track in tracks:
        timestamp = track.timestamp
        if timestamp is None:
            continue
        timestamp_str = datetime.fromtimestamp(int(timestamp)).strftime(
            "%Y-%m-%d %H:%M:%S"
        )
        title: str = str(track.track.title) if track.track.title else ""
        album: str = str(track.album) if track.album else ""
        artist_name: str = str(track.track.artist.name) if track.track.artist else ""
        values.append(
            [
                timestamp_str,
                title,
                album,
                artist_name,
            ]
        )
    return values


def update_scrobbles() -> None:
    """Fetch new scrobbles from Last.fm and add them to Google Sheets."""
    client = authenticate_google_sheets()
    sheet = client.open_by_key(SHEET_ID).worksheet(SHEET_NAME)

    last_datetime = get_last_scrobble_timestamp(sheet)

    network = pylast.LastFMNetwork(
        api_key=_get_api_key(),
        api_secret=_get_api_secret(),
        username=LASTFM_USERNAME,
    )
    user = network.get_user(LASTFM_USERNAME)

    last_unix_timestamp = int(last_datetime.timestamp()) + 1

    try:
        new_tracks = list(user.get_recent_tracks(time_from=last_unix_timestamp))
    except pylast.WSError as e:
        logger.error(f"Last.fm API error [{e.status}]: {e}")
        raise
    except pylast.NetworkError as e:
        logger.error(f"Last.fm network error: {e}")
        raise

    if not new_tracks:
        logger.info(f"No new scrobbles since {last_datetime}.")
        return

    track_data = prepare_track_data(new_tracks)

    sorted_new_data = sorted(
        track_data,
        key=lambda row: datetime.strptime(row[0], "%Y-%m-%d %H:%M:%S"),
        reverse=True,
    )

    try:
        sheet.insert_rows(values=sorted_new_data, row=2)
        logger.info(f"Added {len(sorted_new_data)} new scrobbles to the sheet.")
    except gspread.exceptions.APIError as e:
        logger.error(f"Failed to insert scrobbles into sheet (HTTP {e.response.status_code}): {e.error.get('message', str(e))}")
        raise
