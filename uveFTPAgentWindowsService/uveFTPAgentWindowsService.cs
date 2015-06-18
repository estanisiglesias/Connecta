using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using uveFTPAgent;

namespace uveFTPAgentWindowsService
{
  /// <summary>
  /// Clase que se ejecuta como servicio de Windows. El objetivo es lanzar
  /// el uveFTPAgent.
  /// </summary>
  public partial class uveFTPAgentWindowsService : ServiceBase
  {
      private uveFTPAgent.uveFTPAgent ftpAgent = null;
      private System.Threading.Thread myThread = null;
      private string[] arguments = null;

    /// <summary>
    /// Constructor
    /// </summary>
    public uveFTPAgentWindowsService()
    {
      InitializeComponent();
      this.ServiceName = "uveFTPAgent Service";
      this.EventLog.Log = "Application";
      //this.CanHandlePowerEvent = true;
      //this.CanHandleSessionChangeEvent = true;
      //this.CanPauseAndContinue = true;
      this.CanShutdown = true;
      this.CanStop = true;
    }

    /// <summary>
    /// Inicio de servicio
    /// </summary>
    /// <param name="args">argumentos</param>
    protected override void OnStart(string[] args)
    {
      base.OnStart(args);

#if (DEBUG)
      System.Diagnostics.Debugger.Launch(); //<-- Simple form to debug the service 
#endif

      //Ver: http://msdn.microsoft.com/en-us/library/system.serviceprocess.servicebase.onstart.aspx
      //
      //The arguments in the args parameter array can be set manually in the properties window for the service in the Services console. 
      //The arguments entered in the console are not saved; they are passed to the service on a one-time basis when the service is started from the control panel. 
      //Arguments that must be present when the service is automatically started can be placed in the ImagePath string value for the service's 
      //registry key (HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\<service name>). 
      //You can obtain the arguments from the registry using the GetCommandLineArgs method, 
      //  for example: string[] imagePathArgs = Environment.GetCommandLineArgs();

      //Si los parámetros no llegaran, cogería el INI del directorio de ejecución (que es donde deberían estar todos los ejecutables)
      string[] imagePathArgs = Environment.GetCommandLineArgs();
      if (imagePathArgs.Length > 1)
      {
            //El segundo parámetro es el ".ini" (que puede ocupar mas de una posición si el path tiene espacios)
            //El primer parametro lo ignoramos
            this.arguments = new string[imagePathArgs.Length - 1];
            int iCont = 0;
            foreach (string s in imagePathArgs)
            {
                if (iCont > 0)
                {
                    arguments[iCont-1] = s;
                }
                iCont++;
            }
        }

      //Lanzar un thread con el proceso de integración
      System.Threading.ThreadStart threadStart = new System.Threading.ThreadStart(run_uveFTPAgent);
      myThread = new System.Threading.Thread(threadStart);
      myThread.Start();
    }

    /// <summary>
    /// Invocar a modo automático...
    /// </summary>
    public void run_uveFTPAgent()
    {
      //Arrancar el proceso automático
        ftpAgent = new uveFTPAgent.uveFTPAgent();
        ftpAgent.runAuto(arguments);
    }

    /// <summary>
    /// Detener el servicio
    /// </summary>
    protected override void OnStop()
    {
      if (ftpAgent != null)
        ftpAgent.StopAuto();
    }
  }
}
