using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace DroneLab.UI
{
    /// <summary>Desktop StreamingAssets packs, extracted once into a content-addressed cache.</summary>
    public static class DroneBundledModels
    {
        private static readonly HashSet<string> verified=new HashSet<string>();
        private static readonly object gate=new object();
        public static string Resolve(string archives,string cache,JObject descriptor)
        {
            string archive=(string)descriptor?["archive"],file=(string)descriptor?["file"],sha=(string)descriptor?["sha256"];
            long size=(long?)descriptor?["sizeBytes"]??0;
            if(!BaseName(archive) || !archive.EndsWith(".zip",StringComparison.Ordinal) || !BaseName(file) || !file.EndsWith(".glb",StringComparison.Ordinal) || sha==null || sha.Length!=64 || size<=0 || size>256L*1024*1024)
                throw new ArgumentException("Повреждён каталог встроенных моделей.");
            foreach(char c in sha)if(!Uri.IsHexDigit(c))throw new ArgumentException("Некорректная контрольная сумма модели.");
            string folder=Path.Combine(cache,sha),path=Path.Combine(folder,file);
            lock(gate) {
                if(File.Exists(path) && new FileInfo(path).Length==size && (verified.Contains(path) || Matches(path,sha))){verified.Add(path);return path;}
                Directory.CreateDirectory(folder);
                string temporary=path+".tmp-"+Guid.NewGuid().ToString("N");
                try {
                    using(var zip=ZipFile.OpenRead(Path.Combine(archives,archive))) {
                        var entry=zip.GetEntry(file);
                        if(zip.Entries.Count!=1 || entry==null || entry.Length!=size)throw new InvalidDataException("Повреждён архив встроенной модели: "+archive);
                        using(var input=entry.Open())using(var output=File.Create(temporary)) {
                            var buffer=new byte[81920];long count=0;int read;
                            while((read=input.Read(buffer,0,buffer.Length))>0) {
                                count+=read;if(count>size)throw new InvalidDataException("Размер модели превышает указанный в каталоге.");output.Write(buffer,0,read);
                            }
                            if(count!=size)throw new InvalidDataException("Модель распакована не полностью.");
                        }
                    }
                    if(!Matches(temporary,sha))throw new InvalidDataException("Контрольная сумма встроенной модели не совпадает: "+archive);
                    if(File.Exists(path))File.Delete(path);File.Move(temporary,path);verified.Add(path);return path;
                } finally {if(File.Exists(temporary))File.Delete(temporary);}
            }
        }
        private static bool BaseName(string value)=>!string.IsNullOrEmpty(value) && value!="." && value!=".." && value.IndexOfAny(new[]{'/','\\',':'})<0 && !Path.IsPathRooted(value);
        private static bool Matches(string path,string expected)
        {
            using(var stream=File.OpenRead(path))using(var hash=SHA256.Create())
                return string.Equals(BitConverter.ToString(hash.ComputeHash(stream)).Replace("-",""),expected,StringComparison.OrdinalIgnoreCase);
        }
    }
}
