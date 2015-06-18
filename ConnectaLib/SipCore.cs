using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ConnectaLib
{
  /// <summary>
  /// Clase principal de gestión de "Sips". Básicamente permite lanzar
  /// Sips en modo manual / automático y contiene procesos comunes a la
  /// ejecución de cualquier sip. La lógica de negocio depende del tipo de sip
  /// y se delega en cada una de las clases Sip (ej. SipAlbaran, SipProducto, etc.)
  /// </summary>
  public class SipCore
  {
    /// <summary>
    /// Constructor
    /// </summary>
    public SipCore() 
    {
    }

    /// <summary>
    /// Iniciar proceso automático
    /// </summary>
    /// <param name="msgId">id de mensaje</param>
    /// <param name="msgData">datos del mensaje</param>
    /// <param name="sipTypeName">nombre de tipo de sip</param>
    /// <param name="agent">agente</param>
    public void StartAuto(string msgId, string msgData, string sipTypeName, string agent) 
    {
        SipQueue task = new SipQueue();
        Globals g = Globals.GetNewInstance();   //Obtener una nueva instancia. No aprovechar la existente porque
                                                //hay problemas al estar involucradas transacciones...
        Database db = g.GetDatabase();
        try
        {
            Globals.SetWorkingInstance(g);

            //En msgData, el nombre del fichero (se supone que existe).
            //Decodificar a través del sipTypeName, el tipo de SIP
            //Cambiar el estado del mensaje, timestamp de inicio, etc.
            task.Processing(db, msgId);

            //Ajustar el identificador global del mensaje
            //En la clase de log, se recuperará para añadir los registros 
            //con este identificador.
            g.SetTaskId(msgId);

            if (sipTypeName.ToLower().StartsWith("sipout"))
            {
                //6 paràmetros en msgData (fileformat, filename, fromdate, todate, dealer, filter)
                string s1 = "", s2 = "", s3 = "", s4 = "", s5 = "", s6 = "", s7 = "", s8 = "", s9 = "";
                
                string token = "";
                int contador = 1;
                StringTokenizer st = new StringTokenizer(msgData,";&;");
                while (st.HasMoreTokens())
                {
                    //añadir el operador AND
                    token = st.NextToken();
                    if (contador == 1) s1 = token;
                    if (contador == 2) s2 = token;
                    if (contador == 3) s3 = token;
                    if (contador == 4) s4 = token;
                    if (contador == 5) s5 = token;
                    if (contador == 6) s6 = token;
                    if (contador == 7) s7 = token;
                    if (contador == 8) s8 = token;
                    if (contador == 9) s9 = token;
                    contador++;
                }

                string[] argumentos = { s1, s2, s3, s4, s5, s6, s7, s8, s9 };

                //Procesar este mensaje
                Start(g, sipTypeName, agent, "", argumentos);
            }
            else if (sipTypeName.ToLower().StartsWith("sipupd"))
            {
                //8 paràmetros en msgData (fromdate, todate, dealer, filter, classifier, lockcode, productcode, provider)
                string s1 = "", s2 = "", s3 = "", s4 = "", s5 = "", s6 = "", s7 = "", s8 = "";

                string token = "";
                int contador = 1;
                StringTokenizer st = new StringTokenizer(msgData, ";&;");
                while (st.HasMoreTokens())
                {
                    //añadir el operador AND
                    token = st.NextToken();
                    if (contador == 1) s1 = token;
                    if (contador == 2) s2 = token;
                    if (contador == 3) s3 = token;
                    if (contador == 4) s4 = token;
                    if (contador == 5) s5 = token;
                    if (contador == 6) s6 = token;
                    if (contador == 7) s7 = token;
                    if (contador == 8) s8 = token;
                    contador++;
                }

                string[] argumentos = { s1, s2, s3, s4, s5, s6, s7, s8};

                //Procesar este mensaje
                Start(g, sipTypeName, agent, "", argumentos);
            }
            else if (sipTypeName.ToLower().StartsWith("sipexe"))
            {
                //8 paràmetros en msgData (fromdate, todate, dealer, filter, classifier, lockcode, productcode, provider)
                string s1 = "", s2 = "", s3 = "", s4 = "", s5 = "", s6 = "", s7 = "", s8 = "";

                string token = "";
                int contador = 1;
                StringTokenizer st = new StringTokenizer(msgData, ";&;");
                while (st.HasMoreTokens())
                {
                    //añadir el operador AND
                    token = st.NextToken();
                    if (contador == 1) s1 = token;
                    if (contador == 2) s2 = token;
                    if (contador == 3) s3 = token;
                    if (contador == 4) s4 = token;
                    if (contador == 5) s5 = token;
                    if (contador == 6) s6 = token;
                    if (contador == 7) s7 = token;
                    if (contador == 8) s8 = token;
                    contador++;
                }

                string[] argumentos = { s1, s2, s3, s4, s5, s6, s7, s8 };

                //Procesar este mensaje
                Start(g, sipTypeName, agent, "", argumentos);
            }
            else if (sipTypeName.ToLower().StartsWith("sipdel"))
            {
                //8 paràmetros en msgData (provider, fromdate, todate, filter, fromupddate, toupddate, frominsdate, toinsdate)
                string s1 = "", s2 = "", s3 = "", s4 = "", s5 = "", s6 = "", s7 = "", s8 = "";

                string token = "";
                int contador = 1;
                StringTokenizer st = new StringTokenizer(msgData, ";&;");
                while (st.HasMoreTokens())
                {
                    //añadir el operador AND
                    token = st.NextToken();
                    if (contador == 1) s1 = token;
                    if (contador == 2) s2 = token;
                    if (contador == 3) s3 = token;
                    if (contador == 4) s4 = token;
                    if (contador == 5) s5 = token;
                    if (contador == 6) s6 = token;
                    if (contador == 7) s7 = token;
                    if (contador == 8) s8 = token;
                    contador++;
                }

                string[] argumentos = { s1, s2, s3, s4, s5, s6, s7, s8 };

                //Procesar este mensaje
                Start(g, sipTypeName, agent, "", argumentos);
            }
            else
            {
                //Procesar este mensaje
                Start(g, sipTypeName, agent, msgData, null);
            }

            //Si el proceso es correcto, ponerlo en estado "finalizado"
            task.Completed(db, msgId);
        }
        catch (Exception e)
        {
            g.GetLog2().Error(agent, sipTypeName, e);
            task.Error(db, msgId);
        }
        finally 
        {
          if (g != null) g.Close();
          Globals.SetWorkingInstance(null);
        }
    }

    /// <summary>
    /// Obtener una instancia de sip de entrada
    /// </summary>
    /// <param name="siptype">tipo de sip de entrada</param>
    /// <param name="agent">agente</param>
    /// <returns>instancia de sip de entrada</returns>
    public ISipInInterface GetSipIn(string siptype, string agent) 
    {
        //Delega en cada SIP el proceso del fichero dado que son métodos abstractos y
        //están sobreescritos (override) en cada una de las implementaciones de los Sip.
        //Ver SipInProductosDistribuidores,SipInComerciales,etc.

        string typeLower = siptype.ToLower();
        /**
        *  Ejemplos:
        ·	sipInComercialesDistribuidor
        ·	sipInRutasDistribuidor
        ·	sipInProductosDistribuidor
        ·	sipInProductosFabricante
        ·	sipInClientesFinalesDistribuidor
        ·	sipInClientesFinalesFabricante
        */
        ISipInInterface mySip = null;

        if (typeLower.Equals(SipInProductosDistribuidor.ID_SIP.ToLower()))
            mySip = new SipInProductosDistribuidor(agent);
        else if (typeLower.Equals(SipInClientesFinalesDistribuidor.ID_SIP.ToLower()))
            mySip = new SipInClientesFinalesDistribuidor(agent);
        else if (typeLower.Equals(SipInFacturasDistribuidor.ID_SIP.ToLower()))
            mySip = new SipInFacturasDistribuidor(agent);

        else if (typeLower.Equals(SipInProductosFabricante.ID_SIP.ToLower()))
            mySip = new SipInProductosFabricante(agent);          

        return mySip;
    }

    /// <summary>
    /// Obtener una instancia de sip de salida
    /// </summary>
    /// <param name="siptype">tipo de sip de salida</param>
    /// <param name="agent">agente</param>
    /// <returns>instancia de sip de salida</returns>
    public ISipOutInterface GetSipOut(string siptype, string agent)
    {
        //Delega en cada SIP el proceso del fichero dado que son métodos abstractos y
        //están sobreescritos (override) en cada una de las implementaciones de los Sip.
        //Ver sipOutAlbaranesParaFabricante,etc.

        string typeLower = siptype.ToLower();
        /**
        *  Ejemplos:
        ·	sipOutPedidosFabricante
        */
        ISipOutInterface mySip = null;        
        return mySip;
    }

    /// <summary>
    /// Obtener una instancia de sip de actualización
    /// </summary>
    /// <param name="siptype">tipo de sip de actualización</param>
    /// <param name="agent">agente</param>
    /// <returns>instancia de sip de salida</returns>
    public ISipUpdInterface GetSipUpd(string siptype, string agent)
    {
        //Delega en cada SIP el proceso del fichero dado que son métodos abstractos y
        //están sobreescritos (override) en cada una de las implementaciones de los Sip.

        string typeLower = siptype.ToLower();

        ISipUpdInterface mySip = null;        

        return mySip;
    }

    /// <summary>
    /// Obtener una instancia de sip de ejecución
    /// </summary>
    /// <param name="siptype">tipo de sip de ejecución</param>
    /// <param name="agent">agente</param>
    /// <returns>instancia de sip de salida</returns>
    public ISipExeInterface GetSipExe(string siptype, string agent)
    {
        //Delega en cada SIP el proceso del fichero dado que son métodos abstractos y
        //están sobreescritos (override) en cada una de las implementaciones de los Sip.

        string typeLower = siptype.ToLower();

        ISipExeInterface mySip = null;

        return mySip;
    }

    /// <summary>
    /// Obtener una instancia de sip de eliminación
    /// </summary>
    /// <param name="siptype">tipo de sip de eliminación</param>
    /// <param name="agent">agente</param>
    /// <returns>instancia de sip de salida</returns>
    public ISipDelInterface GetSipDel(string siptype, string agent)
    {
        //Delega en cada SIP el proceso del fichero dado que son métodos abstractos y
        //están sobreescritos (override) en cada una de las implementaciones de los Sip.

        string typeLower = siptype.ToLower();

        ISipDelInterface mySip = null;        

        return mySip;
    }

    public string ConvertSipTypeToSipTypeName(string sipTypeOrig)
    {
        if (sipTypeOrig.ToLower() == "sipinrutas")
            return "Rutas.In";
        if (sipTypeOrig.ToLower() == "sipincomerciales")
            return "Comerciales.In";
        if (sipTypeOrig.ToLower() == "sipinclasificacionesdistribuidor")
            return "Distribuidor.Clasificaciones.In";
        if (sipTypeOrig.ToLower() == "sipinproductosdistribuidor")
            return "Distribuidor.Productos.In";
        if (sipTypeOrig.ToLower() == "sipinclientesfinalesdistribuidor")
            return "Distribuidor.ClientesFinales.In";
        if (sipTypeOrig.ToLower() == "sipinfacturasdistribuidor")
            return "Distribuidor.Facturas.In";
        if (sipTypeOrig.ToLower() == "sipinentregasdistribuidor")
            return "Distribuidor.Entregas.In";
        if (sipTypeOrig.ToLower() == "sipinpedidosdistribuidor")
            return "Distribuidor.Pedidos.In";
        if (sipTypeOrig.ToLower() == "sipinalbaranesdistribuidor")
            return "Distribuidor.Albaranes.In";
        if (sipTypeOrig.ToLower() == "sipinreaprovisionamientosdistribuidor")
            return "Distribuidor.Reaprovisionamientos.In";
        if (sipTypeOrig.ToLower() == "sipinstocksdistribuidor")
            return "Distribuidor.Stocks.In";
        if (sipTypeOrig.ToLower() == "sipinstocksclientedistribuidor")
            return "Distribuidor.StocksCliente.In";
        if (sipTypeOrig.ToLower() == "sipinnumericarutadistribuidor")
            return "Distribuidor.NumericaRuta.In";
        if (sipTypeOrig.ToLower() == "sipinnumericacodigopostaldistribuidor")
            return "Distribuidor.NumericaCodigoPostal.In";
        if (sipTypeOrig.ToLower() == "sipincontrolenviosdistribuidor")
            return "Distribuidor.ControlEnvios.In";

        if (sipTypeOrig.ToLower() == "sipinusuariosfabricante")
            return "Fabricante.Usuarios.In";
        if (sipTypeOrig.ToLower() == "sipindistribuidoresfabricante")
            return "Fabricante.Distribuidores.In";
        if (sipTypeOrig.ToLower() == "sipinempresasfabricante")
            return "Fabricante.Empresas.In";
        if (sipTypeOrig.ToLower() == "sipinclasificacionesfabricante")
            return "Fabricante.Clasificaciones.In";
        if (sipTypeOrig.ToLower() == "sipinproductosfabricante")
            return "Fabricante.Productos.In";
        if (sipTypeOrig.ToLower() == "sipinproductosumfabricante")
            return "Fabricante.ProductosUM.In";
        if (sipTypeOrig.ToLower() == "sipinproductoscompetidoresfabricante")
            return "Fabricante.ProductosCompetidores.In";
        if (sipTypeOrig.ToLower() == "sipinclientesfinalesfabricante")
            return "Fabricante.ClientesFinales.In";
        if (sipTypeOrig.ToLower() == "sipinhorariosfabricante")
            return "Fabricante.Horarios.In";
        if (sipTypeOrig.ToLower() == "sipinjerarquiasfabricante")
            return "Fabricante.Jerarquias.In";
        if (sipTypeOrig.ToLower() == "sipinsurtidosfabricante")
            return "Fabricante.Surtidos.In";
        if (sipTypeOrig.ToLower() == "sipinpreciosfabricante")
            return "Fabricante.Precios.In";
        if (sipTypeOrig.ToLower() == "sipinsellindistribuidoresfabricante")
            return "Fabricante.SellInDistribuidores.In";
        if (sipTypeOrig.ToLower() == "sipinpeticionesfabricante")
            return "Fabricante.Peticiones.In";
        if (sipTypeOrig.ToLower() == "sipinreaprovisionamientosfabricante")
            return "Fabricante.Reaprovisionamientos.In";
        if (sipTypeOrig.ToLower() == "sipinfacturasfabricante")
            return "Fabricante.Facturas.In";
        if (sipTypeOrig.ToLower() == "sipinventainternafabricante")
            return "Fabricante.VentaInterna.In";
		if (sipTypeOrig.ToLower() == "sipinposicionamientopdvfabricante")
            return "Fabricante.PosicionamientoPdV.In";
		if (sipTypeOrig.ToLower() == "sipininfomercadomarketer")
            return "Marketer.InfoMercado.In";
        if (sipTypeOrig.ToLower() == "sipintablasagente")
            return "Agente.Tablas.In";
        if (sipTypeOrig.ToLower() == "SipInPromocionesAgente")
            return "Agente.Promociones.In";
        if (sipTypeOrig.ToLower() == "SipInActividadesAgente")
            return "Agente.Actividades.In";
        if (sipTypeOrig.ToLower() == "SipInClientesData")
            return "ClientesData.In";
        if (sipTypeOrig.ToLower() == "SipInActividadesData")
            return "ActividadesData.In";        

        if (sipTypeOrig.ToLower() == "sipoutalbaranesparafabricante")
            return "Fabricante.Albaranes.Out";
        if (sipTypeOrig.ToLower() == "sipoutreaprovisionamientosparafabricante")
            return "Fabricante.Reaprovisionamientos.Out";
        if (sipTypeOrig.ToLower() == "sipoutfacturasparafabricante")
            return "Fabricante.Facturas.Out";
        if (sipTypeOrig.ToLower() == "sipoutfacturasextendedparafabricante")
            return "Fabricante.FacturasExtended.Out";
        if (sipTypeOrig.ToLower() == "sipoutentregasparafabricante")
            return "Fabricante.Entregas.Out";
        if (sipTypeOrig.ToLower() == "sipoutstocksparafabricante")
            return "Fabricante.Stocks.Out";
        if (sipTypeOrig.ToLower() == "sipoutpedidosparafabricante")
            return "Fabricante.Pedidos.Out";
        if (sipTypeOrig.ToLower() == "sipoutclientesparafabricante")
            return "Fabricante.Clientes.Out";
        if (sipTypeOrig.ToLower() == "sipoutclientesextendedparafabricante")
            return "Fabricante.ClientesExtended.Out";
        if (sipTypeOrig.ToLower() == "sipoutclientesalbaranesparafabricante")
            return "Fabricante.ClientesAlbaranes.Out";
        if (sipTypeOrig.ToLower() == "sipoutventasacumclienteparafabricante")
            return "Fabricante.VentasAcumCliente.Out";
        if (sipTypeOrig.ToLower() == "sipoutventasacummesparafabricante")
            return "Fabricante.VentasAcumMes.Out";
        if (sipTypeOrig.ToLower() == "sipoutliquidacionesacuerdosparafabricante")
            return "Fabricante.LiquidacionesAcuerdos.Out";
        if (sipTypeOrig.ToLower() == "sipoutclasificacionesparafabricante")
            return "Fabricante.Clasificaciones.Out";
        if (sipTypeOrig.ToLower() == "sipoutpersonalizadoparaagente")
            return "Agente.Personalizado.Out";

        if (sipTypeOrig.ToLower() == "sipupdenriquecerclientesfabricante")
            return "Fabricante.EnriquecerClientes.Upd";
        if (sipTypeOrig.ToLower() == "sipupdrefrescarbloqueosalbaranesfabricante")
            return "Fabricante.RefrescarBloqueosAlbaranes.Upd";
        if (sipTypeOrig.ToLower() == "sipUpdRefrescarAlineacionesProductosFabricante")
            return "Fabricante.AlineacionesProductos.Upd";
        if (sipTypeOrig.ToLower() == "sipUpdRefrescarProductosClientesPosicionamientoPdVFabricante")
            return "Fabricante.ProductosClientesPosicionamientoPdV.Upd";
        if (sipTypeOrig.ToLower() == "sipUpdRefrescarProductosClientesVentaInternaFabricante")
            return "Fabricante.ProductosClientesVentaInterna.Upd";
        if (sipTypeOrig.ToLower() == "sipUpdRefrescarProductosClientesInfoMercadoMarketer")
            return "Marketer.ProductosClientesInfoMercado.Upd";        
        if (sipTypeOrig.ToLower() == "sipupdprocesarsellindistribuidoresfabricante")
            return "Fabricante.ProcesarSellInDistribuidores.Upd";        
        if (sipTypeOrig.ToLower() == "sipupdprocesaralbaranesdistribuidor")
            return "Distribuidor.ProcesarAlbaranes.Upd";
        if (sipTypeOrig.ToLower() == "sipupdprocesarstocksdistribuidor")
            return "Distribuidor.ProcesarStocks.Upd";
        if (sipTypeOrig.ToLower() == "sipupdprocesarstocksclientedistribuidor")
            return "Distribuidor.ProcesarStocksCliente.Upd";
        if (sipTypeOrig.ToLower() == "sipupdprocesarfacturasdistribuidor")
            return "Distribuidor.ProcesarFacturas.Upd";
        if (sipTypeOrig.ToLower() == "sipupdprocesarentregasdistribuidor")
            return "Distribuidor.ProcesarEntregas.Upd";
        if (sipTypeOrig.ToLower() == "sipupdpersonalizadoagente")
            return "Agente.Personalizado.Upd";

        if (sipTypeOrig.ToLower() == "sipexepersonalizadoagente")
            return "Agente.Personalizado.Exe";
        
        if (sipTypeOrig.ToLower() == "sipdelfacturasdistribuidor")
            return "Distribuidor.Facturas.Del";
        if (sipTypeOrig.ToLower() == "sipdelentregasdistribuidor")
            return "Distribuidor.Entregas.Del";
        if (sipTypeOrig.ToLower() == "sipdelstocksdistribuidor")
            return "Distribuidor.Stocks.Del";
        if (sipTypeOrig.ToLower() == "sipdelalbaranesdistribuidor")
            return "Distribuidor.Albaranes.Del";
        if (sipTypeOrig.ToLower() == "sipdelpedidosdistribuidor")
            return "Distribuidor.Pedidos.Del";
        if (sipTypeOrig.ToLower() == "sipdelreaprovisionamientosdistribuidor")
            return "Distribuidor.Reaprovisionamientos.Del";
        if (sipTypeOrig.ToLower() == "sipdelliquidacionesacuerdosfabricante")
            return "Fabricante.LiquidacionesAcuerdos.Del";
        if (sipTypeOrig.ToLower() == "sipdelalertlogagente")
            return "Agente.Alertlog.Del";
        if (sipTypeOrig.ToLower() == "sipdelhistorificacionagente")
            return "Agente.Historificacion.Del";
        if (sipTypeOrig.ToLower() == "sipdelusuariosagente")
            return "Agente.Usuarios.Del";
        if (sipTypeOrig.ToLower() == "sipdelpersonalizadoagente")
            return "Agente.Personalizado.Del";

        return sipTypeOrig;
    }

    /// <summary>
    /// Iniciar proceso manual
    /// </summary>
    /// <param name="g">instancia de globals</param>
    /// <param name="siptype">tipo de sip</param>
    /// <param name="agent">agente</param>
    /// <param name="filename">nombre de fichero (opcional)</param>
    /// <param name="args">otros argumentos (opcional)</param> 
    public void Start(Globals g, string siptype, string agent, string filename, string[]args) 
    {
        if (siptype.ToLower().StartsWith("sipout"))
        {
            //SIP de salida
            ISipOutInterface sipOut = GetSipOut(siptype, agent);
            if (sipOut == null)
                throw new Exception("Tipo de SIP incorrecto >> " + siptype);
            
            sipOut.Process(args);
            sipOut.PostProcess(agent);
        }
        else if (siptype.ToLower().StartsWith("sipupd"))
        {
            //SIP de actualización
            ISipUpdInterface sipUpd = GetSipUpd(siptype, agent);
            if (sipUpd == null)
                throw new Exception("Tipo de SIP incorrecto >> " + siptype);

            sipUpd.PreProcess(agent);
            sipUpd.Process(args);
            sipUpd.PostProcess(agent);
        }
        else if (siptype.ToLower().StartsWith("sipexe"))
        {
            //SIP de ejecución
            ISipExeInterface sipUpd = GetSipExe(siptype, agent);
            if (sipUpd == null)
                throw new Exception("Tipo de SIP incorrecto >> " + siptype);

            sipUpd.PreProcess(agent);
            sipUpd.Process(args);
            sipUpd.PostProcess(agent);
        }
        else if (siptype.ToLower().StartsWith("sipdel"))
        {
            //SIP de eliminación
            ISipDelInterface sipDel = GetSipDel(siptype, agent);
            if (sipDel == null)
                throw new Exception("Tipo de SIP incorrecto >> " + siptype);

            sipDel.PreProcess(agent);
            sipDel.Process(args);
            sipDel.PostProcess(agent);
        }
        else
        {            
            //Obtener el Sip de entrada
            ISipInInterface sipIn = GetSipIn(siptype, agent);
            if (sipIn == null)
                throw new Exception("Tipo de SIP incorrecto >> " + siptype);

            //Si se informa del nombre del fichero, se procesa.
            if (!Utils.IsBlankField(filename))
            {
                //Añadir el path del Inbox
                string inboxFolder = new InBox(agent).GetInBoxFolder(agent) + "\\";
                string filename2 = null;

                //Comprobar si hay más de uno...
                if (filename.IndexOf(";") != -1)
                {
                    StringTokenizer st = new StringTokenizer(filename, ";");
                    filename = inboxFolder + st.NextToken();
                    filename2 = inboxFolder + st.NextToken();
                }
                else
                    filename = inboxFolder + filename;

                if (!File.Exists(filename))
                    throw new IOException("Fichero no encontrado >> " + filename);

                if (filename2 != null)
                {
                    if (!File.Exists(filename2))
                        throw new IOException("Fichero no encontrado >> " + filename2);
                    filename += ";" + filename2;
                }

                ProcessFile(g, sipIn, agent, filename);
            }
            else
            {
                //En caso contrario, se recorren todos los ficheros del agente
                InBox inbox = new InBox(agent);
                string[] files = inbox.GetFilesToUpload();
                if (files != null)
                {
                    Nomenclator nomenclator = new Nomenclator(siptype);
                    for (int i = 0; i < files.Length; i++)
                    {
                        //Comprobar si es para el sip indicado. Para ello, pivotamos sobre el nomenclator...
                        if (nomenclator.IsForThisNomenclator(files[i].ToLower()))
                        {
                            if (File.Exists(files[i]))
                            {
                                //Indicar al SIP el nomenclator(que puede ser variable en función de la configuración del INI)
                                sipIn.SetNomenclator(nomenclator.GetIdentifiedNomenclator());

                                //Procesar fichero
                                ProcessFile(g, sipIn, agent, files[i]);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Procesar un fichero de entrada
    /// </summary>
    /// <param name="g"></param>
    /// <param name="agent">agent</param>
    /// <param name="sipIn">SIP</param>
    /// <param name="filename">nombre de fichero</param> 
    private void ProcessFile(Globals g, ISipInInterface sipIn, string agent, string filename) 
    {
        string fname1 = "";
        string fname2 = "";
        string sipTypeName = sipIn.GetSipTypeName();

        //Comprobar si en el nombre del fichero llegan más de uno...
        if (filename.IndexOf(";") != -1)
        {
            StringTokenizer st = new StringTokenizer(filename, ";");
            fname1 = st.NextToken();
            fname2 = st.NextToken();
        }
        else
        {
            fname1 = filename;
            fname2 = "";
        }

        Log2 log = g.GetLog2();
        log.Info(agent, sipTypeName, "Inicio de backup de (" + fname1 + ") ...");

        //Copia de seguridad del fichero a procesar. (de inBox a backupBox).
        InBox inbox = new InBox(agent);
        string backupFileName = inbox.Backup(agent, sipTypeName, fname1);

        log.InfoBackup(agent, sipTypeName, "Backup de (" + backupFileName + ") finalizado");

        //Lectura secuencial del fichero para unificación de formatos de delimited Connect@ o XML a formato interno de procesado.
        SipManager sip = new SipManager();
        if (sip.ProcFile(agent, sipIn, filename))
        {
            //Borrado del fichero procesado (en inBox).
            inbox.Delete(fname1);
        }
    } 
  }
}
