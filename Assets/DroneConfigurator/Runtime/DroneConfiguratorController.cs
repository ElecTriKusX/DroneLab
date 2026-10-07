using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace DroneLab.Configurator
{
    [DisallowMultipleComponent]
    public sealed class DroneConfiguratorController : MonoBehaviour
    {
        private const string SectionModel = "Model";
        private const string SectionPhysics = "Physics";
        private const string SectionPropulsion = "Propulsion";
        private const string SectionAero = "Aerodynamics";
        private const string SectionPower = "Power";
        private const string SectionModules = "Modules";

        [SerializeField] private string backScene = "DimaScene";

        private readonly CultureInfo invariant = CultureInfo.InvariantCulture;
        private readonly List<ConfiguratorIssue> issues = new List<ConfiguratorIssue>();

        private DroneConfiguratorDraft draft;
        private DroneAssetBindings bindings;
        private DronePackageManifest manifest;

        private UIDocument document;
        private PanelSettings panel;
        private VisualElement root;
        private VisualElement inspector;
        private VisualElement hierarchyRoot;
        private VisualElement rotorNav;
        private VisualElement previewPane;
        private ScrollView issueList;
        private Label statusLabel;
        private Label previewHud;
        private Label selectedNodeHud;
        private Label validationBadge;

        private RuntimeGltfModelLoader modelLoader;
        private Transform previewWorld;
        private Transform visualFrame;
        private Transform markerFrame;
        private Camera previewCamera;
        private Light previewLight;
        private Material markerMaterial;
        private Material comMaterial;
        private Material axisMaterial;

        private Transform selectedModelNode;
        private ConfiguratorMarker activeMarker;
        private Plane activeDragPlane;
        private bool orbiting;
        private Vector2 lastPointer;
        private float orbitYaw = 40f;
        private float orbitPitch = 25f;
        private float orbitDistance = 3f;
        private Vector3 orbitTarget;
        private ViewMode viewMode = ViewMode.Top;

        private string currentSection = SectionModel;
        private TextField modelPathField;

        private enum ViewMode
        {
            Top,
            Front,
            Side,
            Perspective
        }

        private void OnEnable()
        {
            draft = DroneConfiguratorFactory.Create(4);
            bindings = new DroneAssetBindings();
            manifest = new DronePackageManifest();

            BuildPreviewWorld();
            BuildUi();
            RebuildRotorMarkers();
            RefreshNavigation();
            SelectSection(SectionModel);
            SetStatus("Новый черновик. Загрузите GLB или откройте сохранённый профиль.");
        }

        private void OnDisable()
        {
            if (document != null) Destroy(document);
            if (panel != null) Destroy(panel);
            if (previewWorld != null) Destroy(previewWorld.gameObject);
            if (markerMaterial != null) Destroy(markerMaterial);
            if (comMaterial != null) Destroy(comMaterial);
            if (axisMaterial != null) Destroy(axisMaterial);
        }

        private void BuildPreviewWorld()
        {
            previewWorld = new GameObject("DroneConfiguratorPreviewWorld").transform;
            previewWorld.position = Vector3.zero;

            visualFrame = new GameObject("VisualModelRoot").transform;
            visualFrame.SetParent(previewWorld, false);

            markerFrame = new GameObject("PhysicalMarkerRoot").transform;
            markerFrame.SetParent(previewWorld, false);

            GameObject cameraObject = new GameObject("DroneConfiguratorPreviewCamera");
            cameraObject.transform.SetParent(previewWorld, false);
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(0.018f, 0.021f, 0.025f);
            previewCamera.nearClipPlane = 0.01f;
            previewCamera.farClipPlane = 2000f;
            previewCamera.allowHDR = true;

            GameObject lightObject = new GameObject("DroneConfiguratorKeyLight");
            lightObject.transform.SetParent(previewWorld, false);
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            previewLight = lightObject.AddComponent<Light>();
            previewLight.type = LightType.Directional;
            previewLight.intensity = 12000f;
            previewLight.shadows = LightShadows.Soft;

            modelLoader = gameObject.GetComponent<RuntimeGltfModelLoader>();
            if (modelLoader == null) modelLoader = gameObject.AddComponent<RuntimeGltfModelLoader>();

            markerMaterial = CreateUnlitMaterial(new Color(0.12f, 0.78f, 1f));
            comMaterial = CreateUnlitMaterial(new Color(1f, 0.72f, 0.1f));
            axisMaterial = CreateUnlitMaterial(new Color(0.92f, 0.95f, 1f));

            ApplyCameraView(ViewMode.Top, false);
        }

        private static Material CreateUnlitMaterial(Color color)
        {
            Shader shader = Shader.Find("HDRP/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            material.color = color;
            return material;
        }

        private void BuildUi()
        {
            panel = ScriptableObject.CreateInstance<PanelSettings>();
            panel.scaleMode = PanelScaleMode.ConstantPixelSize;
            panel.sortingOrder = 200;
            panel.themeStyleSheet = Resources.Load<ThemeStyleSheet>("DroneLab/MainMenuTheme");

            document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = panel;
            root = document.rootVisualElement;
            root.name = "drone-configurator-root";
            root.AddToClassList("dc-root");

            StyleSheet styleSheet = Resources.Load<StyleSheet>("DroneLab/Configurator");
            if (styleSheet != null) root.styleSheets.Add(styleSheet);

            VisualElement header = new VisualElement();
            header.AddToClassList("dc-header");
            root.Add(header);

            VisualElement titleGroup = new VisualElement();
            titleGroup.AddToClassList("dc-title-group");
            header.Add(titleGroup);

            Label title = new Label("DRONELAB");
            title.AddToClassList("dc-title");
            titleGroup.Add(title);

            Label subtitle = new Label("MULTIROTOR DIGITAL TWIN CONFIGURATOR");
            subtitle.AddToClassList("dc-subtitle");
            titleGroup.Add(subtitle);

            validationBadge = new Label("VALIDATION —");
            validationBadge.AddToClassList("dc-badge");
            header.Add(validationBadge);

            VisualElement body = new VisualElement();
            body.AddToClassList("dc-body");
            root.Add(body);

            VisualElement left = BuildLeftPanel();
            body.Add(left);

            previewPane = BuildPreviewPane();
            body.Add(previewPane);

            VisualElement right = new VisualElement();
            right.AddToClassList("dc-right");
            body.Add(right);

            Label inspectorTitle = new Label("INSPECTOR");
            inspectorTitle.AddToClassList("dc-panel-heading");
            right.Add(inspectorTitle);

            ScrollView inspectorScroll = new ScrollView();
            inspectorScroll.AddToClassList("dc-inspector-scroll");
            right.Add(inspectorScroll);

            inspector = new VisualElement();
            inspector.AddToClassList("dc-inspector");
            inspectorScroll.Add(inspector);

            VisualElement validation = new VisualElement();
            validation.AddToClassList("dc-validation");
            root.Add(validation);

            VisualElement validationHeader = new VisualElement();
            validationHeader.AddToClassList("dc-validation-header");
            validation.Add(validationHeader);
            validationHeader.Add(new Label("VALIDATION / CONTRACT 1.0.0"));

            issueList = new ScrollView();
            issueList.AddToClassList("dc-issue-list");
            validation.Add(issueList);

            VisualElement footer = new VisualElement();
            footer.AddToClassList("dc-footer");
            root.Add(footer);

            statusLabel = new Label();
            statusLabel.AddToClassList("dc-status");
            footer.Add(statusLabel);

            VisualElement footerButtons = new VisualElement();
            footerButtons.AddToClassList("dc-footer-buttons");
            footer.Add(footerButtons);

            footerButtons.Add(ActionButton("НАЗАД", Back));
            footerButtons.Add(ActionButton("СОХРАНИТЬ ЧЕРНОВИК", SaveDraft));
            footerButtons.Add(ActionButton("ПРОВЕРИТЬ", ValidateDraft));
            Button saveProfile = ActionButton("СОХРАНИТЬ ПРОФИЛЬ", SaveReadyProfile);
            saveProfile.AddToClassList("dc-primary");
            footerButtons.Add(saveProfile);

            previewPane.RegisterCallback<GeometryChangedEvent>(OnPreviewGeometry);
            previewPane.RegisterCallback<PointerDownEvent>(OnPreviewPointerDown);
            previewPane.RegisterCallback<PointerMoveEvent>(OnPreviewPointerMove);
            previewPane.RegisterCallback<PointerUpEvent>(OnPreviewPointerUp);
            previewPane.RegisterCallback<WheelEvent>(OnPreviewWheel);
        }

        private VisualElement BuildLeftPanel()
        {
            VisualElement left = new VisualElement();
            left.AddToClassList("dc-left");

            Label sectionsTitle = new Label("SECTIONS");
            sectionsTitle.AddToClassList("dc-panel-heading");
            left.Add(sectionsTitle);

            VisualElement nav = new VisualElement();
            nav.AddToClassList("dc-nav");
            left.Add(nav);

            nav.Add(NavButton("МОДЕЛЬ", SectionModel));
            nav.Add(NavButton("ФИЗИКА КОРПУСА", SectionPhysics));
            nav.Add(NavButton("ДВИГАТЕЛИ / РОТОРЫ", SectionPropulsion));

            rotorNav = new VisualElement();
            rotorNav.AddToClassList("dc-rotor-nav");
            nav.Add(rotorNav);

            nav.Add(NavButton("АЭРОДИНАМИКА", SectionAero));
            nav.Add(NavButton("ПИТАНИЕ", SectionPower));
            nav.Add(NavButton("МОДУЛИ", SectionModules));

            Label hierarchyTitle = new Label("MODEL HIERARCHY");
            hierarchyTitle.AddToClassList("dc-panel-heading");
            left.Add(hierarchyTitle);

            ScrollView hierarchyScroll = new ScrollView();
            hierarchyScroll.AddToClassList("dc-hierarchy-scroll");
            left.Add(hierarchyScroll);

            hierarchyRoot = new VisualElement();
            hierarchyRoot.AddToClassList("dc-hierarchy");
            hierarchyScroll.Add(hierarchyRoot);

            return left;
        }

        private VisualElement BuildPreviewPane()
        {
            VisualElement center = new VisualElement();
            center.AddToClassList("dc-preview");

            VisualElement toolbar = new VisualElement();
            toolbar.AddToClassList("dc-preview-toolbar");
            center.Add(toolbar);

            toolbar.Add(ActionButton("TOP", () => ApplyCameraView(ViewMode.Top, true)));
            toolbar.Add(ActionButton("FRONT", () => ApplyCameraView(ViewMode.Front, true)));
            toolbar.Add(ActionButton("SIDE", () => ApplyCameraView(ViewMode.Side, true)));
            toolbar.Add(ActionButton("3D", () => ApplyCameraView(ViewMode.Perspective, true)));
            toolbar.Add(ActionButton("FRAME", FrameModel));

            previewHud = new Label("+Z FRONT  /  X RIGHT  /  Y UP");
            previewHud.AddToClassList("dc-preview-hud");
            center.Add(previewHud);

            selectedNodeHud = new Label("NODE: —");
            selectedNodeHud.AddToClassList("dc-selected-hud");
            center.Add(selectedNodeHud);

            return center;
        }

        private Button NavButton(string text, string section)
        {
            Button button = ActionButton(text, () => SelectSection(section));
            button.AddToClassList("dc-nav-button");
            return button;
        }

        private static Button ActionButton(string text, Action action)
        {
            Button button = new Button(action) { text = text };
            button.AddToClassList("dc-button");
            return button;
        }

        private void SelectSection(string section)
        {
            currentSection = section;
            draft.selectedSection = section;
            RebuildInspector();
        }

        private void RebuildInspector()
        {
            if (inspector == null) return;
            inspector.Clear();

            Label title = new Label(currentSection.ToUpperInvariant());
            title.AddToClassList("dc-inspector-title");
            inspector.Add(title);

            switch (currentSection)
            {
                case SectionModel: BuildModelInspector(); break;
                case SectionPhysics: BuildPhysicsInspector(); break;
                case SectionPropulsion: BuildPropulsionInspector(); break;
                case SectionAero: BuildAeroInspector(); break;
                case SectionPower: BuildPowerInspector(); break;
                case SectionModules: BuildModulesInspector(); break;
            }
        }

        private void BuildModelInspector()
        {
            AddHint("GLB рекомендуется: один файл сохраняет иерархию, материалы и текстуры. Физические параметры не извлекаются из mesh автоматически.");

            modelPathField = new TextField("GLB / glTF path") { value = draft.sourceModelPath ?? "" };
            modelPathField.AddToClassList("dc-wide-field");
            inspector.Add(modelPathField);

            VisualElement row = ButtonRow();
            row.Add(ActionButton("ВСТАВИТЬ ПУТЬ", () => modelPathField.value = GUIUtility.systemCopyBuffer.Trim().Trim('"')));
            row.Add(ActionButton("ЗАГРУЗИТЬ", LoadModelFromUi));
            inspector.Add(row);

            AddNumberField("Масштаб модели", "m/unit", "coordinateSystem.modelScaleMetersPerUnit",
                () => OptionalNumber.From(draft.modelScaleMetersPerUnit),
                value =>
                {
                    draft.modelScaleMetersPerUnit = Math.Max(1e-9, value);
                    ApplyVisualTransform();
                    UpdateDimensionsFromModel();
                    FrameModel();
                }, "Required");

            DropdownField forward = new DropdownField(
                "Вперёд модели",
                new List<string> { "+Z", "-Z", "+X", "-X", "+Y", "-Y" },
                Math.Max(0, new List<string> { "+Z", "-Z", "+X", "-X", "+Y", "-Y" }.IndexOf(draft.visualForwardAxis)));
            forward.RegisterValueChangedCallback(evt =>
            {
                draft.visualForwardAxis = evt.newValue;
                bindings.visualForwardAxis = evt.newValue;
                ApplyVisualTransform();
                UpdateDimensionsFromModel();
                FrameModel();
            });
            inspector.Add(forward);

            DropdownField up = new DropdownField(
                "Вверх модели",
                new List<string> { "+Y", "-Y", "+Z", "-Z", "+X", "-X" },
                Math.Max(0, new List<string> { "+Y", "-Y", "+Z", "-Z", "+X", "-X" }.IndexOf(draft.visualUpAxis)));
            up.RegisterValueChangedCallback(evt =>
            {
                draft.visualUpAxis = evt.newValue;
                bindings.visualUpAxis = evt.newValue;
                ApplyVisualTransform();
                UpdateDimensionsFromModel();
                FrameModel();
            });
            inspector.Add(up);

            AddReadOnly("Selected node", SelectedNodePath());

            if (draft.rotors.Count > 0)
            {
                string rotorId = SelectedRotor().rotorId;
                AddReadOnly("Visual binding", bindings.GetOrCreate(rotorId).nodePath);

                VisualElement bindRow = ButtonRow();
                bindRow.Add(ActionButton("УЗЕЛ = ВИНТ", AssignSelectedNodeToRotor));
                bindRow.Add(ActionButton("MOTOR POS = PIVOT", RotorPositionFromSelectedNode));
                inspector.Add(bindRow);
            }

            inspector.Add(ActionButton("BOX COLLIDER ИЗ ГРАНИЦ МОДЕЛИ", EstimateBoxCollider));
            inspector.Add(ActionButton("ОТКРЫТЬ СОХРАНЁННЫЙ DRAFT", LoadDraftFromProfileId));

            if (draft.dimensionsM.hasValue)
            {
                Vector3 size = draft.dimensionsM.ToUnity();
                AddReadOnly("Габариты после масштаба", $"{size.x:F3} × {size.y:F3} × {size.z:F3} m");
            }
        }

        private void BuildPhysicsInspector()
        {
            TextField id = new TextField("Profile ID") { value = draft.profileId };
            id.RegisterValueChangedCallback(evt => draft.profileId = string.IsNullOrWhiteSpace(evt.newValue) ? "unnamed" : evt.newValue.Trim());
            inspector.Add(id);

            TextField name = new TextField("Название") { value = draft.name };
            name.RegisterValueChangedCallback(evt => draft.name = evt.newValue);
            inspector.Add(name);

            TextField manufacturer = new TextField("Производитель") { value = draft.manufacturer };
            manufacturer.RegisterValueChangedCallback(evt => draft.manufacturer = evt.newValue);
            inspector.Add(manufacturer);

            TextField model = new TextField("Модель") { value = draft.model };
            model.RegisterValueChangedCallback(evt => draft.model = evt.newValue);
            inspector.Add(model);

            AddNumberField("Масса", "kg", "massProperties.massKg", () => draft.massKg,
                value => draft.massKg = OptionalNumber.From(value), "Required");

            AddVectorField("Габариты", "m", "massProperties.dimensionsM", draft.dimensionsM, "Required",
                value => draft.dimensionsM = value);

            AddVectorField("Центр масс", "m", "massProperties.centerOfMassLocalM", draft.centerOfMassLocalM, "Required",
                value =>
                {
                    draft.centerOfMassLocalM = value;
                    RebuildRotorMarkers();
                });

            inspector.Add(ActionButton("COM = ЦЕНТР ГЕОМЕТРИИ (ОЦЕНКА)", EstimateComFromGeometry));

            DropdownField inertia = new DropdownField(
                "Инерция",
                new List<string> { "AutoBox", "ManualPrincipal" },
                draft.inertiaMode == "ManualPrincipal" ? 1 : 0);
            inertia.RegisterValueChangedCallback(evt =>
            {
                draft.inertiaMode = evt.newValue;
                RebuildInspector();
            });
            inspector.Add(inertia);

            if (draft.inertiaMode == "ManualPrincipal")
            {
                AddVectorField("I1 / I2 / I3", "kg·m²", "massProperties.inertia.principalMomentsKgM2",
                    draft.principalMomentsKgM2, "Required", value => draft.principalMomentsKgM2 = value);

                AddQuaternionFields();
            }
            else
            {
                AddHint("AutoBox вычисляет приближённую инерцию равномерного параллелепипеда по массе и габаритам. Это оценка, не CAD/измерение.");
            }
        }

        private void AddQuaternionFields()
        {
            VisualElement block = new VisualElement();
            block.AddToClassList("dc-field-block");
            block.Add(new Label("Главные оси quaternion [x y z w]  • REQUIRED"));

            double[] q = draft.principalAxesRotationXyzw ?? new[] { 0.0, 0.0, 0.0, 1.0 };
            string[] labels = { "X", "Y", "Z", "W" };

            for (int i = 0; i < 4; i++)
            {
                int index = i;
                TextField field = new TextField(labels[i]) { value = draft.hasPrincipalAxesRotation ? Format(q[i]) : "" };
                field.RegisterValueChangedCallback(evt =>
                {
                    if (TryNumber(evt.newValue, out double value))
                    {
                        draft.principalAxesRotationXyzw[index] = value;
                        draft.hasPrincipalAxesRotation = true;
                    }
                    else if (string.IsNullOrWhiteSpace(evt.newValue))
                    {
                        draft.hasPrincipalAxesRotation = false;
                    }
                });
                block.Add(field);
            }

            inspector.Add(block);
        }

        private void BuildPropulsionInspector()
        {
            VisualElement countRow = new VisualElement();
            countRow.AddToClassList("dc-inline");

            TextField count = new TextField("Количество моторов") { value = draft.rotors.Count.ToString(invariant) };
            count.AddToClassList("dc-flex");
            countRow.Add(count);
            countRow.Add(ActionButton("ПРИМЕНИТЬ", () =>
            {
                if (!int.TryParse(count.value, out int value))
                {
                    SetStatus("Количество моторов должно быть целым числом.");
                    return;
                }
                DroneConfiguratorFactory.SetRotorCount(draft, value);
                EnsureBindings();
                RebuildRotorMarkers();
                RefreshNavigation();
                RebuildInspector();
            }));
            inspector.Add(countRow);

            ConfiguratorRotor rotor = SelectedRotor();
            if (rotor == null) return;

            AddReadOnly("Редактируется", rotor.rotorId + $"  ({draft.selectedRotorIndex + 1}/{draft.rotors.Count})");

            TextField id = new TextField("Rotor ID") { value = rotor.rotorId };
            id.RegisterValueChangedCallback(evt =>
            {
                string old = rotor.rotorId;
                string next = string.IsNullOrWhiteSpace(evt.newValue) ? old : evt.newValue.Trim();
                rotor.rotorId = next;
                RotorVisualBinding binding = bindings.rotorVisualBindings.FirstOrDefault(x => x.rotorId == old);
                if (binding != null) binding.rotorId = next;
                RefreshNavigation();
            });
            inspector.Add(id);

            AddVectorField("Позиция ступицы", "m", $"rotors[{draft.selectedRotorIndex}].geometry.positionLocalM",
                rotor.positionLocalM, "Required", value =>
                {
                    rotor.positionLocalM = value;
                    RebuildRotorMarkers();
                });

            AddAxisField(rotor);

            DropdownField spin = new DropdownField("Вращение (вид сверху)", new List<string> { "CW", "CCW" }, rotor.spinDirection == "CW" ? 0 : 1);
            spin.RegisterValueChangedCallback(evt => rotor.spinDirection = evt.newValue);
            inspector.Add(spin);

            AddSubheading("MOTOR / FIRST ORDER");
            AddNumberField("Min RPM", "rpm", RotorPath("motor.minRpm"), () => rotor.motor.minRpm,
                v => rotor.motor.minRpm = OptionalNumber.From(v), "Required");
            AddNumberField("Idle RPM", "rpm", RotorPath("motor.idleRpm"), () => rotor.motor.idleRpm,
                v => rotor.motor.idleRpm = OptionalNumber.From(v), "Required");
            AddNumberField("Max RPM", "rpm", RotorPath("motor.maxRpm"), () => rotor.motor.maxRpm,
                v => rotor.motor.maxRpm = OptionalNumber.From(v), "Required");
            AddNumberField("Разгон τ", "ms", RotorPath("motor.responseTimeUpS"), () => rotor.motor.responseTimeUpS,
                v => rotor.motor.responseTimeUpS = OptionalNumber.From(v / 1000.0), "Required", 1000.0);
            AddNumberField("Снижение τ", "ms", RotorPath("motor.responseTimeDownS"), () => rotor.motor.responseTimeDownS,
                v => rotor.motor.responseTimeDownS = OptionalNumber.From(v / 1000.0), "Required", 1000.0);

            AddSubheading("PROPELLER");
            AddNumberField("Диаметр", "mm", RotorPath("propeller.diameterM"), () => rotor.propeller.diameterM,
                v => rotor.propeller.diameterM = OptionalNumber.From(v / 1000.0), "Required", 1000.0);
            AddNumberField("Шаг", "mm", RotorPath("propeller.pitchM"), () => rotor.propeller.pitchM,
                v => rotor.propeller.pitchM = OptionalNumber.From(v / 1000.0), "Required", 1000.0);

            TextField blades = new TextField("Лопастей") { value = rotor.propeller.bladeCount.ToString(invariant) };
            blades.RegisterValueChangedCallback(evt =>
            {
                if (int.TryParse(evt.newValue, out int value)) rotor.propeller.bladeCount = Mathf.Max(1, value);
            });
            inspector.Add(blades);

            AddSubheading("THRUST / REACTION TORQUE");
            DropdownField performance = new DropdownField("Модель", new List<string> { "OmegaSquared", "CtCq" }, rotor.performance.model == "CtCq" ? 1 : 0);
            performance.RegisterValueChangedCallback(evt =>
            {
                rotor.performance.model = evt.newValue;
                RebuildInspector();
            });
            inspector.Add(performance);

            if (rotor.performance.model == "OmegaSquared")
            {
                AddNumberField("kThrust", "N/(rad/s)²", RotorPath("performance.kThrustNPerRadPerSecSquared"),
                    () => rotor.performance.kThrustNPerRadPerSecSquared,
                    v => rotor.performance.kThrustNPerRadPerSecSquared = OptionalNumber.From(v), "Required");
                AddNumberField("kTorque", "N·m/(rad/s)²", RotorPath("performance.kTorqueNmPerRadPerSecSquared"),
                    () => rotor.performance.kTorqueNmPerRadPerSecSquared,
                    v => rotor.performance.kTorqueNmPerRadPerSecSquared = OptionalNumber.From(v), "Required");
                AddNumberField("ρ калибровки", "kg/m³", RotorPath("performance.referenceAirDensityKgM3"),
                    () => rotor.performance.referenceAirDensityKgM3,
                    v => rotor.performance.referenceAirDensityKgM3 = OptionalNumber.From(v), "Required");
            }
            else
            {
                AddNumberField("CT", "", RotorPath("performance.ct"), () => rotor.performance.ct,
                    v => rotor.performance.ct = OptionalNumber.From(v), "Required");
                AddNumberField("CQ", "", RotorPath("performance.cq"), () => rotor.performance.cq,
                    v => rotor.performance.cq = OptionalNumber.From(v), "Required");
            }

            AddSubheading("VISUAL BINDING");
            RotorVisualBinding visualBinding = bindings.GetOrCreate(rotor.rotorId);
            AddReadOnly("Node", string.IsNullOrWhiteSpace(visualBinding.nodePath) ? "не назначен" : visualBinding.nodePath);
            AddReadOnly("Physical axis", VectorText(rotor.thrustAxisLocal));

            AddPlainVectorField("Visual spin axis", visualBinding.visualRotationAxisLocal, value => visualBinding.visualRotationAxisLocal = value);

            VisualElement bindRow = ButtonRow();
            bindRow.Add(ActionButton("УЗЕЛ = ВИНТ", AssignSelectedNodeToRotor));
            bindRow.Add(ActionButton("POS = PIVOT", RotorPositionFromSelectedNode));
            inspector.Add(bindRow);

            inspector.Add(ActionButton("СКОПИРОВАТЬ ОБЩИЕ ДАННЫЕ НА ВСЕ МОТОРЫ", () =>
            {
                DroneConfiguratorFactory.CopyCommonRotorData(draft, draft.selectedRotorIndex);
                SetStatus("Параметры motor/propeller/performance скопированы. ID, position, axis, CW/CCW не изменены.");
                RebuildInspector();
            }));
        }

        private void BuildAeroInspector()
        {
            AddHint("Для первой рабочей версии используется AxisApproximation. Площади можно оценить по габаритному боксу; Cd остаётся ручным/измеренным параметром.");

            Toggle enabled = new Toggle("Body drag module") { value = draft.modules.bodyDrag };
            enabled.RegisterValueChangedCallback(evt => draft.modules.bodyDrag = evt.newValue);
            inspector.Add(enabled);

            DropdownField model = new DropdownField("Drag model", new List<string> { "AxisApproximation" }, 0);
            inspector.Add(model);

            AddVectorField("Cd X/Y/Z", "", "bodyAerodynamics.dragCd", draft.bodyAerodynamics.dragCd,
                draft.modules.bodyDrag ? "Module" : "Optional", value => draft.bodyAerodynamics.dragCd = value);

            AddVectorField("Area X/Y/Z", "m²", "bodyAerodynamics.referenceAreaM2", draft.bodyAerodynamics.referenceAreaM2,
                draft.modules.bodyDrag ? "Module" : "Optional", value => draft.bodyAerodynamics.referenceAreaM2 = value);

            inspector.Add(ActionButton("ОЦЕНИТЬ ПЛОЩАДИ ИЗ ГАБАРИТНОГО БОКСА", EstimateAreasFromDimensions));
        }

        private void BuildPowerInspector()
        {
            DropdownField mode = new DropdownField("Battery mode", new List<string> { "None", "Simple", "Electrical" },
                Math.Max(0, new List<string> { "None", "Simple", "Electrical" }.IndexOf(draft.battery.mode)));
            mode.RegisterValueChangedCallback(evt =>
            {
                draft.battery.mode = evt.newValue;
                RebuildInspector();
            });
            inspector.Add(mode);

            if (draft.battery.mode == "None")
            {
                AddHint("MVP: питание не ограничивает RPM. batteryDischarge, batteryVoltageSag и motorElectrical должны оставаться выключены.");
                return;
            }

            AddNumberField("Cell count", "", "powerSystem.battery.cellCount", () => draft.battery.cellCount,
                v => draft.battery.cellCount = OptionalNumber.From(Math.Round(v)), "Module");
            AddNumberField("Nominal voltage", "V", "powerSystem.battery.nominalVoltageV", () => draft.battery.nominalVoltageV,
                v => draft.battery.nominalVoltageV = OptionalNumber.From(v), "Module");
            AddNumberField("Capacity", "Ah", "powerSystem.battery.capacityAh", () => draft.battery.capacityAh,
                v => draft.battery.capacityAh = OptionalNumber.From(v), "Module");
            AddNumberField("Initial SOC", "0..1", "powerSystem.battery.initialSoc", () => draft.battery.initialSoc,
                v => draft.battery.initialSoc = OptionalNumber.From(v), "Module");
            AddNumberField("Internal R", "Ω", "powerSystem.battery.internalResistanceOhm", () => draft.battery.internalResistanceOhm,
                v => draft.battery.internalResistanceOhm = OptionalNumber.From(v), "Module");
            AddNumberField("Max discharge", "A", "powerSystem.battery.maxDischargeCurrentA", () => draft.battery.maxDischargeCurrentA,
                v => draft.battery.maxDischargeCurrentA = OptionalNumber.From(v), "Module");

            AddHint("Simple/Electrical требуют дополнительных motor.electrical/OCV данных по действующему контракту. Черновик сохраняется, а SAVE PROFILE покажет адресные ошибки ProfileLoader.");
        }

        private void BuildModulesInspector()
        {
            DropdownField fidelity = new DropdownField("Physics fidelity", new List<string> { "Basic", "Advanced" }, draft.fidelity == "Advanced" ? 1 : 0);
            fidelity.RegisterValueChangedCallback(evt => draft.fidelity = evt.newValue);
            inspector.Add(fidelity);

            AddHint("Переключатели ниже меняют физические модули профиля. Это НЕ режим подробности интерфейса.");

            AddModuleToggle("Motor response", () => draft.modules.motorResponse, v => draft.modules.motorResponse = v);
            AddModuleToggle("Body drag", () => draft.modules.bodyDrag, v => draft.modules.bodyDrag = v);
            AddModuleToggle("Wind interaction", () => draft.modules.windInteraction, v => draft.modules.windInteraction = v);
            AddModuleToggle("Ground effect", () => draft.modules.groundEffect, v => draft.modules.groundEffect = v);
            AddModuleToggle("Rotor aerodynamics", () => draft.modules.rotorAerodynamics, v => draft.modules.rotorAerodynamics = v);
            AddModuleToggle("Blade flapping", () => draft.modules.bladeFlapping, v => draft.modules.bladeFlapping = v);
            AddModuleToggle("Induced drag", () => draft.modules.inducedDrag, v => draft.modules.inducedDrag = v);
            AddModuleToggle("Battery discharge", () => draft.modules.batteryDischarge, v => draft.modules.batteryDischarge = v);
            AddModuleToggle("Battery voltage sag", () => draft.modules.batteryVoltageSag, v => draft.modules.batteryVoltageSag = v);
            AddModuleToggle("Motor electrical", () => draft.modules.motorElectrical, v => draft.modules.motorElectrical = v);
            AddModuleToggle("Gyroscopic rotor effects", () => draft.modules.gyroscopicRotorEffects, v => draft.modules.gyroscopicRotorEffects = v);
        }

        private void AddModuleToggle(string label, Func<bool> getter, Action<bool> setter)
        {
            Toggle toggle = new Toggle(label) { value = getter() };
            toggle.RegisterValueChangedCallback(evt => setter(evt.newValue));
            inspector.Add(toggle);
        }

        private void AddAxisField(ConfiguratorRotor rotor)
        {
            VisualElement block = new VisualElement();
            block.AddToClassList("dc-field-block");
            block.Add(new Label("Ось тяги  • REQUIRED  • физическая"));

            double[] axis = rotor.thrustAxisLocal ?? new[] { 0.0, 1.0, 0.0 };
            string[] labels = { "X", "Y", "Z" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                TextField field = new TextField(labels[i]) { value = Format(axis[i]) };
                field.RegisterValueChangedCallback(evt =>
                {
                    if (!TryNumber(evt.newValue, out double value)) return;
                    rotor.thrustAxisLocal[index] = value;
                });
                block.Add(field);
            }

            block.Add(ActionButton("НОРМАЛИЗОВАТЬ ОСЬ", () =>
            {
                Vector3 v = new Vector3((float)rotor.thrustAxisLocal[0], (float)rotor.thrustAxisLocal[1], (float)rotor.thrustAxisLocal[2]);
                if (v.sqrMagnitude < 1e-10f)
                {
                    SetStatus("Нельзя нормализовать нулевую ось.");
                    return;
                }
                v.Normalize();
                rotor.thrustAxisLocal = new[] { (double)v.x, (double)v.y, (double)v.z };
                RebuildRotorMarkers();
                RebuildInspector();
            }));

            inspector.Add(block);
        }

        private void AddPlainVectorField(string label, double[] data, Action<double[]> setter)
        {
            VisualElement block = new VisualElement();
            block.AddToClassList("dc-field-block");
            block.Add(new Label(label));

            double[] working = data != null && data.Length >= 3 ? (double[])data.Clone() : new[] { 0.0, 1.0, 0.0 };
            string[] axes = { "X", "Y", "Z" };

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                TextField field = new TextField(axes[i]) { value = Format(working[i]) };
                field.RegisterValueChangedCallback(evt =>
                {
                    if (!TryNumber(evt.newValue, out double value)) return;
                    working[index] = value;
                    setter((double[])working.Clone());
                });
                block.Add(field);
            }
            inspector.Add(block);
        }

        private void AddVectorField(
            string label,
            string unit,
            string path,
            OptionalVector vector,
            string requirement,
            Action<OptionalVector> setter)
        {
            VisualElement block = new VisualElement();
            block.AddToClassList("dc-field-block");

            VisualElement header = new VisualElement();
            header.AddToClassList("dc-field-header");
            header.Add(new Label(label + (string.IsNullOrWhiteSpace(unit) ? "" : "  [" + unit + "]")));
            Label req = new Label(requirement.ToUpperInvariant());
            req.AddToClassList("dc-requirement");
            header.Add(req);
            block.Add(header);

            OptionalVector working = vector ?? OptionalVector.Missing();
            if (working.value == null || working.value.Length < 3) working.value = new double[3];

            string[] axes = { "X", "Y", "Z" };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                TextField field = new TextField(axes[i]) { value = working.hasValue ? Format(working.value[i]) : "" };
                field.RegisterValueChangedCallback(evt =>
                {
                    if (string.IsNullOrWhiteSpace(evt.newValue))
                    {
                        working.hasValue = false;
                        setter(working);
                        return;
                    }

                    if (!TryNumber(evt.newValue, out double value)) return;
                    working.value[index] = value;
                    working.hasValue = true;
                    setter(working);
                    SetSource(path, "User", "Configurator UI");
                });
                block.Add(field);
            }

            block.Add(SourceDropdown(path));
            inspector.Add(block);
        }

        private void AddNumberField(
            string label,
            string unit,
            string path,
            Func<OptionalNumber> getter,
            Action<double> setter,
            string requirement,
            double displayMultiplier = 1.0)
        {
            VisualElement block = new VisualElement();
            block.AddToClassList("dc-field-block");

            VisualElement row = new VisualElement();
            row.AddToClassList("dc-field-row");

            Label caption = new Label(label);
            caption.AddToClassList("dc-field-label");
            row.Add(caption);

            OptionalNumber current = getter();
            TextField field = new TextField
            {
                value = current != null && current.hasValue ? Format(current.value * displayMultiplier) : ""
            };
            field.AddToClassList("dc-number");
            row.Add(field);

            Label units = new Label(unit);
            units.AddToClassList("dc-unit");
            row.Add(units);

            Label req = new Label(requirement.ToUpperInvariant());
            req.AddToClassList("dc-requirement");
            row.Add(req);

            block.Add(row);
            block.Add(SourceDropdown(path));

            field.RegisterValueChangedCallback(evt =>
            {
                if (string.IsNullOrWhiteSpace(evt.newValue))
                {
                    OptionalNumber target = getter();
                    if (target != null) target.hasValue = false;
                    return;
                }

                if (!TryNumber(evt.newValue, out double value))
                {
                    field.AddToClassList("dc-invalid");
                    return;
                }

                field.RemoveFromClassList("dc-invalid");
                setter(value);
                SetSource(path, "User", "Configurator UI");
            });

            inspector.Add(block);
        }

        private DropdownField SourceDropdown(string path)
        {
            List<string> sources = new List<string> { "User", "Measured", "Preset", "Estimated", "Computed" };
            ConfiguratorProvenance provenance = draft.GetOrCreateProvenance(path);
            int index = Mathf.Max(0, sources.IndexOf(provenance.sourceType));

            DropdownField source = new DropdownField("Источник", sources, index);
            source.AddToClassList("dc-source");
            source.RegisterValueChangedCallback(evt =>
            {
                provenance.sourceType = evt.newValue;
                if (evt.newValue == "User" && string.IsNullOrWhiteSpace(provenance.source))
                    provenance.source = "Configurator UI";
            });
            return source;
        }

        private void SetSource(string path, string type, string source)
        {
            ConfiguratorProvenance p = draft.GetOrCreateProvenance(path);
            p.sourceType = type;
            p.source = source;
        }

        private void AddReadOnly(string label, string value)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("dc-readonly-row");
            row.Add(new Label(label));
            Label text = new Label(value ?? "—");
            text.AddToClassList("dc-readonly-value");
            row.Add(text);
            inspector.Add(row);
        }

        private void AddHint(string text)
        {
            Label hint = new Label(text);
            hint.AddToClassList("dc-hint");
            inspector.Add(hint);
        }

        private void AddSubheading(string text)
        {
            Label heading = new Label(text);
            heading.AddToClassList("dc-subheading");
            inspector.Add(heading);
        }

        private static VisualElement ButtonRow()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("dc-inline");
            return row;
        }

        private ConfiguratorRotor SelectedRotor()
        {
            if (draft.rotors == null || draft.rotors.Count == 0) return null;
            draft.selectedRotorIndex = Mathf.Clamp(draft.selectedRotorIndex, 0, draft.rotors.Count - 1);
            return draft.rotors[draft.selectedRotorIndex];
        }

        private string RotorPath(string suffix) => $"rotors[{draft.selectedRotorIndex}].{suffix}";

        private void RefreshNavigation()
        {
            if (rotorNav == null) return;
            rotorNav.Clear();

            for (int i = 0; i < draft.rotors.Count; i++)
            {
                int index = i;
                Button button = ActionButton("↳ " + draft.rotors[i].rotorId, () =>
                {
                    draft.selectedRotorIndex = index;
                    SelectSection(SectionPropulsion);
                    RebuildRotorMarkers();
                });
                button.AddToClassList("dc-rotor-button");
                if (i == draft.selectedRotorIndex) button.AddToClassList("dc-selected");
                rotorNav.Add(button);
            }
        }

        private async void LoadModelFromUi()
        {
            string path = modelPathField == null ? draft.sourceModelPath : modelPathField.value;
            await LoadModel(path);
        }

        private async Task LoadModel(string path)
        {
            path = (path ?? "").Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(path))
            {
                SetStatus("Укажите путь к .glb.");
                return;
            }

            SetStatus("Загрузка модели…");
            var result = await modelLoader.LoadAsync(path, visualFrame);

            if (!result.success)
            {
                SetStatus("Ошибка модели: " + result.error);
                return;
            }

            draft.sourceModelPath = path;
            bindings.modelFile = "model.glb";
            selectedModelNode = modelLoader.LoadedRoot;

            ApplyVisualTransform();
            UpdateDimensionsFromModel();
            RebuildHierarchy();
            FrameModel();
            RebuildRotorMarkers();
            RebuildInspector();

            if (string.Equals(Path.GetExtension(path), ".gltf", StringComparison.OrdinalIgnoreCase))
                SetStatus("glTF загружен. Для переносимого пакета используйте GLB: внешние .bin/texture сейчас не копируются.");
            else
                SetStatus("GLB загружен. Выберите узлы винтов слева и назначьте их роторам.");
        }

        private void ApplyVisualTransform()
        {
            if (modelLoader.LoadedRoot == null) return;

            Vector3 forward = AxisVector(draft.visualForwardAxis);
            Vector3 up = AxisVector(draft.visualUpAxis);

            if (Vector3.Cross(forward, up).sqrMagnitude < 1e-6f)
            {
                SetStatus("Forward и Up не могут быть параллельны.");
                return;
            }

            Quaternion sourceFrame = Quaternion.LookRotation(forward, up);
            modelLoader.LoadedRoot.localRotation = Quaternion.Inverse(sourceFrame);
            modelLoader.LoadedRoot.localScale = Vector3.one * (float)draft.modelScaleMetersPerUnit;
        }

        private static Vector3 AxisVector(string axis)
        {
            switch (axis)
            {
                case "+X": return Vector3.right;
                case "-X": return Vector3.left;
                case "+Y": return Vector3.up;
                case "-Y": return Vector3.down;
                case "-Z": return Vector3.back;
                default: return Vector3.forward;
            }
        }

        private void UpdateDimensionsFromModel()
        {
            if (!RuntimeGltfModelLoader.TryGetBoundsInFrame(modelLoader.LoadedRoot, markerFrame, out Bounds bounds))
                return;

            draft.dimensionsM = OptionalVector.From(bounds.size);
            SetSource("massProperties.dimensionsM", "Computed", "GLB renderer bounds after confirmed scale/axes");
        }

        private void RebuildHierarchy()
        {
            if (hierarchyRoot == null) return;
            hierarchyRoot.Clear();

            if (modelLoader.LoadedRoot == null)
            {
                hierarchyRoot.Add(new Label("Модель не загружена"));
                return;
            }

            AddHierarchyNode(modelLoader.LoadedRoot, hierarchyRoot, 0);
        }

        private void AddHierarchyNode(Transform node, VisualElement parent, int depth)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("dc-tree-row");
            row.style.paddingLeft = depth * 10f;

            VisualElement children = new VisualElement();
            bool hasChildren = node.childCount > 0;

            Button expand = ActionButton(hasChildren ? "▾" : "·", () =>
            {
                bool visible = children.style.display != DisplayStyle.None;
                children.style.display = visible ? DisplayStyle.None : DisplayStyle.Flex;
            });
            expand.AddToClassList("dc-tree-expander");
            row.Add(expand);

            string path = RuntimeGltfModelLoader.PathFrom(modelLoader.LoadedRoot, node);
            Button select = ActionButton(node.name, () => SelectModelNode(path));
            select.AddToClassList("dc-tree-node");
            row.Add(select);

            parent.Add(row);
            parent.Add(children);

            for (int i = 0; i < node.childCount; i++)
                AddHierarchyNode(node.GetChild(i), children, depth + 1);
        }

        private void SelectModelNode(string path)
        {
            selectedModelNode = RuntimeGltfModelLoader.FindByPath(modelLoader.LoadedRoot, path);
            selectedNodeHud.text = "NODE: " + (string.IsNullOrWhiteSpace(path) ? "<root>" : path);
            if (currentSection == SectionModel || currentSection == SectionPropulsion)
                RebuildInspector();
        }

        private string SelectedNodePath()
        {
            if (modelLoader.LoadedRoot == null || selectedModelNode == null) return "—";
            string path = RuntimeGltfModelLoader.PathFrom(modelLoader.LoadedRoot, selectedModelNode);
            return string.IsNullOrWhiteSpace(path) ? "<root>" : path;
        }

        private void AssignSelectedNodeToRotor()
        {
            ConfiguratorRotor rotor = SelectedRotor();
            if (rotor == null || selectedModelNode == null || modelLoader.LoadedRoot == null)
            {
                SetStatus("Сначала выберите ротор и узел модели.");
                return;
            }

            bindings.GetOrCreate(rotor.rotorId).nodePath =
                RuntimeGltfModelLoader.PathFrom(modelLoader.LoadedRoot, selectedModelNode);
            SetStatus(rotor.rotorId + ": визуальный винт назначен.");
            RebuildInspector();
        }

        private void RotorPositionFromSelectedNode()
        {
            ConfiguratorRotor rotor = SelectedRotor();
            if (rotor == null || selectedModelNode == null)
            {
                SetStatus("Сначала выберите узел модели.");
                return;
            }

            Vector3 local = markerFrame.InverseTransformPoint(selectedModelNode.position);
            rotor.positionLocalM = OptionalVector.From(local);
            SetSource(RotorPath("geometry.positionLocalM"), "Computed", "Selected GLB node pivot");
            RebuildRotorMarkers();
            RebuildInspector();
            SetStatus(rotor.rotorId + ": положение взято из pivot выбранного узла.");
        }

        private void EstimateBoxCollider()
        {
            if (!RuntimeGltfModelLoader.TryGetBoundsInFrame(modelLoader.LoadedRoot, markerFrame, out Bounds bounds))
            {
                SetStatus("Сначала загрузите модель.");
                return;
            }

            bindings.colliders.Clear();
            bindings.colliders.Add(new ColliderBinding
            {
                nodePath = "",
                type = "Box",
                centerLocalM = new[] { (double)bounds.center.x, (double)bounds.center.y, (double)bounds.center.z },
                sizeM = new[] { (double)bounds.size.x, (double)bounds.size.y, (double)bounds.size.z }
            });

            SetStatus("Box collider binding создан из renderer bounds. Проверьте его перед использованием.");
        }

        private void EstimateComFromGeometry()
        {
            if (!RuntimeGltfModelLoader.TryGetBoundsInFrame(modelLoader.LoadedRoot, markerFrame, out Bounds bounds))
            {
                SetStatus("Сначала загрузите модель.");
                return;
            }

            draft.centerOfMassLocalM = OptionalVector.From(bounds.center);
            SetSource("massProperties.centerOfMassLocalM", "Estimated", "Geometric bounds center; not a measured COM");
            RebuildRotorMarkers();
            RebuildInspector();
            SetStatus("COM поставлен в центр геометрии как ОЦЕНКА.");
        }

        private void EstimateAreasFromDimensions()
        {
            if (!draft.dimensionsM.hasValue)
            {
                SetStatus("Сначала задайте габариты.");
                return;
            }

            Vector3 d = draft.dimensionsM.ToUnity();
            draft.bodyAerodynamics.referenceAreaM2 = OptionalVector.From(new Vector3(
                d.y * d.z,
                d.x * d.z,
                d.x * d.y));
            SetSource("bodyAerodynamics.referenceAreaM2", "Estimated", "Bounding-box projected areas; confirm or replace with silhouette measurement");
            RebuildInspector();
            SetStatus("Площади оценены по габаритному боксу. Это не точный silhouette.");
        }

        private void EnsureBindings()
        {
            HashSet<string> valid = new HashSet<string>(draft.rotors.Select(x => x.rotorId));
            bindings.rotorVisualBindings.RemoveAll(x => !valid.Contains(x.rotorId));
            foreach (ConfiguratorRotor rotor in draft.rotors) bindings.GetOrCreate(rotor.rotorId);
        }

        private void RebuildRotorMarkers()
        {
            if (markerFrame == null) return;

            for (int i = markerFrame.childCount - 1; i >= 0; i--)
                Destroy(markerFrame.GetChild(i).gameObject);

            for (int i = 0; i < draft.rotors.Count; i++)
            {
                ConfiguratorRotor rotor = draft.rotors[i];
                Vector3 position = rotor.positionLocalM.hasValue ? rotor.positionLocalM.ToUnity() : Vector3.zero;

                GameObject anchor = new GameObject("RotorMarker_" + rotor.rotorId);
                anchor.transform.SetParent(markerFrame, false);
                anchor.transform.localPosition = position;

                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "Pick_" + rotor.rotorId;
                sphere.transform.SetParent(anchor.transform, false);
                sphere.transform.localScale = Vector3.one * 0.055f;
                Renderer renderer = sphere.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = markerMaterial;

                ConfiguratorMarker marker = sphere.AddComponent<ConfiguratorMarker>();
                marker.rotorIndex = i;
                marker.isCom = false;

                LineRenderer line = anchor.AddComponent<LineRenderer>();
                line.useWorldSpace = false;
                line.positionCount = 2;
                line.startWidth = 0.009f;
                line.endWidth = 0.003f;
                line.sharedMaterial = axisMaterial;
                line.SetPosition(0, Vector3.zero);
                Vector3 axis = new Vector3((float)rotor.thrustAxisLocal[0], (float)rotor.thrustAxisLocal[1], (float)rotor.thrustAxisLocal[2]);
                if (axis.sqrMagnitude > 1e-10f) axis.Normalize();
                line.SetPosition(1, axis * 0.20f);
            }

            if (draft.centerOfMassLocalM.hasValue)
            {
                GameObject anchor = new GameObject("COMMarker");
                anchor.transform.SetParent(markerFrame, false);
                anchor.transform.localPosition = draft.centerOfMassLocalM.ToUnity();

                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "Pick_COM";
                sphere.transform.SetParent(anchor.transform, false);
                sphere.transform.localScale = Vector3.one * 0.07f;
                Renderer renderer = sphere.GetComponent<Renderer>();
                if (renderer != null) renderer.sharedMaterial = comMaterial;

                ConfiguratorMarker marker = sphere.AddComponent<ConfiguratorMarker>();
                marker.rotorIndex = -1;
                marker.isCom = true;
            }

            RefreshNavigation();
        }

        private void OnPreviewGeometry(GeometryChangedEvent evt)
        {
            if (previewCamera == null || Screen.width <= 0 || Screen.height <= 0) return;

            Rect rect = previewPane.worldBound;
            float x = rect.xMin / Screen.width;
            float y = (Screen.height - rect.yMax) / Screen.height;
            float width = rect.width / Screen.width;
            float height = rect.height / Screen.height;

            previewCamera.rect = new Rect(
                Mathf.Clamp01(x),
                Mathf.Clamp01(y),
                Mathf.Clamp01(width),
                Mathf.Clamp01(height));
        }

        private void OnPreviewPointerDown(PointerDownEvent evt)
        {
            lastPointer = evt.position;

            if (evt.button == 1)
            {
                orbiting = viewMode == ViewMode.Perspective;
                previewPane.CapturePointer(evt.pointerId);
                evt.StopPropagation();
                return;
            }

            if (evt.button != 0) return;

            Ray ray = ScreenRay(evt.position);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                ConfiguratorMarker marker = hit.collider.GetComponent<ConfiguratorMarker>();
                if (marker != null)
                {
                    activeMarker = marker;
                    SetupDragPlane(marker.transform.parent.position);
                    previewPane.CapturePointer(evt.pointerId);

                    if (!marker.isCom && marker.rotorIndex >= 0 && marker.rotorIndex < draft.rotors.Count)
                    {
                        draft.selectedRotorIndex = marker.rotorIndex;
                        currentSection = SectionPropulsion;
                        RefreshNavigation();
                    }
                    else if (marker.isCom)
                    {
                        currentSection = SectionPhysics;
                    }

                    RebuildInspector();
                    evt.StopPropagation();
                }
            }
        }

        private void OnPreviewPointerMove(PointerMoveEvent evt)
        {
            Vector2 current = evt.position;

            if (orbiting && previewPane.HasPointerCapture(evt.pointerId))
            {
                Vector2 delta = current - lastPointer;
                orbitYaw += delta.x * 0.25f;
                orbitPitch = Mathf.Clamp(orbitPitch - delta.y * 0.25f, -80f, 80f);
                UpdatePerspectiveCamera();
                lastPointer = current;
                evt.StopPropagation();
                return;
            }

            if (activeMarker == null || !previewPane.HasPointerCapture(evt.pointerId)) return;

            Ray ray = ScreenRay(evt.position);
            if (!activeDragPlane.Raycast(ray, out float distance)) return;

            Vector3 world = ray.GetPoint(distance);
            Vector3 local = markerFrame.InverseTransformPoint(world);

            if (activeMarker.isCom)
            {
                Vector3 old = draft.centerOfMassLocalM.hasValue ? draft.centerOfMassLocalM.ToUnity() : Vector3.zero;
                local = PreserveLockedCoordinate(old, local);
                draft.centerOfMassLocalM = OptionalVector.From(local);
                activeMarker.transform.parent.localPosition = local;
                SetSource("massProperties.centerOfMassLocalM", "User", "3D marker");
                previewHud.text = $"COM  X {local.x:F3}  Y {local.y:F3}  Z {local.z:F3} m";
            }
            else
            {
                int index = activeMarker.rotorIndex;
                ConfiguratorRotor rotor = draft.rotors[index];
                Vector3 old = rotor.positionLocalM.hasValue ? rotor.positionLocalM.ToUnity() : Vector3.zero;
                local = PreserveLockedCoordinate(old, local);
                rotor.positionLocalM = OptionalVector.From(local);
                activeMarker.transform.parent.localPosition = local;
                SetSource($"rotors[{index}].geometry.positionLocalM", "User", "3D marker");
                previewHud.text = $"{rotor.rotorId}  X {local.x:F3}  Y {local.y:F3}  Z {local.z:F3} m";
            }

            evt.StopPropagation();
        }

        private Vector3 PreserveLockedCoordinate(Vector3 old, Vector3 candidate)
        {
            switch (viewMode)
            {
                case ViewMode.Front: candidate.z = old.z; break;
                case ViewMode.Side: candidate.x = old.x; break;
                default: candidate.y = old.y; break;
            }
            return candidate;
        }

        private void OnPreviewPointerUp(PointerUpEvent evt)
        {
            activeMarker = null;
            orbiting = false;
            if (previewPane.HasPointerCapture(evt.pointerId))
                previewPane.ReleasePointer(evt.pointerId);

            previewHud.text = viewMode == ViewMode.Top
                ? "+Z FRONT  /  X RIGHT  /  Y UP"
                : "RMB: ORBIT  /  WHEEL: ZOOM  /  LMB MARKER: MOVE";

            RebuildInspector();
        }

        private void OnPreviewWheel(WheelEvent evt)
        {
            float sign = Mathf.Sign(evt.delta.y);

            if (previewCamera.orthographic)
            {
                previewCamera.orthographicSize = Mathf.Clamp(previewCamera.orthographicSize * (sign > 0 ? 1.12f : 0.88f), 0.03f, 1000f);
            }
            else
            {
                orbitDistance = Mathf.Clamp(orbitDistance * (sign > 0 ? 1.12f : 0.88f), 0.05f, 1000f);
                UpdatePerspectiveCamera();
            }

            evt.StopPropagation();
        }

        private void SetupDragPlane(Vector3 worldPoint)
        {
            Vector3 normal;
            switch (viewMode)
            {
                case ViewMode.Front: normal = Vector3.forward; break;
                case ViewMode.Side: normal = Vector3.right; break;
                default: normal = Vector3.up; break;
            }
            activeDragPlane = new Plane(normal, worldPoint);
        }

        private Ray ScreenRay(Vector3 panelPosition)
        {
            Vector3 screen = new Vector3(panelPosition.x, Screen.height - panelPosition.y, 0f);
            return previewCamera.ScreenPointToRay(screen);
        }

        private void ApplyCameraView(ViewMode mode, bool frame)
        {
            viewMode = mode;

            if (mode == ViewMode.Perspective)
            {
                previewCamera.orthographic = false;
                if (frame) FrameModel();
                else UpdatePerspectiveCamera();
                return;
            }

            previewCamera.orthographic = true;
            if (frame) FrameModel();
            else
            {
                float distance = Mathf.Max(orbitDistance, 2f);
                if (mode == ViewMode.Top)
                {
                    previewCamera.transform.position = orbitTarget + Vector3.up * distance;
                    previewCamera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
                }
                else if (mode == ViewMode.Front)
                {
                    previewCamera.transform.position = orbitTarget + Vector3.forward * distance;
                    previewCamera.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
                }
                else
                {
                    previewCamera.transform.position = orbitTarget + Vector3.right * distance;
                    previewCamera.transform.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
                }
            }
        }

        private void FrameModel()
        {
            Bounds bounds;
            bool hasBounds = RuntimeGltfModelLoader.TryGetWorldBounds(modelLoader.LoadedRoot, out bounds);

            if (!hasBounds)
            {
                bounds = new Bounds(Vector3.zero, Vector3.one);
                foreach (Transform child in markerFrame) bounds.Encapsulate(child.position);
            }

            orbitTarget = bounds.center;
            float radius = Mathf.Max(0.15f, bounds.extents.magnitude);
            orbitDistance = Mathf.Max(0.5f, radius * 3.2f);

            if (viewMode == ViewMode.Perspective)
            {
                UpdatePerspectiveCamera();
                return;
            }

            previewCamera.orthographic = true;
            previewCamera.orthographicSize = Mathf.Max(0.15f, Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.35f);

            if (viewMode == ViewMode.Top)
            {
                previewCamera.transform.position = orbitTarget + Vector3.up * orbitDistance;
                previewCamera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            }
            else if (viewMode == ViewMode.Front)
            {
                previewCamera.orthographicSize = Mathf.Max(0.15f, Mathf.Max(bounds.extents.x, bounds.extents.y) * 1.35f);
                previewCamera.transform.position = orbitTarget + Vector3.forward * orbitDistance;
                previewCamera.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            }
            else
            {
                previewCamera.orthographicSize = Mathf.Max(0.15f, Mathf.Max(bounds.extents.z, bounds.extents.y) * 1.35f);
                previewCamera.transform.position = orbitTarget + Vector3.right * orbitDistance;
                previewCamera.transform.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
            }
        }

        private void UpdatePerspectiveCamera()
        {
            previewCamera.orthographic = false;
            Quaternion rotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
            Vector3 direction = rotation * Vector3.forward;
            previewCamera.transform.position = orbitTarget - direction * orbitDistance;
            previewCamera.transform.LookAt(orbitTarget, Vector3.up);
        }

        private void ValidateDraft()
        {
            issues.Clear();
            issues.AddRange(DroneConfiguratorProfileAdapter.Validate(draft, out _));
            RefreshIssues();

            int errors = issues.Count(x => string.Equals(x.Severity, "Error", StringComparison.OrdinalIgnoreCase));
            int warnings = issues.Count(x => string.Equals(x.Severity, "Warning", StringComparison.OrdinalIgnoreCase));
            validationBadge.text = errors == 0 ? $"VALID  •  {warnings} WARN" : $"{errors} ERR  •  {warnings} WARN";
            SetStatus(errors == 0 ? "Профиль проходит schema + semantic validation текущего ProfileLoader." : "Найдены ошибки. Нажмите на ошибку снизу, чтобы перейти к разделу.");
        }

        private void RefreshIssues()
        {
            issueList.Clear();

            if (issues.Count == 0)
            {
                Label empty = new Label("Ошибок пока нет. Нажмите ПРОВЕРИТЬ.");
                empty.AddToClassList("dc-hint");
                issueList.Add(empty);
                return;
            }

            foreach (ConfiguratorIssue issue in issues)
            {
                Button row = ActionButton(issue.ToString(), () => JumpToIssue(issue.Path));
                row.AddToClassList(issue.Severity == "Error" ? "dc-issue-error" : "dc-issue-warning");
                issueList.Add(row);
            }
        }

        private void JumpToIssue(string path)
        {
            if (path == null) return;

            if (path.StartsWith("rotors", StringComparison.Ordinal))
            {
                int open = path.IndexOf('[');
                int close = path.IndexOf(']');
                if (open >= 0 && close > open && int.TryParse(path.Substring(open + 1, close - open - 1), out int index))
                    draft.selectedRotorIndex = Mathf.Clamp(index, 0, Mathf.Max(0, draft.rotors.Count - 1));
                RefreshNavigation();
                SelectSection(SectionPropulsion);
            }
            else if (path.StartsWith("massProperties", StringComparison.Ordinal) || path.StartsWith("metadata", StringComparison.Ordinal))
                SelectSection(SectionPhysics);
            else if (path.StartsWith("bodyAerodynamics", StringComparison.Ordinal))
                SelectSection(SectionAero);
            else if (path.StartsWith("powerSystem", StringComparison.Ordinal))
                SelectSection(SectionPower);
            else if (path.StartsWith("physicsConfiguration", StringComparison.Ordinal) || path.StartsWith("groundEffect", StringComparison.Ordinal))
                SelectSection(SectionModules);
            else
                SelectSection(SectionModel);
        }

        private void SaveDraft()
        {
            try
            {
                EnsureBindings();
                manifest.profileId = draft.profileId;
                DroneConfiguratorStorage.SaveDraft(draft, bindings, manifest);
                SetStatus("Draft сохранён: " + DroneConfiguratorStorage.ProfileDirectory(draft.profileId));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                SetStatus("Ошибка сохранения draft: " + ex.Message);
            }
        }

        private void SaveReadyProfile()
        {
            issues.Clear();
            issues.AddRange(DroneConfiguratorProfileAdapter.Validate(draft, out string strictJson));
            RefreshIssues();

            if (DroneConfiguratorProfileAdapter.HasErrors(issues))
            {
                validationBadge.text = issues.Count(x => x.Severity == "Error") + " ERR";
                SetStatus("profile.json НЕ сохранён: исправьте ошибки ProfileLoader.");
                return;
            }

            try
            {
                EnsureBindings();
                manifest.profileId = draft.profileId;
                DroneConfiguratorStorage.SaveReadyProfile(draft, bindings, manifest, strictJson);
                validationBadge.text = "VALID";
                SetStatus("Готовый profile.json и пакет сохранены: " + DroneConfiguratorStorage.ProfileDirectory(draft.profileId));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                SetStatus("Ошибка сохранения profile.json: " + ex.Message);
            }
        }

        private async void LoadDraftFromProfileId()
        {
            if (!DroneConfiguratorStorage.TryLoadDraft(draft.profileId, out DroneConfiguratorDraft loadedDraft,
                    out DroneAssetBindings loadedBindings, out DronePackageManifest loadedManifest, out string error))
            {
                SetStatus("Не удалось открыть draft: " + error);
                return;
            }

            draft = loadedDraft;
            bindings = loadedBindings ?? new DroneAssetBindings();
            manifest = loadedManifest ?? new DronePackageManifest();
            EnsureBindings();

            RebuildRotorMarkers();
            RefreshNavigation();

            if (!string.IsNullOrWhiteSpace(draft.sourceModelPath) && File.Exists(draft.sourceModelPath))
                await LoadModel(draft.sourceModelPath);
            else
                RebuildInspector();

            SetStatus("Draft открыт: " + draft.profileId);
        }

        private void Back()
        {
            if (!string.IsNullOrWhiteSpace(backScene) && Application.CanStreamedLevelBeLoaded(backScene))
                SceneManager.LoadScene(backScene);
            else
                SetStatus("Back scene не найдена в Build Profiles: " + backScene);
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null) statusLabel.text = message;
            Debug.Log("[DroneConfigurator] " + message);
        }

        private bool TryNumber(string text, out double value)
        {
            text = (text ?? "").Trim().Replace(',', '.');
            return double.TryParse(text, NumberStyles.Float, invariant, out value) &&
                   !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private string Format(double value) => value.ToString("0.##########", invariant);

        private static string VectorText(double[] vector)
        {
            if (vector == null || vector.Length < 3) return "—";
            return $"({vector[0]:0.###}, {vector[1]:0.###}, {vector[2]:0.###})";
        }
    }

    public sealed class ConfiguratorMarker : MonoBehaviour
    {
        public int rotorIndex = -1;
        public bool isCom;
    }
}
