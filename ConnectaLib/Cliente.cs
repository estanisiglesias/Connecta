using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data.OleDb;
using System.Collections;

namespace ConnectaLib
{
  /// <summary>
  /// Clase con operaciones comunes sobre clientes
  /// </summary>
  public class Cliente
  {
    private string idcClienteFinal = "";
    private string codigoCliente = "";
    private string nombreCliente = "";
    private string estadoCliente = "";
    private string estadoManualCliente = "";
    private bool excluirDeLiquidacion = false;

    private string codigoClienteFab = "";

    //Getter
    public string IdcClienteFinal { get { return idcClienteFinal; } set { idcClienteFinal = value; } }
    public string CodCliente { get { return codigoCliente; } set { codigoCliente = value; } }
    public string NomCliente { get { return nombreCliente; } set { nombreCliente = value; } }
    public string EstadoCliente { get { return estadoCliente; } set { estadoCliente = value; } }
    public string EstadoManualCliente { get { return estadoManualCliente; } set { estadoManualCliente = value; } }
    public bool ExcluirDeLiquidacion { get { return excluirDeLiquidacion; } set { excluirDeLiquidacion = value; } }
    
    public string CodClienteFab { get { return codigoClienteFab; } }

    public Hashtable htCFAgentes = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
    public Hashtable htCFAgentesNum = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
    public Hashtable htClientesFinales = new Hashtable(StringComparer.InvariantCultureIgnoreCase);

    public struct sCFAgente
    {
        public string idcClienteFinal;
        public string codigoCliente;
        public string nombreCliente;
        public string estadoCliente;
        public string estadoManualCliente;
        public bool excluirDeLiquidacion;        
    }
    public struct sClientesFinales
    {
        public string direccion;
        public string tipoCalle;
        public string calle;
        public string numero;
        public string codigoPostal;
        public string provincia;
        public string codigoPais;
        public string poblacion;
    }

    /// <summary>
    /// Cargar información necesaria de los clientes actuales del agente
    /// </summary>
    /// <param name="db">db</param>
    public void CargarClientesActuales(Database db, string agent)
    {
        htCFAgentes.Clear();
        htCFAgentesNum.Clear();
        htClientesFinales.Clear();
        DbDataReader cursor = null;
        string sql = "select cf.IdcClienteFinal, " +
                     "       cf.CodigoCF, " +
                     "       cf.NombreCF, " +
                     "       cf.Status, " +
                     "       cf.StatusManual, " +
                     "       cf.Acue_Liq_ExcluirDeLiquidacion, " +
                     "       case when (patindex('%[^0-9,.]%',ltrim(rtrim(cf.CodigoCF)))=0 and left(ltrim(cf.CodigoCF),1) not in ('.',',') and right(rtrim(cf.CodigoCF),1) not in ('.',',')) " +
                     "            then cast(replace(cf.CodigoCF,',','.') as numeric(25,0)) else null end as CodigoCFNum, " +
                     "       c.Direccion, c.TipoCalle, c.Calle, c.Numero, c.CodigoPostal, c.Provincia, c.CodigoPais, c.Poblacion " +
                     "  from CFAgentes cf " +
                     "  left join clientesFinales c on cf.idcClienteFinal=c.idcAgente " +
                     " where cf.IdcAgente = " + agent;
        cursor = db.GetDataReader(sql);
        while (cursor.Read())
        {
            sCFAgente cf = new sCFAgente();
            cf.idcClienteFinal = db.GetFieldValue(cursor, 0);
            cf.codigoCliente = db.GetFieldValue(cursor, 1);
            cf.nombreCliente = db.GetFieldValue(cursor, 2);
            cf.estadoCliente = db.GetFieldValue(cursor, 3);
            cf.estadoManualCliente = db.GetFieldValue(cursor, 4);
            cf.excluirDeLiquidacion = (db.GetFieldValue(cursor, 5) == "S" ? true : false);            
            if (!htCFAgentes.ContainsKey(cf.codigoCliente))
                htCFAgentes.Add(cf.codigoCliente, cf);
            Double codigoCFNum = 0;
            Double.TryParse(db.GetFieldValue(cursor, 6), out codigoCFNum);
            if (!htCFAgentesNum.ContainsKey(codigoCFNum))
                htCFAgentesNum.Add(codigoCFNum, cf);            
            sClientesFinales c = new sClientesFinales();
            c.direccion = db.GetFieldValue(cursor, 7);
            c.tipoCalle = db.GetFieldValue(cursor, 8);
            c.calle = db.GetFieldValue(cursor, 9);
            c.numero = db.GetFieldValue(cursor, 10);
            c.codigoPostal = db.GetFieldValue(cursor, 11);
            c.provincia = db.GetFieldValue(cursor, 12);
            c.codigoPais = db.GetFieldValue(cursor, 13);
            c.poblacion = db.GetFieldValue(cursor, 14);
            if (!htClientesFinales.ContainsKey(cf.idcClienteFinal))
                htClientesFinales.Add(cf.idcClienteFinal, c);
        }
        cursor.Close();
        cursor = null;
    }

    /// <summary>
    /// Comprobar cliente
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="albaran">registro de albarán</param>
    /// <returns>true si es correcto</returns>
    public bool ComprobarCliente(Database db, string codigoAgente, string codCli)
    {
        return ComprobarCliente(db, codigoAgente, codCli, false, true);
    }

    public bool ComprobarCliente(Database db, string codigoAgente, string codCli, bool pBuscarAlternativo, bool pBuscarNumerico)
    {
        bool isOK = false;
        DbDataReader reader = null;
        idcClienteFinal = "";
        codigoCliente = "";
        nombreCliente = "";
        estadoCliente = "";
        estadoManualCliente = "";
        excluirDeLiquidacion = false;

        try
        {
            string sql = "SELECT IdcClienteFinal, CodigoCF, NombreCF, Status, StatusManual, Acue_Liq_ExcluirDeLiquidacion FROM CFAgentes WHERE IdcAgente = " + codigoAgente + " ";
            Double numCodigoCliFab = 0;
            if (pBuscarNumerico && Double.TryParse(codCli, out numCodigoCliFab))
            {
                sql += " And ";
                sql += "   ( ";
                sql += "     ( ";
                sql += "      CodigoCF = '" + codCli + "' ";
                sql += "      OR (patindex('%[^0-9,.]%',ltrim(rtrim(CodigoCF)))=0 and left(ltrim(CodigoCF),1) not in ('.',',') and right(rtrim(CodigoCF),1) not in ('.',',') "; 
                sql += "          and cast(replace(CodigoCF,',','.') as numeric(25,0)) = " + db.ValueForSqlAsNumeric(numCodigoCliFab.ToString()) + ") ";
                sql += "     ) ";
                if (pBuscarAlternativo)
                {
                    sql += "     OR ";
                    sql += "     ( ";
                    sql += "      CFAgentes.CodigoCFAlt = '" + codCli + "' ";
                    sql += "      OR (patindex('%[^0-9,.]%',ltrim(rtrim(CodigoCFAlt)))=0 and left(ltrim(CodigoCFAlt),1) not in ('.',',') and right(rtrim(CodigoCFAlt),1) not in ('.',',') "; 
                    sql += "          and cast(replace(CodigoCFAlt,',','.') as numeric(25,0)) = " + db.ValueForSqlAsNumeric(numCodigoCliFab.ToString()) + ") ";
                    sql += "     ) ";
                }
                sql += "   ) ";
            }
            else
            {
                sql += " And ";
                sql += "   ( ";
                sql += "     CFAgentes.CodigoCF = '" + codCli + "' ";
                if (pBuscarAlternativo)
                {
                    sql += "     OR ";
                    sql += "     CFAgentes.CodigoCFAlt = '" + codCli + "' ";
                }
                sql += "   ) ";
            }
            sql += " Order by FechaAlta DESC ";
            
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                idcClienteFinal = db.GetFieldValue(reader, 0);
                codigoCliente = db.GetFieldValue(reader, 1);
                nombreCliente = db.GetFieldValue(reader, 2);
                estadoCliente = db.GetFieldValue(reader, 3);
                estadoManualCliente = db.GetFieldValue(reader, 4);
                excluirDeLiquidacion = (db.GetFieldValue(reader, 5) == "S"? true : false);
                isOK = true;
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return isOK;
    }

    public bool ComprobarClienteNew(Database db, string codigoAgente, string codCli, bool pBuscarAlternativo, bool pBuscarNumerico)
    {
        bool isOK = false;
        DbDataReader reader = null;
        idcClienteFinal = "";
        codigoCliente = "";
        nombreCliente = "";
        estadoCliente = "";
        estadoManualCliente = "";
        try
        {
            string sql = "select IdcClienteFinal, CodigoCF, NombreCF, Status, StatusManual from CFAgentes where IdcAgente = " + codigoAgente + " ";
            Double numCodigoCliFab = 0;
            if (pBuscarNumerico && Double.TryParse(codCli, out numCodigoCliFab))
            {
                sql += " And ";
                sql += "   ( ";
                sql += "      patindex('%[^0-9,.]%',ltrim(rtrim(coalesce(CodigoCF,''))))=0 and left(ltrim(CodigoCF),1) not in ('.',',') and right(rtrim(CodigoCF),1) not in ('.',',') ";
                sql += "      and cast(replace(coalesce(CodigoCF,''),',','.') as numeric(25,0)) = " + db.ValueForSqlAsNumeric(numCodigoCliFab.ToString());
                if (pBuscarAlternativo)
                {
                    sql += "    OR ";
                    sql += "    (";
                    sql += "     patindex('%[^0-9,.]%',ltrim(rtrim(coalesce(CodigoCFAlt,''))))=0  and left(ltrim(CodigoCFAlt),1) not in ('.',',') and right(rtrim(CodigoCFAlt),1) not in ('.',',') ";
                    sql += "     and cast(replace(coalesce(CodigoCFAlt,''),',','.') as numeric(25,0)) = " + db.ValueForSqlAsNumeric(numCodigoCliFab.ToString());
                    sql += "    )";
                }
                sql += "   ) ";
            }
            else
            {
                sql += " And ";
                sql += "   ( ";
                sql += "     CFAgentes.CodigoCF = '" + codCli + "' ";
                if (pBuscarAlternativo)
                {
                    sql += "     OR ";
                    sql += "     CFAgentes.CodigoCFAlt = '" + codCli + "' ";
                }
                sql += "   ) ";
            }
            sql += " Order by FechaAlta DESC ";

            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                idcClienteFinal = db.GetFieldValue(reader, 0);
                codigoCliente = db.GetFieldValue(reader, 1);
                nombreCliente = db.GetFieldValue(reader, 2);
                estadoCliente = db.GetFieldValue(reader, 3);
                estadoManualCliente = db.GetFieldValue(reader, 4);
                isOK = true;
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return isOK;
    }

    /// <summary>
    /// Comprobar cliente ficticio
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <returns>true si es correcto</returns>
    public bool ComprobarClienteFicticio(Database db, string idcFabricante, string codCli)
    {
        return ComprobarClienteFicticio(db, idcFabricante, codCli, "");
    }

    /// <summary>
    /// Comprobar cliente ficticio
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <returns>true si es correcto</returns>
    public bool ComprobarClienteFicticio(Database db, string idcFabricante, string codCli, string idcDistribuidor)
    {
        bool isOK = false;
        DbDataReader reader = null;
        codigoCliente = "";
        nombreCliente = "";
        codigoClienteFab = "";
        try
        {
            string sql = "SELECT CFA.IdcClienteFinal, CFA.NombreCF, CFA.CodigoCF "
                            + " FROM ClasificacionAgente CA"
                            + " LEFT JOIN CFAgentes CFA ON CA.IdcAgente = CFA.IdcAgente And CA.Codigo = CFA.CodigoCF "
                            + " WHERE CA.EntidadClasificada = 'PRESCLFI' "
                            + " AND CA.IdcAgente = " + idcFabricante
                            + " AND CA.Codigo = '" + codCli + "'";
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                codigoCliente = db.GetFieldValue(reader, 0);
                nombreCliente = db.GetFieldValue(reader, 1);
                codigoClienteFab = db.GetFieldValue(reader, 2);
                if (Utils.IsBlankField(codigoCliente))
                {
                    codigoCliente = "";
                    nombreCliente = "";
                    codigoClienteFab = "";
                }
                else
                {
                    isOK = true;
                }
            }
            reader.Close();
            reader = null;
            
            if (!isOK && !Utils.IsBlankField(idcDistribuidor))
            {
                sql = "SELECT CFA.IdcClienteFinal, CFA.NombreCF, CFA.CodigoCF "
                        + " FROM CFCodificacionAgentes CFCA "
                        + " INNER JOIN ClasificacionAgente CA "
                        + "   ON CA.IdcAgente = CFCA.IdcFabricante "
                        + "   AND CA.Codigo = CFCA.CodigoCliFab "
                        + " LEFT JOIN CFAgentes CFA "
                        + "   ON CFCA.IdcFabricante = CFA.IdcAgente "
                        + "   AND CFCA.CodigoCliFab = CFA.CodigoCF "
                        + " WHERE CFCA.IdcAgente = " + idcDistribuidor
                        + " AND CFCA.IdcFabricante = " + idcFabricante
                        + " AND CFCA.CodigoCF = '" + codCli + "' " ;
                reader = db.GetDataReader(sql);
                if (reader.Read())
                {
                    codigoCliente = db.GetFieldValue(reader, 0);
                    nombreCliente = db.GetFieldValue(reader, 1);
                    codigoClienteFab = db.GetFieldValue(reader, 2);
                    if (Utils.IsBlankField(codigoCliente))
                    {
                        codigoCliente = "";
                        nombreCliente = "";
                        codigoClienteFab = "";
                    }
                    else
                    {
                        isOK = true;
                    }
                }
                reader.Close();
                reader = null;
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return isOK;
    }

    public string JerarquiaCliente(Database db, string pIdcFabricante, string pCodCliFab, string fechaControl)
    {
        DbDataReader reader = null;
        string resultado = "";
        string clienteBusqueda = pCodCliFab;
        bool continuarBuscando = true;
        int contadorBusqueda = 0;

        while (continuarBuscando)
        {
            string strSql = "Select cfja.CodigoClientePadre " +
                    " From JerarquiasAgentes as cfja " +
                    " Where cfja.IdcAgente = " + pIdcFabricante +
                    " And cfja.CodigoClienteHijo = " + db.ValueForSql(clienteBusqueda) +
                    " And (" + db.DateForSql(fechaControl) + " Between cfja.FechaIniVigencia And cfja.FechaFinVigencia) ";
            reader = db.GetDataReader(strSql);
            if (reader.Read())
            {
                clienteBusqueda = db.GetFieldValue(reader, 0);  //El próximo elemento de búqueda ahora será el padre
                resultado += ",'" + clienteBusqueda + "'"; //Añadimos un elemento más a la jerarquia

                contadorBusqueda++;
                if (contadorBusqueda > 20)
                    continuarBuscando = false;
            }
            else
            {
                continuarBuscando = false;
            }
            reader.Close();
            reader = null;
        }
        return resultado;
    }
    /// <summary>
    /// Crea agente
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <returns>identificador del agente</returns>
    public int CreaAgente(Database db, string agent, string sipType)
    {
        int idcAgente = 0;
        DbDataReader cursor = null;
        try
        {
            string sql = "insert into Agentes(Tipo,FechaAlta) values(" + db.ValueForSql("C") + "," + db.SysDate() + ")";
            db.ExecuteSql(sql, agent, sipType);

            if (db.GetDbType() == Database.DB_SQLSERVER)
                sql = "SELECT SCOPE_IDENTITY() AS [SCOPE_IDENTITY]";   //Obtener contador asignado ya que es un autonumérico
            else
                sql = "select max(IdcAgente) from Agentes";
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
                idcAgente = Int32.Parse(db.GetFieldValue(cursor, 0));
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
        return idcAgente;
    }

    /// <summary>
    /// Obtener identificador del último cliente dado de alta
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <returns>identificador del último cliente dado de alta</returns>
    public Int32 ObtenerUltimoIdcClienteFinal(Database db)
    {
        Int32 idcClienteFinal = 0;
        DbDataReader cursor = null;
        try
        {
            string sql = "";
            if (db.GetDbType() == Database.DB_SQLSERVER)
                sql = "SELECT SCOPE_IDENTITY() AS [SCOPE_IDENTITY]";   //Obtener contador asignado ya que es un autonumérico
            else
                sql = "select max(IdcAgente) from ClientesFinales";
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
                idcClienteFinal = Int32.Parse(db.GetFieldValue(cursor, 0));
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
        return idcClienteFinal;
    }

    /// <summary>
    /// Crea cliente final
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="idcAgente">id de agente</param>
    /// <param name="nombre">nombre</param>
    /// <param name="razonSocial">razonSocial</param>
    /// <param name="cif">cif</param>
    /// <param name="direccion">direccion</param>
    /// <param name="tipoCalle">tipoCalle</param>
    /// <param name="calle">calle</param>
    /// <param name="numero">numero</param>
    /// <param name="poblacion">poblacion</param>
    /// <param name="codigoPostal">codigoPostal</param>
    /// <param name="codigoPais">codigoPais</param>
    /// <param name="paginaWeb">paginaWeb</param>
    /// <param name="telefono1">telefono1</param>
    /// <param name="telefono2">telefono2</param>
    /// <param name="fax">fax</param>
    /// <param name="email">email</param>
    /// <param name="personaContacto">personaContacto</param>    
    /// <param name="codigoMoneda">codigoMoneda</param>    
    /// <param name="puntoOperacional">puntoOperacional</param>    
    /// <param name="codigoINE">codigoINE</param>    
    /// <param name="barrio">barrio</param>    
    /// <param name="agent">agent</param>    
    /// <param name="location">location</param>    
    /// <param name="sipType">sipType</param>    
    public void CreaClienteFinal(Database db, string nombre, string razonSocial, string cif, string direccion,
        string tipoCalle, string calle, string numero, string poblacion, string codigoPostal, string codigoPais, string paginaWeb, 
        string telefono1, string telefono2, string fax, string email, string personaContacto, string codigoMoneda, 
        string puntoOperacional, string codigoINE, string barrio, string agent, string location, string sipType)
    {
        bool asignarIndFia = (direccion.ToUpper().Replace("Ó", "O").Contains(Constants.CLIENTES_DISTR_SIN_DIRECCION + " " + codigoCliente) && string.IsNullOrEmpty(poblacion));

        string sql = "insert into ClientesFinales (Nombre,RazonSocial,CIF,Direccion" +
                    ",TipoCalle,Calle,Numero,Poblacion,CodigoPostal,Provincia,CodigoPais" +
                    ",PaginaWEB,Telefono1,Telefono2,FAX,Email,PersonaContacto,CodigoMoneda,PuntoOperacional,CodigoINE,Barrio,TimeStampAlta" +
                    (asignarIndFia ? ",N_IndFia" : "") +
                    ")" +
                    " values " +
                    "(" + ((nombre.ToUpper().Contains(Constants.CLIENTES_DISTR_SIN_DESCRIPCION + " " + codigoCliente) && !Utils.IsBlankField(razonSocial))? "''" : ((nombre.Trim() == "." || nombre.Trim() == "#") ? "''" : db.ValueForSql(nombre.ToUpper()))) +
                    "," + db.ValueForSql(razonSocial.ToUpper()) +
                    "," + db.ValueForSql(cif) +
                    "," + db.ValueForSqlOK(Utils.StringTruncate(direccion,100)) +
                    "," + db.ValueForSql(Utils.StringTruncate(tipoCalle,3)) +
                    "," + db.ValueForSql(Utils.StringTruncate(calle,85)) +
                    "," + db.ValueForSql(Utils.StringTruncate(numero,20)) +
                    "," + db.ValueForSqlOK(poblacion) +
                    "," + db.ValueForSql(codigoPostal) +
                    "," + db.ValueForSql(Utils.ObtenerProvincia(codigoPostal, location)) +
                    "," + db.ValueForSql(Utils.ObtenerPais(codigoPais, agent, sipType)) +
                    "," + db.ValueForSql(Utils.StringTruncate(paginaWeb, 60)) +
                    "," + db.ValueForSql(Utils.StringTruncate(telefono1, 20)) +
                    "," + db.ValueForSql(Utils.StringTruncate(telefono2, 20)) +
                    "," + db.ValueForSql(Utils.StringTruncate(fax, 20)) +
                    "," + db.ValueForSql(Utils.StringTruncate(email, 50)) +
                    "," + db.ValueForSql(Utils.StringTruncate(personaContacto, 100)) +
                    "," + db.ValueForSql(Utils.ObtenerMoneda(db, agent, sipType, codigoMoneda)) +
                    "," + db.ValueForSql(puntoOperacional) +
                    "," + db.ValueForSql(Utils.StringTruncate(codigoINE, 20)) +
                    "," + db.ValueForSql(Utils.StringTruncate(barrio, 50)) +
                    "," + db.SysDate() +
                    (asignarIndFia ? ",'0'" : "") +
                    ")";
        db.ExecuteSql(sql, agent, sipType);
    }
    /// <summary>
    /// Crea registro en CFAgentes
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="nombre">nombre</param>
    /// <param name="razonSocial">razonSocial</param>
    /// <param name="clienteFinal">clienteFinal</param>
    /// <param name="codigoCliente">codigoCliente</param>
    /// <param name="status">status</param>
    /// <param name="clasif1">clasif1</param>
    /// <param name="clasif2">clasif2</param>
    /// <param name="clasif3">clasif3</param>
    /// <param name="clasif4">clasif4</param>
    /// <param name="clasif5">clasif5</param>
    /// <param name="clasif6">clasif6</param>
    /// <param name="clasif7">clasif7</param>
    /// <param name="idcclasif1">idcclasif1</param>
    /// <param name="idcclasif2">idcclasif2</param>
    /// <param name="idcclasif3">idcclasif3</param>
    /// <param name="idcclasif4">idcclasif4</param>
    /// <param name="idcclasif5">idcclasif5</param>
    /// <param name="idcclasif6">idcclasif6</param>
    /// <param name="idcclasif7">idcclasif7</param>
    /// <param name="formaPago">formaPago</param>
    /// <param name="idcFormaPago">idcFormaPago</param>
    /// <param name="recargoEquivalencia">recargoEquivalencia</param>
    /// <param name="horarioApertura">horarioApertura</param>
    /// <param name="horarioVisita">horarioVisita</param>
    /// <param name="vacaciones">vacaciones</param>
    /// <param name="fechaAlta">fechaAlta</param>
    /// <param name="fechaBaja">fechaBaja</param>
    /// <param name="motivoBaja">motivoBaja</param>
    /// <param name="idcMotivoBaja">idcMotivoBaja</param>
    /// <param name="agent">agent</param>
    /// <param name="sipType">sipType</param>   
    public void CreaCFAgentes(Database db, int clienteFinal, string codigoCliente, string nombre, string razonSocial, string status,
        string clasif1, string clasif2, string clasif3, string clasif4, string clasif5, string clasif6, string clasif7, string clasif8, string clasif9, string clasif10,
        string clasif11, string clasif12, string clasif13, string clasif14, string clasif15, string clasif16, string clasif17, string clasif18, string clasif19, string clasif20,
        string clasif21, string clasif22, string clasif23, string clasif24, string clasif25, string clasif26, string clasif27, string clasif28, string clasif29, string clasif30,
        string idcclasif1, string idcclasif2, string idcclasif3, string idcclasif4, string idcclasif5, string idcclasif6, string idcclasif7, string idcclasif8, string idcclasif9, string idcclasif10,
        string idcclasif11, string idcclasif12, string idcclasif13, string idcclasif14, string idcclasif15, string idcclasif16, string idcclasif17, string idcclasif18, string idcclasif19, string idcclasif20,
        string idcclasif21, string idcclasif22, string idcclasif23, string idcclasif24, string idcclasif25, string idcclasif26, string idcclasif27, string idcclasif28, string idcclasif29, string idcclasif30,
        string formaPago, string idcFormaPago, string recargoEquivalencia, string horarioApertura, string horarioVisita, string vacaciones, 
        string fechaAlta, string fechaBaja, string motivoBaja, string idcMotivoBaja, string agent, string sipType, string indCreadoAutomatico,
        string areaNielsen, string canalNielsen, string mercadoNielsen1, string mercadoNielsen2, string mercadoNielsen3, string mercadoNielsen4, string mercadoNielsen5)
    {
        //Asignar la razón social si el nombre está vacío
        string nomCli = nombre;
        if ((nomCli.Trim() == "." || nomCli.Trim() == "#"))
            nomCli = "";
        if (Utils.IsBlankField(nomCli))
            nomCli = razonSocial;
        //Recortar el nombre si supera la longitud de 50
        if (nomCli.Trim().Length > 50)
            nomCli = nomCli.Trim().Remove(50);
        else
            nomCli = nomCli.Trim();

        string sql = "insert into CFAgentes (IdcAgente,IdcClienteFinal,CodigoCF,NombreCF,FechaAlta,FechaModificacion,Status" +
                       ",Clasificacion1,Clasificacion2,Clasificacion3,Clasificacion4,Clasificacion5,Clasificacion6,Clasificacion7,Clasificacion8,Clasificacion9,Clasificacion10" +
                       ",Clasificacion11,Clasificacion12,Clasificacion13,Clasificacion14,Clasificacion15,Clasificacion16,Clasificacion17,Clasificacion18,Clasificacion19,Clasificacion20" +
                       ",Clasificacion21,Clasificacion22,Clasificacion23,Clasificacion24,Clasificacion25,Clasificacion26,Clasificacion27,Clasificacion28,Clasificacion29,Clasificacion30" +
                       ",IdcClasificacion1,IdcClasificacion2,IdcClasificacion3,IdcClasificacion4,IdcClasificacion5,IdcClasificacion6,IdcClasificacion7,IdcClasificacion8,IdcClasificacion9,IdcClasificacion10" +
                       ",IdcClasificacion11,IdcClasificacion12,IdcClasificacion13,IdcClasificacion14,IdcClasificacion15,IdcClasificacion16,IdcClasificacion17,IdcClasificacion18,IdcClasificacion19,IdcClasificacion20" +
                       ",IdcClasificacion21,IdcClasificacion22,IdcClasificacion23,IdcClasificacion24,IdcClasificacion25,IdcClasificacion26,IdcClasificacion27,IdcClasificacion28,IdcClasificacion29,IdcClasificacion30" +
                       ",StatusSalida,FormaPago,IdcFormaPago,RecargoEquivalencia,HorarioApertura,HorarioVisita,Vacaciones,FechaAltaAgente,FechaBajaAgente" +
                       ",MotivoBaja,IdcMotivoBaja,IndCreadoAutomatico" +
                       ",AreaNielsen, CanalNielsen, MercadoNielsen1, MercadoNielsen2, MercadoNielsen3, MercadoNielsen4, MercadoNielsen5) " +
                       "values(" + agent + "," + clienteFinal + "," + db.ValueForSql(codigoCliente) + "," + db.ValueForSql(nomCli.ToUpper()) + "," + db.SysDate() + "," + db.SysDate();
        if (!Utils.IsBlankField(status))
            sql += "," + db.ValueForSql(status);
        else
            sql += "," + db.ValueForSql(Constants.ESTADO_ACTIVO);
        sql += "," + db.ValueForSql(clasif1) + "," + db.ValueForSql(clasif2) + "," + db.ValueForSql(clasif3) + "," + db.ValueForSql(clasif4) + "," + db.ValueForSql(clasif5) + "," + db.ValueForSql(clasif6) + "," + db.ValueForSql(clasif7) + "," + db.ValueForSql(clasif8) + "," + db.ValueForSql(clasif9) + "," + db.ValueForSql(clasif10) +
             "," + db.ValueForSql(clasif11) + "," + db.ValueForSql(clasif12) + "," + db.ValueForSql(clasif13) + "," + db.ValueForSql(clasif14) + "," + db.ValueForSql(clasif15) + "," + db.ValueForSql(clasif16) + "," + db.ValueForSql(clasif17) + "," + db.ValueForSql(clasif18) + "," + db.ValueForSql(clasif19) + "," + db.ValueForSql(clasif20) +
             "," + db.ValueForSql(clasif21) + "," + db.ValueForSql(clasif22) + "," + db.ValueForSql(clasif23) + "," + db.ValueForSql(clasif24) + "," + db.ValueForSql(clasif25) + "," + db.ValueForSql(clasif26) + "," + db.ValueForSql(clasif27) + "," + db.ValueForSql(clasif28) + "," + db.ValueForSql(clasif29) + "," + db.ValueForSql(clasif30) +
             "," + db.ValueForSql(idcclasif1) + "," + db.ValueForSql(idcclasif2) + "," + db.ValueForSql(idcclasif3) + "," + db.ValueForSql(idcclasif4) + "," + db.ValueForSql(idcclasif5) + "," + db.ValueForSql(idcclasif6) + "," + db.ValueForSql(idcclasif7) + "," + db.ValueForSql(idcclasif8) + "," + db.ValueForSql(idcclasif9) + "," + db.ValueForSql(idcclasif10) +
             "," + db.ValueForSql(idcclasif11) + "," + db.ValueForSql(idcclasif12) + "," + db.ValueForSql(idcclasif13) + "," + db.ValueForSql(idcclasif14) + "," + db.ValueForSql(idcclasif15) + "," + db.ValueForSql(idcclasif16) + "," + db.ValueForSql(idcclasif17) + "," + db.ValueForSql(idcclasif18) + "," + db.ValueForSql(idcclasif19) + "," + db.ValueForSql(idcclasif20) +
             "," + db.ValueForSql(idcclasif21) + "," + db.ValueForSql(idcclasif22) + "," + db.ValueForSql(idcclasif23) + "," + db.ValueForSql(idcclasif24) + "," + db.ValueForSql(idcclasif25) + "," + db.ValueForSql(idcclasif26) + "," + db.ValueForSql(idcclasif27) + "," + db.ValueForSql(idcclasif28) + "," + db.ValueForSql(idcclasif29) + "," + db.ValueForSql(idcclasif30) +
             "," + db.ValueForSql(Constants.ESTADO_ACTIVO) +
             "," + db.ValueForSql(formaPago) +
             "," + db.ValueForSql(idcFormaPago) +
             "," + db.ValueForSql(recargoEquivalencia) +
             "," + db.ValueForSql(Utils.StringTruncate(horarioApertura, 20)) +
             "," + db.ValueForSql(Utils.StringTruncate(horarioVisita, 20)) +
             "," + db.ValueForSql(vacaciones) +
             "," + db.DateForSql(fechaAlta) +
             "," + db.DateForSql(fechaBaja) +
             "," + db.ValueForSql(motivoBaja) +
             "," + db.ValueForSql(idcMotivoBaja) +
             "," + db.ValueForSql(indCreadoAutomatico) +
             "," + db.ValueForSql(areaNielsen) +
             "," + db.ValueForSql(canalNielsen) +
             "," + db.ValueForSql(mercadoNielsen1) +
             "," + db.ValueForSql(mercadoNielsen2) +
             "," + db.ValueForSql(mercadoNielsen3) +
             "," + db.ValueForSql(mercadoNielsen4) +
             "," + db.ValueForSql(mercadoNielsen5) +
             ")";        
        db.ExecuteSql(sql, agent, sipType);
    }
    public void CreaCFAgentesFab(Database db, Int32 clienteFinal, string codigoCliente, string codigoClienteAlt, string nombre, string razonSocial,
        string status, string clasif1, string clasif2, string clasif3, string clasif4, string clasif5, string clasif6, string clasif7, string clasif8, string clasif9, string clasif10,
        string clasif11, string clasif12, string clasif13, string clasif14, string clasif15, string clasif16, string clasif17, string clasif18, string clasif19, string clasif20,
        string clasif21, string clasif22, string clasif23, string clasif24, string clasif25, string clasif26, string clasif27, string clasif28, string clasif29, string clasif30,
        string jerarquia, string codigoDistribuidor, string libre1, string libre2, string libre3, string libre4, string coordX, string coordY, 
        string EsSolicitante, string EsPdV, string Solicitante, string AreaNielsen, string CanalNielsen,
        string MercadoNielsen1, string MercadoNielsen2, string MercadoNielsen3, string MercadoNielsen4, string MercadoNielsen5,
        string PotencialCompra1, string PotencialCompra2, string PotencialCompra3, string PotencialCompra4, string PotencialCompra5,
        string agent, string sipType, string indCreadoAutomatico)
      {
          //Asignar la razón social si el nombre está vacío
          string nomCli = nombre;
          if (Utils.IsBlankField(nomCli))
              nomCli = razonSocial;
          //Recortar el nombre si supera la longitud de 50
          if (nomCli.Trim().Length > 50)
              nomCli = nomCli.Trim().Remove(50);
          else
              nomCli = nomCli.Trim();

          string sql = "insert into CFAgentes (IdcAgente,IdcClienteFinal,CodigoCF,CodigoCFAlt,NombreCF,FechaAlta,FechaModificacion,Status" +
                         ",Clasificacion1,Clasificacion2,Clasificacion3,Clasificacion4,Clasificacion5,Clasificacion6,Clasificacion7,Clasificacion8,Clasificacion9,Clasificacion10" +
                         ",Clasificacion11,Clasificacion12,Clasificacion13,Clasificacion14,Clasificacion15,Clasificacion16,Clasificacion17,Clasificacion18,Clasificacion19,Clasificacion20" +
                         ",Clasificacion21,Clasificacion22,Clasificacion23,Clasificacion24,Clasificacion25,Clasificacion26,Clasificacion27,Clasificacion28,Clasificacion29,Clasificacion30" +
                         ",Jerarquia,CodigoDistribuidor,Alb_NumPedClieOblig,Alb_MascaraNumPedClie,Reapr_NumPedClieOblig,Reapr_MascaraNumPedClie" +
                         ",Libre3,Libre4,CoordX,CoordY,EsSolicitante,EsPdV,Solicitante,AreaNielsen,CanalNielsen"+
                         ",MercadoNielsen1,MercadoNielsen2,MercadoNielsen3,MercadoNielsen4,MercadoNielsen5" +
                         ",PotencialCompra1,PotencialCompra2,PotencialCompra3,PotencialCompra4,PotencialCompra5" +
                         ",IndCreadoAutomatico) " +
                         "values(" +
                         agent +
                         "," + clienteFinal +
                         "," + db.ValueForSql(codigoCliente) +
                         "," + db.ValueForSql(codigoClienteAlt) +
                         "," + db.ValueForSql(nomCli.ToUpper()) +
                         "," + db.SysDate() +
                         "," + db.SysDate();
          if (!Utils.IsBlankField(status))
              sql += "," + db.ValueForSql(status);
          else
              sql += "," + db.ValueForSql(Constants.ESTADO_ACTIVO);
          sql += "," + db.ValueForSql(clasif1) +
               "," + db.ValueForSql(clasif2) +
               "," + db.ValueForSql(clasif3) +
               "," + db.ValueForSql(clasif4) +
               "," + db.ValueForSql(clasif5) +
               "," + db.ValueForSql(clasif6) +
               "," + db.ValueForSql(clasif7) +
               "," + db.ValueForSql(clasif8) +
               "," + db.ValueForSql(clasif9) +
               "," + db.ValueForSql(clasif10) +
               "," + db.ValueForSql(clasif11) +
               "," + db.ValueForSql(clasif12) +
               "," + db.ValueForSql(clasif13) +
               "," + db.ValueForSql(clasif14) +
               "," + db.ValueForSql(clasif15) +
               "," + db.ValueForSql(clasif16) +
               "," + db.ValueForSql(clasif17) +
               "," + db.ValueForSql(clasif18) +
               "," + db.ValueForSql(clasif19) +
               "," + db.ValueForSql(clasif20) +
               "," + db.ValueForSql(clasif21) +
               "," + db.ValueForSql(clasif22) +
               "," + db.ValueForSql(clasif23) +
               "," + db.ValueForSql(clasif24) +
               "," + db.ValueForSql(clasif25) +
               "," + db.ValueForSql(clasif26) +
               "," + db.ValueForSql(clasif27) +
               "," + db.ValueForSql(clasif28) +
               "," + db.ValueForSql(clasif29) +
               "," + db.ValueForSql(clasif30) +
               "," + db.ValueForSql(jerarquia) +
               "," + db.ValueForSql(codigoDistribuidor) +
               "," + db.ValueForSql(Utils.StringTruncate(libre1, 1)) +
               "," + db.ValueForSql(Utils.StringTruncateLeft(libre1, 2)) +
               "," + db.ValueForSql(Utils.StringTruncate(libre2, 1)) +
               "," + db.ValueForSql(Utils.StringTruncateLeft(libre2, 2)) +
               "," + db.ValueForSql(libre3) +
               "," + db.ValueForSql(libre4) +
               "," + db.ValueForSqlAsNumeric(coordX) +
               "," + db.ValueForSqlAsNumeric(coordY) +
               "," + db.ValueForSql(EsSolicitante) +
               "," + db.ValueForSql(EsPdV) +
               "," + db.ValueForSql(Solicitante) +
               "," + db.ValueForSql(AreaNielsen) +
               "," + db.ValueForSql(CanalNielsen) +
               "," + db.ValueForSql(MercadoNielsen1) +
               "," + db.ValueForSql(MercadoNielsen2) +
               "," + db.ValueForSql(MercadoNielsen3) +
               "," + db.ValueForSql(MercadoNielsen4) +
               "," + db.ValueForSql(MercadoNielsen5) +
               "," + db.ValueForSqlAsNumeric(PotencialCompra1) +
               "," + db.ValueForSqlAsNumeric(PotencialCompra2) +
               "," + db.ValueForSqlAsNumeric(PotencialCompra3) +
               "," + db.ValueForSqlAsNumeric(PotencialCompra4) +
               "," + db.ValueForSqlAsNumeric(PotencialCompra5) +
               "," + db.ValueForSql(indCreadoAutomatico) +
               ")";          
          db.ExecuteSql(sql, agent, sipType);
      }
  }

  public class ClienteCFAgentes
  {
      public string codigoCF = "";
      public string clasificacion1 = "";
      public string clasificacion2 = "";
      public string clasificacion3 = "";
      public string clasificacion4 = "";
      public string clasificacion5 = "";
      public string clasificacion6 = "";
      public string clasificacion7 = "";
      public string descclasificacion1 = "";
      public string descclasificacion2 = "";
      public string descclasificacion3 = "";
      public string descclasificacion4 = "";
      public string descclasificacion5 = "";
      public string descclasificacion6 = "";
      public string descclasificacion7 = "";
      public string coordX = "";
      public string coordY = "";

      /// <summary>
      /// Obtener datos de cliente del fabricante
      /// </summary>
      /// <param name="db">base de datos</param>
      /// <param name="fabricante">fabricante</param>
      /// <param name="AlbaranesCFTipoAlbaran">tipo</param>
      /// <returns>tipo de albarán</returns>
      public void ObtenerDatosClienteFabricante(Database db, Log2 log, string sipTypeName, string distribuidor, string cliente, string fabricante, string codigoDist)
      {
          DbDataReader cursor = null;
          string sql = null;

          codigoCF = "";
          clasificacion1 = "";
          clasificacion2 = "";
          clasificacion3 = "";
          clasificacion4 = "";
          clasificacion5 = "";
          clasificacion6 = "";
          clasificacion7 = "";
          descclasificacion1 = "";
          descclasificacion2 = "";
          descclasificacion3 = "";
          descclasificacion4 = "";
          descclasificacion5 = "";
          descclasificacion6 = "";
          descclasificacion7 = "";
          coordX = "";
          coordY = "";

          try
          {
              sql = "select CodigoCF, Clasificacion1, Clasificacion2, Clasificacion3, Clasificacion4 " +
                          " , Clasificacion5, Clasificacion6, Clasificacion7, CoordX, CoordY " +
                          " from CFAgentes " +
                          " where IdcAgente = " + fabricante + " " +
                          " and CodigoCF = '" + codigoDist.PadLeft(10, '0') + cliente.PadLeft(10, ' ') + "'";
              cursor = db.GetDataReader(sql);
              if (cursor.Read())
              {
                  codigoCF = db.GetFieldValue(cursor, 0);
                  clasificacion1 = db.GetFieldValue(cursor, 1);
                  clasificacion2 = db.GetFieldValue(cursor, 2);
                  clasificacion3 = db.GetFieldValue(cursor, 3);
                  clasificacion4 = db.GetFieldValue(cursor, 4);
                  clasificacion5 = db.GetFieldValue(cursor, 5);
                  clasificacion6 = db.GetFieldValue(cursor, 6);
                  clasificacion7 = db.GetFieldValue(cursor, 7);
                  coordX = db.GetFieldValue(cursor, 8);
                  coordY = db.GetFieldValue(cursor, 9);
              }
              else
              {
                  cursor.Close();
                  cursor = null;

                  sql = "select CodigoCF, Clasificacion1, Clasificacion2, Clasificacion3, Clasificacion4 " +
                              " , Clasificacion5, Clasificacion6, Clasificacion7, CoordX, CoordY " +
                              " from CFAgentes " +
                              " where IdcAgente = " + fabricante + " " +
                              " and CodigoCF = '" + codigoDist.PadLeft(10, '0') + cliente.Trim() + "'";
                  cursor = db.GetDataReader(sql);
                  if (cursor.Read())
                  {
                      codigoCF = db.GetFieldValue(cursor, 0);
                      clasificacion1 = db.GetFieldValue(cursor, 1);
                      clasificacion2 = db.GetFieldValue(cursor, 2);
                      clasificacion3 = db.GetFieldValue(cursor, 3);
                      clasificacion4 = db.GetFieldValue(cursor, 4);
                      clasificacion5 = db.GetFieldValue(cursor, 5);
                      clasificacion6 = db.GetFieldValue(cursor, 6);
                      clasificacion7 = db.GetFieldValue(cursor, 7);
                      coordX = db.GetFieldValue(cursor, 8);
                      coordY = db.GetFieldValue(cursor, 9);
                  }
                  else
                  {
                      cursor.Close();
                      cursor = null;

                      sql = "select CodigoCliFab from CFCodificacionAgentes " +
                                   " where IdcAgente = " + distribuidor + " " +
                                   " and IdcFabricante=" + fabricante + " " +
                                   " and CodigoCF = '" + cliente + "'";
                      cursor = db.GetDataReader(sql);
                      if (cursor.Read())
                      {
                          codigoCF = db.GetFieldValue(cursor, 0);

                          cursor.Close();
                          cursor = null;

                          sql = "select Clasificacion1, Clasificacion2, Clasificacion3, Clasificacion4 " +
                                  " , Clasificacion5, Clasificacion6, Clasificacion7, CoordX, CoordY " +
                                  " from CFAgentes " +
                                  " where IdcAgente = " + fabricante + " " +
                                  " and CodigoCF = '" + codigoCF + "'";
                          cursor = db.GetDataReader(sql);
                          if (cursor.Read())
                          {
                              clasificacion1 = db.GetFieldValue(cursor, 0);
                              clasificacion2 = db.GetFieldValue(cursor, 1);
                              clasificacion3 = db.GetFieldValue(cursor, 2);
                              clasificacion4 = db.GetFieldValue(cursor, 3);
                              clasificacion5 = db.GetFieldValue(cursor, 4);
                              clasificacion6 = db.GetFieldValue(cursor, 5);
                              clasificacion7 = db.GetFieldValue(cursor, 6);
                              coordX = db.GetFieldValue(cursor, 7);
                              coordY = db.GetFieldValue(cursor, 8);
                          }
                      }
                      else
                      {
                          cursor.Close();
                          cursor = null;

                          sql = "select CodigoCF, Clasificacion1, Clasificacion2, Clasificacion3, Clasificacion4 " +
                                  " , Clasificacion5, Clasificacion6, Clasificacion7, CoordX, CoordY " +
                                  " from CFAgentes " +
                                  " where IdcAgente = " + fabricante + " " +
                                  " and CodigoCF = '" + cliente + "'";
                          cursor = db.GetDataReader(sql);
                          if (cursor.Read())
                          {
                              codigoCF = db.GetFieldValue(cursor, 0);
                              clasificacion1 = db.GetFieldValue(cursor, 1);
                              clasificacion2 = db.GetFieldValue(cursor, 2);
                              clasificacion3 = db.GetFieldValue(cursor, 3);
                              clasificacion4 = db.GetFieldValue(cursor, 4);
                              clasificacion5 = db.GetFieldValue(cursor, 5);
                              clasificacion6 = db.GetFieldValue(cursor, 6);
                              clasificacion7 = db.GetFieldValue(cursor, 7);
                              coordX = db.GetFieldValue(cursor, 8);
                              coordY = db.GetFieldValue(cursor, 9);
                          }
                      }
                  }
              }
              cursor.Close();
              cursor = null;

              descclasificacion1 = GetDescClasifCFAgentes(db, "AGENTE001", fabricante, clasificacion1);
              descclasificacion2 = GetDescClasifCFAgentes(db, "AGENTE002", fabricante, clasificacion2);
              descclasificacion3 = GetDescClasifCFAgentes(db, "AGENTE003", fabricante, clasificacion3);
              descclasificacion4 = GetDescClasifCFAgentes(db, "AGENTE004", fabricante, clasificacion4);
              descclasificacion5 = GetDescClasifCFAgentes(db, "AGENTE005", fabricante, clasificacion5);
              descclasificacion6 = GetDescClasifCFAgentes(db, "AGENTE006", fabricante, clasificacion6);
              descclasificacion7 = GetDescClasifCFAgentes(db, "AGENTE007", fabricante, clasificacion7);

          }
          catch (Exception e)
          {
              if (log != null) log.Error(fabricante, sipTypeName, e);
              throw e;
          }
          finally
          {
              if (cursor != null)
                  cursor.Close();
          }
          return;
      }

      public string GetDescClasifCFAgentes(Database db, string pEntidad, string pAgente, string pCodigo)
      {
          string result = "";
          DbDataReader cursor = null;
          string sql = null;

          if (!String.IsNullOrEmpty(pCodigo))
          {
              sql = "select Descripcion " +
                      " from ClasificacionAgente " +
                      " where EntidadClasificada ='" + pEntidad + "'" +
                      " And IdcAgente = " + pAgente + " " +
                      " and Codigo = '" + pCodigo + "'";
              cursor = db.GetDataReader(sql);
              if (cursor.Read())
              {
                  result = db.GetFieldValue(cursor, 0);
              }
              cursor.Close();
              cursor = null;
          }

          return result;
      }
  }
  
  public class BDCliente
  {
      /// <summary>Obtiene el código de cliente del fabricante para un cliente del distribuidor en CFCodificacionAgentes</summary>
      public string CodigoCliFabEnCFCodificacionAgentes(Database db, string pIdcAgente, string pCodigoCF, string pIdcFabricante)
      {
          if (Utils.IsBlankField(pIdcAgente)) return "";
          if (Utils.IsBlankField(pIdcFabricante)) return "";
          if (Utils.IsBlankField(pCodigoCF)) return "";
          DbDataReader reader = null;
          string resultado = "";
          string strSql = " SELECT CodigoCliFab" +
                          " FROM CFCodificacionAgentes" +
                          " WHERE IdcAgente      = " + pIdcAgente + " " +
                          "   AND IdcFabricante  = " + pIdcFabricante + " " +
                          "   AND CodigoCF  = '" + pCodigoCF + "' ";
          reader = db.GetDataReader(strSql);
          while (reader.Read())
          {
              resultado = db.GetFieldValue(reader, 0);
          }
          reader.Close();
          reader = null;
          return resultado;
      }

      public bool ConfirmarAutorizacionCFCodificacionAgentes(Database db, string pFABCliValidaCodificacionClientes, string pCodigoCliFab, string pIdcFabricante, string pIdcDistribuidor)
      {
          bool isOK = true;
          DbDataReader cursor = null;

          try
          {
              if (pFABCliValidaCodificacionClientes == "F" || pFABCliValidaCodificacionClientes == "E")
              {
                  isOK = false;

                  //Debemos verificar si ese cliente està autorizado para el distribuidor por el fabricante 
                  //pero haremos una búsqueda un poco avanzada: si el dato es numérico, además lo buscaremos
                  //convertido a número
                  string sql = "";
                  string sqlCodigoCF = "";

                  Double numCodigoCliFab = 0;
                  if (Double.TryParse(pCodigoCliFab, out numCodigoCliFab))
                  {
                      sqlCodigoCF += " And ";
                      sqlCodigoCF += "   ( ";
                      sqlCodigoCF += "     ( ";
                      sqlCodigoCF += "      CFAgentes.CodigoCF = '" + pCodigoCliFab + "' ";
                      sqlCodigoCF += "      OR (patindex('%[^0-9,.]%',ltrim(rtrim(CFAgentes.CodigoCF)))=0 and left(ltrim(CFAgentes.CodigoCF),1) not in ('.',',') and right(rtrim(CFAgentes.CodigoCF),1) not in ('.',',') ";
                      sqlCodigoCF += "          and cast(replace(CFAgentes.CodigoCF,',','.') as numeric(25,0)) = " + db.ValueForSqlAsNumeric(numCodigoCliFab.ToString()) + ") ";
                      sqlCodigoCF += "     ) ";
                      sqlCodigoCF += "     OR ";
                      sqlCodigoCF += "     ( ";
                      sqlCodigoCF += "      CFAgentes.CodigoCFAlt = '" + pCodigoCliFab + "' ";
                      sqlCodigoCF += "      OR (patindex('%[^0-9,.]%',ltrim(rtrim(CFAgentes.CodigoCFAlt)))=0 and left(ltrim(CFAgentes.CodigoCFAlt),1) not in ('.',',') and right(rtrim(CFAgentes.CodigoCFAlt),1) not in ('.',',') "; 
                      sqlCodigoCF += "          and cast(replace(CFAgentes.CodigoCFAlt,',','.') as numeric(25,0)) = " + db.ValueForSqlAsNumeric(numCodigoCliFab.ToString()) + ") ";
                      sqlCodigoCF += "     ) ";
                      sqlCodigoCF += "   ) ";
                  }
                  else
                  {
                      sqlCodigoCF += " And ";
                      sqlCodigoCF += "   ( ";
                      sqlCodigoCF += "     CFAgentes.CodigoCF = '" + pCodigoCliFab + "' ";
                      sqlCodigoCF += "     OR ";
                      sqlCodigoCF += "     CFAgentes.CodigoCFAlt = '" + pCodigoCliFab + "' ";
                      sqlCodigoCF += "   ) ";
                  }

                  if (pFABCliValidaCodificacionClientes == "F")
                  {
                      //Necesitamos obtener el código de distribuidor según el fabricante
                      string codigoDistFab = "";
                      sql = "Select Codigo From ClasifInterAgentes Where IdcAgenteOrigen = " + pIdcFabricante + " And IdcAgenteDestino = " + pIdcDistribuidor;
                      cursor = db.GetDataReader(sql);
                      if (cursor.Read())
                      {
                          codigoDistFab = db.GetFieldValue(cursor, 0);
                      }
                      cursor.Close();
                      cursor = null;
                      if (!Utils.IsBlankField(codigoDistFab))
                      {
                          sql = "select 1 from CFAgentes " +
                                      " Where CFAgentes.IdcAgente = " + pIdcFabricante + " " +
                                      sqlCodigoCF + " " +
                                      " And CFAgentes.CodigoDistribuidor = '" + codigoDistFab + "'";
                      }
                  }

                  if (pFABCliValidaCodificacionClientes == "E")
                  {
                      sql = "select 1 from CFAgentes, CFDistribuidoresAgentes " +
                                  " Where CFAgentes.IdcAgente = " + pIdcFabricante + " " +
                                  sqlCodigoCF + " " +
                                  " And CFAgentes.IdcAgente = CFDistribuidoresAgentes.IdcAgente " +
                                  " And CFAgentes.CodigoCF = CFDistribuidoresAgentes.CodigoCF " +
                                  " And CFDistribuidoresAgentes.IdcDistribuidor = " + pIdcDistribuidor;
                  }

                  if (!Utils.IsBlankField(sql))
                  {
                      if (Utils.RecordExist(sql))
                      {
                          isOK = true;
                      }
                  }
              }
          }
          finally
          {
              if (cursor != null)
                  cursor.Close();
          }
          return isOK;
      }

      public string GetMascaraNumeroPedidoCliente(Database db, string pFabricante, string codigoClienteFab)
      {
          if (Utils.IsBlankField(pFabricante)) return "";
          if (Utils.IsBlankField(codigoClienteFab)) return "";
          DbDataReader reader = null;
          string resultado = "";
          string strSql = "Select Alb_MascaraNumPedClie " +
                          " FROM CFAgentes " +
                          " WHERE IdcAgente = " + pFabricante + 
                          " AND CodigoCF = '" + codigoClienteFab + "' ";
          reader = db.GetDataReader(strSql);
          while (reader.Read())
          {
              resultado = db.GetFieldValue(reader, 0);
          }
          reader.Close();
          reader = null;
          return resultado;
      }

      public int GetLongitudNumeroPedidoCliente(Database db, string pFabricante, string codigoClienteFab)
      {
          if (Utils.IsBlankField(pFabricante)) return 0;
          if (Utils.IsBlankField(codigoClienteFab)) return 0;
          DbDataReader reader = null;
          int resultado = 0;
          string strSql = "Select Alb_LongitudNumPedClie " +
                          " FROM CFAgentes " +
                          " WHERE IdcAgente = " + pFabricante +
                          " AND CodigoCF = '" + codigoClienteFab + "' ";
          reader = db.GetDataReader(strSql);
          while (reader.Read())
          {
              resultado = Utils.StringToInt(db.GetFieldValue(reader, 0));
          }
          reader.Close();
          reader = null;
          return resultado;
      }

      /// <summary>Obtiene el valor de un campo concreto de un producto</summary>
      public string ValorCampoCFAgentes(Database db, string pNombreCampo, string pIdcAgente, string pCodigoCF)
      {
          if (Utils.IsBlankField(pIdcAgente)) return "";
          if (Utils.IsBlankField(pCodigoCF)) return "";
          DbDataReader reader = null;
          string resultado = "";
          string strSql = " SELECT " + pNombreCampo +
                          " FROM CFAgentes" +
                          " WHERE IdcAgente = " + pIdcAgente + " " +
                          " AND CodigoCF = '" + pCodigoCF + "' ";
          reader = db.GetDataReader(strSql);
          while (reader.Read())
          {
              resultado = db.GetFieldValue(reader, 0);
          }
          reader.Close();
          reader = null;
          return resultado;
      }

  }
  
}
