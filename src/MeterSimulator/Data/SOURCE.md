# Data source: real household electricity consumption

`power-data-current-month.json` is a real, unmodified excerpt of a published dataset — not synthetic or fabricated data. Its content refreshes monthly (see "How it stays current" below); check `current-month.json` for exactly which real month is loaded right now.

## Source

**Individual Household Electric Power Consumption**
Georges Hebrail and Alice Berard, 2012.
UCI Machine Learning Repository. https://doi.org/10.24432/C58K54

Real minute-level electricity measurements from one household in Sceaux, France (7km from Paris), recorded December 2006 – November 2010. Licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).

## Shape

A JSON array of `[date, time, globalActivePowerKw]` tuples, e.g. `["1/2/2007", "00:00:00", 0.326]` — `globalActivePowerKw` is the household's real average active power draw for that minute, in kilowatts.

## What's taken from the real archive each month

- **One real calendar month** at a time, cycling through the archive in order (December 2006 → November 2010, then wrapping back to December 2006) — see `current-month.json` for what's active right now.
- **Columns**: only `Date`, `Time`, and `Global_active_power` — the original file's `Global_reactive_power`, `Voltage`, `Global_intensity`, and three sub-metering columns are dropped; GridPulse never uses them.
- **Missing-value rows**: any row with `?` in the original archive for that month is dropped rather than filled in, so every value in this file is a real, actually-recorded measurement. December 2006 and November 2010 are naturally partial months in the real archive (data starts 16 December 2006) — whatever real rows exist for a given month is exactly what gets checked in, gaps included, never padded or interpolated.

## Why a trimmed excerpt instead of the full file

The full archive is ~127MB, which exceeds GitHub's 100MB single-file limit. Rather than add Git LFS or a download-at-startup dependency, one real month at a time is trimmed down and checked in directly.

## How it stays current

`.github/workflows/refresh-power-dataset.yml` runs monthly, downloads the real archive fresh, advances `current-month.json` to the next real month in sequence, and commits the refreshed `power-data-current-month.json`. See `scripts/refresh-power-dataset.py` for the actual extraction logic. The running application never downloads anything itself — this workflow only ever edits files already in the repo.

## How GridPulse uses it

`HouseholdPowerDataset` loads this file once at startup. Each of the 28 simulated meters gets its own cursor into the loaded sequence (a different starting offset per meter, advancing one row per reading, wrapping around at the end) — so every published reading's kWh value is a real historical measurement, just replayed rather than randomly generated. Timestamps are **not** replayed from the original data — each reading is stamped with the current time at publish, so downstream hourly rollup/billing logic (which assumes readings arrive close to real time) is unaffected. See `todo.md` for the full reasoning.
