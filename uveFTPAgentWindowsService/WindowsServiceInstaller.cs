using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;
using System.Collections;
using Microsoft.Win32;

namespace uveFTPAgentWindowsService
{
  [RunInstaller(true)]
  public class WindowsServiceInstaller : Installer
  {
    /// <summary>

    /// Public Constructor for WindowsServiceInstaller.

    /// - Put all of your Initialization code here.

    /// </summary>

    ServiceProcessInstaller serviceProcessInstaller = new ServiceProcessInstaller();
    ServiceInstaller serviceInstaller = new ServiceInstaller();

    public WindowsServiceInstaller()
    {
      //# Service Account Information

      serviceProcessInstaller.Account = ServiceAccount.LocalSystem;
      serviceProcessInstaller.Username = null;
      serviceProcessInstaller.Password = null;

      //# Service Information

      serviceInstaller.DisplayName = "uveFTPAgent Service";
      serviceInstaller.StartType = ServiceStartMode.Automatic;

      //# This must be identical to the WindowsService.ServiceBase name
      //# set in the constructor of WindowsService.cs
      serviceInstaller.ServiceName = "uveFTPAgent Service";
      serviceInstaller.Description = "Agent for the FTP service of ConnectA";

      this.Installers.Add(serviceProcessInstaller);
      this.Installers.Add(serviceInstaller);
    }

    /// <summary>
    /// El programa installutil.exe no permite especficar parámetros adicionales (como el .ini).
    /// Lo que se hace en este método es modificar la clave de registro "imagePath" para incluirlo 
    /// (si está definido).
    /// </summary>
    /// <param name="stateServer"></param>
    public override void Install(IDictionary stateServer)
    {
      base.Install(stateServer);

      // Definir claves registro
      RegistryKey system,
        //HKEY_LOCAL_MACHINE\Services\CurrentControlSet

                      currentControlSet,
        //...\Services

                      services,
        //...\<Service Name>

                      service;
      
      system = Registry.LocalMachine.OpenSubKey("System");
      currentControlSet = system.OpenSubKey("CurrentControlSet");
      services = currentControlSet.OpenSubKey("Services");
      // Abrir clave de servicio
      service = services.OpenSubKey(this.serviceInstaller.ServiceName, true);
      service.SetValue("Description", this.serviceInstaller.Description);

      // Modificar el path del servicio
      String[] args = System.Environment.GetCommandLineArgs();
      string iniFile = null;
      foreach (string s in args)
      {
        if (s.ToLower().StartsWith("/inifile="))
          iniFile = s.Substring("/inifile=".Length);
      }
      Console.WriteLine("ImagePath: " + service.GetValue("ImagePath"));
      string imagePath = (string)service.GetValue("ImagePath");
      if (iniFile != null)
      {
        imagePath += " " + iniFile;
        service.SetValue("ImagePath", imagePath);
      }

      // Close keys
      service.Close();
      services.Close();
      currentControlSet.Close();
    }

    /// <summary>
    /// Antes de instalar...
    /// </summary>
    /// <param name="savedState"></param>
    protected override void OnBeforeInstall(IDictionary savedState)
    {
      String[] args = System.Environment.GetCommandLineArgs();
      String no_log_file = null;
      InstallContext tmp_ctx = new InstallContext(no_log_file, args);

      //Teóricamente esto debería instalar el servicio con el parámetro de inicio ajustado
      foreach (DictionaryEntry de in tmp_ctx.Parameters)
      {
        // don’t process empty Values, except for password
        if (!de.Key.ToString().Equals("password") && (null == de.Value || string.Empty == de.Value.ToString()))
          continue;
        else
        {
          if (!this.Context.Parameters.ContainsKey(de.Key.ToString()))
            this.Context.Parameters[de.Key.ToString()] = de.Value.ToString();
        }
      }
      base.OnBeforeInstall(savedState);
    }
  }
}