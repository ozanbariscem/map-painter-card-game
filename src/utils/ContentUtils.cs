using Godot;
using System;
using System.IO;
using MoonSharp.Interpreter;
using System.Collections.Generic;

namespace Utils
{
    public static class ContentUtils
    {
        public static string CurrentFolder = OS.GetExecutablePath().GetBaseDir();

        public static List<string> GetScriptNamesOnFolder(string path)
        {
            path = System.IO.Path.Combine(CurrentFolder, path);
            if (!System.IO.Directory.Exists(path))
            {
                GD.PushError($"Hey make sure {path} exists!");
                return null;
            }

            List<string> strings = new List<string>();
            DirectoryInfo directory = new DirectoryInfo(path);
            foreach (var file in directory.GetFiles())
            {
                if (file.FullName.EndsWith(".lua"))
                {
                    strings.Add(System.IO.Path.GetFileNameWithoutExtension(file.Name));
                }
            }
            return strings;
        }

        public static string GetString(string path)
        {
            path = System.IO.Path.Combine(CurrentFolder, path);
            if (!System.IO.File.Exists(path))
            {
                GD.PushError($"Hey make sure {path} exists!");
                return null;
            }

            StreamReader sr = new StreamReader(path);
            string json = sr.ReadToEnd();
            sr.Close();

            return json;
        }

        public static MoonSharp.Interpreter.Script GetScript(string path)
        {
            string json = GetString(path);
            if (json == null) return null;

            UserData.RegisterType<EventArgs>();
            UserData.RegisterType<CardType>();
            var script = new MoonSharp.Interpreter.Script();

            script.Globals[nameof(CardType)] = UserData.CreateStatic<CardType>();

            script.Globals["Log"] = (Action<object[]>)GD.Print;
            script.Globals["LogError"] = (Action<string>)GD.PushError;
            script.Globals["LogWarning"] = (Action<string>)GD.PushWarning;
            
            script.DoString(json);
            return script;
        }
    }
}
