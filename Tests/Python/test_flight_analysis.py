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

    def test_time_reversal_requires_new_segment(self):
        with self.assertRaises(ValueError):
            self.read(self.header + "0,.1,.01,1,9.81,2,0,9.81,100,1\n0,0,.01,1,9.81,2,0,9.81,100,2\n")


if __name__ == "__main__":
    unittest.main()
