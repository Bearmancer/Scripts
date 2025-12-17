from pathlib import Path
from datetime import datetime
from google.auth.transport.requests import Request
import gspread
import pylast
import os
import pickle

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
LASTFM_API_KEY = os.getenv("LASTFM_API_KEY")
LASTFM_API_SECRET = os.getenv("LASTFM_API_SECRET")


def authenticate_google_sheets():
    with open(TOKEN_FILE, "rb") as token:
        creds = pickle.load(token)

        if creds and creds.expired and creds.refresh_token:
            creds.refresh(Request())

        with open(TOKEN_FILE, "wb") as token:
            pickle.dump(creds, token)

    return gspread.authorize(creds)


def get_last_scrobble_timestamp(sheet):
    try:
        values = sheet.get(range_name="A2:A2")

        if not values or not values[0]:
            raise ValueError("No existing scrobbles found.")

        last_timestamp_str = values[0][0]

        print(f"Last scrobble timestamp: {last_timestamp_str}")

        return datetime.strptime(last_timestamp_str, "%Y-%m-%d %H:%M:%S")

    except Exception as e:
        print(f"Error reading last timestamp: {e}")
        raise e


def prepare_track_data(tracks):
    values = []
    for track in tracks:
        timestamp_str = datetime.fromtimestamp(int(track.timestamp)).strftime(
            "%Y-%m-%d %H:%M:%S"
        )
        values.append(
            [
                timestamp_str,
                track.track.title,
                track.album if track.album else "",
                track.track.artist.name,
            ]
        )
    return values


def main():
    client = authenticate_google_sheets()
    sheet = client.open_by_key(SHEET_ID).worksheet(SHEET_NAME)

    last_datetime = get_last_scrobble_timestamp(sheet)

    network = pylast.LastFMNetwork(
        api_key=LASTFM_API_KEY, api_secret=LASTFM_API_SECRET, username=LASTFM_USERNAME
    )
    user = network.get_user(LASTFM_USERNAME)

    last_unix_timestamp = int(last_datetime.timestamp()) + 1

    new_tracks = list(user.get_recent_tracks(time_from=last_unix_timestamp, limit=None))

    if not new_tracks:
        print(f"No new scrobbles since {last_datetime}.")
        return

    track_data = prepare_track_data(new_tracks)

    sorted_new_data = sorted(
        track_data,
        key=lambda row: datetime.strptime(row[0], "%Y-%m-%d %H:%M:%S"),
        reverse=True,
    )

    sheet.insert_rows(values=sorted_new_data, row=2)
    print(f"Added {len(sorted_new_data)} new scrobbles to the sheet.")


if __name__ == "__main__":
    main()
