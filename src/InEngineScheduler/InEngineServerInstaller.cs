using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace InEngineScheduler
{
    [RunInstaller(true)]
    public class InEngineServerInstaller : Installer
    {
        public InEngineServerInstaller()
        {
            Installers.Add(new ServiceProcessInstaller() {
                Account = ServiceAccount.LocalSystem,
                Username = null,
                Password = null
            });

            Installers.Add(new ServiceInstaller() {
                DisplayName = "InEngine.NET Server",
                StartType = ServiceStartMode.Automatic,
                ServiceName = Program.ServiceName
            });
        }
    }
}
