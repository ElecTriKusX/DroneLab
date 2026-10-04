# Реестр параметров — контракт 1.0.0

Сгенерировано из `Tools/generate_physics_contract.py`. Все величины SI; RPM — об/мин; quaternion — [x,y,z,w]. `?` означает необязательное поле, а не автоматически реализованный эффект. Условные требования и доступность исполнения проверяются дополнительно в ProfileLoader.

## Metadata

| Поле | Тип / ограничения |
|---|---|
| `name` | string |
| `manufacturer?` | string |
| `model?` | string |
| `description?` | string |

## CoordinateSystem

| Поле | Тип / ограничения |
|---|---|
| `convention` | ['UnityLeftHandedYUpZForward'] |
| `lengthUnit` | ['m'] |
| `modelScaleMetersPerUnit` | number |

## PhysicsModulesProfile

| Поле | Тип / ограничения |
|---|---|
| `motorResponse` | boolean |
| `bodyDrag` | boolean |
| `windInteraction` | boolean |
| `groundEffect` | boolean |
| `rotorAerodynamics` | boolean |
| `bladeFlapping` | boolean |
| `inducedDrag` | boolean |
| `batteryDischarge` | boolean |
| `batteryVoltageSag` | boolean |
| `motorElectrical` | boolean |
| `gyroscopicRotorEffects` | boolean |

## PhysicsConfiguration

| Поле | Тип / ограничения |
|---|---|
| `fidelity` | ['Basic', 'Advanced'] |
| `modules` | PhysicsModulesProfile |

## InertiaProfile

| Поле | Тип / ограничения |
|---|---|
| `mode` | ['AutoBox', 'ManualPrincipal'] |
| `principalMomentsKgM2?` | array |
| `principalAxesRotationXyzw?` | array |

## MassProperties

| Поле | Тип / ограничения |
|---|---|
| `massKg` | number |
| `centerOfMassLocalM` | array |
| `dimensionsM` | array |
| `inertia` | InertiaProfile |

## EfficiencyPoint

| Поле | Тип / ограничения |
|---|---|
| `loadFraction` | number |
| `efficiency` | number |

## MotorElectricalProfile

| Поле | Тип / ограничения |
|---|---|
| `motorKvRpmPerVolt` | number |
| `motorResistanceOhm` | number |
| `noLoadCurrentA` | number |
| `maxCurrentA` | number |
| `maxPowerW?` | number |
| `motorEfficiency` | number |
| `efficiencyCurve?` | array |
| `escEfficiency` | number |
| `escMaxCurrentA` | number |

## MotorProfile

| Поле | Тип / ограничения |
|---|---|
| `minRpm` | number |
| `idleRpm` | number |
| `maxRpm` | number |
| `responseTimeUpS` | number |
| `responseTimeDownS` | number |
| `rotatingInertiaKgM2?` | number |
| `electrical?` | MotorElectricalProfile |

## PropellerProfile

| Поле | Тип / ограничения |
|---|---|
| `diameterM` | number |
| `pitchM` | number |
| `bladeCount` | integer |

## RpmPerformancePoint

| Поле | Тип / ограничения |
|---|---|
| `rpm` | number |
| `thrustN` | number |
| `torqueNm` | number |
| `currentA?` | number |

## PerformanceMapPoint

| Поле | Тип / ограничения |
|---|---|
| `rpm` | number |
| `advanceRatio` | number |
| `ct` | number |
| `cq` | number |
| `reynolds?` | number |

## RotorPerformanceProfile

| Поле | Тип / ограничения |
|---|---|
| `model` | ['OmegaSquared', 'RpmTable', 'CtCq', 'PerformanceMap'] |
| `kThrustNPerRadPerSecSquared?` | number |
| `kTorqueNmPerRadPerSecSquared?` | number |
| `referenceAirDensityKgM3?` | number |
| `ct?` | number |
| `cq?` | number |
| `rpmTable?` | array |
| `performanceMap?` | array |
| `outOfRangePolicy?` | ['Reject', 'Clamp'] |

## RotorGeometry

| Поле | Тип / ограничения |
|---|---|
| `positionLocalM` | array |
| `thrustAxisLocal` | array |
| `spinDirection` | ['CW', 'CCW'] |

## RotorAerodynamicsProfile

| Поле | Тип / ограничения |
|---|---|
| `rotorDragCoefficientKgPerRad` | number |
| `bladeFlappingCoefficient?` | number |
| `inducedDragCoefficient?` | number |

## RotorProfile

| Поле | Тип / ограничения |
|---|---|
| `rotorId` | string |
| `geometry` | RotorGeometry |
| `motor` | MotorProfile |
| `propeller` | PropellerProfile |
| `performance` | RotorPerformanceProfile |
| `advancedAerodynamics?` | RotorAerodynamicsProfile |

## ProjectedAreaSample

| Поле | Тип / ограничения |
|---|---|
| `directionLocal` | array |
| `areaM2` | number |

## ProjectedAreaProfile

| Поле | Тип / ограничения |
|---|---|
| `mode` | ['MeshDirectionalLUT', 'ManualDirectionalLUT', 'AxisApproximation'] |
| `samples?` | array |
| `referenceAreaM2?` | array |

## AeroSurfaceProfile

| Поле | Тип / ограничения |
|---|---|
| `surfaceId` | string |
| `positionLocalM` | array |
| `normalLocal` | array |
| `areaM2` | number |
| `dragCoefficient` | number |

## BodyAerodynamicsProfile

| Поле | Тип / ограничения |
|---|---|
| `model` | ['AxisApproximation', 'ProjectedArea', 'Surfaces'] |
| `dragCd?` | array |
| `referenceAreaM2?` | array |
| `dragCoefficient?` | number |
| `projectedArea?` | ProjectedAreaProfile |
| `dragApplicationPointLocalM?` | array |
| `surfaces?` | array |

## GroundEffectProfile

| Поле | Тип / ограничения |
|---|---|
| `coefficient` | number |
| `minHeightRadiusRatio` | number |
| `maxMultiplier` | number |

## OcvPoint

| Поле | Тип / ограничения |
|---|---|
| `soc` | number |
| `voltageV` | number |

## BatteryProfile

| Поле | Тип / ограничения |
|---|---|
| `mode` | ['None', 'Simple', 'Electrical'] |
| `cellCount?` | integer |
| `nominalVoltageV?` | number |
| `capacityAh?` | number |
| `initialSoc?` | number |
| `internalResistanceOhm?` | number |
| `maxDischargeCurrentA?` | number |
| `ocvCurve?` | array |

## PowerSystemProfile

| Поле | Тип / ограничения |
|---|---|
| `battery` | BatteryProfile |

## DerivedProfile

| Поле | Тип / ограничения |
|---|---|
| `projectedAreaLut?` | array |
| `hoverRpm?` | number |
| `maxTotalThrustN?` | number |
| `thrustToWeightRatio?` | number |
| `rotorDiskAreasM2?` | array |
| `approximateInertiaKgM2?` | array |

## ParameterProvenance

| Поле | Тип / ограничения |
|---|---|
| `path` | string |
| `sourceType` | ['Manufacturer', 'Measured', 'Calculated', 'AutoGeometry', 'Preset', 'Estimated', 'User'] |
| `source` | string |
| `confidence` | number |

## DroneProfile

| Поле | Тип / ограничения |
|---|---|
| `schemaVersion` | ['1.0.0'] |
| `metadata` | Metadata |
| `coordinateSystem` | CoordinateSystem |
| `physicsConfiguration` | PhysicsConfiguration |
| `massProperties` | MassProperties |
| `rotors` | array |
| `bodyAerodynamics` | BodyAerodynamicsProfile |
| `groundEffect?` | GroundEffectProfile |
| `powerSystem` | PowerSystemProfile |
| `derived?` | DerivedProfile |
| `parameterProvenance` | array |

## EnvironmentProfile

| Поле | Тип / ограничения |
|---|---|
| `schemaVersion` | ['1.0.0'] |
| `gravityMps2` | number |
| `airDensityMode` | ['Constant', 'StandardAtmosphere'] |
| `airDensityKgM3` | number |
| `temperatureK?` | number |
| `pressurePa?` | number |
| `altitudeM?` | number |
| `windMode` | ['None', 'Constant', 'Gust', 'Turbulence', 'CustomField'] |
| `windVelocityWorldMps` | array |
| `gustEnabled` | boolean |
| `gustIntensityMps?` | number |
| `gustTimeScaleS?` | number |
| `turbulenceSeed?` | integer |
