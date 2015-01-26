using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;


namespace IntegrationEngine.ConsoleHost
{
    [RunInstaller(true)]
    public class InEngineServerInstaller : Installer
    {
        public InEngineServerInstaller()
        {
            var serviceProcessInstaller = new ServiceProcessInstaller();
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
            serviceProcessInstaller.Username = null;
            serviceProcessInstaller.Password = null;
            Installers.Add(serviceProcessInstaller);

            var serviceInstaller = new ServiceInstaller();
            serviceInstaller.DisplayName = "InEngine.NET Server";
            serviceInstaller.StartType = ServiceStartMode.Automatic;
            serviceInstaller.ServiceName = Program.ServiceName;
            Installers.Add(serviceInstaller);
        }
    }
}
