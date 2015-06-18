using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data.OleDb;

namespace ConnectaLib
{
  /// <summary>
  /// Clase que contiene información resumida para el control de datos enviados. 
  /// Desde el SIP de facturas de distribuidor
  /// se mantiene una lista de objetos de esta clase para que en el post-proceso 
  /// se pueda construir un resumen que será enviado por e-mail.
  /// </summary>
  public class ControlDatosEnvios
  {
      public const string TIPODATO_VENTAS = "VTA";
      public const string TIPODATO_SERVICIOSTERCEROS = "SER";
      public const string TIPODATO_REAPROVISIONAMIENTOS = "REA";

    public string idcDistribuidor = "";
    public string idcFabricante = "";
    public DateTime fechaIniDatos;
    public DateTime fechaFinDatos;

    /// <summary>
    /// Constructor
    /// </summary>
    public ControlDatosEnvios(string dist, string fab, string fecIni, string fecFin) 
    {
        idcDistribuidor = dist;
        idcFabricante = fab;
        fechaIniDatos = DateTime.Parse(fecIni);
        fechaFinDatos = DateTime.Parse(fecFin);
    }

    public static void GenerarControlDatosEnviosDistribuidor(ArrayList pDatosEnvios, string pTipoDatoEnvio, string pSipTypeName)
    {
        Database db = Globals.GetInstance().GetDatabase();
        DbDataReader cursor = null;
        string sql = "";

        foreach (ControlDatosEnvios cde in pDatosEnvios)
        {
            string enviaCtrlEnvios = "";
            sql = "SELECT EnviaControlEnvios FROM ClasifInterAgentes " +
                         " WHERE IdcAgenteOrigen = " + cde.idcDistribuidor + " " +
                         " AND IdcAgenteDestino = " + cde.idcFabricante + " ";
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
            {
                enviaCtrlEnvios = db.GetFieldValue(cursor, 0);
            }
            if (cursor != null)
                cursor.Close();
            if (enviaCtrlEnvios != "S")
            {
                string sUsuario = Constants.PREFIJO_USUARIO_AGENTE + cde.idcDistribuidor;

                string sFecEnvio = DateTime.Now.ToShortDateString();
                string fecEnvioAAAMMDD = DateTime.Parse(sFecEnvio).Year.ToString() + DateTime.Parse(sFecEnvio).Month.ToString().PadLeft(2, '0') + DateTime.Parse(sFecEnvio).Day.ToString().PadLeft(2, '0');
                string fecIniDatosAAAMMDD = cde.fechaIniDatos.Year.ToString() + cde.fechaIniDatos.Month.ToString().PadLeft(2, '0') + cde.fechaIniDatos.Day.ToString().PadLeft(2, '0');
                string fecFinDatosAAAMMDD = cde.fechaFinDatos.Year.ToString() + cde.fechaFinDatos.Month.ToString().PadLeft(2, '0') + cde.fechaFinDatos.Day.ToString().PadLeft(2, '0');

                string where = "where IdcAgente = " + cde.idcDistribuidor +
                    " and IdcAgenteDestino = " + cde.idcFabricante +
                    " and TipoDatoEnvio = " + db.ValueForSql(pTipoDatoEnvio) +
                    " and CONVERT(BIGINT,CONVERT(VARCHAR,FechaEnvio,112)) = " + fecEnvioAAAMMDD +
                    " and CONVERT(BIGINT,CONVERT(VARCHAR,FechaIniDatos,112)) = " + fecIniDatosAAAMMDD +
                    " and CONVERT(BIGINT,CONVERT(VARCHAR,FechaFinDatos,112)) = " + fecFinDatosAAAMMDD +
                    " ";
                sql = "select IdcAgente from ControlEnviosAgentes " + where;
                if (Utils.RecordExist(sql))
                {
                    sql = "update ControlEnviosAgentes set " +
                            "  FechaModificacion=" + db.SysDate() + " " +
                            ", UsuarioModificacion=" + db.ValueForSql(sUsuario) + " " +
                            where;
                }
                else
                {
                    sql = "insert into ControlEnviosAgentes (IdcAgente,IdcAgenteDestino,TipoDatoEnvio,FechaEnvio,FechaIniDatos,FechaFinDatos,UsuarioInsercion,UsuarioModificacion) " +
                            "values (" + cde.idcDistribuidor + "," + cde.idcFabricante+
                            "," + db.ValueForSql(pTipoDatoEnvio) +
                            "," + db.DateForSql(sFecEnvio) +
                            "," + db.DateForSql(cde.fechaIniDatos.ToShortDateString()) +
                            "," + db.DateForSql(cde.fechaFinDatos.ToShortDateString()) +
                            "," + db.ValueForSql(sUsuario) +
                            "," + db.ValueForSql(sUsuario) +
                            ")";
                }
                db.ExecuteSql(sql, cde.idcDistribuidor, pSipTypeName);
            }
        }
    }

    /// <summary>
    /// Recaluclar control envios agentes según fecha inicial y final de las líneas de factura eliminadas
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="reader">DbDataReader reader</param>        
    public static void RecalcularControlEnvios(Database db, string distribuidor, string idcFabricante, ref int numRows, ref int numRowsInsert, string fechaMinFacturas, string fechaMaxFacturas, string tipo)
    {
        string sqlSelect = "SELECT IdcAgente,IdcAgenteDestino,TipoDatoEnvio,FechaEnvio,FechaIniDatos,FechaFinDatos,FechaInsercion, "
                         + "       FechaModificacion,UsuarioInsercion,UsuarioModificacion,IndOrigenFicheroControl,ID ";
        string sqlFrom = " FROM ControlEnviosAgentes ";
        string sqlWhere = " WHERE IdcAgente = " + distribuidor;
        string sWhere = "";
        if (!string.IsNullOrEmpty(idcFabricante)) sWhere += " AND IdcAgenteDestino = " + idcFabricante;
        sWhere += " AND TipoDatoEnvio = " + db.ValueForSql(tipo);

        string sql = sqlSelect + sqlFrom + sqlWhere + sWhere;

        numRows = 0;
        numRowsInsert = 0;
        DbDataReader reader = null;
        try
        {
            reader = db.GetDataReader(sql);
            while (reader.Read())
            {
                string id = db.GetFieldValue(reader, 11);
                string fechaIniDatos = db.GetFieldValue(reader, 4);
                string fechaFinDatos = db.GetFieldValue(reader, 5);
                DateTime dtControlIni = DateTime.MinValue;
                DateTime dtControlFin = DateTime.MinValue;
                DateTime dtFacturasIni = DateTime.MinValue;
                DateTime dtFacturasFin = DateTime.MinValue;
                DateTime.TryParse(fechaIniDatos, out dtControlIni);
                DateTime.TryParse(fechaFinDatos, out dtControlFin);
                DateTime.TryParse(fechaMinFacturas, out dtFacturasIni);
                DateTime.TryParse(fechaMaxFacturas, out dtFacturasFin);
                // Si la fecha de control final es menor a la fecha inicial de facturas
                // o la fecha final de facturas es menor a la fecha inicial de control,
                // el período de control está fuera del rango de fechas de factura
                if (!((DateTime.Compare(dtControlFin, dtFacturasIni) < 0) ||
                      (DateTime.Compare(dtFacturasFin, dtControlIni) < 0)))
                {
                    // Si el intervalo de fechas de control esta dentro del intervalo de fechas de factura podemos eliminar el registro
                    if ((DateTime.Compare(dtControlIni, dtFacturasIni) >= 0) &&
                        (DateTime.Compare(dtControlFin, dtFacturasFin) <= 0))
                    {
                        sql = EliminarControlEnvios(id);
                        numRows += db.ExecuteSql(sql); 
                    }
                    else
                    {
                        // Si la fecha final de control es mas grande que la fecha final de facturas y la fecha inicial
                        // de control es mas grande o igual que la fecha inicial de facturas tenemos que conservar la parte 
                        // final del control de envios des de la fecha final de facturas hasta la fecha final de control
                        if ((DateTime.Compare(dtControlFin, dtFacturasFin) > 0) &&
                            (DateTime.Compare(dtControlIni, dtFacturasIni) >= 0))
                        {
                            sql = EliminarControlEnvios(id);
                            numRows += db.ExecuteSql(sql); 
                            sql = InsertarControlEnvios(db, reader, fechaMaxFacturas, fechaFinDatos, distribuidor);
                            numRowsInsert += db.ExecuteSql(sql); 
                        }
                        // Si la fecha inicial de control es inferior a la fecha inicial de facturas y la fecha final 
                        // de control es menor o igual a la fecha finar de facturas tenemos que conservar la parte
                        // inicial del control de envios des de la fecha inicial de control hasta la fecha inicial de facturas
                        else if ((DateTime.Compare(dtControlIni, dtFacturasIni) < 0) &&
                                 (DateTime.Compare(dtControlFin, dtFacturasFin) <= 0))
                        {
                            sql = EliminarControlEnvios(id);
                            numRows += db.ExecuteSql(sql); 
                            sql = InsertarControlEnvios(db, reader, fechaIniDatos, fechaMinFacturas, distribuidor);
                            numRowsInsert += db.ExecuteSql(sql); 
                        }
                        // Si la fecha inicial de control es inferior a la fecha inicial de facturas y la fecha final
                        // de control es superior a la fecha final de facturas tenemos que conservar e insertar dos partes
                        // de control de envios, para el período entre la fecha inicial de control y la fecha inicial de facturas
                        // y para el período entre la fecha final de facturas y la fecha final de control
                        else if ((DateTime.Compare(dtControlIni, dtFacturasIni) < 0) &&
                                 (DateTime.Compare(dtControlFin, dtFacturasFin) > 0))
                        {
                            sql = EliminarControlEnvios(id);
                            numRows += db.ExecuteSql(sql); 
                            sql = InsertarControlEnvios(db, reader, fechaIniDatos, fechaMinFacturas, distribuidor);
                            numRowsInsert += db.ExecuteSql(sql); 
                            sql = InsertarControlEnvios(db, reader, fechaMaxFacturas, fechaFinDatos, distribuidor);
                            numRowsInsert += db.ExecuteSql(sql); 
                        }
                    }
                }
            }
            reader.Close();
            reader = null;
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
    }

    /// <summary>
    /// Obtener la cadena sql para eliminar el registro correspondiente de la tabla ControlEnviosAgentes
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="reader">DbDataReader reader</param>        
    private static string EliminarControlEnvios(string id)
    {
        return "DELETE " +
               "  FROM ControlEnviosAgentes " +
               " WHERE Id = '" + id + "'";               
    }

    /// <summary>
    /// Obtener la cadena sql para insertar el registro correspondiente a la tabla ControlEnviosAgentes
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="reader">DbDataReader reader</param>  
    /// <param name="fechaIni">Fecha inicial control envios</param>  
    /// <param name="fechaFin">Fecha final control envios</param>  
    private static string InsertarControlEnvios(Database db, DbDataReader reader, string fechaIni, string fechaFin, string agent)
    {
        return "INSERT INTO ControlEnviosAgentes " +
               " (IdcAgente, IdcAgenteDestino, TipoDatoEnvio, FechaEnvio, FechaIniDatos, FechaFinDatos, FechaInsercion,  " +
               "  FechaModificacion, UsuarioInsercion, UsuarioModificacion, IndOrigenFicheroControl, IndModificadoSipDel) " +
               " VALUES ( " + db.GetFieldValue(reader, 0) +
               "         ," + db.GetFieldValue(reader, 1) +
               "        ,'" + db.GetFieldValue(reader, 2) + "'" +
               "         ," + db.DateTimeForSql(db.GetFieldValue(reader, 3)) +
               "         ," + db.DateTimeForSql(fechaIni) +
               "         ," + db.DateTimeForSql(fechaFin) +
               "         ," + db.SysDate() +
               "         ," + db.SysDate() +
               "        ,'" + Constants.PREFIJO_USUARIO_AGENTE + agent + "'" +
               "        ,'" + Constants.PREFIJO_USUARIO_AGENTE + agent + "'" +
               "        ,'" + db.GetFieldValue(reader, 10) + "'" +
               "        ,'S')";
    }
  }
}
