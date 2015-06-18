using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;

namespace ConnectaLib
{
  /// <summary>
  /// Clase para gestionar las tareas (o mensajes) que no son más que colas de peticiones de
  /// procesos de integración de SIP's.
  /// Se dispone de métodos para consultar la lista de tareas pendientes, de marcar una
  /// tarea como finalizada o errónea, añadir una tarea, etc.
  /// </summary>
  public class SipQueue
  {
    public const string STATUS_PENDING = "P";
    public const string STATUS_PROCESSING = "X";
    public const string STATUS_OK = "E";
    public const string STATUS_ERROR = "R";
    public const string STATUS_CREATING = "C";

    /// <summary>
    /// Obtener SQL que permite procesar la lista de tareas pendientes
    /// </summary>
    /// <returns>sql</returns>
    public static string GetSqlPendings() 
    {
        return "SELECT TOP 1 Msg_Id,Msg_Data,Msg_Orquestacion,Msg_Id_Agente FROM Tareas " +
             " WHERE Msg_status = '" + SipQueue.STATUS_PENDING + "' " +   //Los pendientes de procesar...
             " AND Msg_timestamp < GETDATE() " + //Los que no esten retrasados...
             " ORDER BY Msg_Priority, Msg_timestamp";
    }

    /// <summary>
    /// Obtener SQL que permite procesar la lista de tareas pendientes (solo si son de sipOut)
    /// </summary>
    /// <returns>sql</returns>
    public static string GetSqlPendings_NOTsipIn()
    {
        return "select TOP 1 Msg_Id,Msg_Data,Msg_Orquestacion,Msg_Id_Agente from Tareas " +
             " where Msg_status = '" + SipQueue.STATUS_PENDING + "' " +   //Los pendientes de procesar...
             " And (Msg_Orquestacion Like 'sipOut%' or Msg_Orquestacion Like 'sipUpd%' or Msg_Orquestacion Like 'sipDel%' or Msg_Orquestacion Like 'sipExe%') " + //Los sip de tipo sipOut o sipUpd
             " AND Msg_timestamp < GETDATE() " + //Los que no esten retrasados...
             " order by Msg_Priority, Msg_timestamp";
    }

    /// <summary>
    /// Obtener SQL que permite obtener la lista de tareas
    /// </summary>
    /// <returns>sql</returns>
    public static string GetSqlAll()
    {
      return "select Msg_Id,Msg_Data,Msg_Orquestacion,Msg_Id_Agente,Msg_status,Msg_timestamp,Msg_updatetimestamp,Msg_StartTimeStamp,Msg_Rows "+
             "from Tareas " +
             "order by Msg_timestamp desc";
    }

    /// <summary>
    /// Notificación de mensaje en proceso
    /// </summary>
    /// <param name="db">Conexión con base de datos</param>
    /// <param name="msgid">id de mensaje</param>
    public void Processing(Database db, string msgid)
    {
        string sql = "update Tareas set Msg_status = '" + SipQueue.STATUS_PROCESSING + "',MSG_STARTTIMESTAMP=" + db.SysDate() + " " +  
                   "where Msg_id='" + msgid + "'";
      db.ExecuteSql(sql);
    }

    /// <summary>
    /// Notificación de mensaje procesado correctamente
    /// </summary>
    /// <param name="db">Conexión con base de datos</param>
    /// <param name="msgid">id de mensaje</param>
    public void Completed(Database db, string msgid)
    {
        string sql = "update Tareas set Msg_status = '" + SipQueue.STATUS_OK + "',MSG_UPDATETIMESTAMP=" + db.SysDate() + " " +
                   "where Msg_id='" + msgid + "'";
      db.ExecuteSql(sql);
    }

    /// <summary>
    /// Notificación de mensaje procesado correctamente
    /// </summary>
    /// <param name="db">Conexión con base de datos</param>
    /// <param name="msgid">id de mensaje</param>
    /// <param name="numRows">número de filas</param>
    /// <param name="filename">fichero</param>
    public void Completed(Database db, string msgid, int numRows, string filename)
    {
        string sql = "update Tareas set Msg_Data='" + filename + "',Msg_status = '" + SipQueue.STATUS_OK + "'" +
                   "  ,MSG_UPDATETIMESTAMP=" + db.SysDate() + ",MSG_ROWS="+numRows+" " +
                   "where Msg_id='" + msgid + "'";
      db.ExecuteSql(sql);
    }

    /// <summary>
    /// Notificación de mensaje procesado con errores
    /// </summary>
    /// <param name="db">Conexión con base de datos</param>
    /// <param name="msgid">id de mensaje</param>
    public void Error(Database db, string msgid)
    {
        string sql = "update Tareas set Msg_status = '" + SipQueue.STATUS_ERROR + "',MSG_UPDATETIMESTAMP=" + db.SysDate() + " " +
                   "where Msg_id='" + msgid + "'";
      db.ExecuteSql(sql);
    }

    /// <summary>
    /// Notificación de mensaje procesado con errores
    /// </summary>
    /// <param name="db">Conexión con base de datos</param>
    /// <param name="msgid">id de mensaje</param>
    /// <param name="numRows">número de filas</param>
    /// <param name="filename">fichero</param>
    public void Error(Database db, string msgid, int numRows, string filename)
    {
        string sql = "update Tareas set Msg_Data='" + filename + "',Msg_status = '" + SipQueue.STATUS_ERROR + "'" +
                   "   ,MSG_UPDATETIMESTAMP=" + db.SysDate() + ",MSG_ROWS=" + numRows + " " +
                   "where Msg_id='" + msgid + "'";
      db.ExecuteSql(sql);
    }

    /// <summary>
    /// Poner estado pendiente de procesar a los mensajes en creación
    /// </summary>
    /// <param name="db">Conexión con base de datos</param>
    /// <param name="idAgent">id del agente</param>
    public void Pendiente(Database db, string sipAgent)
    {
        string sql = "update Tareas set Msg_status = '" + SipQueue.STATUS_PENDING + "' " +
                   "where Msg_id_Agente='" + sipAgent + "' And Msg_Status='" + SipQueue.STATUS_CREATING + "'";
        db.ExecuteSql(sql);
    }

    /// <summary>
    /// Añadir un mensaje a la cola de tarea.
    /// </summary>
    /// <param name="db">Conexión con base de datos</param>
    /// <param name="sipAgent">agente</param>
    /// <param name="sipType">SIP</param>
    /// <param name="sipFilename">nombre de fichero</param>
    /// <param name="sipStatus">status</param>
    /// <returns>id de mensaje</returns>
    public string AddToQueue(Database db, string sipAgent, string sipType, string sipFilename, string sipStatus)
    {
      return AddToQueue(db, sipAgent, sipType, sipFilename, sipStatus, 0, "", 0);
    }

    /// <summary>
    /// Añadir un mensaje a la cola de tarea.
    /// </summary>
    /// <param name="db">Conexión con base de datos</param>
    /// <param name="sipAgent">agente</param>
    /// <param name="sipType">SIP</param>
    /// <param name="sipFilename">nombre de fichero</param>
    /// <param name="sipStatus">status</param>
    /// <param name="numRows">rows</param>
    /// <returns>id de mensaje</returns>
    public string AddToQueue(Database db, string sipAgent, string sipType, string sipFilename, string sipStatus, int numRows, string pTimeStamp, int pPriorityDelay)
    {
        string msgId = "";
        string sTimeStamp = "";

        //Obtener valor asignado ya que es valor automático
        DbDataReader cursor = null;
        try
        {
            //Determinar la prioridad en función del tipo de sip
            string typeLower = sipType.ToLower();
            int prioridad = 9999;
            if (typeLower.Equals(SipInProductosDistribuidor.ID_SIP.ToLower()))
                prioridad = 110;            
            
            //Si viene indicado que se debe retrasar la prioridad lo que hacemos es aumentar el valor de esta.
            prioridad = prioridad + pPriorityDelay;

            string sql = "";
            if (db.GetDbType() == Database.DB_SQLSERVER)
                sql = "SELECT NEWID()";   
            else
                sql = "select max(MSG_ID) + 1 from Tareas";
            cursor = db.GetDataReader(sql);
            if (cursor.Read()) msgId = db.GetFieldValue(cursor, 0);

            if (String.IsNullOrEmpty(pTimeStamp))
            {
                sTimeStamp = db.SysDate();
            }
            else
            {
                sTimeStamp = db.DateTimeForSql(pTimeStamp);
            }

            sql = "insert into Tareas (MSG_ID, MSG_ID_AGENTE, MSG_ORQUESTACION";
            sql += ", MSG_DATA";
            sql += ", MSG_STATUS, MSG_TIMESTAMP, MSG_STARTTIMESTAMP, MSG_ROWS, MSG_PRIORITY) ";
            sql += " values ('"+msgId+"',"+ sipAgent + ",'" + sipType + "'";
            sql += ",'" + sipFilename + "'";
            sql += ",'" + sipStatus + "'," + sTimeStamp + "," + sTimeStamp + "," + numRows + "," + prioridad.ToString() + ")";
            db.ExecuteSql(sql);
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
        return msgId;
    }

    /// <summary>
    /// Añadir un mensaje a la cola de tareas (para sipOut).
    /// </summary>
    /// <param name="db">Conexión con base de datos</param>
    /// <param name="sipAgent">agente</param>
    /// <param name="sipType">SIP</param>
    /// <param name="sipFilename">nombre de fichero</param>
    /// <param name="sipStatus">status</param>
    /// <returns>id de mensaje</returns>
    public string AddToQueue_sipOut(Database db, string sipAgent, string sipType, string sipFileName, string sipDealer, string sipFromDate, string sipToDate, string sipFileFormat, string sipFilter, string sqlId, string sipFieldSeparator, bool sipExcludeLastFieldSeparator, bool sipIncludeHeaders, string sipEncoding, string sipStatus)
    {
        string msgId = "";

        //Obtener valor asignado ya que es valor automático
        DbDataReader cursor = null;
        try
        {
            //Insertar una tarea en Connecta para solicitar la descarga "just in time".
            //-sipdealer=DISTR120;&;-sipfromdate=01/01/2009;&;-siptodate=31/12/2009;&;-sipfilter=ProductosAlbaranes.NumAlbaran='23100';ProductosAlbaranes.Ejercicio='2009';&;-sipfileformat=txt
            string sData = "-sipfileformat=" + sipFileFormat;
            if (!String.IsNullOrEmpty(sipFileName))
                sData += ";&;-sipfilename=" + sipFileName;
            if (!String.IsNullOrEmpty(sipFromDate))
                sData += ";&;-sipfromdate=" + sipFromDate;
            if (!String.IsNullOrEmpty(sipToDate))
                sData += ";&;-siptodate=" + sipToDate;
            if (!String.IsNullOrEmpty(sipDealer))
                sData += ";&;-sipdealer=" + sipDealer.Trim();
            if (!String.IsNullOrEmpty(sipFilter))
                sData += ";&;-sipfilter=" + sipFilter;
            if (!String.IsNullOrEmpty(sqlId))
                sData += ";&;-sipsqlid=" + sqlId;
            if (!String.IsNullOrEmpty(sipFieldSeparator))
                sData += ";&;-sipfieldseparator=" + sipFieldSeparator;

            if (sipExcludeLastFieldSeparator)
                sData += ";&;-sipexcludelastfieldseparator";
            if (sipIncludeHeaders)
                sData += ";&;-sipincludeheaders";

            if (!String.IsNullOrEmpty(sipEncoding))
                sData += ";&;-sipencoding=" + sipEncoding;

            //Determinar la prioridad en función del tipo de sip
            string typeLower = sipType.ToLower();
            int prioridad = 10;

            string sql = "";
            if (db.GetDbType() == Database.DB_SQLSERVER)
                sql = "SELECT NEWID()";   
            else
                sql = "select max(MSG_ID) + 1 from Tareas";
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
            msgId = db.GetFieldValue(cursor, 0);

            sql = "insert into Tareas (MSG_ID,MSG_ID_AGENTE,MSG_ORQUESTACION,MSG_DATA,MSG_STATUS,MSG_TIMESTAMP,MSG_STARTTIMESTAMP,MSG_PRIORITY) " +
                "values('"+msgId+"',"+ sipAgent + ",'" + sipType + "','" + sData + "','" + sipStatus + "'," + db.SysDate() + "," + db.SysDate() + "," + prioridad.ToString() + ")";
            db.ExecuteSql(sql);
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
        return msgId;
    }

      /// <summary>
      /// Añadir un mensaje a la cola de tareas (para sipUpd).
      /// </summary>
      /// <param name="db">Conexión con base de datos</param>
      /// <param name="sipAgent">agente</param>
      /// <param name="sipType">SIP</param>
      /// <param name="sipStatus">status</param>
      /// <returns>id de mensaje</returns>
    public string AddToQueue_sipUpd(Database db, string sipAgent, string sipType, string sipFromDate, string sipToDate, string sipDealer, string sipProvider, string sipFilter, string sqlId, string sipClassifier, string sipLockCode, string sipProductCode, string sipDataSource, string sipStatus)
    {
        return AddToQueue_sipUpd(db, sipAgent, sipType, sipFromDate, sipToDate, sipDealer, sipProvider, sipFilter, sqlId, sipClassifier, sipLockCode, sipProductCode, sipDataSource, sipStatus, 0);
    }

    public string AddToQueue_sipUpd(Database db, string sipAgent, string sipType, string sipFromDate, string sipToDate, string sipDealer, string sipProvider, string sipFilter, string sqlId, string sipClassifier, string sipLockCode, string sipProductCode, string sipDataSource, string sipStatus, int incMinutes)
      {
          string msgId = "";

          //Obtener valor asignado ya que es valor automático
          DbDataReader cursor = null;
          try
          {
              //Insertar una tarea en Connecta para solicitar la actualización "just in time".
              string sData = "";
              if (!String.IsNullOrEmpty(sipFromDate))
                  sData += ";&;-sipfromdate=" + sipFromDate;
              if (!String.IsNullOrEmpty(sipToDate))
                  sData += ";&;-siptodate=" + sipToDate;
              if (!String.IsNullOrEmpty(sipDealer))
                  sData += ";&;-sipdealer=" + sipDealer.Trim();
              if (!String.IsNullOrEmpty(sipProvider))
                  sData += ";&;-sipprovider=" + sipProvider.Trim();
              if (!String.IsNullOrEmpty(sipFilter))
                  sData += ";&;-sipfilter=" + sipFilter;
              if (!String.IsNullOrEmpty(sqlId))
                  sData += ";&;-sipsqlid=" + sqlId;
              if (!String.IsNullOrEmpty(sipClassifier))
                  sData += ";&;-sipclassifier=" + sipClassifier;
              if (!String.IsNullOrEmpty(sipLockCode))
                  sData += ";&;-siplockcode=" + sipLockCode;
              if (!String.IsNullOrEmpty(sipProductCode))
                  sData += ";&;-sipproductcode=" + sipProductCode;
              if (!String.IsNullOrEmpty(sipDataSource))
                  sData += ";&;-sipdatasource=" + sipDataSource;
              
              if (sData.StartsWith(";&;")) sData = sData.Substring(3);

              //Determinar la prioridad en función del tipo de sip
              string typeLower = sipType.ToLower();
              int prioridad = 900;

              string sql = "";
              if (db.GetDbType() == Database.DB_SQLSERVER)
                  sql = "SELECT NEWID()";
              else
                  sql = "select max(MSG_ID) + 1 from Tareas";
              cursor = db.GetDataReader(sql);
              if (cursor.Read())
                  msgId = db.GetFieldValue(cursor, 0);              

              sql = "insert into Tareas (MSG_ID,MSG_ID_AGENTE,MSG_ORQUESTACION,MSG_DATA,MSG_STATUS,MSG_TIMESTAMP,MSG_STARTTIMESTAMP,MSG_PRIORITY) " +
                  "values('" + msgId + "'," + sipAgent + ",'" + sipType + "','" + sData + "','" + sipStatus + "'," + db.SysDate(incMinutes) + "," + db.SysDate(incMinutes) + "," + prioridad.ToString() + ")";
              db.ExecuteSql(sql);
          }
          finally
          {
              if (cursor != null)
                  cursor.Close();
          }
          return msgId;
      }

      /// <summary>
      /// Añadir un mensaje a la cola de tareas (para sipDel).
      /// </summary>
      /// <param name="db">Conexión con base de datos</param>
      /// <param name="sipAgent">agente</param>
      /// <param name="sipType">SIP</param>
      /// <param name="sipStatus">status</param>
      /// <returns>id de mensaje</returns>
    public string AddToQueue_sipDel(Database db, string sipAgent, string sipType, string sipFromDate, string sipToDate, string sipDealer, string sipProvider, string sipUser, string sipFilter, string sqlId, string sipFromUpdDate, string sipToUpdDate, string sipFromInsDate, string sipToInsDate, string sipStatus)
    {
          string msgId = "";

          //Obtener valor asignado ya que es valor automático
          DbDataReader cursor = null;
          try
          {
              //Insertar una tarea en Connecta para solicitar la actualización "just in time".
              string sData = "";
              if (!String.IsNullOrEmpty(sipFromDate))
                  sData += ";&;-sipfromdate=" + sipFromDate;
              if (!String.IsNullOrEmpty(sipToDate))
                  sData += ";&;-siptodate=" + sipToDate;
              if (!String.IsNullOrEmpty(sipDealer))
                  sData += ";&;-sipdealer=" + sipDealer.Trim();
              if (!String.IsNullOrEmpty(sipProvider))
                  sData += ";&;-sipprovider=" + sipProvider.Trim();
              if (!String.IsNullOrEmpty(sipUser))
                  sData += ";&;-sipuser=" + sipUser.Trim();
              if (!String.IsNullOrEmpty(sipFilter))
                  sData += ";&;-sipfilter=" + sipFilter;
              if (!String.IsNullOrEmpty(sqlId))
                  sData += ";&;-sipsqlid=" + sqlId;
              if (!String.IsNullOrEmpty(sipFromUpdDate))
                  sData += ";&;-sipfromupddate=" + sipFromUpdDate;
              if (!String.IsNullOrEmpty(sipToUpdDate))
                  sData += ";&;-siptoupddate=" + sipToUpdDate;
              if (!String.IsNullOrEmpty(sipFromInsDate))
                  sData += ";&;-sipfrominsdate=" + sipFromInsDate;
              if (!String.IsNullOrEmpty(sipToInsDate))
                  sData += ";&;-siptoinsdate=" + sipToInsDate;

              if (sData.StartsWith(";&;")) sData = sData.Substring(3);

              //Determinar la prioridad en función del tipo de sip
              string typeLower = sipType.ToLower();
              int prioridad = 900;

              string sql = "";
              if (db.GetDbType() == Database.DB_SQLSERVER)
                  sql = "SELECT NEWID()";
              else
                  sql = "select max(MSG_ID) + 1 from Tareas";
              cursor = db.GetDataReader(sql);
              if (cursor.Read())
                  msgId = db.GetFieldValue(cursor, 0);

              sql = "insert into Tareas (MSG_ID,MSG_ID_AGENTE,MSG_ORQUESTACION,MSG_DATA,MSG_STATUS,MSG_TIMESTAMP,MSG_STARTTIMESTAMP,MSG_PRIORITY) " +
                  "values('" + msgId + "'," + sipAgent + ",'" + sipType + "','" + sData + "','" + sipStatus + "'," + db.SysDate() + "," + db.SysDate() + "," + prioridad.ToString() + ")";
              db.ExecuteSql(sql);
          }
          finally
          {
              if (cursor != null)
                  cursor.Close();
          }
          return msgId;
    }

    /// <summary>
    /// Añadir un mensaje a la cola de tareas (para sipExe).
    /// </summary>
    /// <param name="db">Conexión con base de datos</param>
    /// <param name="sipAgent">agente</param>
    /// <param name="sipType">SIP</param>
    /// <param name="sipStatus">status</param>
    /// <returns>id de mensaje</returns>
    public string AddToQueue_sipExe(Database db, string sipAgent, string sipType, string sipExeId, string sipProcessName, string sipPath, string sipParams, string sipStatus)
    {
        string msgId = "";

        //Obtener valor asignado ya que es valor automático
        DbDataReader cursor = null;
        try
        {
            //Insertar una tarea en Connecta para solicitar la actualización "just in time".
            string sData = "";
            if (!String.IsNullOrEmpty(sipExeId))
                sData += ";&;-sipexeid=" + sipExeId;
            if (!String.IsNullOrEmpty(sipProcessName))
                sData += ";&;-sipprocessname=" + sipProcessName;
            if (!String.IsNullOrEmpty(sipPath))
                sData += ";&;-sippath=" + sipPath.Trim();
            if (!String.IsNullOrEmpty(sipParams))
                sData += ";&;-sipparams=" + sipParams.Trim();

            if (sData.StartsWith(";&;")) sData = sData.Substring(3);

            //Determinar la prioridad en función del tipo de sip
            string typeLower = sipType.ToLower();
            int prioridad = 900;

            string sql = "";
            if (db.GetDbType() == Database.DB_SQLSERVER)
                sql = "SELECT NEWID()";
            else
                sql = "select max(MSG_ID) + 1 from Tareas";
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
                msgId = db.GetFieldValue(cursor, 0);

            sql = "insert into Tareas (MSG_ID,MSG_ID_AGENTE,MSG_ORQUESTACION,MSG_DATA,MSG_STATUS,MSG_TIMESTAMP,MSG_STARTTIMESTAMP,MSG_PRIORITY) " +
                "values('" + msgId + "'," + sipAgent + ",'" + sipType + "','" + sData + "','" + sipStatus + "'," + db.SysDate() + "," + db.SysDate() + "," + prioridad.ToString() + ")";
            db.ExecuteSql(sql);
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
        return msgId;
    }
  }

}
