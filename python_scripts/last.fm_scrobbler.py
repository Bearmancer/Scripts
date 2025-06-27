import pylast
import os
import csv
import time
import logging
import pandas as pd
from pathlib import Path
from datetime import datetime

USERNAME = "kanishknishar"
CUTOFF_DATE_STRING = "2025-06-26 5:16:39"

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger()


def lastfmconnect():
    api_key = os.environ.get("LASTFM_API_KEY")
    api_secret = os.environ.get("LASTFM_API_SECRET")
    password = os.environ.get("LASTFM_PASSWORD")

    if password and len(password) != 32:
        password_hash = pylast.md5(password)
    else:
        password_hash = password

    return pylast.LastFMNetwork(
        api_key=api_key,
        api_secret=api_secret,
        username=USERNAME,
        password_hash=password_hash,
    )


def fetch_new_tracks(user, hard_cutoff_timestamp):
    tracks, last_timestamp = [], None

    while True:
        batch = user.get_recent_tracks(limit=100, time_to=last_timestamp)
        filtered = [t for t in batch if int(t.timestamp) >= hard_cutoff_timestamp]

        if not filtered:
            break

        tracks.extend(filtered)
        logger.info(f"Now scraped: {len(tracks)} tracks")
        last_timestamp = batch[-1].timestamp if batch else None
        time.sleep(0.2)

    return tracks


def main():
    logger.info("Connecting to Last.fm API...")
    network = lastfmconnect()
    user = network.get_user(USERNAME)

    logger.info(f"Fetching timeline for: {USERNAME}")

    cutoff_timestamp = int(datetime.strptime(CUTOFF_DATE_STRING, "%Y-%m-%d %H:%M:%S").timestamp()) if CUTOFF_DATE_STRING else 0
    tracks = fetch_new_tracks(user, hard_cutoff_timestamp=cutoff_timestamp)

    timeline = []
    for track in tracks:
        timestamp = int(track.timestamp)
        formatted_date_time = datetime.fromtimestamp(timestamp).strftime('%Y-%m-%d %H:%M:%S')
        timeline.append({
            "Date": formatted_date_time,
            "Title": track.track.title,
            "Artist": track.track.artist.name,
            "Album": track.album if track.album else "",
        })

    df = pd.DataFrame(timeline)

    string_columns = df.select_dtypes(include="object").columns
    df[string_columns] = df[string_columns].applymap(lambda value: value.strip() if isinstance(value, str) else value)

    max_date = df["Date"].max().replace(":", ".")

    output_file = Path.home() / "Desktop" / f"last.fm scrobbles - {USERNAME} - until {max_date}.csv"

    df.to_csv(
        output_file,
        sep=',',
        index=False,
        columns=["Date", "Title", "Album", "Artist"],
        quoting=csv.QUOTE_ALL,
        quotechar='"',
        escapechar='\\',
        encoding='utf-8',
        doublequote=False,
        na_rep=''
    )

    logger.info(f"Done. Exported {len(df)} tracks to: {output_file}")


if __name__ == "__main__":
    main()