using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectaLib
{
  /// <summary>
  /// Clase base para todos los SIP's de eliminación (SipDel*)
  /// </summary>
  public abstract class SipDel
  {
    //Agente
    protected string agent = "";

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="agent">agente</param>
    public SipDel(string agent) 
    {
      this.agent = agent;
    }

    /// <summary>
    /// Invocar a un proceso externo
    /// </summary>
    /// <param name="sip">sip</param>
    /// <param name="agente">agente</param> 
    public void PreProcess(ISipDelInterface sip, string agent)
    {
      Utils.InvokeExternalProgram("pre" + sip.GetId(), sip.GetSipTypeName(), agent);
    }

    /// <summary>
    /// Invocar a un proceso externo
    /// </summary>
    /// <param name="sip">sip</param>
    /// <param name="agente">agente</param> 
    public void PostProcess(ISipDelInterface sip, string agent)
    {
        Utils.InvokeExternalProgram("post" + sip.GetId(), sip.GetSipTypeName(), agent);
    }
}
}
