using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Neo.Common
{
    public class CommandLineTool
    {

        public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static readonly bool IsMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        public static readonly ConcurrentQueue<Process> CurrentProcesses = new ConcurrentQueue<Process>();

        private static string shell
        {
            get
            {
                return IsWindows ? "cmd" : "bash";
            }
        }

        private static string shellArg
        {
            get
            {
                return IsWindows ? "/c " : "-c ";
            }
        }
        

        public static Process Run(string command, string workDirectory = "")
        {
            Process p = new Process();
            p.StartInfo.FileName = shell;
            p.StartInfo.Arguments = shellArg + command;
            p.StartInfo.WorkingDirectory = workDirectory;
            p.OutputDataReceived += (s, r) =>
            {
                if (r.Data == null)
                {
                    Console.WriteLine($"close");
                    p = null;
                    return;
                }
                Console.WriteLine(r.Data);
            };
            p.Start();
            CurrentProcesses.Enqueue(p);
            return p;
        }

        public static Process Run(string command, string workDirectory = "", Action<string> receiveOutput = null)
        {
            Process p = new Process();
            //Set the application to start
            p.StartInfo.FileName = shell;
            p.StartInfo.WorkingDirectory = workDirectory;
            //Whether to use operating system shell to start
            p.StartInfo.UseShellExecute = false;
            //Accept input information from the calling program
            p.StartInfo.RedirectStandardInput = true;
            //Output information
            p.StartInfo.RedirectStandardOutput = true;
            //Output errors
            p.StartInfo.RedirectStandardError = true;
            //p.StartInfo.StandardOutputEncoding=Encoding.Unicode;
            ;
            //Do not show program window
            //p.StartInfo.CreateNoWindow = true;
            p.OutputDataReceived += (s, r) =>
            {
                if (r.Data == null)
                {
                    Console.WriteLine($"close");
                    p = null;
                    return;
                }
                //Console.WriteLine(r.Data);
                //receiveOutput?.Invoke(r.Data);
            };
            //Start the program
            p.Start();
            p.StandardInput.WriteLine(command);
            p.BeginOutputReadLine();
            CurrentProcesses.Enqueue(p);
            return p;
        }


        public static void Close()
        {
            foreach (var currentProcess in CurrentProcesses)
            {
                currentProcess.Kill();
            }
        }
    }
}
