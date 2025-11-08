using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace NtfyService {
    public partial class NtfyNotifMe : ServiceBase {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        /*
         * As User
         */
        [DllImport("Wtsapi32.dll")]
        private static extern bool WTSQueryUserToken(uint sessionId, out IntPtr Token);

        [DllImport("kernel32.dll")]
        private static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        public enum ServiceState {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };

        /*
         * As User
         */
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct STARTUPINFO {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        //private Process ntfyProcess;
        private readonly Dictionary<uint, Process> ntfyProcesses = new Dictionary<uint, Process>();
        private readonly string ntfyRoot;
        private readonly string ntfyPath;
        private readonly string ntfyArgs;
        private bool stopping = false;

        public NtfyNotifMe() {
            InitializeComponent();

            ntfyRoot = @"C:\Program Files\NotifMe";
            ntfyPath = $@"{ntfyRoot}\ntfy.exe";
            ntfyArgs = $@"subscribe --config=""{ntfyRoot}\client.yml"" --from-config";

            ServiceName = "NtfyService";
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
            CanHandleSessionChangeEvent = true;

            eventLog = new EventLog();
            if (!EventLog.SourceExists("NotifMe")) {
                EventLog.CreateEventSource("NotifMe", "Application");
            }

            eventLog.Source = "NotifMe";
            eventLog.Log = "Application";
        }

        protected override void OnStart(string[] args) {
            ServiceStatus serviceStatus = new ServiceStatus {
                dwCurrentState = ServiceState.SERVICE_START_PENDING,
                dwWaitHint = 100000
            };
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            eventLog.WriteEntry("Service start", EventLogEntryType.Information);

            uint sessionId = WTSGetActiveConsoleSessionId();

            if (sessionId == 0xFFFFFFFF) {
                eventLog.WriteEntry("No active session at startup, waiting for user logon",
                    EventLogEntryType.Information);
            } else {
                StartNtfyForSession(sessionId);
            }

            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            // StartNtfyProcess();
        }

        protected override void OnStop() {
            ServiceStatus serviceStatus = new ServiceStatus {
                dwCurrentState = ServiceState.SERVICE_STOP_PENDING,
                dwWaitHint = 100000
            };
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            eventLog.WriteEntry("Service stop", EventLogEntryType.Information);

            stopping = true;
            StopAllNtfyProcesses();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            //StopNtfyProcess();
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription) {
            base.OnSessionChange(changeDescription);

            switch (changeDescription.Reason) {
                case SessionChangeReason.SessionLogon:
                    // Un utilisateur vient de se connecter
                    eventLog.WriteEntry($"User logged on (Session {changeDescription.SessionId})",
                        EventLogEntryType.Information);
                    StartNtfyForSession((uint)changeDescription.SessionId);
                    break;

                case SessionChangeReason.SessionLogoff:
                    // Un utilisateur vient de se déconnecter
                    eventLog.WriteEntry($"User logged off (Session {changeDescription.SessionId})",
                        EventLogEntryType.Information);
                    StopNtfyForSession((uint)changeDescription.SessionId);
                    break;
            }
        }

        /*private void StartNtfyProcess() {
            try {
                uint sessionId = WTSGetActiveConsoleSessionId();
                if (!WTSQueryUserToken(sessionId, out IntPtr userToken)) {
                    EventLog.WriteEntry("Cannot get user token", EventLogEntryType.Error);
                    return;
                }

                STARTUPINFO si = new STARTUPINFO();
                si.cb = Marshal.SizeOf(si);
                si.lpDesktop = @"winsta0\default";
                si.dwFlags = 0x00000001; // STARTF_USESHOWWINDOW
                si.wShowWindow = 0;      // SW_HIDE


                string commandLine = $"\"{ntfyPath}\" {ntfyArgs}";

                bool success = CreateProcessAsUser(
                    userToken,
                    null,
                    commandLine,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    0,
                    IntPtr.Zero,
                    ntfyRoot,
                    ref si,
                    out PROCESS_INFORMATION pi);

                if (!success) {
                    EventLog.WriteEntry($"Error CreateProcessAsUser: {Marshal.GetLastWin32Error()}", EventLogEntryType.Error);
                    return;
                }

                ntfyProcess = Process.GetProcessById((int)pi.dwProcessId);
                ntfyProcess.EnableRaisingEvents = true;
                ntfyProcess.Exited += OnProcessExited;

                EventLog.WriteEntry($"ntfy started in user context (PID: {ntfyProcess.Id})", EventLogEntryType.Information);
            } catch (Exception ex) {
                EventLog.WriteEntry($"Starting error: {ex.Message}", EventLogEntryType.Error);
            }
        }*/

        private void StartNtfyForSession(uint sessionId) {
            try {
                // Vérifie si un processus ntfy existe déjà pour cette session
                if (ntfyProcesses.ContainsKey(sessionId)) {
                    eventLog.WriteEntry($"ntfy already running for session {sessionId}",
                        EventLogEntryType.Information);
                    return;
                }

                // Récupère le token de l'utilisateur de cette session
                if (!WTSQueryUserToken(sessionId, out IntPtr userToken)) {
                    eventLog.WriteEntry($"Cannot get user token for session {sessionId}: {Marshal.GetLastWin32Error()}",
                        EventLogEntryType.Error);
                    return;
                }

                STARTUPINFO si = new STARTUPINFO();
                si.cb = Marshal.SizeOf(si);
                si.lpDesktop = @"winsta0\default";
                si.dwFlags = 0x00000001; // STARTF_USESHOWWINDOW
                si.wShowWindow = 0;      // SW_HIDE

                string commandLine = $"\"{ntfyPath}\" {ntfyArgs}";

                bool success = CreateProcessAsUser(
                    userToken,
                    null,
                    commandLine,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    0,
                    IntPtr.Zero,
                    ntfyRoot,
                    ref si,
                    out PROCESS_INFORMATION pi);

                if (!success) {
                    eventLog.WriteEntry($"Error CreateProcessAsUser for session {sessionId}: {Marshal.GetLastWin32Error()}",
                        EventLogEntryType.Error);
                    return;
                }

                // Récupère le processus créé et configure la surveillance
                Process ntfyProcess = Process.GetProcessById((int)pi.dwProcessId);
                ntfyProcess.EnableRaisingEvents = true;

                // Capture la sessionId dans le lambda pour savoir quelle session a planté
                //ntfyProcess.Exited += (sender, e) => OnProcessExited(sender, e, sessionId);
                ntfyProcess.Exited += (sender, e) => OnProcessExited(sessionId);

                // Stocke le processus dans le dictionnaire avec son ID de session
                ntfyProcesses[sessionId] = ntfyProcess;

                eventLog.WriteEntry($"ntfy started for session {sessionId} (PID: {ntfyProcess.Id})",
                    EventLogEntryType.Information);

            } catch (Exception ex) {
                eventLog.WriteEntry($"Starting error for session {sessionId}: {ex.Message}",
                    EventLogEntryType.Error);
            }
        }

        /*private void OnProcessExited(object sender, EventArgs e) {
            if (stopping && !this.CanStop)
                return;

            EventLog.WriteEntry("Process ntfy stopped. Restarting...", EventLogEntryType.Warning);
            EventLog.WriteEntry(ntfyProcess.StandardOutput.ReadToEnd(), EventLogEntryType.Information);
            EventLog.WriteEntry(ntfyProcess.StandardError.ReadToEnd(), EventLogEntryType.Information);
            System.Threading.Thread.Sleep(5000);
            StartNtfyProcess();
        }*/

        //private void OnProcessExited(object sender, EventArgs e, uint sessionId) {
        private void OnProcessExited(uint sessionId) {
            // Si le service est en cours d'arrêt, ne pas redémarrer
            if (stopping)
                return;

            eventLog.WriteEntry($"Process ntfy stopped unexpectedly for session {sessionId}. Restarting...",
                EventLogEntryType.Warning);

            // Nettoie l'entrée du dictionnaire
            if (ntfyProcesses.ContainsKey(sessionId)) {
                ntfyProcesses[sessionId].Dispose();
                ntfyProcesses.Remove(sessionId);
            }

            // Attend 5 secondes avant de redémarrer (évite les boucles infinies en cas d'erreur)
            System.Threading.Thread.Sleep(5000);

            // Redémarre le processus pour cette session
            StartNtfyForSession(sessionId);
        }

        /*private void StopNtfyProcess() {
            if (ntfyProcess != null && !ntfyProcess.HasExited) {
                ntfyProcess.Kill();
                ntfyProcess.WaitForExit(5000);
                ntfyProcess.Dispose();
            }
        }*/

        private void StopNtfyForSession(uint sessionId) {
            if (ntfyProcesses.TryGetValue(sessionId, out Process process)) {
                try {
                    if (!process.HasExited) {
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                    process.Dispose();
                    ntfyProcesses.Remove(sessionId);

                    eventLog.WriteEntry($"ntfy stopped for session {sessionId}",
                        EventLogEntryType.Information);
                } catch (Exception ex) {
                    eventLog.WriteEntry($"Error stopping ntfy for session {sessionId}: {ex.Message}",
                        EventLogEntryType.Error);
                }
            }
        }

        private void StopAllNtfyProcesses() {
            foreach (var sessionId in new List<uint>(ntfyProcesses.Keys)) {
                StopNtfyForSession(sessionId);
            }
        }

        private void EventLog1_EntryWritten(object sender, EntryWrittenEventArgs e) {

        }
    }
}
