# DroneLab Drone Configurator UI

Standalone distribution package containing **only the drone configurator UI and its runtime logic**.

## Included

- editable hand-authored uGUI configurator scene;
- left navigation, 3D preview, right inspector and footer controls;
- GLB/glTF runtime model loading;
- rotor position / thrust-axis / CW-CCW editing;
- motor and propeller inputs;
- OmegaSquared, RPM-table and PerformanceMap editors;
- mass, dimensions, COM and inertia UI;
- body-aerodynamics inputs;
- physics module toggles;
- battery/OCV UI;
- Russian one-at-a-time validation messages;
- draft/profile/asset-bindings/package-manifest save pipeline.

Not included: main menu, weather/scenario UI, flight HUD, sensors, autopilot, telemetry UI, or the physics implementation itself.

## Required project dependencies

The package references the existing `DroneLab.Physics` assembly. Use DroneLab Physics 0.3.4 or the matching current source assembly.

The project must also have glTFast installed. The current DroneLab project uses this entry in `Packages/manifest.json`:

```json
"com.unity.cloud.gltfast": "https://github.com/Unity-Technologies/com.unity.cloud.gltfast.git?path=/Packages/com.unity.cloud.gltfast#fdb580527a41fd7006916030768ba74d3728f987"
```

## Install

From Unity Package Manager:

1. **+ -> Add package from disk**
2. Select `Distribution/Packages/com.dronelab.configurator/package.json`
3. Import the sample **Configurator UI Scene**
4. Open the imported `DroneConfigurator.unity`

The sample is copied under `Assets/Samples/` and can then be edited normally.

### Important for the current DroneLab branch

This distribution package preserves the GUIDs of the current scene/scripts so all prefab and scene links stay intact.
Do **not** install it alongside the legacy `Assets/DroneConfigurator` + `Assets/Scenes/DroneConfigurator 1.unity` copy.
For migration, keep the distribution package, remove/move the legacy configurator assets, then install/import the package sample.

## Runtime files

- `DroneConfiguratorCanvasController.cs`
- `DroneConfiguratorAdvancedEditors.cs`
- `DroneConfiguratorData.cs`
- `RuntimeGltfModelLoader.cs`

The old UI Toolkit `DroneConfiguratorController.cs` is intentionally **not** included; the package is based on the current hand-authored uGUI implementation.

## User-created drone data

The configurator saves the drone itself as:

- `profile.json`
- `draft.json`
- `model.glb`
- `asset-bindings.json`
- `package-manifest.json`

Those files are user data and are not part of this UI package.

## Встроенный экран DroneLab

В этом репозитории пакет подключён через `Packages/manifest.json`. Новый UI Toolkit-экран находится в `Assets/DroneUI/Runtime/Configurator` и открывается из главного меню. Исходная uGUI-сцена сохранена как Sample; импортировать её для работы нового меню не нужно. Загрузчик обновлён: корректная замена/освобождение glTF-ресурсов, mipmaps и bounds в координатах заданного frame. Подробности — `Docs/UI/DRONE_CONFIGURATOR.md` в корне проекта.
