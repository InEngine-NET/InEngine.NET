using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace InEngine
{
    [RunInstaller(true)]
    public class InEngineInstaller : Installer
    {
        public InEngineInstaller()
        {
            Installers.Add(new ServiceProcessInstaller() {
                Account = ServiceAccount.LocalSystem,
                Username = null,
                Password = null
            });

            Installers.Add(new ServiceInstaller() {
                DisplayName = "InEngine.NET",
                StartType = ServiceStartMode.Automatic,
                ServiceName = Program.ServiceName
            });
        }
    }
}
