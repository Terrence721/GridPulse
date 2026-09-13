#!/usr/bin/env python3
"""Refreshes GridPulse's real household power dataset excerpt to the next real month.

Downloads the full UCI "Individual Household Electric Power Consumption" archive,
extracts the next real calendar month in sequence (wrapping December 2006 <-> November
2010), and writes it to src/MeterSimulator/Data/power-data-current-month.json. Advances
src/MeterSimulator/Data/current-month.json to match. Run monthly by
.github/workflows/refresh-power-dataset.yml; never run by the application itself.
"""

import io
import json
import urllib.request
import zipfile
from pathlib import Path

ARCHIVE_URL = "https://archive.ics.uci.edu/static/public/235/individual+household+electric+power+consumption.zip"
ARCHIVE_ENTRY_NAME = "household_power_consumption.txt"

# The real archive covers December 2006 through November 2010, inclusive.
ARCHIVE_START = (2006, 12)
ARCHIVE_END = (2010, 11)

REPO_ROOT = Path(__file__).resolve().parent.parent
DATA_DIR = REPO_ROOT / "src" / "MeterSimulator" / "Data"
CURRENT_MONTH_FILE = DATA_DIR / "current-month.json"
OUTPUT_FILE = DATA_DIR / "power-data-current-month.json"


def next_month(year: int, month: int) -> tuple[int, int]:
    if (year, month) == ARCHIVE_END:
        return ARCHIVE_START
    if month == 12:
        return year + 1, 1
    return year, month + 1


def download_archive() -> bytes:
    with urllib.request.urlopen(ARCHIVE_URL) as response:
        return response.read()


def extract_month(archive_bytes: bytes, year: int, month: int) -> list[list]:
    rows: list[list] = []
    with zipfile.ZipFile(io.BytesIO(archive_bytes)) as zf:
        with zf.open(ARCHIVE_ENTRY_NAME) as raw:
            text = io.TextIOWrapper(raw, encoding="utf-8")
            header = text.readline()
            for line in text:
                fields = line.rstrip("\n").split(";")
                date, time, global_active_power = fields[0], fields[1], fields[2]
                if global_active_power == "?":
                    continue
                day, mon, yr = (int(part) for part in date.split("/"))
                if yr == year and mon == month:
                    rows.append([date, time, float(global_active_power)])
    return rows


def main() -> None:
    current = json.loads(CURRENT_MONTH_FILE.read_text(encoding="utf-8"))
    target_year, target_month = next_month(current["year"], current["month"])

    print(f"Refreshing to {target_year}-{target_month:02d}...")
    archive_bytes = download_archive()
    rows = extract_month(archive_bytes, target_year, target_month)

    if not rows:
        raise SystemExit(
            f"No real rows found for {target_year}-{target_month:02d}; "
            "refusing to write an empty dataset."
        )

    OUTPUT_FILE.write_text(json.dumps(rows), encoding="utf-8")
    CURRENT_MONTH_FILE.write_text(
        json.dumps({"year": target_year, "month": target_month}, indent=2) + "\n",
        encoding="utf-8",
    )
    print(f"Wrote {len(rows)} real rows for {target_year}-{target_month:02d}.")


if __name__ == "__main__":
    main()
