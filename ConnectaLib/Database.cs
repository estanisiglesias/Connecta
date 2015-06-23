using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data.OleDb;
using System.Data;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ConnectaLib
{
  /// <summary>
  /// Clase gestora de la conexión con la base de datos.
  /// Es importante utilizar esta clase que actúa como wrapper
  /// de una OleDbConnection para hacer "log" de todas las operaciones de
  /// actualización.
  /// </summary>
  public class Database
  {
    private OleDbConnection db = null;
    private OleDbTransaction transaction = null;
    private int dbtype = DB_SQLSERVER;
    
    //Constantes de tipo de base de datos
    public const int DB_SQLSERVER = 1;
    public const int DB_ACCESS = 2;
    public const int DB_OTHER = 3;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="dsn">dsn</param>
    public Database(string dsn) 
    {
        db = GetDbConnection(dsn);
        if (db.Provider.ToUpper().IndexOf("SQLOLEDB") != -1)
            dbtype = DB_SQLSERVER;
        else
            dbtype = DB_OTHER;
    }

    /// <summary>
    /// Ejecutar sql (SIN TRAZA)
    /// </summary>
    /// <param name="sql">sql</param>
    public int ExecuteSql(string sql)
    {
        int NumeroRegistros = -1;

        OleDbCommand cmd = db.CreateCommand();
        cmd.CommandTimeout = 600;
        cmd.CommandText = sql;
        if (transaction != null)
            cmd.Transaction = transaction;
        try
        {
            NumeroRegistros = cmd.ExecuteNonQuery();
            return NumeroRegistros;
        }
        catch(OleDbException eOleDb)
        {
            if (eOleDb.ErrorCode == Constants.OLEDB_EX_TIMEOUT)
                throw new TimeoutException(eOleDb.Message + ". SQL: " + sql);
            else if (eOleDb.ErrorCode == Constants.OLEDB_EX_TRUNCATE)
                throw new Exception(TratarTruncateException(eOleDb.Message + ". SQL: " + sql));
            else
                throw new Exception(eOleDb.Message + ". SQL: " + sql);
        }
        catch (Exception e)
        {
            throw new Exception(e.Message + ". SQL: " + sql);
        }
    }

    private string TratarTruncateException(string newMsg)
    {
        UVESqlParser sqlParser = new UVESqlParser(newMsg).Parse();
        if (!sqlParser.Error)
        {
            string logDetails = LogDetailsTruncateException(sqlParser.Tabla, sqlParser.Campos.ToArray(), sqlParser.Valores.ToArray());
            newMsg = !string.IsNullOrEmpty(logDetails) ? logDetails + newMsg : newMsg;
        }

        return newMsg;
    }

    /// <summary>
    /// Ejecutar SQL (con traza)
    /// </summary>
    /// <param name="sql">sql</param>
    /// <param name="sipagent">agente</param>
    /// <param name="siptypeName">nombre de tipo</param>
    public int ExecuteSql(string sql, string sipagent, string siptypeName)
    {
        int NumeroRegistros = -1;
        
        Globals g = Globals.GetInstance();
        g.GetLog2().TraceToFile(Log2.TYPE_SQL, Log2.LEVEL_MEDIUM, sql, sipagent, siptypeName);

        NumeroRegistros = ExecuteSql(sql);
        return NumeroRegistros;
    }

    /// <summary>
    /// Obtener conexión con base de datos
    /// </summary>
    /// <param name="dsn">dsn</param>
    /// <returns>conexión con base de datos</returns>
    private OleDbConnection GetDbConnection(string dsn)
    {
        return new System.Data.OleDb.OleDbConnection(dsn);
    }

    /// <summary>
    /// Abrir base de datos
    /// </summary>
    public void Open() 
    {
        if (db != null)
            db.Open();
    }

    /// <summary>
    /// Cerrar conexión
    /// </summary>
    public void Close()
    {
        if (db != null)
            db.Close();
        db = null;
    }

    /// <summary>
    /// Obtener un data reader para lanzar consultas contra la base de datos
    /// </summary>
    /// <param name="sql">sentencia sql</param>
    /// <returns>un data reader</returns>
    public DbDataReader GetDataReader(string sql) 
    {
        Globals g = Globals.GetInstance();
        //Log2 log = g.GetLog2();
        //if (log.LogMode() == Log2.LOG_ALL_EVEN_QUERYS) log.TraceToFile(Log2.TYPE_SQL, Log2.LEVEL_MEDIUM, sql, "", "");
        g.GetLog2().TraceToFile(Log2.TYPE_SQL, Log2.LEVEL_MEDIUM, sql, "", "");

        DbCommand oCmd = db.CreateCommand();
        oCmd.CommandTimeout = 1200;
        oCmd.CommandText = sql;
        if (transaction != null)
            oCmd.Transaction = transaction;
        return oCmd.ExecuteReader(CommandBehavior.Default);
    }

    /// <summary>
    /// Obtener el valor de un campo como string
    /// </summary>
    /// <param name="rs">recordset</param>
    /// <param name="field">campo</param>
    /// <returns>valor de un campo como string</returns>
    public string GetFieldValue(DbDataReader rs, int field)
    {
        Type type = rs.GetFieldType(field);
        string val = "";
        object o;
        if (type.Name.Equals("Double"))
        {
            o = rs.GetValue(field);
            if (o is System.DBNull)
                val = "0";
            else
                val = rs.GetDouble(field) + "";
        }
        else if (type.Name.Equals("Int32"))
        {
            o = rs.GetValue(field);
            if (o is System.DBNull)
                val = "0";
            else
                val = rs.GetInt32(field) + "";
        }
        else if (type.Name.Equals("Int16"))
        {
            o = rs.GetValue(field);
            if (o is System.DBNull)
                val = "0";
            else
                val = rs.GetInt16(field) + "";
        }
        else if (type.Name.Equals("DateTime"))
        {
            o = rs.GetValue(field);
            if (o is System.DBNull)
                val = "";
            else
            {
                //Cambio para evitar problemas localización
                //val = rs.GetDateTime(field) + "";
                val = ((DateTime)o).ToString("dd/MM/yyyy");
            }                
            //Remove time (if exists)
            int ix = val.IndexOf(" ");
            if (ix != -1)
                val = val.Substring(0, ix);
        }
        else if (type.Name.Equals("Boolean"))
        {
            o = rs.GetValue(field);
            if (o is System.DBNull)
                val = "0";
            else
            {
                bool b = rs.GetBoolean(field);
                if (b == true)
                    val = "1";
                else
                    val = "0";
            }
        }
        else if (type.Name.Equals("String"))
        {
            o = rs.GetValue(field);
            if (o is System.DBNull)
                val = "";
            else
                val = (String)o;
            val = val.Replace("|", "");
            val = val.Replace("\0", "");
            val = val.Replace("\n", " ");
            val = val.Replace("\r", " ");
        }
        else if (type.Name.Equals("Decimal"))
        {
            o = rs.GetValue(field);
            if (o is System.DBNull)
                val = "0";
            else
                val = rs.GetDecimal(field) + "";
        }
        else
        {
            o = rs.GetValue(field);
            if (o is System.DBNull)
                val = "";
            else
                val = o.ToString();
            val = val.Replace("|", "");
            val = val.Replace("\0", "");
            val = val.Replace("\n", " ");
            val = val.Replace("\r", " ");
        }
        return val;
    }

    /// <summary>
    /// Obtener valor para SQL en el caso de números
    /// </summary>
    /// <param name="val">valor</param>
    /// <returns>valor para SQL</returns>
    public string ValueForSqlAsNumeric(string val)
    {
        if (Utils.IsBlankField(val))
            return "NULL";
        else
        {
            //Pueden venir valores de varios tipos: 
            //  1234,45   -> 1234.45
            //  12.45     -> 12.45
            //  1,234.45  -> 1234.45
            //  1.234,45  -> 1234.45
            string tmp = "";
            val = val.Replace(",", ".");
            char c;
            int posSep = val.LastIndexOf(".");

            for (int i = 0; i < val.Length; i++)
            {
                c = val[i];
                if (c == '.')
                {
                    if (i == posSep)
                        tmp += c;
                }
                else
                    tmp += c;
            }
            return tmp;
        }
    }

    /// <summary>
    /// Obtener valor para SQL en el caso de string
    /// </summary>
    /// <param name="val">valor</param>    
    /// <returns>valor para SQL</returns>
    public string ValueForSql(string val) 
    {
        if (Utils.IsBlankField(val))
            return "NULL";
        else
        {
            val = val.Replace("'", "-");
            return "'" + val + "'";
        }
    }    
    public string ValueForSqlOK(string val)
    {
        if (Utils.IsBlankField(val))
            return "NULL";
        else
        {
            val = val.Replace("'", "''");
            return "'" + val + "'";
        }
    }
    
    /// <summary>
    /// Obtener valor para SQL en el caso de string sin convertir a NULL si es un valor en blanco
    /// </summary>
    /// <param name="val">valor</param>
    /// <param name="replacePh">si se quiere reemplazar apóstrofe ' por guión - o mantener apóstrofe '</param>
    /// <returns>valor para SQL</returns>
    public string ValueForSqlNotNull(string val)
    {
        if (Utils.IsBlankField(val))
            val = "";
        else
            val = val.Replace("'", "-");
        return "'" + val + "'";
    }
    public string ValueForSqlNotNullOK(string val)
    {
        if (Utils.IsBlankField(val))
            val = "";
        else
            val = val.Replace("'", "''");
        return "'" + val + "'";
    }

    /// <summary>
    /// Obtener fecha y hora para SQL
    /// </summary>
    /// <param name="value">valor(se admite un valor nulo)</param>
    /// <returns>fecha y hora para SQL</returns>
    public string DateTimeForSql(string value)
    {
        if (Utils.IsBlankField(value))
            return "NULL";
        DateTime dt;

        string s = "";

        if (DateTime.TryParse(value, out dt))
        {
            if (dbtype == DB_SQLSERVER)
            {
                string datetimediffformat = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_DATETIMEDIFFFORMAT, IniManager.GetIniFile());
                if (datetimediffformat == "S")
                    s = "'" + dt.Day + "/" + dt.Month + "/" + dt.Year + " " + dt.Hour + ":" + dt.Minute + ":" + dt.Second + "'";
                else
                    s = "'" + dt.Year + "/" + dt.Month + "/" + dt.Day + " " + dt.Hour + ":" + dt.Minute + ":" + dt.Second + "'";
            }
            else
                s = "#" + dt.ToString() + "#";
        }
        else
        {
            s = "NULL";
        }
        return s;
    }
    /// <summary>
    /// Obtener fecha para SQL
    /// </summary>
    /// <param name="value">valor(se admite un valor nulo)</param>
    /// <returns>fecha para SQL</returns>
    public string DateForSql(string value)
    {
      if (Utils.IsBlankField(value))
        return "NULL";
      string s = "";
      object x = GetDate(value);
      if (x != null) {
        DateTime dt = (DateTime)x;
        if (dbtype == DB_SQLSERVER)
        {
            string datetimediffformat = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_DATETIMEDIFFFORMAT, IniManager.GetIniFile());
            if (datetimediffformat == "S")
                s = "'" + dt.Day + "/" + dt.Month + "/" + dt.Year + "'";
            else
                s = "'" + dt.Year + "/" + dt.Month + "/" + dt.Day + "'";
        }
        else
            s = "#" + dt.ToString() + "#";
      }
      else
        s = "NULL";
      return s;
    }

    /// <summary>
    /// Obtener fecha
    /// </summary>
    /// <param name="s">valor</param>
    /// <returns>null si fecha errónea</returns>
    public object GetDate(string s)
    {
        //Eliminar la hora...
        int ix = s.IndexOf(" ");
        if (ix != -1)
            s = s.Substring(0, ix);

        // Formatos soportados
        string[] dateformats =
            { 
            "dd-MM-yy",
            "dd-MM-yyyy",
            "yyyy-MM-dd",
            "dd/MM/yy",
            "dd/MM/yyyy",
            "yyyy/MM/dd",
            "ddMMyyyy", 
            "yyyyMMdd", 
            "d-M-yy",
            "d-M-yyyy",
            "yyyy-M-d",
            "d/M/yy",
            "d/M/yyyy",
            "yyyy/M/d"
            };

        DateTimeFormatInfo myDTFI = new DateTimeFormatInfo();
        for (int i = 0; i < dateformats.Length; i++)
        {
            try
            {
                //es una fecha válida según este formato? 
                //si no cascarà, passarà por el catch e ignora este formato
                myDTFI.ShortDatePattern = dateformats[i];
                DateTime dt = DateTime.ParseExact(s, dateformats[i], System.Globalization.CultureInfo.CurrentCulture);
                //si es una fecha válida según este formato el año debe quedar comprendido entre rango va´lido que tiene sqlserver.
                if (dt.Year >= 1753 && dt.Year <= 9999) return dt;
            }
            catch (Exception)
            {
                //ignorar
            }
        }
        return null;
    }

    /// <summary>
    /// Get an OLEDB connection
    /// </summary>
    /// <returns>OLEDB connection</returns>
    public OleDbConnection GetConnection() 
    {
        return db;
    }

    /// <summary>
    /// Obtener fecha del sistema
    /// </summary>
    /// <returns></returns>
    public string SysDate()
    {
        return SysDate(0);
    }
    public string SysDate(int incMinutes) 
    {
        if (dbtype==DB_SQLSERVER)
            return incMinutes == 0 ? "GETDATE()" : "DATEADD(minute, " + incMinutes + ", GETDATE())";
        else
            return "NOW";
    }

    /// <summary>
    /// Iniciar transacción
    /// </summary>
    public void BeginTransaction() 
    {
        //En realidad nos apuntamos que hay una transacción activa. Cualquier operación de 
        //inserción debe ajustar la propiedad "Transaction" para que sea efectiva...
        if (transaction == null)
        {
            transaction = GetConnection().BeginTransaction(IsolationLevel.ReadCommitted);
        }
    }

    /// <summary>
    /// Commit de la transacción
    /// </summary>
    public void Commit()
    {
        if (transaction != null)
        {
            transaction.Commit();
            ReleaseTransaction();
        }
    }

    /// <summary>
    /// Rollback de la transacción
    /// </summary>
    public void Rollback()
    {
        if (transaction != null)
        {
            transaction.Rollback();
            ReleaseTransaction();
        }
    }

    /// <summary>
    /// Liberar recursos de la transacción
    /// </summary>
    private void ReleaseTransaction() 
    {
        transaction.Dispose();
        transaction = null;
    }

    /// <summary>
    /// Obtener tipo de base de datos
    /// </summary>
    /// <returns>tipo (constante del tipo DB_)</returns>
    public int GetDbType() 
    {
        return dbtype;
    }

    /// <summary>
    /// Log detallado de excepciones tipo "truncate" a base de datos. 
    /// Se informará de que campo ha fallado y que longitud es la máxima permitida.
    /// </summary>
    /// <param name="tabla">tabla</param>
    /// <param name="campos">lista de campos</param>
    /// <param name="valores">lista de valores</param>
    /// <returns>Mensaje error ampliado</returns>
    public string LogDetailsTruncateException(string tabla, string[] campos, string[] valores)
    {
        Regex regex = new Regex(@"\d{1,2}/\d{1,2}/\d{4}"); // Fecha dd/mm/aaaa
        string msg = "";
        string sql = "";
        string val = "";
        string type = "";
        int i = 0;
        int lorig, ldest;
        DbDataReader cursor = null;
        try
        {
            foreach (string c in campos)
            {
                sql = "SELECT so.name AS Tabla, " +
                      "       sc.name AS Columna, " +
                      "       st.name AS Tipo, " +
                      "       sc.max_length AS Tamano " +
                      "  FROM sys.objects so  " +
                      " INNER JOIN sys.columns sc ON so.object_id = sc.object_id  " +
                      " INNER JOIN sys.types st ON st.system_type_id = sc.system_type_id AND so.name='" + tabla.Replace("[", "").Replace("]", "") + "' and sc.name='" + c.Replace("[", "").Replace("]", "") + "' " +
                      " WHERE so.type = 'U'";
                cursor = this.GetDataReader(sql);
                if (cursor.Read())
                {
                    type = this.GetFieldValue(cursor, 2);
                    ldest = int.Parse(this.GetFieldValue(cursor, 3));
                    val = valores[i];
                    if (val == "NULL" || (type == "datetime"))
                        val = "";
                    lorig = val.Length;
                    if (lorig > ldest)
                    {
                        msg = "El campo " + c + " de la tabla " + tabla + " tiene una longitud de " + ldest +
                            ". Se ha intentado insertar el valor " + valores[i].ToString() + " con longitud " + lorig +
                            " por lo que se ha producido una excepción. ";
                        break;
                    }
                }
                i++;
            }
        }
        catch
        {
            msg = "";
        }
        return msg;
    }
  }

  class UVESqlParser
  {
      const string REGEX_CAMPO = @"(\w+)|(\[( ?\w+){0,}\])";
      const string REGEX_VALOR = @"'[^']'|'((('')?|\W* {0,}('')?|\W*\w+('')?|\W* {0,}('')?|\W*){0,})'|(-?[0-9]\d*(\.\d+)?)|\w+\(([^)]*)\)|NULL";

      public bool Error { get; set; }
      public string StrSql { get; set; }
      private string Tipo { get; set; }
      public string Tabla { get; set; }
      public List<string> Campos { get; set; }
      public List<string> Valores { get; set; }

      public UVESqlParser(string strSql)
      {
          StrSql = GetSqlFromMessage(strSql).Trim().ToUpper();
          Campos = new List<string>();
          Valores = new List<string>();

          if (string.IsNullOrEmpty(StrSql))
              Error = true;
      }

      private string GetSqlFromMessage(string msg)
      {
          if (string.IsNullOrEmpty(msg))
              return string.Empty;

          int index = msg.IndexOf(". SQL: ");
          if (index != -1)
          {
              index += ". SQL: ".Length;
              return msg.Substring(index, msg.Length - index).Trim().ToUpper();
          }
          else
              return string.Empty;
      }
      /// <summary>
      /// Parsea una sentencia SQL Insert o Update. Extrae el nombre de la tabla, los campos y los valores
      /// </summary>
      /// <returns></returns>
      public UVESqlParser Parse()
      {
          string tmpStrSql = StrSql;
          tmpStrSql = ExtraerNombreTabla(tmpStrSql);
          if (Tipo == "INSERT INTO")
          {
              tmpStrSql = ExtraerCamposInsert(tmpStrSql);
              ExtraerValoresInsert(tmpStrSql);
          }
          if (Tipo == "UPDATE")
              ExtraerCamposValoresUpdate(tmpStrSql);

          Error = ObtenerErrores(tmpStrSql);

          return this;
      }

      /// <summary>
      /// Devuelve true si los datos extraidos no son correctos
      /// </summary>
      /// <param name="tmpStrSql"></param>
      /// <returns></returns>
      private bool ObtenerErrores(string tmpStrSql)
      {
          if (string.IsNullOrEmpty(tmpStrSql))
              return true;
          if (string.IsNullOrEmpty(Tipo))
              return true;
          if (string.IsNullOrEmpty(Tabla))
              return true;
          if (Campos.Count == 0)
              return true;
          if (Campos.Count != Valores.Count)
              return true;
          return false;
      }

      /// <summary>
      /// Obtiene el nombre de la tabla y el tipo de sentencia, y devuelve la sentencia sql restante
      /// </summary>
      /// <returns></returns>
      private string ExtraerNombreTabla(string tmpStrSql)
      {
          if (tmpStrSql.StartsWith("INSERT INTO"))
              Tipo = "INSERT INTO";
          else if (tmpStrSql.StartsWith("UPDATE"))
              Tipo = "UPDATE";
          else
          {
              int indInsert = tmpStrSql.IndexOf("INSERT INTO");
              int indUpdate = tmpStrSql.IndexOf("UPDATE");
              if (indInsert != -1)
              {
                  Tipo = "INSERT INTO";
                  tmpStrSql = tmpStrSql.Substring(indInsert, tmpStrSql.Length - indInsert);
              }
              else if (indUpdate != -1)
              {
                  Tipo = "UPDATE";
                  tmpStrSql = tmpStrSql.Substring(indUpdate, tmpStrSql.Length - indUpdate);
              }
              else
                  return string.Empty;
          }

          tmpStrSql = tmpStrSql.Substring(Tipo.Length, tmpStrSql.Length - Tipo.Length).TrimStart();

          Regex rgx = new Regex(@"^(\S+)");

          Tabla = rgx.Match(tmpStrSql).ToString();

          return tmpStrSql.Substring(Tabla.Length, tmpStrSql.Length - Tabla.Length).TrimStart();
      }

      #region Tratamiento inserts
      /// <summary>
      /// Extrae los campos de un insert. La cadena debe empezar por el caracter '('
      /// </summary>
      /// <param name="tmpStrSql"></param>
      /// <returns></returns>
      private string ExtraerCamposInsert(string tmpStrSql)
      {
          if (!tmpStrSql.StartsWith("("))
              return string.Empty;

          int finCampos = tmpStrSql.IndexOf(")") - 1;
          string strCampos = tmpStrSql.Substring(1, finCampos);
          if (strCampos.Length <= 0)
              return string.Empty;

          Regex rgx = new Regex(REGEX_CAMPO);

          var matches = rgx.Matches(strCampos);

          foreach (var match in matches)
              Campos.Add(match.ToString());

          return tmpStrSql.Substring(finCampos + 2).TrimStart();
      }

      /// <summary>
      /// Extrae los valores de un insert. El texto de entrada debe empezar por la cadena VALUES
      /// </summary>
      /// <param name="tmpStrSql"></param>
      /// <returns></returns>
      private string ExtraerValoresInsert(string tmpStrSql)
      {
          if (!tmpStrSql.StartsWith("VALUES"))
              return string.Empty;

          tmpStrSql = tmpStrSql.Substring("VALUES".Length, tmpStrSql.Length - "VALUES".Length).TrimStart();
          int indFinValues = tmpStrSql.LastIndexOf(")");
          if (indFinValues != -1)
              tmpStrSql = tmpStrSql.Substring(0, indFinValues + 1);

          if (!tmpStrSql.StartsWith("(") || !tmpStrSql.EndsWith(")"))
              return string.Empty;

          string strValores = tmpStrSql.Substring(1, tmpStrSql.Length - 2);

          Regex rgx = new Regex(REGEX_VALOR);

          var matches = rgx.Matches(strValores);

          foreach (var match in matches)
              Valores.Add(TrimOne(match.ToString(), "'"));

          return strValores;
      }
      #endregion

      #region Tratamiento updates
      /// <summary>
      /// Extrae los campos y valores de un Uppdate. La cadena de entrada debe empezar por "SET"
      /// </summary>
      /// <param name="tmpStrSql"></param>
      /// <returns></returns>
      private string ExtraerCamposValoresUpdate(string tmpStrSql)
      {
          if (!tmpStrSql.StartsWith("SET"))
              return string.Empty;

          tmpStrSql = tmpStrSql.Substring("SET".Length, tmpStrSql.Length - "SET".Length).TrimStart();

          int finSet = tmpStrSql.LastIndexOf("WHERE");
          if (finSet == -1)
              finSet = tmpStrSql.Length;

          string strSet = tmpStrSql.Substring(0, finSet).Trim();
          string campo;
          string valor;
          int indIgual;
          Regex rgx = new Regex(REGEX_VALOR);

          while (!string.IsNullOrEmpty(strSet))
          {
              indIgual = strSet.IndexOf("=");

              if (indIgual == -1)
                  return tmpStrSql;
              campo = strSet.Substring(0, indIgual).TrimStart(',').Trim();
              strSet = strSet.Substring(indIgual + 1, strSet.Length - indIgual - 1).TrimStart();
              valor = rgx.Match(strSet).ToString();
              strSet = strSet.Substring(valor.Length, strSet.Length - valor.Length).TrimStart();
              if (!string.IsNullOrEmpty(campo) && !string.IsNullOrEmpty(valor))
              {
                  Campos.Add(campo);
                  Valores.Add(TrimOne(valor, "'"));
              }
          }

          return tmpStrSql;
      }
      #endregion

      /// <summary>
      /// Realiza un trim pero sólo una vez
      /// </summary>
      /// <param name="str">Texto</param>
      /// <param name="c">Cadena a eliminar del inicio y del final del texto</param>
      /// <returns></returns>
      public string TrimOne(string str, string c)
      {
          if (str.StartsWith(c) && str.Length > c.Length) str = str.Substring(c.Length, str.Length - c.Length);
          if (str.EndsWith(c) && str.Length > c.Length) str = str.Substring(0, str.Length - c.Length);
          return str;
      }
  }

}
