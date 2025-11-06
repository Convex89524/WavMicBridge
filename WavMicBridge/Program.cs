/*
 * This file is part of WavMicBridge.
 * Copyright (C) 2025-2029 Convex89524
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using CMLS.CLogger;

namespace WavMicBridge
{
    internal static class Program
    {
        private static void init()
        {
            var composite = new CompositeAppender();
            composite.AddAppender(new ConsoleAppender());
            composite.AddAppender(new FileAppender("logs"));
            LogManager.Configure(LogLevel.DEBUG, composite);
            Clogger.SetGlobalLevel(LogLevel.DEBUG);
        }
        
        [STAThread]
        static void Main()
        {
            init();
            
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}