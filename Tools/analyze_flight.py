"""DroneLab CSV summary; Python 3 standard library, no profile changes or auto-fitting."""
import argparse
import csv
import json
import math
from pathlib import Path
from statistics import mean


def read_flight(path):
    with Path(path).open(encoding="utf-8-sig", newline="") as stream:
        reader = csv.DictReader(stream)
        required = {"segment", "time_s", "dt_s", "mass_kg", "gravity_mps2", "position_y_m", "velocity_y_mps"}
        if not required.issubset(reader.fieldnames or []):
            raise ValueError("Not a DroneLab flight CSV: required columns missing")
        groups = {}
        for line, row in enumerate(reader, 2):
            if None in row or any(value is None for value in row.values()):
                raise ValueError(f"Incomplete CSV row {line}")
            parsed = {}
            for key, value in row.items():
                if key in ("control_mode", "precipitation"):
                    if key == "precipitation" and value not in ("None", "Rain", "Snow", "Hail"):
                        raise ValueError(f"Invalid precipitation at row {line}")
                    parsed[key] = value
                elif value == "":
                    parsed[key] = None
                else:
                    parsed[key] = float(value)
                    if not math.isfinite(parsed[key]):
                        raise ValueError(f"Non-finite value at row {line}: {key}")
            if any(parsed[k] is None for k in required) or parsed["dt_s"] <= 0:
                raise ValueError(f"Missing state or invalid dt at row {line}")
            if parsed["segment"] != int(parsed["segment"]) or parsed["segment"] < 0:
                raise ValueError(f"Invalid segment at row {line}")
            group = groups.setdefault(int(parsed["segment"]), [])
            if group and parsed["time_s"] <= group[-1]["time_s"]:
                raise ValueError(f"Non-increasing time within segment at row {line}")
            group.append(parsed)
    if not groups:
        raise ValueError("CSV has no samples")
    return groups


def summarize(rows):
    thrust_columns = [key for key in rows[0] if key.startswith("rotor_") and key.endswith("_thrust_n")]
    thrust = [sum(row[key] for key in thrust_columns) for row in rows]
    result = {"samples": len(rows), "start_s": rows[0]["time_s"],
              "end_s": rows[-1]["time_s"] + rows[-1]["dt_s"],
              "dt_min_s": min(r["dt_s"] for r in rows), "dt_max_s": max(r["dt_s"] for r in rows),
              "height_span_m": max(r["position_y_m"] for r in rows) - min(r["position_y_m"] for r in rows),
              "vertical_speed_rms_mps": math.sqrt(mean(r["velocity_y_mps"] ** 2 for r in rows)),
              "mean_sum_rotor_thrust_n": mean(thrust),
              "mean_weight_n": mean(r["mass_kg"] * r["gravity_mps2"] for r in rows),
              "drive_fault_samples": sum(r.get("drive_fault") == 1 for r in rows),
              "power_limited_samples": sum(r.get("power_limited") == 1 for r in rows),
              "thermal_derated_samples": sum(r.get("thermal_derated_end") == 1 for r in rows)}
    result["wind_under_resolved_samples"] = sum(r.get("wind_under_resolved") == 1 for r in rows)
    result["rotor_envelope_exceeded_samples"] = sum(any(r[key] == 1 for key in r if key.startswith("rotor_") and key.endswith("_envelope_exceeded")) for r in rows)
    result["positive_thrust_descent_samples"] = sum(any(r[key] == 3 for key in r if key.startswith("rotor_") and key.endswith("_flow_regime_code")) for r in rows)
    ratios = [r["wind_sampling_nyquist_ratio"] for r in rows if r.get("wind_sampling_nyquist_ratio") is not None]
    if ratios:
        result["max_wind_sampling_nyquist_ratio"] = max(ratios)
    for suffix, label in (("_motor_temp_k_end", "motor"), ("_esc_temp_k_end", "esc")):
        values = [r[key] for r in rows for key in r if key.startswith("rotor_") and key.endswith(suffix) and r[key] is not None]
        if values:
            result[f"max_{label}_temperature_k"] = max(values)
    if all(r.get("battery_temp_k_end") is not None for r in rows):
        result["max_battery_temperature_k"] = max(r["battery_temp_k_end"] for r in rows)
    thermal_keys = ("thermal_generated_j_end", "thermal_rejected_j_end", "thermal_stored_j_end")
    if all(r.get(key) is not None for r in rows for key in thermal_keys):
        result["max_thermal_energy_balance_error_j"] = max(abs(r[thermal_keys[0]] - r[thermal_keys[1]] - r[thermal_keys[2]]) for r in rows)
    for key in ("soc_end", "consumed_ah_end", "bus_energy_j_end"):
        if all(r.get(key) is not None for r in rows):
            result[key + "_first"] = rows[0][key]
            result[key + "_last"] = rows[-1][key]
    if all(r.get("bus_power_w") is not None and r.get("bus_energy_j_end") is not None for r in rows):
        integrated = sum(r["bus_power_w"] * r["dt_s"] for r in rows)
        baseline = rows[0]["bus_energy_j_end"] - rows[0]["bus_power_w"] * rows[0]["dt_s"]
        result["bus_energy_balance_error_j"] = rows[-1]["bus_energy_j_end"] - baseline - integrated
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("csv", type=Path)
    parser.add_argument("--from-s", type=float, default=-math.inf, help="Select steady hover interval")
    parser.add_argument("--to-s", type=float, default=math.inf)
    args = parser.parse_args()
    if args.from_s > args.to_s:
        parser.error("--from-s must not exceed --to-s")
    try:
        groups = read_flight(args.csv)
        output = {str(segment): summarize(selected) for segment, rows in groups.items()
                  if (selected := [r for r in rows if args.from_s <= r["time_s"] <= args.to_s])}
        if not output:
            raise ValueError("No samples in selected time interval")
        print(json.dumps(output, indent=2, ensure_ascii=False, allow_nan=False))
    except (OSError, ValueError, TypeError) as error:
        parser.exit(1, f"Flight analysis failed: {error}\n")


if __name__ == "__main__":
    main()
