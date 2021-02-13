using UnityEngine;
using System;
using System.Collections;
using com.cozyhome.Systems;
using com.cozyhome.Singleton;
using System.Collections.Generic;

namespace com.cozyhome.Console
{
    [DefaultExecutionOrder(-2000)]
    public class MonoConsole : SingletonBehaviour<MonoConsole>
    {
        [Header("Console References")]
        [SerializeField] MonoPrinter Printer;

        private Dictionary<string, ConsoleHeader.Command> Commands
            = new Dictionary<string, ConsoleHeader.Command>() { };

        protected override void OnAwake()
        {
            Printer.CacheLines();
            Printer.Write("Console initialized at frame: " + Time.frameCount);
        }

        private void AttemptInvokation(string rawinput)
        {
            string[] keys = ConsoleHeader.Parse(rawinput);

            // if action is described:
            if (keys.Length > 0)
            {
                // if action exists:
                if (Commands.TryGetValue(keys[0], out ConsoleHeader.Command cmd))
                {
                    int size = keys.Length - 1;
                    size = Mathf.Max(size, 0);
                    string[] modifiers = new string[size];

                    // deep copy strings bc C# forces me to
                    for (int i = 0; i < size; i++)
                        modifiers[i] = keys[i + 1];

                    cmd.Invoke(modifiers, out string output);

                    WriteToScreen(output);
                }
            }
        }

        private void InsertCMD(string key, ConsoleHeader.Command command)
            => Commands?.Add(key.ToLower(), command);

        private void RemoveCMD(string key)
            => Commands?.Remove(key.ToLower());

        private void WriteToScreen(string output)
            => Printer.Write(output);

        public static void InsertCommand(string key, ConsoleHeader.Command command)
            => _instance.InsertCMD(key, command);

        public static void RemoveCommand(string key)
            => _instance.RemoveCMD(key);

        public void AppendCommandString(string inputString)
        =>
            Printer.AppendCommandString(inputString);

        public void RemoveCharacterFromString(int amt)
        =>
            Printer.RemoveCharactersFromString(amt);
    }
}