using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Collections;

namespace ConnectaLib
{
  /// <summary>
  /// Clase base de todos los SIP de entrada
  /// </summary>
  public class SipIn
  {
    //Agente
    protected string agent = "";

    //Ubicación del agente
    protected string agentLocation = "";

    //Nomenclator
    protected string nomenclator = "";

    //Título
    public string masterTitle = "";
    public string slaveTitle = "";

    //Para poder consultar como ha ido el proceso de un Master
    public bool resultadoMaster = false;
    //Para poder consultar como ha ido el proceso de un Slave
    public bool resultadoSlave = false;

    //Para pasar parametros especiales a los sipIN
    public string customParams = "";

    public Hashtable htLogs = new Hashtable(10);
    public Logs logs;
    public struct Logs
    {
        public string pk;
        public string agent;        
        public string sipType;
        public string type;
        public string codigoAlerta;
        public string myAlertMsg;
        public string[] valores;
        public string[] claves;
        public string[] clavesExt;
        public int count;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="agent">agente</param>
    /// <param name="sipId">id de sip</param>
    public SipIn(string agent, string sipId) 
    {
        this.agent = agent;
        nomenclator = IniManager.INIGetKeyValue(IniManager.CFG_NOMENCLATORS, sipId, IniManager.GetIniFile());

        SipCore sipWrk = new SipCore();
        logs = new Logs();
        logs.agent = agent;
        logs.sipType = sipWrk.ConvertSipTypeToSipTypeName(sipId);
        htLogs.Clear();

        Database db = Globals.GetInstance().GetDatabase();
        DbDataReader rs = null;
        try
        {
            string strSql = "Select Ubicacion From Agentes Where IdcAgente=" + agent;
            rs = db.GetDataReader(strSql);
            if (rs.Read())
            {
                this.agentLocation = db.GetFieldValue(rs, 0);
            }
            if (rs != null)
                rs.Close();
            rs = null;
        }
        catch (Exception e)
        {
            sipWrk = new SipCore();
            Globals.GetInstance().GetLog2().Error(agent, sipWrk.ConvertSipTypeToSipTypeName(sipId), e);
        }
        finally
        {            
            if (rs != null)
                rs.Close();
        }
    }

    /// <summary>
    /// Invocar a un proceso externo
    /// </summary>
    /// <param name="sip">sip</param>
    /// <param name="agente">agente</param> 
    protected void InvokeExternalProgram(ISipInInterface sip, string agent) 
    {
        Utils.InvokeExternalProgram(sip.GetId(), sip.GetSipTypeName(), agent);
    }

    /// <summary>
    /// Invocar a un proceso externo en el POSTPROCESS
    /// </summary>
    /// <param name="sip">sip</param>
    /// <param name="agente">agente</param> 
    protected void InvokeExternalProgramPOST(ISipInInterface sip, string agent)
    {
        Utils.InvokeExternalProgramPOST(sip.GetId(), sip.GetSipTypeName(), agent);
    }

  }
}
