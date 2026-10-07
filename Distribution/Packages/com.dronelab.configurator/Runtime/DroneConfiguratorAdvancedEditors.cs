using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DroneLab.Configurator
{
    /// <summary>
    /// Binds UI objects that are already authored in DroneConfigurator 1.unity.
    /// This component never creates UI GameObjects at runtime.
    /// Add/Delete only activates or deactivates pre-authored row slots.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DroneConfiguratorAdvancedEditors : MonoBehaviour
    {
        private DroneConfiguratorCanvasController owner;
        private DroneConfiguratorDraft Draft => owner != null ? owner.Draft : null;
        private int RotorIndex => owner != null ? owner.SelectedRotorIndex : 0;
        private ConfiguratorRotor Rotor =>
            Draft != null && Draft.rotors != null && Draft.rotors.Count > 0
                ? Draft.rotors[Mathf.Clamp(RotorIndex, 0, Draft.rotors.Count - 1)]
                : null;

        private readonly CultureInfo invariant = CultureInfo.InvariantCulture;

        private GameObject pageOmega;
        private GameObject pageRpm;
        private GameObject pageMap;
        private GameObject pageBattery;
        private GameObject pageOcv;
        private Transform pageParent;

        private Button btnOmega;
        private Button btnRpm;
        private Button btnMap;
        private Button btnBatteryParameters;
        private Button btnOcv;

        private TMP_InputField omegaKThrust;
        private TMP_InputField omegaKTorque;
        private TMP_InputField omegaDensity;

        private TMP_InputField rpmDensity;
        private TMP_Dropdown rpmPolicy;
        private TMP_Text rpmHeader;
        private Button btnAddRpm;
        private Transform rpmContent;
        private readonly List<RowSlot> rpmRows = new List<RowSlot>();

        private TMP_Dropdown mapPolicy;
        private TMP_Text mapHeader;
        private Button btnAddMap;
        private Transform mapContent;
        private readonly List<RowSlot> mapRows = new List<RowSlot>();

        private Button btnAddOcv;
        private Transform ocvContent;
        private readonly List<RowSlot> ocvRows = new List<RowSlot>();

        private TMP_Dropdown batteryMode;
        private TMP_InputField batteryCellCount;
        private TMP_InputField batteryNominalVoltage;
        private TMP_InputField batteryCapacityMah;
        private TMP_InputField batteryInitialSocPercent;
        private TMP_InputField batteryInternalResistance;
        private TMP_InputField batteryMaxDischargeCurrent;

        private bool initialized;

        private sealed class RowSlot
        {
            public GameObject root;
            public TMP_InputField a;
            public TMP_InputField b;
            public TMP_InputField c;
            public TMP_InputField d;
            public Button delete;
        }

        public void Initialize(DroneConfiguratorCanvasController controller)
        {
            if (initialized || controller == null)
                return;

            owner = controller;

            pageOmega = FindObject("Page_PerformanceOmegaSquared");
            pageRpm = FindObject("Page_PerformanceRpmTable");
            pageMap = FindObject("Page_PerformanceMap");
            pageBattery = FindObject("Page_Battery");
            pageOcv = FindObject("Page_BatteryOcvCurve");
            pageParent = pageOmega != null ? pageOmega.transform.parent :
                         pageRpm != null ? pageRpm.transform.parent :
                         pageBattery != null ? pageBattery.transform.parent : null;

            btnOmega = FindButton("Btn_PerformanceOmegaSquared");
            btnRpm = FindButton("Btn_PerformanceRpmTable");
            btnMap = FindButton("Btn_PerformanceMap");
            btnBatteryParameters = FindButton("Btn_BatteryParameters");
            btnOcv = FindButton("Btn_BatteryOcvCurve");

            BindOmegaPage();
            BindRpmPage();
            BindMapPage();
            BindBatteryPage();
            BindOcvPage();
            BindNavigation();

            HideDynamicPages();
            initialized = true;
        }

        public void HideDynamicPages()
        {
            SetActive(pageOmega, false);
            SetActive(pageRpm, false);
            SetActive(pageMap, false);
            SetActive(pageOcv, false);
        }

        public void RefreshSelectedRotor()
        {
            if (!initialized)
                return;

            if (pageOmega != null && pageOmega.activeSelf) RefreshOmega();
            if (pageRpm != null && pageRpm.activeSelf) RefreshRpmRows();
            if (pageMap != null && pageMap.activeSelf) RefreshMapRows();
        }

        public void OpenDefaultPerformancePage()
        {
            if (!initialized || Rotor == null || pageOmega == null)
                return;

            Rotor.performance.model = "OmegaSquared";
            ShowOnly(pageOmega);
            RefreshOmega();
            owner.SetConfiguratorStatus($"Ротор {Rotor.rotorId}: Ω² коэффициенты.");
        }

        private void BindNavigation()
        {
            Bind(btnOmega, () =>
            {
                if (Rotor == null) return;
                Rotor.performance.model = "OmegaSquared";
                ShowOnly(pageOmega);
                RefreshOmega();
                owner.SetConfiguratorStatus($"Ротор {Rotor.rotorId}: Ω² коэффициенты.");
            });

            Bind(btnRpm, () =>
            {
                if (Rotor == null) return;
                Rotor.performance.model = "RpmTable";
                EnsureRpmSeed();
                ShowOnly(pageRpm);
                RefreshRpmRows();
                owner.SetConfiguratorStatus($"Ротор {Rotor.rotorId}: RPM-таблица.");
            });

            Bind(btnMap, () =>
            {
                if (Rotor == null) return;
                Rotor.performance.model = "PerformanceMap";
                ShowOnly(pageMap);
                RefreshMapRows();
                owner.SetConfiguratorStatus($"Ротор {Rotor.rotorId}: карта характеристик RPM × J.");
            });

            Bind(btnBatteryParameters, () =>
            {
                ShowOnly(pageBattery);
                RefreshBattery();
                owner.SetConfiguratorStatus("Параметры батареи.");
            });

            Bind(btnOcv, () =>
            {
                EnsureOcvSeed();
                ShowOnly(pageOcv);
                RefreshOcvRows();
                owner.SetConfiguratorStatus("Кривая OCV: SOC → напряжение.");
            });
        }

        private void BindOmegaPage()
        {
            if (pageOmega == null) return;
            omegaKThrust = FindIn<TMP_InputField>(pageOmega.transform, "Input_KThrust");
            omegaKTorque = FindIn<TMP_InputField>(pageOmega.transform, "Input_KTorque");
            omegaDensity = FindIn<TMP_InputField>(pageOmega.transform, "Input_ReferenceDensity");
        }

        private void RefreshOmega()
        {
            if (Rotor == null) return;
            WriteOptional(omegaKThrust, Rotor.performance.kThrustNPerRadPerSecSquared);
            WriteOptional(omegaKTorque, Rotor.performance.kTorqueNmPerRadPerSecSquared);
            WriteOptional(omegaDensity, Rotor.performance.referenceAirDensityKgM3);
        }

        private void BindRpmPage()
        {
            if (pageRpm == null) return;

            rpmHeader = FindIn<TMP_Text>(pageRpm.transform, "Header");
            rpmDensity = FindIn<TMP_InputField>(pageRpm.transform, "Input_RpmReferenceDensity");
            rpmPolicy = FindIn<TMP_Dropdown>(pageRpm.transform, "Dropdown_RpmOutOfRangePolicy");
            btnAddRpm = FindIn<Button>(pageRpm.transform, "Btn_AddRpmRow");
            rpmContent = FindTransform("RpmTableContent");

            SetupDropdown(rpmPolicy, new[] { "Reject", "Clamp" });

            BindEnd(rpmDensity, value =>
            {
                if (Rotor != null) SetOptional(value, x => Rotor.performance.referenceAirDensityKgM3 = x);
            });

            if (rpmPolicy != null)
            {
                rpmPolicy.onValueChanged.RemoveAllListeners();
                rpmPolicy.onValueChanged.AddListener(i =>
                {
                    if (Rotor != null) Rotor.performance.outOfRangePolicy = rpmPolicy.options[i].text;
                });
            }

            CacheRows(rpmContent, "RpmTableRow_", rpmRows, row =>
            {
                row.a = FindIn<TMP_InputField>(row.root.transform, "Input_Rpm");
                row.b = FindIn<TMP_InputField>(row.root.transform, "Input_ThrustN");
                row.c = FindIn<TMP_InputField>(row.root.transform, "Input_TorqueNm");
                row.d = FindIn<TMP_InputField>(row.root.transform, "Input_CurrentA");
                row.delete = FindIn<Button>(row.root.transform, "Btn_DeleteRow");
            });

            Bind(btnAddRpm, AddRpmRow);
        }

        private void EnsureRpmSeed()
        {
            if (Rotor == null) return;
            if (Rotor.performance.rpmTable == null)
                Rotor.performance.rpmTable = new List<ConfiguratorRpmPoint>();

            if (Rotor.performance.rpmTable.Count == 0)
            {
                Rotor.performance.rpmTable.Add(new ConfiguratorRpmPoint
                {
                    rpm = OptionalNumber.From(0),
                    thrustN = OptionalNumber.From(0),
                    torqueNm = OptionalNumber.From(0)
                });
                Rotor.performance.rpmTable.Add(new ConfiguratorRpmPoint());
            }
        }

        private void AddRpmRow()
        {
            if (Rotor == null) return;
            if (Rotor.performance.rpmTable == null)
                Rotor.performance.rpmTable = new List<ConfiguratorRpmPoint>();

            if (Rotor.performance.rpmTable.Count >= rpmRows.Count)
            {
                owner.SetConfiguratorStatus($"На сцене подготовлено {rpmRows.Count} строк RPM. При необходимости продублируйте RpmTableRow_* вручную.");
                return;
            }

            Rotor.performance.rpmTable.Add(new ConfiguratorRpmPoint());
            RefreshRpmRows();
        }

        private void RefreshRpmRows()
        {
            if (Rotor == null) return;
            EnsureRpmSeed();

            if (rpmHeader != null) rpmHeader.text = $"РОТОР {Rotor.rotorId} — RPM-ТАБЛИЦА";
            WriteOptional(rpmDensity, Rotor.performance.referenceAirDensityKgM3);
            SetDropdown(rpmPolicy, Rotor.performance.outOfRangePolicy);

            for (int i = 0; i < rpmRows.Count; i++)
            {
                RowSlot slot = rpmRows[i];
                bool used = i < Rotor.performance.rpmTable.Count;
                slot.root.SetActive(used);
                ClearListeners(slot);
                if (!used) continue;

                int index = i;
                ConfiguratorRpmPoint point = Rotor.performance.rpmTable[i];
                WriteOptional(slot.a, point.rpm);
                WriteOptional(slot.b, point.thrustN);
                WriteOptional(slot.c, point.torqueNm);
                WriteOptional(slot.d, point.currentA);

                BindEnd(slot.a, v => SetOptional(v, x => point.rpm = x));
                BindEnd(slot.b, v => SetOptional(v, x => point.thrustN = x));
                BindEnd(slot.c, v => SetOptional(v, x => point.torqueNm = x));
                BindEnd(slot.d, v => SetOptional(v, x => point.currentA = x));
                Bind(slot.delete, () =>
                {
                    if (Rotor.performance.rpmTable.Count <= 2)
                    {
                        owner.SetConfiguratorStatus("RPM-таблица должна содержать минимум две строки.");
                        return;
                    }
                    Rotor.performance.rpmTable.RemoveAt(index);
                    RefreshRpmRows();
                });
            }
        }

        private void BindMapPage()
        {
            if (pageMap == null) return;

            mapHeader = FindIn<TMP_Text>(pageMap.transform, "Header");
            mapPolicy = FindIn<TMP_Dropdown>(pageMap.transform, "Dropdown_MapOutOfRangePolicy");
            btnAddMap = FindIn<Button>(pageMap.transform, "Btn_AddMapPoint");
            mapContent = FindTransform("PerformanceMapContent");

            SetupDropdown(mapPolicy, new[] { "Reject", "Clamp" });

            if (mapPolicy != null)
            {
                mapPolicy.onValueChanged.RemoveAllListeners();
                mapPolicy.onValueChanged.AddListener(i =>
                {
                    if (Rotor != null) Rotor.performance.outOfRangePolicy = mapPolicy.options[i].text;
                });
            }

            CacheRows(mapContent, "PerformanceMapRow_", mapRows, row =>
            {
                row.a = FindIn<TMP_InputField>(row.root.transform, "Input_MapRpm");
                row.b = FindIn<TMP_InputField>(row.root.transform, "Input_AdvanceRatio");
                row.c = FindIn<TMP_InputField>(row.root.transform, "Input_Ct");
                row.d = FindIn<TMP_InputField>(row.root.transform, "Input_Cq");
                row.delete = FindIn<Button>(row.root.transform, "Btn_DeleteMapPoint");
            });

            Bind(btnAddMap, AddMapRow);
        }

        private void AddMapRow()
        {
            if (Rotor == null) return;
            if (Rotor.performance.performanceMap == null)
                Rotor.performance.performanceMap = new List<ConfiguratorPerformanceMapPoint>();

            if (Rotor.performance.performanceMap.Count >= mapRows.Count)
            {
                owner.SetConfiguratorStatus($"На сцене подготовлено {mapRows.Count} строк карты. При необходимости продублируйте PerformanceMapRow_* вручную.");
                return;
            }

            Rotor.performance.performanceMap.Add(new ConfiguratorPerformanceMapPoint());
            RefreshMapRows();
        }

        private void RefreshMapRows()
        {
            if (Rotor == null) return;
            if (Rotor.performance.performanceMap == null)
                Rotor.performance.performanceMap = new List<ConfiguratorPerformanceMapPoint>();

            if (mapHeader != null) mapHeader.text = $"РОТОР {Rotor.rotorId} — КАРТА ХАРАКТЕРИСТИК";
            SetDropdown(mapPolicy, Rotor.performance.outOfRangePolicy);

            for (int i = 0; i < mapRows.Count; i++)
            {
                RowSlot slot = mapRows[i];
                bool used = i < Rotor.performance.performanceMap.Count;
                slot.root.SetActive(used);
                ClearListeners(slot);
                if (!used) continue;

                int index = i;
                ConfiguratorPerformanceMapPoint point = Rotor.performance.performanceMap[i];
                WriteOptional(slot.a, point.rpm);
                WriteOptional(slot.b, point.advanceRatio);
                WriteOptional(slot.c, point.ct);
                WriteOptional(slot.d, point.cq);

                BindEnd(slot.a, v => SetOptional(v, x => point.rpm = x));
                BindEnd(slot.b, v => SetOptional(v, x => point.advanceRatio = x));
                BindEnd(slot.c, v => SetOptional(v, x => point.ct = x));
                BindEnd(slot.d, v => SetOptional(v, x => point.cq = x));
                Bind(slot.delete, () =>
                {
                    Rotor.performance.performanceMap.RemoveAt(index);
                    RefreshMapRows();
                });
            }
        }

        private void BindBatteryPage()
        {
            if (pageBattery == null) return;

            batteryMode = FindIn<TMP_Dropdown>(pageBattery.transform, "Dropdown_BatteryMode");
            batteryCellCount = FindIn<TMP_InputField>(pageBattery.transform, "Input_BatteryCellCount");
            batteryNominalVoltage = FindIn<TMP_InputField>(pageBattery.transform, "Input_BatteryNominalVoltageV");
            batteryCapacityMah = FindIn<TMP_InputField>(pageBattery.transform, "Input_BatteryCapacityMah");
            batteryInitialSocPercent = FindIn<TMP_InputField>(pageBattery.transform, "Input_BatteryInitialSocPercent");
            batteryInternalResistance = FindIn<TMP_InputField>(pageBattery.transform, "Input_BatteryInternalResistanceOhm");
            batteryMaxDischargeCurrent = FindIn<TMP_InputField>(pageBattery.transform, "Input_BatteryMaxDischargeCurrentA");

            // Current README/MVP supports a complete battery profile only in None mode.
            // Simple/Electrical additionally require per-motor electrical inputs that are
            // intentionally not exposed by the current hand-authored Canvas yet.
            SetupDropdown(batteryMode, new[] { "None" });

            if (batteryMode != null)
            {
                batteryMode.onValueChanged.RemoveAllListeners();
                batteryMode.onValueChanged.AddListener(i =>
                {
                    Draft.battery.mode = batteryMode.options[i].text;
                    RefreshBatteryInteractable();
                    if (Draft.battery.mode != "None")
                        owner.SetConfiguratorStatus("Питание включено: для готового профиля также нужны электрические параметры каждого двигателя.");
                });
            }

            BindEnd(batteryCellCount, v => SetOptional(v, x => Draft.battery.cellCount = x));
            BindEnd(batteryNominalVoltage, v => SetOptional(v, x => Draft.battery.nominalVoltageV = x));
            BindEnd(batteryCapacityMah, v => SetOptionalScaled(v, 0.001, x => Draft.battery.capacityAh = x));
            BindEnd(batteryInitialSocPercent, v => SetOptionalScaled(v, 0.01, x => Draft.battery.initialSoc = x));
            BindEnd(batteryInternalResistance, v => SetOptional(v, x => Draft.battery.internalResistanceOhm = x));
            BindEnd(batteryMaxDischargeCurrent, v => SetOptional(v, x => Draft.battery.maxDischargeCurrentA = x));
        }

        private void RefreshBattery()
        {
            if (Draft == null) return;

            SetDropdown(batteryMode, Draft.battery.mode);
            WriteOptional(batteryCellCount, Draft.battery.cellCount);
            WriteOptional(batteryNominalVoltage, Draft.battery.nominalVoltageV);
            WriteOptionalScaled(batteryCapacityMah, Draft.battery.capacityAh, 1000.0);
            WriteOptionalScaled(batteryInitialSocPercent, Draft.battery.initialSoc, 100.0);
            WriteOptional(batteryInternalResistance, Draft.battery.internalResistanceOhm);
            WriteOptional(batteryMaxDischargeCurrent, Draft.battery.maxDischargeCurrentA);
            RefreshBatteryInteractable();
        }

        private void RefreshBatteryInteractable()
        {
            bool enabled = Draft != null && Draft.battery.mode != "None";
            foreach (TMP_InputField field in new[]
                     {
                         batteryCellCount, batteryNominalVoltage, batteryCapacityMah,
                         batteryInitialSocPercent, batteryInternalResistance, batteryMaxDischargeCurrent
                     })
                if (field != null) field.interactable = enabled;
        }

        private void BindOcvPage()
        {
            if (pageOcv == null) return;

            btnAddOcv = FindIn<Button>(pageOcv.transform, "Btn_AddOcvPoint");
            ocvContent = FindTransform("OcvCurveContent");

            CacheRows(ocvContent, "OcvPointRow_", ocvRows, row =>
            {
                row.a = FindIn<TMP_InputField>(row.root.transform, "Input_SocPercent");
                row.b = FindIn<TMP_InputField>(row.root.transform, "Input_VoltageV");
                row.delete = FindIn<Button>(row.root.transform, "Btn_DeleteOcvPoint");
            });

            Bind(btnAddOcv, AddOcvRow);
        }

        private void EnsureOcvSeed()
        {
            if (Draft.battery.ocvCurve == null)
                Draft.battery.ocvCurve = new List<ConfiguratorOcvPoint>();

            if (Draft.battery.ocvCurve.Count == 0)
            {
                Draft.battery.ocvCurve.Add(new ConfiguratorOcvPoint { soc = OptionalNumber.From(0) });
                Draft.battery.ocvCurve.Add(new ConfiguratorOcvPoint { soc = OptionalNumber.From(1) });
            }
        }

        private void AddOcvRow()
        {
            EnsureOcvSeed();

            if (Draft.battery.ocvCurve.Count >= ocvRows.Count)
            {
                owner.SetConfiguratorStatus($"На сцене подготовлено {ocvRows.Count} OCV-точек. При необходимости продублируйте OcvPointRow_* вручную.");
                return;
            }

            Draft.battery.ocvCurve.Insert(
                Mathf.Max(1, Draft.battery.ocvCurve.Count - 1),
                new ConfiguratorOcvPoint());

            RefreshOcvRows();
        }

        private void RefreshOcvRows()
        {
            EnsureOcvSeed();

            for (int i = 0; i < ocvRows.Count; i++)
            {
                RowSlot slot = ocvRows[i];
                bool used = i < Draft.battery.ocvCurve.Count;
                slot.root.SetActive(used);
                ClearListeners(slot);
                if (!used) continue;

                int index = i;
                ConfiguratorOcvPoint point = Draft.battery.ocvCurve[i];
                WriteOptionalScaled(slot.a, point.soc, 100.0);
                WriteOptional(slot.b, point.voltageV);

                BindEnd(slot.a, v => SetOptionalScaled(v, 0.01, x => point.soc = x));
                BindEnd(slot.b, v => SetOptional(v, x => point.voltageV = x));
                Bind(slot.delete, () =>
                {
                    if (Draft.battery.ocvCurve.Count <= 2)
                    {
                        owner.SetConfiguratorStatus("OCV-кривая должна содержать минимум две точки.");
                        return;
                    }
                    Draft.battery.ocvCurve.RemoveAt(index);
                    RefreshOcvRows();
                });
            }
        }

        private void ShowOnly(GameObject page)
        {
            if (pageParent == null || page == null) return;

            foreach (Transform child in pageParent)
            {
                if (child.name.StartsWith("Page_", StringComparison.Ordinal) || child.name == "PageRotor")
                    child.gameObject.SetActive(child.gameObject == page);
            }
            page.SetActive(true);
        }

        private static void CacheRows(
            Transform content,
            string prefix,
            List<RowSlot> target,
            Action<RowSlot> configure)
        {
            target.Clear();
            if (content == null) return;

            IEnumerable<Transform> rows = content.Cast<Transform>()
                .Where(x => x.name.StartsWith(prefix, StringComparison.Ordinal))
                .OrderBy(x => x.GetSiblingIndex());

            foreach (Transform rowTransform in rows)
            {
                var row = new RowSlot { root = rowTransform.gameObject };
                configure(row);
                target.Add(row);
            }
        }

        private static void ClearListeners(RowSlot slot)
        {
            if (slot.a != null) slot.a.onEndEdit.RemoveAllListeners();
            if (slot.b != null) slot.b.onEndEdit.RemoveAllListeners();
            if (slot.c != null) slot.c.onEndEdit.RemoveAllListeners();
            if (slot.d != null) slot.d.onEndEdit.RemoveAllListeners();
            if (slot.delete != null) slot.delete.onClick.RemoveAllListeners();
        }

        private void SetOptional(string valueText, Action<OptionalNumber> setter)
        {
            if (string.IsNullOrWhiteSpace(valueText))
            {
                setter(OptionalNumber.Missing());
                return;
            }

            if (TryNumber(valueText, out double value))
                setter(OptionalNumber.From(value));
        }

        private void SetOptionalScaled(string valueText, double factor, Action<OptionalNumber> setter)
        {
            if (string.IsNullOrWhiteSpace(valueText))
            {
                setter(OptionalNumber.Missing());
                return;
            }

            if (TryNumber(valueText, out double value))
                setter(OptionalNumber.From(value * factor));
        }

        private bool TryNumber(string valueText, out double value)
        {
            valueText = (valueText ?? string.Empty).Trim().Replace(',', '.');
            return double.TryParse(valueText, NumberStyles.Float, invariant, out value)
                   && !double.IsNaN(value)
                   && !double.IsInfinity(value);
        }

        private void WriteOptional(TMP_InputField field, OptionalNumber value)
        {
            if (field == null) return;
            field.SetTextWithoutNotify(value != null && value.hasValue
                ? value.value.ToString("0.##########", invariant)
                : string.Empty);
        }

        private void WriteOptionalScaled(TMP_InputField field, OptionalNumber value, double factor)
        {
            if (field == null) return;
            field.SetTextWithoutNotify(value != null && value.hasValue
                ? (value.value * factor).ToString("0.##########", invariant)
                : string.Empty);
        }

        private static void SetupDropdown(TMP_Dropdown dropdown, IEnumerable<string> options)
        {
            if (dropdown == null) return;
            dropdown.ClearOptions();
            dropdown.AddOptions(options.ToList());
            dropdown.SetValueWithoutNotify(0);
            dropdown.RefreshShownValue();
        }

        private static void SetDropdown(TMP_Dropdown dropdown, string value)
        {
            if (dropdown == null) return;

            int index = dropdown.options.FindIndex(x =>
                string.Equals(x.text.Trim(), value, StringComparison.OrdinalIgnoreCase));

            dropdown.SetValueWithoutNotify(index >= 0 ? index : 0);
            dropdown.RefreshShownValue();
        }

        private static void Bind(Button button, Action callback)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback());
        }

        private static void BindEnd(TMP_InputField field, Action<string> callback)
        {
            if (field == null) return;
            field.onEndEdit.RemoveAllListeners();
            field.onEndEdit.AddListener(value => callback(value));
        }

        private Button FindButton(string name) =>
            FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(x => x.name == name);

        private GameObject FindObject(string name)
        {
            Transform found = FindObjectsByType<Transform>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(x => x.name == name);
            return found != null ? found.gameObject : null;
        }

        private Transform FindTransform(string name) =>
            FindObjectsByType<Transform>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault(x => x.name == name);

        private static T FindIn<T>(Transform root, string name) where T : Component
        {
            if (root == null) return null;
            return root.GetComponentsInChildren<T>(true).FirstOrDefault(x => x.name == name);
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
