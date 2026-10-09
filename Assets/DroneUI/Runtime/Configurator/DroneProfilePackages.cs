using System;
using System.IO;
using System.IO.Compression;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DroneLab.UI
{
    public static class DroneProfilePackages
    {
        public static void Export(DroneProfileDocument document,string destination,string previewPath=null)
        {
            if(document==null)throw new ArgumentNullException(nameof(document));
            ExportResolved(document,destination,DroneProfileLibrary.ModelPath(document),
                previewPath??Path.Combine(DroneProfileLibrary.Folder(document.id),"preview.png"));
        }
        // Resolve Unity-owned paths before entering a worker thread.
        public static void ExportResolved(DroneProfileDocument document,string destination,string modelPath,string previewPath)
        {
            if(document==null)throw new ArgumentNullException(nameof(document));
            destination=Path.GetFullPath(destination);
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            string staging=Path.Combine(Path.GetTempPath(),"DroneLab-export-"+Guid.NewGuid().ToString("N"));
            string temporary=destination+".tmp-"+Guid.NewGuid().ToString("N");
            try {
                Directory.CreateDirectory(staging);var copy=document.Copy();
                string model=modelPath;
                if(model!=null) {
                    string copied=DroneModelFiles.Copy(model,Path.Combine(staging,"model"));
                    copy.visual["modelFile"]="model/"+Path.GetFileName(copied);
                }
                copy.sourceModel=null;copy.visual.Remove("bundledModel");
                File.WriteAllText(Path.Combine(staging,"document.json"),JsonConvert.SerializeObject(copy,Formatting.Indented));
                File.WriteAllText(Path.Combine(staging,copy.draft?"draft.json":"profile.json"),copy.profile.ToString());
                File.WriteAllText(Path.Combine(staging,"asset-bindings.json"),copy.visual.ToString());
                string preview=previewPath;
                if(File.Exists(preview))File.Copy(preview,Path.Combine(staging,"preview.png"));
                ZipFile.CreateFromDirectory(staging,temporary,CompressionLevel.Optimal,false);
                if(File.Exists(destination))File.Replace(temporary,destination,null);else File.Move(temporary,destination);
            } finally {
                if(Directory.Exists(staging))Directory.Delete(staging,true);
                if(File.Exists(temporary))File.Delete(temporary);
            }
        }
        public static DroneProfileDocument Import(string archive)
        {
            string folder=Path.Combine(DroneProfileLibrary.Root,".imports",Guid.NewGuid().ToString("N"));
            try {
                Directory.CreateDirectory(folder);
                using(var zip=ZipFile.OpenRead(archive)) {
                    long total=0;if(zip.Entries.Count>4096)throw new InvalidDataException("В архиве слишком много файлов.");
                    foreach(var entry in zip.Entries) {
                        string path=DroneModelFiles.Contained(folder,entry.FullName);
                        if(entry.FullName.EndsWith("/",StringComparison.Ordinal)){Directory.CreateDirectory(path);continue;}
                        total+=entry.Length;if(entry.Length<0 || total>512L*1024*1024)throw new InvalidDataException("Архив профиля слишком большой.");
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        using(var source=entry.Open())using(var output=new FileStream(path,FileMode.CreateNew)) {
                            var buffer=new byte[81920];long length=0;int count;
                            while((count=source.Read(buffer,0,buffer.Length))>0){length+=count;if(length>entry.Length)throw new InvalidDataException("Некорректный размер файла в архиве.");output.Write(buffer,0,count);}
                            if(length!=entry.Length)throw new InvalidDataException("Архив распакован не полностью.");
                        }
                    }
                }
                string documentPath=Path.Combine(folder,"document.json");
                var packageDocument=JObject.Parse(File.ReadAllText(documentPath));packageDocument["sourceModel"]=null;
                File.WriteAllText(documentPath,packageDocument.ToString());
                var document=DroneProfileLibrary.Import(documentPath);
                if(document.sourceModel!=null)DroneModelFiles.ValidateLocalModel(document.sourceModel);
                string preview=Path.Combine(folder,"preview.png");
                if(File.Exists(preview)) {
                    string target=DroneProfileLibrary.Folder(document.id);Directory.CreateDirectory(target);File.Copy(preview,Path.Combine(target,"preview.png"));
                }
                // Keep dependencies available until the imported draft is saved, including after closing the dialog.
                return document;
            } catch {if(Directory.Exists(folder))Directory.Delete(folder,true);throw;}
        }
    }
}
