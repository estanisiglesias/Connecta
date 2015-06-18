using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data.OleDb;
using System.Collections;

namespace ConnectaLib
{
  /// <summary>
  /// Clase con operaciones comunes sobre productos
  /// </summary>
  public class Producto
  {
    private string codigoProducto = "";
    private string descripcionProducto = "";
    private string umBase = "";
    private string codigoProdFab = "";
    private string jerarquiaProducto = "";
    private string estadoProducto = "";
    private string fechaBloqueoProducto = "";
    private string fechaBajaProducto = "";
    private bool flagCalculaCantidades = true;
    private bool flagFijaUMGestion = false;
    private string flagEsKit = "";
    private string idcFabricante = "";

    //Getter
    public string CodigoProducto { get { return codigoProducto; } set { codigoProducto = value; } }
    public string DescripcionProducto { get { return descripcionProducto; } set { descripcionProducto = value; } }
    public string UMBase { get { return umBase; } }
    public string CodigoProdFab { get { return codigoProdFab; } }
    public string JerarquiaProducto { get { return jerarquiaProducto; } }
    public string EstadoProducto { get { return estadoProducto; } }
    public string FechaBloqueoProducto { get { return fechaBloqueoProducto; } }
    public string FechaBajaProducto { get { return fechaBajaProducto; } }
    public bool FlagCalculaCantiades { get { return flagCalculaCantidades; } }
    public bool FlagFijaUMGestion{ get { return flagFijaUMGestion; } }
    public string FlagEsKit { get { return flagEsKit; } }
    public string IdcFabricante { get { return idcFabricante; } }

    public Hashtable htProductos = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
    public Hashtable htProductosReutilizados = new Hashtable(StringComparer.InvariantCultureIgnoreCase);

    public struct sProductos
    {
        public string idcProducto;
        public string codigo;
        public string descripcion;
        public string idcFabricante;
        public string indCalculaCantidades;
        public string indFijaUMGestion;
        public string indEsKit;
        public string UMc;
        public string UMEstadistica;
        public string UMEstadistica2;
        public string UMEstadistica3;
    }

    public struct sProductosReutilizados
    {       
        public string codigo;
        public string codigoNuevo;       
        public DateTime FechaInicial;
        public DateTime FechaFinal;        
    }
    public List<sProductosReutilizados> listsProductosReutilizados;

    /// <summary>
    /// Cargar información necesaria de los productos actuales del agente
    /// </summary>
    /// <param name="db">db</param>
    public void CargarProductosActuales(Database db, string agent)
    {
        //Productos
        htProductos.Clear();        
        DbDataReader cursor = null;
        string sql = "Select pa.IdcProducto, pa.Codigo, pa.Descripcion, p.IdcFabricante, p.IndCalculaCantidades, " +
                     "       p.IndFijaUMGestion, p.IndEsKit, p.UMc, p.UMEstadistica, p.UMEstadistica2, p.UMEstadistica3 " +
                     "  from ProductosAgentes pa " +
                     "  left join productos p on pa.idcproducto=p.idcproducto" +        
                     " Where pa.IdcAgente = " + agent + " Order by pa.Codigo, pa.Status"; 
        cursor = db.GetDataReader(sql);
        while (cursor.Read())
        {
            sProductos p = new sProductos();
            p.idcProducto = db.GetFieldValue(cursor, 0);
            p.codigo = db.GetFieldValue(cursor, 1);
            p.descripcion = db.GetFieldValue(cursor, 2);
            p.idcFabricante = db.GetFieldValue(cursor, 3);
            p.indCalculaCantidades = db.GetFieldValue(cursor, 4);
            p.indFijaUMGestion = db.GetFieldValue(cursor, 5);
            p.indEsKit = db.GetFieldValue(cursor, 6);
            p.UMc = db.GetFieldValue(cursor, 7);
            p.UMEstadistica = db.GetFieldValue(cursor, 8);
            p.UMEstadistica2 = db.GetFieldValue(cursor, 9);
            p.UMEstadistica3 = db.GetFieldValue(cursor, 10);            
            if (!htProductos.ContainsKey(p.codigo))
                htProductos.Add(p.codigo, p);            
        }
        cursor.Close();
        cursor = null;        
    }

    /// <summary>
    /// Cargar información necesaria de los productos con reutilización de códigos del agente
    /// </summary>
    /// <param name="db">db</param>
    public void CargarProductosReutilizados(Database db, string agent)
    {
        //Productos reutilizados
        htProductosReutilizados.Clear();
        if (listsProductosReutilizados != null)
            listsProductosReutilizados.Clear();
        DbDataReader cursor = null;
        string sql = "select CodigoProducto, " +
              "       case when ROW_NUMBER() OVER(PARTITION BY CodigoProducto ORDER BY FechaFinal ASC) = 1 " +
			  " 		 then CodigoProducto " +
			  "			 else CodigoProducto + 'v' + cast((ROW_NUMBER() OVER(PARTITION BY CodigoProducto ORDER BY FechaFinal ASC)) as varchar(100)) end AS versionProducto, " +              
              "       FechaInicial, FechaFinal " +
              "  from ProductosAgentesHistorico  " +
              " where IdcAgente = " + agent + " and IndReutilizacionCodigos='S'" +
              " order by CodigoProducto";
        cursor = db.GetDataReader(sql);
        while (cursor.Read())
        {
            sProductosReutilizados pr = new sProductosReutilizados();
            pr.codigo = db.GetFieldValue(cursor, 0);
            pr.codigoNuevo = db.GetFieldValue(cursor, 1);
            if (db.GetDate(db.GetFieldValue(cursor, 2)) != null)
                pr.FechaInicial = (DateTime)db.GetDate(db.GetFieldValue(cursor, 2));
            if (db.GetDate(db.GetFieldValue(cursor, 3)) != null)
                pr.FechaFinal = (DateTime)db.GetDate(db.GetFieldValue(cursor, 3));
            if (!htProductosReutilizados.ContainsKey(pr.codigo))
            {
                listsProductosReutilizados = new List<sProductosReutilizados>();
                listsProductosReutilizados.Add(pr);
                htProductosReutilizados.Add(pr.codigo, listsProductosReutilizados);
            }
            else
            {
                listsProductosReutilizados = (List<sProductosReutilizados>)htProductosReutilizados[pr.codigo];
                listsProductosReutilizados.Add(pr);
                htProductosReutilizados[pr.codigo] = listsProductosReutilizados;
            }
        }
        cursor.Close();
        cursor = null;
    }

    /// <summary>
    /// Comprobar código de producto
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="codigoDistribuidor">código de distribuidor</param>
    /// <param name="codProd">código de producto</param> 
    /// <returns>true si es correcto</returns>
    public bool ComprobarCodigoProducto(Database db, string idcAgente, string codProd)
    {
        return ComprobarCodigoProducto(db, idcAgente, codProd, false);
    }

    /// <summary>
    /// Comprobar código de producto
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="codigoDistribuidor">código de distribuidor</param>
    /// <param name="codProd">código de producto</param> 
    /// <param name="buscarNumerico">busca el código de producto en formato numérico</param> 
    /// <returns>true si es correcto</returns>
    public bool ComprobarCodigoProducto(Database db, string idcAgente, string codProd, bool buscarNumerico)
    {
        bool isOK = false;
        DbDataReader reader = null;
        codigoProducto = "";        
        descripcionProducto = "";
        try
        {
            string sql = "Select IdcProducto, Descripcion from ProductosAgentes Where IdcAgente = " + idcAgente;
            if (!buscarNumerico)
                sql += " And Codigo = '" + codProd + "' ";                                     
            else
            {
                double numCodProd;
                if (Double.TryParse(codProd, out numCodProd))
                {
                    sql += " AND (";
                    sql += " Codigo = '" + codProd + "' ";
                    sql += " OR (not Codigo is null and ltrim(rtrim(Codigo))<>'' and patindex('%[^0-9,.]%',ltrim(rtrim(Codigo)))=0 and left(ltrim(Codigo),1) not in ('.',',') and right(rtrim(Codigo),1) not in ('.',',') ";
                    sql += "     and cast(replace(Codigo,',','.') as numeric(18,0)) = " + db.ValueForSqlAsNumeric(numCodProd.ToString()) + ") ";
                    sql += " ) ";
                }
                else sql += " And Codigo = '" + codProd + "' ";                                     
            }
            sql += " Order by status "; //añadido por SVO para que si tenemos dos productos con un mismo codigo se queda con el que tiene status AC, y los BD queden detràs
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                codigoProducto = db.GetFieldValue(reader, 0);
                descripcionProducto = db.GetFieldValue(reader, 1);
                isOK = true;
            }
        }
        catch
        {
            if (buscarNumerico)
                return ComprobarCodigoProducto(db, idcAgente, codProd, false);
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return isOK;
    }

    /// <summary>
    /// Comprobar código de producto
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="codigoDistribuidor">código de distribuidor</param>
    /// <param name="codProd">código de producto</param> 
    /// <param name="buscarNumerico">busca el código de producto en formato numérico</param> 
    /// <returns>true si es correcto</returns>
    public bool ComprobarCodigoProductoNew(Database db, string idcAgente, string codProd, bool buscarNumerico)
    {
        bool isOK = false;
        DbDataReader reader = null;
        codigoProducto = "";
        descripcionProducto = "";
        try
        {
            string sql = "Select IdcProducto, Descripcion from ProductosAgentes Where IdcAgente = " + idcAgente;
            if (!buscarNumerico)
                sql += " And Codigo = '" + codProd + "' ";
            else
            {
                double numCodProd;
                if (Double.TryParse(codProd, out numCodProd))
                {
                    sql += " And patindex('%[^0-9,.]%',ltrim(rtrim(coalesce(Codigo,''))))=0 and left(ltrim(Codigo),1) not in ('.',',') and right(rtrim(Codigo),1) not in ('.',',') ";
                    sql += " And cast(replace(coalesce(Codigo,''),',','.') as numeric(18,0)) = " + db.ValueForSqlAsNumeric(numCodProd.ToString());
                }
                else sql += " And Codigo = '" + codProd + "' ";
            }
            sql += " Order by status "; //añadido por SVO para que si tenemos dos productos con un mismo codigo se queda con el que tiene status AC, y los BD queden detràs
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                codigoProducto = db.GetFieldValue(reader, 0);
                descripcionProducto = db.GetFieldValue(reader, 1);
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
    /// Comprobar código de subproducto
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="codigoDistribuidor">código de distribuidor</param>
    /// <param name="codProd">código de producto</param> 
    /// <returns>true si es correcto</returns>
    public bool ComprobarCodigoSubProducto(Database db, string idcAgente, string codProd)
    {
        bool isOK = false;
        DbDataReader reader = null;
        codigoProducto = "";
        descripcionProducto = "";
        try
        {
            string sql = "Select IdcProducto, Descripcion from SubProductosAgentes Where IdcAgente = " + idcAgente +
                     " And Codigo = '" + codProd + "'";
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                codigoProducto = db.GetFieldValue(reader, 0);
                descripcionProducto = db.GetFieldValue(reader, 1);
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
    /// Comprobar código de producto agrupado
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="codigoDistribuidor">código de distribuidor</param>
    /// <param name="codProd">código de producto</param> 
    /// <param name="buscarNumerico">busca el código de producto en formato numérico</param> 
    /// <returns>true si es correcto</returns>
    public bool ComprobarCodigoProductoAgrup(Database db, string idcAgente, string codProd)
    {
        bool isOK = false;
        DbDataReader reader = null;
        codigoProducto = "";
        descripcionProducto = "";
        try
        {
            string sql = "Select IdcProducto, Descripcion from ProductosAgentes Where IdcAgente = " + idcAgente;
            sql += " And CodigoAgrup = '" + codProd + "' ";
            sql += " Order by status "; //añadido por SVO para que si tenemos dos productos con un mismo codigo se queda con el que tiene status AC, y los BD queden detràs
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                codigoProducto = db.GetFieldValue(reader, 0);
                descripcionProducto = db.GetFieldValue(reader, 1);
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
    /// Obtener identificador ConnectA del fabricante de un producto
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="idcProducto">Identificador ConnectA del producto</param>
    public bool ObtenerFabricanteProducto(Database db, string pIdcProducto)
    {
        bool isOK = false;
        DbDataReader reader = null;
        idcFabricante = "";
        
        if (Utils.IsBlankField(pIdcProducto)) return isOK;

        try
        {
            string sql = "Select idcFabricante" +
                        " From Productos" +
                        " Where IdcProducto = " + pIdcProducto;
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                idcFabricante = db.GetFieldValue(reader, 0);
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
    /// Obtener código de producto y jerarquia según el fabricante
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="codigoDistribuidor">código de distribuidor</param>
    /// <param name="codProd">código de producto</param> 
    /// <returns>true si es correcto</returns>
    public void ObtenerCodigoProductoFabricante(Database db, string idcAgente, string codProd)
    {
        DbDataReader reader = null;
        codigoProdFab = "";
        jerarquiaProducto = "";
        estadoProducto = "";
        fechaBloqueoProducto = "";
        fechaBajaProducto = "";
        try
        {
            string sql = "Select Codigo,Jerarquia,Status,FechaBloqueo,FechaBaja "+ 
                     " From ProductosAgentes Where IdcAgente = " + idcAgente +
                     " And IdcProducto = '" + codProd + "'";
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                codigoProdFab = db.GetFieldValue(reader, 0);
                jerarquiaProducto = db.GetFieldValue(reader, 1);
                estadoProducto = db.GetFieldValue(reader, 2);
                fechaBloqueoProducto = db.GetFieldValue(reader, 3);
                fechaBajaProducto = db.GetFieldValue(reader, 4);
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
    }

    /// <summary>
    /// Comprobar código de producto en productos no existentes del agente
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="codigoDistribuidor">código de distribuidor</param>
    /// <param name="codProd">código de producto</param> 
    /// <returns>true si es correcto</returns>
    public bool ComprobarCodigoProductoEnNoExistentes(Database db, string idcAgente, string codProd)
    {
        bool isOK = false;
        DbDataReader reader = null;
        try
        {
            string sql = "Select CodigoProducto from ProductosAgentesNoExistentes Where IdcAgente = " + idcAgente +
                     " And CodigoProducto = '" + codProd + "'";
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
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
    /// Marcar un producto como no obsoleto en ProductosAgentesNoExistentes
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="codigoDistribuidor">código de distribuidor</param>
    /// <param name="codProd">código de producto</param> 
    /// <returns>true si es correcto</returns>
    public int MarcarProductoComoNoObsoletoEnNoExistentes(Database db, string pSipTypeName, string idcAgente, string codProd)
    {
        string sqlWhere = " WHERE IdcAgente = " + idcAgente + " And CodigoProducto = '" + codProd + "'";
        string sql = "UPDATE ProductosAgentesNoExistentes SET IndObsoleto='N' " + sqlWhere + " And (IndObsoletoFijo <> 'S' OR IndObsoletoFijo Is Null)";
        int NRegs = db.ExecuteSql(sql, idcAgente, pSipTypeName);
        if (NRegs > 0) return 0; //significa que el producto existe en ProductosAgentesNoExistentes y ha sido marcado como NO obsoleto

        sql = "SELECT 1 FROM ProductosAgentesNoExistentes " + sqlWhere;
        if (Utils.RecordExist(sql)) return 1; //significa que el producto existe en ProductosAgentesNoExistentes pero está marcado como obsoleto fijo

        return 2; //significa que el producto NO existe en ProductosAgentesNoExistentes
    }

    /// <summary>
    /// Recuperar el fabricante de un producto de la tabla ProductosAgentesNoExistentes
    /// </summary>
    /// <param name="db"></param>
    /// <param name="IdcCliente"></param>
    /// <param name="codProd"></param>
    /// <returns></returns>
    public string ObtenerFabricanteDeProductosAgentesNoExistentes(Database db, string idcAgente, string codProd)
    {
        DbDataReader reader = null;
        string result = "";
        try
        {
            string sql = "Select IdcAgenteDestino From ClasifInterAgentes " +
                        " Inner Join ProductosAgentesNoExistentes " +
                        "     On ClasifInterAgentes.IdcAgenteOrigen = ProductosAgentesNoExistentes.IdcAgente " +
                        "     And ClasifInterAgentes.Codigo = ProductosAgentesNoExistentes.Fabricante " +
                        " Where IdcAgenteOrigen=" + idcAgente +
                        " And ProductosAgentesNoExistentes.CodigoProducto = '" + codProd + "'";
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                result = db.GetFieldValue(reader, 0);
            }
            if (Utils.IsBlankField(result))
            {
                sql = "Select IdcFabricante From Productos " +
                            " Inner Join ProductosAgentesNoExistentes " +
                            "     On Productos.EAN13 = ProductosAgentesNoExistentes.EAN13 " +
                            " Where ProductosAgentesNoExistentes.IdcAgente=" + idcAgente +
                            " And ProductosAgentesNoExistentes.CodigoProducto = '" + codProd + "'";
                reader = db.GetDataReader(sql);
                if (reader.Read())
                {
                    result = db.GetFieldValue(reader, 0);
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
        return result;
    }

    /// <summary>
    /// Recuperar la descripción de un producto de la tabla ProductosAgentesNoExistentes
    /// </summary>
    /// <param name="db"></param>
    /// <param name="IdcCliente"></param>
    /// <param name="codProd"></param>
    /// <returns></returns>
    public string ObtenerDescripcionDeProductosAgentesNoExistentes(Database db, string idcAgente, string codProd)
    {
        DbDataReader reader = null;
        string result = "";
        try
        {
            string sql = "Select Descripcion From ProductosAgentesNoExistentes " +
                        " Where IdcAgente=" + idcAgente +
                        " And CodigoProducto = '" + codProd + "'";
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                result = db.GetFieldValue(reader, 0);
            }
            reader.Close();
            reader = null;
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return result;
    }

    /// <summary>
    /// Obtiene el falg que indica si el producto requiere calcular cantidades o no
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="codProd">código de producto</param> 
    /// <returns>true si es correcto</returns>
    public bool ObtenerFlagsProducto(Database db, string codProd)
    {
        bool isOK = false;
        DbDataReader reader = null;
        flagCalculaCantidades = true;
        flagFijaUMGestion = false;
        idcFabricante = "";
        flagEsKit = "";
        umBase = "";
        try
        {
            string sql = "Select IdcFabricante, IndCalculaCantidades, IndFijaUMGestion, IndEsKit, UMc from Productos Where IdcProducto = " + codProd;
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                idcFabricante = db.GetFieldValue(reader, 0);
                flagCalculaCantidades = (db.GetFieldValue(reader, 1) != "N");
                flagFijaUMGestion = (db.GetFieldValue(reader, 2) == "S");
                flagEsKit = db.GetFieldValue(reader, 3);
                umBase = db.GetFieldValue(reader, 4);
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
    /// Comprobar que el producto exista en el surtido del cliente
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="linea">registro de línea de albarán</param>
    /// <returns>true si es correcto</returns>
    public bool ProductoDentroSurtido(Database db, string pIdcFabricante, string pIdcAgente, string jerarquiaCliente, string jerarquiaProducto, string pCodigoProdFab, string fechaControl)
    {
        bool isOK = false;
        string sql = "";

        sql = "select 1 From Surtidos as s " +
                   "Where s.IdcFabricante=" + pIdcFabricante +
                   " and (s.IdcDistribuidor=0 or s.IdcDistribuidor=" + pIdcAgente + ")" +
                   " and (s.EjeCliente In ('*'" + jerarquiaCliente + "))" +
                   " and (s.EjeProducto='*' ";
        if (!String.IsNullOrEmpty(jerarquiaProducto))
        {
            sql += " or s.EjeProducto=Left(" + db.ValueForSql(jerarquiaProducto) + ",Len(s.EjeProducto)) ";
        }
        sql += " or s.EjeProducto=" + db.ValueForSql(pCodigoProdFab) + ")" +
                  " and (" + db.DateForSql(fechaControl) + " Between s.FechaIniVigencia and s.FechaFinVigencia)";

        if (Utils.RecordExist(sql))
        {
            isOK = true;
        }
        return isOK;
    }

    /// <summary>Alineación de productos</summaryA></summary>
    public static string AlinearProducto(string idcAgenteDistr, string idcAgenteFab, string codigoProductoDistr, string codigoProductoFab)
    {
        bool Alineado = false;
        string resultado = null;
        string IdcProducto = "";

        Database db = Globals.GetInstance().GetDatabase();
        BDProducto ObjBD = new BDProducto();

        // 1. Confirmamos que por motivos esotéricos el producto no esté ya alineado
        string IdcProductoYaAlineado = ObjBD.IdcProductoEnProductosAgentes(db, idcAgenteDistr, codigoProductoDistr);

        if (!String.IsNullOrEmpty(IdcProductoYaAlineado))
        {
            Alineado = true;
            resultado = "Producto " + codigoProductoDistr + ": Ya estaba alineado.";
        }
        else
        {
            // 2. Obtener el IdcProducto Connect@ del producto, a partir de la codificación de fabricante
            IdcProducto = ObjBD.IdcProductoEnProductosAgentes(db, idcAgenteFab, codigoProductoFab);

            if (String.IsNullOrEmpty(IdcProducto))            
                resultado = "Producto " + codigoProductoDistr + ": El código de fabricante " + codigoProductoFab + " no es válido, no se ha podido alinear.";            
            else
            {
                // 3. Insertar en ProductosAgentes con el IdcProducto que hemos encontrado
                Alineado = ObjBD.AlinearProducto(db, idcAgenteDistr, idcAgenteFab, codigoProductoDistr, IdcProducto);
                if (!Alineado)                
                    resultado = "Producto " + codigoProductoDistr + ": No se ha podido alinear.";                
            }
        }
        if (Alineado)
        {
            // Da igual si ya lo estaba o se ha hecho ahora, eliminamos el registor de ProductosNoExistentes
            ObjBD.BorrarNoExistentes(db, idcAgenteDistr, codigoProductoDistr);    
        
            //4. Reprocesamos ventas asociadas a éste producto
            sipUpdProcesarFacturasDistribuidor mySip = new sipUpdProcesarFacturasDistribuidor(idcAgenteDistr);
            mySip.Process(idcAgenteDistr, "", "", "", codigoProductoDistr, "productosfacturasconerrores", "");
        }        
        return resultado;
    }
  }

  public class BDProducto
  {            
      /// <summary>Inserta en ProductosAgentes el producto actual, asociado al código Connect@ IdcProducto</summary>
      public bool AlinearProducto(Database db, string idcAgenteDistr, string idcAgenteFab, string codigoProductoDistr, string idcProducto)
      {          
          // Hacer la conversión de unidades de medida
          string UnidadMedidaDistribuidor = ValorCampoProductosAgentesNoExistentes(db, "UnidadMedida", idcAgenteDistr, codigoProductoDistr);
          UnidadMedida UM = new UnidadMedida();
          string UnidadMedidaConnecta = UM.ObtenerUMcDistribuidorProducto(db, idcAgenteDistr, idcProducto, codigoProductoDistr, UnidadMedidaDistribuidor);

          string sUsuario = Constants.PREFIJO_USUARIO_AGENTE + idcAgenteDistr;

          // Grabarlo en la tabla ProductosAgentes con el IdcProducto que hemos encontrado
          String strSql =
              " INSERT INTO [ProductosAgentes] " +
              " ( [IdcAgente],[IdcProducto],[Codigo],[Descripcion],[Status],[UMc]" +
              "  ,[Clasificacion1] ,[Clasificacion2] ,[Clasificacion3] ,[Clasificacion4] ,[Clasificacion5]" +
              "  ,[Clasificacion6] ,[Clasificacion7] ,[Clasificacion8] ,[Clasificacion9],[Clasificacion10]" +
              "  ,[Clasificacion11],[Clasificacion12],[Clasificacion13],[Clasificacion14]" +
              "  ,[Jerarquia],[Origen],[FechaInsercion],[IdcFabricante], [UsuarioInsercion], [UsuarioModificacion])" +
              " SELECT IdcAgente, " + idcProducto + " , CodigoProducto,[Descripcion],[Status], '" + UnidadMedidaConnecta + "'" +
              "  ,[Clasificacion1] ,[Clasificacion2] ,[Clasificacion3] ,[Clasificacion4] ,[Clasificacion5]" +
              "  ,[Clasificacion6] ,[Clasificacion7] ,[Clasificacion8] ,[Clasificacion9],[Clasificacion10]" +
              "  ,[Clasificacion11],[Clasificacion12],[Clasificacion13],[Clasificacion14]" +
              "  ,[Jerarquia],NULL," + db.SysDate() + "," + idcAgenteFab + ", '" + sUsuario + "', '" + sUsuario + "'" +
              " FROM [ProductosAgentesNoExistentes]" +
              " WHERE IdcAgente = " + idcAgenteDistr +
              "   AND CodigoProducto = '" + codigoProductoDistr + "'";

          int numRows = db.ExecuteSql(strSql);
          return numRows > 0;
      }

      /// <summary>Borra de productosagentesnoexistentes</summary>
      public bool BorrarNoExistentes(Database db, string idcAgenteDistr, string codigoProductoDistr)
      {
          String strSql =
              " DELETE FROM [ProductosAgentesNoExistentes]" +
              " WHERE IdcAgente = " + idcAgenteDistr +
              "   AND CodigoProducto = '" + codigoProductoDistr + "'";
          int numRows = db.ExecuteSql(strSql);
          return numRows > 0;
      }

      /// <summary>Obtiene el valor de un campo concreto de un producto</summary>
      public string ValorCampoProductos(Database db, string pNombreCampo, string pIdcProducto)
      {
          if (Utils.IsBlankField(pIdcProducto)) return "";
          DbDataReader reader = null;
          string resultado = "";
          string strSql = " SELECT " + pNombreCampo +
                          " FROM Productos" +
                          " WHERE IdcProducto = " + pIdcProducto + " ";
          reader = db.GetDataReader(strSql);
          while (reader.Read())
          {
              resultado = db.GetFieldValue(reader, 0);
          }
          reader.Close();
          reader = null;
          return resultado;
      }

      /// <summary>Obtiene el valor de un campo concreto de un producto</summary>
      public string ValorCampoProductosAgentes(Database db, string pNombreCampo, string pIdcAgente, string pIdcProducto)
      {
          if (Utils.IsBlankField(pIdcAgente)) return "";
          if (Utils.IsBlankField(pIdcProducto)) return "";
          DbDataReader reader = null;
          string resultado = "";
          string strSql = " SELECT " + pNombreCampo +
                          " FROM ProductosAgentes" +
                          " WHERE IdcAgente = " + pIdcAgente + " " +
                          " AND IdcProducto = " + pIdcProducto + " ";
          reader = db.GetDataReader(strSql);
          while (reader.Read())
          {
              resultado = db.GetFieldValue(reader, 0);
          }
          reader.Close();
          reader = null;
          return resultado;
      }

      /// <summary>Obtiene el valor de un campo concreto de un producto</summary>
      public string ValorCampoProductosAgentesPorJerarquia(Database db, string pNombreCampo, string pIdcAgente, string pJerarquia, string pExcluir)
      {
          if (Utils.IsBlankField(pIdcAgente)) return "";
          if (Utils.IsBlankField(pJerarquia)) return "";
          DbDataReader reader = null;
          string resultado = "";
          string strSql = " SELECT " + pNombreCampo +
                          " FROM ProductosAgentes" +
                          " WHERE IdcAgente = " + pIdcAgente + " " +
                          " AND Jerarquia = '" + pJerarquia + "' ";
          if (!Utils.IsBlankField(pExcluir))
          {
              strSql += " AND not " + pNombreCampo + " IN (" + pExcluir + ") ";
          }
          reader = db.GetDataReader(strSql);
          if (reader.Read())
          {
              resultado = db.GetFieldValue(reader, 0);
          }
          reader.Close();
          reader = null;
          return resultado;
      }

      /// <summary>Obtiene el valor de un campo concreto de un producto</summary>
      public string ValorCampoProductosAgentesNoExistentes(Database db, string nombreCampo, string idcAgenteDistr, string codigoProductoDistr)
      {
          string resultado = "";
          DbDataReader reader = null;
          string strSql = " SELECT " + nombreCampo +
                          " FROM ProductosAgentesNoExistentes" +
                          " WHERE IdcAgente      = " + idcAgenteDistr +
                          "   AND CodigoProducto = '" + codigoProductoDistr + "'";
          reader = db.GetDataReader(strSql);
          if (reader.Read())
              resultado = db.GetFieldValue(reader, 0);
          reader.Close();
          reader = null;
          return resultado;
      }

      /// <summary>Obtiene el IdcProducto de un producto para un agente en ProductosAgentes</summary>
      public string IdcProductoEnProductosAgentes(Database db, string pIdcAgente, string pCodigoProducto)
      {
          if (Utils.IsBlankField(pIdcAgente)) return "";
          if (Utils.IsBlankField(pCodigoProducto)) return "";
          DbDataReader reader = null;
          string resultado = "";
          string strSql = " SELECT IdcProducto" +
                          " FROM ProductosAgentes" +
                          " WHERE IdcAgente   = " + pIdcAgente +
                          "   AND Codigo      ='" + pCodigoProducto + "'";
          reader = db.GetDataReader(strSql);
          if (reader.Read())
          {
              resultado = db.GetFieldValue(reader, 0);
          }
          reader.Close();
          reader = null;
          return resultado;
      }

    /// <summary>Obtiene el precio unitario de un producto</summary>
    public string PrecioUnitarioProducto(Database db, string pIdcFabricante, string pCodigo, string pUM, string pIdPrecio)
    {
        DbDataReader reader = null;
        string resultado = "";
        string strSql = " SELECT Precio " +
                          " FROM Precios " +
                          " WHERE IdcFabricante = " + pIdcFabricante +
                          " And EjeProducto = '" + pCodigo + "' " +
                          " And TipoPrecio = 'TAR' ";
        if (pIdPrecio.Contains("%"))
        {
            strSql += " And IdPrecio Like '" + pIdPrecio + "' ";
        }
        else
        {
            strSql += " And IdPrecio = '" + pIdPrecio + "' ";
        }
        strSql += " And (UMc = '" + pUM + "' Or UMc='' Or UMc=null)";
        reader = db.GetDataReader(strSql);
        if (reader.Read())
        {
            resultado = db.GetFieldValue(reader, 0);
        }
        reader.Close();
        reader = null;
        return resultado;
    }

      /// <summary>Obtiene la UM de un producto que tiene precio inferior a otro en un porcentaje parámetro</summary>
      public string UMProductoInferiorA(Database db, string pIdcFabricante, string pCodigo, string pPrecRef, string pPorcRef, string pIdPrecio)
      {
          DbDataReader reader = null;
          string resultado = "";
          string strSql = " SELECT DISTINCT UMc " +
                                 " FROM Precios " +
                                 " WHERE IdcFabricante = " + pIdcFabricante +
                                 " And EjeProducto = '" + pCodigo + "' " +
                                 " And TipoPrecio = 'TAR' ";
          if (pIdPrecio.Contains("%"))
          {
              strSql += " And IdPrecio Like '" + pIdPrecio + "' ";
          }
          else
          {
              strSql += " And IdPrecio = '" + pIdPrecio + "' ";
          }
          strSql += " And ABS(((Precio - " + db.ValueForSqlAsNumeric(pPrecRef) + ") *100)/Precio) < " + pPorcRef;
          reader = db.GetDataReader(strSql);
          if (reader.Read())
          {
              resultado = db.GetFieldValue(reader, 0);
          }
          reader.Close();
          reader = null;
          return resultado;
      }
  }

}
