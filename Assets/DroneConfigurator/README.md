# DroneLab Drone Configurator

Branch: `feature/drone-loader-configurator`

Runtime multirotor configurator for Unity 6 HDRP.

## Current scene

The active hand-authored configurator scene is:

`Assets/Scenes/DroneConfigurator 1.unity`

It uses standard uGUI / TextMeshPro, not UI Toolkit. Stable buttons, panels and inputs are authored manually in the scene so they can be moved and styled in the Unity Editor.

Runtime code:

```text
Assets/DroneConfigurator/Runtime/
  DroneConfiguratorCanvasController.cs
  DroneConfiguratorData.cs
  RuntimeGltfModelLoader.cs
```

## Implemented path

- Runtime GLB/glTF import through glTFast.
- RenderTexture preview with isolated HDRP exposure.
- Top / front / side / 3D views and framing.
- Visual model node selection.
- Physical rotor markers independent from visual propeller nodes.
- Rotor placement and thrust-axis visualization.
- Model scale and visual forward/up axes.
- Mass, dimensions, center of mass.
- AutoBox / ManualPrincipal inertia.
- Rotor position, thrust axis and CW/CCW.
- min / idle / max RPM.
- FirstOrder response up/down constants.
- Propeller diameter, pitch and blade count.
- OmegaSquared performance inputs: kT, kQ and reference air density.
- AxisApproximation body-drag inputs: Cd XYZ and reference area XYZ.
- Explicit battery mode for the current MVP: None.
- Explicit physics fidelity: Basic / Advanced.
- All 11 physics module flags.
- Draft/package persistence under:
  `Application.persistentDataPath/DroneProfiles/<profileId>/`.
- Ready `profile.json` is validated through the existing DroneLab `ProfileLoader.Load`.

## UI semantics

Header buttons `Базовый / Расширенный` change only UI presentation. They do not change physical fidelity.

Physical fidelity is edited separately through `Dropdown_Fidelity` on `Page_Modules`.

The current complete MVP intentionally exposes only branches for which the hand-authored UI has all required inputs:

- performance: `OmegaSquared`;
- body aerodynamics: `AxisApproximation`;
- battery: `None`.

Other schema branches remain supported by the physics contract, but must not be offered as editable choices until their required fields are present in the Canvas.

## Table-driven performance and battery editors

The old bottom graph panel is no longer used. Selection happens in the left navigation and editing happens in the right inspector.

Propeller performance:
- `Ω² Коэффициенты` -> existing `Page_PerformanceOmegaSquared`;
- `RPM-таблица` -> runtime `Page_PerformanceRpmTable`;
- `Карта характеристик` -> runtime `Page_PerformanceMap`.

Battery:
- `Параметры батареи` -> `Page_Battery`;
- `Кривая OCV` -> runtime `Page_BatteryOcvCurve`.

Variable-length rows are intentionally generated at runtime by `DroneConfiguratorAdvancedEditors`, while the navigation and stable inspector shell remain scene-authored.

`RpmTable` edits RPM / thrust / torque and optional current. The required zero-RPM origin is seeded automatically, while measured non-zero values remain empty until entered by the user.

`PerformanceMap` edits RPM / advance ratio J / Ct / Cq rows. Validation still requires a complete rectangular RPM x J grid, at least two J values and a J=0 column.

The OCV editor uses SOC in percent in the UI and exports SOC as 0..1. Endpoint SOC values 0% and 100% are seeded structurally; voltage is never invented.

For `OmegaSquared`, kT/kQ/reference-density remain direct numeric inputs; no graph is required.

## Important separation

- `profile.json` — strict DroneLab physics contract.
- `model.glb` — visual geometry.
- `asset-bindings.json` — visual node bindings and collider draft.
- `package-manifest.json` — package paths/version.
- `draft.json` — incomplete UI state.

Physics root convention: X right, Y up, Z forward. Physics values are stored in SI units.

## Quick test

1. Checkout `feature/drone-loader-configurator`.
2. Open `Assets/Scenes/DroneConfigurator 1.unity`.
3. Enter Play Mode.
4. Load a GLB.
5. Confirm model scale and axes.
6. Fill mass, dimensions and COM.
7. Configure M1-M4.
8. Set OmegaSquared kT/kQ/reference density.
9. Leave battery mode at None for the complete MVP.
10. Select physics fidelity and module flags explicitly.
11. If bodyDrag is enabled, fill Cd XYZ and reference-area XYZ.
12. Press `Проверить профиль`.
13. `Сохранить черновик` may save incomplete data.
14. `Сохранить профиль` only writes a ready profile when validation has no Error issues.
