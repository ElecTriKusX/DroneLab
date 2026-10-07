using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DroneLab.Physics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DroneLab.Configurator
{
    [Serializable]
    public sealed class OptionalNumber
    {
        public bool hasValue;
        public double value;

        public static OptionalNumber Missing() => new OptionalNumber();
        public static OptionalNumber From(double value) => new OptionalNumber { hasValue = true, value = value };
    }

    [Serializable]
    public sealed class OptionalVector
    {
        public bool hasValue;
        public double[] value = new double[3];

        public static OptionalVector Missing() => new OptionalVector();
        public static OptionalVector From(Vector3 value) => new OptionalVector
        {
            hasValue = true,
            value = new[] { (double)value.x, (double)value.y, (double)value.z }
        };

        public Vector3 ToUnity() =>
            value == null || value.Length < 3
                ? Vector3.zero
                : new Vector3((float)value[0], (float)value[1], (float)value[2]);
    }

    [Serializable]
    public sealed class ConfiguratorProvenance
    {
        public string path = "";
        public string sourceType = "User";
        public string source = "Configurator UI";
        public double confidence = 1.0;
    }

    [Serializable]
    public sealed class ConfiguratorModules
    {
        public bool motorResponse = true;
        public bool bodyDrag;
        public bool windInteraction;
        public bool groundEffect;
        public bool rotorAerodynamics;
        public bool bladeFlapping;
        public bool inducedDrag;
        public bool batteryDischarge;
        public bool batteryVoltageSag;
        public bool motorElectrical;
        public bool gyroscopicRotorEffects;
    }

    [Serializable]
    public sealed class ConfiguratorMotor
    {
        public string dynamicsModel = "FirstOrder";
        public OptionalNumber minRpm = OptionalNumber.From(0);
        public OptionalNumber idleRpm = OptionalNumber.From(0);
        public OptionalNumber maxRpm = OptionalNumber.Missing();
        public OptionalNumber responseTimeUpS = OptionalNumber.Missing();
        public OptionalNumber responseTimeDownS = OptionalNumber.Missing();
        public OptionalNumber rotatingInertiaKgM2 = OptionalNumber.Missing();
    }

    [Serializable]
    public sealed class ConfiguratorPropeller
    {
        public OptionalNumber diameterM = OptionalNumber.Missing();
        public OptionalNumber pitchM = OptionalNumber.Missing();
        public int bladeCount = 2;
    }

    [Serializable]
    public sealed class ConfiguratorRpmPoint
    {
        public OptionalNumber rpm = OptionalNumber.Missing();
        public OptionalNumber thrustN = OptionalNumber.Missing();
        public OptionalNumber torqueNm = OptionalNumber.Missing();
        public OptionalNumber currentA = OptionalNumber.Missing();
    }

    [Serializable]
    public sealed class ConfiguratorPerformanceMapPoint
    {
        public OptionalNumber rpm = OptionalNumber.Missing();
        public OptionalNumber advanceRatio = OptionalNumber.Missing();
        public OptionalNumber ct = OptionalNumber.Missing();
        public OptionalNumber cq = OptionalNumber.Missing();
    }

    [Serializable]
    public sealed class ConfiguratorPerformance
    {
        public string model = "OmegaSquared";
        public OptionalNumber kThrustNPerRadPerSecSquared = OptionalNumber.Missing();
        public OptionalNumber kTorqueNmPerRadPerSecSquared = OptionalNumber.Missing();
        public OptionalNumber referenceAirDensityKgM3 = OptionalNumber.From(1.225);
        public OptionalNumber ct = OptionalNumber.Missing();
        public OptionalNumber cq = OptionalNumber.Missing();
        public string outOfRangePolicy = "Reject";
        public List<ConfiguratorRpmPoint> rpmTable = new List<ConfiguratorRpmPoint>();
        public List<ConfiguratorPerformanceMapPoint> performanceMap = new List<ConfiguratorPerformanceMapPoint>();
    }

    [Serializable]
    public sealed class ConfiguratorRotor
    {
        public string rotorId = "M1";
        public OptionalVector positionLocalM = OptionalVector.Missing();
        public double[] thrustAxisLocal = { 0.0, 1.0, 0.0 };
        public string spinDirection = "CCW";
        public ConfiguratorMotor motor = new ConfiguratorMotor();
        public ConfiguratorPropeller propeller = new ConfiguratorPropeller();
        public ConfiguratorPerformance performance = new ConfiguratorPerformance();
    }

    [Serializable]
    public sealed class ConfiguratorBodyAerodynamics
    {
        public string model = "AxisApproximation";
        public OptionalVector dragCd = OptionalVector.Missing();
        public OptionalVector referenceAreaM2 = OptionalVector.Missing();
        public OptionalVector dragApplicationPointLocalM = OptionalVector.Missing();
    }

    [Serializable]
    public sealed class ConfiguratorOcvPoint
    {
        public OptionalNumber soc = OptionalNumber.Missing();
        public OptionalNumber voltageV = OptionalNumber.Missing();
    }

    [Serializable]
    public sealed class ConfiguratorBattery
    {
        public string mode = "None";
        public OptionalNumber cellCount = OptionalNumber.Missing();
        public OptionalNumber nominalVoltageV = OptionalNumber.Missing();
        public OptionalNumber capacityAh = OptionalNumber.Missing();
        public OptionalNumber initialSoc = OptionalNumber.Missing();
        public OptionalNumber internalResistanceOhm = OptionalNumber.Missing();
        public OptionalNumber maxDischargeCurrentA = OptionalNumber.Missing();
        public List<ConfiguratorOcvPoint> ocvCurve = new List<ConfiguratorOcvPoint>();
    }

    [Serializable]
    public sealed class DroneConfiguratorDraft
    {
        public string draftVersion = "0.2.0";
        public string targetSchemaVersion = "1.0.0";
        public string profileId = "new-multirotor";

        public string name = "New Multirotor";
        public string manufacturer = "";
        public string model = "";
        public string description = "";

        public string sourceModelPath = "";
        public double modelScaleMetersPerUnit = 1.0;
        public string visualForwardAxis = "+Z";
        public string visualUpAxis = "+Y";

        public OptionalNumber massKg = OptionalNumber.Missing();
        public OptionalVector centerOfMassLocalM = OptionalVector.Missing();
        public OptionalVector dimensionsM = OptionalVector.Missing();

        public string inertiaMode = "AutoBox";
        public OptionalVector principalMomentsKgM2 = OptionalVector.Missing();
        public bool hasPrincipalAxesRotation;
        public double[] principalAxesRotationXyzw = { 0.0, 0.0, 0.0, 1.0 };

        public string fidelity = "Basic";
        public ConfiguratorModules modules = new ConfiguratorModules();
        public List<ConfiguratorRotor> rotors = new List<ConfiguratorRotor>();

        public ConfiguratorBodyAerodynamics bodyAerodynamics = new ConfiguratorBodyAerodynamics();
        public ConfiguratorBattery battery = new ConfiguratorBattery();

        public List<ConfiguratorProvenance> parameterProvenance = new List<ConfiguratorProvenance>();

        public string selectedSection = "Model";
        public int selectedRotorIndex;

        public ConfiguratorProvenance GetOrCreateProvenance(string path)
        {
            var existing = parameterProvenance.FirstOrDefault(x => x.path == path);
            if (existing != null) return existing;
            existing = new ConfiguratorProvenance { path = path };
            parameterProvenance.Add(existing);
            return existing;
        }
    }

    [Serializable]
    public sealed class RotorVisualBinding
    {
        public string rotorId = "M1";
        public string nodePath = "";
        public double[] visualRotationAxisLocal = { 0.0, 1.0, 0.0 };
    }

    [Serializable]
    public sealed class ColliderBinding
    {
        public string nodePath = "";
        public string type = "Box";
        public double[] centerLocalM = { 0.0, 0.0, 0.0 };
        public double[] sizeM = { 1.0, 1.0, 1.0 };
    }

    [Serializable]
    public sealed class DroneAssetBindings
    {
        public string version = "0.2.0";
        public string modelFile = "model.glb";
        public string visualForwardAxis = "+Z";
        public string visualUpAxis = "+Y";
        public List<RotorVisualBinding> rotorVisualBindings = new List<RotorVisualBinding>();
        public List<ColliderBinding> colliders = new List<ColliderBinding>();

        public RotorVisualBinding GetOrCreate(string rotorId)
        {
            var found = rotorVisualBindings.FirstOrDefault(x => x.rotorId == rotorId);
            if (found != null) return found;
            found = new RotorVisualBinding { rotorId = rotorId };
            rotorVisualBindings.Add(found);
            return found;
        }
    }

    [Serializable]
    public sealed class DronePackageManifest
    {
        public string packageVersion = "0.2.0";
        public string profileId = "new-multirotor";
        public string profileFile = "profile.json";
        public string draftFile = "draft.json";
        public string modelFile = "model.glb";
        public string assetBindingsFile = "asset-bindings.json";
    }

    public static class DroneConfiguratorFactory
    {
        public static DroneConfiguratorDraft Create(int rotorCount = 4)
        {
            var draft = new DroneConfiguratorDraft();
            SetRotorCount(draft, rotorCount);
            draft.GetOrCreateProvenance("coordinateSystem.modelScaleMetersPerUnit").sourceType = "User";
            draft.GetOrCreateProvenance("rotors[].performance.referenceAirDensityKgM3").sourceType = "Preset";
            draft.GetOrCreateProvenance("rotors[].performance.referenceAirDensityKgM3").source = "Standard sea-level calibration condition; confirm for measured kT/kQ";
            return draft;
        }

        public static void SetRotorCount(DroneConfiguratorDraft draft, int count)
        {
            count = Mathf.Clamp(count, 1, 32);

            while (draft.rotors.Count < count)
            {
                int index = draft.rotors.Count;
                float angle = Mathf.PI * 2f * index / count;
                var rotor = new ConfiguratorRotor
                {
                    rotorId = "M" + (index + 1),
                    positionLocalM = OptionalVector.From(new Vector3(
                        Mathf.Sin(angle) * 0.25f,
                        0f,
                        Mathf.Cos(angle) * 0.25f)),
                    spinDirection = index % 2 == 0 ? "CCW" : "CW"
                };
                draft.rotors.Add(rotor);
            }

            while (draft.rotors.Count > count)
                draft.rotors.RemoveAt(draft.rotors.Count - 1);

            draft.selectedRotorIndex = Mathf.Clamp(draft.selectedRotorIndex, 0, Mathf.Max(0, draft.rotors.Count - 1));
        }

        public static void CopyCommonRotorData(DroneConfiguratorDraft draft, int sourceIndex)
        {
            if (draft == null || sourceIndex < 0 || sourceIndex >= draft.rotors.Count) return;
            var source = draft.rotors[sourceIndex];

            foreach (var rotor in draft.rotors)
            {
                rotor.motor = JsonConvert.DeserializeObject<ConfiguratorMotor>(
                    JsonConvert.SerializeObject(source.motor));
                rotor.propeller = JsonConvert.DeserializeObject<ConfiguratorPropeller>(
                    JsonConvert.SerializeObject(source.propeller));
                rotor.performance = JsonConvert.DeserializeObject<ConfiguratorPerformance>(
                    JsonConvert.SerializeObject(source.performance));
            }
        }
    }

    public sealed class ConfiguratorIssue
    {
        public string Severity;
        public string Path;
        public string Message;

        public override string ToString() => "[" + Severity + "] " + Path + ": " + Message;
    }

    public static class DroneConfiguratorProfileAdapter
    {
        private static JArray Vec(OptionalVector vector)
        {
            return vector != null && vector.hasValue && vector.value != null && vector.value.Length >= 3
                ? new JArray(vector.value[0], vector.value[1], vector.value[2])
                : null;
        }

        private static JArray Vec(double[] vector)
        {
            return vector != null && vector.Length >= 3
                ? new JArray(vector[0], vector[1], vector[2])
                : null;
        }

        private static void AddOptional(JObject parent, string name, OptionalNumber value)
        {
            if (value != null && value.hasValue)
                parent[name] = value.value;
        }

        public static string BuildStrictProfileJson(DroneConfiguratorDraft draft)
        {
            if (draft == null) throw new ArgumentNullException(nameof(draft));

            var root = new JObject
            {
                ["schemaVersion"] = "1.0.0",
                ["metadata"] = new JObject { ["name"] = draft.name ?? "" },
                ["coordinateSystem"] = new JObject
                {
                    ["convention"] = "UnityLeftHandedYUpZForward",
                    ["lengthUnit"] = "m",
                    ["modelScaleMetersPerUnit"] = draft.modelScaleMetersPerUnit
                }
            };

            var metadata = (JObject)root["metadata"];
            if (!string.IsNullOrWhiteSpace(draft.manufacturer)) metadata["manufacturer"] = draft.manufacturer;
            if (!string.IsNullOrWhiteSpace(draft.model)) metadata["model"] = draft.model;
            if (!string.IsNullOrWhiteSpace(draft.description)) metadata["description"] = draft.description;

            root["physicsConfiguration"] = new JObject
            {
                ["fidelity"] = draft.fidelity,
                ["modules"] = new JObject
                {
                    ["motorResponse"] = draft.modules.motorResponse,
                    ["bodyDrag"] = draft.modules.bodyDrag,
                    ["windInteraction"] = draft.modules.windInteraction,
                    ["groundEffect"] = draft.modules.groundEffect,
                    ["rotorAerodynamics"] = draft.modules.rotorAerodynamics,
                    ["bladeFlapping"] = draft.modules.bladeFlapping,
                    ["inducedDrag"] = draft.modules.inducedDrag,
                    ["batteryDischarge"] = draft.modules.batteryDischarge,
                    ["batteryVoltageSag"] = draft.modules.batteryVoltageSag,
                    ["motorElectrical"] = draft.modules.motorElectrical,
                    ["gyroscopicRotorEffects"] = draft.modules.gyroscopicRotorEffects
                }
            };

            var mass = new JObject();
            AddOptional(mass, "massKg", draft.massKg);
            var com = Vec(draft.centerOfMassLocalM);
            if (com != null) mass["centerOfMassLocalM"] = com;
            var dimensions = Vec(draft.dimensionsM);
            if (dimensions != null) mass["dimensionsM"] = dimensions;

            var inertia = new JObject { ["mode"] = draft.inertiaMode };
            if (draft.inertiaMode == "ManualPrincipal")
            {
                var principal = Vec(draft.principalMomentsKgM2);
                if (principal != null) inertia["principalMomentsKgM2"] = principal;
                if (draft.hasPrincipalAxesRotation && draft.principalAxesRotationXyzw != null && draft.principalAxesRotationXyzw.Length >= 4)
                    inertia["principalAxesRotationXyzw"] = new JArray(draft.principalAxesRotationXyzw[0], draft.principalAxesRotationXyzw[1], draft.principalAxesRotationXyzw[2], draft.principalAxesRotationXyzw[3]);
            }
            mass["inertia"] = inertia;
            root["massProperties"] = mass;

            var rotors = new JArray();
            foreach (var rotor in draft.rotors)
            {
                var geometry = new JObject
                {
                    ["thrustAxisLocal"] = Vec(rotor.thrustAxisLocal),
                    ["spinDirection"] = rotor.spinDirection
                };
                var position = Vec(rotor.positionLocalM);
                if (position != null) geometry["positionLocalM"] = position;

                var motor = new JObject { ["dynamicsModel"] = rotor.motor.dynamicsModel };
                AddOptional(motor, "minRpm", rotor.motor.minRpm);
                AddOptional(motor, "idleRpm", rotor.motor.idleRpm);
                AddOptional(motor, "maxRpm", rotor.motor.maxRpm);
                AddOptional(motor, "responseTimeUpS", rotor.motor.responseTimeUpS);
                AddOptional(motor, "responseTimeDownS", rotor.motor.responseTimeDownS);
                AddOptional(motor, "rotatingInertiaKgM2", rotor.motor.rotatingInertiaKgM2);

                var propeller = new JObject { ["bladeCount"] = rotor.propeller.bladeCount };
                AddOptional(propeller, "diameterM", rotor.propeller.diameterM);
                AddOptional(propeller, "pitchM", rotor.propeller.pitchM);

                var performance = new JObject
                {
                    ["model"] = rotor.performance.model,
                    ["outOfRangePolicy"] = rotor.performance.outOfRangePolicy
                };
                if (rotor.performance.model == "OmegaSquared")
                {
                    AddOptional(performance, "kThrustNPerRadPerSecSquared", rotor.performance.kThrustNPerRadPerSecSquared);
                    AddOptional(performance, "kTorqueNmPerRadPerSecSquared", rotor.performance.kTorqueNmPerRadPerSecSquared);
                    AddOptional(performance, "referenceAirDensityKgM3", rotor.performance.referenceAirDensityKgM3);
                }
                else if (rotor.performance.model == "CtCq")
                {
                    AddOptional(performance, "ct", rotor.performance.ct);
                    AddOptional(performance, "cq", rotor.performance.cq);
                }
                else if (rotor.performance.model == "RpmTable")
                {
                    AddOptional(performance, "referenceAirDensityKgM3", rotor.performance.referenceAirDensityKgM3);
                    var table = new JArray();
                    foreach (var point in rotor.performance.rpmTable ?? new List<ConfiguratorRpmPoint>())
                    {
                        var row = new JObject();
                        AddOptional(row, "rpm", point.rpm);
                        AddOptional(row, "thrustN", point.thrustN);
                        AddOptional(row, "torqueNm", point.torqueNm);
                        AddOptional(row, "currentA", point.currentA);
                        table.Add(row);
                    }
                    performance["rpmTable"] = table;
                }
                else if (rotor.performance.model == "PerformanceMap")
                {
                    var map = new JArray();
                    foreach (var point in rotor.performance.performanceMap ?? new List<ConfiguratorPerformanceMapPoint>())
                    {
                        var row = new JObject();
                        AddOptional(row, "rpm", point.rpm);
                        AddOptional(row, "advanceRatio", point.advanceRatio);
                        AddOptional(row, "ct", point.ct);
                        AddOptional(row, "cq", point.cq);
                        map.Add(row);
                    }
                    performance["performanceMap"] = map;
                }

                rotors.Add(new JObject
                {
                    ["rotorId"] = rotor.rotorId,
                    ["geometry"] = geometry,
                    ["motor"] = motor,
                    ["propeller"] = propeller,
                    ["performance"] = performance
                });
            }
            root["rotors"] = rotors;

            var body = new JObject { ["model"] = draft.bodyAerodynamics.model };
            var dragCd = Vec(draft.bodyAerodynamics.dragCd);
            if (dragCd != null) body["dragCd"] = dragCd;
            var area = Vec(draft.bodyAerodynamics.referenceAreaM2);
            if (area != null) body["referenceAreaM2"] = area;
            var dragPoint = Vec(draft.bodyAerodynamics.dragApplicationPointLocalM);
            if (dragPoint != null) body["dragApplicationPointLocalM"] = dragPoint;
            root["bodyAerodynamics"] = body;

            var battery = new JObject { ["mode"] = draft.battery.mode };
            if (draft.battery.mode != "None")
            {
                AddOptional(battery, "cellCount", draft.battery.cellCount);
                AddOptional(battery, "nominalVoltageV", draft.battery.nominalVoltageV);
                AddOptional(battery, "capacityAh", draft.battery.capacityAh);
                AddOptional(battery, "initialSoc", draft.battery.initialSoc);
                AddOptional(battery, "internalResistanceOhm", draft.battery.internalResistanceOhm);
                AddOptional(battery, "maxDischargeCurrentA", draft.battery.maxDischargeCurrentA);

                var ocv = new JArray();
                foreach (var point in draft.battery.ocvCurve ?? new List<ConfiguratorOcvPoint>())
                {
                    var row = new JObject();
                    AddOptional(row, "soc", point.soc);
                    AddOptional(row, "voltageV", point.voltageV);
                    ocv.Add(row);
                }
                battery["ocvCurve"] = ocv;
            }
            root["powerSystem"] = new JObject
            {
                ["battery"] = battery,
                ["thermalEnabled"] = false
            };

            var provenance = new JArray();
            foreach (var p in draft.parameterProvenance)
            {
                if (string.IsNullOrWhiteSpace(p.path)) continue;
                provenance.Add(new JObject
                {
                    ["path"] = p.path,
                    ["sourceType"] = p.sourceType,
                    ["source"] = string.IsNullOrWhiteSpace(p.source) ? "Configurator UI" : p.source,
                    ["confidence"] = Math.Max(0, Math.Min(1, p.confidence))
                });
            }
            root["parameterProvenance"] = provenance;

            return root.ToString(Formatting.Indented);
        }

        public static List<ConfiguratorIssue> Validate(DroneConfiguratorDraft draft, out string strictJson)
        {
            strictJson = BuildStrictProfileJson(draft);
            var issues = new List<ConfiguratorIssue>();

            TextAsset droneSchema = Resources.Load<TextAsset>("DronePhysics/drone-profile.schema");
            TextAsset environmentSchema = Resources.Load<TextAsset>("DronePhysics/environment-profile.schema");
            TextAsset environment = Resources.Load<TextAsset>("DronePhysics/environment_calm");

            if (droneSchema == null || environmentSchema == null || environment == null)
            {
                issues.Add(new ConfiguratorIssue
                {
                    Severity = "Error",
                    Path = "$",
                    Message = "DronePhysics schema/environment resources are missing."
                });
                return issues;
            }

            ProfileLoadResult loaded = ProfileLoader.Load(strictJson, environment.text, droneSchema.text, environmentSchema.text);
            foreach (ValidationIssue issue in loaded.Issues)
            {
                issues.Add(new ConfiguratorIssue
                {
                    Severity = issue.Severity,
                    Path = issue.Path,
                    Message = issue.Message
                });
            }

            if (draft.rotors.Count != 4)
            {
                issues.Add(new ConfiguratorIssue
                {
                    Severity = "Warning",
                    Path = "rotors",
                    Message = "Profile can be saved, but the current Quad flight allocator/controller is intended for four compatible rotors."
                });
            }

            return issues;
        }

        public static bool HasErrors(IEnumerable<ConfiguratorIssue> issues) =>
            issues.Any(x => string.Equals(x.Severity, "Error", StringComparison.OrdinalIgnoreCase));
    }

    public static class DroneConfiguratorStorage
    {
        public static string ProfilesRoot => Path.Combine(Application.persistentDataPath, "DroneProfiles");

        public static string ProfileDirectory(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) id = "unnamed";
            foreach (char c in Path.GetInvalidFileNameChars()) id = id.Replace(c, '_');
            return Path.Combine(ProfilesRoot, id.Trim());
        }

        public static void SaveDraft(
            DroneConfiguratorDraft draft,
            DroneAssetBindings bindings,
            DronePackageManifest manifest)
        {
            string directory = ProfileDirectory(draft.profileId);
            Directory.CreateDirectory(directory);

            manifest.profileId = draft.profileId;
            bindings.visualForwardAxis = draft.visualForwardAxis;
            bindings.visualUpAxis = draft.visualUpAxis;

            AtomicWrite(Path.Combine(directory, manifest.draftFile), JsonConvert.SerializeObject(draft, Formatting.Indented));
            AtomicWrite(Path.Combine(directory, manifest.assetBindingsFile), JsonConvert.SerializeObject(bindings, Formatting.Indented));
            AtomicWrite(Path.Combine(directory, "package-manifest.json"), JsonConvert.SerializeObject(manifest, Formatting.Indented));

            if (!string.IsNullOrWhiteSpace(draft.sourceModelPath) &&
                File.Exists(draft.sourceModelPath) &&
                string.Equals(Path.GetExtension(draft.sourceModelPath), ".glb", StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(draft.sourceModelPath, Path.Combine(directory, manifest.modelFile), true);
            }
        }

        public static bool TryLoadDraft(
            string profileId,
            out DroneConfiguratorDraft draft,
            out DroneAssetBindings bindings,
            out DronePackageManifest manifest,
            out string error)
        {
            draft = null;
            bindings = null;
            manifest = null;
            error = "";

            try
            {
                string directory = ProfileDirectory(profileId);
                string manifestPath = Path.Combine(directory, "package-manifest.json");
                if (!File.Exists(manifestPath))
                {
                    error = "package-manifest.json not found.";
                    return false;
                }

                manifest = JsonConvert.DeserializeObject<DronePackageManifest>(File.ReadAllText(manifestPath));
                draft = JsonConvert.DeserializeObject<DroneConfiguratorDraft>(
                    File.ReadAllText(Path.Combine(directory, manifest.draftFile)));
                string bindingPath = Path.Combine(directory, manifest.assetBindingsFile);
                bindings = File.Exists(bindingPath)
                    ? JsonConvert.DeserializeObject<DroneAssetBindings>(File.ReadAllText(bindingPath))
                    : new DroneAssetBindings();

                string packagedModel = Path.Combine(directory, manifest.modelFile);
                if (File.Exists(packagedModel)) draft.sourceModelPath = packagedModel;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static void SaveReadyProfile(
            DroneConfiguratorDraft draft,
            DroneAssetBindings bindings,
            DronePackageManifest manifest,
            string strictJson)
        {
            SaveDraft(draft, bindings, manifest);
            AtomicWrite(Path.Combine(ProfileDirectory(draft.profileId), manifest.profileFile), strictJson);
        }

        public static void AtomicWrite(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temp = path + ".tmp";
            File.WriteAllText(temp, content);

            if (File.Exists(path))
            {
                string backup = path + ".bak";
                if (File.Exists(backup)) File.Delete(backup);
                File.Replace(temp, path, backup);
                if (File.Exists(backup)) File.Delete(backup);
            }
            else
            {
                File.Move(temp, path);
            }
        }
    }
}
