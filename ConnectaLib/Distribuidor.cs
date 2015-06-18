using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data.OleDb;


namespace ConnectaLib
{
  /// <summary>
  /// Clase para la gestión de fabricantes
  /// </summary>
  public class Distribuidor
  {
    private string distFacRecibeAcuseRecibo = "";
    private string distAlbRecibeAcuseRecibo = "";
    private string distReaprRecibeAcuseRecibo = "";
    private string distEstado = "";
    private string distCliRequiereAvisoNuevo = "";
    private string distCliAvisoNuevoEMailFrom = "";
    private string distCliAvisoNuevoEMailTo = "";
    private string distCliAvisoNuevoEMailFirma = "";
    private string distCliBuscarPorDatosIguales = "";

    //Getters
    public string DISTFacRecibeAcuseRecibo { get { return distFacRecibeAcuseRecibo; } }
    public string DISTAlbRecibeAcuseRecibo { get { return distAlbRecibeAcuseRecibo; } }
    public string DISTReaprRecibeAcuseRecibo { get { return distReaprRecibeAcuseRecibo; } }
    public string DISTEstado { get { return distEstado; } }
    public string DISTCliRequiereAvisoNuevo { get { return distCliRequiereAvisoNuevo; } }
    public string DISTCliAvisoNuevoEMailFrom { get { return distCliAvisoNuevoEMailFrom; } }
    public string DISTCliAvisoNuevoEMailTo { get { return distCliAvisoNuevoEMailTo; } }
    public string DISTCliAvisoNuevoEMailFirma { get { return distCliAvisoNuevoEMailFirma; } }
    public string DISTCliBuscarPorDatosIguales { get { return distCliBuscarPorDatosIguales; } }
    /// <summary>
    /// Obtener parámetros del distribuidor
    /// </summary>
    /// <param name="db"></param>
    /// <param name="agente"></param>
    public bool ObtenerParametros(Database db, string agente)
    { 
        bool isOK = false;
        DbDataReader reader = null;
        distFacRecibeAcuseRecibo = "";
        distAlbRecibeAcuseRecibo = "";
        distReaprRecibeAcuseRecibo = "";
        distEstado = "";
        distCliRequiereAvisoNuevo = "";
        distCliAvisoNuevoEMailFrom = "";
        distCliAvisoNuevoEMailTo = "";
        distCliAvisoNuevoEMailFirma = "";
        distCliBuscarPorDatosIguales = "";

        try
        {
            string sql = "SELECT d.Fac_RecibeAcuseRecibo " +
                         "      ,d.Alb_RecibeAcuseRecibo, d.Reapr_RecibeAcuseRecibo " +
                         "      ,a.Status " +
                         "      ,d.Cli_RequiereAvisoNuevo, d.Cli_AvisoNuevoEMailFrom, d.Cli_AvisoNuevoEMailTo, d.Cli_AvisoNuevoEMailFirma " +
                         "  FROM Distribuidores d " +
                         "  LEFT JOIN Agentes a ON (d.IdcAgente = a.IdcAgente)" +
                         " WHERE d.IdcAgente = " + agente;
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                distFacRecibeAcuseRecibo = db.GetFieldValue(reader, 0);
                distAlbRecibeAcuseRecibo = db.GetFieldValue(reader, 1);
                distReaprRecibeAcuseRecibo = db.GetFieldValue(reader, 2);
                distEstado = db.GetFieldValue(reader, 3);
                distCliRequiereAvisoNuevo = db.GetFieldValue(reader, 4);
                distCliAvisoNuevoEMailFrom= db.GetFieldValue(reader, 5);
                distCliAvisoNuevoEMailTo = db.GetFieldValue(reader, 6);
                distCliAvisoNuevoEMailFirma = db.GetFieldValue(reader, 7);
                isOK = true;
            }
            //Parametros generales
            sql = "Select Parametro, Valor " +
                  "  From ParametrosAgentes " +
                  " Where IdcAgenteOrigen = " + agente + " And IdcAgenteDestino = 0";
            reader = db.GetDataReader(sql);
            while (reader.Read())
            {
                if (db.GetFieldValue(reader, 0) == "DistCli_BuscarPorDatosIguales") distCliBuscarPorDatosIguales = db.GetFieldValue(reader, 1);
                if (db.GetFieldValue(reader, 0) == "Fac_RecibeAcuseRecibe")
                {
                    //Mantenemos compatibilidad con el parámetro que aun mantenemos con la tabla Distribuidores
                    if (Utils.IsBlankField(distFacRecibeAcuseRecibo)) distFacRecibeAcuseRecibo = db.GetFieldValue(reader, 1); 
                }
                isOK = isOK && true;
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return isOK;
    }

    public bool ObtenerParametro_AjustarUMPorPrecio(Database db, string pDist, string pFab)
    {
        bool bResult = false;
        DbDataReader reader = null;
        try
        {
            string sql = "Select Fac_AjustarUMPorPrecio from ClasifInterAgentes where IdcAgenteOrigen = " + pDist + " And IdcAgenteDestino = " + pFab;
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                bResult = db.GetFieldValue(reader, 0).ToUpper().Equals("S");
            }
            if (!bResult)
            {
                reader.Close();
                reader = null;

                sql = "Select Fac_AjustarUMPorPrecio from ClasifInterAgentes where IdcAgenteOrigen = " + pFab + " And IdcAgenteDestino = " + pDist;
                reader = db.GetDataReader(sql);
                if (reader.Read())
                {
                    bResult = db.GetFieldValue(reader, 0).ToUpper().Equals("S");
                }
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return bResult;
    }

    public bool ObtenerParametro_VentasActivas(Database db, string pDist, string pFab)
    {
        bool bResult = false;
        DbDataReader reader = null;
        try
        {
            string sql = "Select VentasActivas from ClasifInterAgentes where IdcAgenteOrigen = " + pFab + " And IdcAgenteDestino = " + pDist;
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                bResult = db.GetFieldValue(reader, 0).ToUpper().Equals("S");
            }
            if (!bResult)
            {
                reader.Close();
                reader = null;

                sql = "Select VentasActivas from ClasifInterAgentes where IdcAgenteOrigen = " + pDist + " And IdcAgenteDestino = " + pFab;
                reader = db.GetDataReader(sql);
                if (reader.Read())
                {
                    bResult = db.GetFieldValue(reader, 0).ToUpper().Equals("S");
                }
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return bResult;
    }

    public bool ObtenerParametro_LiquidacionesAcuerdosActivas(Database db, string pDist, string pFab)
    {
        bool bResult = false;
        DbDataReader reader = null;
        try
        {
            string sql = "Select LiquidacionesAcuerdosActivas from ClasifInterAgentes where IdcAgenteOrigen = " + pFab + " And IdcAgenteDestino = " + pDist;
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                bResult = db.GetFieldValue(reader, 0).ToUpper().Equals("S");
            }
            if (!bResult)
            {
                reader.Close();
                reader = null;

                sql = "Select LiquidacionesAcuerdosActivas from ClasifInterAgentes where IdcAgenteOrigen = " + pDist + " And IdcAgenteDestino = " + pFab;
                reader = db.GetDataReader(sql);
                if (reader.Read())
                {
                    bResult = db.GetFieldValue(reader, 0).ToUpper().Equals("S");
                }
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return bResult;
    }

    public bool ObtenerParametro_LiquidacionesAcuerdosAjustarPorPuntoVerde(Database db, string pDist, string pFab)
    {
        bool bResult = false;
        DbDataReader reader = null;
        try
        {
            string sql = "Select Acue_Liq_AjustarPorPuntoVerde from ClasifInterAgentes where IdcAgenteOrigen = " + pFab + " And IdcAgenteDestino = " + pDist;
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                bResult = db.GetFieldValue(reader, 0).ToUpper().Equals("S");
            }
            if (!bResult)
            {
                reader.Close();
                reader = null;

                sql = "Select Acue_Liq_AjustarPorPuntoVerde from ClasifInterAgentes where IdcAgenteOrigen = " + pDist + " And IdcAgenteDestino = " + pFab;
                reader = db.GetDataReader(sql);
                if (reader.Read())
                {
                    bResult = db.GetFieldValue(reader, 0).ToUpper().Equals("S");
                }
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return bResult;
    }

    public string ObtenerParametro_AsignaUMDefecto(Database db, string pDist, string pFab)
    {
        string sResult = "";
        DbDataReader reader = null;
        try
        {
            string sql = "Select AsignaUMDefecto from ClasifInterAgentes where IdcAgenteOrigen = " + pDist + " And IdcAgenteDestino = " + pFab;
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                sResult = db.GetFieldValue(reader, 0).ToUpper();
            }
            if (Utils.IsBlankField(sResult))
            {
                reader.Close();
                reader = null;

                sql = "Select AsignaUMDefecto from ClasifInterAgentes where IdcAgenteOrigen = " + pFab + " And IdcAgenteDestino = " + pDist;
                reader = db.GetDataReader(sql);
                if (reader.Read())
                {
                    sResult = db.GetFieldValue(reader, 0).ToUpper();
                }
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return sResult;
    }

    public string ObtenerParametro_FechaInicioCargarFacturasDist(Database db, string pDist, string pFab)
    {
        string sResult = "";
        DbDataReader reader = null;
        try
        {
            string sql = "Select FechaInicioCargarFacturasDist from ClasifInterAgentes where IdcAgenteOrigen = " + pDist + " And IdcAgenteDestino = " + pFab;
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                sResult = db.GetFieldValue(reader, 0);
            }
            if (Utils.IsBlankField(sResult))
            {
                reader.Close();
                reader = null;

                sql = "Select FechaInicioCargarFacturasDist from ClasifInterAgentes where IdcAgenteOrigen = " + pFab + " And IdcAgenteDestino = " + pDist;
                reader = db.GetDataReader(sql);
                if (reader.Read())
                {
                    sResult = db.GetFieldValue(reader, 0);
                }
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return sResult;
    }

    /// <summary>
    /// Obtener parámetro general según relación distribuidor - fabricante
    /// </summary>
    /// <param name="db"></param>
    /// <param name="agente"></param>
    public string ObtenerParametro(Database db, string pDist, string pFab, string parametro)
    {
        string bResult = "";
        DbDataReader reader = null;
        try
        {
            string sql = "Select Valor from ParametrosAgentes where IdcAgenteOrigen = " + pDist + " And IdcAgenteDestino = " + pFab + " And Parametro = '" + parametro + "'";
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                bResult = db.GetFieldValue(reader, 0);
            }
            else
            {
                reader.Close();
                reader = null;

                sql = "Select Valor from ParametrosAgentes where IdcAgenteOrigen = " + pFab + " And IdcAgenteDestino = " + pDist + " And Parametro = '" + parametro + "'";
                reader = db.GetDataReader(sql);
                if (reader.Read())
                {
                    bResult = db.GetFieldValue(reader, 0);
                }
            }
        }
        catch
        {
            bResult = "";
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return bResult;
    }

    /// <summary>
    /// Obtener parámetro general distribuidor 
    /// </summary>
    /// <param name="db"></param>
    /// <param name="agente"></param>
    public string ObtenerParametro(Database db, string pDist, string parametro)
    {
        string bResult = "";
        DbDataReader reader = null;
        try
        {
            string sql = "Select Valor from ParametrosAgentes where IdcAgenteOrigen = " + pDist + " And Parametro = '" + parametro + "'";
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                bResult = db.GetFieldValue(reader, 0);
            }
        }
        catch
        {
            bResult = "";
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return bResult;
    }

    /// <summary>
    /// Obtener nombre del distribuidor
    /// </summary>
    /// <param name="db"></param>
    /// <param name="agente"></param>
    public string ObtenerNombre(Database db, string agente)
    {
        string sNom = "";
        DbDataReader reader = null;

        try
        {
            if (Utils.IsBlankField(agente)) return sNom;

            string sql = "Select Nombre " +
                            " From Distribuidores " +
                            " Where IdcAgente = " + agente;
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                sNom = db.GetFieldValue(reader, 0);
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return sNom;
    }
  }
}
