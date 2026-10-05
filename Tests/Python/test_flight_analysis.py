import importlib.util
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location("analysis", Path(__file__).resolve().parents[2] / "Tools/analyze_flight.py")
analysis = importlib.util.module_from_spec(spec)
spec.loader.exec_module(analysis)


class FlightAnalysisTests(unittest.TestCase):
    def read(self, text):
        with tempfile.TemporaryDirectory() as folder:
            p = Path(folder) / "flight.csv"
            p.write_text(text, encoding="utf-8")
            return analysis.read_flight(p)

    header = "segment,time_s,dt_s,mass_kg,gravity_mps2,position_y_m,velocity_y_mps,rotor_0_FL_thrust_n,bus_power_w,bus_energy_j_end\n"

    def test_resets_are_separate_and_energy_accounts_for_first_interval(self):
        groups = self.read(self.header + "0,0,.01,1,9.81,2,0,9.81,100,1\n0,.01,.01,1,9.81,2,0,9.81,100,2\n1,0,.01,1,9.81,2,0,9.81,100,1\n")
        self.assertEqual(len(groups), 2)
        result = analysis.summarize(groups[0])
        self.assertAlmostEqual(result["bus_energy_balance_error_j"], 0)
        self.assertAlmostEqual(result["mean_sum_rotor_thrust_n"], 9.81)

    def test_incomplete_row_is_rejected(self):
        with self.assertRaises(ValueError):
            self.read(self.header + "0,0,.01\n")

    def test_rotor_flow_columns_do_not_double_count_corrected_thrust(self):
        header = self.header.rstrip("\n") + ",rotor_0_FL_thrust_correction_n,rotor_0_FL_flap_x_nm,rotor_0_FL_flow_clamped\n"
        groups = self.read(header + "0,0,.01,1,9.81,2,0,9.9,100,1,.09,.01,1\n")
        self.assertAlmostEqual(analysis.summarize(groups[0])["mean_sum_rotor_thrust_n"], 9.9)
        self.assertAlmostEqual(groups[0][0]["rotor_0_FL_thrust_correction_n"], .09)

    def test_nonfinite_data_is_rejected(self):
        with self.assertRaises(ValueError):
            self.read(self.header + "0,0,.01,1,9.81,nan,0,9.81,100,1\n")

    def test_weather_and_thermal_csv_keeps_bus_energy_separate(self):
        header = self.header.rstrip("\n") + ",precipitation,precipitation_mmph,thermal_derated_end,rotor_0_FL_motor_temp_k_end,battery_temp_k_end,thermal_generated_j_end,thermal_rejected_j_end,thermal_stored_j_end\n"
        groups = self.read(header + "0,0,.01,1,9.81,2,0,9.81,100,1,Rain,10,1,310,300,.2,.1,.1\n")
        result = analysis.summarize(groups[0])
        self.assertEqual(groups[0][0]["precipitation"], "Rain")
        self.assertAlmostEqual(result["bus_energy_balance_error_j"], 0)
        self.assertAlmostEqual(result["max_thermal_energy_balance_error_j"], 0)
        self.assertEqual(result["max_motor_temperature_k"], 310)
        self.assertEqual(result["thermal_derated_samples"], 1)

    def test_unknown_precipitation_is_rejected(self):
        header = self.header.rstrip("\n") + ",precipitation\n"
        with self.assertRaises(ValueError):
            self.read(header + "0,0,.01,1,9.81,2,0,9.81,100,1,Typo\n")

    def test_descent_and_wind_diagnostics_count_rows_without_inventing_thrust(self):
        header = self.header.rstrip("\n") + ",wind_sampling_nyquist_ratio,wind_under_resolved,rotor_0_FL_envelope_exceeded,rotor_0_FL_flow_regime_code,rotor_1_FR_envelope_exceeded\n"
        groups = self.read(header + "0,0,.01,1,9.81,2,-3,9.81,100,1,.7,1,1,3,1\n0,.01,.01,1,9.81,2,-3,9.81,100,2,,0,,0,0\n")
        result = analysis.summarize(groups[0])
        self.assertEqual(result["rotor_envelope_exceeded_samples"], 1)
        self.assertEqual(result["positive_thrust_descent_samples"], 1)
        self.assertEqual(result["wind_under_resolved_samples"], 1)
        self.assertEqual(result["max_wind_sampling_nyquist_ratio"], .7)
        self.assertEqual(result["mean_sum_rotor_thrust_n"], 9.81)

    def test_time_reversal_requires_new_segment(self):
        with self.assertRaises(ValueError):
            self.read(self.header + "0,.1,.01,1,9.81,2,0,9.81,100,1\n0,0,.01,1,9.81,2,0,9.81,100,2\n")


if __name__ == "__main__":
    unittest.main()
