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
    /// Runtime editors for the variable-size parts of the configurator:
    /// RPM table, performance map and battery OCV curve.
    /// Stable navigation buttons/pages remain scene-authored; variable rows are generated here.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DroneConfiguratorAdvancedEditors : MonoBehaviour
    {
        private DroneConfiguratorCanvasController owner;
        private DroneConfiguratorDraft Draft => owner != null ? owner.Draft : null;
        private int RotorIndex => owner != null ? owner.SelectedRotorIndex : 0;
        private ConfiguratorRotor Rotor =>
            Draft != null && Draft.rotors.Count > 0
                ? Draft.rotors[Mathf.Clamp(RotorIndex, 0, Draft.rotors.Count - 1)]
                : null;

        private readonly CultureInfo invariant = CultureInfo.InvariantCulture;

        private Transform pageParent;
        private GameObject pageOmega;
        private GameObject pageRpmTable;
        private GameObject pagePerformanceMap;
        private GameObject pageBattery;
        private GameObject pageBatteryOcv;

        private Button btnOmega;
        private Button btnRpmTable;
        private Button btnPerformanceMap;
        private Button btnBatteryParameters;
        private Button btnBatteryOcv;

        private TMP_InputField inputTemplate;
        private Button buttonTemplate;
        private TMP_Dropdown dropdownTemplate;
        private TMP_Text textTemplate;

        private TMP_Text rpmTitle;
        private Transform rpmRows;
        private TMP_InputField rpmDensity;
        private TMP_Dropdown rpmPolicy;

        private TMP_Text mapTitle;
        private Transform mapRows;
        private TMP_Dropdown mapPolicy;

        private Transform ocvRows;

        private TMP_Dropdown batteryMode;
        private TMP_InputField batteryCellCount;
        private TMP_InputField batteryNominalVoltage;
        private TMP_InputField batteryCapacityMah;
        private TMP_InputField batteryInitialSocPercent;
        private TMP_InputField batteryInternalResistance;
        private TMP_InputField batteryMaxDischargeCurrent;

        private TMP_InputField omegaKThrust;
        private TMP_InputField omegaKTorque;
        private TMP_InputField omegaDensity;

        private bool initialized;

        public void Initialize(DroneConfiguratorCanvasController controller)
        {
            if (initialized || controller == null)
                return;

            owner = controller;
            FindTemplates();
            FindSceneNavigation();
            FindExistingPages();

            if (pageParent == null || inputTemplate == null || buttonTemplate == null || textTemplate == null)
            {
                owner.SetConfiguratorStatus("Не удалось инициализировать редакторы таблиц: отсутствует шаблон UI.");
                return;
            }

            RenameNavigationObjects();
            HideLegacyPerformanceEditor();
            ConfigureOmegaPage();
            ConfigureBatteryParametersPage();

            pageRpmTable = BuildRpmTablePage();
            pagePerformanceMap = BuildPerformanceMapPage();
            pageBatteryOcv = BuildOcvPage();

            BindNavigation();
            HideDynamicPages();

            initialized = true;
        }

        public void HideDynamicPages()
        {
            SetActive(pageOmega, false);
            SetActive(pageRpmTable, false);
            SetActive(pagePerformanceMap, false);
            SetActive(pageBatteryOcv, false);
        }

        public void RefreshSelectedRotor()
        {
            if (!initialized)
                return;

            if (pageOmega != null && pageOmega.activeSelf)
                RefreshOmega();
            if (pageRpmTable != null && pageRpmTable.activeSelf)
                RebuildRpmRows();
            if (pagePerformanceMap != null && pagePerformanceMap.activeSelf)
                RebuildMapRows();
        }

        private void FindTemplates()
        {
            inputTemplate = FindAll<TMP_InputField>().FirstOrDefault(x => x.name == "Input_KThrust")
                            ?? FindAll<TMP_InputField>().FirstOrDefault();
            buttonTemplate = FindAll<Button>().FirstOrDefault(x => x.name == "Btn_SaveDraft")
                             ?? FindAll<Button>().FirstOrDefault();
            dropdownTemplate = FindAll<TMP_Dropdown>().FirstOrDefault(x => x.name == "Dropdown_Fidelity")
                               ?? FindAll<TMP_Dropdown>().FirstOrDefault();
            textTemplate = FindAll<TMP_Text>().FirstOrDefault(x => x.name == "Title")
                           ?? FindAll<TMP_Text>().FirstOrDefault();
        }

        private void FindSceneNavigation()
        {
            btnOmega = FindButtonByNameOrLabel("Btn_PerformanceOmegaSquared", "Ω² Коэффициенты");
            btnRpmTable = FindButtonByNameOrLabel("Btn_PerformanceRpmTable", "RPM-таблица");
            btnPerformanceMap = FindButtonByNameOrLabel("Btn_PerformanceMap", "Карта характеристик");
            btnBatteryParameters = FindButtonByNameOrLabel("Btn_BatteryParameters", "Параметры батареи");
            btnBatteryOcv = FindButtonByNameOrLabel("Btn_BatteryOcvCurve", "Кривая OCV");
        }

        private void FindExistingPages()
        {
            pageOmega = FindObject("Page_PerformanceOmegaSquared");
            pageBattery = FindObject("Page_Battery");

            if (pageOmega != null)
                pageParent = pageOmega.transform.parent;
            else if (pageBattery != null)
                pageParent = pageBattery.transform.parent;
        }

        private void RenameNavigationObjects()
        {
            if (btnRpmTable != null) btnRpmTable.name = "Btn_PerformanceRpmTable";
            if (btnPerformanceMap != null) btnPerformanceMap.name = "Btn_PerformanceMap";
            if (btnBatteryParameters != null) btnBatteryParameters.name = "Btn_BatteryParameters";
            if (btnBatteryOcv != null) btnBatteryOcv.name = "Btn_BatteryOcvCurve";

            Transform performanceList = btnOmega != null ? btnOmega.transform.parent : null;
            if (performanceList != null)
                performanceList.name = "PerformanceList";

            Transform batteryList = btnBatteryParameters != null ? btnBatteryParameters.transform.parent : null;
            if (batteryList != null)
                batteryList.name = "BatteryList";
        }

        private void HideLegacyPerformanceEditor()
        {
            GameObject old = FindObject("PerformanceSection");
            if (old != null && (pageOmega == null || !old.transform.IsChildOf(pageOmega.transform)))
                old.SetActive(false);

            GameObject oldDropdown = FindObject("Dropdown_PerformanceModel");
            if (oldDropdown != null)
                oldDropdown.SetActive(false);
        }

        private void BindNavigation()
        {
            Bind(btnOmega, () =>
            {
                if (Rotor == null) return;
                Rotor.performance.model = "OmegaSquared";
                ShowOnly(pageOmega);
                RefreshOmega();
                owner.SetConfiguratorStatus($"Ротор {Rotor.rotorId}: модель характеристики OmegaSquared.");
            });

            Bind(btnRpmTable, () =>
            {
                if (Rotor == null) return;
                Rotor.performance.model = "RpmTable";
                EnsureRpmSeed();
                ShowOnly(pageRpmTable);
                RebuildRpmRows();
                owner.SetConfiguratorStatus($"Ротор {Rotor.rotorId}: редактирование RPM-таблицы.");
            });

            Bind(btnPerformanceMap, () =>
            {
                if (Rotor == null) return;
                Rotor.performance.model = "PerformanceMap";
                EnsureMapSeed();
                ShowOnly(pagePerformanceMap);
                RebuildMapRows();
                owner.SetConfiguratorStatus($"Ротор {Rotor.rotorId}: редактирование карты RPM × J.");
            });

            Bind(btnBatteryParameters, () =>
            {
                ShowOnly(pageBattery);
                RefreshBatteryParameters();
                owner.SetConfiguratorStatus("Параметры батареи.");
            });

            Bind(btnBatteryOcv, () =>
            {
                EnsureOcvSeed();
                ShowOnly(pageBatteryOcv);
                RebuildOcvRows();
                owner.SetConfiguratorStatus("Кривая OCV: SOC → напряжение аккумулятора.");
            });
        }

        private void ConfigureOmegaPage()
        {
            if (pageOmega == null)
                return;

            omegaKThrust = FindIn<TMP_InputField>(pageOmega.transform, "Input_KThrust");
            omegaKTorque = FindIn<TMP_InputField>(pageOmega.transform, "Input_KTorque");
            omegaDensity = FindIn<TMP_InputField>(pageOmega.transform, "Input_ReferenceDensity");

            BindEnd(omegaKThrust, value => SetOptional(value, v => Rotor.performance.kThrustNPerRadPerSecSquared = v));
            BindEnd(omegaKTorque, value => SetOptional(value, v => Rotor.performance.kTorqueNmPerRadPerSecSquared = v));
            BindEnd(omegaDensity, value => SetOptional(value, v => Rotor.performance.referenceAirDensityKgM3 = v));
        }

        private void RefreshOmega()
        {
            if (Rotor == null)
                return;
            WriteOptional(omegaKThrust, Rotor.performance.kThrustNPerRadPerSecSquared);
            WriteOptional(omegaKTorque, Rotor.performance.kTorqueNmPerRadPerSecSquared);
            WriteOptional(omegaDensity, Rotor.performance.referenceAirDensityKgM3);
        }

        private void ConfigureBatteryParametersPage()
        {
            if (pageBattery == null)
                return;

            batteryMode = FindIn<TMP_Dropdown>(pageBattery.transform, "Dropdown_BatteryMode");
            if (batteryMode != null)
            {
                batteryMode.ClearOptions();
                batteryMode.AddOptions(new List<string> { "None", "Simple", "Electrical" });
                batteryMode.onValueChanged.RemoveAllListeners();
                batteryMode.onValueChanged.AddListener(index =>
                {
                    Draft.battery.mode = batteryMode.options[index].text;
                    bool enabled = Draft.battery.mode != "None";
                    SetBatteryFieldsInteractable(enabled);
                    if (enabled)
                        owner.SetConfiguratorStatus("Батарея включена. Для готового профиля также потребуются электрические параметры каждого двигателя.");
                });
            }

            Transform layout = EnsureVerticalLayout(pageBattery);
            CreateSectionLabel(layout, "ПАРАМЕТРЫ БАТАРЕИ");

            batteryCellCount = CreateLabeledInput(layout, "Количество ячеек", "Input_BatteryCellCount");
            batteryNominalVoltage = CreateLabeledInput(layout, "Номинальное напряжение, В", "Input_BatteryNominalVoltageV");
            batteryCapacityMah = CreateLabeledInput(layout, "Ёмкость, мА·ч", "Input_BatteryCapacityMah");
            batteryInitialSocPercent = CreateLabeledInput(layout, "Начальный заряд, %", "Input_BatteryInitialSocPercent");
            batteryInternalResistance = CreateLabeledInput(layout, "Внутреннее сопротивление, Ω", "Input_BatteryInternalResistanceOhm");
            batteryMaxDischargeCurrent = CreateLabeledInput(layout, "Макс. ток разряда, А", "Input_BatteryMaxDischargeCurrentA");

            BindEnd(batteryCellCount, v => SetOptional(v, x => Draft.battery.cellCount = x));
            BindEnd(batteryNominalVoltage, v => SetOptional(v, x => Draft.battery.nominalVoltageV = x));
            BindEnd(batteryCapacityMah, v => SetOptionalScaled(v, 0.001, x => Draft.battery.capacityAh = x));
            BindEnd(batteryInitialSocPercent, v => SetOptionalScaled(v, 0.01, x => Draft.battery.initialSoc = x));
            BindEnd(batteryInternalResistance, v => SetOptional(v, x => Draft.battery.internalResistanceOhm = x));
            BindEnd(batteryMaxDischargeCurrent, v => SetOptional(v, x => Draft.battery.maxDischargeCurrentA = x));

            RefreshBatteryParameters();
        }

        private void RefreshBatteryParameters()
        {
            if (Draft == null)
                return;

            if (batteryMode != null)
            {
                int index = batteryMode.options.FindIndex(x => x.text == Draft.battery.mode);
                batteryMode.SetValueWithoutNotify(index >= 0 ? index : 0);
            }

            WriteOptional(batteryCellCount, Draft.battery.cellCount);
            WriteOptional(batteryNominalVoltage, Draft.battery.nominalVoltageV);
            WriteOptionalScaled(batteryCapacityMah, Draft.battery.capacityAh, 1000.0);
            WriteOptionalScaled(batteryInitialSocPercent, Draft.battery.initialSoc, 100.0);
            WriteOptional(batteryInternalResistance, Draft.battery.internalResistanceOhm);
            WriteOptional(batteryMaxDischargeCurrent, Draft.battery.maxDischargeCurrentA);
            SetBatteryFieldsInteractable(Draft.battery.mode != "None");
        }

        private void SetBatteryFieldsInteractable(bool value)
        {
            foreach (TMP_InputField field in new[]
                     {
                         batteryCellCount, batteryNominalVoltage, batteryCapacityMah,
                         batteryInitialSocPercent, batteryInternalResistance, batteryMaxDischargeCurrent
                     })
                if (field != null) field.interactable = value;
        }

        private GameObject BuildRpmTablePage()
        {
            GameObject page = CreatePage("Page_PerformanceRpmTable");
            Transform root = EnsureVerticalLayout(page);
            rpmTitle = CreateSectionLabel(root, "РОТОР M1 — RPM-ТАБЛИЦА");

            Transform settings = CreateHorizontalRow(root, "RpmSettings", 42f);
            CreateInlineLabel(settings, "Плотность, кг/м³", 125f);
            rpmDensity = CloneInput(settings, "Input_RpmReferenceDensity", 110f);
            CreateInlineLabel(settings, "За диапазоном", 105f);
            rpmPolicy = CloneDropdown(settings, "Dropdown_RpmOutOfRangePolicy", 115f, new[] { "Reject", "Clamp" });

            BindEnd(rpmDensity, value => SetOptional(value, x => Rotor.performance.referenceAirDensityKgM3 = x));
            if (rpmPolicy != null)
            {
                rpmPolicy.onValueChanged.RemoveAllListeners();
                rpmPolicy.onValueChanged.AddListener(i => Rotor.performance.outOfRangePolicy = rpmPolicy.options[i].text);
            }

            Transform header = CreateHorizontalRow(root, "TableHeader", 30f);
            CreateInlineLabel(header, "Обороты, об/мин", 115f);
            CreateInlineLabel(header, "Тяга, Н", 90f);
            CreateInlineLabel(header, "Момент, Н·м", 100f);
            CreateInlineLabel(header, "Ток, А", 80f);
            CreateInlineLabel(header, "", 36f);

            rpmRows = CreateScrollRows(root, "RpmTableScroll", 440f);
            Button add = CloneButton(root, "Btn_AddRpmRow", "+ Добавить строку");
            Bind(add, () =>
            {
                Rotor.performance.rpmTable.Add(new ConfiguratorRpmPoint());
                RebuildRpmRows();
            });
            return page;
        }

        private void EnsureRpmSeed()
        {
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

        private void RebuildRpmRows()
        {
            if (Rotor == null || rpmRows == null)
                return;

            EnsureRpmSeed();
            rpmTitle.text = $"РОТОР {Rotor.rotorId} — RPM-ТАБЛИЦА";
            WriteOptional(rpmDensity, Rotor.performance.referenceAirDensityKgM3);
            SetDropdown(rpmPolicy, Rotor.performance.outOfRangePolicy);
            ClearRows(rpmRows);

            for (int i = 0; i < Rotor.performance.rpmTable.Count; i++)
            {
                int index = i;
                ConfiguratorRpmPoint point = Rotor.performance.rpmTable[i];
                Transform row = CreateHorizontalRow(rpmRows, "RpmTableRow_" + i, 38f);

                TMP_InputField rpm = CloneInput(row, "Input_Rpm", 115f);
                TMP_InputField thrust = CloneInput(row, "Input_ThrustN", 90f);
                TMP_InputField torque = CloneInput(row, "Input_TorqueNm", 100f);
                TMP_InputField current = CloneInput(row, "Input_CurrentA", 80f);
                Button delete = CloneButton(row, "Btn_DeleteRow", "×", 36f);

                WriteOptional(rpm, point.rpm);
                WriteOptional(thrust, point.thrustN);
                WriteOptional(torque, point.torqueNm);
                WriteOptional(current, point.currentA);

                BindEnd(rpm, v => SetOptional(v, x => point.rpm = x));
                BindEnd(thrust, v => SetOptional(v, x => point.thrustN = x));
                BindEnd(torque, v => SetOptional(v, x => point.torqueNm = x));
                BindEnd(current, v => SetOptional(v, x => point.currentA = x));
                Bind(delete, () =>
                {
                    if (Rotor.performance.rpmTable.Count <= 2)
                    {
                        owner.SetConfiguratorStatus("RPM-таблица должна содержать минимум две строки.");
                        return;
                    }
                    Rotor.performance.rpmTable.RemoveAt(index);
                    RebuildRpmRows();
                });
            }
        }

        private GameObject BuildPerformanceMapPage()
        {
            GameObject page = CreatePage("Page_PerformanceMap");
            Transform root = EnsureVerticalLayout(page);
            mapTitle = CreateSectionLabel(root, "РОТОР M1 — КАРТА ХАРАКТЕРИСТИК");

            Transform settings = CreateHorizontalRow(root, "MapSettings", 42f);
            CreateInlineLabel(settings, "За диапазоном", 120f);
            mapPolicy = CloneDropdown(settings, "Dropdown_MapOutOfRangePolicy", 130f, new[] { "Reject", "Clamp" });
            if (mapPolicy != null)
            {
                mapPolicy.onValueChanged.RemoveAllListeners();
                mapPolicy.onValueChanged.AddListener(i => Rotor.performance.outOfRangePolicy = mapPolicy.options[i].text);
            }

            Transform header = CreateHorizontalRow(root, "MapHeader", 30f);
            CreateInlineLabel(header, "RPM", 100f);
            CreateInlineLabel(header, "J", 85f);
            CreateInlineLabel(header, "Ct", 85f);
            CreateInlineLabel(header, "Cq", 85f);
            CreateInlineLabel(header, "", 36f);

            mapRows = CreateScrollRows(root, "PerformanceMapScroll", 470f);
            Button add = CloneButton(root, "Btn_AddMapPoint", "+ Добавить точку");
            Bind(add, () =>
            {
                Rotor.performance.performanceMap.Add(new ConfiguratorPerformanceMapPoint());
                RebuildMapRows();
            });

            CreateHint(root, "Для валидной карты нужна полная прямоугольная сетка RPM × J, минимум два значения J и обязательный J = 0.");
            return page;
        }

        private void EnsureMapSeed()
        {
            if (Rotor.performance.performanceMap == null)
                Rotor.performance.performanceMap = new List<ConfiguratorPerformanceMapPoint>();

            if (Rotor.performance.performanceMap.Count == 0)
            {
                Rotor.performance.performanceMap.Add(new ConfiguratorPerformanceMapPoint
                {
                    advanceRatio = OptionalNumber.From(0)
                });
                Rotor.performance.performanceMap.Add(new ConfiguratorPerformanceMapPoint());
            }
        }

        private void RebuildMapRows()
        {
            if (Rotor == null || mapRows == null)
                return;

            EnsureMapSeed();
            mapTitle.text = $"РОТОР {Rotor.rotorId} — КАРТА ХАРАКТЕРИСТИК";
            SetDropdown(mapPolicy, Rotor.performance.outOfRangePolicy);
            ClearRows(mapRows);

            for (int i = 0; i < Rotor.performance.performanceMap.Count; i++)
            {
                int index = i;
                ConfiguratorPerformanceMapPoint point = Rotor.performance.performanceMap[i];
                Transform row = CreateHorizontalRow(mapRows, "PerformanceMapRow_" + i, 38f);

                TMP_InputField rpm = CloneInput(row, "Input_MapRpm", 100f);
                TMP_InputField j = CloneInput(row, "Input_AdvanceRatio", 85f);
                TMP_InputField ct = CloneInput(row, "Input_Ct", 85f);
                TMP_InputField cq = CloneInput(row, "Input_Cq", 85f);
                Button delete = CloneButton(row, "Btn_DeleteMapPoint", "×", 36f);

                WriteOptional(rpm, point.rpm);
                WriteOptional(j, point.advanceRatio);
                WriteOptional(ct, point.ct);
                WriteOptional(cq, point.cq);

                BindEnd(rpm, v => SetOptional(v, x => point.rpm = x));
                BindEnd(j, v => SetOptional(v, x => point.advanceRatio = x));
                BindEnd(ct, v => SetOptional(v, x => point.ct = x));
                BindEnd(cq, v => SetOptional(v, x => point.cq = x));
                Bind(delete, () =>
                {
                    if (Rotor.performance.performanceMap.Count <= 2)
                    {
                        owner.SetConfiguratorStatus("PerformanceMap должна содержать минимум две точки.");
                        return;
                    }
                    Rotor.performance.performanceMap.RemoveAt(index);
                    RebuildMapRows();
                });
            }
        }

        private GameObject BuildOcvPage()
        {
            GameObject page = CreatePage("Page_BatteryOcvCurve");
            Transform root = EnsureVerticalLayout(page);
            CreateSectionLabel(root, "КРИВАЯ OCV");

            Transform header = CreateHorizontalRow(root, "OcvHeader", 30f);
            CreateInlineLabel(header, "SOC, %", 140f);
            CreateInlineLabel(header, "Напряжение, В", 180f);
            CreateInlineLabel(header, "", 36f);

            ocvRows = CreateScrollRows(root, "OcvScroll", 500f);
            Button add = CloneButton(root, "Btn_AddOcvPoint", "+ Добавить точку");
            Bind(add, () =>
            {
                Draft.battery.ocvCurve.Add(new ConfiguratorOcvPoint());
                RebuildOcvRows();
            });

            CreateHint(root, "Кривая должна покрывать SOC от 0% до 100%; SOC строго возрастает, напряжение не убывает.");
            return page;
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

        private void RebuildOcvRows()
        {
            if (ocvRows == null)
                return;

            EnsureOcvSeed();
            ClearRows(ocvRows);

            for (int i = 0; i < Draft.battery.ocvCurve.Count; i++)
            {
                int index = i;
                ConfiguratorOcvPoint point = Draft.battery.ocvCurve[i];
                Transform row = CreateHorizontalRow(ocvRows, "OcvPointRow_" + i, 38f);

                TMP_InputField soc = CloneInput(row, "Input_SocPercent", 140f);
                TMP_InputField voltage = CloneInput(row, "Input_VoltageV", 180f);
                Button delete = CloneButton(row, "Btn_DeleteOcvPoint", "×", 36f);

                WriteOptionalScaled(soc, point.soc, 100.0);
                WriteOptional(voltage, point.voltageV);

                BindEnd(soc, v => SetOptionalScaled(v, 0.01, x => point.soc = x));
                BindEnd(voltage, v => SetOptional(v, x => point.voltageV = x));
                Bind(delete, () =>
                {
                    if (Draft.battery.ocvCurve.Count <= 2)
                    {
                        owner.SetConfiguratorStatus("OCV-кривая должна содержать минимум две точки.");
                        return;
                    }
                    Draft.battery.ocvCurve.RemoveAt(index);
                    RebuildOcvRows();
                });
            }
        }

        private void ShowOnly(GameObject page)
        {
            if (pageParent == null)
                return;

            foreach (Transform child in pageParent)
            {
                if (child.name.StartsWith("Page_", StringComparison.Ordinal) || child.name == "PageRotor")
                    child.gameObject.SetActive(child.gameObject == page);
            }

            if (page != null)
                page.SetActive(true);
        }

        private GameObject CreatePage(string name)
        {
            GameObject existing = FindObject(name);
            if (existing != null)
                return existing;

            GameObject page = new GameObject(name, typeof(RectTransform));
            page.layer = 5;
            RectTransform rect = page.GetComponent<RectTransform>();
            rect.SetParent(pageParent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 12f);
            rect.offsetMax = new Vector2(-12f, -12f);
            page.SetActive(false);
            return page;
        }

        private Transform EnsureVerticalLayout(GameObject page)
        {
            VerticalLayoutGroup layout = page.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
                layout = page.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            return page.transform;
        }

        private TMP_Text CreateSectionLabel(Transform parent, string text)
        {
            TMP_Text label = Instantiate(textTemplate, parent);
            label.gameObject.name = "Header";
            label.text = text;
            label.fontSize = Mathf.Max(17f, label.fontSize);
            label.fontStyle = FontStyles.Bold;
            LayoutElement layout = EnsureLayout(label.gameObject);
            layout.preferredHeight = 34f;
            return label;
        }

        private TMP_Text CreateHint(Transform parent, string text)
        {
            TMP_Text label = Instantiate(textTemplate, parent);
            label.gameObject.name = "Hint";
            label.text = text;
            label.fontSize = 12f;
            label.enableWordWrapping = true;
            LayoutElement layout = EnsureLayout(label.gameObject);
            layout.preferredHeight = 48f;
            return label;
        }

        private TMP_InputField CreateLabeledInput(Transform parent, string labelText, string name)
        {
            Transform row = CreateHorizontalRow(parent, name + "_Row", 38f);
            CreateInlineLabel(row, labelText, 230f);
            return CloneInput(row, name, 180f);
        }

        private Transform CreateHorizontalRow(Transform parent, string name, float height)
        {
            GameObject row = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.layer = 5;
            row.transform.SetParent(parent, false);
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
            row.GetComponent<LayoutElement>().preferredHeight = height;
            return row.transform;
        }

        private TMP_Text CreateInlineLabel(Transform parent, string text, float width)
        {
            TMP_Text label = Instantiate(textTemplate, parent);
            label.gameObject.name = "Label";
            label.text = text;
            label.fontSize = 12f;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            LayoutElement le = EnsureLayout(label.gameObject);
            le.preferredWidth = width;
            le.flexibleWidth = 0;
            return label;
        }

        private TMP_InputField CloneInput(Transform parent, string name, float width)
        {
            TMP_InputField field = Instantiate(inputTemplate, parent);
            field.gameObject.name = name;
            field.onValueChanged.RemoveAllListeners();
            field.onEndEdit.RemoveAllListeners();
            field.SetTextWithoutNotify("");
            field.contentType = TMP_InputField.ContentType.Standard;
            LayoutElement le = EnsureLayout(field.gameObject);
            le.preferredWidth = width;
            le.flexibleWidth = 0;
            le.preferredHeight = 32f;
            return field;
        }

        private Button CloneButton(Transform parent, string name, string text, float width = 180f)
        {
            Button button = Instantiate(buttonTemplate, parent);
            button.gameObject.name = name;
            button.onClick.RemoveAllListeners();
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = text;
            LayoutElement le = EnsureLayout(button.gameObject);
            le.preferredWidth = width;
            le.flexibleWidth = 0;
            le.preferredHeight = 34f;
            return button;
        }

        private TMP_Dropdown CloneDropdown(Transform parent, string name, float width, IEnumerable<string> options)
        {
            if (dropdownTemplate == null)
                return null;
            TMP_Dropdown dropdown = Instantiate(dropdownTemplate, parent);
            dropdown.gameObject.name = name;
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.ClearOptions();
            dropdown.AddOptions(options.ToList());
            dropdown.SetValueWithoutNotify(0);
            LayoutElement le = EnsureLayout(dropdown.gameObject);
            le.preferredWidth = width;
            le.flexibleWidth = 0;
            le.preferredHeight = 32f;
            return dropdown;
        }

        private Transform CreateScrollRows(Transform parent, string name, float height)
        {
            GameObject scrollGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            scrollGo.layer = 5;
            scrollGo.transform.SetParent(parent, false);
            LayoutElement sle = scrollGo.GetComponent<LayoutElement>();
            sle.preferredHeight = height;
            sle.flexibleHeight = 1;

            Image bg = scrollGo.GetComponent<Image>();
            bg.color = new Color(0.035f, 0.045f, 0.055f, 0.6f);

            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.layer = 5;
            viewportGo.transform.SetParent(scrollGo.transform, false);
            RectTransform viewport = viewportGo.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewportGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.layer = 5;
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup vlg = contentGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 5f;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;
            return contentGo.transform;
        }

        private void ClearRows(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        private static LayoutElement EnsureLayout(GameObject go)
        {
            LayoutElement element = go.GetComponent<LayoutElement>();
            if (element == null) element = go.AddComponent<LayoutElement>();
            return element;
        }

        private void SetOptional(string text, Action<OptionalNumber> setter)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                setter(OptionalNumber.Missing());
                return;
            }

            if (TryNumber(text, out double value))
                setter(OptionalNumber.From(value));
        }

        private void SetOptionalScaled(string text, double factor, Action<OptionalNumber> setter)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                setter(OptionalNumber.Missing());
                return;
            }

            if (TryNumber(text, out double value))
                setter(OptionalNumber.From(value * factor));
        }

        private bool TryNumber(string text, out double value)
        {
            text = (text ?? "").Trim().Replace(',', '.');
            return double.TryParse(text, NumberStyles.Float, invariant, out value)
                   && !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private void WriteOptional(TMP_InputField field, OptionalNumber value)
        {
            if (field == null) return;
            field.SetTextWithoutNotify(value != null && value.hasValue
                ? value.value.ToString("0.##########", invariant)
                : "");
        }

        private void WriteOptionalScaled(TMP_InputField field, OptionalNumber value, double factor)
        {
            if (field == null) return;
            field.SetTextWithoutNotify(value != null && value.hasValue
                ? (value.value * factor).ToString("0.##########", invariant)
                : "");
        }

        private static void SetDropdown(TMP_Dropdown dropdown, string value)
        {
            if (dropdown == null) return;
            int index = dropdown.options.FindIndex(x => x.text == value);
            dropdown.SetValueWithoutNotify(index >= 0 ? index : 0);
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

        private Button FindButtonByNameOrLabel(string name, string label)
        {
            Button named = FindAll<Button>().FirstOrDefault(x => x.name == name);
            if (named != null) return named;

            return FindAll<Button>().FirstOrDefault(button =>
            {
                TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
                return text != null && text.text.Trim().Contains(label, StringComparison.OrdinalIgnoreCase);
            });
        }

        private GameObject FindObject(string name)
        {
            Transform found = FindAll<Transform>().FirstOrDefault(x => x.name == name);
            return found != null ? found.gameObject : null;
        }

        private static T FindIn<T>(Transform root, string name) where T : Component
        {
            if (root == null) return null;
            return root.GetComponentsInChildren<T>(true).FirstOrDefault(x => x.name == name);
        }

        private static T[] FindAll<T>() where T : UnityEngine.Object =>
            FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        private static void SetActive(GameObject go, bool value)
        {
            if (go != null) go.SetActive(value);
        }
    }
}
