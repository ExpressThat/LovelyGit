using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace LovelyGit.Tools
{
    public sealed class KillOnCloseProcess : IDisposable
    {
        private const uint KillOnJobClose = 0x00002000;
        private readonly SafeFileHandle job;
        private readonly Process process;

        private KillOnCloseProcess(SafeFileHandle job, Process process)
        {
            this.job = job;
            this.process = process;
        }

        public int Id { get { return process.Id; } }
        public int ExitCode { get { return process.ExitCode; } }

        public static KillOnCloseProcess Start(
            string fileName,
            string[] arguments,
            string workingDirectory)
        {
            SafeFileHandle job = CreateJobObject(IntPtr.Zero, null);
            if (job.IsInvalid)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            try
            {
                ConfigureKillOnClose(job);
                Process process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = BuildArguments(arguments),
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                if (!process.Start())
                {
                    throw new InvalidOperationException("The process did not start.");
                }

                try
                {
                    if (!AssignProcessToJobObject(job, process.Handle))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
                catch
                {
                    process.Kill();
                    process.Dispose();
                    throw;
                }

                return new KillOnCloseProcess(job, process);
            }
            catch
            {
                job.Dispose();
                throw;
            }
        }

        public bool WaitForExit(int milliseconds)
        {
            return process.WaitForExit(milliseconds);
        }

        public void Dispose()
        {
            job.Dispose();
            process.Dispose();
        }

        private static void ConfigureKillOnClose(SafeFileHandle job)
        {
            JOBOBJECT_EXTENDED_LIMIT_INFORMATION information =
                new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
            information.BasicLimitInformation.LimitFlags = KillOnJobClose;
            int length = Marshal.SizeOf(information);
            IntPtr pointer = Marshal.AllocHGlobal(length);
            try
            {
                Marshal.StructureToPtr(information, pointer, false);
                if (!SetInformationJobObject(job, 9, pointer, (uint)length))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pointer);
            }
        }

        private static string BuildArguments(string[] arguments)
        {
            StringBuilder commandLine = new StringBuilder();
            foreach (string argument in arguments)
            {
                if (commandLine.Length > 0)
                {
                    commandLine.Append(' ');
                }
                commandLine.Append(Quote(argument));
            }
            return commandLine.ToString();
        }

        private static string Quote(string argument)
        {
            if (argument.Length > 0 && argument.IndexOfAny(new[] { ' ', '\t', '"' }) < 0)
            {
                return argument;
            }

            StringBuilder result = new StringBuilder("\"");
            int slashes = 0;
            foreach (char value in argument)
            {
                if (value == '\\')
                {
                    slashes++;
                }
                else
                {
                    result.Append('\\', value == '"' ? slashes * 2 + 1 : slashes);
                    result.Append(value);
                    slashes = 0;
                }
            }
            result.Append('\\', slashes * 2);
            result.Append('"');
            return result.ToString();
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateJobObject(IntPtr securityAttributes, string name);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(
            SafeFileHandle job,
            int informationClass,
            IntPtr information,
            uint informationLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(SafeFileHandle job, IntPtr process);

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public UIntPtr Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
            public IO_COUNTERS IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }
    }
}
