# DroneLab Drone Configurator

Branch: `feature/drone-loader-configurator`

Runtime multirotor configurator for Unity 6 HDRP.

## What is implemented

- Dedicated `Assets/Scenes/DroneConfigurator.unity`.
- Main-menu button opens the configurator scene.
- Runtime GLB/glTF import through Unity glTFast.
- Model hierarchy browser and node selection.
- Physical rotor markers independent from visual propeller nodes.
- Top/front/side/3D views.
- Drag rotor/COM markers in the preview.
- Arbitrary rotor count for editing/saving.
- Physical fields for:
  - mass, dimensions, COM;
  - AutoBox / ManualPrincipal inertia;
  - rotor position, axis, CW/CCW;
  - min/idle/max RPM;
  - first-order up/down response constants;
  - propeller diameter, pitch, blade count;
  - OmegaSquared or Ct/Cq performance;
  - axis body drag;
  - battery mode;
  - all 11 physics module flags.
- Per-field provenance source selector.
- Visual rotor bindings stored separately from strict physics JSON.
- Draft/package persistence under:
  `Application.persistentDataPath/DroneProfiles/<profileId>/`.
- Ready `profile.json` is generated against contract 1.0.0.
- Validation uses the existing DroneLab `ProfileLoader.Load` with current drone/environment schemas.
- Validation errors are clickable and navigate to the relevant section.
- Non-quad rotor counts can be edited and saved, but validation shows a compatibility warning for the current quad controller.

## Files

```text
Assets/DroneConfigurator/
  Runtime/
    DroneConfiguratorData.cs
    DroneConfiguratorController.cs
    RuntimeGltfModelLoader.cs

Assets/DroneUI/Resources/DroneLab/
  Configurator.uss

Assets/Scenes/
  DroneConfigurator.unity
```

## Quick test

1. Checkout this branch.
2. Open Unity 6 and allow Package Manager to resolve glTFast.
3. Open `Assets/Scenes/DroneConfigurator.unity`.
4. Play.
5. Paste an absolute path to a `.glb` and press LOAD.
6. Confirm scale/axes.
7. Fill mass, dimensions and COM.
8. Open Propulsion and configure the rotors.
9. Assign visual propeller nodes from the hierarchy if desired.
10. Press VALIDATE.
11. SAVE DRAFT works even when incomplete.
12. SAVE PROFILE only writes a ready profile when `ProfileLoader` reports no errors.

## Important separation

- `profile.json` — strict DroneLab physics contract.
- `model.glb` — visual geometry.
- `asset-bindings.json` — visual node bindings and collider draft.
- `package-manifest.json` — package paths/version.
- `draft.json` — incomplete UI state and not-yet-valid values.

The physics root convention remains X right, Y up, Z forward, SI units.
