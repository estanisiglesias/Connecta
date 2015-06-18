using System;
using System.Collections;
using System.Text;
using System.Data.Common;

namespace ConnectaLib
{
  /// <summary>
  /// Clase de acceso desde cualquier punto de la aplicación. 
  /// Permite obtener una conexión con la base de datos, acceso al sistema 
  /// de logs, configuración, etc.
  /// </summary>
  public class Globals
  {
    private static Globals instance = null;
    //private static Log log = null;
    private static Log2 log2 = null;
    private static Database db = null;
    private string connectionString = ""; 
    private static string inBoxPath = "";
    private static string backupBoxPath = "";
    private static string logBoxPath = "";
    private static string xsdPath = "";
    private static string fieldSeparator = "";
    private static string fieldSeparator2 = "";
    private static int logMode = Log2.LOG_ALL;
    private static string outBoxPath = "";
    private static string workBoxPath = "";
    private string taskId = null;
    private static Globals workingInstance = null;

    /// <summary>
    /// Obtener una instancia de la clase
    /// </summary>
    /// <returns>instancia de la clase</returns>
    public static Globals GetInstance()
    {
      if (workingInstance != null)
        return workingInstance;

      if (instance == null) 
      {
        instance = new Globals(null);
      }
      return instance;
    }

    /// <summary>
    /// Obtener una instancia de la clase especificando un ficheo de configuración
    /// </summary>
    /// <param name="iniFile">fichero de configuración</param>
    /// <returns>instancia de la clase</returns>
    public static Globals GetInstance(string iniFile)
    {
      if (workingInstance != null)
        return workingInstance;

      if (instance == null)
      {
        instance = new Globals(iniFile);
      }
      return instance;
    }

    /// <summary>
    /// Ajustar la variable de trabajo
    /// </summary>
    /// <param name="g">instancia de la clase globals</param>
    public static void SetWorkingInstance(Globals g)
    {
      workingInstance = g;
    }

    /// <summary>
    /// Obtener una nueva instancia de la clase. 
    /// Este método sólo debe usarse cuando no pueda reaprovecharse la
    /// instancia existente.
    /// </summary>
    /// <returns>instancia de la clase</returns>
    public static Globals GetNewInstance()
    {
      return new Globals(null);
    }

    /// <summary>
    /// Constructor privado. Usar GetInstance.
    /// </summary>
    /// <param name="iniFile">fichero de configuración</param>
    private Globals(string iniFile)
    {
        if(iniFile!=null)
            IniManager.SetIniFile(iniFile);

        //Obtener parámetros de conexión con base de datos
        connectionString = GetConnectionString();

        //Obtener otros valores de configuración
        inBoxPath = IniManager.INIGetKeyValue(IniManager.CFG_SECTION_PATH, IniManager.CFG_INBOXPATH, IniManager.GetIniFile());
        outBoxPath = IniManager.INIGetKeyValue(IniManager.CFG_SECTION_PATH, IniManager.CFG_OUTBOXPATH, IniManager.GetIniFile());
        workBoxPath = IniManager.INIGetKeyValue(IniManager.CFG_SECTION_PATH, IniManager.CFG_WORKBOXPATH, IniManager.GetIniFile());
        backupBoxPath = IniManager.INIGetKeyValue(IniManager.CFG_SECTION_PATH, IniManager.CFG_BACKUPBOXPATH, IniManager.GetIniFile());
        logBoxPath = IniManager.INIGetKeyValue(IniManager.CFG_SECTION_PATH, IniManager.CFG_LOGBOXPATH, IniManager.GetIniFile());
        xsdPath = IniManager.INIGetKeyValue(IniManager.CFG_SECTION_PATH, IniManager.CFG_XSDPATH, IniManager.GetIniFile());
        fieldSeparator = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_FIELD_SEPARATOR, IniManager.GetIniFile(), Constants.FIELD_SEPARATOR);
        fieldSeparator2 = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_FIELD_SEPARATOR2, IniManager.GetIniFile(), Constants.FIELD_SEPARATOR2);
        try
        {
            logMode = Int32.Parse(IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_LOGMODE, IniManager.GetIniFile(), Log2.LOG_ALL + ""));
        }
        catch (Exception)
        {
            logMode = Log2.LOG_ALL;
        }
        db = new Database(connectionString);
        if (db != null)
        {
            db.Open();

            //Crear log
            //log = new Log(db, logBoxPath, logMode);
            bool insertNewMessages = (IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_INSERT_NEW_MESSAGES, IniManager.GetIniFile(), "N") == "S");
            log2 = new Log2(db, logBoxPath, logMode, insertNewMessages);
        }
    }

    /// <summary>
    /// Obtener cadena de conexión a la base de datos
    /// </summary>
    /// <returns>cadena de conexión a la base de datos</returns>
    private string GetConnectionString() 
    {
      string connstr = "";
      string provider = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_PROVIDER, IniManager.GetIniFile());
      if (provider != null && !provider.Equals(""))
      {
        connstr = "Provider = " + provider;
      }
      else
      {
        string datasource = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_DATASOURCE, IniManager.GetIniFile());
        string userid = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_USERID, IniManager.GetIniFile());
        string pwd = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_PWD, IniManager.GetIniFile());
        string catalog = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_CATALOG, IniManager.GetIniFile());
        connstr = "Provider = SQLOLEDB; Data Source = " + datasource + "; User Id = " + userid + "; Password = " + pwd + "; ";
        if (catalog != null && !catalog.Equals(""))
          connstr += "Initial Catalog = " + catalog + ";";
      }
      return connstr;
    }

    /// <summary>
    /// Obtener cadena de conexión a la base de datos para sql connections
    /// </summary>
    /// <returns>cadena de conexión a la base de datos</returns>
    public static string GetSQLConnectionString()
    {
        string connstr = "";                
        string datasource = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_DATASOURCE, IniManager.GetIniFile());
        string user = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_USERID, IniManager.GetIniFile());
        string pwd = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_PWD, IniManager.GetIniFile());
        string catalog = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_CATALOG, IniManager.GetIniFile());
        connstr = "Data Source = " + datasource + "; User = " + user + "; Password = " + pwd + "; ";
        if (catalog != null && !catalog.Equals(""))
            connstr += "Initial Catalog = " + catalog + ";";        
        return connstr;
    }

    /// <summary>
    /// Obtener una instancia de la clase Database que contiene una conexión a la
    /// base de datos. El invocador de este método es el responsable de liberarla.
    /// </summary>
    /// <returns>instancia de la clase Database</returns>
    public Database GetNewDatabase() 
    {
      Database database = new Database(connectionString);
      if (database != null)
        database.Open();
      return database;
    }

    /// <summary>
    /// Obtener log (OBSOLETOOOOOOOO!!!)
    /// </summary>
    /// <returns>log</returns>
    //public Log GetLog()
    //{
    //    //Tenemos que cargar el id de mensajes en el objeto log
    //    log.SetTaskId(GetTaskId());
    //    return log;
    //}

    /// <summary>
    /// Obtener log (nueva versión de log)
    /// </summary>
    /// <returns>log</returns>
    public Log2 GetLog2()
    {
        //Tenemos que cargar el id de mensajes en el objeto log
        log2.SetTaskId(GetTaskId());
        return log2;
    }

    /// <summary>
    /// Liberar recursos de la instancia del objeto
    /// </summary>
    public void Close() 
    {
      if (db != null)
        db.Close();
      db = null;
      instance = null;
    }

    /// <summary>
    /// Obtener directorio de backup
    /// </summary>
    /// <returns>directorio de backups</returns>
    public string GetBackupBoxPath() 
    {
      return backupBoxPath;
    }

    /// <summary>
    /// Obtener directorio de backup
    /// </summary>
    /// <returns>directorio de backups</returns>
    public string GetInBoxPath()
    {
      return inBoxPath;
    }

    /// <summary>
    /// Obtener directorio de salida
    /// </summary>
    /// <returns>directorio de salida</returns>
    public string GetOutBoxPath()
    {
      return outBoxPath;
    }

    /// <summary>
    /// Obtener directorio de trabajo
    /// </summary>
    /// <returns>directorio de trabajo</returns>
    public string GetWorkBoxPath()
    {
        return workBoxPath;
    }

    /// <summary>
    /// Obtener directorio de XSD
    /// </summary>
    /// <returns>directorio de xsd</returns>
    public string GetXSDPath()
    {
      return xsdPath;
    }

    /// <summary>
    /// Obtiene una conexíón a la base de datos
    /// </summary>
    /// <returns></returns>
    public Database GetDatabase() 
    {
      return db;
    }

        /// <summary>
        /// Obtener separador de campos para ficheros delimited. Si no se define
        /// en el fichero .ini, se retorna un valor por defecto.
        /// </summary>
        /// <returns>separador</returns>
        public string GetFieldSeparator() 
        {
            return fieldSeparator;
        }

        public string GetFieldSeparator(string pRow)
        {
            if (pRow.IndexOf(fieldSeparator) != -1) return fieldSeparator;
            if (pRow.IndexOf(fieldSeparator2) != -1) return fieldSeparator2;
            return fieldSeparator;
        }

    /// <summary>
    /// Modo de log
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
  }

  /// <summary>
  /// Clase de acceso desde cualquier punto de la aplicación. 
  /// Permite obtener una conexión con una base de datos secundaria del Data
  /// </summary>
  public class GlobalsData
  {
      private const string idConexionGlobal = "idcagente";      

      /// <summary>
      /// Obtener cadena de conexión a la base de datos
      /// </summary>
      /// <returns>cadena de conexión a la base de datos</returns>
      private static string GetConnectionString(string agent)
      {
          bool isOK = false;
          string connstr = "";
          Database db = Globals.GetInstance().GetDatabase();
          DbDataReader cursor = null;
          string datasource = "";
          string userid = "";
          string pwd = "";
          string catalog = "";
          try
          {
              string sql = "select servidor, catalogo, usuario, contrasenya from CfgConexionesBD " +
                           " where IdConnexion = '" + agent + "'";
              cursor = db.GetDataReader(sql);
              if (cursor.Read())
              {
                  datasource = db.GetFieldValue(cursor, 0);
                  catalog = db.GetFieldValue(cursor, 1);
                  userid = db.GetFieldValue(cursor, 2);
                  pwd = db.GetFieldValue(cursor, 3);
                  isOK = true;
              }
              else
              {
                  sql = "select servidor, catalogo, usuario, contrasenya from CfgConexionesBD " +
                        " where IdConnexion = '" + idConexionGlobal + "'";
                  cursor = db.GetDataReader(sql);
                  if (cursor.Read())
                  {
                      datasource = db.GetFieldValue(cursor, 0);
                      catalog = db.GetFieldValue(cursor, 1) + agent;
                      userid = db.GetFieldValue(cursor, 2);
                      pwd = db.GetFieldValue(cursor, 3);
                      isOK = true;
                  }
              }
          }
          finally
          {
              if (cursor != null)
                  cursor.Close();
          }
          if (isOK)
          {
              string dataSourceMapping = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_DATASOURCEMAPPING, IniManager.GetIniFile());
              if (!string.IsNullOrEmpty(dataSourceMapping) && dataSourceMapping.Contains(","))
              {
                  string[] dsm = dataSourceMapping.Split(';');
                  foreach (string d in dsm)
                  {
                      string datasourcename = d.Split(',')[0];
                      string datasourcenum = d.Split(',')[1];
                      if (datasource.Trim().ToLower().Equals(datasourcename.Trim().ToLower()))
                          datasource = datasourcenum;
                  }
              }
              connstr = "Provider = SQLOLEDB; Data Source = " + datasource + "; User Id = " + userid + "; Password = " + pwd + "; ";
              if (catalog != null && !catalog.Equals(""))
                  connstr += "Initial Catalog = " + catalog + ";";
          }
          return connstr;                                                            
      }

      /// <summary>
      /// Obtener una instancia de la clase Database que contiene una conexión a la
      /// base de datos. El invocador de este método es el responsable de liberarla.
      /// </summary>
      /// <returns>instancia de la clase Database</returns>
      public static Database GetNewDatabase(string agent)
      {
          string connectionString = GetConnectionString(agent);
          Database database = new Database(connectionString);
          if (database != null)
              database.Open();
          return database;
      }                          
  }
}
