using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace DroneLab.Configurator
{
    /// <summary>
    /// Controller for the hand-authored uGUI configurator scene.
    /// It never creates Canvas/panels/buttons/fields. The UI remains editable in the scene/prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DroneConfiguratorCanvasController : MonoBehaviour
    {
        [Header("Scene references (auto-found by name if empty)")]
        [SerializeField] private RuntimeGltfModelLoader modelLoader;
        [SerializeField] private Transform visualModelRoot;
        [SerializeField] private Transform physicalMarkerRoot;
        [SerializeField] private GameObject editorPlaceholder;
        [SerializeField] private Camera previewCamera;
        [SerializeField] private RawImage previewRawImage;

        [Header("Navigation")]
        [SerializeField] private string backScene = "DimaScene";
        [SerializeField] private bool autoFindByName = true;

        [Header("HDRP preview")]
        [Tooltip("Fixed EV used only by PreviewCamera. This prevents HDRP auto exposure from blowing out dark preview scenes.")]
        [SerializeField, Range(-10f, 10f)] private float previewFixedExposure = 0f;
        [SerializeField] private Color previewBackgroundColor = new Color(0.0157f, 0.0275f, 0.0353f, 1f);
        [Tooltip("Dedicated volume layer used only by PreviewCamera.")]
        [SerializeField, Range(0, 31)] private int previewVolumeLayer = 31;

        private GameObject previewVolumeObject;
        private VolumeProfile previewVolumeProfile;

        private readonly CultureInfo invariant = CultureInfo.InvariantCulture;

        private DroneConfiguratorDraft draft;
        private DroneAssetBindings bindings;
        private DronePackageManifest manifest;

        private GameObject pageModel;
        private GameObject pageMass;
        private GameObject pageRotor;
        private GameObject manualInertiaPanel;

        private readonly List<Button> rotorButtons = new();
        private readonly List<Transform> rotorMarkers = new();

        private int selectedRotorIndex;
        private Transform selectedModelNode;

        // Header / navigation
        private Button btnModel, btnMass, btnRotors, btnPropeller, btnAerodynamics, btnBattery, btnTemperature, btnModules, btnValidation;
        private Button btnBasic, btnAdvanced;
        private Button btnBack, btnValidateProfile, btnSaveDraft, btnSaveProfile;
        private TMP_Text statusText, rotorCountText;

        // Preview
        private Button btnMove, btnThrustAxis, btnTop, btnFront, btnSide, btn3D, btnFrame;
        private Button btnThrustGraph, btnTorqueGraph, btnCurrentGraph;
        private EventTrigger previewTrigger;

        // Model page
        private TMP_InputField inputModelPath;
        private TMP_Text inputModelPathText;
        private TMP_InputField inputModelScale;
        private TMP_Dropdown dropdownForwardAxis, dropdownUpAxis;
        private TMP_Text textDimensions, textSelectedNode;
        private Button btnPasteModelPath, btnLoadModel, btnCreateBoxCollider;

        // Mass page
        private TMP_InputField inputProfileId, inputName, inputManufacturer, inputModelName, inputMassKg;
        private TMP_InputField inputDimensionX, inputDimensionY, inputDimensionZ;
        private TMP_InputField inputComX, inputComY, inputComZ;
        private TMP_InputField inputI1, inputI2, inputI3, inputQx, inputQy, inputQz, inputQw;
        private Button btnEstimateCom;
        private TMP_Dropdown dropdownInertiaMode;

        // Rotor page
        private TMP_Text textRotorName, textVisualNode;
        private TMP_InputField inputPosX, inputPosY, inputPosZ;
        private TMP_InputField inputAxisX, inputAxisY, inputAxisZ;
        private TMP_InputField inputMinRpm, inputIdleRpm, inputMaxRpm, inputResponseUpMs, inputResponseDownMs;
        private TMP_InputField inputDiameterMm, inputPitchMm, inputBladeCount;
        private TMP_InputField inputKThrust, inputKTorque, inputReferenceDensity;
        private TMP_Dropdown dropdownPerformanceModel;
        private Button btnAxisFromModel, btnCW, btnCCW, btnAssignVisualNode, btnPositionFromNode;

        private enum PreviewView { Top, Front, Side, Perspective }
        private PreviewView previewView = PreviewView.Perspective;
        private Vector3 previewTarget = Vector3.zero;
        private float previewDistance = 2.5f;
        private float orbitYaw = 135f;
        private float orbitPitch = 28f;

        private bool draggingMarker;
        private int draggedMarkerIndex = -1;
        private Plane dragPlane;
        private bool orbiting;
        private Vector2 previousPointerPosition;

        private void Awake()
        {
            if (autoFindByName)
                FindSceneObjects();

            if (modelLoader == null || visualModelRoot == null || physicalMarkerRoot == null || previewCamera == null || previewRawImage == null)
            {
                Debug.LogError("DroneConfiguratorCanvasController: missing RuntimeGltfModelLoader / VisualModelRoot / PhysicalMarkerRoot / PreviewCamera / PreviewRawImage.", this);
                enabled = false;
                return;
            }

            int authoredRotorCount = Mathf.Max(4, Mathf.Max(rotorButtons.Count, rotorMarkers.Count));
            draft = DroneConfiguratorFactory.Create(authoredRotorCount);
            bindings = new DroneAssetBindings();
            manifest = new DronePackageManifest();

            ConfigureIsolatedHdrpPreview();

            InitializeDropdowns();
            BindButtons();
            BindInputs();
            BindPreviewEvents();

            SelectPage(pageModel);
            SelectRotor(0);
            SyncDraftPositionsFromSceneMarkers();
            ApplyPreviewView(PreviewView.Perspective);
            RefreshAllUi();
            FramePreview();

            SetStatus("Готово. Загрузите GLB или заполните профиль вручную.");
        }

        private void ConfigureIsolatedHdrpPreview()
        {
            if (previewCamera == null)
                return;

            // The main scene may use Enviro / auto exposure / bright HDRP skies.
            // A RenderTexture preview must not inherit those exposure decisions,
            // otherwise a dark background can make a PBR GLB look completely blown out.
            HDAdditionalCameraData hdCamera = previewCamera.GetComponent<HDAdditionalCameraData>();
            if (hdCamera == null)
                hdCamera = previewCamera.gameObject.AddComponent<HDAdditionalCameraData>();

            int layer = Mathf.Clamp(previewVolumeLayer, 0, 31);
            int layerMask = 1 << layer;

            hdCamera.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
            hdCamera.backgroundColorHDR = previewBackgroundColor;
            hdCamera.volumeLayerMask = layerMask;

            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = previewBackgroundColor;

            if (previewVolumeObject != null)
                Destroy(previewVolumeObject);

            previewVolumeObject = new GameObject("PreviewVolumeRuntime");
            previewVolumeObject.hideFlags = HideFlags.DontSave;
            previewVolumeObject.layer = layer;
            previewVolumeObject.transform.SetParent(previewCamera.transform.parent, false);

            Volume volume = previewVolumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10000f;
            volume.weight = 1f;

            previewVolumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            previewVolumeProfile.hideFlags = HideFlags.DontSave;
            volume.profile = previewVolumeProfile;

            Exposure exposure = previewVolumeProfile.Add<Exposure>();
            exposure.active = true;
            exposure.mode.Override(ExposureMode.Fixed);
            exposure.fixedExposure.Override(previewFixedExposure);
            exposure.compensation.Override(0f);

            Tonemapping tonemapping = previewVolumeProfile.Add<Tonemapping>();
            tonemapping.active = true;
            tonemapping.mode.Override(TonemappingMode.Neutral);
        }

        private void FindSceneObjects()
        {
            if (modelLoader == null)
                modelLoader = FindObjectsByType<RuntimeGltfModelLoader>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            visualModelRoot ??= FindTransform("VisualModelRoot");
            physicalMarkerRoot ??= FindTransform("PhysicalMarkerRoot");
            editorPlaceholder ??= FindObject("EditorPlaceholder");
            previewCamera ??= FindComponent<Camera>("PreviewCamera");
            previewRawImage ??= FindComponent<RawImage>("PreviewRawImage");

            pageModel = FindObject("Page_Model");
            pageMass = FindObject("Page_Mass");
            pageRotor = FindObject("PageRotor") ?? FindObject("Page_Rotor");
            manualInertiaPanel = FindObject("ManualInertiaPanel");

            btnModel = FindButton("Btn_Model");
            btnMass = FindButton("Btn_Mass");
            btnRotors = FindButton("Btn_Rotors");
            btnPropeller = FindButton("Btn_Propeller");
            btnAerodynamics = FindButton("Btn_Aerodynamics");
            btnBattery = FindButton("Btn_Battery");
            btnTemperature = FindButton("Btn_Temperature");
            btnModules = FindButton("Btn_Modules");
            btnValidation = FindButton("Btn_Validation");
            btnBasic = FindButton("BasicButton");
            btnAdvanced = FindButton("AdvancedButton");

            btnMove = FindButton("Btn_Move");
            btnThrustAxis = FindButton("Btn_ThrustAxis");
            btnTop = FindButton("Btn_Top");
            btnFront = FindButton("Btn_Front");
            btnSide = FindButton("Btn_Side");
            btn3D = FindButton("Btn_3D");
            btnFrame = FindButton("Btn_Frame");

            btnThrustGraph = FindButton("Btn_ThrustGraph");
            btnTorqueGraph = FindButton("Btn_TorqueGraph");
            btnCurrentGraph = FindButton("Btn_CurrentGraph");

            btnBack = FindButton("Btn_Back");
            btnValidateProfile = FindButton("Btn_ValidateProfile");
            btnSaveDraft = FindButton("Btn_SaveDraft");
            btnSaveProfile = FindButton("Btn_SaveProfile");

            statusText = FindTmpText("StatusText");
            rotorCountText = FindTmpText("RotorCountText");

            inputModelPath = FindInput("Input_ModelPath");
            inputModelPathText = FindTmpText("Input_ModelPath");
            inputModelScale = FindInput("Input_ModelScale");
            dropdownForwardAxis = FindDropdown("Dropdown_ForwardAxis");
            dropdownUpAxis = FindDropdown("Dropdown_UpAxis");
            textDimensions = FindTmpText("Text_Dimensions");
            textSelectedNode = FindTmpText("Text_SelectedNode");
            btnPasteModelPath = FindButton("Btn_PasteModelPath");
            btnLoadModel = FindButton("Btn_LoadModel");
            btnCreateBoxCollider = FindButton("Btn_CreateBoxCollider");

            inputProfileId = FindInput("Input_ProfileId");
            inputName = FindInput("Input_Name");
            inputManufacturer = FindInput("Input_Manufacturer");
            inputModelName = FindInput("Input_ModelName");
            inputMassKg = FindInput("Input_MassKg");
            inputDimensionX = FindInput("Input_DimensionX");
            inputDimensionY = FindInput("Input_DimensionY");
            inputDimensionZ = FindInput("Input_DimensionZ");
            inputComX = FindInput("Input_ComX");
            inputComY = FindInput("Input_ComY");
            inputComZ = FindInput("Input_ComZ");
            inputI1 = FindInput("Input_I1");
            inputI2 = FindInput("Input_I2");
            inputI3 = FindInput("Input_I3");
            inputQx = FindInput("Input_Qx");
            inputQy = FindInput("Input_Qy");
            inputQz = FindInput("Input_Qz");
            inputQw = FindInput("Input_Qw");
            btnEstimateCom = FindButton("Btn_EstimateCOM");
            dropdownInertiaMode = FindDropdown("Dropdown_InertiaMode");

            textRotorName = FindTmpText("Text_RotorName");
            inputPosX = FindInput("Input_PosX");
            inputPosY = FindInput("Input_PosY");
            inputPosZ = FindInput("Input_PosZ");
            inputAxisX = FindInput("Input_AxisX");
            inputAxisY = FindInput("Input_AxisY");
            inputAxisZ = FindInput("Input_AxisZ");
            btnAxisFromModel = FindButton("Btn_AxisFromModel");
            btnCW = FindButton("Btn_CW");
            btnCCW = FindButton("Btn_CCW");
            inputMinRpm = FindInput("Input_MinRPM");
            inputIdleRpm = FindInput("Input_IdleRPM");
            inputMaxRpm = FindInput("Input_MaxRPM");
            inputResponseUpMs = FindInput("Input_ResponseUpMs");
            inputResponseDownMs = FindInput("Input_ResponseDownMs");
            inputDiameterMm = FindInput("Input_DiameterMm");
            inputPitchMm = FindInput("Input_PitchMm");
            inputBladeCount = FindInput("Input_BladeCount");
            dropdownPerformanceModel = FindDropdown("Dropdown_PerformanceModel");
            inputKThrust = FindInput("Input_KThrust");
            inputKTorque = FindInput("Input_KTorque");
            inputReferenceDensity = FindInput("Input_ReferenceDensity");
            textVisualNode = FindTmpText("Text_VisualNode");
            btnAssignVisualNode = FindButton("Btn_AssignVisualNode");
            btnPositionFromNode = FindButton("Btn_PositionFromNode");

            CacheRotorButtons();
            CacheRotorMarkers();
        }

        private void InitializeDropdowns()
        {
            EnsureDropdownOptions(dropdownForwardAxis, new[] { "+Z", "-Z", "+X", "-X", "+Y", "-Y" }, "+Z");
            EnsureDropdownOptions(dropdownUpAxis, new[] { "+Y", "-Y", "+Z", "-Z", "+X", "-X" }, "+Y");
            EnsureDropdownOptions(dropdownInertiaMode, new[] { "Автоматически", "Ручной ввод" }, "Автоматически");
            EnsureDropdownOptions(dropdownPerformanceModel, new[] { "OmegaSquared", "CtCq" }, "OmegaSquared");
        }

        private static void EnsureDropdownOptions(TMP_Dropdown dropdown, IEnumerable<string> options, string defaultValue)
        {
            if (dropdown == null) return;
            if (dropdown.options == null || dropdown.options.Count == 0)
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(options.ToList());
            }

            int index = dropdown.options.FindIndex(x => string.Equals(x.text.Trim(), defaultValue, StringComparison.OrdinalIgnoreCase));
            if (index >= 0) dropdown.SetValueWithoutNotify(index);
        }

        private void BindButtons()
        {
            Bind(btnModel, () => SelectPage(pageModel));
            Bind(btnMass, () => SelectPage(pageMass));
            Bind(btnRotors, () => { SelectPage(pageRotor); SelectRotor(selectedRotorIndex); });

            for (int i = 0; i < rotorButtons.Count; i++)
            {
                int index = i;
                Bind(rotorButtons[i], () =>
                {
                    SelectPage(pageRotor);
                    SelectRotor(index);
                });
            }

            Bind(btnBasic, () => { draft.fidelity = "Basic"; SetStatus("Fidelity: Basic."); });
            Bind(btnAdvanced, () => { draft.fidelity = "Advanced"; SetStatus("Fidelity: Advanced."); });

            Bind(btnPasteModelPath, PasteModelPath);
            Bind(btnLoadModel, () => _ = LoadModelFromUiAsync());
            Bind(btnCreateBoxCollider, CreateBoxColliderBinding);
            Bind(btnEstimateCom, EstimateComFromModel);

            Bind(btnCW, () => { CurrentRotor().spinDirection = "CW"; RefreshRotorUi(); });
            Bind(btnCCW, () => { CurrentRotor().spinDirection = "CCW"; RefreshRotorUi(); });
            Bind(btnAxisFromModel, UseSelectedNodeAxis);
            Bind(btnAssignVisualNode, AssignSelectedNodeAsVisualPropeller);
            Bind(btnPositionFromNode, MoveRotorToSelectedNodePivot);

            Bind(btnMove, () => SetStatus("Режим: перемещение физической точки ротора. Перетащите маркер в preview."));
            Bind(btnThrustAxis, () => SetStatus("Ось тяги редактируется в Axis X/Y/Z; «На модели» берёт local up выбранного узла."));
            Bind(btnTop, () => ApplyPreviewView(PreviewView.Top));
            Bind(btnFront, () => ApplyPreviewView(PreviewView.Front));
            Bind(btnSide, () => ApplyPreviewView(PreviewView.Side));
            Bind(btn3D, () => ApplyPreviewView(PreviewView.Perspective));
            Bind(btnFrame, FramePreview);

            Bind(btnThrustGraph, () => SetStatus("График тяги использует текущую performance-модель. Визуальная линия будет отдельным этапом."));
            Bind(btnTorqueGraph, () => SetStatus("График момента использует kTorque / CQ."));
            Bind(btnCurrentGraph, () => SetStatus("Ток доступен после подключения Electrical-модели двигателя."));

            Bind(btnBack, Back);
            Bind(btnValidateProfile, ValidateProfile);
            Bind(btnSaveDraft, SaveDraft);
            Bind(btnSaveProfile, SaveReadyProfile);

            Bind(btnValidation, ValidateProfile);
            Bind(btnPropeller, () => { SelectPage(pageRotor); SetStatus("Характеристики винта редактируются на странице ротора."); });
            Bind(btnAerodynamics, () => SetStatus("Страница аэродинамики ещё не свёрстана в ручной Canvas."));
            Bind(btnBattery, () => SetStatus("Страница батареи ещё не свёрстана в ручной Canvas."));
            Bind(btnTemperature, () => SetStatus("Температура относится к расширенному профилю и пока не свёрстана."));
            Bind(btnModules, () => SetStatus("Страница модулей ещё не свёрстана в ручной Canvas."));

            Button addRotor = FindButton("Btn_AddRotor");
            Bind(addRotor, () =>
            {
                SetStatus("UI создаётся руками: для M5/M6 продублируйте Btn_Rotor_M4 и Marker_M4 в сцене, затем перезапустите Play.");
            });
        }

        private void BindInputs()
        {
            BindEnd(inputModelScale, _ => ApplyModelTransform());

            BindEnd(inputProfileId, value => draft.profileId = string.IsNullOrWhiteSpace(value) ? "unnamed" : value.Trim());
            BindEnd(inputName, value => draft.name = value ?? "");
            BindEnd(inputManufacturer, value => draft.manufacturer = value ?? "");
            BindEnd(inputModelName, value => draft.model = value ?? "");
            BindOptionalNumber(inputMassKg, value => draft.massKg = value);

            BindEnd(inputDimensionX, _ => ReadDimensions());
            BindEnd(inputDimensionY, _ => ReadDimensions());
            BindEnd(inputDimensionZ, _ => ReadDimensions());
            BindEnd(inputComX, _ => ReadCom());
            BindEnd(inputComY, _ => ReadCom());
            BindEnd(inputComZ, _ => ReadCom());

            if (dropdownForwardAxis != null)
            {
                dropdownForwardAxis.onValueChanged.RemoveAllListeners();
                dropdownForwardAxis.onValueChanged.AddListener(_ => ApplyModelTransform());
            }

            if (dropdownUpAxis != null)
            {
                dropdownUpAxis.onValueChanged.RemoveAllListeners();
                dropdownUpAxis.onValueChanged.AddListener(_ => ApplyModelTransform());
            }

            if (dropdownInertiaMode != null)
            {
                dropdownInertiaMode.onValueChanged.RemoveAllListeners();
                dropdownInertiaMode.onValueChanged.AddListener(_ =>
                {
                    string value = dropdownInertiaMode.options[dropdownInertiaMode.value].text;
                    draft.inertiaMode = value.Contains("Руч", StringComparison.OrdinalIgnoreCase) || value.Contains("Manual", StringComparison.OrdinalIgnoreCase)
                        ? "ManualPrincipal"
                        : "AutoBox";

                    if (manualInertiaPanel != null)
                        manualInertiaPanel.SetActive(draft.inertiaMode == "ManualPrincipal");

                    ReadManualInertia();
                });
            }

            BindEnd(inputI1, _ => ReadManualInertia());
            BindEnd(inputI2, _ => ReadManualInertia());
            BindEnd(inputI3, _ => ReadManualInertia());
            BindEnd(inputQx, _ => ReadManualInertia());
            BindEnd(inputQy, _ => ReadManualInertia());
            BindEnd(inputQz, _ => ReadManualInertia());
            BindEnd(inputQw, _ => ReadManualInertia());

            BindEnd(inputPosX, _ => ReadRotorPosition());
            BindEnd(inputPosY, _ => ReadRotorPosition());
            BindEnd(inputPosZ, _ => ReadRotorPosition());
            BindEnd(inputAxisX, _ => ReadRotorAxis());
            BindEnd(inputAxisY, _ => ReadRotorAxis());
            BindEnd(inputAxisZ, _ => ReadRotorAxis());

            BindOptionalNumber(inputMinRpm, v => CurrentRotor().motor.minRpm = v);
            BindOptionalNumber(inputIdleRpm, v => CurrentRotor().motor.idleRpm = v);
            BindOptionalNumber(inputMaxRpm, v => CurrentRotor().motor.maxRpm = v);
            BindScaledOptionalNumber(inputResponseUpMs, 0.001, v => CurrentRotor().motor.responseTimeUpS = v);
            BindScaledOptionalNumber(inputResponseDownMs, 0.001, v => CurrentRotor().motor.responseTimeDownS = v);
            BindScaledOptionalNumber(inputDiameterMm, 0.001, v => CurrentRotor().propeller.diameterM = v);
            BindScaledOptionalNumber(inputPitchMm, 0.001, v => CurrentRotor().propeller.pitchM = v);

            BindEnd(inputBladeCount, value =>
            {
                if (int.TryParse(value, out int count))
                    CurrentRotor().propeller.bladeCount = Mathf.Max(1, count);
            });

            if (dropdownPerformanceModel != null)
            {
                dropdownPerformanceModel.onValueChanged.RemoveAllListeners();
                dropdownPerformanceModel.onValueChanged.AddListener(_ =>
                {
                    string label = dropdownPerformanceModel.options[dropdownPerformanceModel.value].text;
                    CurrentRotor().performance.model = label.Contains("Ct", StringComparison.OrdinalIgnoreCase) ? "CtCq" : "OmegaSquared";
                });
            }

            BindOptionalNumber(inputKThrust, v => CurrentRotor().performance.kThrustNPerRadPerSecSquared = v);
            BindOptionalNumber(inputKTorque, v => CurrentRotor().performance.kTorqueNmPerRadPerSecSquared = v);
            BindOptionalNumber(inputReferenceDensity, v => CurrentRotor().performance.referenceAirDensityKgM3 = v);
        }

        private void BindPreviewEvents()
        {
            if (previewRawImage == null) return;

            previewTrigger = previewRawImage.GetComponent<EventTrigger>();
            if (previewTrigger == null)
                previewTrigger = previewRawImage.gameObject.AddComponent<EventTrigger>();

            previewTrigger.triggers.Clear();
            AddTrigger(EventTriggerType.PointerDown, data => PreviewPointerDown((PointerEventData)data));
            AddTrigger(EventTriggerType.Drag, data => PreviewDrag((PointerEventData)data));
            AddTrigger(EventTriggerType.PointerUp, data => PreviewPointerUp((PointerEventData)data));
            AddTrigger(EventTriggerType.Scroll, data => PreviewScroll((PointerEventData)data));
        }

        private void AddTrigger(EventTriggerType type, Action<BaseEventData> callback)
        {
            EventTrigger.Entry entry = new() { eventID = type };
            entry.callback.AddListener(data => callback(data));
            previewTrigger.triggers.Add(entry);
        }

        private void PasteModelPath()
        {
            string path = GUIUtility.systemCopyBuffer.Trim().Trim('"');
            draft.sourceModelPath = path;
            SetModelPathDisplay(path);
        }

        private string GetModelPath()
        {
            if (inputModelPath != null)
                return inputModelPath.text.Trim().Trim('"');

            if (!string.IsNullOrWhiteSpace(draft.sourceModelPath))
                return draft.sourceModelPath.Trim().Trim('"');

            return inputModelPathText != null ? inputModelPathText.text.Trim().Trim('"') : "";
        }

        private void SetModelPathDisplay(string path)
        {
            if (inputModelPath != null)
                inputModelPath.SetTextWithoutNotify(path);
            else if (inputModelPathText != null)
                inputModelPathText.text = string.IsNullOrWhiteSpace(path) ? "…" : path;
        }

        private async Task LoadModelFromUiAsync()
        {
            string path = GetModelPath();

            if (string.IsNullOrWhiteSpace(path))
            {
                SetStatus("Скопируйте путь к .glb и нажмите «Вставить путь».");
                return;
            }

            SetStatus("Загрузка GLB…");
            (bool success, string error) = await modelLoader.LoadAsync(path, visualModelRoot);

            if (!success)
            {
                SetStatus("Ошибка загрузки: " + error);
                return;
            }

            draft.sourceModelPath = path;
            SetModelPathDisplay(path);

            if (editorPlaceholder != null)
                editorPlaceholder.SetActive(false);

            selectedModelNode = modelLoader.LoadedRoot;
            ApplyModelTransform();
            UpdateDimensionsFromLoadedModel();
            FramePreview();
            RefreshSelectedNodeText();

            SetStatus("GLB загружен. Можно кликать по частям модели в preview и назначать их роторам.");
        }

        private void ApplyModelTransform()
        {
            if (inputModelScale != null && TryNumber(inputModelScale.text, out double scale))
                draft.modelScaleMetersPerUnit = Math.Max(1e-9, scale);

            if (dropdownForwardAxis != null && dropdownForwardAxis.options.Count > 0)
                draft.visualForwardAxis = dropdownForwardAxis.options[dropdownForwardAxis.value].text.Trim();
            if (dropdownUpAxis != null && dropdownUpAxis.options.Count > 0)
                draft.visualUpAxis = dropdownUpAxis.options[dropdownUpAxis.value].text.Trim();

            if (modelLoader == null || modelLoader.LoadedRoot == null)
                return;

            Vector3 forward = AxisVector(draft.visualForwardAxis);
            Vector3 up = AxisVector(draft.visualUpAxis);

            if (Vector3.Cross(forward, up).sqrMagnitude < 1e-6f)
            {
                SetStatus("Forward Axis и Up Axis не могут быть параллельны.");
                return;
            }

            modelLoader.LoadedRoot.localRotation = Quaternion.Inverse(Quaternion.LookRotation(forward, up));
            modelLoader.LoadedRoot.localScale = Vector3.one * (float)draft.modelScaleMetersPerUnit;

            UpdateDimensionsFromLoadedModel();
            FramePreview();
        }

        private void UpdateDimensionsFromLoadedModel()
        {
            if (modelLoader.LoadedRoot == null) return;
            if (!RuntimeGltfModelLoader.TryGetBoundsInFrame(modelLoader.LoadedRoot, physicalMarkerRoot, out Bounds bounds))
                return;

            draft.dimensionsM = OptionalVector.From(bounds.size);
            Write(inputDimensionX, bounds.size.x);
            Write(inputDimensionY, bounds.size.y);
            Write(inputDimensionZ, bounds.size.z);

            if (textDimensions != null)
                textDimensions.text = $"{bounds.size.x:F3} × {bounds.size.y:F3} × {bounds.size.z:F3} m";
        }

        private void CreateBoxColliderBinding()
        {
            if (modelLoader.LoadedRoot == null)
            {
                SetStatus("Сначала загрузите модель.");
                return;
            }

            if (!RuntimeGltfModelLoader.TryGetBoundsInFrame(modelLoader.LoadedRoot, physicalMarkerRoot, out Bounds bounds))
                return;

            bindings.colliders.Clear();
            bindings.colliders.Add(new ColliderBinding
            {
                nodePath = "",
                type = "Box",
                centerLocalM = new[] { (double)bounds.center.x, (double)bounds.center.y, (double)bounds.center.z },
                sizeM = new[] { (double)bounds.size.x, (double)bounds.size.y, (double)bounds.size.z }
            });

            SetStatus("Box collider binding рассчитан по renderer bounds модели.");
        }

        private void EstimateComFromModel()
        {
            if (modelLoader.LoadedRoot == null)
            {
                SetStatus("Сначала загрузите модель.");
                return;
            }

            if (!RuntimeGltfModelLoader.TryGetBoundsInFrame(modelLoader.LoadedRoot, physicalMarkerRoot, out Bounds bounds))
                return;

            draft.centerOfMassLocalM = OptionalVector.From(bounds.center);
            Write(inputComX, bounds.center.x);
            Write(inputComY, bounds.center.y);
            Write(inputComZ, bounds.center.z);
            SetStatus("COM = геометрический центр модели. Это ОЦЕНКА, а не измеренное значение.");
        }

        private void ReadDimensions()
        {
            if (TryVector(inputDimensionX, inputDimensionY, inputDimensionZ, out Vector3 value))
                draft.dimensionsM = OptionalVector.From(value);
            else
                draft.dimensionsM.hasValue = false;
        }

        private void ReadCom()
        {
            if (TryVector(inputComX, inputComY, inputComZ, out Vector3 value))
                draft.centerOfMassLocalM = OptionalVector.From(value);
            else
                draft.centerOfMassLocalM.hasValue = false;
        }

        private void ReadManualInertia()
        {
            if (draft.inertiaMode != "ManualPrincipal") return;

            if (TryVector(inputI1, inputI2, inputI3, out Vector3 principal))
                draft.principalMomentsKgM2 = OptionalVector.From(principal);
            else
                draft.principalMomentsKgM2.hasValue = false;

            if (TryNumber(Text(inputQx), out double qx)
                && TryNumber(Text(inputQy), out double qy)
                && TryNumber(Text(inputQz), out double qz)
                && TryNumber(Text(inputQw), out double qw))
            {
                Quaternion q = new((float)qx, (float)qy, (float)qz, (float)qw);
                float magnitudeSquared = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
                if (magnitudeSquared > 1e-8f)
                {
                    q = Quaternion.Normalize(q);
                    draft.hasPrincipalAxesRotation = true;
                    draft.principalAxesRotationXyzw = new[] { (double)q.x, (double)q.y, (double)q.z, (double)q.w };
                }
            }
            else
            {
                draft.hasPrincipalAxesRotation = false;
            }
        }

        private void ReadRotorPosition()
        {
            if (!TryVector(inputPosX, inputPosY, inputPosZ, out Vector3 value))
            {
                CurrentRotor().positionLocalM.hasValue = false;
                return;
            }

            CurrentRotor().positionLocalM = OptionalVector.From(value);
            UpdateMarkerFromRotor(selectedRotorIndex);
        }

        private void ReadRotorAxis()
        {
            if (!TryVector(inputAxisX, inputAxisY, inputAxisZ, out Vector3 axis) || axis.sqrMagnitude < 1e-10f)
                return;

            axis.Normalize();
            CurrentRotor().thrustAxisLocal = new[] { (double)axis.x, (double)axis.y, (double)axis.z };
            Write(inputAxisX, axis.x);
            Write(inputAxisY, axis.y);
            Write(inputAxisZ, axis.z);
        }

        private void UseSelectedNodeAxis()
        {
            if (selectedModelNode == null)
            {
                SetStatus("Кликните по нужной подмодели в PreviewRawImage.");
                return;
            }

            Vector3 axis = physicalMarkerRoot.InverseTransformDirection(selectedModelNode.up).normalized;
            CurrentRotor().thrustAxisLocal = new[] { (double)axis.x, (double)axis.y, (double)axis.z };
            RefreshRotorUi();
        }

        private void AssignSelectedNodeAsVisualPropeller()
        {
            if (selectedModelNode == null || modelLoader.LoadedRoot == null)
            {
                SetStatus("Кликните по нужной подмодели в preview.");
                return;
            }

            string path = RuntimeGltfModelLoader.PathFrom(modelLoader.LoadedRoot, selectedModelNode);
            bindings.GetOrCreate(CurrentRotor().rotorId).nodePath = path;
            if (textVisualNode != null) textVisualNode.text = path;
            SetStatus(CurrentRotor().rotorId + ": визуальный узел назначен.");
        }

        private void MoveRotorToSelectedNodePivot()
        {
            if (selectedModelNode == null)
            {
                SetStatus("Кликните по нужной подмодели в preview.");
                return;
            }

            Vector3 local = physicalMarkerRoot.InverseTransformPoint(selectedModelNode.position);
            CurrentRotor().positionLocalM = OptionalVector.From(local);
            UpdateMarkerFromRotor(selectedRotorIndex);
            RefreshRotorUi();
        }

        private void PreviewPointerDown(PointerEventData eventData)
        {
            previousPointerPosition = eventData.position;

            if (eventData.button == PointerEventData.InputButton.Right && previewView == PreviewView.Perspective)
            {
                orbiting = true;
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            int marker = FindNearestMarker(eventData.position, 34f);
            if (marker >= 0)
            {
                draggedMarkerIndex = marker;
                draggingMarker = true;
                SelectPage(pageRotor);
                SelectRotor(marker);
                SetupDragPlane(rotorMarkers[marker].position);
                return;
            }

            SelectModelNodeAtScreenPoint(eventData.position);
        }

        private void PreviewDrag(PointerEventData eventData)
        {
            if (orbiting && previewView == PreviewView.Perspective)
            {
                Vector2 delta = eventData.position - previousPointerPosition;
                previousPointerPosition = eventData.position;
                orbitYaw += delta.x * 0.22f;
                orbitPitch = Mathf.Clamp(orbitPitch - delta.y * 0.22f, -80f, 80f);
                UpdatePerspectiveCamera();
                return;
            }

            if (!draggingMarker || draggedMarkerIndex < 0 || draggedMarkerIndex >= rotorMarkers.Count)
                return;

            Ray ray = PreviewRay(eventData.position);
            if (!dragPlane.Raycast(ray, out float distance))
                return;

            Vector3 candidateWorld = ray.GetPoint(distance);
            Vector3 candidateLocal = physicalMarkerRoot.InverseTransformPoint(candidateWorld);
            ConfiguratorRotor rotor = draft.rotors[draggedMarkerIndex];
            Vector3 old = rotor.positionLocalM.hasValue ? rotor.positionLocalM.ToUnity() : Vector3.zero;

            switch (previewView)
            {
                case PreviewView.Front: candidateLocal.z = old.z; break;
                case PreviewView.Side: candidateLocal.x = old.x; break;
                default: candidateLocal.y = old.y; break;
            }

            rotor.positionLocalM = OptionalVector.From(candidateLocal);
            rotorMarkers[draggedMarkerIndex].localPosition = candidateLocal;
            RefreshRotorPositionFieldsOnly();
        }

        private void PreviewPointerUp(PointerEventData eventData)
        {
            draggingMarker = false;
            draggedMarkerIndex = -1;
            orbiting = false;
        }

        private void PreviewScroll(PointerEventData eventData)
        {
            float direction = Mathf.Sign(eventData.scrollDelta.y);

            if (previewCamera.orthographic)
                previewCamera.orthographicSize = Mathf.Clamp(previewCamera.orthographicSize * (direction > 0 ? 1.12f : 0.88f), 0.03f, 1000f);
            else
            {
                previewDistance = Mathf.Clamp(previewDistance * (direction > 0 ? 1.12f : 0.88f), 0.05f, 1000f);
                UpdatePerspectiveCamera();
            }
        }

        private int FindNearestMarker(Vector2 screenPosition, float maxPixels)
        {
            if (rotorMarkers.Count == 0) return -1;
            if (!TryScreenToPreviewUv(screenPosition, out Vector2 pointerUv)) return -1;

            int best = -1;
            float bestPixels = maxPixels;
            Rect rect = ((RectTransform)previewRawImage.transform).rect;
            float width = Mathf.Max(1f, rect.width);
            float height = Mathf.Max(1f, rect.height);

            for (int i = 0; i < rotorMarkers.Count; i++)
            {
                Vector3 viewport = previewCamera.WorldToViewportPoint(rotorMarkers[i].position);
                if (viewport.z <= 0f) continue;

                float dx = (viewport.x - pointerUv.x) * width;
                float dy = (viewport.y - pointerUv.y) * height;
                float pixels = Mathf.Sqrt(dx * dx + dy * dy);

                if (pixels < bestPixels)
                {
                    bestPixels = pixels;
                    best = i;
                }
            }

            return best;
        }

        private void SelectModelNodeAtScreenPoint(Vector2 screenPosition)
        {
            if (modelLoader.LoadedRoot == null) return;

            Ray ray = PreviewRay(screenPosition);
            Renderer[] renderers = modelLoader.LoadedRoot.GetComponentsInChildren<Renderer>(true);

            Renderer best = null;
            float nearest = float.PositiveInfinity;

            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled) continue;
                if (!renderer.bounds.IntersectRay(ray, out float distance)) continue;
                if (distance < nearest)
                {
                    nearest = distance;
                    best = renderer;
                }
            }

            if (best == null)
            {
                selectedModelNode = null;
                RefreshSelectedNodeText();
                return;
            }

            selectedModelNode = best.transform;
            RefreshSelectedNodeText();
            SetStatus("Выбран узел: " + SelectedNodePath());
        }

        private Ray PreviewRay(Vector2 screenPosition)
        {
            if (!TryScreenToPreviewUv(screenPosition, out Vector2 uv))
                return previewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            return previewCamera.ViewportPointToRay(new Vector3(uv.x, uv.y, 0f));
        }

        private bool TryScreenToPreviewUv(Vector2 screenPosition, out Vector2 uv)
        {
            uv = Vector2.zero;
            RectTransform rectTransform = previewRawImage.rectTransform;
            Canvas canvas = previewRawImage.canvas;
            Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPosition, uiCamera, out Vector2 local))
                return false;

            Rect rect = rectTransform.rect;
            if (!rect.Contains(local)) return false;

            uv = new Vector2(
                Mathf.InverseLerp(rect.xMin, rect.xMax, local.x),
                Mathf.InverseLerp(rect.yMin, rect.yMax, local.y));
            return true;
        }

        private void SetupDragPlane(Vector3 markerWorld)
        {
            Vector3 normal = previewView switch
            {
                PreviewView.Front => Vector3.forward,
                PreviewView.Side => Vector3.right,
                _ => Vector3.up
            };
            dragPlane = new Plane(normal, markerWorld);
        }

        private void RefreshSelectedNodeText()
        {
            if (textSelectedNode != null)
                textSelectedNode.text = selectedModelNode == null ? "—" : SelectedNodePath();
        }

        private string SelectedNodePath()
        {
            if (selectedModelNode == null || modelLoader.LoadedRoot == null) return "—";
            string path = RuntimeGltfModelLoader.PathFrom(modelLoader.LoadedRoot, selectedModelNode);
            return string.IsNullOrWhiteSpace(path) ? "<root>" : path;
        }

        private void SelectRotor(int index)
        {
            if (draft.rotors.Count == 0) return;
            selectedRotorIndex = Mathf.Clamp(index, 0, draft.rotors.Count - 1);
            draft.selectedRotorIndex = selectedRotorIndex;
            RefreshRotorUi();
            HighlightSelectedMarker();
        }

        private ConfiguratorRotor CurrentRotor()
        {
            selectedRotorIndex = Mathf.Clamp(selectedRotorIndex, 0, draft.rotors.Count - 1);
            return draft.rotors[selectedRotorIndex];
        }

        private void RefreshAllUi()
        {
            SetModelPathDisplay(draft.sourceModelPath);
            Write(inputModelScale, draft.modelScaleMetersPerUnit);
            if (inputProfileId != null) inputProfileId.SetTextWithoutNotify(draft.profileId);
            if (inputName != null) inputName.SetTextWithoutNotify(draft.name);
            if (inputManufacturer != null) inputManufacturer.SetTextWithoutNotify(draft.manufacturer);
            if (inputModelName != null) inputModelName.SetTextWithoutNotify(draft.model);
            WriteOptional(inputMassKg, draft.massKg);

            if (rotorCountText != null)
                rotorCountText.text = draft.rotors.Count + (draft.rotors.Count == 1 ? " ротор" : " ротора");

            if (manualInertiaPanel != null)
                manualInertiaPanel.SetActive(draft.inertiaMode == "ManualPrincipal");

            RefreshRotorUi();
        }

        private void RefreshRotorUi()
        {
            if (draft.rotors.Count == 0) return;

            ConfiguratorRotor rotor = CurrentRotor();

            if (textRotorName != null)
                textRotorName.text = "РОТОР " + rotor.rotorId;

            RefreshRotorPositionFieldsOnly();

            double[] axis = rotor.thrustAxisLocal ?? new[] { 0.0, 1.0, 0.0 };
            Write(inputAxisX, axis[0]);
            Write(inputAxisY, axis[1]);
            Write(inputAxisZ, axis[2]);

            WriteOptional(inputMinRpm, rotor.motor.minRpm);
            WriteOptional(inputIdleRpm, rotor.motor.idleRpm);
            WriteOptional(inputMaxRpm, rotor.motor.maxRpm);
            WriteOptionalScaled(inputResponseUpMs, rotor.motor.responseTimeUpS, 1000.0);
            WriteOptionalScaled(inputResponseDownMs, rotor.motor.responseTimeDownS, 1000.0);
            WriteOptionalScaled(inputDiameterMm, rotor.propeller.diameterM, 1000.0);
            WriteOptionalScaled(inputPitchMm, rotor.propeller.pitchM, 1000.0);

            if (inputBladeCount != null)
                inputBladeCount.SetTextWithoutNotify(rotor.propeller.bladeCount.ToString(invariant));

            WriteOptional(inputKThrust, rotor.performance.kThrustNPerRadPerSecSquared);
            WriteOptional(inputKTorque, rotor.performance.kTorqueNmPerRadPerSecSquared);
            WriteOptional(inputReferenceDensity, rotor.performance.referenceAirDensityKgM3);

            if (dropdownPerformanceModel != null)
            {
                int index = dropdownPerformanceModel.options.FindIndex(x =>
                    string.Equals(x.text.Trim(), rotor.performance.model, StringComparison.OrdinalIgnoreCase));
                if (index >= 0) dropdownPerformanceModel.SetValueWithoutNotify(index);
            }

            string nodePath = bindings.GetOrCreate(rotor.rotorId).nodePath;
            if (textVisualNode != null)
                textVisualNode.text = string.IsNullOrWhiteSpace(nodePath) ? "—" : nodePath;
        }

        private void RefreshRotorPositionFieldsOnly()
        {
            ConfiguratorRotor rotor = CurrentRotor();
            if (!rotor.positionLocalM.hasValue) return;
            Vector3 p = rotor.positionLocalM.ToUnity();
            Write(inputPosX, p.x);
            Write(inputPosY, p.y);
            Write(inputPosZ, p.z);
        }

        private void SyncDraftPositionsFromSceneMarkers()
        {
            for (int i = 0; i < Mathf.Min(rotorMarkers.Count, draft.rotors.Count); i++)
                draft.rotors[i].positionLocalM = OptionalVector.From(rotorMarkers[i].localPosition);
        }

        private void UpdateMarkerFromRotor(int index)
        {
            if (index < 0 || index >= rotorMarkers.Count || index >= draft.rotors.Count) return;
            if (draft.rotors[index].positionLocalM.hasValue)
                rotorMarkers[index].localPosition = draft.rotors[index].positionLocalM.ToUnity();
        }

        private void HighlightSelectedMarker()
        {
            for (int i = 0; i < rotorMarkers.Count; i++)
                rotorMarkers[i].localScale = Vector3.one * (i == selectedRotorIndex ? 1.25f : 1f);
        }

        private void SelectPage(GameObject page)
        {
            if (pageModel != null) pageModel.SetActive(page == pageModel);
            if (pageMass != null) pageMass.SetActive(page == pageMass);
            if (pageRotor != null) pageRotor.SetActive(page == pageRotor);
        }

        private void ApplyPreviewView(PreviewView view)
        {
            previewView = view;

            if (view == PreviewView.Perspective)
            {
                previewCamera.orthographic = false;
                UpdatePerspectiveCamera();
                return;
            }

            previewCamera.orthographic = true;

            if (view == PreviewView.Top)
            {
                previewCamera.transform.position = previewTarget + Vector3.up * previewDistance;
                previewCamera.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            }
            else if (view == PreviewView.Front)
            {
                previewCamera.transform.position = previewTarget + Vector3.back * previewDistance;
                previewCamera.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            }
            else
            {
                previewCamera.transform.position = previewTarget + Vector3.right * previewDistance;
                previewCamera.transform.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
            }
        }

        private void UpdatePerspectiveCamera()
        {
            Quaternion orbit = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
            Vector3 direction = orbit * Vector3.forward;
            previewCamera.transform.position = previewTarget - direction * previewDistance;
            previewCamera.transform.LookAt(previewTarget, Vector3.up);
        }

        private void FramePreview()
        {
            Bounds bounds = default;
            bool hasBounds = modelLoader != null && modelLoader.LoadedRoot != null
                && RuntimeGltfModelLoader.TryGetWorldBounds(modelLoader.LoadedRoot, out bounds);

            if (!hasBounds)
            {
                Renderer[] renderers = editorPlaceholder != null
                    ? editorPlaceholder.GetComponentsInChildren<Renderer>(true)
                    : Array.Empty<Renderer>();

                if (renderers.Length > 0)
                {
                    bounds = renderers[0].bounds;
                    for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                    hasBounds = true;
                }
            }

            if (!hasBounds)
                bounds = new Bounds(Vector3.zero, Vector3.one);

            previewTarget = bounds.center;
            float radius = Mathf.Max(0.2f, bounds.extents.magnitude);
            previewDistance = Mathf.Max(0.8f, radius * 3.0f);

            if (previewCamera.orthographic)
            {
                float horizontal = Mathf.Max(bounds.extents.x, bounds.extents.z);
                float vertical = Mathf.Max(bounds.extents.y, bounds.extents.z);
                previewCamera.orthographicSize = Mathf.Max(0.15f,
                    previewView == PreviewView.Top ? horizontal * 1.35f : vertical * 1.35f);
            }

            ApplyPreviewView(previewView);
        }

        private void ValidateProfile()
        {
            List<ConfiguratorIssue> issues = DroneConfiguratorProfileAdapter.Validate(draft, out _);
            int errors = issues.Count(x => string.Equals(x.Severity, "Error", StringComparison.OrdinalIgnoreCase));
            int warnings = issues.Count(x => string.Equals(x.Severity, "Warning", StringComparison.OrdinalIgnoreCase));

            foreach (ConfiguratorIssue issue in issues)
                Debug.Log("[DroneConfigurator validation] " + issue);

            SetStatus(errors == 0
                ? $"Профиль валиден. Предупреждений: {warnings}."
                : $"Ошибок: {errors}, предупреждений: {warnings}. Подробности — Console.");
        }

        private void SaveDraft()
        {
            try
            {
                manifest.profileId = draft.profileId;
                DroneConfiguratorStorage.SaveDraft(draft, bindings, manifest);
                SetStatus("Черновик сохранён: " + DroneConfiguratorStorage.ProfileDirectory(draft.profileId));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                SetStatus("Ошибка сохранения черновика: " + ex.Message);
            }
        }

        private void SaveReadyProfile()
        {
            try
            {
                List<ConfiguratorIssue> issues = DroneConfiguratorProfileAdapter.Validate(draft, out string strictJson);
                if (DroneConfiguratorProfileAdapter.HasErrors(issues))
                {
                    SetStatus("profile.json не сохранён: исправьте ошибки проверки.");
                    foreach (ConfiguratorIssue issue in issues) Debug.Log("[DroneConfigurator validation] " + issue);
                    return;
                }

                manifest.profileId = draft.profileId;
                DroneConfiguratorStorage.SaveReadyProfile(draft, bindings, manifest, strictJson);
                SetStatus("Профиль сохранён: " + DroneConfiguratorStorage.ProfileDirectory(draft.profileId));
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                SetStatus("Ошибка сохранения профиля: " + ex.Message);
            }
        }

        private void Back()
        {
            if (!string.IsNullOrWhiteSpace(backScene) && Application.CanStreamedLevelBeLoaded(backScene))
                SceneManager.LoadScene(backScene);
            else
                SetStatus("Сцена возврата не найдена в Build Profiles: " + backScene);
        }

        private void CacheRotorButtons()
        {
            rotorButtons.Clear();
            Transform root = FindTransform("RotorList");
            if (root == null) return;

            rotorButtons.AddRange(root.GetComponentsInChildren<Button>(true)
                .Where(x => x.name.StartsWith("Btn_Rotor_M", StringComparison.Ordinal))
                .OrderBy(x => ParseTrailingNumber(x.name)));
        }

        private void CacheRotorMarkers()
        {
            rotorMarkers.Clear();
            if (physicalMarkerRoot == null) return;

            rotorMarkers.AddRange(physicalMarkerRoot.GetComponentsInChildren<Transform>(true)
                .Where(x => x != physicalMarkerRoot && x.name.StartsWith("Marker_M", StringComparison.Ordinal))
                .OrderBy(x => ParseTrailingNumber(x.name)));
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
            Debug.Log("[DroneConfiguratorCanvas] " + message);
        }

        private static void Bind(Button button, Action action)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action());
        }

        private static void BindEnd(TMP_InputField input, Action<string> action)
        {
            if (input == null) return;
            input.onEndEdit.RemoveAllListeners();
            input.onEndEdit.AddListener(value => action(value));
        }

        private void BindOptionalNumber(TMP_InputField input, Action<OptionalNumber> setter)
        {
            BindEnd(input, value =>
            {
                if (string.IsNullOrWhiteSpace(value)) { setter(OptionalNumber.Missing()); return; }
                if (TryNumber(value, out double parsed)) setter(OptionalNumber.From(parsed));
            });
        }

        private void BindScaledOptionalNumber(TMP_InputField input, double factor, Action<OptionalNumber> setter)
        {
            BindEnd(input, value =>
            {
                if (string.IsNullOrWhiteSpace(value)) { setter(OptionalNumber.Missing()); return; }
                if (TryNumber(value, out double parsed)) setter(OptionalNumber.From(parsed * factor));
            });
        }

        private bool TryVector(TMP_InputField x, TMP_InputField y, TMP_InputField z, out Vector3 value)
        {
            value = Vector3.zero;
            if (!TryNumber(Text(x), out double vx) || !TryNumber(Text(y), out double vy) || !TryNumber(Text(z), out double vz))
                return false;

            value = new Vector3((float)vx, (float)vy, (float)vz);
            return true;
        }

        private bool TryNumber(string text, out double value)
        {
            text = (text ?? "").Trim().Replace(',', '.');
            return double.TryParse(text, NumberStyles.Float, invariant, out value)
                   && !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static string Text(TMP_InputField input) => input != null ? input.text : "";

        private void Write(TMP_InputField input, double value)
        {
            if (input != null) input.SetTextWithoutNotify(value.ToString("0.##########", invariant));
        }

        private void WriteOptional(TMP_InputField input, OptionalNumber value)
        {
            if (input != null)
                input.SetTextWithoutNotify(value != null && value.hasValue ? value.value.ToString("0.##########", invariant) : "");
        }

        private void WriteOptionalScaled(TMP_InputField input, OptionalNumber value, double multiplier)
        {
            if (input != null)
                input.SetTextWithoutNotify(value != null && value.hasValue ? (value.value * multiplier).ToString("0.##########", invariant) : "");
        }

        private static Vector3 AxisVector(string axis)
        {
            return (axis ?? "").Trim() switch
            {
                "+X" => Vector3.right,
                "-X" => Vector3.left,
                "+Y" => Vector3.up,
                "-Y" => Vector3.down,
                "-Z" => Vector3.back,
                _ => Vector3.forward
            };
        }

        private static int ParseTrailingNumber(string value)
        {
            int index = value.Length - 1;
            while (index >= 0 && char.IsDigit(value[index])) index--;
            return int.TryParse(value[(index + 1)..], out int result) ? result : int.MaxValue;
        }

        private GameObject FindObject(string objectName)
        {
            Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Transform found = all.FirstOrDefault(x => x.name == objectName);
            return found != null ? found.gameObject : null;
        }

        private Transform FindTransform(string name) => FindObject(name)?.transform;
        private T FindComponent<T>(string name) where T : Component => FindObject(name)?.GetComponent<T>();
        private Button FindButton(string name) => FindComponent<Button>(name);
        private TMP_InputField FindInput(string name) => FindComponent<TMP_InputField>(name);
        private TMP_Dropdown FindDropdown(string name) => FindComponent<TMP_Dropdown>(name);
        private TMP_Text FindTmpText(string name) => FindComponent<TMP_Text>(name);
    }
}
