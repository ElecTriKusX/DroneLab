using System;
using System.IO;
using System.Linq;
using System.Text;
using DroneLab.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

public sealed class ConfiguratorDataTests
{
    private string temp;
    [SetUp] public void SetUp(){temp=Path.Combine(Path.GetTempPath(),"drone-config-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);UnityEngine.Application.persistentDataPath=Path.Combine(temp,"library");}
    [TearDown] public void TearDown(){Directory.Delete(temp,true);}
    [Test] public void EveryContractFieldHasPhysicalHelp()
    {
        foreach(var def in ((JObject)DroneParameterSchema.Schema["$defs"]).Properties())
            foreach(var field in ((JObject)def.Value["properties"]).Properties())
                Assert.That((string)DroneParameterSchema.Help[field.Name]?["description"],Is.Not.Empty,def.Name+"."+field.Name);
    }
    [TestCase("quad_test_basic")][TestCase("quad_test_thermal")][TestCase("quad_test_performance_map")]
    [TestCase("quad_test_surfaces")][TestCase("quad_test_rpm_table")][TestCase("reference_crazyflie20")]
    public void EditingAndPersistencePreserveEntirePhysicalProfile(string name)
    {
        var doc=new DroneProfileDocument{profile=JObject.Parse(DroneProfileLibrary.Resource(name))};
        string before=doc.profile.ToString();var clone=doc.Copy();clone.visual["rotationEulerDeg"]=new JArray(90,0,20);
        Assert.That(clone.profile.ToString(),Is.EqualTo(before));
        var validated=DroneProfileLibrary.Validate(clone);Assert.That(validated.Success,Is.True,name+"\n"+string.Join("\n",validated.Issues.Where(i=>i.Severity=="Error").Select(i=>i.ToString())));
        DroneProfileLibrary.Save(clone,false);
        var reloaded=JsonConvert.DeserializeObject<DroneProfileDocument>(File.ReadAllText(Path.Combine(DroneProfileLibrary.Folder(clone.id),"document.json")));
        Assert.That(reloaded.profile.ToString(),Is.EqualTo(before));
        Assert.That(File.ReadAllText(Path.Combine(DroneProfileLibrary.Folder(clone.id),"profile.json")),Is.EqualTo(before));
    }
    [Test] public void InvalidProfileCannotOverwriteReadyFile()
    {
        var doc=DroneProfileLibrary.Create();DroneProfileLibrary.Save(doc,false);
        string path=Path.Combine(DroneProfileLibrary.Folder(doc.id),"profile.json"),before=File.ReadAllText(path);
        doc.profile["massProperties"]["massKg"]=-1;
        Assert.Throws<ArgumentException>(()=>DroneProfileLibrary.Save(doc,false));Assert.That(File.ReadAllText(path),Is.EqualTo(before));
    }
    [Test] public void ThermalGalleryCalibrationDoesNotBypassSelectedEnvironment()
    {
        var doc=new DroneProfileDocument{profile=JObject.Parse(DroneProfileLibrary.Resource("quad_test_thermal"))};
        Assert.That(DroneProfileLibrary.Validate(doc).Success,Is.True);
        Assert.That(DroneProfileLibrary.Validate(doc,DroneProfileLibrary.Resource("environment_calm")).Success,Is.False);
    }
    [TestCase("rotors")][TestCase("massProperties")][TestCase("coordinateSystem")]
    public void MalformedImportReturnsClearErrorBeforeEditorIsOpened(string key)
    {
        var doc=DroneProfileLibrary.Create();doc.profile[key]="wrong type";
        string path=Path.Combine(temp,"broken.json");File.WriteAllText(path,doc.profile.ToString());
        Assert.Throws<ArgumentException>(()=>DroneProfileLibrary.Import(path));
    }
    [Test] public void ImportedEnvelopeRestoresAbsentVisualDefaultsWithoutChangingPhysics()
    {
        var doc=DroneProfileLibrary.Create();string before=doc.profile.ToString();
        var envelope=JObject.FromObject(doc);envelope["visual"]=null;envelope["unfinishedInputs"]=null;
        string path=Path.Combine(temp,"package.json");File.WriteAllText(path,envelope.ToString());
        var imported=DroneProfileLibrary.Import(path);Assert.That(imported.profile.ToString(),Is.EqualTo(before));
        Assert.That((bool)imported.visual["centerModel"],Is.True);Assert.That(imported.unfinishedInputs,Is.Empty);
    }
    [Test] public void PositivePhysicalDimensionsAreValidatedBeforeCommit()
    {
        var rule=DroneParameterSchema.ComponentRule("dimensionsM",new JObject{["type"]="number"},"massProperties.dimensionsM");
        Assert.That(DroneParameterSchema.TryNumber("0",rule,out _,out _),Is.False);
        Assert.That(DroneParameterSchema.TryNumber("0,12",rule,out _,out _),Is.True);
    }
    [Test] public void DraftPreservesUnfinishedInputAndDoesNotPublishIt()
    {
        var doc=DroneProfileLibrary.Create();doc.unfinishedInputs["massProperties.massKg"]="1e";DroneProfileLibrary.Save(doc,true);
        var reloaded=JsonConvert.DeserializeObject<DroneProfileDocument>(File.ReadAllText(Path.Combine(DroneProfileLibrary.Folder(doc.id),"document.json")));
        Assert.That(reloaded.unfinishedInputs["massProperties.massKg"],Is.EqualTo("1e"));
        Assert.That(reloaded.draft,Is.True);Assert.That(File.Exists(Path.Combine(DroneProfileLibrary.Folder(doc.id),"profile.json")),Is.False);
    }
    [Test] public void GltfCopyKeepsRelativeTexturesAndBuffers()
    {
        Directory.CreateDirectory(Path.Combine(temp,"textures"));File.WriteAllBytes(Path.Combine(temp,"textures","albedo.png"),new byte[]{1,2,3});File.WriteAllBytes(Path.Combine(temp,"mesh.bin"),new byte[]{4,5});
        var gltf=new JObject{["asset"]=new JObject{["version"]="2.0"},["buffers"]=new JArray(new JObject{["uri"]="mesh.bin"}),["images"]=new JArray(new JObject{["uri"]="textures/albedo.png"})};
        string source=Path.Combine(temp,"drone.gltf");File.WriteAllText(source,gltf.ToString());string copy=DroneModelFiles.Copy(source,Path.Combine(temp,"copy"));
        Assert.That(Path.GetExtension(copy),Is.EqualTo(".gltf"));Assert.That(File.ReadAllText(copy),Is.EqualTo(gltf.ToString()));
        Assert.That(File.ReadAllBytes(Path.Combine(temp,"copy","textures","albedo.png")),Is.EqualTo(new byte[]{1,2,3}));
        Assert.That(File.Exists(Path.Combine(temp,"copy","mesh.bin")),Is.True);
    }
    [Test] public void MissingTextureFailsBeforeAnyDestinationIsWritten()
    {
        string source=Path.Combine(temp,"drone.gltf");File.WriteAllText(source,"{\"images\":[{\"uri\":\"missing.png\"}]}");
        Assert.Throws<FileNotFoundException>(()=>DroneModelFiles.Copy(source,Path.Combine(temp,"copy")));
        Assert.That(Directory.Exists(Path.Combine(temp,"copy")),Is.False);
    }
    [TestCase("../secret.png")][TestCase("%2e%2e/secret.png")][TestCase("https://example.com/image.png")][TestCase("/etc/passwd")]
    public void ModelDependencyMustStayInsideItsFolder(string uri){Assert.Throws<ArgumentException>(()=>DroneModelFiles.Contained(temp,uri));}
    [Test] public void GlbCopyKeepsExternalTextureWhenPresent()
    {
        File.WriteAllBytes(Path.Combine(temp,"albedo.png"),new byte[]{8});
        string json="{\"asset\":{\"version\":\"2.0\"},\"images\":[{\"uri\":\"albedo.png\"}]}";while(json.Length%4!=0)json+=" ";byte[] bytes=Encoding.UTF8.GetBytes(json);
        string path=Path.Combine(temp,"drone.glb");using(var writer=new BinaryWriter(File.Create(path))){writer.Write(0x46546C67u);writer.Write(2u);writer.Write((uint)(20+bytes.Length));writer.Write((uint)bytes.Length);writer.Write(0x4E4F534Au);writer.Write(bytes);}
        string copied=DroneModelFiles.Copy(path,Path.Combine(temp,"copy"));Assert.That(File.ReadAllBytes(copied),Is.EqualTo(File.ReadAllBytes(path)));
        Assert.That(File.Exists(Path.Combine(temp,"copy","albedo.png")),Is.True);
    }
    [Test] public void CsvRoundTripKeepsNumbersAndOptionalMissingCurrent()
    {
        var rule=DroneParameterSchema.ObjectRule("RpmPerformancePoint");var rows=new JArray(new JObject{["rpm"]=0,["thrustN"]=0,["torqueNm"]=0},new JObject{["rpm"]=10000,["thrustN"]=4.2,["torqueNm"]=.02,["currentA"]=3.5});
        var roundtrip=DroneProfileCsv.Read(DroneProfileCsv.Write(rows,rule),rule);Assert.That(roundtrip.Count,Is.EqualTo(rows.Count));foreach(var key in new[]{"rpm","thrustN","torqueNm"})for(int i=0;i<rows.Count;i++)Assert.That((double)roundtrip[i][key],Is.EqualTo((double)rows[i][key]));Assert.That(roundtrip[0]["currentA"],Is.Null);Assert.That((double)roundtrip[1]["currentA"],Is.EqualTo(3.5));
    }
    [Test] public void CsvSemicolonAcceptsDecimalComma()
    {var rows=DroneProfileCsv.Read("rpm;thrustN;torqueNm\n1000;1,25;0,03",DroneParameterSchema.ObjectRule("RpmPerformancePoint"));Assert.That((double)rows[0]["thrustN"],Is.EqualTo(1.25));}
    [Test] public void CsvPreservesQuotedSources()
    {
        var rule=DroneParameterSchema.ObjectRule("ParameterProvenance");var rows=new JArray(new JObject{["path"]="rotors",["sourceType"]="User",["source"]="Стенд, \"А\"",["confidence"]=.8});
        Assert.That(JToken.DeepEquals(rows,DroneProfileCsv.Read(DroneProfileCsv.Write(rows,rule),rule)),Is.True);
    }
    [Test] public void CsvPreservesMultilineSources()
    {
        var rule=DroneParameterSchema.ObjectRule("ParameterProvenance");var rows=new JArray(new JObject{["path"]="massProperties.massKg",["sourceType"]="Measured",["source"]="Строка 1\nСтрока 2, \"опыт\"",["confidence"]=.9});
        Assert.That(JToken.DeepEquals(DroneProfileCsv.Read(DroneProfileCsv.Write(rows,rule),rule),rows),Is.True);
    }
    [Test] public void CsvRejectsDuplicateHeadersAfterNormalization()
    {Assert.Throws<ArgumentException>(()=>DroneProfileCsv.Read("rpm, RPM ,thrustN,torqueNm\n1,2,3,4",DroneParameterSchema.ObjectRule("RpmPerformancePoint")));}
    [Test] public void FailedModelReplacementLeavesPreviousWholePackageUntouched()
    {
        var doc=DroneProfileLibrary.Create();DroneProfileLibrary.Save(doc,false);string folder=DroneProfileLibrary.Folder(doc.id),previous=File.ReadAllText(Path.Combine(folder,"document.json"));
        string path=Path.Combine(temp,"broken.gltf");File.WriteAllText(path,"{\"images\":[{\"uri\":\"missing.png\"}]}");doc.sourceModel=path;doc.profile["metadata"]["name"]="Replacement";
        Assert.Throws<FileNotFoundException>(()=>DroneProfileLibrary.Save(doc,false));
        Assert.That(File.ReadAllText(Path.Combine(folder,"document.json")),Is.EqualTo(previous));Assert.That(doc.sourceModel,Is.EqualTo(path));
    }
    [TestCase("NaN")][TestCase("Infinity")][TestCase("1e99")][TestCase("−4")]
    public void CsvRejectsInvalidNumericCells(string value)
    {Assert.Throws<ArgumentException>(()=>DroneProfileCsv.Read("rpm,thrustN,torqueNm\n1000,"+value+",0.1",DroneParameterSchema.ObjectRule("RpmPerformancePoint")));}
    [Test] public void CsvRejectsDuplicateHeaders(){Assert.Throws<ArgumentException>(()=>DroneProfileCsv.Read("rpm,rpm,torqueNm\n1,1,1",DroneParameterSchema.ObjectRule("RpmPerformancePoint")));}
    [Test] public void VisualTransformNeverMovesPhysicalRotorPoints()
    {
        var doc=DroneProfileLibrary.Create();var geometry=doc.profile["rotors"].DeepClone();doc.visual["scale"]=.01;doc.visual["rotationEulerDeg"]=new JArray(90,180,0);
        Assert.That(JToken.DeepEquals(doc.profile["rotors"],geometry),Is.True);
    }
    [Test] public void RemovingRowPreservesAndReindexesOtherUnfinishedCells()
    {
        var rows=new JArray(new JObject(),new JObject(),new JObject());var input=new System.Collections.Generic.Dictionary<string,string>{["table[0].rpm"]="0",["table[1].rpm"]="wrong",["table[2].rpm"]="1e",["other.mass"]="4"};
        DroneArrayEdits.Remove(rows,1,"table",input);Assert.That(rows.Count,Is.EqualTo(2));Assert.That(input["table[1].rpm"],Is.EqualTo("1e"));Assert.That(input["other.mass"],Is.EqualTo("4"));Assert.That(input.ContainsKey("table[2].rpm"),Is.False);
    }
    [Test] public void DraftWithUnfinishedNumberCannotBePublished()
    {var doc=DroneProfileLibrary.Create();doc.unfinishedInputs["massProperties.massKg"]="1e";Assert.Throws<ArgumentException>(()=>DroneProfileLibrary.Save(doc,false));}
    [Test] public void CategorySurvivesSaveCopyAndPackageImportWithoutChangingPhysicalJson()
    {
        var doc=DroneProfileLibrary.Create();string physical=doc.profile.ToString();doc.category="  FPV  ";DroneProfileLibrary.Save(doc,false);
        string path=Path.Combine(DroneProfileLibrary.Folder(doc.id),"document.json");var saved=JsonConvert.DeserializeObject<DroneProfileDocument>(File.ReadAllText(path));
        Assert.That(saved.Category,Is.EqualTo("FPV"));Assert.That(saved.Copy().Category,Is.EqualTo("FPV"));
        Assert.That(DroneProfileLibrary.Import(path).Category,Is.EqualTo("FPV"));
        Assert.That(File.ReadAllText(Path.Combine(DroneProfileLibrary.Folder(doc.id),"profile.json")),Is.EqualTo(physical));
    }
    [Test] public void OlderPackageWithoutCategoryRemainsEditable()
    {
        var token=JObject.FromObject(DroneProfileLibrary.Create());token.Remove("category");
        string path=Path.Combine(temp,"old-document.json");File.WriteAllText(path,token.ToString());
        Assert.That(DroneProfileLibrary.Import(path).Category,Is.EqualTo("Без категории"));
    }
    [Test] public void DeleteRemovesOnlySelectedDronePackage()
    {
        var first=DroneProfileLibrary.Create();var second=DroneProfileLibrary.Create();DroneProfileLibrary.Save(first,false);DroneProfileLibrary.Save(second,false);
        string secondFile=Path.Combine(DroneProfileLibrary.Folder(second.id),"document.json"),before=File.ReadAllText(secondFile);
        DroneProfileLibrary.Delete(first);
        Assert.That(Directory.Exists(DroneProfileLibrary.Folder(first.id)),Is.False);Assert.That(File.ReadAllText(secondFile),Is.EqualTo(before));
        var reloaded=DroneProfileLibrary.LoadAll(out var warnings);Assert.That(warnings,Is.Empty);Assert.That(reloaded.Any(d=>d.id==first.id),Is.False);Assert.That(reloaded.Any(d=>d.id==second.id),Is.True);
    }
    [Test] public void DeletedBuiltinStaysHiddenAfterReloadAndDoesNotHideItsCopy()
    {
        var original=DroneProfileLibrary.LoadAll(out _).First();var copy=original.Copy();copy.id=Guid.NewGuid().ToString("N");DroneProfileLibrary.Save(copy,false);
        DroneProfileLibrary.Delete(original);var reloaded=DroneProfileLibrary.LoadAll(out var warnings);
        Assert.That(warnings,Is.Empty);Assert.That(reloaded.Any(d=>d.id==original.id),Is.False);Assert.That(reloaded.Any(d=>d.id==copy.id),Is.True);
    }
    private static DroneProfileEditBuffer EditBuffer(JToken source,JToken rule,string path="table")=>new DroneProfileEditBuffer(source,rule,path,new System.Collections.Generic.Dictionary<string,string>(),new System.Collections.Generic.Dictionary<string,string>());
    [Test] public void CancellingModalLeavesOriginalTableUnchanged()
    {
        var rows=new JArray(new JObject{["rpm"]=1000,["thrustN"]=1,["torqueNm"]=.1});var original=rows.DeepClone();
        var buffer=EditBuffer(rows,new JObject{["type"]="array",["items"]=DroneParameterSchema.ObjectRule("RpmPerformancePoint")});
        buffer.Value[0]["thrustN"]=2;((JArray)buffer.Value).Add(new JObject());
        Assert.That(buffer.Dirty,Is.True);Assert.That(JToken.DeepEquals(rows,original),Is.True);
    }
    [Test] public void ApplyingModalReturnsValidatedDetachedValue()
    {
        var source=new JObject{["rpm"]=1000,["thrustN"]=1,["torqueNm"]=.1};var buffer=EditBuffer(source,DroneParameterSchema.ObjectRule("RpmPerformancePoint"));
        buffer.Value["thrustN"]=2;var result=buffer.BuildValue();buffer.Value["thrustN"]=3;
        Assert.That((double)source["thrustN"],Is.EqualTo(1));Assert.That((double)result["thrustN"],Is.EqualTo(2));
    }
    [Test] public void ModalInputErrorsAreIsolatedAndRespectPathBoundaries()
    {
        var errors=new System.Collections.Generic.Dictionary<string,string>{["rotors[0].performance.rpmTable[0].rpm"]="Bad number",["rotors[0].performanceExtra"]="Other",["massProperties.massKg"]="Other"};
        var inputs=new System.Collections.Generic.Dictionary<string,string>{["rotors[0].performance.rpmTable[0].rpm"]="1e"};
        var buffer=new DroneProfileEditBuffer(new JObject(),new JObject{["type"]="object"},"rotors[0].performance",errors,inputs);
        Assert.That(buffer.Errors.Count,Is.EqualTo(1));Assert.Throws<ArgumentException>(()=>buffer.BuildValue());buffer.Errors.Clear();buffer.Inputs.Clear();
        Assert.That(errors.Count,Is.EqualTo(3));Assert.That(inputs.Count,Is.EqualTo(1));Assert.That(buffer.Dirty,Is.True);
    }
    [Test] public void ModalCannotApplyIncompleteRequiredRow()
    {
        var source=new JArray();var buffer=EditBuffer(source,new JObject{["type"]="array",["items"]=DroneParameterSchema.ObjectRule("RpmPerformancePoint")});
        ((JArray)buffer.Value).Add(new JObject{["rpm"]=1000});Assert.Throws<ArgumentException>(()=>buffer.BuildValue());Assert.That(source,Is.Empty);
    }
    [Test] public void ModalChecksArrayBoundsAndNumericRange()
    {
        var rule=new JObject{["type"]="array",["minItems"]=1,["maxItems"]=2,["items"]=new JObject{["type"]="number",["minimum"]=0}};
        var buffer=EditBuffer(new JArray(1),rule);Assert.That(buffer.ValidationError(),Is.Null);buffer.Value[0]=-1;Assert.Throws<ArgumentException>(()=>buffer.BuildValue());
        buffer.Value[0]=1;((JArray)buffer.Value).Add(2);((JArray)buffer.Value).Add(3);Assert.Throws<ArgumentException>(()=>buffer.BuildValue());
        ((JArray)buffer.Value).RemoveAll();Assert.Throws<ArgumentException>(()=>buffer.BuildValue());
    }

    [Test] public void EmptyOptionalMetadataCanBeSavedWithoutSchemaErrors()
    {
        var doc=DroneProfileLibrary.Create();var metadata=(JObject)doc.profile["metadata"];
        foreach(var key in new[]{"manufacturer","model","description"})DroneProfileEdits.SetOptionalText(metadata,key,"   ");
        Assert.That(DroneProfileLibrary.Validate(doc).Success,Is.True);DroneProfileLibrary.Save(doc,false);
        var stored=JObject.Parse(File.ReadAllText(Path.Combine(DroneProfileLibrary.Folder(doc.id),"profile.json")));
        foreach(var key in new[]{"manufacturer","model","description"})Assert.That(stored["metadata"][key],Is.Null);
        Assert.That(stored["metadata"]["name"],Is.Not.Null);
    }
    [Test] public void FilledOptionalMetadataIsPreserved()
    {
        var doc=DroneProfileLibrary.Create();DroneProfileEdits.SetOptionalText((JObject)doc.profile["metadata"],"manufacturer","Тестовый производитель");
        DroneProfileLibrary.Save(doc,false);Assert.That((string)doc.profile["metadata"]["manufacturer"],Is.EqualTo("Тестовый производитель"));
    }
    [Test] public void ValidationApprovalExpiresWhenDocumentChanges()
    {
        var doc=DroneProfileLibrary.Create();Assert.That(DroneProfileLibrary.Validate(doc).Success,Is.True);string checkedSnapshot=doc.Snapshot();
        Assert.That(DroneProfileEdits.MatchesValidation(doc,checkedSnapshot),Is.True);
        doc.profile["massProperties"]["massKg"]=2;Assert.That(DroneProfileEdits.MatchesValidation(doc,checkedSnapshot),Is.False);
    }
    [Test] public void UncheckedOrUnfinishedDocumentCannotUseValidationApproval()
    {
        var doc=DroneProfileLibrary.Create();Assert.That(DroneProfileEdits.MatchesValidation(doc,null),Is.False);
        doc.unfinishedInputs["massProperties.massKg"]="1e";Assert.That(DroneProfileEdits.MatchesValidation(doc,doc.Snapshot()),Is.False);
    }

    [Test] public void ModelScaleUsesCurrentBoundsAndIsIdempotent()
    {
        var physical=new JArray(.2,.3,.6);var bounds=new JArray(2,4,6);var before=bounds.DeepClone();
        double scale=DroneProfileEdits.UniformScaleToDimensions(physical,bounds,2);Assert.That(scale,Is.EqualTo(.2).Within(1e-12));
        var resized=new JArray(bounds.Select(v=>(double)v*scale/2));Assert.That(DroneProfileEdits.UniformScaleToDimensions(physical,resized,scale),Is.EqualTo(scale).Within(1e-12));
        Assert.That(JToken.DeepEquals(bounds,before),Is.True);Assert.That((double)physical[1],Is.EqualTo(.3));
    }
    [Test] public void ModelScaleRejectsUnusableBoundsAndInvalidDimensions()
    {
        Assert.Throws<ArgumentException>(()=>DroneProfileEdits.UniformScaleToDimensions(new JArray(1,2,3),new JArray(0,0,0),1));
        Assert.Throws<ArgumentException>(()=>DroneProfileEdits.UniformScaleToDimensions(new JArray(1,-2,3),new JArray(1,2,3),1));
        Assert.Throws<ArgumentException>(()=>DroneProfileEdits.UniformScaleToDimensions(new JArray(1,2,3),new JArray(1,2,3),double.NaN));
    }
    [Test] public void NormalizationPreservesDirectionAndDoesNotMutateSource()
    {
        var source=new JArray(3,4,0);Assert.That(DroneProfileEdits.TryNormalize(source,out var unit,out var error),Is.True);Assert.That(error,Is.Null);
        Assert.That((double)unit[0],Is.EqualTo(.6).Within(1e-12));Assert.That((double)unit[1],Is.EqualTo(.8).Within(1e-12));Assert.That((double)source[0],Is.EqualTo(3));
    }
    [Test] public void QuaternionNormalizationRejectsZeroAndProducesUnitLength()
    {
        Assert.That(DroneProfileEdits.TryNormalize(new JArray(0,0,0,0),out _,out var error),Is.False);Assert.That(error,Is.Not.Empty);
        Assert.That(DroneProfileEdits.TryNormalize(new JArray(1,2,3,4),out var unit,out _),Is.True);Assert.That(unit.Sum(v=>Math.Pow((double)v,2)),Is.EqualTo(1).Within(1e-12));
    }
    [Test] public void DroneProfileHasNoCurrentWeatherFieldsButKeepsMeasurementDensity()
    {
        var doc=DroneProfileLibrary.Create();
        foreach(var key in new[]{"environment","airDensityKgM3","temperatureK","pressurePa","altitudeM","wind","weather"})Assert.That(doc.profile[key],Is.Null);
        var measured=doc.profile["rotors"][0]["performance"];Assert.That((double)measured["referenceAirDensityKgM3"],Is.GreaterThan(0));
        Assert.That(DroneProfileLibrary.Validate(doc).Success,Is.True);
    }
    [Test] public void NumericHelpUsesPhysicalBoundsWithoutRuntimeImplementationText()
    {
        Assert.That(DroneParameterSchema.Range(new JObject{["type"]="number"}),Is.EqualTo("Конечное число"));
        Assert.That(DroneParameterSchema.Tooltip("principalAxesRotationXyzw",DroneParameterSchema.ObjectRule("InertiaProfile")["properties"]["principalAxesRotationXyzw"]),Does.Not.Contain("runtime"));
    }

}
