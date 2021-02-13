using System;
namespace com.cozyhome.Console
{
    public static class ConsoleHeader
    {
        public enum ConsoleState { Enabled = 0, Disabled = 1, Transitioning = 2 };
        public delegate void Command(string[] modifiers, out string output);

        public static string[] Parse(string rawinput)
        {
            // find action and subsequent modifiers
            rawinput = rawinput.ToLower();
            string[] keys = rawinput.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return keys;
        }
    }
}
