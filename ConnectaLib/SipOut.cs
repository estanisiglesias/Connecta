using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectaLib
{
  /// <summary>
  /// Clase base para todos los SIP's de salida (SipOut*)
  /// </summary>
  public abstract class SipOut
  {
    public const string FORMAT_XML = "xml";
    public const string FORMAT_DELIMITED = "txt";

    //Agente
    protected string agent = "";

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="agent">agente</param>
    public SipOut(string agent) 
    {
        this.agent = agent;
    }

    /// <summary>
    /// Pre-proceso. Debe ser invocado antes de lanzar la l�gica de negocio del WebService.
    /// </summary>
    /// <param name="sipAgent">agente</param>
    /// <param name="sipType">SIP</param>
    /// <returns>id de mensaje asignado</returns>
    public string PreProcessWebService(string sipAgent, string sipType) 
    {
        //Buscar el fichero "ini" en el fichero Web.Config
        string iniFile = System.Configuration.ConfigurationManager.AppSettings["uveIntegrator_IniFile"];

        //Crear una nueva instancia de la clase Globals a partir del fichero de configuraci�n.
        //Se encarga de abrir la base de datos, cargar los par�metros, etc.
        Globals.GetInstance(iniFile);

        Globals g = Globals.GetInstance();

        SipCore sipWrk = new SipCore();

        g.GetLog2().Info(sipAgent, sipWrk.ConvertSipTypeToSipTypeName(sipType), "Inicio petici�n SIP de salida");
        return new SipQueue().AddToQueue(g.GetDatabase(), sipAgent, sipType, "", SipQueue.STATUS_PROCESSING);
    }

    /// <summary>
    /// Post-proceso. Debe ser invocado despu�s de lanzar la l�gica de negocio del WebService.
    /// </summary>
    /// <param name="msgId">id de mensaje</param>
    /// <param name="ok">true si el proceso ha finalizado correctamente</param>
    /// <param name="numRows">n�mero de filas</param>
    /// <param name="filename">fichero asociado</param>
    public void PostProcessWebService(string msgId, bool ok, int numRows, string filename) 
    {
        SipQueue msg = new SipQueue();
        Globals g = Globals.GetInstance();
        if(ok)
            msg.Completed(g.GetDatabase(), msgId, numRows, filename);
        else
            msg.Error(g.GetDatabase(), msgId, numRows, filename);
      
        //Cerrar instancia de la clase Global
        //No utilizar la conexi�n a la base de datos a partir de este punto.
        if (g != null) g.Close();
    }

    /// <summary>
    /// Chequeo de formato de salida correcto
    /// </summary>
    /// <param name="s">formato</param>
    /// <returns>true si correcto</returns>
    public bool FileFormatIsOK(string s)
    {
        return s.ToLower().Equals(SipOut.FORMAT_XML) || s.ToLower().Equals(SipOut.FORMAT_DELIMITED);
    }

    /// <summary>
    /// Invocar a un proceso externo
    /// </summary>
    /// <param name="sip">sip</param>
    /// <param name="agente">agente</param> 
    public void PostProcess(ISipOutInterface sip, string agent)
    {
        Utils.InvokeExternalProgram(sip.GetId(), sip.GetSipTypeName(), agent);
    }
  }
}
