using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DroneLab.UI
{
    internal static class DroneFileDialog
    {
        internal sealed class Result { public string Path; public bool Failed; public string Error; }
        public static Task<Result> Pick(bool save, string directory)
        {
            Directory.CreateDirectory(directory);
#if UNITY_EDITOR
            try {
                string path = save ? UnityEditor.EditorUtility.SaveFilePanel("Экспорт профиля среды", directory, "environment.json", "json") :
                    UnityEditor.EditorUtility.OpenFilePanel("Импорт профиля среды", directory, "json");
                return Task.FromResult(new Result { Path = path });
            } catch (Exception ex) { return Task.FromResult(new Result { Failed = true, Error = ex.Message }); }
#elif UNITY_STANDALONE_WIN
            var completion = new TaskCompletionSource<Result>(); var owner = GetActiveWindow();
            var thread = new Thread(() => { try { completion.SetResult(Windows(save,directory,owner)); } catch (Exception ex) { completion.SetResult(new Result { Failed = true, Error = ex.Message }); } });
            thread.IsBackground = true; thread.SetApartmentState(ApartmentState.STA); thread.Start(); return completion.Task;
#elif UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            return Task.Run(() => Unix(save,directory));
#else
            return Task.FromResult(new Result { Failed = true, Error = "Системный файловый диалог недоступен на этой платформе." });
#endif
        }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private sealed class OpenFileName
        {
            public int size; public IntPtr owner, instance; public string filter; public IntPtr customFilter;
            public int customFilterSize, filterIndex; public IntPtr file; public int fileSize;
            public IntPtr fileTitle; public int fileTitleSize; public string directory, title; public int flags;
            public short fileOffset, extensionOffset; public string extension; public IntPtr customData, hook;
            public string template; public IntPtr reserved; public int reservedCount, flagsEx;
        }
        [DllImport("comdlg32.dll",CharSet=CharSet.Unicode,SetLastError=true)] [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetOpenFileNameW([In,Out] OpenFileName data);
        [DllImport("comdlg32.dll",CharSet=CharSet.Unicode,SetLastError=true)] [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetSaveFileNameW([In,Out] OpenFileName data);
        [DllImport("comdlg32.dll")] private static extern int CommDlgExtendedError();
        [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();
        private static Result Windows(bool save,string directory,IntPtr owner)
        {
            const int capacity = 32768; IntPtr buffer = Marshal.AllocHGlobal(capacity * 2);
            try {
                byte[] initial = Encoding.Unicode.GetBytes((save ? "environment.json" : "") + "\0"); Marshal.Copy(initial,0,buffer,initial.Length);
                var data = new OpenFileName { owner = owner, size = Marshal.SizeOf(typeof(OpenFileName)), file = buffer, fileSize = capacity,
                    filter = "Профиль среды (*.json)\0*.json\0\0", filterIndex = 1, directory = directory, extension = "json",
                    title = save ? "Экспорт профиля среды" : "Импорт профиля среды",
                    flags = 0x00080000 | 0x00000008 | 0x00000800 | (save ? 0x00000002 : 0x00001000) };
                bool ok = save ? GetSaveFileNameW(data) : GetOpenFileNameW(data);
                int error = ok ? 0 : CommDlgExtendedError();
                return new Result { Path = ok ? Marshal.PtrToStringUni(buffer) : "", Failed = error != 0, Error = error == 0 ? null : "Ошибка файлового диалога: " + error };
            } finally { Marshal.FreeHGlobal(buffer); }
        }
#endif
#if (UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX) && !UNITY_EDITOR
        private static string Quote(string arg) => "\"" + arg.Replace("\\","\\\\").Replace("\"","\\\"") + "\"";
        private static Result Run(string program, string[] args, string input = null)
        {
            try {
                using (var process = new Process { StartInfo = new ProcessStartInfo(program, string.Join(" ",Array.ConvertAll(args,Quote))) {
                    UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = input != null } }) {
                    process.Start(); if (input != null) { process.StandardInput.Write(input); process.StandardInput.Close(); }
                    var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
                    process.WaitForExit(); Task.WaitAll(output,error);
                    string diagnostics = error.Result.ToLowerInvariant();
                    bool displayFailure = diagnostics.Contains("cannot open display") || diagnostics.Contains("could not connect to display") || diagnostics.Contains("no display");
                    bool cancel = (program == "zenity" || program == "kdialog") && process.ExitCode == 1 && !displayFailure || error.Result.Contains("(-128)");
                    return new Result { Path = process.ExitCode == 0 ? output.Result.Trim() : "", Failed = process.ExitCode != 0 && !cancel, Error = error.Result };
                }
            } catch (Exception ex) { return new Result { Failed = true, Error = ex.Message }; }
        }
        private static Result Unix(bool save,string directory)
        {
#if UNITY_STANDALONE_OSX
            // Fixed AppleScript; the directory is a separate argument, never source code.
            string script = "on run argv\ntry\nset baseFolder to (POSIX file (item 1 of argv)) as alias\n" +
                (save ? "set chosen to choose file name with prompt \"Экспорт профиля среды\" default name \"environment.json\" default location baseFolder\n" :
                        "set chosen to choose file with prompt \"Импорт профиля среды\" of type {\"json\"} default location baseFolder\n") +
                "return POSIX path of chosen\non error number -128\nreturn \"\"\nend try\nend run";
            return Run("/usr/bin/osascript",new[] { "-",directory },script);
#else
            var args = new System.Collections.Generic.List<string> { "--file-selection", "--title=" + (save ? "Экспорт профиля среды" : "Импорт профиля среды"),
                "--filename=" + Path.Combine(directory,save ? "environment.json" : ""),"--file-filter=JSON | *.json" };
            if (save) { args.Add("--save"); args.Add("--confirm-overwrite"); }
            var result = Run("zenity",args.ToArray());
            if (!result.Failed) return result;
            return Run("kdialog",new[] { save ? "--getsavefilename" : "--getopenfilename", Path.Combine(directory,save ? "environment.json" : ""),"*.json|Профиль среды" });
#endif
        }
#endif
    }
}
