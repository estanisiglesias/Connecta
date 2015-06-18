using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectaLib
{
  /// <summary>
  /// Clase base para todos los SIP's de actualización (SipUpd*)
  /// </summary>
  public abstract class SipUpd
  {
    //Agente
    protected string agent = "";

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="agent">agente</param>
    public SipUpd(string agent) 
    {
      this.agent = agent;
    }

    /// <summary>
    /// Invocar a un proceso externo
    /// </summary>
    /// <param name="sip">sip</param>
    /// <param name="agente">agente</param> 
    public void PreProcess(ISipUpdInterface sip, string agent)
    {
      Utils.InvokeExternalProgram("pre" + sip.GetId(), sip.GetSipTypeName(), agent);
    }

    /// <summary>
    /// Invocar a un proceso externo
    /// </summary>
    /// <param name="sip">sip</param>
    /// <param name="agente">agente</param> 
    public void PostProcess(ISipUpdInterface sip, string agent)
    {
        Utils.InvokeExternalProgram("post" + sip.GetId(), sip.GetSipTypeName(), agent);
    }
}
}
