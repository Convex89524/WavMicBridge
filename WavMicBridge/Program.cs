using System;
using System.Windows.Forms;
using CMLS.CLogger;

namespace WavMicBridge
{
    internal static class Program
    {
        private static void init()
        {
            var composite = new CompositeAppender();
            composite.AddAppender(new ConsoleAppender());
            LogManager.Configure(LogLevel.DEBUG, composite);
            Clogger.SetGlobalLevel(LogLevel.DEBUG);
        }
        
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}