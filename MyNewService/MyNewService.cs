using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

public enum ServiceState
{
    SERVICE_STOPPED = 0x00000001,
    SERVICE_START_PENDING = 0x00000002,
    SERVICE_STOP_PENDING = 0x00000003,
    SERVICE_RUNNING = 0x00000004,
    SERVICE_CONTINUE_PENDING = 0x00000005,
    SERVICE_PAUSE_PENDING = 0x00000006,
    SERVICE_PAUSED = 0x00000007,
}

[StructLayout(LayoutKind.Sequential)]
public struct ServiceStatus
{
    public int dwServiceType;
    public ServiceState dwCurrentState;
    public int dwControlsAccepted;
    public int dwWin32ExitCode;
    public int dwServiceSpecificExitCode;
    public int dwCheckPoint;
    public int dwWaitHint;
};

namespace MyNewService
{
    public partial class MyNewService : ServiceBase
    {
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        private int eventId = 1;

        public MyNewService(string[] args)
        {
            InitializeComponent();

            string eventSourceName = "MySource";
            string logName = "MyNewLog";

            if (args.Length > 0)
            {
                eventSourceName = args[0];
            }

            if (args.Length > 1)
            {
                logName = args[1];
            }

            eventLog1 = new System.Diagnostics.EventLog();

            if (!System.Diagnostics.EventLog.SourceExists(eventSourceName))
            {
                System.Diagnostics.EventLog.CreateEventSource(eventSourceName, logName);
            }

            eventLog1.Source = eventSourceName;
            eventLog1.Log = logName;
        }

        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);

            eventLog1.WriteEntry("In OnStart");

            // Set up a timer that triggers every minute.
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 30000; // 30 seconds
            timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
            timer.Start();

            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        public void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            // TODO: Insert monitoring activities here.
            eventLog1.WriteEntry("Monitoring the System", EventLogEntryType.Information, eventId++);
            string caminho = @"D:\inetpub\";
            foreach (string pastaCliente in Directory.GetDirectories(caminho))
            {
                foreach (string pastasInterna in Directory.GetDirectories(pastaCliente))
                {
                    if(pastasInterna.Remove(0, pastaCliente.Length).ToLower() == @"\imagens")
                    {
                        // Dar permissão a pasta
                        // Create a new DirectoryInfo object.
                        DirectoryInfo dInfo = new DirectoryInfo(pastasInterna);

                        DirectorySecurity oDirSec = Directory.GetAccessControl(pastasInterna);

                        // Define o usuário Everyone (Todos)
                        SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                        //SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
                        NTAccount oAccount = sid.Translate(typeof(NTAccount)) as NTAccount;

                        oDirSec.PurgeAccessRules(oAccount);

                        FileSystemAccessRule fsAR = new FileSystemAccessRule("IIS_IUSRS",
                                                                             FileSystemRights.Modify,
                                                                             InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                                                                             PropagationFlags.None,
                                                                             AccessControlType.Allow);

                        // Atribui a regra de acesso alterada
                        oDirSec.SetAccessRule(fsAR);
                        Directory.SetAccessControl(pastasInterna, oDirSec);

                        // USUARIO IUSR
                        fsAR = new FileSystemAccessRule("IUSR",
                                                        FileSystemRights.Modify,
                                                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                                                        PropagationFlags.None,
                                                        AccessControlType.Allow);

                        // Atribui a regra de acesso alterada
                        oDirSec.SetAccessRule(fsAR);
                        Directory.SetAccessControl(pastasInterna, oDirSec);

                        eventLog1.WriteEntry(oDirSec.ToString(), EventLogEntryType.Information);

                        /*
                        // Get a DirectorySecurity object that represents the 
                        // current security settings.
                        DirectorySecurity dSecurity = dInfo.GetAccessControl();

                        // Add the FileSystemAccessRule to the security settings. 
                        dSecurity.AddAccessRule(new FileSystemAccessRule(@"WIN10-EWERTON\Usuários",
                                                                        FileSystemRights.FullControl,
                                                                        AccessControlType.Allow));

                        */

                        eventLog1.WriteEntry(pastasInterna.ToString() + " adicionado direito", EventLogEntryType.Information);
                    } else
                    {
                        eventLog1.WriteEntry(pastasInterna.Remove(0, pastaCliente.Length).ToLower() + " Não é pasta imagem", EventLogEntryType.Information);
                    }
                }
            }
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("In OnStop.");
        }

        protected override void OnContinue()
        {
            eventLog1.WriteEntry("In OnContinue.");
        }
    }
}
