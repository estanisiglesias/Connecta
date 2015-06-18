using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;

namespace ConnectaLib
{
  /// <summary>
  /// Clase con operaciones comunes sobre peticiones
  /// </summary>
  public class BDPeticion
  {
    
    public string agent = "";
    public string sipTypeName = "";
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="agent">agente</param>
    public BDPeticion(string agent, string sipTypeName) 
    {
        this.agent = agent;
        this.sipTypeName = sipTypeName;
    }

    private string GetSipTypeName()
    {
        return this.sipTypeName;
    }

    /// <summary>Obtiene el valor de un campo concreto de un campode PeticionesCF</summary>
    public string ValorCampoPeticion(Database db, string pNombreCampo, string pIdcAgente, string pNumPedido, string pEjercicio)
    {
        if (Utils.IsBlankField(pIdcAgente)) return "";
        if (Utils.IsBlankField(pNumPedido)) return "";
        if (Utils.IsBlankField(pEjercicio)) return "";

        DbDataReader reader = null;
        string resultado = "";
        string strSql = " SELECT " + pNombreCampo +
                          " FROM PeticionesCF " +
                          " WHERE IdcAgente = " + pIdcAgente + " " +
                          " AND NumPedido = " + db.ValueForSql(pNumPedido) + " " +
                          " AND Ejercicio = " + db.ValueForSql(pEjercicio);
        reader = db.GetDataReader(strSql);
        if (reader.Read())
        {
              resultado = db.GetFieldValue(reader, 0);
        }
        reader.Close();
        reader = null;
        return resultado;
    }

    /// <summary>Actualiza un dato concreto en un albarán</summary>
    public bool ActualizarPeticion(Database db, string pNombreCampo, string pValorCampo, DbType pTipoCampo, string pIdcAgente, string pNumPedido, string pEjercicio)
    {
        if (pTipoCampo == DbType.String) pValorCampo = db.ValueForSql(pValorCampo);
        else if (pTipoCampo == DbType.DateTime) pValorCampo = db.DateForSql(pValorCampo);
        else pValorCampo = db.ValueForSqlAsNumeric(pValorCampo);
        string strSql = " UPDATE PeticionesCF" +
                        " SET " + pNombreCampo + " = " + pValorCampo + "," +
                        "     FechaModificacion    = GETDATE()," +
                        //"     UsuarioModificacion  = '" + Constants.PREFIJO_USUARIO_AGENTE + agent + "' " +
                        " WHERE IdcAgente        = " + pIdcAgente +
                        " AND NumPedido = " + db.ValueForSql(pNumPedido) +
                        " AND Ejercicio = " + db.ValueForSql(pEjercicio);
        int NRegs = db.ExecuteSql(strSql, agent, GetSipTypeName());
        return NRegs > 0;
    }
  }
}
