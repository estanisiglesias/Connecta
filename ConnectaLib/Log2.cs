using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Data.OleDb;
using System.IO;
using System.Data.Common;
using System.Data;

namespace ConnectaLib
{
  /// <summary>
  /// Clase gestora de logs. El sistema de logs permite escribir en base de datos y 
  /// en ficheros (1 por día)
  /// </summary>
  public class Log2
  {
    /** Constantes de tipo de log, etc. */
    public const string LEVEL_CRITICAL = "[Critical]";
    public const string LEVEL_HIGH = "[High]";
    public const string LEVEL_MEDIUM = "[Medium]";
    public const string LEVEL_LOW = "[Low]";
    public const string TYPE_ERROR = "[Error]";
    public const string TYPE_SQL = "[Sql]";
    public const string TYPE_INFO = "[Info]";
    public const string TYPE_WARNING = "[Warning]";
    public const string TYPE_DETAILED_ERROR = "[DetailedError]";
    public const string TYPE_INFO_BACKUP = "[InfoBackup]";

    //Constantes para desactivar el log de
    public const int LOG_NONE = 0;
    public const int LOG_ALL = 1;
    public const int LOG_ALL_EVEN_QUERYS = 2;

    public const string LOG_APPLICATION = "CONNECTA";

    private Database db = null;
    private string logBoxPath = System.Environment.CurrentDirectory;
    private int logMode = Log2.LOG_ALL;
    private bool insertNewMessages = false;
    private string taskId = null;

    private DateTime dt = DateTime.MinValue;

    private Hashtable _AlertLogExceptionsMsgList;
    protected Hashtable AlertLogExceptionsMsgList
    {
        get
        {
            if (_AlertLogExceptionsMsgList == null) _AlertLogExceptionsMsgList = GetAlertLogExceptionsMsgList();
            else
            {
                if (dt == DateTime.MinValue) dt = DateTime.Now;
                TimeSpan ts = DateTime.Now - dt;
                if (ts.Hours >= 1)
                {
                    _AlertLogExceptionsMsgList = GetAlertLogExceptionsMsgList();
                    dt = DateTime.Now;
                }
            }
            return _AlertLogExceptionsMsgList;
        }
    }

    private Hashtable _AlertLogMsgList;
    protected Hashtable AlertLogMsgList
    {
        get
        {
            if (_AlertLogMsgList == null) _AlertLogMsgList = GetAlertLogMsgList();
            else
            {
                if (dt == DateTime.MinValue) dt = DateTime.Now;
                TimeSpan ts = DateTime.Now - dt;
                if (ts.Hours >= 1)
                {
                    _AlertLogMsgList = GetAlertLogMsgList();
                    dt = DateTime.Now;
                }
            }
            return _AlertLogMsgList;
        }
    }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="db">conexión con base de datos</param>
    /// <param name="logBoxPath">path de los ficheros</param>
    /// <param name="logMode">modo de log</param>
    public Log2(Database db, string logBoxPath, int logMode, bool insertNewMessages)
    {
        this.db = db;
        this.logMode = logMode;
        this.insertNewMessages = insertNewMessages;
        if (logBoxPath != null)
        {
            this.logBoxPath = logBoxPath;
            if (!Directory.Exists(logBoxPath))
            Directory.CreateDirectory(logBoxPath);
        }
    }

    /// <summary>
    /// Obtener modo de log
    /// </summary>
    /// <returns>modo de log</returns>
    public int LogMode()
    {
        return logMode;
    }

    /// <summary>
    /// Obtener identificador de mensaje
    /// </summary>
    /// <returns>identificador de mensaje</returns>
    public string GetTaskId()
    {
        return taskId;
    }

    /// <summary>
    /// Ajustar identificador de mensaje
    /// </summary>
    /// <param name="id">identificador de mensaje</param>
    public void SetTaskId(string id)
    {
        this.taskId = id;
    }

    /// <summary>
    /// Carga una lista hash table con todos los mensajes de alerta disponibles
    /// </summary>
    /// <param name="id">identificador de mensaje</param>
    private Hashtable GetAlertLogMsgList()
    {
        Hashtable htResult = new Hashtable();

        string strSql = " SELECT CodigoAlerta, Descripcion, NivelAlerta, Orquestacion, UsuarioResolucion, Resuelto " +
                        " FROM AlertLogCfgCodigos " +
                        " WHERE Aplicacion = '" + LOG_APPLICATION + "'" +
                        " ORDER BY Orquestacion, CodigoAlerta";

        string sCodigoAlerta = "";
        DbDataReader rs = null;
        rs = db.GetDataReader(strSql);
        while (rs.Read())
        {
            sCodigoAlerta = db.GetFieldValue(rs, 0);
            LogAlertLogMsgRecord recALM = new LogAlertLogMsgRecord(db.GetFieldValue(rs, 0), db.GetFieldValue(rs, 1), db.GetFieldValue(rs, 2), db.GetFieldValue(rs, 3), db.GetFieldValue(rs, 4), db.GetFieldValue(rs, 5));
            htResult.Add(sCodigoAlerta, recALM);
        }
        if (rs != null) rs.Close();
        rs = null;

        return htResult;
    }

    /// <summary>
    /// Carga una lista hash table con todos las excepciones de alerta disponibles
    /// </summary>    
    private Hashtable GetAlertLogExceptionsMsgList()
    {
        Hashtable htResult = new Hashtable();

        string strSql = " SELECT IdcAgenteOrigen, IdcAgenteDestino, CodigoAlerta, NivelAlerta, UsuarioResolucion, Resuelto " +
                        " FROM AlertLogCfgCodigosExcepciones " +                        
                        " ORDER BY CodigoAlerta";

        string sCodigoAlerta = "";
        DbDataReader rs = null;
        rs = db.GetDataReader(strSql);
        while (rs.Read())
        {
            sCodigoAlerta = db.GetFieldValue(rs, 0) + "_" + db.GetFieldValue(rs, 1) + "_" + db.GetFieldValue(rs, 2);
            LogAlertLogExceptionMsgRecord recALEM = new LogAlertLogExceptionMsgRecord(db.GetFieldValue(rs, 0), db.GetFieldValue(rs, 1), db.GetFieldValue(rs, 2), db.GetFieldValue(rs, 3), db.GetFieldValue(rs, 4), db.GetFieldValue(rs, 5));
            htResult.Add(sCodigoAlerta, recALEM);
        }
        if (rs != null) rs.Close();
        rs = null;

        return htResult;
    }

    /// <summary>
    /// Traza de un mensaje
    /// </summary>
    /// <param name="type">tipo de log(TYPE_)</param>
    /// <param name="level">nivel(LEVEL_)</param>
    /// <param name="msg">mensaje</param>
    /// <param name="sipagent">agente</param>
    /// <param name="sipTypeName">tipo de sip</param>    
    public void Trace(string pAgent, string sipTypeName, string pCodAlerta, string pMsg, params string[] pValores)
    {
        Trace(pAgent, "", sipTypeName, pCodAlerta, pMsg, 0, pValores, null, null);
    }

    public void Trace(string pAgent, Int32 pDest, string sipTypeName, string pCodAlerta, string pMsg, params string[] pValores)
    {
        Trace(pAgent, pDest.ToString(), sipTypeName, pCodAlerta, pMsg, 0, pValores, null, null);
    }

    public void Trace(string pAgent, string sipTypeName, string pCodAlerta, string pMsg, string[] pValores, string[] pClaves, string[] pClavesExt)
    {
        Trace(pAgent, "", sipTypeName, pCodAlerta, pMsg, 0, pValores, pClaves, pClavesExt);
    }

    public void Trace(string pAgent, string sipTypeName, string pCodAlerta, string pMsg, int count, string[] pValores, string[] pClaves, string[] pClavesExt)
    {
        Trace(pAgent, "", sipTypeName, pCodAlerta, pMsg, count, pValores, pClaves, pClavesExt);
    }

    public void Trace(string pAgent, string pDest, string sipTypeName, string pCodAlerta, string pMsg, int count, string[] pValores, string[] pClaves, string[] pClavesExt)
    {
        string sNivel = "";
        string sMsg = "";
        string sUsuResolucion = "";
        string sResuelto = "";
        string sClaveBusqueda = "";
        string sClaveBusquedaExt = "";
        bool encontrado = false;

        if (!Utils.IsBlankField(pDest))
        {            
            //buscamos si existe un mensaje excepción para agente + destino + código alerta
            if (AlertLogExceptionsMsgList.Contains(pAgent + "_" + pDest + "_" + pCodAlerta))
            {
                LogAlertLogExceptionMsgRecord recALEM = (LogAlertLogExceptionMsgRecord)AlertLogExceptionsMsgList[pAgent + "_" + pDest + "_" + pCodAlerta];
                if (AlertLogMsgList.Contains(pCodAlerta))
                {
                    LogAlertLogMsgRecord recALM = (LogAlertLogMsgRecord)AlertLogMsgList[pCodAlerta];
                    sMsg = Utils.MyFormat(recALM.sDescripcion, pValores);
                } 
                sNivel = recALEM.sNivelAlerta;
                if (sNivel.Equals("10"))
                    sNivel = Log2.TYPE_DETAILED_ERROR;
                else if (sNivel.Equals("20"))
                    sNivel = Log2.TYPE_ERROR;
                else if (sNivel.Equals("30"))
                    sNivel = Log2.TYPE_WARNING;
                else if (sNivel.Equals("40"))
                    sNivel = Log2.TYPE_INFO;
                else
                    sNivel = Log2.TYPE_ERROR;
                sUsuResolucion = recALEM.sUsuarioResolucion;
                sResuelto = recALEM.sResuelto;
                encontrado = true;
            }
        }
        if (!encontrado)
        {
            //buscamos si existe un mensaje excepción para agente + 0 + código alerta
            if (AlertLogExceptionsMsgList.Contains(pAgent + "_" + "0" + "_" + pCodAlerta))
            {
                LogAlertLogExceptionMsgRecord recALEM = (LogAlertLogExceptionMsgRecord)AlertLogExceptionsMsgList[pAgent + "_" + "0" + "_" + pCodAlerta];
                if (AlertLogMsgList.Contains(pCodAlerta))
                {
                    LogAlertLogMsgRecord recALM = (LogAlertLogMsgRecord)AlertLogMsgList[pCodAlerta];
                    sMsg = Utils.MyFormat(recALM.sDescripcion, pValores);
                } 
                sNivel = recALEM.sNivelAlerta;
                if (sNivel.Equals("10"))
                    sNivel = Log2.TYPE_DETAILED_ERROR;
                else if (sNivel.Equals("20"))
                    sNivel = Log2.TYPE_ERROR;
                else if (sNivel.Equals("30"))
                    sNivel = Log2.TYPE_WARNING;
                else if (sNivel.Equals("40"))
                    sNivel = Log2.TYPE_INFO;
                else
                    sNivel = Log2.TYPE_ERROR;
                sUsuResolucion = recALEM.sUsuarioResolucion;
                sResuelto = recALEM.sResuelto;
                encontrado = true;
            }
        }
        if (!encontrado)
        {
            if (!Utils.IsBlankField(pDest))
            {
                //buscamos si existe un mensaje excepción para 0 + destino + código alerta
                if (AlertLogExceptionsMsgList.Contains("0" + "_" + pDest + "_" + pCodAlerta))
                {
                    LogAlertLogExceptionMsgRecord recALEM = (LogAlertLogExceptionMsgRecord)AlertLogExceptionsMsgList["0" + "_" + pDest + "_" + pCodAlerta];
                    if (AlertLogMsgList.Contains(pCodAlerta))
                    {
                        LogAlertLogMsgRecord recALM = (LogAlertLogMsgRecord)AlertLogMsgList[pCodAlerta];
                        sMsg = Utils.MyFormat(recALM.sDescripcion, pValores); 
                    }                    
                    sNivel = recALEM.sNivelAlerta;
                    if (sNivel.Equals("10"))
                        sNivel = Log2.TYPE_DETAILED_ERROR;
                    else if (sNivel.Equals("20"))
                        sNivel = Log2.TYPE_ERROR;
                    else if (sNivel.Equals("30"))
                        sNivel = Log2.TYPE_WARNING;
                    else if (sNivel.Equals("40"))
                        sNivel = Log2.TYPE_INFO;
                    else
                        sNivel = Log2.TYPE_ERROR;
                    sUsuResolucion = recALEM.sUsuarioResolucion;
                    sResuelto = recALEM.sResuelto;
                    encontrado = true;
                }
            }
        }
        if (!encontrado)
        {
            //buscamos si existe ese código de alerta
            if (AlertLogMsgList.Contains(pCodAlerta))
            {
                LogAlertLogMsgRecord recALM = (LogAlertLogMsgRecord)AlertLogMsgList[pCodAlerta];

                sMsg = Utils.MyFormat(recALM.sDescripcion, pValores); //sMsg = string.Format(recALM.sDescripcion, pValores);      

                sNivel = recALM.sNivelAlerta;
                if (sNivel.Equals("10"))
                    sNivel = Log2.TYPE_DETAILED_ERROR;
                else if (sNivel.Equals("20"))
                    sNivel = Log2.TYPE_ERROR;
                else if (sNivel.Equals("30"))
                    sNivel = Log2.TYPE_WARNING;
                else if (sNivel.Equals("40"))
                    sNivel = Log2.TYPE_INFO;
                else
                    sNivel = Log2.TYPE_ERROR;

                if (!Utils.IsBlankField(recALM.sOrquestacion))
                {
                    if (!sipTypeName.ToUpper().Contains(Constants.SIP_PERSONALIZADO.ToUpper()))
                        sipTypeName = recALM.sOrquestacion;
                }
                sUsuResolucion = recALM.sUsuarioResolucion;
                sResuelto = recALM.sResuelto;
                encontrado = true;
            }
        }
        if (!encontrado)
        {
            if (!Utils.IsBlankField(pMsg))
            {
                if (insertNewMessages)
                {
                    //Primero miramos y aseguramos si existe el código de alerta en AlertLog
                    string sSql = "Select 1 from AlertLogCfgCodigos where CodigoAlerta=" + db.ValueForSql(pCodAlerta);
                    if (!Utils.RecordExist(sSql))
                    {
                        sSql = "insert into AlertLogCfgCodigos (CodigoAlerta,Descripcion,NivelAlerta,Orquestacion,Aplicacion)" +
                                    " VALUES " +
                                    "(" + db.ValueForSql(pCodAlerta) + "," + db.ValueForSql(pMsg) + "," + db.ValueForSql("20") + "," + db.ValueForSql(sipTypeName) + "," + db.ValueForSql(Log2.LOG_APPLICATION) + ")";
                        db.ExecuteSql(sSql, pAgent, sipTypeName);
                    }
                }

                sMsg = string.Format(pMsg, pValores);
                sNivel = Log2.TYPE_ERROR;
            }
            else
            {
                sMsg = "Alerta desconocida (" + pCodAlerta + ") en " + sipTypeName + " para el agente " + pAgent + ".";
                sNivel = Log2.TYPE_ERROR;
            }
        }

        if (pClaves != null && pClaves.Length > 0) sClaveBusqueda = String.Join(";", pClaves);
        if (pClavesExt != null && pClavesExt.Length > 0) sClaveBusquedaExt = String.Join(";", pClavesExt);

        //Para evitar problemas de concurrencia, bloquear
        lock (this)
        {
            if (count > 1)
                sMsg = sMsg + " (" + count + ")";
            TraceToFile(sNivel, Log2.LEVEL_MEDIUM, sMsg, pAgent, sipTypeName);
            TraceToDb(sNivel, sMsg, pAgent, sipTypeName, pCodAlerta, sClaveBusqueda, sClaveBusquedaExt, sUsuResolucion, sResuelto);
        }
    }

    /// <summary>
    /// Traza de un mensaje de información
    /// </summary>
    /// <param name="sipagent">agente</param>
    /// <param name="sipTypeName">tipo de sip</param>
    /// <param name="msg">mensaje</param>
    public void Info(string pAgent, string sipTypeName, string pMsg)
    {
        //Para evitar problemas de concurrencia, bloquear
        lock (this)
        {
            TraceToFile(Log2.TYPE_INFO, Log2.LEVEL_LOW, pMsg, pAgent, sipTypeName);
            TraceToDb(Log2.TYPE_INFO, pMsg, pAgent, sipTypeName, "", "", "", "", "S");
        }
    }

    /// <summary>
    /// Traza de un mensaje de información de backup
    /// </summary>
    /// <param name="sipagent">agente</param>
    /// <param name="sipTypeName">tipo de sip</param>
    /// <param name="msg">mensaje</param>
    public void InfoBackup(string pAgent, string sipTypeName, string pMsg)
    {
        //Para evitar problemas de concurrencia, bloquear
        lock (this)
        {
            TraceToFile(Log2.TYPE_INFO_BACKUP, Log2.LEVEL_LOW, pMsg, pAgent, sipTypeName);
            TraceToDb(Log2.TYPE_INFO_BACKUP, pMsg, pAgent, sipTypeName, "", "", "", "", "S");
        }
    }


    ///// <summary>
    ///// Traza de un mensaje de aviso
    ///// </summary>
    ///// <param name="sipagent">agente</param>
    ///// <param name="sipTypeName">tipo de sip</param>
    ///// <param name="msg">mensaje</param>
    //public void Warning(string sipagent, string sipTypeName, string msg)
    //{
    //    Trace(Log.TYPE_WARNING, Log.LEVEL_MEDIUM, msg, sipagent, sipTypeName);
    //}

    ///// <summary>
    ///// Traza de un mensaje de error detallado
    ///// </summary>
    ///// <param name="sipagent">agente</param>
    ///// <param name="sipTypeName">tipo de sip</param>
    ///// <param name="msg">mensaje</param>
    //public void DetailedError(string sipagent, string sipTypeName, string msg)
    //{
    //    TraceToFile(Log2.TYPE_DETAILED_ERROR, Log2.LEVEL_CRITICAL, msg, sipagent, sipTypeName);
    //    TraceToDb(Log2.TYPE_DETAILED_ERROR, msg, sipagent, sipTypeName, "", "", "", "", "");
    //}



    /// <summary>
    /// Traza de un mensaje de error
    /// </summary>
    /// <param name="sipagent">agente</param>
    /// <param name="sipTypeName">tipo de sip</param>
    /// <param name="e">Exception</param>
    public void Error(string sipagent, string sipTypeName, Exception e)
    {
        string sMsg = e.Message;
        if (e.InnerException != null) sMsg += " (" + e.InnerException.Message + ").";

        TraceToFile(Log2.TYPE_ERROR, Log2.LEVEL_CRITICAL, sMsg + " --> " + e.StackTrace, sipagent, sipTypeName);
        if (!Utils.IsBlankField(sipagent) && !Utils.IsBlankField(sipTypeName))
        {
            TraceToDb(Log2.TYPE_ERROR, sMsg, sipagent, sipTypeName, "", "", "", "", "N");
        }
    }
    public void Error(string sipagent, string sipTypeName, string pMsg)
    {
        TraceToFile(Log2.TYPE_ERROR, Log2.LEVEL_CRITICAL, pMsg, sipagent, sipTypeName);
        if (!Utils.IsBlankField(sipagent) && !Utils.IsBlankField(sipTypeName))
        {
            TraceToDb(Log2.TYPE_ERROR, pMsg, sipagent, sipTypeName, "", "", "", "", "N");
        }
    }

    /// <summary>
    /// Traza de un mensaje a fichero
    /// </summary>
    /// <param name="nivel">nivel</param>
    /// <param name="severidad">severidad</param>
    /// <param name="msg">mensaje</param>
    /// <param name="sipagent">agente</param>
    /// <param name="sipTypeName">tipo de sip</param>
    public void TraceToFile(string nivel, string severidad, string msg, string sipagent, string sipTypeName)
    {
        TraceToFile(nivel, severidad, msg, sipagent, sipTypeName, false);
    }

    public void TraceToFile(string nivel, string severidad, string msg, string sipagent, string sipTypeName, bool soloStatus)
    {
        //Para evitar problemas de concurrencia, bloquear
        lock (this)
        {
            bool canLog = false;
            switch (logMode)
            {
                case Log2.LOG_ALL:
                case Log2.LOG_ALL_EVEN_QUERYS:
                    canLog = true;
                    break;
                default:
                    if(!nivel.Equals(Log2.TYPE_SQL))
                    canLog = true;
                    break;
            }
            if (canLog) 
            { 
                Console.WriteLine(msg);

                if (Utils.IsBlankField(nivel)) nivel = Log2.TYPE_INFO;

                if (!Utils.IsBlankField(sipagent)) sipagent = "[" + sipagent + "]";
                if (!Utils.IsBlankField(sipTypeName)) sipTypeName = "[" + sipTypeName + "]";

                DateTime dt = DateTime.Now;
                String logDirectory = logBoxPath + "\\Log";
                String filePath = logDirectory + dt.ToString("yyyyMMdd") + ".log";
                if (!File.Exists(filePath))
                {
                    FileStream fs = File.Create(filePath);
                    fs.Close();
                }
                
                try
                {
                    string line = nivel + severidad + "[" + dt.ToString("yyyy/MM/dd HH:mm:ss") + "]" + sipagent + sipTypeName + "[" + msg + "]";
                    if (soloStatus)
                        Status(line); //Dejamos traza estado actual connecta
                    else
                    {
                        StreamWriter sw = File.AppendText(filePath);                        
                        sw.WriteLine(line);
                        sw.Flush();
                        sw.Close();
                        Status(line); //Dejamos traza estado actual connecta
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message.ToString());
                }
            }
        }
    }

    /// <summary>
    /// Traza de un mensaje a base de datos
    /// </summary>
    /// <param name="nivel">nivel</param>
    /// <param name="msg">mensaje</param>
    /// <param name="sipagent">agente</param>
    /// <param name="sipTypeName">tipo de sip</param>
    public void TraceToDb(string nivel, string msg, string sipagent, string sipTypeName, string pCodAlerta, string pCfgClaveBusqueda, string pCfgClaveBusquedaExt, string pUsuResolucion, string pResuelto)
    {
        //Para evitar problemas de concurrencia, bloquear
        lock (this)
        {
            if (!nivel.Equals(Log2.TYPE_SQL))
            {
                /**
                Nivel de alerta. Sus valores posibles son:
                10 – Errores detallados.
                20 – Errores.
                30 – Avisos.
                40 – Informativo (inicio y fin de procesos, etc.).
                */
                string nivelAlerta = "40";  //Informativo
                if (nivel.Equals(Log2.TYPE_SQL))
                    nivelAlerta = "5";
                else if (nivel.Equals(Log2.TYPE_DETAILED_ERROR))
                    nivelAlerta = "10";
                else if (nivel.Equals(Log2.TYPE_ERROR))
                    nivelAlerta = "20";
                else if (nivel.Equals(Log2.TYPE_WARNING))
                    nivelAlerta = "30";
                else if (nivel.Equals(Log2.TYPE_INFO))
                    nivelAlerta = "40";
                else if (nivel.Equals(Log2.TYPE_INFO_BACKUP))
                    nivelAlerta = "41";
          
                if (msg.Length > 2048)
                    msg = msg.Substring(0, 2048);
                msg = msg.Replace("'", "-");
                msg = msg.Replace("\n", " ");
                msg = msg.Replace("\r", " ");

                //Globals g = Globals.GetInstance();
                string taskIdTxt = "";
                if(GetTaskId() == null)
                    taskIdTxt = "NULL";
                else
                    taskIdTxt = "'" + GetTaskId() + "'";

                if (Utils.IsBlankField(pResuelto)) pResuelto = "N";

                string sql = "insert into AlertLog(NivelAlerta, [Timestamp], Descripcion, Orquestacion, IdcAgenteOrigen, IdMsg, Resuelto, UsuarioResolucion, CodigoAlerta, ClaveBusqueda, ClaveBusquedaExt) " +
                             " values ('" + nivelAlerta + "'," + db.SysDate() + ",'" + msg + "','" + sipTypeName + "','" + sipagent + "'," + taskIdTxt + ",'" + pResuelto + "','" + pUsuResolucion + "','" + pCodAlerta + "','" + pCfgClaveBusqueda + "','" + pCfgClaveBusquedaExt + "')";
                db.ExecuteSql(sql);
            }
        }
    }

    /// <summary>
    /// Guardamos estado actual de lo que esta haciendo connecta
    /// </summary>
    /// <param name="proc">proc</param>    
    public void Status(string line)
    {
        //Para evitar problemas de concurrencia, bloquear
        lock (this)
        {
            if (SipManager.dtLogsIntegrator == null)
                SipManager.dtLogsIntegrator = DateTime.MinValue;
            if (SipManager.dtLogsMailAgent == null)
                SipManager.dtLogsMailAgent = DateTime.MinValue;
            if (SipManager.dtLogsFtpAgent == null)
                SipManager.dtLogsFtpAgent = DateTime.MinValue;
            DateTime lastDtI = SipManager.dtLogsIntegrator;
            DateTime lastDtF = SipManager.dtLogsFtpAgent;
            DateTime lastDtM = SipManager.dtLogsMailAgent;
            DateTime nowDt = DateTime.Now;
            TimeSpan tsI = nowDt - lastDtI;
            TimeSpan tsF = nowDt - lastDtF;
            TimeSpan tsM = nowDt - lastDtM;
            
            string proc = "uveIntegrator";
            double s = tsI.TotalSeconds;            
            if (line.ToUpper().Contains("uveMailAgent".ToUpper()))
            {
                proc = "uveMailAgent";
                s = tsM.TotalSeconds;                
            }
            else if (line.ToUpper().Contains("uveFTPAgent".ToUpper()))
            {
                proc = "uveFTPAgent";
                s = tsF.TotalSeconds;                
            }
            if (s > 20) //Por ahora fijamos 20 segundos pero lo haremos variable por parametro del ini más adelante..
            {
                DateTime dt = DateTime.Now;
                String logDirectory = logBoxPath + "\\Log.";
                String filePath = logDirectory + proc + ".log";
                try
                {
                    StreamWriter sw = new StreamWriter(filePath, false);
                    sw.WriteLine(line);
                    sw.Flush();
                    sw.Close();                                        
                    if (proc == "uveIntegrator")
                        SipManager.dtLogsIntegrator = nowDt;
                    else if (proc == "uveFTPAgent")
                        SipManager.dtLogsFtpAgent = nowDt;
                    else if (proc == "uveMailAgent")
                        SipManager.dtLogsMailAgent = nowDt;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message.ToString());
                }
            }
        }
    }
  }
}
