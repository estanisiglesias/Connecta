using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Data.Common;
using System.Xml;
using System.Text;
using System.IO;

namespace ConnectaLib
{
  /// <summary>
  /// Actualización para procesar stocks con errores del distribuidor
  /// </summary>
  public class sipUpdProcesarFacturasDistribuidor : SipUpd, ISipUpdInterface
  {
    private const string SEPARADOR_FILTRO = ";";

    //Id de SIP
    public const string ID_SIP = "sipUpdProcesarFacturasDistribuidor";    

    /// <summary>
    /// Obtener id de sip
    /// </summary>
    /// <returns>id de sip</returns>
    public string GetId()
    {
      return ID_SIP;
    }

    /// <summary>
    /// Obtener nombre de Sip (básicamente a efectos de log)
    /// </summary>
    /// <returns>nombre de sip</returns>
    public string GetSipTypeName()
    {
      return "Distribuidor.ProcesarFacturas.Upd";
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public sipUpdProcesarFacturasDistribuidor(string agent) : base(agent)
    {
    }

    /// <summary>
    /// Lanzar proceso de salida
    /// </summary>
    /// <param name="args">argumentos</param>
    /// <returns>resultados del proceso</returns>
    public string Process(string[] args)
    { 
        //Obtener parámetros del proceso 
        string sipFromDate = "";
        string sipToDate = "";
        string sipFilter = null;
        string sipProductCode = "";
        string sipDataSource = "";
        string sipFabricante = "";
        string s = "";
        for (int i = 0; i < args.Length; i++)
        {
            s = args[i].ToLower();
            if (s.StartsWith("-sipfromdate="))
                sipFromDate = args[i].Substring("-sipfromdate=".Length);
            else if (s.StartsWith("-siptodate="))
                sipToDate = args[i].Substring("-siptodate=".Length);
            else if (s.StartsWith("-sipfilter="))
                sipFilter = args[i].Substring("-sipfilter=".Length);
            else if (s.StartsWith("-sipproductcode="))
                sipProductCode = args[i].Substring("-sipproductcode=".Length);
            else if (s.StartsWith("-sipprovider="))
                sipFabricante = args[i].Substring("-sipprovider=".Length);
            else if (s.StartsWith("-sipdatasource="))
                sipDataSource = args[i].Substring("-sipdatasource=".Length);
        }

        if (sipFromDate != null || sipToDate != null)
        {
            //Si la fecha tiene el tag especial @last2m se filtran los últimos dos meses
            if (sipFromDate.Trim().ToLower() == "@last2m" || sipToDate.Trim().ToLower() == "@last2m")
            {
                char pad = '0';
                sipToDate = DateTime.DaysInMonth(DateTime.Now.AddMonths(-1).Year, DateTime.Now.AddMonths(-1).Month).ToString().PadLeft(2, pad) +
                                "/" + DateTime.Now.AddMonths(-1).Month.ToString().PadLeft(2, pad) +
                                "/" + DateTime.Now.AddMonths(-1).Year.ToString();

                sipFromDate = "01/" + DateTime.Now.AddMonths(-2).Month.ToString().PadLeft(2, pad) +
                                "/" + DateTime.Now.AddMonths(-2).Year.ToString();
            }
        }

        if (!Utils.IsBlankField(sipDataSource))
        {
            if (!sipDataSource.ToLower().Equals("productosfacturas") && !sipDataSource.ToLower().Equals("productosfacturasconerrores")) sipDataSource = "productosfacturasconerrores";
        }

        return Process(agent, sipFromDate, sipToDate, sipFilter, sipProductCode, sipDataSource, sipFabricante);
    }

    /// <summary>
    /// Refrescar los  los valores de un clasificador para el fabricante.
    /// </summary>
    /// <param name="fabricante">fabricante</param>
    /// <param name="dealer">distribuidor</param>
    /// <param name="fechaDesde">desde fecha</param>
    /// <param name="fechaHasta">hasta fecha</param>
    /// <returns> </returns>
    public string Process(string distribuidor, string fechaDesde, string fechaHasta, string filtro, string codigoProducto, string dataSource, string fabricante)
    {
        Globals g = null;
        Log2 log = null;
        int numProcesados = 0;
        int numIntegrados = 0;
        int numRows = 0;
        string result = "";
        DbDataReader rs = null;
        string sql = "";
        StreamReader sr = null;
        StreamWriter sw = null;

        string fieldSeparator = "";
        string filename = "";
        string row;        
        //Administra - config                
        try
        {
            g = Globals.GetInstance();
            Database db = g.GetDatabase();

            log = g.GetLog2();
            log.Info(agent, GetSipTypeName(), "Proceso de actualización (procesar " + (dataSource.ToLower().Equals("productosfacturas") ? "facturas distribuidor" : "facturas distribuidor con errores") + ") iniciado. Parámetros: " +
                                                    "distribuidor=" + agent + " " +
                                                    "desde=" + fechaDesde + " " +
                                                    "hasta=" + fechaHasta + " " +
                                                    "filtros=" + filtro + " " +
                                                    "fuentedatos=" + dataSource + " " +
                                                    "codigofabricante=" + fabricante + " " +
                                                    "codigoproducto=" + codigoProducto);

            //Obtener Idc fabricante a partir del código de fabricante
            string idcFabricante = "";
            if (!Utils.IsBlankField(fabricante)) idcFabricante = ObtenerIdcFabricante(db, fabricante, agent);

            //------------------------------------------------------------------------------------------
            //Debemos seleccionar las líneas de facturas con errores (o lineas de facturas, depende de la fuente de datos) 
            //del distribuidor y cargarlas en un registro como  si fuera una línea de fichero de sipIn y ejecutar 
            //el proceso de integracion de facturas individualmente para cada registro.
            //------------------------------------------------------------------------------------------
            string sqlSelect = "SELECT F.NumFactura, F.Ejercicio, F.NumLinea" +
                            ", " + (dataSource.ToLower().Equals("productosfacturas") ? "F.ProductoDistribuidor" : "F.CodigoProducto") +
                            ", Cantidad, " + (dataSource.ToLower().Equals("productosfacturas") ? "F.UMDistribuidor" : "F.UM") +
                            ", " + (dataSource.ToLower().Equals("productosfacturas") ? "CASE WHEN F.PesoCalculado='S' THEN null ELSE F.Peso END as Peso" : "F.Peso") +
                            ", " + (dataSource.ToLower().Equals("productosfacturas") ? "'' as UMPeso" : "F.UMPeso") +
                            ", " + (dataSource.ToLower().Equals("productosfacturas") ? "CASE WHEN F.VolumenCalculado='S' THEN null ELSE F.Volumen END as Volumen" : "F.Volumen") +
                            ", " + (dataSource.ToLower().Equals("productosfacturas") ? "'' as UMVolumen" : "F.UMVolumen") +
                            ", F.PrecioBase, F.Descuentos, F.PrecioBrutoTotal, F.FechaEntrega, F.Almacen, F.Lote, F.FechaCaducidad, F.CodigoPostal, F.Direccion" +
                            ", F.TipoCalle, F.Calle, F.Numero, F.CodigoPais, F.Ruta, F.CodigoComercial, F.EjercicioEntrega, F.NumEntrega, F.NumLineaEntrega" +
                            ", F.CosteDistribuidor, F.DocRelacionado, F.CodigoCliente, F.FechaFactura" +
                            ", " + (dataSource.ToLower().Equals("productosfacturas") ? "'' as UM2" : "F.UM2") +
                            ", " + (dataSource.ToLower().Equals("productosfacturas") ? "'' as Cantidad2" : "F.Cantidad2") +
                            ", " + (dataSource.ToLower().Equals("productosfacturas") ? "'' as PrecioBase2" : "F.PrecioBase2") +
                            ", F.TipoVenta, F.MotivoAbono, F.CodigoPromocion, F.Libre1, F.Libre2, F.Libre3, F.CantLibre1, F.CantLibre2, F.CantLibre3" +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.TipoAcuerdo1") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.SubtipoAcuerdo1") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.ImporteLiquidar1") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.TipoAcuerdo2") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.SubtipoAcuerdo2") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.ImporteLiquidar2") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.TipoAcuerdo3") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.SubtipoAcuerdo3") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.ImporteLiquidar3") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.ValorLibre1Liq") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.ValorLibre2Liq") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.ValorLibre3Liq") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.ValorLibre4Liq") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.ValorLibre5Liq") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.ValorLibre6Liq") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.ValorLibre7Liq") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.ValorLibre8Liq") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.ValorLibre9Liq") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.ValorLibre10Liq") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.ObservacionesLiq") +
                            (dataSource.ToLower().Equals("productosfacturas") ? ", ''" : ", F.TipoRegistro");

            //Muy importante añadir esto en el último lugar, pues después lo recuperamos de alli
            if (dataSource.ToLower().Equals("productosfacturas")) sqlSelect += " , F.PrecioBaseKit, F.DescuentosKit, F.PrecioBrutoTotalKit, F.ProductoKitDistribuidor, F.UMcPeso, F.UMcVolumen "; //Muy importante añadir esto en el último lugar, pues después lo recuperamos de alli
            //fin muy importante

            string sqlFrom = " FROM  " + dataSource + " as F ";

            string sqlWhere = " WHERE "
                + (dataSource.ToLower().Equals("productosfacturas") && (!Utils.IsBlankField(idcFabricante)) ? "F.IdcFabricante = " + idcFabricante + " AND " : "")
                + "F.IdcAgente = " + agent + " ";

            string sWhere = "";
            if (!Utils.IsBlankField(codigoProducto))
            {
                string token = "";
                string tokenComponentes = "";
                string tokenTodos = "";
                string tokenizedString = "";
                string tokenizedStringComponentes = "";
                string tokenizedStringTodos = "";
                StringTokenizer st = new StringTokenizer(codigoProducto, SEPARADOR_FILTRO);
                while (st.HasMoreTokens())
                {
                    if (!String.IsNullOrEmpty(tokenizedString)) tokenizedString += ", ";
                    if (!String.IsNullOrEmpty(tokenizedStringComponentes)) tokenizedStringComponentes += ", ";
                    if (!String.IsNullOrEmpty(tokenizedStringTodos)) tokenizedStringTodos += ", ";
                    
                    token = st.NextToken();
                    tokenizedString += "'" + token + "'";

                    tokenComponentes = ObtenerCodProdComponentes(db, agent, token);
                    if (tokenComponentes == "_noeskit_")
                    {
                        tokenComponentes = token;
                    }
                    else
                    {
                        if (!Utils.IsBlankField(tokenComponentes))
                        {
                            tokenComponentes = tokenComponentes.Substring(1); //Eliminamos la primera comilla
                            tokenComponentes = tokenComponentes.Substring(0, tokenComponentes.Length - 1); //Eliminamos la última comilla
                        }
                    }
                    tokenizedStringComponentes += "'" + tokenComponentes + "'";

                    tokenTodos = tokenComponentes;
                    tokenTodos = (tokenTodos.Contains(token) ? tokenTodos : token + (Utils.IsBlankField(tokenTodos) ? "" : "', '" + tokenTodos));
                    tokenTodos = (tokenTodos.Contains("UNKNOWN") ? tokenTodos : "UNKNOWN', '" + tokenTodos);
                    tokenizedStringTodos += "'" + tokenTodos + "'";
                }
                if (dataSource.ToLower().Equals("productosfacturas"))
                {
                    ////sWhere += " AND " +
                    ////                "(" +
                    ////                "  ( F.ProductoDistribuidor In (" + tokenizedStringComponentes + ") AND (F.ProductoKitDistribuidor IS NULL OR F.ProductoKitDistribuidor In (" + tokenizedString + ") ) ) " +
                    ////                //----INICIO: Esto es porquè podria ser que un producto que no es kit antes si lo fuera
                    ////                "OR " +
                    ////                "  ( F.ProductoDistribuidor = 'UNKNOWN' AND F.ProductoKitDistribuidor In (" + tokenizedString + ") ) " +
                    ////                //----FIN: Esto es porquè podria ser que un producto que no es kit antes si lo fuera
                    ////                ")";

                    sWhere += " AND F.ProductoDistribuidor In (" + tokenizedStringTodos + ") " +
                              " AND " +
                                " ( " +
                                    //----INICIO: Esto es para cuando un producto no es kit
                                    " (F.ProductoDistribuidor In (" + tokenizedString + ") AND F.ProductoKitDistribuidor IS NULL) " +
                                    //----FIN: Esto es para cuando un producto no es kit
                                    //----INICIO: Esto es para obtener todo el desglose de componentes del kit
                                    " OR " +
                                    " (F.ProductoDistribuidor In (" + tokenizedStringComponentes + ") and F.ProductoKitDistribuidor In (" + tokenizedString + ")) " +
                                    //----FIN: Esto es para obtener todo el desglose de componentes del kit
                                    //----INICIO: Esto es porquè podria ser que un producto que no es kit antes si lo fuera
                                    " OR " +
                                    " (F.ProductoDistribuidor = 'UNKNOWN' AND F.ProductoKitDistribuidor In (" + tokenizedString + ")) " +
                                    //----FIN: Esto es porquè podria ser que un producto que no es kit antes si lo fuera
                                " ) ";
                }
                else
                {
                    sWhere += "  AND F.CodigoProducto In (" + tokenizedString + ") ";
                }
            }            

            if (!Utils.IsBlankField(fechaDesde) && !Utils.IsBlankField(fechaHasta))
            {
                sWhere += " AND " + (dataSource.ToLower().Equals("productosfacturas") ? "F.FechaFactura" : "F.FechaModificacion") + " >= " + db.DateForSql(fechaDesde) + " ";
                sWhere += " AND " + (dataSource.ToLower().Equals("productosfacturas") ? "F.FechaFactura" : "F.FechaModificacion") + " <= " + db.DateForSql(fechaHasta) + " ";
            }
            if (!Utils.IsBlankField(fechaDesde) && Utils.IsBlankField(fechaHasta))
            {
                sWhere += " AND " + (dataSource.ToLower().Equals("productosfacturas") ? "F.FechaFactura" : "F.FechaModificacion") + " >= " + db.DateForSql(fechaDesde) + " ";
            }
            if (Utils.IsBlankField(fechaDesde) && !Utils.IsBlankField(fechaHasta))
            {
                sWhere += " AND " + (dataSource.ToLower().Equals("productosfacturas") ? "F.FechaFactura" : "F.FechaModificacion") + " <= " + db.DateForSql(fechaHasta) + " ";
            }

            if (!Utils.IsBlankField(filtro))
            {
                string token = "";
                StringTokenizer st = new StringTokenizer(filtro, SEPARADOR_FILTRO);
                while (st.HasMoreTokens())
                {
                    //añadir el operador AND
                    sWhere += " AND ";
                    token = st.NextToken();
                    sWhere += token + " ";
                }
            }
            sql = sqlSelect + sqlFrom + sqlWhere + sWhere 
                + (dataSource.ToLower().Equals("productosfacturas") ? " ORDER BY F.NumFactura, F.Ejercicio, F.NumLinea " : string.Empty);

            //Primero recorremos los registros a actualizar y los guardamos temporalmente en un fichero de trabajo con el formato de entrada del sipIn
            fieldSeparator = g.GetFieldSeparator();

            SipCore sip = new SipCore();
            SipInFacturasDistribuidor mySip = new SipInFacturasDistribuidor(agent);

            DateTime dt = DateTime.Now;
            string now = dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
            now = now.Replace(" ", "-");
            now = now.Replace(":", "-");

            WorkBox workbox = new WorkBox();
            string workfilePath = workbox.GetWorkBoxFolder();
            filename = workfilePath + "\\wrk" + mySip.GetSlaveNomenclator() + "." + now + ".txt";

            sw = File.AppendText(filename); //creamos el fichero de trabajo
            rs = db.GetDataReader(sql);
            string liniaComponenteAnterior = "";
            string numLinia = "";
            string numLiniaAux = "";
            int i;
            while (rs.Read())
            {
                row = "";
                //Si el campo ProductoKitDistribuidor no está vacío tratamos componentes de un kit
                string codigoKit = (dataSource.ToLower().Equals("productosfacturas") ? db.GetFieldValue(rs, rs.FieldCount - 3) : "");                
                if (!string.IsNullOrEmpty(codigoKit))
                {
                    numLiniaAux = Math.Round(Convert.ToDouble(db.GetFieldValue(rs, 2)), 1).ToString();
                    if (numLiniaAux.Contains(",") && int.TryParse(numLiniaAux.Substring(numLiniaAux.IndexOf(",") + 1), out i) && i != 0)
                        numLinia = numLiniaAux;
                    else
                        numLinia = Convert.ToInt64(Convert.ToDouble(db.GetFieldValue(rs, 2))).ToString();
                    //Control para tratar sólo una línia del desglose de un mismo kit
                    if ((string.IsNullOrEmpty(liniaComponenteAnterior)) ||
                        ((!string.IsNullOrEmpty(liniaComponenteAnterior)) &&
                         (liniaComponenteAnterior != db.GetFieldValue(rs, 0) + db.GetFieldValue(rs, 1)
                            + numLinia + codigoKit)))
                    {
                        //Nos guardamos NumFactura, Ejercicio, int(NumLinea) y ProductoKitDistribuidor para identificar el desglose de un mismo kit
                        liniaComponenteAnterior = db.GetFieldValue(rs, 0) + db.GetFieldValue(rs, 1)
                            + numLinia + codigoKit;
                        for (int pos = 0; pos < 65; pos++)
                        {
                            if (pos == 2) //número línea
                            {
                                row += numLinia + fieldSeparator;
                            }
                            else if (pos == 3) //producto
                            {
                                row += codigoKit + fieldSeparator;
                            }
                            else if (pos == 6) //peso
                            {
                                row += string.Empty + fieldSeparator;
                            }
                            else if (pos == 8) //volumen
                            {
                                row += string.Empty + fieldSeparator;
                            }
                            else if (pos == 10) //precio base
                            {
                                row += db.GetFieldValue(rs, rs.FieldCount - 6) + fieldSeparator;
                            }
                            else if (pos == 11) //descuentos
                            {
                                row += db.GetFieldValue(rs, rs.FieldCount - 5) + fieldSeparator;
                            }
                            else if (pos == 12) //precio bruto total
                            {
                                row += db.GetFieldValue(rs, rs.FieldCount - 4) + fieldSeparator;
                            }
                            else
                            {
                                row += db.GetFieldValue(rs, pos) + fieldSeparator;
                            }
                        }
                        sw.WriteLine(Utils.StringToPrintable(row));
                        numRows++;
                    }
                }
                else
                {
                    for (int pos = 0; pos < 65; pos++)
                    {
                        if (pos == 7 && dataSource.ToLower().Equals("productosfacturas"))
                        {
                            //row += db.GetFieldValue(rs, pos) + fieldSeparator;
                            row += ObtenerUMAgenteDistribuidor(db, agent, db.GetFieldValue(rs, rs.FieldCount - 2)) + fieldSeparator;
                        }
                        else if (pos == 9 && dataSource.ToLower().Equals("productosfacturas"))
                        {
                            //row += db.GetFieldValue(rs, pos) + fieldSeparator;
                            row += ObtenerUMAgenteDistribuidor(db, agent, db.GetFieldValue(rs, rs.FieldCount - 1)) + fieldSeparator;
                        }
                        else
                        {
                            row += db.GetFieldValue(rs, pos) + fieldSeparator;
                        }
                    }
                    sw.WriteLine(Utils.StringToPrintable(row));
                    numRows++;
                }                
            }
            rs.Close();
            rs = null;
            sw.Flush();
            sw.Close();
            sw = null;

            if (numRows > 0)
            {
                CommonRecord rec = null;
                rec = mySip.GetSlaveRecord();

                //Leer fichero de trabajo
                Encoding enc = Utils.GetFileEncoding(filename);
                sr = new StreamReader(filename, enc);
                row = sr.ReadLine();
                //Cargamos datos comunes en memoria
                mySip.LoadData(db, agent);
                while (row != null)
                {
                    //Mapear a la fila al registro de stocks
                    rec.MapRow(row);
                    //Procesar el registro como master
                    mySip.ProcessSlave(rec);
                    if (mySip.resultadoSlave) numIntegrados++;
                    numProcesados++;

                    row = sr.ReadLine();
                }                
                //Limpiamos datos cargados en memoria
                mySip.DeleteData();
                //Generamos logs de forma masiva
                mySip.WriteLogs();

                sr.Close();
                sr = null;
            }

            //eliminamos el fichero de trabajo
            if (File.Exists(filename)) File.Delete(filename);

            log.Info(agent, GetSipTypeName(), "Procesar facturas con errores distribuidor. Se han procesado " + numProcesados + " líneas de factura, de las que se han integrado/actualizado correctamente " + numIntegrados + " .");

            result = "OK";

            log.Info(agent, GetSipTypeName(), "Proceso de actualización terminado con éxito");
        }
        catch (Exception e)
        {
            if (log != null) log.Error(agent, GetSipTypeName(), e);
            throw e;
        }
        finally
        {
            if (rs != null)
                rs.Close();
            if (sw != null)
                sw.Close();
            if (sr != null)
                sr.Close();
        }
        return result;
    }

    /// <summary>
    /// Obtener el código de producto del distribuidor de los componentes de un kit, si no es kit devuelve el mismo código de producto
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="prodFab">registro</param>
    /// <returns>instancia de la clase UMCheck</returns>
    private string ObtenerCodProdComponentes(Database db, string pCodigoDistribuidor, string pCodProd)
    {
        string result = "";
        string resultWrk = "";
        DbDataReader reader = null;
        try
        {
            if (!Utils.IsBlankField(pCodProd))
            {
                string esKit = "";
                //Obtenemos los codigos de producto según el distribuidor de los componentes del producto.
                //Si no es un kit, es decir, si no tiene componentes, entonces la query no devolverá nada
                //Si es un kit de distribuidor esto no funciona (QUEDA PENDIENTE RESOLVER ESTE CASO) ya que la parte de la join con ProductosAgentesKits debería ser diferente
                string sql = "SELECT P.IndEsKit, PADist.Codigo " +
                            " FROM ProductosAgentes PA " +
                            " INNER JOIN Productos P ON PA.Idcagente = " + pCodigoDistribuidor + " AND PA.IdcProducto=P.IdcProducto " + 
                            " INNER JOIN ProductosAgentes PAFab ON PAFab.IdcAgente = P.IdcFabricante AND PAFab.IdcProducto = P.IdcProducto " +
                            " INNER JOIN ProductosAgentesKits PAKit ON PAKit.IdcAgente = P.IdcFabricante AND PAKit.CodigoProductoKit = PAFab.Codigo " +
                            " LEFT JOIN ProductosAgentes PADist ON PADist.IdcAgente = " + pCodigoDistribuidor + " AND PADist.IdcProducto = PAKit.IdcProductoComponente " +
                            " WHERE PA.Codigo = '" + pCodProd + "'";
                reader = db.GetDataReader(sql);
                while (reader.Read())
                {
                    if (Utils.IsBlankField(esKit)) esKit = db.GetFieldValue(reader, 0);
                    string codProdComp = "";
                    codProdComp = db.GetFieldValue(reader, 1);
                    if (Utils.IsBlankField(codProdComp)) codProdComp = "UNKNOWN";
                    if (!String.IsNullOrEmpty(resultWrk)) resultWrk += ", ";
                    resultWrk += "'" + codProdComp + "'";
                }
                reader.Close();
                reader = null;

                ////////if (esKit == "")
                ////////{
                ////////    result = pCodProd;
                ////////}
                ////////else
                ////////{
                ////////    if (!Utils.IsBlankField(resultWrk)) resultWrk = resultWrk.Substring(0, resultWrk.Length -1);
                ////////    result = pCodProd + "', " + resultWrk;
                ////////}
                if (esKit == "")
                {
                    result = "_noeskit_";
                }
                else
                {
                    result = resultWrk;
                }
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return result;
    }

    /// <summary>
    /// Obtener unidad de medida no primaria
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="prodFab">registro</param>
    /// <returns>instancia de la clase UMCheck</returns>
    private string ObtenerUMAgenteDistribuidor(Database db, string pCodigoDistribuidor, string pUMc)
    {
        string result = "";
        DbDataReader reader = null;
        try
        {
            if (!Utils.IsBlankField(pUMc))
            {
                string sql = "Select UMAgente From UMAgente Where IdcAgente = " + pCodigoDistribuidor + " " +
                             "And UMc = '" + pUMc + "'";
                reader = db.GetDataReader(sql);
                if (reader.Read())
                {
                    result = db.GetFieldValue(reader, 0);
                }
                reader.Close();
                reader = null;

                if (Utils.IsBlankField(result))
                {
                    //No existe la conversión de UM. Comprobamos si la unidad de medida que nos ha pasado coincide con una UM de ConnectA. 
                    sql = "select UMc from UnidadesMedida where UMc='" + pUMc + "'";
                    if (Utils.RecordExist(sql))
                    {
                        result = pUMc;
                    }
                }
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return result;
    }

    /// <summary>
    /// Obtener Idc fabricante a partir del código de un fabricante
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="pIdcFabricante">fabricante</param>
    /// <param name="pIdcDistribuidor">distribuidor</param>
    /// <returns>idc fabricante</returns>
    private string ObtenerIdcFabricante(Database db, string pCodigoFabricante, string pIdcDistribuidor)
    {
        string result = "";
        DbDataReader reader = null;
        try
        {
            string sql = "Select IdcAgenteDestino From ClasifInterAgentes Where IdcAgenteOrigen = " + pIdcDistribuidor + " And Codigo = '" + pCodigoFabricante + "'";
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
    /// Invocar a pre proceso
    /// </summary>
    /// <param name="agente">agente</param> 
    public void PreProcess(string agent)
    {
        base.PreProcess(this, agent);
    }

    /// <summary>
    /// Invocar a post proceso
    /// </summary>
    /// <param name="agente">agente</param> 
    public void PostProcess(string agent)
    {
        base.PostProcess(this, agent);
    }


  }
}
