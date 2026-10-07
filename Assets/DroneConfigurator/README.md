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

## Graphs and table-driven parameters

The lower graph area is currently only a visualization shell. It does not yet edit profile data.

The physics contract has branches where graph/table UI is appropriate:

- `RpmTable`: editable RPM -> thrust / torque rows, optional current;
- `PerformanceMap`: RPM x advance-ratio grid with Ct/Cq;
- battery OCV curve: SOC -> pack voltage.

For `OmegaSquared`, thrust and torque graphs are derived read-only plots from kT/kQ and RPM; the graph itself is not an independent input.

Current graph buttons can therefore remain display-only until RpmTable / PerformanceMap / OCV editors are implemented.

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
