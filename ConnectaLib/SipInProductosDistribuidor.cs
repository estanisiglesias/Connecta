using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Data.Common;
using System.Data.OleDb;
using System.Data;
using System.Text.RegularExpressions;

namespace ConnectaLib
{
  /// <summary>
  /// Clase que gestiona las entradas de productos de distribuidores
  /// </summary>
  public class SipInProductosDistribuidor : SipIn, ISipInInterface
  {
    private string codigoProductoConnecta = "";
    private bool existeProducto = false;
    private bool mismoCodigoProductoFabricante = false;
    private string prefijoMismoCodigoProductoFabricante = "";
    private string idcFabricante = "";
    private string codigoProductoKitCentinela = "";
    private string condicionesUMFijaProducto = ""; 

    // Id de SIP
    public const string ID_SIP = "SipInProductosDistribuidor";

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="agent">agente</param>
    public SipInProductosDistribuidor(string agent) : base(agent, ID_SIP)
    {
    }

    /// <summary>
    /// Obtener id de sip
    /// </summary>
    /// <returns>id de sip</returns>
    public string GetId() 
    {
      return ID_SIP;
    }

    /// <summary>
    /// Obtener XSD de validación de formato XML
    /// </summary>
    /// <returns>XSD de validación de formato XML</returns>
    public string GetXSD() 
    {
      return "schProductosDistribuidor.xsd";
    }

    /// <summary>
    /// Pre-proceso
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    public void PreProcess(string filename)
    {
        InvokeExternalProgram(this, agent);

        Database db = Globals.GetInstance().GetDatabase();
        Distribuidor dist = new Distribuidor();
        condicionesUMFijaProducto = dist.ObtenerParametro(db, agent, "0", "Prod_CondicionesUMFija").Trim();
    }

    /// <summary>
    /// Post-proceso
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    public void PostProcess(string filename)
    {
    }

    /// <summary>
    /// Obtener nomenclator del sip que actúa como maestro (el primero en la
    /// definición del ".ini")
    /// </summary>
    /// <returns>nomenclator</returns>
    public string GetMasterNomenclator()
    {
      return Nomenclator.GetMaster(nomenclator);
    }

    /// <summary>
    /// Obtener nomenclator del sip que actúa como esclavo (el segundo en la
    /// definición del ".ini")
    /// </summary>
    /// <returns>nomenclator</returns>
    public string GetSlaveNomenclator()
    {
        return Nomenclator.GetSlave(nomenclator);
    }

    /// <summary>
    /// Obtener registro maestro
    /// </summary>
    /// <returns>registro maestro</returns>
    public CommonRecord GetMasterRecord()
    {
      return new RecordProductosDistribuidor();
    }

    /// <summary>
    /// Obtener registro esclavo
    /// </summary>
    /// <returns>registro esclavo</returns>
    public CommonRecord GetSlaveRecord()
    {
        return new RecordProductosKitDistribuidor();
    }

    /// <summary>
    /// Obtener sección de XML para el registro maestro
    /// </summary>
    /// <returns>sección de XML</returns>
    public string GetMasterXMLSection()
    {
        return "productodistribuidor";
    }

    /// <summary>
    /// Obtener sección de XML para el registro esclavo
    /// </summary>
    /// <returns>sección de XML</returns>
    public string GetSlaveXMLSection()
    {
        return "ComponenteProductoDistribuidor";
    }

    /// <summary>
    /// Obtener nombre de Sip (básicamente a efectos de log)
    /// </summary>
    /// <returns>nombre de sip</returns>
    public string GetSipTypeName()
    {
        return "Distribuidor.Productos.In";
    }

    /// <summary>
    /// Ajustar el nomenclator. Puede ser variable ya que el fichero de configuración
    /// permite más de un valor para cada sip.
    /// </summary>
    /// <param name="nomenclator">nomenclator</param>
    public void SetNomenclator(string nomenclator)
    {
        this.nomenclator = nomenclator;
    }

    /// <summary>
    /// Proceso del objeto (que puede venir de cualquier fuente de información) y 
    /// lógica de negocio asociada.
    /// </summary>
    /// <param name="pd">objeto</param>
    public void ProcessMaster(CommonRecord rec)
    {
        string sUsuario = Constants.PREFIJO_USUARIO_AGENTE + agent;

        RecordProductosDistribuidor pd = (RecordProductosDistribuidor)rec;
        DbDataReader cursor = null;
        try
        {
            //  - Recoger el código de producto de Connect@ (IdcProducto de la tabla ProductosAgente) en la variable codigoProductoConnecta
            string sql = "";
            bool recalcularErrores = false;
            codigoProductoConnecta = BuscaProducto(pd);
            existeProducto = (codigoProductoConnecta != null);
            string sCodigoFab = "";
            idcFabricante = BuscaFabricante(pd, out sCodigoFab);
            if (!Utils.IsBlankField(sCodigoFab))
            {
                pd.Fabricante = sCodigoFab; //Si hemos obtenido el idcfabricante del codigo alternativo, entonces debemos sustituir el valor de pd.Fabricante que 
                                            //hemos recibido con el valor del campo Codigo que hemos encontrado.
            }
            if (!Utils.IsBlankField(idcFabricante))
            {
                mismoCodigoProductoFabricante = MismoCodigoFab(pd, idcFabricante, out prefijoMismoCodigoProductoFabricante);
            }
            else
            {
                //Si no somos capaces de encontrar el id fabricante, reset de los parametros
                mismoCodigoProductoFabricante = false;
                prefijoMismoCodigoProductoFabricante = "";
            }

            //Ajustamos el EAN
            Int64 numEAN13 = 0;
            if (!Utils.IsBlankField(pd.EAN13))
            {
                if (Int64.TryParse(pd.EAN13, out numEAN13))
                {
                    if (numEAN13 == 0)
                    {
                        pd.EAN13 = "";
                    }
                }
            }

            //Ajustamos la descripción del producto
            string DescProducto = "";
            if (pd.Descripcion.Trim().Length > 60)
                DescProducto = pd.Descripcion.Trim().Remove(60);
            else
                DescProducto = pd.Descripcion.Trim();

            Database db = Globals.GetInstance().GetDatabase();

            //  - Verificar si el el fabricante del producto contra el que está alineado es diferente del fabricante que nos llega
            bool fabricanteOK = true;
            if (existeProducto)
            {
                fabricanteOK = VerificarMismoFabricante(pd);
                if (!fabricanteOK) existeProducto = false;
            }

            //Si existe el producto verificamos si hay reutilización de código
            string pdCodigoProductoOld = pd.CodigoProducto;
            if (existeProducto) 
            {
                string codigoNew = VerificarReutilizacionCodigos(db, pd.CodigoProducto);
                if (!Utils.IsBlankField(codigoNew))
                {
                    pd.CodigoProducto = codigoNew;
                    codigoProductoConnecta = BuscaProducto(pd);
                    existeProducto = (codigoProductoConnecta != null);
                }
            }

            if (existeProducto) //Si existe el producto 
            {
                //Si existe el producto para el distribuidor

                //  - Verificar si el producto que nos llega por INTER.CodigoProducto (que ya existe) efectivamente continua correspondiéndose 
                //      con el mismo producto del fabricante.
                VerificarProducto(pd);

                //  - Obtener la Unidad de Medida Primaria de Connect@ a partir de: INTER.UnidadMedida, CodigoDistribuidor,CodigoProductoConnecta
                string unidadMedidaConnecta = UnidadMedida(pd, codigoProductoConnecta);

                //  - Obtener la descripción actual del producto i la fecha de inserción actual para después decidir si ha habido cambios en la descripción
                string descripcionAnterior = "";
                string fechaInsercion = null;
                BuscaDescripcionFecha(db, pd.CodigoProducto, out descripcionAnterior, out fechaInsercion);

                //  - Actualizar datos de ProductosAgente
                sql = "UPDATE ProductosAgentes SET " +
                    " Descripcion=" + (DescProducto.ToUpper().Contains(Constants.PRODUCTOS_DISTR_SIN_DESCRIPCION + " " + pd.CodigoProducto) ? "Descripcion ": db.ValueForSql(DescProducto) + " ") +
                    (!Utils.IsBlankField(pd.Status)? ", Status=" + db.ValueForSql(pd.Status) : "") +
                    ", UMc=" + db.ValueForSql(unidadMedidaConnecta) +
                    ", Clasificacion1=" + db.ValueForSql(pd.Clasificacion1) +
                    ", Clasificacion2=" + db.ValueForSql(pd.Clasificacion2) +
                    ", Clasificacion3=" + db.ValueForSql(pd.Clasificacion3) +
                    ", Clasificacion4=" + db.ValueForSql(pd.Clasificacion4) +
                    ", Clasificacion5=" + db.ValueForSql(pd.Clasificacion5) +
                    ", Clasificacion6=" + db.ValueForSql(pd.Clasificacion6) +
                    ", Clasificacion7=" + db.ValueForSql(pd.Clasificacion7) +
                    ", Clasificacion8=" + db.ValueForSql(pd.Clasificacion8) +
                    ", Clasificacion9=" + db.ValueForSql(pd.Clasificacion9) +
                    ", Clasificacion10=" + db.ValueForSql(pd.Clasificacion10) +
                    ", Clasificacion11=" + db.ValueForSql(pd.Clasificacion11) +
                    ", Clasificacion12=" + db.ValueForSql(pd.Clasificacion12) +
                    ", Clasificacion13=" + db.ValueForSql(pd.Clasificacion13) +
                    ", Clasificacion14=" + db.ValueForSql(pd.Clasificacion14) +
                    ", Jerarquia=" + db.ValueForSql(pd.Jerarquia) +
                    ", FechaModificacion=" + db.SysDate() +
                    ", UsuarioModificacion=" + db.ValueForSql(sUsuario) + " " +
                    (!Utils.IsBlankField(idcFabricante) ? ", IdcFabricante=" + idcFabricante + " " : "");
                sql += " WHERE IdcAgente = " + agent + " and Codigo = '" + pd.CodigoProducto + "'";
                db.ExecuteSql(sql, agent, GetSipTypeName());                

                //Analizamos aquí si ha havido cambios en la descripción, 
                //lo hacemos en este punto después de haber modificado el registro en base de datos 
                //porque el registro texto que nos llega no queda igual una vez lo hemos grabado en la base de datos
                //si el valor contiene caracteres especiales
                //y por lo tanto queremos comparar peras con peras
                //y lo podemos hacer gracias a que anteriormente hemos precargado la descripción anterior
                //  - Obtener la descripción actual del producto i la fecha de inserción actual para después decidir si ha habido cambios en la descripción
                string descripcionActual = "";
                string fechaInsercionkk = null;
                BuscaDescripcionFecha(db, pd.CodigoProducto, out descripcionActual, out fechaInsercionkk);

                //Formateamos cadenas para descartar los digitos que no sean ni números ni letras antes de la comparación entre descripción actual y anterior
                Regex regex = new Regex("[a-zA-Z0-9]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                MatchCollection mCollection = regex.Matches(descripcionAnterior);
                string descripcionAnteriorFormateada = "";
                foreach (Match match in mCollection)                
                    descripcionAnteriorFormateada += match.Value.Trim();
                mCollection = regex.Matches(descripcionActual);
                string descripcionActualFormateada = "";
                foreach (Match match in mCollection)
                    descripcionActualFormateada += match.Value.Trim();

                if (descripcionAnteriorFormateada.ToLower() != descripcionActualFormateada.ToLower())
                {
                    // Si la descripción actual es diferente a la nueva descripción, puede ser un caso de reutilización de códigos de producto.
                    // En éste caso, guardamos la descripción antigua con su vigencia en la tabla ProductosAgentesHistorico calculando las 
                    // diferencias respeto la descripción actual.
                    int dif = Utils.ComputeDistanceWithLevenshtein(descripcionAnteriorFormateada.ToLower(), descripcionActualFormateada.ToLower());
                    InsertarProductosAgentesHistorico(db, pdCodigoProductoOld, descripcionAnterior, descripcionActual, fechaInsercion, dif);
                }

                //Tratamos UM fija: Verificamos si el producto (la descripción del producto) nos determina una UM fija.
                TratarUMFijaProducto(db, pd.CodigoProducto, DescProducto);
            }
            else   //No existe producto (2.2) o si existe pero el fabricante no coincide
            {
                if (pd.EsKit == "S")
                {
                    //Si el producto es un kit de distribuidor lo alineamos contra el producto kit "ficticio"
                    codigoProductoConnecta = BuscaProductoKitDistribuidor();
                    existeProducto = !string.IsNullOrEmpty(codigoProductoConnecta);
                }
                else
                {
                    //Intentamos mirar si el producto está alineado para otro distribuidor
                    //del mismo Grupo de productos
                    VerificarGrupoProductos(db, pd);

                    if (!existeProducto)
                    {
                        //Buscamos el producto por alineación con el código de fabricante
                        AlinearConProductoFabricante(db, pd);
                    }

                    //Si aún no existe el producto... 
                    if (!existeProducto)
                    {
                        //SVO: 03-03-2011: Hemos decidido no hacer el alineamiento por EAN
                        //Int64 valEAN13 = 0;
                        //if (!Utils.IsBlankField(pd.EAN13)) 
                        //{
                        //    if (!Int64.TryParse(pd.EAN13, out valEAN13))
                        //    {
                        //        VerificarEAN(db, pd);
                        //    }
                        //    else if (!valEAN13.Equals(0))
                        //    {
                        //        VerificarEAN(db, pd);
                        //    }
                        //}
                    }
                    if (!existeProducto)
                    {
                        //Se insertará un registro en la tabla de ProductosAgentesNoExistentes
                        if (InsertarProductoNoExistente(db, pd))
                        {
                            string myAlertMsg = "Error en producto distribuidor. El producto {0} – {1} no se ha encontrado en Connecta.";
                            Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "PRDI0001", myAlertMsg, pd.CodigoProducto, pd.Descripcion);
                        }
                    }
                }
                if (existeProducto && fabricanteOK) //significa que es un producto totalmente nuevo (que no se trata del caso de un producto que ya existia pero para otro fabricante)
                {
                    //Si existe el producto... (punto 2.4)
                    //Obtener unidad de medida primaria
                    string unidadMedidaConnecta = UnidadMedida(pd, codigoProductoConnecta);

                    //Dar de alta en Productos agente
                    sql = "insert into ProductosAgentes ( IdcAgente,IdcProducto,Codigo,Descripcion";
                    if (!Utils.IsBlankField(pd.Status))
                        sql += ",Status";
                    sql += ",UMc,Clasificacion1,Clasificacion2" +
                        ",Clasificacion3,Clasificacion4,Clasificacion5,Clasificacion6,Clasificacion7" +
                        ",Clasificacion8,Clasificacion9,Clasificacion10,Clasificacion11,Clasificacion12" +
                        ",Clasificacion13,Clasificacion14,Jerarquia,UsuarioInsercion,UsuarioModificacion";
                    if (!Utils.IsBlankField(idcFabricante))
                        sql += ",IdcFabricante";
                    sql += " ) " +
                        "values ( " + agent + "," + codigoProductoConnecta +
                        "," + db.ValueForSql(pd.CodigoProducto) +
                        "," + db.ValueForSql(DescProducto);
                    if (!Utils.IsBlankField(pd.Status))
                        sql += "," + db.ValueForSql(pd.Status);
                    sql += "," + db.ValueForSql(unidadMedidaConnecta) +
                        "," + db.ValueForSql(pd.Clasificacion1) +
                        "," + db.ValueForSql(pd.Clasificacion2) +
                        "," + db.ValueForSql(pd.Clasificacion3) +
                        "," + db.ValueForSql(pd.Clasificacion4) +
                        "," + db.ValueForSql(pd.Clasificacion5) +
                        "," + db.ValueForSql(pd.Clasificacion6) +
                        "," + db.ValueForSql(pd.Clasificacion7) +
                        "," + db.ValueForSql(pd.Clasificacion8) +
                        "," + db.ValueForSql(pd.Clasificacion9) +
                        "," + db.ValueForSql(pd.Clasificacion10) +
                        "," + db.ValueForSql(pd.Clasificacion11) +
                        "," + db.ValueForSql(pd.Clasificacion12) +
                        "," + db.ValueForSql(pd.Clasificacion13) +
                        "," + db.ValueForSql(pd.Clasificacion14) +
                        "," + db.ValueForSql(pd.Jerarquia) +
                        "," + db.ValueForSql(sUsuario) +
                        "," + db.ValueForSql(sUsuario);
                    if (!Utils.IsBlankField(idcFabricante))
                        sql += "," + idcFabricante;
                    sql += " ) ";
                    db.ExecuteSql(sql, agent, GetSipTypeName());

                    //Además, para asegurar el tiro, se borra el registro que potencialmente 
                    //pueda existir en la tabla ProductosAgentesNoExistentes
                    int i = BorrarProductoNoExistente(db, pd);

                    //Si el producto existía a Productos No Existentes
                    //  Y tenía sell out, stocks, con errores (porque no estaba alineado) lanzamos sipUpd de Facturas Con Errores
                    if (i > 0)
                        recalcularErrores = true;

                    //Tratamos UM fija: Verificamos si el producto (la descripción del producto) nos determina una UM fija.
                    TratarUMFijaProducto(db, pd.CodigoProducto, DescProducto);
                }
                else if (existeProducto && !fabricanteOK) //significa que veniamos de que el producto ya existia pero estava alineado con un producto de otro fabricante y lo que hemos hecho es buscar el alineamiento de nuevo
                {
                    //  - Obtener la Unidad de Medida Primaria de Connect@ a partir de: INTER.UnidadMedida, CodigoDistribuidor,CodigoProductoConnecta
                    string unidadMedidaConnecta = UnidadMedida(pd, codigoProductoConnecta);

                    //  - Actualizar datos de ProductosAgente
                    sql = "UPDATE ProductosAgentes SET " +
                        " Descripcion=" + (DescProducto.ToUpper().Contains(Constants.PRODUCTOS_DISTR_SIN_DESCRIPCION + " " + pd.CodigoProducto) ? "Descripcion " : db.ValueForSql(DescProducto) + " ") +
                        (!Utils.IsBlankField(pd.Status) ? ", Status=" + db.ValueForSql(pd.Status) : "") +
                        ", UMc=" + db.ValueForSql(unidadMedidaConnecta) +
                        ", Clasificacion1=" + db.ValueForSql(pd.Clasificacion1) +
                        ", Clasificacion2=" + db.ValueForSql(pd.Clasificacion2) +
                        ", Clasificacion3=" + db.ValueForSql(pd.Clasificacion3) +
                        ", Clasificacion4=" + db.ValueForSql(pd.Clasificacion4) +
                        ", Clasificacion5=" + db.ValueForSql(pd.Clasificacion5) +
                        ", Clasificacion6=" + db.ValueForSql(pd.Clasificacion6) +
                        ", Clasificacion7=" + db.ValueForSql(pd.Clasificacion7) +
                        ", Clasificacion8=" + db.ValueForSql(pd.Clasificacion8) +
                        ", Clasificacion9=" + db.ValueForSql(pd.Clasificacion9) +
                        ", Clasificacion10=" + db.ValueForSql(pd.Clasificacion10) +
                        ", Clasificacion11=" + db.ValueForSql(pd.Clasificacion11) +
                        ", Clasificacion12=" + db.ValueForSql(pd.Clasificacion12) +
                        ", Clasificacion13=" + db.ValueForSql(pd.Clasificacion13) +
                        ", Clasificacion14=" + db.ValueForSql(pd.Clasificacion14) +
                        ", Jerarquia=" + db.ValueForSql(pd.Jerarquia) +
                        ", FechaModificacion=" + db.SysDate() +
                        ", UsuarioModificacion=" + db.ValueForSql(sUsuario) + " " +
                        (!Utils.IsBlankField(idcFabricante) ? ", IdcFabricante=" + idcFabricante + " " : "") +
                        ", IdcProducto =" + codigoProductoConnecta + " ";
                    sql += " WHERE IdcAgente = " + agent + " and Codigo = '" + pd.CodigoProducto + "'";
                    db.ExecuteSql(sql, agent, GetSipTypeName());

                    //Tratamos UM fija: Verificamos si el producto (la descripción del producto) nos determina una UM fija.
                    TratarUMFijaProducto(db, pd.CodigoProducto, DescProducto);
                }
            }

            //Si el producto tenía sell out, stocks... con errores (porque no estaba alineado) lanzamos sipUpd de Facturas Con Errores
            if (recalcularErrores)
            {
                if (ExistenConErrores(db, agent, pd.CodigoProducto, "ProductosFacturasConErrores"))
                    EjecutarSipUpdFacturasDistribuidor(db, agent, pd.CodigoProducto, "productosfacturasconerrores", "sipUpdProcesarFacturasDistribuidor");
                if (ExistenConErrores(db, agent, pd.CodigoProducto, "StocksConErrores"))
                    EjecutarSipUpdStocksDistribuidor(db, agent, pd.CodigoProducto, "stocksconerrores", "sipUpdProcesarStocksDistribuidor");
                if (ExistenConErrores(db, agent, pd.CodigoProducto, "StocksClienteConErrores"))
                    EjecutarSipUpdStocksClienteDistribuidor(db, agent, pd.CodigoProducto, "stocksclienteconerrores", "sipUpdProcesarStocksClienteDistribuidor");
            }

            //Eliminar los componentes para productos kit (lo hacemos en todos los casos por si era kit y ha dejado de serlo)
            sql = "Delete From ProductosAgentesKits where IdcAgente = " + agent + " and CodigoProductoKit = '" + pd.CodigoProducto + "' ";
            db.ExecuteSql(sql, agent, GetSipTypeName());

        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
    }

    /// <summary>
    /// Proceso del objeto (que puede venir de cualquier fuente de información) y 
    /// lógica de negocio asociada.
    /// </summary>
    /// <param name="pd">objeto</param>
    public void ProcessSlave(CommonRecord rec)
    {
        DbDataReader reader = null;
        string sql = "";
        try
        {
            string sUsuario = Constants.PREFIJO_USUARIO_AGENTE + agent;
            string idcProductoKit = "";
            string idcProductoComp = "";
            RecordProductosKitDistribuidor linea = (RecordProductosKitDistribuidor)rec;
            Log2 log = Globals.GetInstance().GetLog2();
            Database db = Globals.GetInstance().GetDatabase();
            Producto prod = new Producto();

            //Verificamos si existe reutilización de códigos ya sea para el producto kit cómo para los componentes
            string codigoNew = VerificarReutilizacionCodigos(db, linea.CodigoProductoKit);
            if (!Utils.IsBlankField(codigoNew))
                linea.CodigoProductoKit = codigoNew;
            codigoNew = VerificarReutilizacionCodigos(db, linea.CodigoProductoComp);
            if (!Utils.IsBlankField(codigoNew))
                linea.CodigoProductoComp = codigoNew;

            if (!codigoProductoKitCentinela.Equals(linea.CodigoProductoKit))
            {
                //Comprobamos que exista el producto kit            
                if (!prod.ComprobarCodigoProducto(db, agent, linea.CodigoProductoKit))
                {
                    string myAlertMsg = "Error en producto kit distribuidor. No existe el código de producto kit {0}.";
                    log.Trace(agent, GetSipTypeName(), "PRDI0009", myAlertMsg, linea.CodigoProductoKit);
                    return;
                }
                idcProductoKit = prod.CodigoProducto;
                codigoProductoKitCentinela = linea.CodigoProductoKit;

                //Eliminar los componentes para productos kit
                sql = "Delete From ProductosAgentesKits where IdcAgente = " + agent + " and CodigoProductoKit = '" + linea.CodigoProductoKit + "' ";
                db.ExecuteSql(sql, agent, GetSipTypeName());
            }

            //Comprobamos que exista el componente del kit.
            //Si existe nos guardamos el código de producto "connecta" idcProducto            
            if (!prod.ComprobarCodigoProducto(db, agent, linea.CodigoProductoComp))
            {
                string myAlertMsg = "Error en producto kit distribuidor. No existe el código de producto componente para el producto kit {0}.";
                log.Trace(agent, GetSipTypeName(), "PRDI0010", myAlertMsg, linea.CodigoProductoKit);
                return;
            }
            idcProductoComp = prod.CodigoProducto;

            //Obtener la Unidad de Medida de Connecta para el componente
            string UMc = UnidadMedida(linea.UMComp, linea.CodigoProductoComp, idcProductoComp);

            string where = "WHERE IdcAgente =" + agent + " " +
                            " AND CodigoProductoKit = '" + linea.CodigoProductoKit + "'" +
                            " AND IdcProductoComponente = " + idcProductoComp ;
            sql = "SELECT IdcAgente FROM ProductosAgentesKits " + where;
            if (Utils.RecordExist(sql))
            {
                sql = "update ProductosAgentesKits set " +
                              "UMcComponente = " + db.ValueForSql(UMc) +
                              ",CantidadComponente = " + db.ValueForSqlAsNumeric(linea.CantidadComp) +
                              ",ProporcionImporteComponente = " + db.ValueForSqlAsNumeric(linea.ProporcionImporteComp) +
                              ",FechaModificacion = " + db.SysDate() +
                              ",UsuarioModificacion = " + db.ValueForSql(sUsuario) +
                              ",IdcProductoKit = " + idcProductoKit +
                              ",CodigoProductoComponente = " + db.ValueForSql(linea.CodigoProductoComp) +
                              " " + where;
            }
            else
            {
                sql = "insert into ProductosAgentesKits(" +
                              "IdcAgente" +
                              ",CodigoProductoKit" +
                              ",IdcProductoKit" +
                              ",CodigoProductoComponente" +
                              ",IdcProductoComponente" +
                              ",UMcComponente" +
                              ",CantidadComponente" +
                              ",ProporcionImporteComponente" +
                              ",UsuarioInsercion" +
                              ") values (" +
                              agent +
                              "," + db.ValueForSql(linea.CodigoProductoKit) +
                              "," + idcProductoKit +
                              "," + db.ValueForSql(linea.CodigoProductoComp) +
                              "," + idcProductoComp +
                              "," + db.ValueForSql(UMc) +
                              "," + db.ValueForSqlAsNumeric(linea.CantidadComp) +
                              "," + db.ValueForSqlAsNumeric(linea.ProporcionImporteComp) +
                              "," + db.ValueForSql(sUsuario) +
                              ")";
            }
            db.ExecuteSql(sql, agent, GetSipTypeName());
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
    }

    /// <summary>Ejecuta sipUpd para hacer recalculo cantidades (FACTURAS, STOCKS, ENTREGAS, STOCKS CLIENTE)</summary>
    private void EjecutarSipUpdFacturasDistribuidor(Database db, string psipAgent, string psipProductCode, string psipDataSource, string psipType)
    {
        if (String.IsNullOrEmpty(psipAgent))
            return;

        sipUpdProcesarFacturasDistribuidor mySip = new sipUpdProcesarFacturasDistribuidor(psipAgent);
        mySip.Process(psipAgent, "", "", "", psipProductCode, psipDataSource, "");
    }

    private void EjecutarSipUpdEntregasDistribuidor(Database db, string psipAgent, string psipProductCode, string psipDataSource, string psipType)
    {
        if (String.IsNullOrEmpty(psipAgent))
            return;

        //sipUpdProcesarEntregasDistribuidor mySip = new sipUpdProcesarEntregasDistribuidor(psipAgent);
        //mySip.Process(psipAgent, "", "", "", psipProductCode, psipDataSource);
    }

    private void EjecutarSipUpdStocksDistribuidor(Database db, string psipAgent, string psipProductCode, string psipDataSource, string psipType)
    {
        if (String.IsNullOrEmpty(psipAgent))
            return;

        //sipUpdProcesarStocksDistribuidor mySip = new sipUpdProcesarStocksDistribuidor(psipAgent);
        //mySip.Process(psipAgent, "", "", "", psipProductCode, psipDataSource);
    }

    private void EjecutarSipUpdStocksClienteDistribuidor(Database db, string psipAgent, string psipProductCode, string psipDataSource, string psipType)
    {
        if (String.IsNullOrEmpty(psipAgent))
            return;

        //sipUpdProcesarStocksClienteDistribuidor mySip = new sipUpdProcesarStocksClienteDistribuidor(psipAgent);
        //mySip.Process(psipAgent, "", "", "", psipProductCode, "", psipDataSource);
    }

    /// <summary>
    /// Verificar si se ha reutilizado el código de producto.
    /// Si encontramos reutilización creamos código virtual para identificar nuevo producto.
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="codigoProducto">Código del producto del distribuidor</param>
    private string VerificarReutilizacionCodigos(Database db, string codigoProducto)
    {
        string cod = "";
        DbDataReader cursor = null;
        try
        {
            string sql = " select CodigoProducto + 'v' + cast(count(1) as varchar(100)) as versionProducto " +
                         "   from ProductosAgentesHistorico " +
                         "  where IdcAgente=" + agent + " and CodigoProducto=" + db.ValueForSql(codigoProducto) + " and IndReutilizacionCodigos='S'" +
                         "  group by CodigoProducto";
            cursor = db.GetDataReader(sql);            
            if (cursor.Read())
                cod = db.GetFieldValue(cursor, 0);       
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }        
        return cod;
    }

    /// <summary>
    /// Tratar la UM fija del producto. Si en la descripción del producto se indica, asignar una UM fija al producto 
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="pAgent">agente</param>
    private void TratarUMFijaProducto(Database db, string codigoProducto, string descripcion)
    {
        try
        {
            //Si está informado el parametro que indica que se debe tratar la UM fija, se trata
            if (!Utils.IsBlankField(condicionesUMFijaProducto))
            {
                //Explotar las diferentes expresiones contenidas en el valor del parámetro.
                //Y para cada una de las expresiones, evaluar la condición
                string[] condiciones = condicionesUMFijaProducto.Split(';');
                foreach (string item in condiciones)
                {
                    int ix = item.IndexOf(":");
                    if (ix != -1)
                    {
                        string expresion = item.Substring(0, ix);
                        string umFija = item.Substring(ix).Replace(":","");

                        //Evaluar la expresión
                        Regex regex = new Regex(expresion.Replace("(", "xxx").Replace(")", "xxx"), RegexOptions.IgnoreCase);
                        MatchCollection mCollection = regex.Matches(descripcion.Replace("(", "xxx").Replace(")", "xxx"));
                        if (mCollection.Count > 0)
                        {
                            //Si se cumple la condición insertar la UM fija (si ya existe, actualizarla)
                            string where = "WHERE IdcAgente =" + agent + " AND Codigo = " + db.ValueForSql(codigoProducto);
                            string sql = "SELECT IdcAgente FROM ProductosAgentesUMsFijas WHERE IdcAgente = " + agent + " AND Codigo = " + db.ValueForSql(codigoProducto);
                            if (!Utils.RecordExist(sql))
                            {
                                sql = "insert into ProductosAgentesUMsFijas (" +
                                              "IdcAgente" +
                                              ",Codigo" +
                                              ",Descripcion" +
                                              ",UMcFija" +
                                              ") values (" +
                                              agent +
                                              "," + db.ValueForSql(codigoProducto) +
                                              "," + db.ValueForSql(descripcion) +
                                              "," + db.ValueForSql(umFija) +
                                              ")";
                                db.ExecuteSql(sql, agent, GetSipTypeName());

                                string myAlertMsg = "Aviso en producto distribuidor. Producto {0} – {1} se ha fijado con la UM {2} según la expresión {3}.";
                                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "PRDI0016", myAlertMsg, codigoProducto, descripcion, umFija, expresion);
                            }
                            //else
                            //{
                            //    sql = "UPDATE ProductosAgentesUMsFijas SET " +
                            //                  "Descripcion = " + db.ValueForSql(descripcion) +
                            //                  ",UMcFija = " + db.ValueForSql(umFija) +
                            //                  " " + where;
                            //    db.ExecuteSql(sql, agent, GetSipTypeName());

                            //    string myAlertMsg = "Aviso en producto distribuidor. Producto {0} – {1} se ha fijado con la UM {2} según la expresión {3}.";
                            //    Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "PRDI0016", myAlertMsg, codigoProducto, descripcion, umFija, expresion);
                            //}
                        }
                    }
                }
            }
        }
        finally
        {
        }
    }

    /// <summary>
    /// Alinear con el producto del fabricante, ajustando variables de la clase codigoProductoConnecta y 
    /// existeProducto si fuera necesario
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="pd">registro</param>
    private void AlinearConProductoFabricante(Database db, RecordProductosDistribuidor pd)
    {
        DbDataReader cursor = null;
        try
        {
            string sql = "";
            string codigoProductoFabricante = "";
            if (Utils.IsBlankField(pd.CodigoProdFab) && mismoCodigoProductoFabricante)
            {
                codigoProductoFabricante = pd.CodigoProducto;
                if (!Utils.IsBlankField(prefijoMismoCodigoProductoFabricante))
                {
                    if (codigoProductoFabricante.StartsWith(prefijoMismoCodigoProductoFabricante))
                    {
                        codigoProductoFabricante = codigoProductoFabricante.Substring(prefijoMismoCodigoProductoFabricante.Length);
                    }
                }
            }
            else
            {
                codigoProductoFabricante = pd.CodigoProdFab;
            }

            //Mirar si está en blanco el código prod. fabricante o el fabricante
            if (!Utils.IsBlankField(codigoProductoFabricante) && !Utils.IsBlankField(pd.Fabricante))
            {
                sql = "Select IdcAgenteDestino From ClasifInterAgentes Where IdcAgenteOrigen = " + agent + " And Codigo = '" + pd.Fabricante + "'";
                cursor = db.GetDataReader(sql);
                int idcFab = -1;
                bool fabricanteOK = false;
                if (cursor.Read())
                {
                    idcFab = Int32.Parse(db.GetFieldValue(cursor, 0));
                    fabricanteOK = true;
                }
                cursor.Close();
                cursor = null;

                if (idcFab != -1)
                    idcFabricante = idcFab.ToString();

                if (fabricanteOK)
                {
                    Double numCodigoProductoFabricante = 0;

                    //Se ha encontrado el fabricante... debemos buscar el producto, 
                    //pero haremos una búsqueda un poco avanzada: si el dato es numérico, además lo buscaremos convertido a número
                    sql = "Select IdcProducto From ProductosAgentes Where IdcAgente = " + idcFabricante;
                    if (Double.TryParse(codigoProductoFabricante, out numCodigoProductoFabricante))
                    {
                        sql += " And ";
                        sql += "   ( ";
                        sql += "     ( ";
                        sql += "      Codigo = '" + codigoProductoFabricante + "' ";
                        sql += "      OR (not Codigo is null and ltrim(rtrim(Codigo))<>'' and patindex('%[^0-9,.]%',ltrim(rtrim(Codigo)))=0  and left(ltrim(Codigo),1) not in ('.',',') and right(rtrim(Codigo),1) not in ('.',',') ";
                        sql += "          and cast(replace(Codigo,',','.') as numeric(18,0)) = " + db.ValueForSqlAsNumeric(numCodigoProductoFabricante.ToString()) + ") ";
                        sql += "     ) ";
                        sql += "     OR ";
                        sql += "     ( ";
                        sql += "      CodigoAlt = '" + codigoProductoFabricante + "' ";
                        sql += "      OR (not CodigoAlt is null and ltrim(rtrim(CodigoAlt))<>'' and patindex('%[^0-9,.]%',ltrim(rtrim(CodigoAlt)))=0  and left(ltrim(CodigoAlt),1) not in ('.',',') and right(rtrim(CodigoAlt),1) not in ('.',',') ";
                        sql += "          and cast(replace(CodigoAlt,',','.') as numeric(18,0)) = " + db.ValueForSqlAsNumeric(numCodigoProductoFabricante.ToString()) + ") ";
                        sql += "     ) ";
                        sql += "   ) ";
                    }
                    else
                    {
                        sql += " And ";
                        sql += "   ( ";
                        sql += "     Codigo = '" + codigoProductoFabricante + "' ";
                        sql += "     OR ";
                        sql += "     CodigoAlt = '" + codigoProductoFabricante + "' ";
                        sql += "   ) ";
                    }

                    cursor = db.GetDataReader(sql);
                    if (cursor.Read())
                    {
                        codigoProductoConnecta = db.GetFieldValue(cursor, 0);
                        existeProducto = true;
                    }
                    cursor.Close();
                    cursor = null;

                    if (!existeProducto) //si aun no hemos encontrado el producto del fabricante
                    {
                        if (mismoCodigoProductoFabricante) //y si está activo el flag de que usa el mismo código de producto que el fabricante
                        {
                            if (codigoProductoFabricante == pd.CodigoProdFab) //y si el código que se ha buscado no es el código de producto del distribuidor
                            {
                                //entonces buscaremos de nuevo con el código de producto del distribuidor
                                codigoProductoFabricante = pd.CodigoProducto;
                                if (!Utils.IsBlankField(prefijoMismoCodigoProductoFabricante))
                                {
                                    if (codigoProductoFabricante.StartsWith(prefijoMismoCodigoProductoFabricante))
                                    {
                                        codigoProductoFabricante = codigoProductoFabricante.Substring(prefijoMismoCodigoProductoFabricante.Length);
                                    }
                                }
                                sql = "Select IdcProducto From ProductosAgentes Where IdcAgente = " + idcFabricante;
                                if (Double.TryParse(codigoProductoFabricante, out numCodigoProductoFabricante))
                                {
                                    sql += " And ";
                                    sql += "   ( ";
                                    sql += "     ( ";
                                    sql += "      Codigo = '" + codigoProductoFabricante + "' ";
                                    sql += "      OR (not Codigo is null and ltrim(rtrim(Codigo))<>'' and patindex('%[^0-9,.]%',ltrim(rtrim(Codigo)))=0 and left(ltrim(Codigo),1) not in ('.',',') and right(rtrim(Codigo),1) not in ('.',',') ";
                                    sql += "          and cast(replace(Codigo,',','.') as numeric(18,0)) = " + db.ValueForSqlAsNumeric(numCodigoProductoFabricante.ToString()) + ") ";
                                    sql += "     ) ";
                                    sql += "     OR ";
                                    sql += "     ( ";
                                    sql += "      CodigoAlt = '" + codigoProductoFabricante + "' ";
                                    sql += "      OR (not CodigoAlt is null and ltrim(rtrim(CodigoAlt))<>'' and patindex('%[^0-9,.]%',ltrim(rtrim(CodigoAlt)))=0  and left(ltrim(CodigoAlt),1) not in ('.',',') and right(rtrim(CodigoAlt),1) not in ('.',',') ";
                                    sql += "          and cast(replace(CodigoAlt,',','.') as numeric(18,0)) = " + db.ValueForSqlAsNumeric(numCodigoProductoFabricante.ToString()) + ") ";
                                    sql += "     ) ";
                                    sql += "   ) ";
                                }
                                else
                                {
                                    sql += " And ";
                                    sql += "   ( ";
                                    sql += "     Codigo = '" + codigoProductoFabricante + "' ";
                                    sql += "     OR ";
                                    sql += "     CodigoAlt = '" + codigoProductoFabricante + "' ";
                                    sql += "   ) ";
                                }
                                cursor = db.GetDataReader(sql);
                                if (cursor.Read())
                                {
                                    codigoProductoConnecta = db.GetFieldValue(cursor, 0);
                                    existeProducto = true;
                                }
                                cursor.Close();
                                cursor = null;
                            }
                        }
                    }
                }
                else
                {
                    //No se ha encontrado el fabricante. Escribir en el log...
                    //Globals.GetInstance().GetLog().DetailedError(agent, GetSipTypeName(), "Error en producto distribuidor. No se puede encontrar el código de fabricante:" + pd.Fabricante + " para el producto: " + pd.CodigoProducto + " – " + pd.Descripcion);                                        
                    string myAlertMsg = "Error en producto distribuidor. No se puede encontrar el código de fabricante ({0}) para el producto {1} – {2}";
                    string[] aValores = new string[] { pd.Fabricante, pd.CodigoProducto, pd.Descripcion };
                    string[] aClaves = new string[] { pd.Fabricante, pd.CodigoProducto };
                    string[] aClavesExt = new string[] { pd.Fabricante, pd.CodigoProducto };
                    Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "PRDI0002", myAlertMsg, aValores, aClaves, aClavesExt);                                                                         
                }
            }
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }    
    }

    /// <summary>
    /// Verificar si el producto está alineado para otro distribuidor del mismo grupo de productos, 
    /// en caso afirmativo se ajustan variables de la clase codigoProductoConnecta y 
    /// existeProducto si fuera necesario
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="pd">registro</param>
    private void VerificarGrupoProductos(Database db, RecordProductosDistribuidor pd)
    {
        DbDataReader cursor = null;
        try
        {
            string sql = "select pa.IdcProducto, p.IdcFabricante from ProductosAgentes pa " +
                        "left join productos p on pa.IdcProducto=p.IdcProducto " +
                        "where pa.Codigo = '" + pd.CodigoProducto + "' " +
                        "and pa.IdcAgente in " +
	                    "       (select idcagente from distribuidores " +
                        "        where idcagente<>" + agent + " " +
                        "        and COALESCE(GrupoProductos,'')<>'' " +
                        "        and grupoproductos in " +
                        "                 (select grupoproductos from Distribuidores " +
                        "                  where IdcAgente=" + agent + " " +
                        "                  ) " +
                        "       ) ";
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
            {
                codigoProductoConnecta = db.GetFieldValue(cursor, 0);
                idcFabricante = db.GetFieldValue(cursor, 1);
                existeProducto = true;
            }
            cursor.Close();
            cursor = null;
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
    }

    /// <summary>
    /// Verificar EAN, ajustando variables de la clase codigoProductoConnecta y 
    /// existeProducto si fuera necesario
    /// </summary>
    /// <param name="db"></param>
    /// <param name="pd"></param>
    private void VerificarEAN(Database db, RecordProductosDistribuidor pd)
    {
        DbDataReader cursor = null;
        try
        {
            /**
              Se mirará si el producto existe en el maestro de Connect@ (Productos) seleccionando por Productos.EAN13 = INTER.EAN13 
             * y ordenando por Productos.Status ASC, Productos.UMc ASC, Productos.idcConnecta Desc, 
             * para que encuentre primero los que están activos (estado AC), con unidad de medida (BOX antes que DSP) 
             * y los que tienen IdcConnecta mayor, es decir, los más nuevos.
             */
            string sql = "select IdcProducto, IdcFabricante from Productos " +
                     "where EAN13 = '" + pd.EAN13 + "' " +
                     "order by Status ASC, UMc ASC, IdcProducto Desc";
            /*
            Si existe,  se toma el primero:
            Se pondrá el valor de la variable existeProducto = Verdadero y se guardará en la variable CodigoProductoConnecta = Productos.IdcConnecta
            */
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
            {
                codigoProductoConnecta = db.GetFieldValue(cursor, 0);
                idcFabricante = db.GetFieldValue(cursor, 1);
                existeProducto = true;
            }
            cursor.Close();
            cursor = null;

            if (!existeProducto) 
            {
                //Si no existe, se comprobará si existe el código EAN en la tabla de unidades de medida de producto
                sql = "SELECT distinct Prod.IdcProducto, Prod.IdcFabricante, Prod.Status, Prod.UMc " +
                        "FROM ConvUMProducto as ConvPrd, Productos as Prod "+
                        "WHERE ConvPrd.EANDestino = '" + pd.EAN13 + "' " +
                        "And Prod.idcproducto=ConvPrd.idcProducto " +
                        "ORDER by Prod.Status ASC, Prod.UMc ASC,Prod.idcproducto Desc";          
                cursor = db.GetDataReader(sql);
                int cnt = 0;
                string codprod = null;

                //Si hay más de uno se toma el primero y se escribe un aviso
                while (cursor.Read())
                {
                    if (codprod == null)
                    {
                        //Guardamos el codigoProductoConnecta por si hay más de uno
                        codprod = db.GetFieldValue(cursor, 0);
                        codigoProductoConnecta = codprod;
                        idcFabricante = db.GetFieldValue(cursor, 1);
                    }
                    existeProducto = true; 
                    cnt++;
                }
                cursor.Close();
                cursor = null;

                if (codprod != null && cnt > 1) 
                {
                    //Más de uno, aviso. Ya habra cogido el primero de la lista.
                    //Globals.GetInstance().GetLog().Warning(agent, GetSipTypeName(), "Aviso en producto distribuidor. Producto " + pd.CodigoProducto + " – " + pd.Descripcion + " con más de un registro EAN13:" + pd.EAN13);
                    string myAlertMsg = "Aviso en producto distribuidor. Producto {0} – {1} con más de un registro EAN13: {2}";
                    Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "PRDI0003", myAlertMsg, pd.CodigoProducto, pd.Descripcion, pd.EAN13); 
                }
            }
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
    }

    /// <summary>
    /// Insertar histórico de productos
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="codigoProducto">código de producto</param>
    /// <param name="descripcion">descripción de producto</param>
    private void InsertarProductosAgentesHistorico(Database db, string codigoProducto, string descripcion, string descripcionNueva, string fechaInsercion, int diferencias)
    {        
        // Primero comprobamos si ya hay algun cambio anterior por el producto que estamos insertando
        string sql = "select 1 from ProductosAgentesHistorico " +
                     " where IdcAgente = " + agent + 
                     "   and CodigoProducto = " + db.ValueForSql(codigoProducto);
        string fechaInicial = null;
        if (!Utils.RecordExist(sql))
        {
            // Si no hay ningún cambio anterior, la fecha inicial será la fecha de alta del producto y la fecha final será la fecha actual
            fechaInicial = fechaInsercion;
        }
        else
        {
            // Si hay cambios anteriores, la fecha inicial será la fecha final del último cambio y la fecha final será la fecha actual
            fechaInicial = BuscaFechaFinalUltimoCambioDescripcion(db, codigoProducto);
        }

        sql = "Select 1 from ProductosAgentesHistorico "
            + " Where IdcAgente=" + agent
            + " And CodigoProducto=" + db.ValueForSql(codigoProducto)
            + " And FechaCambio=" + db.SysDate();            
        if (Utils.RecordExist(sql))
        {
            sql = "UPDATE ProductosAgentesHistorico SET "
                + "   Descripcion=" + db.ValueForSql(descripcion)
                + " , DescripcionNueva=" + db.ValueForSql(descripcionNueva)
                + " , FechaInicial=" + db.DateTimeForSql(fechaInicial)
                + " , FechaFinal=" + db.SysDate()
                + " , NumDiferencias=" + diferencias
                + " Where IdcAgente=" + agent
                + " And CodigoProducto=" + db.ValueForSql(codigoProducto)
                + " And FechaCambio=" + db.SysDate();
        }
        else
        {
            sql = "insert into ProductosAgentesHistorico (IdcAgente,CodigoProducto,FechaCambio,Descripcion,DescripcionNueva,FechaInicial,FechaFinal,NumDiferencias) " +
                            "values(" + agent
                            + "," + db.ValueForSql(codigoProducto)
                            + "," + db.SysDate()
                            + "," + db.ValueForSql(descripcion)
                            + "," + db.ValueForSql(descripcionNueva)
                            + "," + db.DateTimeForSql(fechaInicial)
                            + "," + db.SysDate()
                            + "," + diferencias
                            + ")";
        }
        Globals.GetInstance().GetDatabase().ExecuteSql(sql, agent, GetSipTypeName());

        string myAlertMsg = "Aviso en producto distribuidor. ATENCIÓN, posible reutilización de códigos. Se ha detectado cambio de descripción en el producto {0} del agente {1}.";
        Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "PRDI0008", myAlertMsg, codigoProducto, agent); 

    }

    /// <summary>
    /// Insertar producto inexistente
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="pd">registro</param>
    private bool InsertarProductoNoExistente(Database db, RecordProductosDistribuidor pd) 
    {
        bool resultado = false;
        string sql = "";

        //En caso de que el fabricante llegue vacío, comprobamos las relaciones que tiene el agente con sus fabricantes.
        //Si sólo tiene relación con un fabricante, obtenemos el código del fabricante según el distribuidor y lo asignamos a la variable pd.Fabricante.
        int count = 0;
        string codigoFab = string.Empty;
        if (Utils.IsBlankField(pd.Fabricante))
        {
            DbDataReader reader = null;
            sql = "SELECT Codigo " +
                  "  FROM ClasifInterAgentes  " +
                  " WHERE IdcAgenteOrigen = " + agent;
            reader = db.GetDataReader(sql);            
            while (reader.Read())
            {
                codigoFab = db.GetFieldValue(reader, 0);
                count++;
            }
            reader.Close();
            reader = null;
        }
        //Primero miramos si el producto ya existe, si ya existe lo insertamos como IndObsoleto='S',
        //si no existe lo actualizamos sin hacer caso al IndObsoleto
        sql = "select 1 from ProductosAgentesNoExistentes " +
                   "where IdcAgente = " + agent + 
                   " and CodigoProducto=" + db.ValueForSql(pd.CodigoProducto);
        if (!Utils.RecordExist(sql))
        {
            resultado = true; //para que se escriba en el log

            sql = "insert into ProductosAgentesNoExistentes (IdcAgente,CodigoProducto,Descripcion";
            if (!Utils.IsBlankField(pd.Status))
                sql += ",Status";
            sql += ",UnidadMedida,Clasificacion1,Clasificacion2" +
                        ",Clasificacion3,Clasificacion4,Clasificacion5,Clasificacion6,Clasificacion7" +
                        ",Clasificacion8,Clasificacion9,Clasificacion10,Clasificacion11,Clasificacion12,Clasificacion13,Clasificacion14,Jerarquia" +
                        ",EAN13,Fabricante,CodigoProdFab,IndObsoleto,FechaInsercion) " +
                        "values(" + agent + 
                        "," + db.ValueForSql(pd.CodigoProducto) +
                        "," + db.ValueForSql(Utils.StringTruncate(pd.Descripcion.Trim(),60));
            if (!Utils.IsBlankField(pd.Status))
                sql += "," + db.ValueForSql(pd.Status);
            sql += "," + db.ValueForSql(pd.UnidadMedida) +
                        "," + db.ValueForSql(pd.Clasificacion1) +
                        "," + db.ValueForSql(pd.Clasificacion2) +
                        "," + db.ValueForSql(pd.Clasificacion3) +
                        "," + db.ValueForSql(pd.Clasificacion4) +
                        "," + db.ValueForSql(pd.Clasificacion5) +
                        "," + db.ValueForSql(pd.Clasificacion6) +
                        "," + db.ValueForSql(pd.Clasificacion7) +
                        "," + db.ValueForSql(pd.Clasificacion8) +
                        "," + db.ValueForSql(pd.Clasificacion9) +
                        "," + db.ValueForSql(pd.Clasificacion10) +
                        "," + db.ValueForSql(pd.Clasificacion11) +
                        "," + db.ValueForSql(pd.Clasificacion12) +
                        "," + db.ValueForSql(pd.Clasificacion13) +
                        "," + db.ValueForSql(pd.Clasificacion14) +
                        "," + db.ValueForSql(pd.Jerarquia) +
                        "," + db.ValueForSql(pd.EAN13) +
                        "," + db.ValueForSql(((Utils.IsBlankField(pd.Fabricante)) && (count == 1) ? codigoFab.Trim() : pd.Fabricante)) + //"," + db.ValueForSql(pd.Fabricante) +
                        "," + db.ValueForSql(pd.CodigoProdFab) +
                        "," + db.ValueForSql("S") +
                        "," + db.SysDate() +
                        ")";
        }
        else
        {
            resultado = false; //para que no quede escrito en el log

            sql = "update ProductosAgentesNoExistentes SET " +
                " Descripcion=" + db.ValueForSql(Utils.StringTruncate(pd.Descripcion.Trim(),60)) + " ";
            if (!Utils.IsBlankField(pd.Status))
                sql += ",Status=" + db.ValueForSql(pd.Status) + " ";
            sql += ",UnidadMedida=" + db.ValueForSql(pd.UnidadMedida) + " " +
                    ",Clasificacion1=" + db.ValueForSql(pd.Clasificacion1) + " " +
                    ",Clasificacion2=" + db.ValueForSql(pd.Clasificacion2) + " " +
                    ",Clasificacion3=" + db.ValueForSql(pd.Clasificacion3) + " " +
                    ",Clasificacion4=" + db.ValueForSql(pd.Clasificacion4) + " " +
                    ",Clasificacion5=" + db.ValueForSql(pd.Clasificacion5) + " " +
                    ",Clasificacion6=" + db.ValueForSql(pd.Clasificacion6) + " " +
                    ",Clasificacion7=" + db.ValueForSql(pd.Clasificacion7) + " " +
                    ",Clasificacion8=" + db.ValueForSql(pd.Clasificacion8) + " " +
                    ",Clasificacion9=" + db.ValueForSql(pd.Clasificacion9) + " " +
                    ",Clasificacion10=" + db.ValueForSql(pd.Clasificacion10) + " " +
                    ",Clasificacion11=" + db.ValueForSql(pd.Clasificacion11) + " " +
                    ",Clasificacion12=" + db.ValueForSql(pd.Clasificacion12) + " " +
                    ",Clasificacion13=" + db.ValueForSql(pd.Clasificacion13) + " " +
                    ",Clasificacion14=" + db.ValueForSql(pd.Clasificacion14) + " " +
                    ",Jerarquia=" + db.ValueForSql(pd.Jerarquia) + " " +
                    ",EAN13=" + db.ValueForSql(pd.EAN13) + " " +
                    ",Fabricante=" + db.ValueForSql(((Utils.IsBlankField(pd.Fabricante)) && (count == 1) ? codigoFab.Trim() : pd.Fabricante)) + " " + //",Fabricante=" + db.ValueForSql(pd.Fabricante) + " " +
                    ",CodigoProdFab=" + db.ValueForSql(pd.CodigoProdFab) + " " +
                    ",FechaInsercion=" + db.SysDate() + " ";
            sql += " where IdcAgente = " + agent + " and CodigoProducto=" + db.ValueForSql(pd.CodigoProducto);
        }
        Globals.GetInstance().GetDatabase().ExecuteSql(sql, agent, GetSipTypeName());
        
        return resultado;
    }

    /// <summary>
    /// Eliminar producto inexistente
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="pd">registro</param>
    private int BorrarProductoNoExistente(Database db, RecordProductosDistribuidor pd)
    {
        string sql = "Delete from ProductosAgentesNoExistentes " +
                     "where IdcAgente = " + agent + " and CodigoProducto=" + db.ValueForSql(pd.CodigoProducto);

        return Globals.GetInstance().GetDatabase().ExecuteSql(sql, agent, GetSipTypeName());
    }

    /// <summary>
    /// Comprobar si existen facturas con errores
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="agente">agente</param>
    /// <param name="producto">producto</param>
    private bool ExistenConErrores(Database db, string agente, string producto, string tabla)
    {
        bool existen = false;
        DbDataReader cursor = null;
        try
        {
            string sql = "SELECT 1 from " + tabla +
                         " WHERE IdcAgente = " + agent + " and CodigoProducto=" + db.ValueForSql(producto);
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
                existen = true;            
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
        return existen;
    }

    /// <summary>
    /// Busca el código de producto del agente en base al codigo "interno" 
    /// </summary>
    /// <param name="pd">datos del registro</param>
    /// <returns>codigo producto o null si no lo encuentra</returns>
    private string BuscaProducto(RecordProductosDistribuidor pd) 
    {
      string producto = null;
      Database db=Globals.GetInstance().GetDatabase();
      DbDataReader cursor = null;
      try
      {
        string sql = "select IdcProducto from ProductosAgentes where IdcAgente = " + agent + " and Codigo = '" + pd.CodigoProducto+"'";
        cursor = db.GetDataReader(sql);
        if (cursor.Read())
        {
          producto = db.GetFieldValue(cursor,0);
        }
      }
      finally 
      {
        if (cursor != null)
          cursor.Close();
      }
      return producto;
    }

    /// <summary>
    /// Busca el código de producto del kit de distribuidor "ficticio" 
    /// </summary>    
    /// <returns>codigo producto o null si no lo encuentra</returns>
    private string BuscaProductoKitDistribuidor()
    {
        string producto = null;
        Database db = Globals.GetInstance().GetDatabase();
        DbDataReader cursor = null;
        try
        {
            string sql = "select IdcProducto from productos WHERE indEsKit = " + db.ValueForSql(Constants.KIT_DISTRIBUIDOR);
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
            {
                producto = db.GetFieldValue(cursor, 0);
            }
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
        return producto;
    }

    ///// <summary>
    ///// Busca la fecha alta del producto del agente 
    ///// </summary>
    ///// <param name="codigoProducto">código del producto</param>
    ///// <returns>fecha alta o null si no la encuentra</returns>
    //private string BuscaFechaAltaProducto(Database db, string codigoProducto)
    //{
    //    string fechaAlta = null;
    //    DbDataReader cursor = null;
    //    try
    //    {            
    //        string sql = "select FechaInsercion from ProductosAgentes where IdcAgente = " + agent + " and Codigo = '" + codigoProducto + "'";
    //        cursor = db.GetDataReader(sql);
    //        if (cursor.Read())
    //        {
    //            fechaAlta = cursor.GetDateTime(0).ToString(); // necesitamos fecha y hora                    
    //        }
    //    }
    //    finally
    //    {
    //        if (cursor != null)
    //            cursor.Close();
    //    }
    //    return fechaAlta;
    //}

    /// <summary>
    /// Busca la fecha final del último cambio de descripción (histórico)
    /// </summary>
    /// <param name="codigoProducto">código del producto</param>
    /// <returns>fecha último cambio o null si no la encuentra</returns>
    private string BuscaFechaFinalUltimoCambioDescripcion(Database db, string codigoProducto)
    {
        string fechaCambio = null;
        DbDataReader cursor = null;
        try
        {            
            string sql = "select max(FechaFinal) from ProductosAgentesHistorico where IdcAgente = " + agent + " and CodigoProducto = '" + codigoProducto + "'";
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
            {
                fechaCambio = cursor.GetDateTime(0).ToString(); // necesitamos fecha y hora                    
            }
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
        return fechaCambio;
    }

    /// <summary>
    /// Busca la descripción actual del producto
    /// </summary>
    /// <param name="pd">datos del registro</param>
    /// <returns>descripción del producto o null si no lo encuentra</returns>
    private void BuscaDescripcionFecha(Database db, string pCodigo, out string desc, out string fec)
    {
        DbDataReader cursor = null;
        try
        {
            desc = "";
            fec = "";
            string sql = "select Descripcion, FechaInsercion from ProductosAgentes where IdcAgente = " + agent + " and Codigo = '" + pCodigo + "'";
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
            {
                desc = db.GetFieldValue(cursor, 0).Trim();
                fec = cursor.GetDateTime(1).ToString(); 
            }
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
    }

    /// <summary>
    /// Busca el identificador Connect@ del fabricante del producto
    /// </summary>
    /// <param name="pd">datos del registro</param>
    /// <returns>identificdor del fabricante</returns>
    private string BuscaFabricante(RecordProductosDistribuidor pd, out string pCodigoFab)
    {
        string fabricante = null;
        Database db = Globals.GetInstance().GetDatabase();
        DbDataReader cursor = null;
        try
        {
            pCodigoFab = "";

            string sql = "Select IdcAgenteDestino From ClasifInterAgentes Where IdcAgenteOrigen = " + agent + " And Codigo = '" + pd.Fabricante + "'";
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
            {
                fabricante = db.GetFieldValue(cursor, 0);
            }
            if (String.IsNullOrEmpty(fabricante) && !String.IsNullOrEmpty(pd.Fabricante))
            {
                cursor.Close();
                //En segundo lugar buscamos por codigo alternativo però con like porque no spodemos encontrar varios códigos de fabricante separados por ";"
                //En este caso devemos devolver el código de fabricante principal para asignarselo o susutituir al código fabricante recibido para simular que hemos recibido este
                sql = "Select IdcAgenteDestino, Codigo From ClasifInterAgentes Where IdcAgenteOrigen = " + agent + " And CodigoAlt like '%" + pd.Fabricante + "%'";
                cursor = db.GetDataReader(sql);
                if (cursor.Read())
                {
                    fabricante = db.GetFieldValue(cursor, 0);
                    pCodigoFab = db.GetFieldValue(cursor, 1);
                }
            }
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
        return fabricante;
    }

    /// <summary>
    /// Determina si se utiliza el mismo código del fabricante
    /// </summary>
    /// <param name="pd">datos del registro</param>
    /// <returns>true si se utiliza</returns>
    private bool MismoCodigoFab(RecordProductosDistribuidor pd, string pIdcFabricante, out string prefijo)
    {
        string s = null;
        bool mismoCodigo = false;
        Database db = Globals.GetInstance().GetDatabase();
        DbDataReader cursor = null;
        prefijo = "";
        try
        {
            string sql = "select MismoCodigoFab, PrefijoMismoCodigoFab from ClasifInterAgentes where IdcAgenteOrigen = " + agent + " And IdcAgenteDestino = " + pIdcFabricante;
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
            {
                s = db.GetFieldValue(cursor, 0);
                if (s.ToUpper().Equals("S")) mismoCodigo = true;
                prefijo = db.GetFieldValue(cursor, 1).Trim();
            }
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
        return mismoCodigo;
    }

    /// <summary>
    /// Buscar la unidad de medida
    /// </summary>
    /// <param name="pd">datos del registro</param>
    /// <returns>unidad de medida o null si no la encuentra</returns>
    private string UnidadMedida(RecordProductosDistribuidor pd, string productConnecta)
    {
        return UnidadMedida(pd.UnidadMedida, pd.CodigoProducto, productConnecta);
    }    
    private string UnidadMedida(string unidadMedida, string codigoProducto, string productConnecta)
    {
        string um = null;
        Database db = Globals.GetInstance().GetDatabase();
        DbDataReader cursor = null;
        try
        {
            //Si no está definida...buscaremos la UM por defecto del producto
            string sql = "select UMc from Productos where IdcProducto = " + productConnecta + "";
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
            {
                um = db.GetFieldValue(cursor, 0);
            }
            if (um == null)
            {
                //Error                
                string myAlertMsg = "Error en producto distribuidor. Unidad de Medida : {0} no existe según el fabricante para el producto : {1}";
                string[] aValores = new string[] { unidadMedida, codigoProducto };
                string[] aClaves = new string[] { unidadMedida, agent };
                string[] aClavesExt = new string[] { unidadMedida, agent, codigoProducto };
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "PRDI0004", myAlertMsg, aValores, aClaves, aClavesExt);
                um = "";
            }
            /*
            if (Utils.IsBlankField(unidadMedida))
            {
                //Si no está definida...buscaremos la UM por defecto del producto
                string sql = "select UMc from Productos where IdcProducto = " + productConnecta + "";
                cursor = db.GetDataReader(sql);
                if (cursor.Read())
                {
                    um = db.GetFieldValue(cursor, 0);
                }
            }
            else 
            {
                //Verificar si existe conversión de unidad entre la UM del agente y la de Connecta
                string sql = "select UMc from UMAgente where IdcAgente = " + agent + " and UMAgente = '" + unidadMedida + "'";
                cursor = db.GetDataReader(sql);
                if (cursor.Read())
                {
                    //Existe la conversión
                    um = db.GetFieldValue(cursor, 0);
                }
                //Verificar si existe conversión de unidad entre la UM del agente y la de Connecta por fabricante
                int fab;
                if (um == null && int.TryParse(idcFabricante, out fab))
                {
                    sql = "select UMc from UMAgentePorFabricante where IdcAgente = " + agent + " and UMAgente = '" + unidadMedida + "' and IdcFabricante = " + idcFabricante;
                    cursor = db.GetDataReader(sql);
                    if (cursor.Read())
                    {
                        //Existe la conversión
                        um = db.GetFieldValue(cursor, 0);
                    }
                }                                
                cursor.Close();
                cursor = null;

                if(um==null) 
                {
                    //No existe la conversión, seleccionamos en la tabla UnidadesMedida por UMc = UnidadMedida.
                    sql = "select UMc from UnidadesMedida where UMc = '" + unidadMedida + "'";
                    cursor = db.GetDataReader(sql);
                    if (cursor.Read())
                    {
                        //Existe la conversión
                        um = db.GetFieldValue(cursor, 0);
                    }
                    else 
                    { 
                        //Error
                        //Globals.GetInstance().GetLog().Trace(Log.TYPE_ERROR, Log.LEVEL_HIGH, "Unidad de Medida no existe : " + pd.UnidadMedida + " para el producto : "+ pd.CodigoProducto, agent, GetSipTypeName());
                        string myAlertMsg = "Error en producto distribuidor. Unidad de Medida no existe : {0} para el producto : {1}";
                        string[] aValores = new string[] { unidadMedida, codigoProducto };
                        string[] aClaves = new string[] { unidadMedida, agent };
                        string[] aClavesExt = new string[] { unidadMedida, agent, codigoProducto };
                        Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "PRDI0004", myAlertMsg, aValores, aClaves, aClavesExt);                                                                         
                        um = "";
                    }
                }
            }*/
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
        return um;
    }      

    /// <summary>
    /// Verificar si el el fabricante del producto contra el que está alineado es diferente del fabricante que nos llega    
    /// </summary>
    /// <param name="pd">datos del registro</param>    
    private bool VerificarMismoFabricante(RecordProductosDistribuidor pd)
    {
        bool bOK = true;
        string fab = null;        
        Database db = Globals.GetInstance().GetDatabase();
        DbDataReader cursor = null;
        try
        {
            string sql = "select IdcFabricante from Productos where IdcProducto = " + codigoProductoConnecta;
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
                fab = db.GetFieldValue(cursor, 0);                            
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
        if (fab != idcFabricante)
        {
            bOK = false;
            
            string myAlertMsg = "Aviso en producto distribuidor. El producto {0} - {1} del agente {2} y del fabricante {3} ya está previamente alineado contra un producto del fabricante {4}.";
            string fabRecibido = "";
            if (idcFabricante == null)
                fabRecibido = "<vacío>";
            else
                fabRecibido = string.IsNullOrEmpty(idcFabricante.Trim()) ? "<vacío>" : idcFabricante;
            string[] aValores = new string[] { pd.CodigoProducto, pd.Descripcion, agent, fabRecibido, fab };
            string[] aClaves = new string[] { pd.CodigoProducto, fabRecibido, agent };
            string[] aClavesExt = new string[] { pd.CodigoProducto, fabRecibido, agent, pd.Descripcion, fab };
            Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "PRDI0015", myAlertMsg, aValores, aClaves, aClavesExt);                                        
        }
        return bOK;
    }

    /// <summary>
    /// Verifica si el producto que nos llega por interfase(que debe existir) efectivamente continua correspondiéndose 
    //      con el mismo producto del fabricante
    /// </summary>
    /// <param name="pd">datos del registro</param>
    /// <param name="codigoProductoConnecta">código de producto Connecta</param>
    /// <param name="mismoCodigoProductoFabricante">true si el agente utiliza el mismo cod.producto fabricante</param>
    private void VerificarProducto(RecordProductosDistribuidor pd)
    {
        bool verificacionOK = false, alternativaPosible = false;

        string sql;
        //Comprobar por CodigoProductoFabricante y después por EAN13 

        //Si CodigoProdFab y Fabricante no son ni blancos ni nulos...
        if (!Utils.IsBlankField(pd.CodigoProdFab) && !Utils.IsBlankField(pd.Fabricante)) 
        { 
            sql = "Select PA.IdcProducto From ProductosAgentes PA, ClasifInterAgentes CI "+
               "Where CI.IdcagenteOrigen= "+agent+" and "+
               "CI.Codigo='"+pd.Fabricante+"' and "+
               "CI.IdcAgenteDestino=PA.IdcAgente "+
               "And PA.Codigo = '"+pd.CodigoProdFab+"' "+
               "And PA.IdcProducto = "+codigoProductoConnecta+" ";
            verificacionOK = Utils.RecordExist(sql);

            //SVO 20-11-2013: Creo que no es necesario esto porque ya se està haciendo la misma verificación mas abajo
            ////////Si no existe, de momento solo significa que por Código Producto no se ha encontrado, 
            ////////debe continuarse la verificación, pero miramos si el sistema podría asignar otro distinto:
            //////sql = "Select count(*) " +
            //////  "From ProductosAgentes PA, ClasifInterAgentes CI " +
            //////  "Where CI.IdcagenteOrigen= " + agent + " and " +
            //////  "CI.Codigo='" + pd.Fabricante + "' and " +
            //////  "CI.IdcAgenteDestino=PA.IdcAgente and " +
            //////  "PA.Codigo = '" + pd.CodigoProdFab + "'";
            //////alternativaPosible = Utils.HasRecords(sql);
        }

        //SVO: 03-03-2011: Hemos decidido no hacer el alineamiento por EAN
        //Por EAN13
        //if (!Utils.IsBlankField(pd.EAN13)) 
        //{
        //    //Verificar si existe en la tabla de productos
        //    sql = "SELECT IdcProducto from PRODUCTOS where EAN13='" + pd.EAN13 + "' and idcproducto=" + codigoProductoConnecta;
        //    if (Utils.RecordExist(sql))
        //    {
        //        verificacionOK = true;
        //    }
        //    else 
        //    {
        //        //Si no existe, miramos los EANS de las Unidades de Medida
        //        sql = "SELECT IdcProducto from CONVUMPRODUCTO where EANdestino='" + pd.EAN13 + "' and idcproducto=" + codigoProductoConnecta;
        //        if (Utils.RecordExist(sql))
        //        {
        //            verificacionOK = true;
        //        }
        //        else 
        //        { 
        //            //Si no existe, si podríamos haber asignado producto por EANS:
        //            sql = "Select count(*) From Productos Where EAN13='" + pd.EAN13 + "' ";
        //            if (!Utils.IsBlankField(idcFabricante))
        //                sql += " And IdcFabricante=" + idcFabricante; //ATENCIÓN: añadido el 10-01-2011 para reforzar la verificacion (antes solo verificaba por ean)
        //            if (Utils.HasRecords(sql))
        //                alternativaPosible = true;
        //            else
        //            {
        //                // Si no, miramos en los EANS de las unidades de medida:
        //                sql = "SELECT count(*) from CONVUMPRODUCTO where EANdestino='" + pd.EAN13 + "' ";
        //                if (!Utils.IsBlankField(idcFabricante))
        //                    sql += " And IdcAgente=" + idcFabricante; //ATENCIÓN: añadido el 10-01-2011 para reforzar la verificacion (antes solo verificaba por ean)
        //                if (Utils.HasRecords(sql))
        //                    alternativaPosible = true;
        //            }
        //        }
        //    }
        //}

        //Finalmente, si la comprobación NO HA SIDO SATISFACTORIA, comprobamos por código de producto del distribuidor
        if (!verificacionOK) 
        {
            if (mismoCodigoProductoFabricante)
            {   //punto 2.1.3.3
                //comprobamos por código de producto del distribuidor
                sql = "Select PA.IdcProducto " +
                        "From ProductosAgentes PA, ClasifInterAgentes CI " +
                        "Where CI.IdcagenteOrigen=" + agent + " and " +
                        "CI.Codigo='" + pd.Fabricante + "' and " +
                        "CI.IdcAgenteDestino=PA.IdcAgente and " +
                        "PA.Codigo = '" + pd.CodigoProducto + "' And PA.IdcProducto = " + codigoProductoConnecta;
                if (Utils.RecordExist(sql))
                    verificacionOK = true;
                if (!verificacionOK)
                {
                    if (!Utils.IsBlankField(prefijoMismoCodigoProductoFabricante))
                    {
                        string codBusqueda = pd.CodigoProducto;
                        if (codBusqueda.StartsWith(prefijoMismoCodigoProductoFabricante))
                        {
                            codBusqueda = codBusqueda.Substring(prefijoMismoCodigoProductoFabricante.Length);
                        }
                        sql = "Select PA.IdcProducto " +
                                "From ProductosAgentes PA, ClasifInterAgentes CI " +
                                "Where CI.IdcagenteOrigen=" + agent + " and " +
                                "CI.Codigo='" + pd.Fabricante + "' and " +
                                "CI.IdcAgenteDestino=PA.IdcAgente and " +
                                "PA.Codigo = '" + codBusqueda + "' And PA.IdcProducto = " + codigoProductoConnecta;
                        if (Utils.RecordExist(sql))
                            verificacionOK = true;
                    }
                }
            }
            if (!verificacionOK)
            {
                //Miramos si el sistema podría asignar otro distinto que sea del mismo fabricante
                sql = "Select count(*) " +
                    "From ProductosAgentes PA, ClasifInterAgentes CI " +
                    "Where CI.IdcagenteOrigen= " + agent + " and " +
                    "CI.Codigo='" + pd.Fabricante + "' and " +
                    "CI.IdcAgenteDestino=PA.IdcAgente  " +
                    "And PA.Codigo = '" + pd.CodigoProducto + "'";
                if (Utils.HasRecords(sql))
                    alternativaPosible = true;
            }
        }

        //si la comprobación NO ha sido satisfactoria (VerificacionOK=FALSE) Y existe una alternativa (AlternativaPosible=CIERTO), entonces:
        if (!verificacionOK && alternativaPosible)
        {
            string myAlertMsg = "Aviso en producto distribuidor. El producto {0} - {1} del agente {2} ahora se corresponde con otro producto del fabricante {3}.";
            Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "PRDI0005", myAlertMsg, pd.CodigoProducto, pd.Descripcion, agent, pd.Fabricante); 
        }
    }

    /// <summary>
    /// Validar el formato de un registro
    /// </summary>
    /// <param name="rec">registro</param>
    public bool ValidateMaster(CommonRecord rec)
    {
        RecordProductosDistribuidor pd = (RecordProductosDistribuidor)rec;
        Log2 log = Globals.GetInstance().GetLog2();

        if (Utils.IsBlankField(pd.CodigoProducto))
        {
            string myAlertMsg = "Validación formato. Error en producto distribuidor. Código de producto vacío.";
            log.Trace(agent, GetSipTypeName(), "PRDI0006", myAlertMsg); 
            return false;
        }

        //if (Utils.IsBlankField(pd.Fabricante) && Utils.IsBlankField(pd.CodigoProdFab) && Utils.IsBlankField(pd.EAN13))
        //{
        //    log.DetailedError(agent, GetSipTypeName(), "Validación formato. Error en producto distribuidor. Código fabricante y código producto del fabricante y código ean13 vacíos para el producto " + pd.CodigoProducto + ".");
        //    return false;
        //}

        if (!Utils.IsBlankField(pd.CodigoProdFab))
        {
            if (Utils.IsBlankField(pd.Fabricante))
            {
                string myAlertMsg = "Validación formato. Error en producto distribuidor. Fabricante del producto vacío para el producto {0}.";
                log.Trace(agent, GetSipTypeName(), "PRDI0007", myAlertMsg, pd.CodigoProducto); 
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Validar el formato de un registro
    /// </summary>
    /// <param name="rec">registro</param>
    public bool ValidateSlave(CommonRecord rec)
    {
        RecordProductosKitDistribuidor pdkit = (RecordProductosKitDistribuidor)rec;
        Log2 log = Globals.GetInstance().GetLog2();
        double cantAux;

        if (Utils.IsBlankField(pdkit.CodigoProductoKit))
        {
            string myAlertMsg = "Validación formato. Error en producto kit distribuidor. Código de producto kit vacío.";
            log.Trace(agent, GetSipTypeName(), "PRDI0011", myAlertMsg);
            return false;
        }

        if (Utils.IsBlankField(pdkit.CodigoProductoComp))
        {
            string myAlertMsg = "Validación formato. Error en producto kit distribuidor. Código de producto componente vacío para el producto kit {0}.";
            log.Trace(agent, GetSipTypeName(), "PRDI0012", myAlertMsg, pdkit.CodigoProductoKit);
            return false;
        }

        //Comprobamos la cantidad recibida        
        if (Utils.IsBlankField(pdkit.CantidadComp))
        {
            string myAlertMsg = "Validación formato. Error en producto kit distribuidor. Cantidad vacía para el componente {0}.";
            log.Trace(agent, GetSipTypeName(), "PRDI0013", myAlertMsg, pdkit.CodigoProductoComp);
            return false;
        }
        if (!Utils.IsBlankField(pdkit.CantidadComp))
        {
            if (!Double.TryParse(pdkit.CantidadComp, out cantAux))
            {
                string myAlertMsg = "Validación formato. Error en producto kit distribuidor. Cantidad {0} no es numérico para el componente {1}.";
                log.Trace(agent, GetSipTypeName(), "PRDI0014", myAlertMsg, pdkit.CantidadComp, pdkit.CodigoProductoComp);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Procesar un fichero de manera masiva
    /// </summary>
    /// <param name="rec">registro</param>
    public void ProcessBulk(string filename, CommonRecord rec)
    {
    }
  }
}
