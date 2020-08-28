using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace UnityVRPatcher
{
    internal static class Program
    {
        [STAThread]
        private static void Main(List<string> args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var patcher = new UnityVRPatcher();
        }

        [DllImport("User32.dll")]
        public static extern int SetForegroundWindow(IntPtr point);
    }

    internal class UnityVRPatcher
    {
        internal DirectoryInfo gameDir;

        internal UnityVRPatcher()
        {
            // sdakfjniksodf
        }
    }
}