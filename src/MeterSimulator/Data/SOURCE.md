# Data source: real household electricity consumption

`power-data-february-2007.json` is a real, unmodified excerpt of a published dataset — not synthetic or fabricated data.

## Source

**Individual Household Electric Power Consumption**
Georges Hebrail and Alice Berard, 2012.
UCI Machine Learning Repository. https://doi.org/10.24432/C58K54

Real minute-level electricity measurements from one household in Sceaux, France (7km from Paris), recorded December 2006 – November 2010. Licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).

## Shape

A JSON array of `[date, time, globalActivePowerKw]` tuples, e.g. `["1/2/2007", "00:00:00", 0.326]` — `globalActivePowerKw` is the household's real average active power draw for that minute, in kilowatts.

## What was taken from it

- **Date range**: 1 February 2007 – 28 February 2007 (one complete real month), out of the full ~4-year archive.
- **Columns**: only `Date`, `Time`, and `Global_active_power` — the original file's `Global_reactive_power`, `Voltage`, `Global_intensity`, and three sub-metering columns are dropped; GridPulse never uses them.
- **Rows dropped**: 2 rows in this range had missing values (`?`) in the original archive and were removed rather than filled in, so every value in this file is a real, actually-recorded measurement.
- **Result**: 40,318 real rows, ~1.3MB.

## Why a trimmed excerpt instead of the full file

The full archive is ~127MB, which exceeds GitHub's 100MB single-file limit. Rather than add Git LFS or a download-at-startup dependency, a real, complete, representative month was trimmed down and checked in directly.

## How GridPulse uses it

`HouseholdPowerDataset` loads this file once at startup. Each of the 28 simulated meters gets its own cursor into the 40,318-row sequence (a different starting offset per meter, advancing one row per reading, wrapping around at the end) — so every published reading's kWh value is a real historical measurement, just replayed rather than randomly generated. Timestamps are **not** replayed from the original data — each reading is stamped with the current time at publish, so downstream hourly rollup/billing logic (which assumes readings arrive close to real time) is unaffected. See `todo.md` for the full reasoning.
