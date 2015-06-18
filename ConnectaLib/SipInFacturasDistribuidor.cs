using System;
using System.Collections;
using System.Text;
using System.Xml;
using System.Data.Common;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.Data;
using System.IO;
using System.Collections.Generic;

namespace ConnectaLib 
{
  /// <summary>
  /// Clase que gestiona las facturas de distribuidor
  /// </summary>
  public class SipInFacturasDistribuidor : SipIn, ISipInInterface
  {      
    private Hashtable htFacturas = new Hashtable();
    private Hashtable htFacturasRepetidas = new Hashtable();
    private struct FacturasRepetidas
    {
        public string idcFab;
        public string idcDistr;
        public string numFactura;
        public string ejercicio;
        public string numLinea;
        public int cont;
    }

    private ArrayList facturasRecibidas = new ArrayList();
    private ArrayList datosEnvios = new ArrayList();

    private Int64 contFacturasErroneas = 0;
    
    private Hashtable htResetFacturas = new Hashtable();
    private struct ResetFacturas
    {
        public string idcFab;
        public string idcDistr;
        public string tipo;
        public string dateIni;
        public string dateFin;        
    }    

    private Hashtable htLiquidaciones = new Hashtable();
    private struct CabeceraLiquidacion
    {
        public bool ok;
        public string idcFab;
        public string idcDistr;
        public string numLiq;
        public string fecLiq;
        public string fecDesde;
        public string fecHasta;
        public string descLiq;
        public string statusLiq;
        public double numLineaMax;
    }

    private string codigoDistribuidor = "";
    private string idcClienteFinal = "";
    private string codigoCliente = "";
    private bool excluirClienteDeLiquidacion = false;
    private string ultimoFabricante = ""; //centinela 2 para cambio de fabricante
    private string ultimoNumFactura = ""; //centinela 2 para cambio de número de factura
    private string ultimoEjercicio = ""; //centinela 2 para cambio de ejercicio
    private string ultimoCodigoCliente = ""; //centinela para cambio de código de cliente
    private string idcCodigoFabricante = "";
    private string centinelaIdcCodigoFabricante = "";
    private bool productoCalculaCantidades = true;
    private bool productoFijaUMGestion = false;
    private string productoEsKit = "";
    private string NumFactura = "";   //centinela para cambio de factura
    private string ejercicio = "";   //centinela para cambio de ejercicio
    private string numLinea = "";
    private string codigoProducto = "";
    private string descripcionProducto = "";
    private string precioBrutoTotal = "";
    private string descuentos = "";
    private string precioBase = "";
    private string codigoProdFab = "";
    private string cantEstadisticaKit = "";
    private string proporcionImporteTotalKit = "";
    private string numeroComponentesKit = "";
    private string codigoProductoComponenteKitDist = "";
    private string UMProducto = "";
    private string UMProducto2 = "";
    private string UMProductoKit = "";
    private string fechaFactura = "";
    private DatosDireccionCliente datosDireccion = new DatosDireccionCliente();
    private UnidadMedida unidadMedida = new UnidadMedida();
    private Volumen volumen = new Volumen();
    private Peso peso = new Peso();
    private string CantidadEstadistica = "";
    private string UMEstadistica = "";
    private string CantidadEstadistica2 = "";
    private string UMEstadistica2 = "";
    private string CantidadEstadistica3 = "";
    private string UMEstadistica3 = "";
    private string TipoCalle = "";
    private string Calle = "";
    private string Numero = "";
    private string Provincia = "";
    private string CodigoPais = "";
    private string CodigoPostal = "";
    private string Direccion = "";
    private Producto prod = new Producto();
    private Cliente cli = new Cliente();
    private Fabricante fab = new Fabricante();
    private Distribuidor dist = new Distribuidor();
    private CodigoPostal codPostal = new CodigoPostal();

    private bool ajustarUMPorPrecio = false;
    private string fabDistAsignaUMDefecto = "";
    private string fechaInicioCargarFacturasDist = "";
    private bool fabDistVentasActivas = false;
    private bool fabDistLiquidacionesAcuerdosActivas = false;
    private bool fabDistLiquidacionesAcuerdosAjustarPorPuntoVerde = false;
    private string fabDistResetAutomatico = "";
    private string fabDistResetPorNumeroFactura = "";
    private string prodNoTratarKit = "";

    private string idcEntidadClasificada = "";

    //Clase interna con los datos de dirección del cliente
    private class DatosDireccionCliente
    {
      public string Direccion = "";
      public string TipoCalle = "";
      public string Calle = "";
      public string Numero = "";
      public string CodigoPostal = "";
      public string Provincia = "";
      public string CodigoPais = "";
      public string Poblacion = "";
      public bool direccionEncontrada = false;

      /// <summary>
      /// Inicializar
      /// </summary>
      public void Reset() { 
        Direccion = "";
        TipoCalle = "";
        Calle = "";
        Numero = "";
        CodigoPostal = "";
        Provincia = "";
        CodigoPais = "";
        Poblacion = "";
        direccionEncontrada = false;
      }
    }

    private Hashtable htAlbaranes = new Hashtable();
    private struct Albaranes
    {
        public string direccionEntrega;
        public string tipoCalle;
        public string calle;
        public string numero;
        public string codigoPostal;
        public string provincia;
        public string codigoPais;
        public string poblacion;        
    }

    private Hashtable htClasificadores = new Hashtable();
    private Hashtable htFacturasConErrores = new Hashtable();

    private bool modeBulk = false;
    private bool doBulk = false;
    private RecordLineasFacturasDistribuidor mbulk;

    //Id de SIP
    public const string ID_SIP = "SipInFacturasDistribuidor";

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="agent">agente</param>
    public SipInFacturasDistribuidor(string agent) : base(agent, ID_SIP)
    {
        masterTitle = "factura distribuidor";
        slaveTitle = "línea factura distribuidor";
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
      return "schFacturasDistribuidor.xsd";
    }

    /// <summary>
    /// Obtener nombre de Sip (básicamente a efectos de log)
    /// </summary>
    /// <returns>nombre de sip</returns>
    public string GetSipTypeName()
    {
      return "Distribuidor.Facturas.In";
    }

    /// <summary>
    /// Cargar datos comunes en memoria
    /// </summary>
    public void LoadData(Database db, string agent)
    {        
        cli.CargarClientesActuales(db, agent);        
        prod.CargarProductosActuales(db, agent);
        prod.CargarProductosReutilizados(db, agent);
        CargarAlbaranes(db, agent);
        unidadMedida.CargarUnidadesMedida(db, agent);
        CargarClasificadores(db, agent);
        CargarFacturasConErrores(db, agent);
    }

    /// <summary>
    /// Liberar datos comunes en memoria
    /// </summary>
    public void DeleteData()
    {
        cli.htCFAgentes.Clear();
        cli.htCFAgentesNum.Clear();
        cli.htClientesFinales.Clear();
        prod.htProductos.Clear();
        prod.htProductosReutilizados.Clear();        
        htAlbaranes.Clear();
        unidadMedida.VaciarUnidadesMedida();
        htClasificadores.Clear();
        htFacturasConErrores.Clear();
    }

    /// <summary>
    /// Pre-proceso
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    public void PreProcess(string filename)
    {
        Database db = Globals.GetInstance().GetDatabase();
        dist.ObtenerParametros(db, agent);     

        if (filename.ToUpper().Contains("PART001") || !filename.ToUpper().Contains("PART"))        
            SipManager.dtInicioIntegracion = DateTime.Now;

        //Cargar datos en memoria
        LoadData(db, agent);

        InvokeExternalProgram(this, agent);           
    }

    /// <summary>
    /// Post-proceso
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    public void PostProcess(string filename)
    {
        //RESET automático
        if (htResetFacturas != null && htResetFacturas.Count > 0)
        {
            Database db = Globals.GetInstance().GetDatabase();
            foreach (DictionaryEntry item in htResetFacturas)
            {
                ResetFacturas rf = (ResetFacturas)item.Value;
                DateTime dtIni = DateTime.Parse(rf.dateIni);
                DateTime dtFin = DateTime.Parse(rf.dateFin);
                string tipo = rf.tipo;
                string dateIni = "";
                string dateFin = "";
                if (tipo.Equals("D"))
                {
                    dateIni = dtIni.Day.ToString().PadLeft(2, '0') + "/" + dtIni.Month.ToString().PadLeft(2, '0') + "/" + dtIni.Year.ToString();
                    dateFin = dtFin.Day.ToString().PadLeft(2, '0') + "/" + dtFin.Month.ToString().PadLeft(2, '0') + "/" + dtFin.Year.ToString();
                }
                else
                {
                    dateIni = "01" + "/" + dtIni.Month.ToString().PadLeft(2, '0') + "/" + dtIni.Year.ToString();
                    dateFin = DateTime.DaysInMonth(dtFin.Year, dtFin.Month).ToString().PadLeft(2, '0') + "/" + dtFin.Month.ToString().PadLeft(2, '0') + "/" + dtFin.Year.ToString();
                }
                BorrarLineasFacturasPorPeriodo(db, rf.idcDistr, dateIni, dateFin, rf.idcFab, SipManager.dtInicioIntegracion);
                string myAlertMsg = "Aviso en " + slaveTitle + ". RESET de facturas automático para el ditribuidor {0} y fabricante {1} entre las fechas {2} - {3}. Y con fecha modificación inferior a {4}.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "FADI0034", myAlertMsg, rf.idcDistr, rf.idcFab, rf.dateIni, rf.dateFin, SipManager.dtInicioIntegracion.ToString("dd/MM/yyyy hh:mm:ss"));                
            }
        }

        //Generar cabeceras de liquidaciones si es necesario
        if (htLiquidaciones != null && htLiquidaciones.Count > 0)
        {
            ProcesarCabecerasLiquidaciones();

            htLiquidaciones.Clear();
        }

        //Generar alertas si existen facturas repetidas en un mismo fichero recibido
        if (htFacturasRepetidas != null && htFacturasRepetidas.Count > 0)
        {
            foreach (DictionaryEntry item in htFacturasRepetidas)            
            {
                FacturasRepetidas fr = (FacturasRepetidas)item.Value;
                string myAlertMsg = "Aviso en " + slaveTitle + ". Hay {0} facturas repetidas en un mismo fichero recibido. Factura {1} línea {2} ejercicio {3}.";
                string[] aValores = new string[] { fr.cont.ToString(), fr.numFactura, fr.numLinea, fr.ejercicio };
                string[] aClaves = new string[] { fr.numFactura, fr.numLinea, fr.ejercicio };
                string[] aClavesExt = new string[] { fr.numFactura, fr.numLinea, fr.ejercicio };
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "FADI0029", myAlertMsg, aValores, aClaves, aClavesExt);
            }
        }

        //Tratar el envío del acuse de recibo al remitente (distribuidor) vía mail
        //(Tiene que ser una númerica de las facturas recibidas (las correctas + las erróneas)). 
        //////////////////////////////////////////////////////
        Nomenclator n = new Nomenclator(GetId());
        //Si filename contiene dos nombres de ficheros entonces se debe tratar el acuse de recibo
        //Y si no, solo se trata el resumen si filename es un proceso slave.
        if (filename.IndexOf(";") != -1 || !n.IsMasterFile(GetId(), filename))
        {
            if ((contFacturasErroneas > 0 && facturasRecibidas.Count > 0) || (facturasRecibidas.Count > 0))
            {
                GenerarAcuseReciboFacturas();

                //Inicializamos el contador de facturas erroneas por si acaso.
                contFacturasErroneas = 0;
                //Inicializamos el contador de facturas recibidas por si acaso.
                facturasRecibidas.Clear();
            }            
        }

        //Generar control envíos
        if (datosEnvios.Count > 0)
        {
            ControlDatosEnvios.GenerarControlDatosEnviosDistribuidor(datosEnvios, ControlDatosEnvios.TIPODATO_VENTAS, GetSipTypeName());

            //Inicializamos el control de datos de envíos por si acaso
            datosEnvios.Clear();
        }

        //Limpiamos datos cargados en memoria
        DeleteData();

        //Escribimos logs de manera masiva
        WriteLogs();

        //Salimos del modo bulk
        modeBulk = false;

        //Finalmente ejecutamos un programa externo personalizado
        InvokeExternalProgramPOST(this, agent);
  }

    /// <summary>
    /// Cargar datos entrega en albaranes
    /// </summary>
    private void CargarAlbaranes(Database db, string agent)
    {
        htAlbaranes.Clear();
        DbDataReader cursor = null;
        string sql = "Select distinct NumAlbaran, Ejercicio, DireccionEntrega, TipoCalle, Calle, Numero, CodigoPostal, Provincia, CodigoPais, Poblacion " +
                     "  from AlbaranesCF Where IdcAgente = " + agent;
        cursor = db.GetDataReader(sql);
        string numAlb = "";
        string ejercicio = "";
        while (cursor.Read())
        {
            Albaranes alb = new Albaranes();
            numAlb = db.GetFieldValue(cursor, 0);
            ejercicio = db.GetFieldValue(cursor, 1);
            alb.direccionEntrega = db.GetFieldValue(cursor, 2);
            alb.tipoCalle = db.GetFieldValue(cursor, 3);
            alb.calle = db.GetFieldValue(cursor, 4);
            alb.numero = db.GetFieldValue(cursor, 5);
            alb.codigoPostal = db.GetFieldValue(cursor, 6);
            alb.provincia = db.GetFieldValue(cursor, 7);
            alb.codigoPais = db.GetFieldValue(cursor, 8);
            alb.poblacion = db.GetFieldValue(cursor, 9);
            if (!htAlbaranes.ContainsKey(numAlb + ";" + ejercicio))
                htAlbaranes.Add(numAlb + ";" + ejercicio, alb);            
        }
        cursor.Close();
        cursor = null;
    }

    /// <summary>
    /// Cargar datos clasificadores agente
    /// </summary>
    private void CargarClasificadores(Database db, string agent)
    {
        htClasificadores.Clear();
        DbDataReader cursor = null;
        string sql = "select EntidadClasificada, Codigo, Libre1 " +
                     "  from ClasificacionAgente " +
                     " where IdcAgente = " + agent;
        cursor = db.GetDataReader(sql);
        string entidadClasificada = "";
        string codigo = "";
        while (cursor.Read())
        {            
            entidadClasificada = db.GetFieldValue(cursor, 0);
            codigo = db.GetFieldValue(cursor, 1);
            if (!htClasificadores.ContainsKey(entidadClasificada + ";" + codigo))
                htClasificadores.Add(entidadClasificada + ";" + codigo, db.GetFieldValue(cursor, 2));
        }
        cursor.Close();
        cursor = null;
    }

    /// <summary>
    /// Cargar datos entrega en albaranes
    /// </summary>
    private void CargarFacturasConErrores(Database db, string agent)
    {
        htFacturasConErrores.Clear();
        DbDataReader cursor = null;
        string sql =  "select distinct NumFactura, Ejercicio from ProductosFacturasConErrores where idcagente=" + agent;
        cursor = db.GetDataReader(sql);
        string numFactura = "";
        string ejercicio = "";
        while (cursor.Read())
        {
            numFactura = db.ValueForSql(db.GetFieldValue(cursor, 0));
            ejercicio = db.ValueForSql(db.GetFieldValue(cursor, 1));            
            if (!htFacturasConErrores.ContainsKey(numFactura + ";" + ejercicio))
                htFacturasConErrores.Add(numFactura + ";" + ejercicio, numFactura + ";" + ejercicio);
        }
        cursor.Close();
        cursor = null;
    }

    private void ProcesarCabecerasLiquidaciones()
    {
        Database db = Globals.GetInstance().GetDatabase();
        string sql = "";

        foreach (DictionaryEntry item in htLiquidaciones)
        {
            CabeceraLiquidacion cl = (CabeceraLiquidacion)item.Value;

            if (cl.ok)
            {
                sql = "SELECT 1 FROM LiquidacionesAcuerdos WHERE IdcFabricante = " + cl.idcFab + " AND IdcDistribuidor = " + cl.idcDistr + " AND NumLiquidacion = '" + cl.numLiq + "'";
                if (Utils.RecordExist(sql))
                {
                    sql = "UPDATE LiquidacionesAcuerdos SET " +
                        " FechaLiquidacion = " + db.DateForSql(cl.fecLiq) +
                        " ,FechaDesde = " + db.DateForSql(cl.fecDesde) +
                        " ,FechaHasta = " + db.DateForSql(cl.fecHasta) +
                        " ,DescLiquidacion = " + db.ValueForSql(cl.descLiq) +
                        " ,Status = " + db.ValueForSql(Constants.ESTADO_PENDIENTE) +
                        " ,FechaModificacion = " + db.SysDate() + " " +
                        " ,UsuarioModificacion = " + db.ValueForSql(Constants.PREFIJO_USUARIO_AGENTE + agent) +
                        " WHERE IdcFabricante = " + cl.idcFab + " AND IdcDistribuidor = " + cl.idcDistr + " AND NumLiquidacion = '" + cl.numLiq + "'";
                    db.ExecuteSql(sql, agent, GetSipTypeName());
                }
                else
                {
                    sql = "INSERT INTO LiquidacionesAcuerdos" +
                        " (IdcFabricante,IdcDistribuidor,NumLiquidacion,FechaLiquidacion,FechaDesde,FechaHasta,DescLiquidacion,Status,FechaInsercion,FechaModificacion,UsuarioInsercion,UsuarioModificacion)" +
                        " VALUES " +
                        "(" + cl.idcFab + "," + cl.idcDistr + "," + db.ValueForSql(cl.numLiq) + "," + db.DateForSql(cl.fecLiq) + "," + db.DateForSql(cl.fecDesde) + "," + db.DateForSql(cl.fecHasta) + "," + db.ValueForSql(cl.descLiq) + "," + db.ValueForSql(Constants.ESTADO_PENDIENTE) + "," + db.SysDate() +  "," + db.SysDate() +  "," + db.ValueForSql(Constants.PREFIJO_USUARIO_AGENTE + agent) +  "," + db.ValueForSql(Constants.PREFIJO_USUARIO_AGENTE + agent) + ")";
                    db.ExecuteSql(sql, agent, GetSipTypeName());
                }
            }
        }
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
        return new RecordFacturasDistribuidor();
    }

    /// <summary>
    /// Obtener registro esclavo
    /// </summary>
    /// <returns>registro esclavo</returns>
    public CommonRecord GetSlaveRecord()
    {
        return new RecordLineasFacturasDistribuidor();
    }

    /// <summary>
    /// Obtener sección de XML para el registro maestro
    /// </summary>
    /// <returns>sección de XML</returns>
    public string GetMasterXMLSection()
    {
        return "FacturaVentasDistribuidor";
    }

    /// <summary>
    /// Obtener sección de XML para el registro esclavo
    /// </summary>
    /// <returns>sección de XML</returns>
    public string GetSlaveXMLSection()
    {
        return "LineaFacturaVentasDistribuidor";
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
    /// Procesar de un objeto interno que representa un registro de un
    /// fichero delimited o una sección XML
    /// </summary>
    /// <param name="rec">registro</param>
    public void ProcessMaster(CommonRecord rec)
    {
        RecordFacturasDistribuidor factura = (RecordFacturasDistribuidor)rec;
        Database db = Globals.GetInstance().GetDatabase();
        Log2 log = Globals.GetInstance().GetLog2();
        codigoDistribuidor = agent;
        string sql = "";

        codigoCliente = factura.CodigoCliente;

        //1.	Comprobar el código de cliente que nos ha pasado el distribuidor
        string codigoClienteCopia = codigoCliente;
        if (!ComprobarCliente(db, codigoCliente))
        {            
            if (dist.DISTEstado == Constants.ESTADO_ACTIVO)
            {
                //Si el agente está activo creamos el cliente automáticamente
                //Crearemos un nuevo registro en ClientesFinales                    
                
                //No está dado de alta en Connecta...
                Int32 clienteFinal = 0;
                ////////////////////Crearemos un nuevo registro en la tabla Agentes
                ////////////////////Obtenemos el identificador que le ha asignado
                //////////////////clienteFinal = cli.CreaAgente(db, agent, GetSipTypeName());

                cli.CreaClienteFinal(db, Constants.CLIENTES_DISTR_SIN_DESCRIPCION + " " + codigoClienteCopia,
                    string.Empty, string.Empty, Constants.CLIENTES_DISTR_SIN_DIRECCION + " " + codigoClienteCopia,
                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                    string.Empty, string.Empty, string.Empty, string.Empty, agent, agentLocation, GetSipTypeName());

                //Obtener código de cliente...
                //Obtenemos el identificador que le ha asignado
                clienteFinal = cli.ObtenerUltimoIdcClienteFinal(db);            

                //Daremos de alta un nuevo registro en CFAgentes        
                cli.CreaCFAgentes(db, clienteFinal, codigoClienteCopia, Constants.CLIENTES_DISTR_SIN_DESCRIPCION + " " + codigoClienteCopia, 
                    string.Empty, Constants.ESTADO_ACTIVO,
                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                    agent, GetSipTypeName(), "S",
                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
                
                idcClienteFinal = clienteFinal.ToString();
                codigoCliente = codigoClienteCopia;

                string myAlertMsg = "Aviso en " + masterTitle + ". Se ha creado automáticamente el cliente {0} de la factura {1}.";
                log.Trace(agent, GetSipTypeName(), "FADI0028", myAlertMsg, codigoClienteCopia, factura.NumFactura);

                ConnectaLib.Cliente.sCFAgente cf = new ConnectaLib.Cliente.sCFAgente();
                cf.idcClienteFinal = clienteFinal.ToString();
                cf.codigoCliente = codigoCliente;
                cf.nombreCliente = Constants.CLIENTES_DISTR_SIN_DESCRIPCION + " " + codigoCliente;
                cf.estadoCliente = Constants.ESTADO_ACTIVO;
                cf.estadoManualCliente = "";
                cf.excluirDeLiquidacion = false;
                if (!cli.htCFAgentes.ContainsKey(cf.codigoCliente))
                    cli.htCFAgentes.Add(cf.codigoCliente, cf);

                ConnectaLib.Cliente.sClientesFinales c = new ConnectaLib.Cliente.sClientesFinales();
                c.direccion = Constants.CLIENTES_DISTR_SIN_DIRECCION + " " + codigoCliente;
                c.tipoCalle = "";
                c.calle = "";
                c.numero = "";
                c.codigoPostal = "";
                c.provincia = "";
                c.codigoPais = "";
                c.poblacion = "";
                if (!cli.htClientesFinales.ContainsKey(cf.idcClienteFinal))
                    cli.htClientesFinales.Add(cf.idcClienteFinal, c);
            }
            else
            {
                string myAlertMsg = "Error en " + masterTitle + ". No existe el cliente {0} en la factura {1}.";
                log.Trace(agent, GetSipTypeName(), "FADI0001", myAlertMsg, codigoClienteCopia, factura.NumFactura);
                return;
            }
        }

        //2.	Comprobar que la fecha de factura que nos ha pasado el distribuidor esté informada
        if (Utils.IsBlankField(factura.FechaFra))
        {
            //log.DetailedError(agent, GetSipTypeName(), "Error en " + masterTitle + ". La fecha de factura está vacía en la factura " + factura.NumFactura);
            string myAlertMsg = "Error en " + masterTitle + ". La fecha de factura está vacía en la factura {0}.";
            log.Trace(agent, GetSipTypeName(), "FADI0002", myAlertMsg, factura.NumFactura);
            return;
        }

        //3.	Obtener el código de moneda que nos ha pasado el distribuidor
        string moneda = Utils.ObtenerMoneda(db, agent, GetSipTypeName(), factura.CodigoMoneda);

        //4.	Comprobar si existe la factura en ConnectA
        string where = "where IdcAgente = " + codigoDistribuidor + " and NumFactura = '" + factura.NumFactura + "' and Ejercicio = '" + factura.Ejercicio + "'";
        sql = "select IdcAgente from FacturasCF "+where;
        if(Utils.RecordExist(sql)) 
        {
            //Factura existente
            sql = "update FacturasCF " +
                    "set IdcClienteFinal=" + idcClienteFinal +
                    ",FechaFra=" + db.DateForSql(factura.FechaFra) +
                    ",ImporteBruto=" + db.ValueForSqlAsNumeric(factura.ImporteBruto) +
                    ",Impuestos=" + db.ValueForSqlAsNumeric(factura.Impuestos) +
                    ",ImporteTotal=" + db.ValueForSqlAsNumeric(factura.ImporteTotal) +
                    ",CodigoMoneda=" + db.ValueForSql(moneda) +
                    ",CodigoCliente=" + db.ValueForSql(codigoCliente) +
                    where;
            db.ExecuteSql(sql, agent, GetSipTypeName());
        }
        else 
        {
            //Factura no existente. Dar de alta la cabecera.
            sql = "insert into FacturasCF (IdcAgente,NumFactura,Ejercicio,IdcClienteFinal" +
                    ",FechaFra,ImporteBruto,Impuestos,ImporteTotal,CodigoMoneda,CodigoCliente) " +
                    "values (" + agent + "," + db.ValueForSql(factura.NumFactura) + "," + db.ValueForSql(factura.Ejercicio) +
                    "," + idcClienteFinal +
                    "," + db.DateForSql(factura.FechaFra) +
                    "," + db.ValueForSqlAsNumeric(factura.ImporteBruto) +
                    "," + db.ValueForSqlAsNumeric(factura.Impuestos) +
                    "," + db.ValueForSqlAsNumeric(factura.ImporteTotal) +
                    "," + db.ValueForSql(moneda)+
                    "," + db.ValueForSql(codigoCliente) +
                    ")";
            db.ExecuteSql(sql, agent, GetSipTypeName());
        }
    }

    ///// <summary>
    ///// Borrar factura y líneas
    ///// </summary>
    ///// <param name="codDist">distribuidor</param>
    ///// <param name="NumFactura">número de factura</param>
    ///// <param name="ejercicio">ejercicio</param>
    //private void BorrarFactura(string codDist, string NumFactura, string ejercicio)
    //{
    //  string sql = "";
    //  Database db = Globals.GetInstance().GetDatabase();
    //  Globals.GetInstance().GetLog().TraceToFile(Log.TYPE_INFO, Log.LEVEL_MEDIUM, "Borrado de factura erroneo, distribuidor = " + codDist +
    //    ",factura = " + NumFactura + ",ejercicio = " + ejercicio);

    //  //Borrar líneas actuales (primero deben ser las líneas por la integridad referencial...)
    //  sql = "DELETE from ProductosFacturas WHERE IdcAgente = " + codDist + " and NumFactura = '" + NumFactura + "' " +
    //        "AND Ejercicio = " + ejercicio;
    //  db.ExecuteSql(sql, agent, GetSipTypeName());

    //  //Cabecera
    //  sql = "DELETE from FacturasCF where IdcAgente = " + codDist + " and NumFactura = '" + NumFactura + "' " +
    //        " and Ejercicio = " + ejercicio;
    //  db.ExecuteSql(sql, agent, GetSipTypeName());
    //}

    ///// <summary>
    ///// Añadir a lista de facturas erróneas
    ///// </summary>
    ///// <param name="factura">factura</param>
    ///// <param name="ejercicio">ejercicio</param>
    //private void MarcarFacturaErronea(string factura, string ejercicio, string numLinea)
    //{
    //    FacturasErroneas pe = new FacturasErroneas(factura, ejercicio, numLinea);
    //    facturasErroneas.Add(pe);
    //}

    /// <summary>
    /// Incrementar el contador de facturas erróneas
    /// </summary>
    private void MarcarFacturaErronea()
    {
        contFacturasErroneas++;
    }

    /// <summary>
    /// Contabiliza una factura como recibida
    /// </summary>
    private void MarcarFacturaRecibida(string pAgenteDist, string pAgenteFab)
    {
        bool bEncontrado = false;

        foreach (FacturaRecibida fr in facturasRecibidas)
        {
            if (pAgenteDist == fr.idcDistribuidor && pAgenteFab == fr.idcFabricante)
            {
                fr.contador++;
                bEncontrado = true;
                break;
            }
        }
        if (!bEncontrado)
        {
            Database db = Globals.GetInstance().GetDatabase();
            DbDataReader cursor = null;
            string sql = "";

            //Obtenemos la información del fabricante según el distribuidor.
            string codFab = "";
            string nomFab = "";
            sql = "SELECT Codigo, Nombre FROM ClasifInterAgentes " +
                         " WHERE IdcAgenteOrigen = " + pAgenteDist + " " +
                         " AND IdcAgenteDestino = " + pAgenteFab + " ";
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
            {
                codFab = db.GetFieldValue(cursor, 0);
                nomFab = db.GetFieldValue(cursor, 1);
            }
            if (cursor != null)
                cursor.Close();

            //Añadir a la lista resumen
            FacturaRecibida fr = new FacturaRecibida(pAgenteDist, pAgenteFab, codFab, nomFab);
            facturasRecibidas.Add(fr);
        }
    }

    private void MarcarControlDatosEnvios(string pAgenteDist, string pAgenteFab, string pFecha)
    {
        bool bEncontrado = false;

        foreach (ControlDatosEnvios cde in datosEnvios)
        {
            if (pAgenteDist == cde.idcDistribuidor && pAgenteFab == cde.idcFabricante)
            {
                if (cde.fechaIniDatos > DateTime.Parse(pFecha)) cde.fechaIniDatos = DateTime.Parse(pFecha);
                if (cde.fechaFinDatos < DateTime.Parse(pFecha)) cde.fechaFinDatos = DateTime.Parse(pFecha);
                bEncontrado = true;
                break;
            }
        }
        if (!bEncontrado)
        {
            //Añadir a la lista resumen
            ControlDatosEnvios cde = new ControlDatosEnvios(pAgenteDist, pAgenteFab, pFecha, pFecha);
            datosEnvios.Add(cde);
        }
    }

    /// <summary>
    /// Generar el acuse de recibo de facturas recibidas para enviarlo por email al distribuidor remitente
    /// </summary>
    private void GenerarAcuseReciboFacturas()
    {
        Database db = Globals.GetInstance().GetDatabase();
        DbDataReader cursor = null;
        string sql = "";

        //Contador de facturas erroneas
        Int64 numFactErr = contFacturasErroneas;

        string sNombreDistribuidor = "";
        string sFabricantes = "";
        string sNombreFabricantes = "";
        
        string emailFrom = "";
        string emailTo = "";
        string emailCC = "";

        //Cargamos la configuración del cliente SMTP
        string sSMTPServer = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_SERVER, IniManager.GetIniFile());
        string sRequiresAuthentication = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_AUTHENTICATION, IniManager.GetIniFile());
        string sUser = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_USER, IniManager.GetIniFile());
        string sPassword = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_PWRD, IniManager.GetIniFile());
        
        if (dist.DISTFacRecibeAcuseRecibo == "S")
        {
            //Primero buscamos el nombre del distribuidor
            sNombreDistribuidor = dist.ObtenerNombre(db, agent);
            
            //Ahora obtenemos una lista de los fabricantes.
            foreach (FacturaRecibida fr in facturasRecibidas)
            {
                sFabricantes += "'" + fr.idcFabricante + "',";
                sNombreFabricantes += " " + fr.codFabricante + "-" + fr.nomFabricante + " / ";
            }
            sFabricantes = sFabricantes.Remove(sFabricantes.LastIndexOf(","));
            sNombreFabricantes = sNombreFabricantes.Remove(sNombreFabricantes.LastIndexOf(" / ")).Trim();

            //Ahora, obtenemos la lista de destinatarios acumulando para todos los fabricantes.
            //Si el parámetro está activo para el distribuidor, se envia un único mail para todos los fabricantes
            sql = "SELECT Fac_AcuseReciboEMailFrom, fac_AcuseReciboEMailTo FROM CfgEMailAgentes " +
                            " WHERE IdcAgenteOrigen In (" + sFabricantes + ") " +
                            " AND IdcAgenteDestino = " + agent + " ";
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
            {
                emailFrom = db.GetFieldValue(cursor, 0);
                emailTo = db.GetFieldValue(cursor, 1);
            }
            else
            {
                if (cursor != null)
                    cursor.Close();

                sql = "SELECT Fac_AcuseReciboEMailFrom, fac_AcuseReciboEMailTo FROM CfgEMailAgentes " +
                                " WHERE IdcAgenteDestino In (" + sFabricantes + ") " +
                                " AND IdcAgenteOrigen = " + agent + " ";
                cursor = db.GetDataReader(sql);
                if (cursor.Read())
                {
                    emailFrom = db.GetFieldValue(cursor, 0);
                    emailTo = db.GetFieldValue(cursor, 1);
                }
            }
            if (cursor != null)
                cursor.Close();

            if (!Utils.IsBlankField(emailFrom) && !Utils.IsBlankField(emailTo))
            {
                //Instanciamos el objeto para enviar SMTP, preparamos su configuración
                //e invocamos el método que hace el trabajo.
                SMTPSender oSMTPSender = new SMTPSender();
                oSMTPSender.sFROM = emailFrom;
                oSMTPSender.sTO = emailTo;
                oSMTPSender.sCC = emailCC;
                oSMTPSender.sBCC = "";
                oSMTPSender.sSUBJECT = ObtenerMensajeAsuntoAcuseRecibo(sNombreFabricantes, sNombreDistribuidor, true);

                oSMTPSender.sBODY = ObtenerMensajeCuerpoAcuseRecibo(facturasRecibidas, numFactErr, sFabricantes, sNombreFabricantes, sNombreDistribuidor, true);
                oSMTPSender.isHTML = true;
                oSMTPSender.sATTACHMENTS = "";

                oSMTPSender.sSMTPServer = sSMTPServer;
                oSMTPSender.sRequiresAuthentication = sRequiresAuthentication;
                oSMTPSender.sUser = sUser;
                oSMTPSender.sPassword = sPassword;
                oSMTPSender.sPathLog = "";
                oSMTPSender.DoWork(agent, GetSipTypeName());
                if (!oSMTPSender.bResult)
                {
                    string myAlertMsg = "uveSMTPSender no pudo enviar el mensaje al distribuidor {0}.";
                    Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "FADI0003", myAlertMsg, agent);
                }
            }
        }

        //Si el parámetro está activo para el distribuidor-fabricante o fabricante-distribuidor, se envia un mail por fabricante
        //Ahora obtenemos una lista de los fabricantes.
        string fabDistRecibeAcuseRecibo = "";
        foreach (FacturaRecibida fr in facturasRecibidas)
        {
            emailFrom = "";
            emailTo = "";
            emailCC = "";

            sFabricantes = fr.idcFabricante;
            sNombreFabricantes = " " + fr.codFabricante + "-" + fr.nomFabricante + " / ";

            fabDistRecibeAcuseRecibo = dist.ObtenerParametro(db, agent, sFabricantes, "Fac_RecibeAcuseRecibo").ToUpper().Trim();

            if (fabDistRecibeAcuseRecibo == "S")
            {
                if (Utils.IsBlankField(sNombreDistribuidor))
                {
                    sNombreDistribuidor = dist.ObtenerNombre(db, agent);
                }
                
                sql = "SELECT Fac_AcuseReciboEMailFrom, fac_AcuseReciboEMailTo FROM CfgEMailAgentes " +
                                " WHERE IdcAgenteOrigen = " + sFabricantes + " " +
                                " AND IdcAgenteDestino = " + agent + " ";
                cursor = db.GetDataReader(sql);
                if (cursor.Read())
                {
                    emailFrom = db.GetFieldValue(cursor, 0);
                    emailTo = db.GetFieldValue(cursor, 1);
                }
                if (cursor != null)
                    cursor.Close();

                if (Utils.IsBlankField(emailFrom) || Utils.IsBlankField(emailTo))
                {
                    sql = "SELECT Fac_AcuseReciboEMailFrom, fac_AcuseReciboEMailTo FROM CfgEMailAgentes " +
                                    " WHERE IdcAgenteDestino = " + sFabricantes + " " +
                                    " AND IdcAgenteOrigen = " + agent + " ";
                    cursor = db.GetDataReader(sql);
                    if (cursor.Read())
                    {
                        emailFrom = db.GetFieldValue(cursor, 0);
                        emailTo = db.GetFieldValue(cursor, 1);
                    }
                    if (cursor != null)
                        cursor.Close();
                }

                if (!Utils.IsBlankField(emailFrom) && !Utils.IsBlankField(emailTo))
                {
                    //Instanciamos el objeto para enviar SMTP, preparamos su configuración
                    //e invocamos el método que hace el trabajo.
                    SMTPSender oSMTPSender = new SMTPSender();
                    oSMTPSender.sFROM = emailFrom;
                    oSMTPSender.sTO = emailTo;
                    oSMTPSender.sCC = emailCC;
                    oSMTPSender.sBCC = "";
                    oSMTPSender.sSUBJECT = ObtenerMensajeAsuntoAcuseRecibo(sNombreFabricantes, sNombreDistribuidor, false);

                    oSMTPSender.sBODY = ObtenerMensajeCuerpoAcuseRecibo(facturasRecibidas, numFactErr, sFabricantes, sNombreFabricantes, sNombreDistribuidor, false);
                    oSMTPSender.isHTML = true;
                    oSMTPSender.sATTACHMENTS = "";

                    oSMTPSender.sSMTPServer = sSMTPServer;
                    oSMTPSender.sRequiresAuthentication = sRequiresAuthentication;
                    oSMTPSender.sUser = sUser;
                    oSMTPSender.sPassword = sPassword;
                    oSMTPSender.sPathLog = "";
                    oSMTPSender.DoWork(agent, GetSipTypeName());
                    if (!oSMTPSender.bResult)
                    {
                        string myAlertMsg = "uveSMTPSender no pudo enviar el mensaje al distribuidor {0} del fabricante {1}.";
                        Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "FADI0037", myAlertMsg, agent, sFabricantes);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Obtener /crear el asunto de un mensaje e-mail
    /// /// </summary>
    private string ObtenerMensajeAsuntoAcuseRecibo(string pNomFabricantes, string pNombreDistribuidor, bool pVariosFabricantes)
    {
        string sStr = "";
        if (pVariosFabricantes) sStr = "Informe ConnectA: Acuse de recibo de ventas de " + pNombreDistribuidor + ". Fabricante/s: " + pNomFabricantes;
        else sStr = "Informe ConnectA: Acuse de recibo de ventas de " + pNombreDistribuidor + " para " + pNomFabricantes + ".";

        return sStr;
    }

    /// <summary>
    /// Obtener /crear el cuerpo de un mensaje e-mail
    /// /// </summary>
    private string ObtenerMensajeCuerpoAcuseRecibo(ArrayList facRec, Int64 pNumErr, string pFabricantes, string pNomFabricantes, string pNombreDistribuidor, bool pVariosFabricantes)
    {

        string sBody = "";

        //Logo de Connect@
        sBody += "<table style='width:100%'>"; //abrir tabla
        sBody += "<tr>"; //abrir fila
        sBody += "<td style='text-align:right'>"; //abrir columna
        sBody += "<img src=\"https://connecta.uvesolutions.com/images/connectA%20(172%20x%2040).gif\">";
        sBody += "</td>"; //cerrar columna
        sBody += "</tr>"; //cerrar fila
        sBody += "</table>"; //cerrar tabla

        //Introducción
        sBody += "<br/>"; //Retorno de carro
        sBody += "Confirmamos que se han recibido en ConnectA las ventas de " + pNombreDistribuidor + " dirigidas a " + pNomFabricantes + ". El resultado del proceso de recepción es el siguiente:";
        sBody += "<br/>"; //Retorno de carro

        //Resumen totales
        sBody += "<table align=center style='width:50%; font-family:Calibri; font-size:10.0pt'>"; //abrir tabla
        foreach (FacturaRecibida fr in facRec)
        {
            if (pVariosFabricantes || (!pVariosFabricantes && pFabricantes == fr.idcFabricante))
            {
                sBody += "<tr>"; //abrir fila
                sBody += "<td style='text-align:left;width:400px'>"; //abrir columna
                sBody += "Líneas de venta recibidas correctamente de " + fr.nomFabricante + ": ";
                sBody += "</td>"; //cerrar columna
                sBody += "<td style='text-align:left'>"; //abrir columna
                sBody += fr.contador.ToString();
                sBody += "</td>"; //cerrar columna
                sBody += "</tr>"; //cerrar fila
            }
        }
        sBody += "<tr>"; //abrir fila
        sBody += "<td style='text-align:left;width:400px'>"; //abrir columna
        sBody += "Líneas de venta erróneas a revisar por el equipo de soporte: ";
        sBody += "</td>"; //cerrar columna
        sBody += "<td style='text-align:left'>"; //abrir columna
        sBody += pNumErr.ToString();
        sBody += "</td>"; //cerrar columna
        sBody += "</tr>"; //cerrar fila

        sBody += "</table>"; //cerrar tabla
        sBody += "<br/>"; //Retorno de carro

        //Conclusión y despedidda
        sBody += "<br/>"; //Retorno de carro
        sBody += "Para cualquier tema no dudes contactar con nosotros.";
        sBody += "<br/>"; //Retorno de carro
        sBody += "<br/>"; //Retorno de carro
        sBody += "<br/>"; //Retorno de carro
        sBody += "Atentamente,";
        sBody += "<br/>"; //Retorno de carro
        sBody += "<br/>"; //Retorno de carro
        sBody += "<img width=213 height=49 id=\"_x0000_i1025\" src=\"https://connecta.uvesolutions.com/images/UVE_Powwering.jpg?v=20140715\" alt=\"https://connecta.uvesolutions.com/images/UVE_Powwering.jpg?v=20140715\">";

        return sBody;
    }      

    /// <summary>
    /// Comprobar cliente
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="factura">registro de factura</param>
    /// <returns>true si es correcto</returns>
    private bool ComprobarCliente(Database db, string pCliente)
    {
        bool isOK = false;
        idcClienteFinal = "";
        codigoCliente = "";
        excluirClienteDeLiquidacion = false;

        cli.IdcClienteFinal = "";
        cli.CodCliente = "";
        cli.NomCliente = "";
        cli.EstadoCliente = "";
        cli.EstadoManualCliente = "";
        cli.ExcluirDeLiquidacion = false;
        
        Double numCodigoCliente = 0;         
        //Obtener código de cliente final
        if (Double.TryParse(pCliente, out numCodigoCliente))
        {
            if (cli.htCFAgentes.ContainsKey(pCliente))
            {
                ConnectaLib.Cliente.sCFAgente cf = (ConnectaLib.Cliente.sCFAgente)cli.htCFAgentes[pCliente];
                isOK = true;
                idcClienteFinal = cf.idcClienteFinal;
                codigoCliente = cf.codigoCliente;
                excluirClienteDeLiquidacion = cf.excluirDeLiquidacion;

                cli.IdcClienteFinal = cf.idcClienteFinal;
                cli.CodCliente = cf.codigoCliente;
                cli.NomCliente = cf.nombreCliente;
                cli.EstadoCliente = cf.estadoCliente;
                cli.EstadoManualCliente = cf.estadoManualCliente;
                cli.ExcluirDeLiquidacion = cf.excluirDeLiquidacion;
            }
            else if (cli.htCFAgentesNum.ContainsKey(numCodigoCliente))
            {
                ConnectaLib.Cliente.sCFAgente cf = (ConnectaLib.Cliente.sCFAgente)cli.htCFAgentesNum[numCodigoCliente];
                isOK = true;
                idcClienteFinal = cf.idcClienteFinal;
                codigoCliente = cf.codigoCliente;
                excluirClienteDeLiquidacion = cf.excluirDeLiquidacion;

                cli.IdcClienteFinal = cf.idcClienteFinal;
                cli.CodCliente = cf.codigoCliente;
                cli.NomCliente = cf.nombreCliente;
                cli.EstadoCliente = cf.estadoCliente;
                cli.EstadoManualCliente = cf.estadoManualCliente;
                cli.ExcluirDeLiquidacion = cf.excluirDeLiquidacion;
            }
        }
        else
        {
            if (cli.htCFAgentes.ContainsKey(pCliente))
            {
                ConnectaLib.Cliente.sCFAgente cf = (ConnectaLib.Cliente.sCFAgente)cli.htCFAgentes[pCliente];
                isOK = true;
                idcClienteFinal = cf.idcClienteFinal;
                codigoCliente = cf.codigoCliente;
                excluirClienteDeLiquidacion = cf.excluirDeLiquidacion;

                cli.IdcClienteFinal = cf.idcClienteFinal;
                cli.CodCliente = cf.codigoCliente;
                cli.NomCliente = cf.nombreCliente;
                cli.EstadoCliente = cf.estadoCliente;
                cli.EstadoManualCliente = cf.estadoManualCliente;
                cli.ExcluirDeLiquidacion = cf.excluirDeLiquidacion;
            }
        }             
        return isOK;
    }

    /// <summary>
    /// Comprobar código de producto
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="linea">registro de línea de factura</param>
    /// <returns>true si es correcto</returns>
    private bool ComprobarCodigoProducto(Database db, RecordLineasFacturasDistribuidor linea)
    {
        //Primero comprobamos si existe reutilización de código de producto según fecha factura
        if (prod.htProductosReutilizados.ContainsKey(linea.CodigoProducto))
        {
            DateTime fechaFra = (DateTime)db.GetDate(linea.FechaFra);
            prod.listsProductosReutilizados = (List<ConnectaLib.Producto.sProductosReutilizados>)prod.htProductosReutilizados[linea.CodigoProducto];
            foreach (ConnectaLib.Producto.sProductosReutilizados pr in prod.listsProductosReutilizados)
            {
                if (fechaFra >= pr.FechaInicial && fechaFra <= pr.FechaFinal)
                    linea.CodigoProducto = pr.codigoNuevo;
            }            
        }
        //Seguno comprobamos producto
        bool isOK = false;        
        codigoProducto = "";
        productoCalculaCantidades = true;
        productoFijaUMGestion = false;
        prod.CodigoProducto = "";
        prod.DescripcionProducto = "";
        if (prod.htProductos.ContainsKey(linea.CodigoProducto))
        {
            ConnectaLib.Producto.sProductos p = (ConnectaLib.Producto.sProductos)prod.htProductos[linea.CodigoProducto];
            prod.CodigoProducto = p.idcProducto;
            prod.DescripcionProducto = p.descripcion;
            isOK = true;
            codigoProducto = p.idcProducto;
            //flags
            productoCalculaCantidades = p.indCalculaCantidades != "N";
            productoFijaUMGestion = p.indFijaUMGestion == "S";
            productoEsKit = p.indEsKit;
            idcCodigoFabricante = p.idcFabricante;
            if (productoEsKit == "D") idcCodigoFabricante = ObtenerIdcFabricanteProductoKit(db, linea);
        }
        else
        {
            if (prod.ComprobarCodigoSubProducto(db, codigoDistribuidor, linea.CodigoProducto))
            {
                isOK = true;
                codigoProducto = prod.CodigoProducto;
                //flags
                prod.ObtenerFlagsProducto(db, codigoProducto);
                productoCalculaCantidades = prod.FlagCalculaCantiades;
                productoFijaUMGestion = prod.FlagFijaUMGestion;
                productoEsKit = prod.FlagEsKit;
                idcCodigoFabricante = prod.IdcFabricante;
                if (productoEsKit == "D") idcCodigoFabricante = ObtenerIdcFabricanteProductoKit(db, linea);
            }            
        }        
        return isOK;
    }

    private string ObtenerIdcFabricanteProductoKit(Database db, RecordLineasFacturasDistribuidor linea)
    {        
        DbDataReader cursor = null;
        string idcFabricante = "";
        try
        {
            string sql = "SELECT TOP 1 idcFabricante " +
	                     "  FROM productosAgentesKits pak " +
	                     "LEFT JOIN productos p on pak.IdcProductoComponente = p.idcProducto " +
	                     " WHERE IdcAgente=" + agent + " and CodigoProductoKit=" + db.ValueForSql(linea.CodigoProducto) +
                         " ORDER BY ProporcionImporteComponente DESC";   
            cursor = db.GetDataReader(sql);
            if (cursor.Read()) idcFabricante = db.GetFieldValue(cursor, 0);                       
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
        return idcFabricante;
    }

    /// <summary>
    /// Marcar un producto como no obsoleto en ProductosAgentesNoExistentes
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="linea">registro de línea de factura</param>
    /// <returns>true si es correcto</returns>
    private int MarcarProductoComoNoObsoletoEnNoExistentes(Database db, RecordLineasFacturasDistribuidor linea)
    {
        int result = prod.MarcarProductoComoNoObsoletoEnNoExistentes(db, GetSipTypeName(), codigoDistribuidor, linea.CodigoProducto);
        if (result == 0 || result == 1) 
        {
            //Necesitamos obtener el fabricante de ProductosAgentesNoExistentes
            //(en el caso de que result == 2 no hace falta porque significa que el producto no està en productosAgentesnoexistentes)
            idcCodigoFabricante = prod.ObtenerFabricanteDeProductosAgentesNoExistentes(db, codigoDistribuidor, linea.CodigoProducto);
            descripcionProducto = prod.ObtenerDescripcionDeProductosAgentesNoExistentes(db, codigoDistribuidor, linea.CodigoProducto);
        }
        return result;
    }

    /// <summary>
    /// Inicializar valores log
    /// </summary>
    public void SetLogs(string type, string codigoAlerta, string msg, params string[] valores)
    {
        SetLogs(type, codigoAlerta, msg, valores, null, null);
    }
    public void SetLogs(string type, string codigoAlerta, string msg, string[] valores, string[] claves, string[] clavesExt)
    {
        logs.type = type;
        logs.codigoAlerta = codigoAlerta;
        logs.myAlertMsg = msg;        
        logs.valores = valores;
        logs.claves = claves;
        logs.clavesExt = clavesExt;
        logs.pk = logs.codigoAlerta + ";";
        logs.count = 1;
        foreach (string v in valores)
            logs.pk += (v + ";");
        if (!htLogs.ContainsKey(logs.pk))
            htLogs.Add(logs.pk, logs);
        else
        {
            Logs logsAux = (Logs)htLogs[logs.pk];
            logsAux.count = logsAux.count + 1;
            htLogs[logs.pk] = logsAux;
        }
    }

    /// <summary>
    /// Escribir logs de forma masiva
    /// </summary>
    public void WriteLogs()
    {
        Log2 log = Globals.GetInstance().GetLog2();
        foreach (DictionaryEntry item in htLogs)
        {
            Logs logs = (Logs)item.Value;            
            if (logs.type == Constants.LOGS_TRACE)
                log.Trace(logs.agent, logs.sipType, logs.codigoAlerta, logs.myAlertMsg, logs.count, logs.valores, logs.claves, logs.clavesExt);           
        }
        htLogs.Clear();
    }

    /// <summary>
    /// Recuperar dirección del clente
    /// </summary>
    /// <param name="db"></param>
    /// <param name="IdcCliente"></param>
    /// <param name="linea"></param>
    /// <returns></returns>
    private bool RecuperarDireccionCliente(Database db, string IdcCliente, RecordLineasFacturasDistribuidor linea)
    {     
        datosDireccion.Reset();      
        if (!Utils.IsBlankField(linea.NumEntrega) && !Utils.IsBlankField(linea.EjercicioEntrega))
        {
            if (htAlbaranes.ContainsKey(linea.NumEntrega + ";" + linea.EjercicioEntrega))
            {
                Albaranes alb = (Albaranes)htAlbaranes[linea.NumEntrega + ";" + linea.EjercicioEntrega];
                datosDireccion.Direccion = alb.direccionEntrega;
                datosDireccion.TipoCalle = alb.tipoCalle;
                datosDireccion.Calle = alb.calle;
                datosDireccion.Numero = alb.numero;
                datosDireccion.CodigoPostal = alb.codigoPostal;
                datosDireccion.Provincia = alb.provincia;
                datosDireccion.CodigoPais = alb.codigoPais;
                datosDireccion.Poblacion = alb.poblacion;
                datosDireccion.direccionEncontrada = true;   
            }        
        }

        if (!datosDireccion.direccionEncontrada && !Utils.IsBlankField(IdcCliente))
        {
            if (cli.htClientesFinales.ContainsKey(IdcCliente))
            {
                ConnectaLib.Cliente.sClientesFinales c = (ConnectaLib.Cliente.sClientesFinales)cli.htClientesFinales[IdcCliente];
                datosDireccion.Direccion = c.direccion;
                datosDireccion.TipoCalle = c.tipoCalle;
                datosDireccion.Calle = c.calle;
                datosDireccion.Numero = c.numero;
                datosDireccion.CodigoPostal = c.codigoPostal;
                datosDireccion.Provincia = c.provincia;
                datosDireccion.CodigoPais = c.codigoPais;
                datosDireccion.Poblacion = c.poblacion;
                datosDireccion.direccionEncontrada = true;
            }          
        }            
        return datosDireccion.direccionEncontrada;
    }

    /// <summary>
    /// Proceso del objeto (que puede venir de cualquier fuente de información) y 
    /// lógica de negocio asociada.
    /// </summary>
    /// <param name="pd">objeto</param>
    public void ProcessSlave(CommonRecord rec)
    {       
        RecordLineasFacturasDistribuidor linea = (RecordLineasFacturasDistribuidor)rec;
        string sql = "";
        Log2 log = Globals.GetInstance().GetLog2();
        Database db = Globals.GetInstance().GetDatabase();
        DbDataReader cursor = null;

        resultadoSlave = false;

        //En el caso del XML, puede que en las líneas no venga ni el número de factura ni el ejercicio ni el fabricante. 
        //Por lo tanto, lo tomamos del registro de cabecera.
        //Esto no pasará en el delimited porque las líneas ya contienen dicha información.
        try
        {
            if (linea.NumFactura == null)
            {
                RecordFacturasDistribuidor factura = (RecordFacturasDistribuidor)linea.GetParent();
                if (factura != null)
                {
                    linea.NumFactura = factura.NumFactura;
                    linea.Ejercicio = factura.Ejercicio;
                }
            }

            if (Utils.IsBlankField(codigoDistribuidor)) codigoDistribuidor = agent;

            if (Utils.IsBlankField(linea.TipoDatoRegistro)) linea.TipoDatoRegistro= "A";

            if (linea.NumFactura.StartsWith(Constants.INDICADOR_RESET))
            {
                //Si en el número de factura tenemos la palabra clave RESET significa que tenemos que inicializar/eliminar líneas de factura
                //En tal caso tenemos la fecha de inicio del periodo a eliminar en el Ejercicio y la fecha final en NumLinea.
                string idcFabricante = BuscaFabricante(linea.CodigoProducto.Trim());
                string msg = "Indicador de RESET de línea detectado en " + slaveTitle + ". " +
                    "Se eliminarán las líneas de factura del agente " + codigoDistribuidor +
                    (Utils.IsBlankField(idcFabricante) ? "" : " y del fabricante " + idcFabricante) +
                    " desde la fecha (" + linea.Ejercicio + ") hasta la fecha (" + linea.NumLinea + ").";                
                string tipo = linea.NumFactura.Replace(Constants.INDICADOR_RESET, "").Trim();
                if (!string.IsNullOrEmpty(tipo))
                    msg += " Sólo las facturas que empiecen por número de factura: " + tipo;
                log.Info(agent, GetSipTypeName(), msg);
                BorrarLineasFacturasPorPeriodo(db, codigoDistribuidor, linea.Ejercicio, linea.NumLinea, idcFabricante, tipo);

                //Si el distribuidor tiene liquidaciones activas también debemos intentar hacer reset de las liquidaciones
                if (!Utils.IsBlankField(idcFabricante))
                {
                    fabDistLiquidacionesAcuerdosActivas = dist.ObtenerParametro_LiquidacionesAcuerdosActivas(db, codigoDistribuidor, idcFabricante);
                    if (fabDistLiquidacionesAcuerdosActivas && (linea.TipoDatoRegistro == "L" || linea.TipoDatoRegistro == "A") )
                    {
                        BorrarLineasLiquidacionesPorPeriodo(db, codigoDistribuidor, linea.Ejercicio, linea.NumLinea, idcFabricante);
                    }
                }
                return;
            }
            else
            {
                //Comprobar el código postal (TENEMOS QUE HACERLO AQUÍ PORQUE ES NECESARIO EL CODIGO POSTAL PARA LA FUNCIÓN ObtenerDireccion()
                if (!codPostal.TratarCodigoPostal(linea.CodigoPostal, agentLocation))
                {
                    string myAlertMsg = "Aviso en " + slaveTitle + ". Código postal ({0}) erroneo para la factura {1} línea {2}.";
                    SetLogs(Constants.LOGS_TRACE, "FADI0019", myAlertMsg, linea.CodigoPostal, linea.NumFactura, linea.NumLinea);
                }
                
                //Si los campos de código de cliente o fecha factura de la línea NO están informados, entonces
                //se accede a la cabecera de factura para obtenerlos, sino se obtienen de la línea.
                if (Utils.IsBlankField(linea.CodigoCliente) || Utils.IsBlankField(linea.FechaFra))
                {
                    //Gestionar una variable centinela para detectar y agrupar todas las líneas de un mismo factura
                    if (!NumFactura.Equals(linea.NumFactura) || !ejercicio.Equals(linea.Ejercicio))
                    {
                        //Comprobar la existencia de la cabecera de la factura 
                        sql = "Select IdcClienteFinal,FechaFra,CodigoCliente from FacturasCF " +
                                    "Where IdcAgente = " + codigoDistribuidor +
                                    " And NumFactura = '" + linea.NumFactura + "' And  Ejercicio = '" + linea.Ejercicio + "'";
                        cursor = db.GetDataReader(sql);
                        if (!cursor.Read())
                        {
                            MarcarFacturaErronea();
                            string myAlertMsg = "Error en " + slaveTitle + ". No existe la cabecera para la línea de factura {0} línea {1}.";
                            log.Trace(agent, GetSipTypeName(), "FADI0004", myAlertMsg, linea.NumFactura, linea.NumLinea);
                            if (cursor != null)
                                cursor.Close();
                            return;
                        }

                        //La factura existe...obtener codigo de cliente y fecha de factura
                        NumFactura = linea.NumFactura;
                        ejercicio = linea.Ejercicio;
                        idcClienteFinal = db.GetFieldValue(cursor, 0);
                        fechaFactura = db.GetFieldValue(cursor, 1);
                        codigoCliente = db.GetFieldValue(cursor, 2);

                        cursor.Close();
                        cursor = null;

                        //Optimización... si el cliente es el mismo, no hacer nada, mantener los valores
                        if (ultimoCodigoCliente.Equals("") || !ultimoCodigoCliente.Equals(codigoCliente))
                        {
                            //Tratar dirección
                            ObtenerDireccion(db, linea);

                            ultimoCodigoCliente = codigoCliente;
                        }
                    }
                }
                else
                {
                    //Cargar los datos que sea necesario y obtener el cliente
                    NumFactura = linea.NumFactura;
                    ejercicio = linea.Ejercicio;
                    codigoCliente = linea.CodigoCliente;
                    fechaFactura = linea.FechaFra;

                    //Comprobar la fecha factura
                    if (Utils.IsBlankField(fechaFactura))
                    {
                        SetLogs(Constants.LOGS_TRACE, "FADI0005", "Error en " + slaveTitle + ". Fecha factura vacía.");                                                                                                                       
                        return;
                    }
                    if (db.GetDate(fechaFactura) == null)
                    {
                        SetLogs(Constants.LOGS_TRACE, "FADI0006", "Error en " + slaveTitle + ". Fecha factura {0} errónea.", fechaFactura);                             
                        return;
                    }

                    //Optimización... si el cliente es el mismo, no hacer nada, mantener los valores
                    if (ultimoCodigoCliente.Equals("") || !ultimoCodigoCliente.Equals(codigoCliente))
                    {
                        idcClienteFinal = ""; //La función ComprobarCliente lo cargará
                        //Comprobar el código de cliente que nos ha pasado el distribuidor
                        string codigoClienteCopia = codigoCliente;
                        if (!ComprobarCliente(db, codigoCliente))
                        {
                            if (dist.DISTEstado == Constants.ESTADO_ACTIVO)
                            {
                                //Si el agente está activo creamos el cliente automáticamente
                                //Crearemos un nuevo registro en ClientesFinales                    

                                //No está dado de alta en Connecta...
                                Int32 clienteFinal = 0;
                                //////////////////////////Crearemos un nuevo registro en la tabla Agentes
                                //////////////////////////Obtenemos el identificador que le ha asignado
                                ////////////////////////clienteFinal = cli.CreaAgente(db, agent, GetSipTypeName());

                                cli.CreaClienteFinal(db, Constants.CLIENTES_DISTR_SIN_DESCRIPCION + " " + codigoClienteCopia,
                                    string.Empty, string.Empty, Constants.CLIENTES_DISTR_SIN_DIRECCION + " " + codigoClienteCopia,
                                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                                    string.Empty, string.Empty, string.Empty, string.Empty, agent, agentLocation, GetSipTypeName());

                                //Obtener código de cliente...
                                //Obtenemos el identificador que le ha asignado
                                clienteFinal = cli.ObtenerUltimoIdcClienteFinal(db);            

                                //Daremos de alta un nuevo registro en CFAgentes        
                                cli.CreaCFAgentes(db, clienteFinal, codigoClienteCopia, Constants.CLIENTES_DISTR_SIN_DESCRIPCION + " " + codigoClienteCopia, 
                                    string.Empty, Constants.ESTADO_ACTIVO,
                                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                                    agent, GetSipTypeName(), "S",
                                    string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);

                                idcClienteFinal = clienteFinal.ToString();
                                codigoCliente = codigoClienteCopia;                               

                                SetLogs(Constants.LOGS_TRACE, "FADI0028", "Aviso en " + slaveTitle + ". Se ha creado automáticamente el cliente {0} de la factura {1}.", codigoClienteCopia, linea.NumFactura);       

                                ConnectaLib.Cliente.sCFAgente cf = new ConnectaLib.Cliente.sCFAgente();
                                cf.idcClienteFinal = clienteFinal.ToString();
                                cf.codigoCliente = codigoCliente;
                                cf.nombreCliente = Constants.CLIENTES_DISTR_SIN_DESCRIPCION + " " + codigoCliente;
                                cf.estadoCliente = Constants.ESTADO_ACTIVO;
                                cf.estadoManualCliente = "";
                                cf.excluirDeLiquidacion = false;                                
                                if (!cli.htCFAgentes.ContainsKey(cf.codigoCliente))
                                    cli.htCFAgentes.Add(cf.codigoCliente, cf);

                                ConnectaLib.Cliente.sClientesFinales c = new ConnectaLib.Cliente.sClientesFinales();
                                c.direccion = Constants.CLIENTES_DISTR_SIN_DIRECCION + " " + codigoCliente;
                                c.tipoCalle = "";
                                c.calle = "";
                                c.numero = "";
                                c.codigoPostal = "";
                                c.provincia = "";
                                c.codigoPais = "";
                                c.poblacion = "";
                                if (!cli.htClientesFinales.ContainsKey(cf.idcClienteFinal))
                                    cli.htClientesFinales.Add(cf.idcClienteFinal, c);
                            }
                            else
                            {
                                ultimoCodigoCliente = "";
                                MarcarFacturaErronea();
                                SetLogs(Constants.LOGS_TRACE, "FADI0007", "Error en " + slaveTitle + ". No existe el cliente {0} en la factura {1}.", codigoClienteCopia, linea.NumFactura);       
                                return;
                            }
                        }

                        //Tratar dirección
                        ObtenerDireccion(db, linea);

                        ultimoCodigoCliente = codigoCliente;
                    }
                }
            }

            //Comprobar que NumFactura, Ejercicio i NumLinea no esten vacíos
            if (Utils.IsBlankField(linea.NumFactura))
            {
                MarcarFacturaErronea();                
                SetLogs(Constants.LOGS_TRACE, "FADI0008", "Error en " + slaveTitle + ". Número de factura vacío");           
                return;
            }
            if (Utils.IsBlankField(linea.Ejercicio))
            {
                MarcarFacturaErronea();
                SetLogs(Constants.LOGS_TRACE, "FADI0009", "Error en " + slaveTitle + ". Ejercicio vacío");           
                return;
            }
            if (Utils.IsBlankField(linea.NumLinea))
            {
                MarcarFacturaErronea();
                SetLogs(Constants.LOGS_TRACE, "FADI0010", "Error en " + slaveTitle + ". Número de línea vacío");           
                return;
            }

            //Comprobar el código de producto que nos ha pasado el distribuidor
            //(Si OK entonces carga algunos flags, com por ejemplo el idcCodigoFabricante, que es importante
            //, si no OK intenteremos obtener este flag de ProductosAgentesNoExistentes por que lo necesitamos para el ControlDatosEnvios)
            if (!ComprobarCodigoProducto(db, linea))
            {
                //Es posible que el producto esté en ProductosAgentesNoExistentes,pendiente de ser alineado o marcado
                //como obsoleto, si ha llegado una línea de factura, significa que no debería ser obsoleto y por
                //lo tanto le quitamos la marca de obsoleto.
                int iRes = MarcarProductoComoNoObsoletoEnNoExistentes(db, linea);
                if (iRes == 0 || iRes == 1)
                {
                    if (iRes == 0)
                    {
                        string myAlertMsg = "Error en " + slaveTitle + ". Producto {0}-{4} del fabricante {3} está pendiente de alinear para la factura {1} línea {2}.";
                        string[] aValores = new string[] { linea.CodigoProducto, linea.NumFactura, linea.NumLinea, idcCodigoFabricante, descripcionProducto };
                        string[] aClaves = new string[] { linea.CodigoProducto, codigoDistribuidor, idcCodigoFabricante };
                        string[] aClavesExt = new string[] { linea.CodigoProducto, codigoDistribuidor, idcCodigoFabricante, linea.NumFactura, linea.NumLinea };
                        SetLogs(Constants.LOGS_TRACE, "FADI0011", myAlertMsg, aValores, aClaves, aClavesExt);           
                    }
                    else
                    {
                        string myAlertMsg = "Aviso en " + slaveTitle + ". Producto {0}-{4} del fabricante {3} está marcado como obsoleto fijo para la factura {1} línea {2}.";                        
                        SetLogs(Constants.LOGS_TRACE, "FADI0012", myAlertMsg, linea.CodigoProducto, linea.NumFactura, linea.NumLinea, idcCodigoFabricante, descripcionProducto);
                    }
                    
                    //Insertar el línea de factura en la tabla de productos factura con errores
                    InsertarProductoFacturaConErrores(db, linea);
                    
                    //Marcar los datos para el control de envíos
                    //ATENCIÓN: se debe hacer justo en este punto, cuanto antes posible siempre y que ja tengamos el fabricante i la fecha.
                    //Lo que hayan dado error anteriormente no contabilizarán para el control de envíos
                    //Y solo si el idcCodigoFabricante no es vacío
                    if (!Utils.IsBlankField(idcCodigoFabricante))
                    {
                        MarcarControlDatosEnvios(agent, idcCodigoFabricante, db.GetDate(fechaFactura).ToString());
                    }
                }
                else //iRes == 2
                {
                    string myAlertMsg = "Error en " + slaveTitle + ". Código de producto {0} no existe para la factura {1} línea {2}.";
                    string[] aValores = new string[] { linea.CodigoProducto, linea.NumFactura, linea.NumLinea };
                    string[] aClaves = new string[] { linea.CodigoProducto, codigoDistribuidor };
                    string[] aClavesExt = new string[] { linea.CodigoProducto, codigoDistribuidor, linea.NumFactura, linea.NumLinea };                    
                    SetLogs(Constants.LOGS_TRACE, "FADI0013", myAlertMsg, aValores, aClaves, aClavesExt);           
                }

                MarcarFacturaErronea();
                return;
            }           

            //Cargar una serie de parámetros del fabricante para gestionar los bloqueos y otros
            //Optimización... si el fabricante es el mismo, no hacer nada, mantener los valores
            if (centinelaIdcCodigoFabricante.Equals("") || !centinelaIdcCodigoFabricante.Equals(idcCodigoFabricante))
            {
                fab.ObtenerParametros(db, idcCodigoFabricante);

                centinelaIdcCodigoFabricante = idcCodigoFabricante;

                ajustarUMPorPrecio = dist.ObtenerParametro_AjustarUMPorPrecio(db, codigoDistribuidor, idcCodigoFabricante);
                fabDistAsignaUMDefecto = dist.ObtenerParametro_AsignaUMDefecto(db, codigoDistribuidor, idcCodigoFabricante);

                fechaInicioCargarFacturasDist = dist.ObtenerParametro_FechaInicioCargarFacturasDist(db, codigoDistribuidor, idcCodigoFabricante);

                fabDistVentasActivas = dist.ObtenerParametro_VentasActivas(db, codigoDistribuidor, idcCodigoFabricante);
                fabDistLiquidacionesAcuerdosActivas = dist.ObtenerParametro_LiquidacionesAcuerdosActivas(db, codigoDistribuidor, idcCodigoFabricante);
                fabDistLiquidacionesAcuerdosAjustarPorPuntoVerde = dist.ObtenerParametro_LiquidacionesAcuerdosAjustarPorPuntoVerde(db, codigoDistribuidor, idcCodigoFabricante);
                fabDistResetAutomatico = dist.ObtenerParametro(db, codigoDistribuidor, idcCodigoFabricante, "Fac_ResetAutomatico").ToUpper().Trim();
                fabDistResetPorNumeroFactura = dist.ObtenerParametro(db, codigoDistribuidor, idcCodigoFabricante, "Fac_ResetPorNumeroFactura").ToUpper().Trim();
                prodNoTratarKit = fab.FABProdNoTratarKits;
                if (Utils.IsBlankField(prodNoTratarKit)) prodNoTratarKit = dist.ObtenerParametro(db, codigoDistribuidor, idcCodigoFabricante, "Prod_NoTratarKit").ToUpper().Trim();
            }

            DateTime dtFechaFactura = (DateTime)db.GetDate(fechaFactura); //OJO que la función GetDate() puede devolver nulo pero en este caso aseguramos que no pues ya se ha comprobado arriba
            //Si hay fecha inicial de carga de facturas informada según relación fabricante - distribuidor validamos que la factura en curso sea posterior a dicha fecha, en caso contrario no integramos la factura actual.
            DateTime dtIniCarga;
            if (!Utils.IsBlankField(fechaInicioCargarFacturasDist) && DateTime.TryParse(fechaInicioCargarFacturasDist, out dtIniCarga) && dtFechaFactura < dtIniCarga)
            {
                string myAlertMsg = "Aviso en " + slaveTitle + ". Línea de factura con fecha {4} anterior a la fecha inicial de carga de facturas {5} según el fabricante {0} y el distribuidor {1}. La factura {2} línea {3} no será integrada.";
                SetLogs(Constants.LOGS_TRACE, "FADI0030", myAlertMsg, idcCodigoFabricante, codigoDistribuidor, linea.NumFactura, linea.NumLinea, fechaFactura, fechaInicioCargarFacturasDist);      
                return;
            }
            //Si la fecha de factura es posterior a la fecha actual + 7 dias, entonces no integramos esa factura
            DateTime dtFinCarga = DateTime.Now.AddDays(7);
            if (dtFechaFactura > dtFinCarga)
            {
                string myAlertMsg = "Aviso en " + slaveTitle + ". Línea de factura con fecha {4} posterior a la fecha {5} según el fabricante {0} y el distribuidor {1}. La factura {2} línea {3} no será integrada.";                
                SetLogs(Constants.LOGS_TRACE, "FADI0032", myAlertMsg, idcCodigoFabricante, codigoDistribuidor, linea.NumFactura, linea.NumLinea, fechaFactura, dtFinCarga.ToShortDateString());      
                return;
            }

            //Marcar los datos para el control de envíos
            //ATENCIÓN: se debe hacer justo en este punto, cuanto antes posible siempre y que ja tengamos el fabricante i la fecha.
            //Lo que hayan dado error anteriormente no contabilizarán para el control de envíos
            MarcarControlDatosEnvios(agent, idcCodigoFabricante, db.GetDate(fechaFactura).ToString());

            //Gestionar una SEGUNDA VARIABLE CENTINELA para detectar cambio de fabricante o número de factura o ejercicio
            if (!ultimoFabricante.Equals(idcCodigoFabricante) || !ultimoNumFactura.Equals(linea.NumFactura) || !ultimoEjercicio.Equals(linea.Ejercicio))
            {
                //Hacer reset por numero de factura
                //ATENCIÓN: Hacerlo justo en este punto cuando ya tenemos el valor del fabricante.
                if (fabDistResetPorNumeroFactura == "S")
                {
                    BorrarLineasFacturasPorNumFactura(db, idcCodigoFabricante, codigoDistribuidor, linea.NumFactura, linea.Ejercicio);
                }

                //Refrescar la variable centinela 2
                ultimoFabricante = idcCodigoFabricante;
                ultimoNumFactura = linea.NumFactura;
                ultimoEjercicio = linea.Ejercicio;
            }

            string where1 = "where IdcFabricante=" + idcCodigoFabricante +
                " and IdcAgente=" + codigoDistribuidor +
                " and NumFactura=" + db.ValueForSql(linea.NumFactura) +
                " and Ejercicio=" + db.ValueForSql(linea.Ejercicio);
            string where2 = " and NumLinea=" + db.ValueForSqlAsNumeric(linea.NumLinea);
            string where = where1 + where2;

            //Si el fabricante lo requiere, comprobar que la factura no haya sido integrada e imputada a facturas previamente
            if (fab.FABFacCtrlFacturasImputadas == "S")
            {
                sql = "select IdcAgente from ProductosFacturas " + where + " And IndLinFacturaImputada='S'";
                if (Utils.RecordExist(sql))
                {
                    string myAlertMsg = "Aviso en " + slaveTitle + ". Línea de factura ya imputada. El fabricante {0} no admite su modificación una vez imputada. La factura {1} línea {2} no será integrada.";
                    SetLogs(Constants.LOGS_TRACE, "FADI0015", myAlertMsg, idcCodigoFabricante, linea.NumFactura, linea.NumLinea);
                    return;
                }
            }

            //Comprobar la cantidad (si es vacia generar un aviso)
            if (Utils.IsBlankField(linea.Cantidad) || Double.Parse(linea.Cantidad) == 0)
            {
                if (Utils.IsBlankField(linea.PrecioBrutoTotal) || Double.Parse(linea.PrecioBrutoTotal) == 0)
                {
                    string myAlertMsg = "Aviso en " + slaveTitle + ". Cantidad e importe total son 0 en la factura {0} línea {1}.";
                    SetLogs(Constants.LOGS_TRACE, "FADI0016", myAlertMsg, linea.NumFactura, linea.NumLinea);
                    //Se sigue...
                }
                else
                {
                    string myAlertMsg = "Aviso en " + slaveTitle + ". Cantidad 0 e importe total diferente 0 en la factura {0} línea {1}.";
                    SetLogs(Constants.LOGS_TRACE, "FADI0031", myAlertMsg, linea.NumFactura, linea.NumLinea);
                    //Se sigue...
                }
            }

            //Comprobar la unidad de medida
            if (!unidadMedida.ComprobarUnidadMedida(db, codigoDistribuidor, codigoProducto, linea.UM, linea.CodigoProducto, idcCodigoFabricante, ((fab.FABAsignaUMDefecto == "S" || fabDistAsignaUMDefecto == "S") ? "S" : "N"), productoFijaUMGestion))
            {
                MarcarFacturaErronea();
                string myAlertMsg = "Error en " + slaveTitle + ". Unidad de medida {0} no existe para la factura {1} línea {2}.";
                string[] aValores = new string[] { linea.UM, linea.NumFactura, linea.NumLinea };
                string[] aClaves = new string[] { linea.UM, codigoDistribuidor };
                string[] aClavesExt = new string[] { linea.UM, codigoDistribuidor, linea.NumFactura, linea.NumLinea };                
                SetLogs(Constants.LOGS_TRACE, "FADI0017", myAlertMsg, aValores, aClaves, aClavesExt);
                return;
            }
            UMProducto = unidadMedida.UMProducto;
            
            //Ajustar por precio la UM si es necesario
            if (ajustarUMPorPrecio)
            {
                UMProducto = unidadMedida.AjustarUnidadMedidaPorPrecio(db, codigoProducto, idcCodigoFabricante, linea.Cantidad, linea.PrecioBase, linea.Descuentos, linea.PrecioBrutoTotal, UMProducto);
            }

            //Tratar cantidad y precio complementario
            if (!Utils.IsBlankField(linea.UM2) && (!Utils.IsBlankField(linea.Cantidad2) || !Utils.IsBlankField(linea.PrecioBase2)))
            {
                if (!unidadMedida.ComprobarUnidadMedida(db, codigoDistribuidor, codigoProducto, linea.UM2, linea.CodigoProducto, idcCodigoFabricante, ((fab.FABAsignaUMDefecto == "S" || fabDistAsignaUMDefecto == "S") ? "S" : "N"), productoFijaUMGestion))
                {
                    MarcarFacturaErronea();
                    //log.DetailedError(agent, GetSipTypeName(), "Error en " + slaveTitle + ". Unidad de medida complementaria " + linea.UM2 + " no existe para la factura " + linea.NumFactura + " línea " + linea.NumLinea);
                    string myAlertMsg = "Error en " + slaveTitle + ". Unidad de medida complementaria {0} no existe para la factura {1} línea {2}.";
                    string[] aValores = new string[] { linea.UM2, linea.NumFactura, linea.NumLinea };
                    string[] aClaves = new string[] { linea.UM2, codigoDistribuidor };
                    string[] aClavesExt = new string[] { linea.UM2, codigoDistribuidor, linea.NumFactura, linea.NumLinea };                    
                    SetLogs(Constants.LOGS_TRACE, "FADI0018", myAlertMsg, aValores, aClaves, aClavesExt);
                    return;
                }
                UMProducto2 = unidadMedida.UMProducto;

                //Conversión de la cantidad complementario, se suma a la cantidad.
                if (!Utils.IsBlankField(linea.Cantidad2))
                {
                    linea.Cantidad = ConvertirCantidadComplementaria(db, linea);
                }

                //Conversión del precio base complementario, sustituye al precio base.
                if (!Utils.IsBlankField(linea.PrecioBase2))
                {
                    linea.PrecioBase = ConvertirPrecioBaseComplementario(db, linea);
                }
            }

            ///LO HEMOS SUBIDO MÁS ARRIBA EN ESTA MISMA FUNCIÓN
            ////////////////////////Comprobar el código postal
            //////////////////////if (!codPostal.TratarCodigoPostal(linea.CodigoPostal, agentLocation))
            //////////////////////{
            //////////////////////    string myAlertMsg = "Aviso en " + slaveTitle + ". Código postal ({0}) erroneo para la factura {1} línea {2}.";                
            //////////////////////    SetLogs(Constants.LOGS_TRACE, "FADI0019", myAlertMsg, linea.CodigoPostal, linea.NumFactura, linea.NumLinea);
            //////////////////////}

            //Comprobar tipo venta
            if (!Utils.IsBlankField(linea.TipoVenta))
            {
                if (!ComprobarEntidadClasificada(db, "TIPOVENTA", linea.TipoVenta))
                {
                    InsertarEntidadClasificada(db, "TIPOVENTA", linea.TipoVenta, "Tipo venta " + linea.TipoVenta);
                    //Globals.GetInstance().GetLog().Warning(agent, GetSipTypeName(), "Aviso en " + slaveTitle + ". Tipo de venta (" + linea.TipoVenta + ") no encontrado como entidad TIPOVENTA para la factura " + linea.NumFactura + " línea " + linea.NumLinea + ". Ha sido insertado de forma automática como entidad clasificada.");
                    string myAlertMsg = "Aviso en " + slaveTitle + ". Tipo de venta ({0}) no encontrado como entidad TIPOVENTA para la factura {1} línea {2}. Ha sido insertado de forma automática como entidad clasificada.";
                    SetLogs(Constants.LOGS_TRACE, "FADI0020", myAlertMsg, linea.TipoVenta, linea.NumFactura, linea.NumLinea);

                    string clave = "TIPOVENTA" + ";" + linea.TipoVenta;
                    if (!htClasificadores.ContainsKey(clave))
                        htClasificadores.Add(clave, "");
                }
            }

            //Comprobar motivo de abono
            if (!Utils.IsBlankField(linea.MotivoAbono))
            {
                if (!ComprobarEntidadClasificada(db, "MOTABONO", linea.MotivoAbono))
                {
                    InsertarEntidadClasificada(db, "MOTABONO", linea.MotivoAbono, "Motivo abono " + linea.MotivoAbono);
                    //Globals.GetInstance().GetLog().Warning(agent, GetSipTypeName(), "Aviso en " + slaveTitle + ". Motivo de abono (" + linea.MotivoAbono + ") no encontrado como entidad MOTABONO para la factura " + linea.NumFactura + " línea " + linea.NumLinea + ". Ha sido insertado de forma automática como entidad clasificada.");
                    string myAlertMsg = "Aviso en " + slaveTitle + ". Motivo de abono ({0}) no encontrado como entidad MOTABONO para la factura {1} línea {2}. Ha sido insertado de forma automática como entidad clasificada.";                    
                    SetLogs(Constants.LOGS_TRACE, "FADI0021", myAlertMsg, linea.MotivoAbono, linea.NumFactura, linea.NumLinea);

                    string clave = "MOTABONO" + ";" + linea.MotivoAbono;
                    if (!htClasificadores.ContainsKey(clave))
                        htClasificadores.Add(clave, "");
                }
            }

            //Comprobar código promoción
            if (!Utils.IsBlankField(linea.CodigoPromocion))
            {
                if (!ComprobarEntidadClasificada(db, "TIPOPROMO", linea.CodigoPromocion))
                {
                    InsertarEntidadClasificada(db, "TIPOPROMO", linea.CodigoPromocion, "Tipo promoción " + linea.CodigoPromocion);
                    string myAlertMsg = "Aviso en " + slaveTitle + ". Código de promoción ({0}) no encontrado como entidad TIPOPROMO para la factura {1} línea {2}. Ha sido insertado de forma automática como entidad clasificada.";
                    SetLogs(Constants.LOGS_TRACE, "FADI0022", myAlertMsg, linea.CodigoPromocion, linea.NumFactura, linea.NumLinea);

                    string clave = "TIPOPROMO" + ";" + linea.CodigoPromocion;
                    if (!htClasificadores.ContainsKey(clave))
                        htClasificadores.Add(clave, "");
                }
            }

            //RESET automático de facturas//
            //Guardamos datos por hacer RESET automático al finalizar, si flag activo
            if (fabDistResetAutomatico.Equals("S") || fabDistResetAutomatico.Equals("D"))
            {
                string fechaFacturaStr = db.GetDate(fechaFactura).ToString();
                ResetFacturas rf = new ResetFacturas();
                rf.idcDistr = codigoDistribuidor;
                rf.idcFab = idcCodigoFabricante;
                rf.tipo = fabDistResetAutomatico;
                rf.dateIni = fechaFacturaStr;
                rf.dateFin = fechaFacturaStr;
                string key = codigoDistribuidor + "_" + idcCodigoFabricante;
                if (!htResetFacturas.ContainsKey(key))
                    htResetFacturas.Add(key, rf);
                else
                {
                    ResetFacturas rfAux = (ResetFacturas)htResetFacturas[key];
                    DateTime dtAct = DateTime.Parse(fechaFacturaStr);
                    DateTime dtRFIni = DateTime.Parse(rfAux.dateIni);
                    DateTime dtRFFin = DateTime.Parse(rfAux.dateFin);
                    if (dtAct < dtRFIni)
                        rfAux.dateIni = fechaFacturaStr;
                    if (dtAct > dtRFFin)
                        rfAux.dateFin = fechaFacturaStr;
                    htResetFacturas[key] = rfAux;
                }                               
            }

            if (!EsKit() || (EsKit() && prodNoTratarKit.Equals("S")))
            {
                //Si no es un kit el tratamiento normal se realiza para la línea que nos llega

                //Tratar volúmen
                if (!volumen.TratarVolumen(db, codigoDistribuidor, codigoProducto, linea.Volumen, linea.UMVolumen, GetSipTypeName(), linea.Cantidad, UMProducto, unidadMedida, productoCalculaCantidades))
                {
                    unidadMedida.BuscarUnidadesMedidaLogisticas(db, codigoProducto, UMProducto, codigoDistribuidor, volumen, peso, linea.Cantidad, true, false);
                }

                //Tratar peso                                                
                if (!peso.TratarPeso(db, codigoDistribuidor, codigoProducto, linea.Peso, linea.UMPeso, GetSipTypeName(), linea.Cantidad, UMProducto, unidadMedida, productoCalculaCantidades))
                {
                    unidadMedida.BuscarUnidadesMedidaLogisticas(db, codigoProducto, UMProducto, codigoDistribuidor, volumen, peso, linea.Cantidad, false, true);
                }

                //Tratar unidad estadística y su cantidad a partir de CodigoProducto, inter.Cantidad y UMc (ver anexo buscar unidades de medida estadísticas). 
                //Obtendremos las variables CantidadEstadistica y UMEstadistica, CantidadEstadistica2 y UMEstadistica2, y CantidadEstadistica3 y UMEstadistica3
                BuscarUnidadesMedidaEstadistica(db, linea, productoCalculaCantidades);

                //Guardar facturas repetidas en un mismo fichero para generar alerta en caso de que existan
                FacturasRepetidas fr = new FacturasRepetidas();
                fr.idcFab = idcCodigoFabricante;
                fr.idcDistr = codigoDistribuidor;
                fr.numFactura = linea.NumFactura;
                fr.ejercicio = linea.Ejercicio;
                fr.numLinea = linea.NumLinea;  
                fr.cont = 1;
                string key = idcCodigoFabricante + "_" + codigoDistribuidor + "_" + linea.NumFactura + "_" + linea.Ejercicio + "_" + linea.NumLinea;
                if (!htFacturas.ContainsKey(key))
                    htFacturas.Add(key, fr);
                else
                {
                    if (!htFacturasRepetidas.ContainsKey(key))
                    {
                        fr.cont++;
                        htFacturasRepetidas.Add(key, fr);
                    }
                    else
                    {
                        FacturasRepetidas frAux = (FacturasRepetidas)htFacturasRepetidas[key];
                        frAux.cont++;
                        htFacturasRepetidas[key] = frAux;
                    }
                }

                //Quan el tipus de línea sigui V o A, només inserim si tenim vendes actives o liquidaciones actives o les vendes i liquidacions no estan actives (això es pels casos en que estem fent proves de un distribuidor)
                if ((fabDistVentasActivas || fabDistLiquidacionesAcuerdosActivas || (!fabDistVentasActivas && !fabDistLiquidacionesAcuerdosActivas)) && (linea.TipoDatoRegistro == "V" || linea.TipoDatoRegistro == "A")) 
                {
                    bool lineaExiste = false;
                    if (fab.FABProdKitsActivos != "S")
                    {
                        sql = "select IdcAgente from ProductosFacturas " + where;
                        if (Utils.RecordExist(sql)) lineaExiste = true;
                    }
                    else
                    {
                        //A partir del número de línea recibido generamos el número de línea de los componentes añadiendo decimales
                        string numLineaBase = "";
                        string whereTmp = "";
                        int i;
                        if (linea.NumLinea.IndexOf(",") != -1)
                        {
                            numLineaBase = db.ValueForSqlAsNumeric((Double.Parse(linea.NumLinea) * 1000).ToString());
                            if (int.TryParse(linea.NumLinea.Substring(linea.NumLinea.IndexOf(",") + 1), out i) && i != 0)
                                whereTmp = where1 + " and convert(bigint,(round(NumLinea,2)*1000))=" + numLineaBase;
                            else
                                whereTmp = where1 + " and convert(bigint,NumLinea)*1000=" + numLineaBase;     
                        }
                        else
                        {
                            numLineaBase = db.ValueForSqlAsNumeric(linea.NumLinea);
                            whereTmp = where1 + " and convert(bigint,NumLinea)=" + numLineaBase;
                        }
                        sql = "select ProductoKitDistribuidor from ProductosFacturas " + whereTmp;
                        cursor = db.GetDataReader(sql);
                        if (cursor.Read())
                        {
                            if (!Utils.IsBlankField(db.GetFieldValue(cursor, 0)))
                            {
                                //estamos insertando una línea que ya existe y que ahora no es kit pero antes si lo era, tenemos que borrar sus componentes
                                //Eliminamos el desglose de líneas que ya existen de este kit
                                sql = "delete from ProductosFacturas " + whereTmp;
                                db.ExecuteSql(sql, agent, GetSipTypeName());
                            }
                            else
                            {
                                lineaExiste = true;
                            }
                        }
                        cursor.Close();
                        cursor = null;
                    }
                    if (lineaExiste)
                    {
                        ActualizarProductosFacturas(db, linea, where);
                    }
                    else
                    {
                        if (!modeBulk)
                            InsertarProductosFacturas(db, linea);
                        else
                            doBulk = true;
                    }
                }

                //Quan el tipus de línea sigui L o A, només inserim si tenim liquidaciones actives o les vendes y liquidaciones no estan actives (això es pels casos en que estem fent proves de un distribuidor)
                if ((fabDistLiquidacionesAcuerdosActivas || (!fabDistVentasActivas && !fabDistLiquidacionesAcuerdosActivas)) && (linea.TipoDatoRegistro == "L" || linea.TipoDatoRegistro == "A"))
                {
                    //Si tenim liquidacions actives aleshores inseriem les dades de la línea de factura com una línea de liquidació.
                    GrabarLiquidacion(db, linea, productoCalculaCantidades);
                }
            }
            else
            {
                //Si es un kit el tratamiento de peso, volumen, cantidades estadisticas e inserció se debe realizar para cada uno de los componenetes del kit

                //A partir del número de línea recibido generamos el número de línea de los componentes                
                string numLineaBase = string.Empty;                
                int i;
                if (linea.NumLinea.IndexOf(",") != -1)
                {
                    numLineaBase = db.ValueForSqlAsNumeric((Double.Parse(linea.NumLinea) * 1000).ToString());
                    if (int.TryParse(linea.NumLinea.Substring(linea.NumLinea.IndexOf(",") + 1), out i) && i != 0)                    
                        where = where1 + " and convert(bigint,(round(NumLinea,2)*1000))=" + numLineaBase;                    
                    else                    
                        where = where1 + " and convert(bigint,NumLinea)*1000=" + numLineaBase;                    
                }
                else
                {
                    numLineaBase = db.ValueForSqlAsNumeric(linea.NumLinea);
                    where = where1 + " and convert(bigint,NumLinea)=" + numLineaBase;
                }

                //Eliminamos el desglose de líneas que ya existen de este kit
                sql = "delete from ProductosFacturas " + where;
                db.ExecuteSql(sql, agent, GetSipTypeName());

                //SVO 7-11-2013 (Esto era antes): Calcular la cantidad estadistica del kit convirtiendo de manera fija (por ahora) a UDK. Su valor se almacenará en el campo CantEstadisticaKit.
                //SVO 7-11-2013 (Esto era antes): BuscarUnidadesMedidaEstadisticaKit_OLD(db, linea.NumFactura, linea.NumLinea, linea.CodigoProducto, UMProducto, linea.Cantidad, productoCalculaCantidades);
                //SVO 7-11-2013 (Esto es ahora): Calcular la cantidad estadistica del kit convirtiendo a la UM base del producto. Su valor se almacenará en el campo CantEstadisticaKit.
                if (Utils.StringToDouble(linea.Cantidad) == 0) cantEstadisticaKit = "0";
                else
                {
                    BuscarUnidadesMedidaEstadisticaKit(db, linea.NumFactura, linea.NumLinea, linea.CodigoProducto, UMProducto, linea.Cantidad);
                    if (Utils.IsBlankField(cantEstadisticaKit)) cantEstadisticaKit = "1";
                }
                Double nCantEstadisticaKit = Utils.StringToDouble(cantEstadisticaKit);

                //Si es un kit de fabricante obtenemos el código de producto según el fabricante
                if (EsKitFabricante()) ObtenerCodigoProductoFabricante(db);
                
                //Obtenemos el importe total de los componentes del kit
                ObtenerImporteNumeroTotalComponentesKit(db, linea);

                //Obtenemos los componentes del kit y recorremos en un bucle cada uno de los componentes                
                sql = "SELECT IdcAgente " +
                      "      ,CodigoProductoKit " +
                      "      ,IdcProductoComponente " +
                      "      ,UMcComponente " +
                      "      ,CantidadComponente " +
                      "      ,ProporcionImporteComponente " +
                      "      ,FechaInsercion " +
                      "      ,FechaModificacion " +
                      "      ,UsuarioInsercion " +
                      "      ,UsuarioModificacion " +
                      "      ,IndCalculaCantidades " +
                      "  FROM ProductosAgentesKits " +
                      " INNER JOIN Productos ON ProductosAgentesKits.IdcProductoComponente = Productos.IdcProducto " +
                      //" WHERE IdcAgente = " + agent +
                      " WHERE IdcAgente = " + (EsKitFabricante() ? idcCodigoFabricante : agent) +
                      "  AND CodigoProductoKit = " + (EsKitFabricante() ? db.ValueForSql(codigoProdFab) : db.ValueForSql(linea.CodigoProducto)) + 
                      " ORDER BY ProporcionImporteComponente ";

                DbDataReader reader = null;
                try
                {
                    Producto prodComponente = new Producto();

                    int numLineaExt = 1;
                    int ix = linea.NumLinea.IndexOf(",");
                    if (ix != -1)
                    {
                        string sParteEntera = linea.NumLinea.Substring(0, ix);
                        numLineaBase = sParteEntera;
                        string sParteDecimal = linea.NumLinea.Substring(ix + 1);
                        if (Int32.Parse(sParteDecimal) != 0)
                        {
                            string sPrimerDecimal = sParteDecimal.Substring(0, 1);
                            if (Int32.Parse(sPrimerDecimal) > 0)
                            {
                                numLineaExt = Int32.Parse(sPrimerDecimal + "01");
                            }
                            else
                            {
                                numLineaExt = Int32.Parse(sParteDecimal) + 1;
                            }
                        }
                    }
                    else
                    {
                        numLineaBase = db.ValueForSqlAsNumeric(linea.NumLinea);
                    }

                    Double nProporcionImporteTotalKit = Double.Parse(proporcionImporteTotalKit);
                    
                    Double nPrecioBase = 0;
                    Double.TryParse(linea.PrecioBase, out nPrecioBase);
                    
                    Double nDescuentos = 0;
                    Double.TryParse(linea.Descuentos, out nDescuentos);
                    
                    Double nPrecioBrutoTotal = 0;
                    Double.TryParse(linea.PrecioBrutoTotal, out nPrecioBrutoTotal);
                    
                    Double nPrecioBrutoTotalAcum = 0;
                    Double nDescuentosAcum = 0;
                    Double nPrecioBaseAcum = 0;
                    
                    string UMProductoComponente = "";
                    bool productoCalculaCantidadesComponente = true;
                    codigoProductoComponenteKitDist = "";

                    bool hayComponentesKit = false;

                    reader = db.GetDataReader(sql);
                    while (reader.Read())
                    {
                        hayComponentesKit = true;

                        numLinea = numLineaBase + "." + numLineaExt.ToString().PadLeft(3, '0');
                        
                        codigoProducto = db.GetFieldValue(reader, 2);
                        //Para obtener el código de producto del componente según el distribuidor utilizamos la función ObtenerCodigoProductoFabricante() 
                        //que no se corresponde según el nombre pero nos va bien de todas maneras
                        prodComponente.ObtenerCodigoProductoFabricante(db, codigoDistribuidor, codigoProducto);
                        codigoProductoComponenteKitDist = prodComponente.CodigoProdFab;
                        if (Utils.IsBlankField(codigoProductoComponenteKitDist))
                        {
                            codigoProductoComponenteKitDist = "UNKNOWN";
                        }
                        UMProductoComponente = db.GetFieldValue(reader, 3);
                        productoCalculaCantidadesComponente = (db.GetFieldValue(reader, 10) != "N");

                        Double nProporcionImporte = 0;
                        Double.TryParse(db.GetFieldValue(reader, 5), out nProporcionImporte);                        

                        Double nCantidadComponente = 0;
                        Double.TryParse(db.GetFieldValue(reader, 4), out nCantidadComponente);

                        //SVO 7-11-2013: En este punto muy importante:
                        //SVO 7-11-2013: Multiplicar la cantidad del componente por la cantidad estadistica kit, 
                        //SVO 7-11-2013: que es la cantidad que ha llegado en la línea kit convertida a la um base del producto
                        nCantidadComponente = nCantidadComponente * nCantEstadisticaKit;

                        //Calculamos el precio, descuentos e importe del componente en base a la proporción de importe indicada en la definición del kit
                        precioBase = "0";
                        descuentos = "0";
                        precioBrutoTotal = "0";
                        if (numeroComponentesKit == numLineaExt.ToString())
                        {
                            //Si estamos en el último componente calculamos diferencia con el acumulado
                            precioBase = (nPrecioBase - nPrecioBaseAcum).ToString();
                            descuentos = (nDescuentos - nDescuentosAcum).ToString();
                            precioBrutoTotal = (nPrecioBrutoTotal - nPrecioBrutoTotalAcum).ToString();
                        }
                        else
                        {
                            if (nProporcionImporteTotalKit != 0)
                            {
                                //SVO 7-11-2013: precioBase = (((nPrecioBase * nProporcionImporte) / nProporcionImporteTotalKit) / nCantidadComponente).ToString();
                                //SVO 7-11-2013: descuentos = (((nDescuentos * nProporcionImporte) / nProporcionImporteTotalKit) / nCantidadComponente).ToString();
                                //SVO 7-11-2013: precioBrutoTotal = ((Double.Parse(precioBase) - Double.Parse(descuentos)) * (nCantidadComponente * nCantEstadisticaKit)).ToString();
                                precioBrutoTotal = ((nPrecioBrutoTotal * nProporcionImporte) / nProporcionImporteTotalKit).ToString();
                                precioBase = (((nPrecioBase * nProporcionImporte) / nProporcionImporteTotalKit)).ToString();
                                descuentos = (((nDescuentos * nProporcionImporte) / nProporcionImporteTotalKit)).ToString();

                                nPrecioBrutoTotalAcum += Double.Parse(precioBrutoTotal);
                                nPrecioBaseAcum += Double.Parse(precioBase);
                                nDescuentosAcum += Double.Parse(descuentos);
                            }
                        }

                        //Tratar volúmen
                        if (!volumen.TratarVolumen(db, codigoDistribuidor, codigoProducto, "", "", GetSipTypeName(), nCantidadComponente.ToString(), UMProductoComponente, unidadMedida, productoCalculaCantidadesComponente))
                        {
                            unidadMedida.BuscarUnidadesMedidaLogisticas(db, codigoProducto, UMProductoComponente, codigoDistribuidor, volumen, peso, nCantidadComponente.ToString(), true, false);
                        }

                        //Tratar peso
                        if (!peso.TratarPeso(db, codigoDistribuidor, codigoProducto, "", "", GetSipTypeName(), nCantidadComponente.ToString(), UMProductoComponente, unidadMedida, productoCalculaCantidadesComponente))
                        {
                            unidadMedida.BuscarUnidadesMedidaLogisticas(db, codigoProducto, UMProductoComponente, codigoDistribuidor, volumen, peso, nCantidadComponente.ToString(), false, true);
                        }

                        //Tratar unidad estadística y su cantidad a partir de CodigoProducto, inter.Cantidad y UMc (ver anexo buscar unidades de medida estadísticas). 
                        //Obtendremos las variables CantidadEstadistica y UMEstadistica, CantidadEstadistica2 y UMEstadistica2, y CantidadEstadistica3 y UMEstadistica3
                        BuscarUnidadesMedidaEstadistica(db, linea.NumFactura, numLinea, codigoProductoComponenteKitDist, UMProductoComponente, nCantidadComponente.ToString(), productoCalculaCantidadesComponente);

                        //insertar la sublínea en la tabla                        
                        //Quan el tipus de línea sigui V o A, només inserim si tenim vendes actives o liquidaciones actives o les vendes i liquidacions no estan actives (això es pels casos en que estem fent proves de un distribuidor)
                        if ((fabDistVentasActivas || fabDistLiquidacionesAcuerdosActivas || (!fabDistVentasActivas && !fabDistLiquidacionesAcuerdosActivas)) && (linea.TipoDatoRegistro == "V" || linea.TipoDatoRegistro == "A")) 
                        {
                            InsertarProductosFacturas(db, linea);
                        }

                        //Quan el tipus de línea sigui L o A, només inserim si tenim liquidaciones actives o les vendes y liquidaciones no estan actives (això es pels casos en que estem fent proves de un distribuidor)
                        if (fabDistLiquidacionesAcuerdosActivas && (linea.TipoDatoRegistro == "L" || linea.TipoDatoRegistro == "A"))
                        {
                            //Si tenim liquidacions actives aleshores inseriem les dades de la línea de factura com una línea de liquidació.
                            GrabarLiquidacion(db, linea, productoCalculaCantidadesComponente);
                        }

                        numLineaExt++;
                    }
                    reader.Close();
                    reader = null;

                    if (!hayComponentesKit)
                    {
                        string myAlertMsg = "Error en " + slaveTitle + ". Producto {0}-{4} del fabricante {3} es un kit y no tiene componentes para la factura {1} línea {2}.";
                        string[] aValores = new string[] { linea.CodigoProducto, linea.NumFactura, linea.NumLinea, idcCodigoFabricante, descripcionProducto };
                        string[] aClaves = new string[] { linea.CodigoProducto, codigoDistribuidor, idcCodigoFabricante };
                        string[] aClavesExt = new string[] { linea.CodigoProducto, codigoDistribuidor, idcCodigoFabricante, linea.NumFactura, linea.NumLinea };
                        log.Trace(agent, GetSipTypeName(), "FADI0035", myAlertMsg, aValores, aClaves, aClavesExt);

                        //Insertar el línea de factura en la tabla de productos factura con errores
                        InsertarProductoFacturaConErrores(db, linea);

                        MarcarFacturaErronea();
                        return;
                    }
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                }
            }
            //Además, para asegurar el tiro, se borra el registro que potencialmente 
            //pueda existir en la tabla ProductosFacturasConErrores
            BorrarProductoFacturaConErrores(db, linea);

            MarcarFacturaRecibida(agent, idcCodigoFabricante);

            mbulk = linea;
            resultadoSlave = true;
        }
        finally 
        {
            if (cursor != null)
                cursor.Close();
        }
    }

    private void InsertarProductosFacturas(Database db, RecordLineasFacturasDistribuidor linea)
    {        
        //Insertar la línea de factura
        string sql = "";
        if (!EsKit() || (EsKit() && prodNoTratarKit.Equals("S")))
        {
            sql = "insert into ProductosFacturas (IdcAgente,NumFactura,Ejercicio" +
                  ",NumLinea,IdcClienteFinal,FechaFactura,IdcProducto" +
                  ",Cantidad,UMc,Peso,UMcPeso,Volumen,UMcVolumen" +
                  ",PrecioBase,Descuentos,PrecioBrutoTotal" +
                  ",FechaEntrega,Almacen,Lote,FechaCaducidad,CodigoPostal" +
                  ",TipoCalle,Calle,Numero,Provincia,CodigoPais" +
                  ",Ruta,CodigoComercial,EjercicioEntrega,NumEntrega,NumLineaEntrega" +
                  ",CosteDistribuidor,DocRelacionado,Direccion" +
                  ",CantEstadistica,UMEstadistica" +
                  ",CantEstadistica2,UMEstadistica2" +
                  ",CantEstadistica3,UMEstadistica3" +
                  ",ProductoDistribuidor,UMDistribuidor,Poblacion,Status" +
                  ",IdcFabricante,CodigoCliente" +
                  ",TipoVenta,MotivoAbono,CodigoPromocion" +
                  ",Libre1,Libre2,Libre3" +
                  ",CantLibre1,CantLibre2,CantLibre3" +
                  ",PesoCalculado, VolumenCalculado" +
                  ") " +
                  "values(" +
                  codigoDistribuidor +
                  "," + db.ValueForSql(linea.NumFactura) +
                  "," + db.ValueForSql(linea.Ejercicio) +
                  "," + db.ValueForSqlAsNumeric(linea.NumLinea) +
                  "," + idcClienteFinal +
                  "," + db.DateForSql(fechaFactura) +
                  "," + codigoProducto +
                  "," + db.ValueForSqlAsNumeric(linea.Cantidad) +
                  "," + db.ValueForSql(UMProducto) +
                  "," + db.ValueForSqlAsNumeric(peso.PesoTotal + "") +
                  "," + db.ValueForSql(peso.UMcPeso) +
                  "," + db.ValueForSqlAsNumeric(volumen.VolumenTotal + "") +
                  "," + db.ValueForSql(volumen.UMcVolumen) +
                  "," + db.ValueForSqlAsNumeric(linea.PrecioBase) +
                  "," + db.ValueForSqlAsNumeric(linea.Descuentos) +
                  "," + db.ValueForSqlAsNumeric(linea.PrecioBrutoTotal) +
                  "," + db.DateForSql(linea.FechaEntrega) +
                  "," + db.ValueForSql(linea.Almacen) +
                  "," + db.ValueForSql(linea.Lote) +
                  "," + db.DateForSql(linea.FechaCaducidad) +
                  "," + db.ValueForSql(CodigoPostal) +
                  "," + db.ValueForSql(TipoCalle) +
                  "," + db.ValueForSql(Calle.ToUpper()) +
                  "," + db.ValueForSql(Numero) +
                  "," + db.ValueForSql(Provincia) +
                  "," + db.ValueForSql(CodigoPais) +
                  "," + db.ValueForSql(linea.Ruta) +
                  "," + db.ValueForSql(linea.CodigoComercial) +
                  "," + db.ValueForSql(linea.EjercicioEntrega) +
                  "," + db.ValueForSql(linea.NumEntrega) +
                  "," + db.ValueForSqlAsNumeric(linea.NumLineaEntrega) +
                  "," + db.ValueForSqlAsNumeric(linea.CosteDistribuidor) +
                  "," + db.ValueForSql(linea.DocRelacionado) +
                  "," + db.ValueForSql(Direccion.ToUpper()) +
                  "," + db.ValueForSqlAsNumeric(CantidadEstadistica) +
                  "," + db.ValueForSql(UMEstadistica) +
                  "," + db.ValueForSqlAsNumeric(CantidadEstadistica2) +
                  "," + db.ValueForSql(UMEstadistica2) +
                  "," + db.ValueForSqlAsNumeric(CantidadEstadistica3) +
                  "," + db.ValueForSql(UMEstadistica3) +
                  "," + db.ValueForSql(linea.CodigoProducto) +
                  "," + db.ValueForSql(linea.UM) +
                  "," + db.ValueForSql(datosDireccion.Poblacion.ToUpper()) +
                  "," + db.ValueForSql(Constants.ESTADO_ACTIVO) +
                  "," + idcCodigoFabricante +
                  "," + db.ValueForSql(codigoCliente) +
                  "," + db.ValueForSql(linea.TipoVenta) +
                  "," + db.ValueForSql(linea.MotivoAbono) +
                  "," + db.ValueForSql(linea.CodigoPromocion) +
                  "," + db.ValueForSql(linea.Libre1) +
                  "," + db.ValueForSql(linea.Libre2) +
                  "," + db.ValueForSql(linea.Libre3) +
                  "," + db.ValueForSqlAsNumeric(linea.CantLibre1) +
                  "," + db.ValueForSqlAsNumeric(linea.CantLibre2) +
                  "," + db.ValueForSqlAsNumeric(linea.CantLibre3) +
                  "," + (peso.HaSidoCalculado ? db.ValueForSql("S") : db.ValueForSql("N")) +
                  "," + (volumen.HaSidoCalculado ? db.ValueForSql("S") : db.ValueForSql("N")) +
                  ")";
        }
        else
        {
            sql = "insert into ProductosFacturas (IdcAgente,NumFactura,Ejercicio" +
                  ",NumLinea,IdcClienteFinal,FechaFactura,IdcProducto" +
                  ",Cantidad,UMc,Peso,UMcPeso,Volumen,UMcVolumen" +
                  ",PrecioBase,Descuentos,PrecioBrutoTotal" +
                  ",FechaEntrega,Almacen,Lote,FechaCaducidad,CodigoPostal" +
                  ",TipoCalle,Calle,Numero,Provincia,CodigoPais" +
                  ",Ruta,CodigoComercial,EjercicioEntrega,NumEntrega,NumLineaEntrega" +
                  ",CosteDistribuidor,DocRelacionado,Direccion" +
                  ",CantEstadistica,UMEstadistica" +
                  ",CantEstadistica2,UMEstadistica2" +
                  ",CantEstadistica3,UMEstadistica3" +
                  ",ProductoDistribuidor,UMDistribuidor,Poblacion,Status" +
                  ",IdcFabricante,CodigoCliente" +
                  ",TipoVenta,MotivoAbono,CodigoPromocion" +
                  ",Libre1,Libre2,Libre3" +
                  ",CantLibre1,CantLibre2,CantLibre3" +
                  ",CantEstadisticaKit" +
                  ",ProductoKitDistribuidor" +
                  ",PrecioBaseKit" +
                  ",DescuentosKit" +
                  ",PrecioBrutoTotalKit" +
                  ",PesoCalculado, VolumenCalculado" +
                  ") " +
                  "values(" +
                  codigoDistribuidor +
                  "," + db.ValueForSql(linea.NumFactura) +
                  "," + db.ValueForSql(linea.Ejercicio) +
                  "," + db.ValueForSqlAsNumeric(numLinea) +
                  "," + idcClienteFinal +
                  "," + db.DateForSql(fechaFactura) +
                  "," + codigoProducto +
                  "," + db.ValueForSqlAsNumeric(linea.Cantidad) +
                  "," + db.ValueForSql(UMProducto) +
                  "," + db.ValueForSqlAsNumeric(peso.PesoTotal + "") +
                  "," + db.ValueForSql(peso.UMcPeso) +
                  "," + db.ValueForSqlAsNumeric(volumen.VolumenTotal + "") +
                  "," + db.ValueForSql(volumen.UMcVolumen) +
                  "," + db.ValueForSqlAsNumeric(precioBase) +
                  "," + db.ValueForSqlAsNumeric(descuentos) +
                  "," + db.ValueForSqlAsNumeric(precioBrutoTotal) +
                  "," + db.DateForSql(linea.FechaEntrega) +
                  "," + db.ValueForSql(linea.Almacen) +
                  "," + db.ValueForSql(linea.Lote) +
                  "," + db.DateForSql(linea.FechaCaducidad) +
                  "," + db.ValueForSql(CodigoPostal) +
                  "," + db.ValueForSql(TipoCalle) +
                  "," + db.ValueForSql(Calle.ToUpper()) +
                  "," + db.ValueForSql(Numero) +
                  "," + db.ValueForSql(Provincia) +
                  "," + db.ValueForSql(CodigoPais) +
                  "," + db.ValueForSql(linea.Ruta) +
                  "," + db.ValueForSql(linea.CodigoComercial) +
                  "," + db.ValueForSql(linea.EjercicioEntrega) +
                  "," + db.ValueForSql(linea.NumEntrega) +
                  "," + db.ValueForSqlAsNumeric(linea.NumLineaEntrega) +
                  "," + db.ValueForSqlAsNumeric(linea.CosteDistribuidor) +
                  "," + db.ValueForSql(linea.DocRelacionado) +
                  "," + db.ValueForSql(Direccion.ToUpper()) +
                  "," + db.ValueForSqlAsNumeric(CantidadEstadistica) +
                  "," + db.ValueForSql(UMEstadistica) +
                  "," + db.ValueForSqlAsNumeric(CantidadEstadistica2) +
                  "," + db.ValueForSql(UMEstadistica2) +
                  "," + db.ValueForSqlAsNumeric(CantidadEstadistica3) +
                  "," + db.ValueForSql(UMEstadistica3) +
                  "," + db.ValueForSql(codigoProductoComponenteKitDist) + 
                  "," + db.ValueForSql(linea.UM) +
                  "," + db.ValueForSql(datosDireccion.Poblacion.ToUpper()) +
                  "," + db.ValueForSql(Constants.ESTADO_ACTIVO) +
                  "," + idcCodigoFabricante +
                  "," + db.ValueForSql(codigoCliente) +
                  "," + db.ValueForSql(linea.TipoVenta) +
                  "," + db.ValueForSql(linea.MotivoAbono) +
                  "," + db.ValueForSql(linea.CodigoPromocion) +
                  "," + db.ValueForSql(linea.Libre1) +
                  "," + db.ValueForSql(linea.Libre2) +
                  "," + db.ValueForSql(linea.Libre3) +
                  "," + db.ValueForSqlAsNumeric(linea.CantLibre1) +
                  "," + db.ValueForSqlAsNumeric(linea.CantLibre2) +
                  "," + db.ValueForSqlAsNumeric(linea.CantLibre3) +
                  "," + db.ValueForSqlAsNumeric(cantEstadisticaKit) +
                  "," + db.ValueForSql(linea.CodigoProducto) +
                  "," + db.ValueForSqlAsNumeric(linea.PrecioBase) +                    
                  "," + db.ValueForSqlAsNumeric(linea.Descuentos) +
                  "," + db.ValueForSqlAsNumeric(linea.PrecioBrutoTotal) +
                  "," + (peso.HaSidoCalculado ? db.ValueForSql("S") : db.ValueForSql("N")) +
                  "," + (volumen.HaSidoCalculado ? db.ValueForSql("S") : db.ValueForSql("N")) +
                  ")";
        }
        db.ExecuteSql(sql, agent, GetSipTypeName());
        return;
    }

    private void ActualizarProductosFacturas(Database db, RecordLineasFacturasDistribuidor linea, string where)
    {
        //Actualizar la línea de factura
        string sql = "update ProductosFacturas set " +
              "IdcClienteFinal=" + idcClienteFinal +
              ",FechaFactura=" + db.DateForSql(fechaFactura) +
              ",IdcProducto=" + codigoProducto +
              ",Cantidad=" + db.ValueForSqlAsNumeric(linea.Cantidad) +
              ",UMc=" + db.ValueForSql(UMProducto) +
              ",Peso=" + db.ValueForSqlAsNumeric(peso.PesoTotal + "") +
              ",UMcPeso=" + db.ValueForSql(peso.UMcPeso) +
              ",Volumen=" + db.ValueForSqlAsNumeric(volumen.VolumenTotal + "") +
              ",UMcVolumen=" + db.ValueForSql(volumen.UMcVolumen) +
              ",PrecioBase=" + db.ValueForSqlAsNumeric(linea.PrecioBase) +
              ",Descuentos=" + db.ValueForSqlAsNumeric(linea.Descuentos) +
              ",PrecioBrutoTotal=" + db.ValueForSqlAsNumeric(linea.PrecioBrutoTotal) +
              ",FechaEntrega=" + db.DateForSql(linea.FechaEntrega) +
              ",Almacen=" + db.ValueForSql(linea.Almacen) +
              ",Lote=" + db.ValueForSql(linea.Lote) +
              ",FechaCaducidad=" + db.DateForSql(linea.FechaCaducidad) +
              ",CodigoPostal=" + db.ValueForSql(CodigoPostal) +
              ",TipoCalle=" + db.ValueForSql(TipoCalle) +
              ",Calle=" + db.ValueForSql(Calle.ToUpper()) +
              ",Numero=" + db.ValueForSql(Numero) +
              ",Provincia=" + db.ValueForSql(Provincia) +
              ",CodigoPais=" + db.ValueForSql(CodigoPais) +
              ",Ruta=" + db.ValueForSql(linea.Ruta) +
              ",CodigoComercial=" + db.ValueForSql(linea.CodigoComercial) +
              ",EjercicioEntrega=" + db.ValueForSql(linea.EjercicioEntrega) +
              ",NumEntrega=" + db.ValueForSql(linea.NumEntrega) +
              ",NumLineaEntrega=" + db.ValueForSqlAsNumeric(linea.NumLineaEntrega) +
              ",CosteDistribuidor=" + db.ValueForSqlAsNumeric(linea.CosteDistribuidor) +
              ",DocRelacionado=" + db.ValueForSql(linea.DocRelacionado) +
              ",Direccion=" + db.ValueForSql(Direccion.ToUpper()) +
              ",CantEstadistica=" + db.ValueForSqlAsNumeric(CantidadEstadistica) +
              ",UMEstadistica=" + db.ValueForSql(UMEstadistica) +
              ",CantEstadistica2=" + db.ValueForSqlAsNumeric(CantidadEstadistica2) +
              ",UMEstadistica2=" + db.ValueForSql(UMEstadistica2) +
              ",CantEstadistica3=" + db.ValueForSqlAsNumeric(CantidadEstadistica3) +
              ",UMEstadistica3=" + db.ValueForSql(UMEstadistica3) +
              ",ProductoDistribuidor=" + db.ValueForSql(linea.CodigoProducto) +
              ",UMDistribuidor=" + db.ValueForSql(linea.UM) +
              ",Poblacion=" + db.ValueForSql(datosDireccion.Poblacion.ToUpper()) + " " +
              ",FechaModificacion=" + db.SysDate() + " " +
              ",Status=" + db.ValueForSql(Constants.ESTADO_ACTIVO) + " " +
            //",IdcFabricante=" + idcCodigoFabricante + " " +
              ",CodigoCliente=" + db.ValueForSql(codigoCliente) + " " +
              ",TipoVenta=" + db.ValueForSql(linea.TipoVenta) + " " +
              ",MotivoAbono=" + db.ValueForSql(linea.MotivoAbono) + " " +
              ",CodigoPromocion=" + db.ValueForSql(linea.CodigoPromocion) + " " +
              ",Libre1=" + db.ValueForSql(linea.Libre1) + " " +
              ",Libre2=" + db.ValueForSql(linea.Libre2) + " " +
              ",Libre3=" + db.ValueForSql(linea.Libre3) + " " +
              ",CantLibre1=" + db.ValueForSqlAsNumeric(linea.CantLibre1) + " " +
              ",CantLibre2=" + db.ValueForSqlAsNumeric(linea.CantLibre2) + " " +
              ",CantLibre3=" + db.ValueForSqlAsNumeric(linea.CantLibre3) + " " +
              ",PesoCalculado=" + (peso.HaSidoCalculado ? db.ValueForSql("S") : db.ValueForSql("N")) + " " +
              ",VolumenCalculado=" + (volumen.HaSidoCalculado ? db.ValueForSql("S") : db.ValueForSql("N")) + " " +
              where;

        db.ExecuteSql(sql, agent, GetSipTypeName());
        return;
    }

    private void GrabarLiquidacion(Database db, RecordLineasFacturasDistribuidor linea, bool pProdCalculaCantidades)
    {
        bool bLiqOk = true;
        string key = "";
        string sql = "";
        string sWhereLiq = "";
        DbDataReader reader = null;
        string fecFacturaString = "";
        string numLiquidacion = "";
        double numLineaLiq = 0;
        string codProdFab = "";
        string codProdFabAgrup = "";
        string UMc = "";
        string LiqCantidad = "";
        string LiqUM = "";
        string LiqCantidadEstadistica = "";
        string LiqUMEstadistica = "";
        double cantVtaWrk = 0;
        string LiqPrecioCompra = "";
        double impVtaWrk = 0;
        string LiqMargen = "", LiqMargen2 = "", LiqMargen3 = "";
        string idcTipoAcuerdo = "", idcTipoAcuerdo2 = "", idcTipoAcuerdo3 = "";
        string TipoAcuerdoFab = "", TipoAcuerdoFab2 = "", TipoAcuerdoFab3 = "";
        string puntoVerde = "";
        string umPuntoVerde = "";
        string LiqCantidadRegalada = "";


        //Mirar si tenemos los mínimos datos de liquidación.
        if (!(!Utils.IsBlankField(fechaFactura) && db.GetDate(fechaFactura) != null)) bLiqOk = false;
        if (Utils.IsBlankField(linea.NumFactura) || Utils.IsBlankField(linea.Ejercicio) || Utils.IsBlankField(linea.NumLinea)) bLiqOk = false;
        if (Utils.IsBlankField(idcClienteFinal)) bLiqOk = false;
        if (Utils.IsBlankField(codigoProducto)) bLiqOk = false;
        if (Utils.IsBlankField(linea.ImporteLiq) && Utils.IsBlankField(linea.ImporteLiq2) && Utils.IsBlankField(linea.ImporteLiq3)) bLiqOk = false;

        if (excluirClienteDeLiquidacion) bLiqOk = false;
        
        if (bLiqOk)
        {
            //Calcular el número de liquidación. Se calcula a partir de la fecha de factura (AAAAMM)
            fecFacturaString = db.GetDate(fechaFactura).ToString();
            numLiquidacion = DateTime.Parse(fecFacturaString).Year.ToString() + DateTime.Parse(fecFacturaString).Month.ToString().PadLeft(2, '0');

            sWhereLiq = "L.IdcFabricante = " + idcCodigoFabricante + " AND L.IdcDistribuidor = " + codigoDistribuidor + " AND L.NumLiquidacion = '" + numLiquidacion + "' ";

            //Registrar los datos de cabecera para que después en el postproces se procese.
            key = idcCodigoFabricante + "_" + codigoDistribuidor + "_" + numLiquidacion;
            if (!htLiquidaciones.ContainsKey(key))
            {
                CabeceraLiquidacion cl = new CabeceraLiquidacion();
                cl.ok = true;
                cl.idcFab = idcCodigoFabricante;
                cl.idcDistr = codigoDistribuidor;
                cl.numLiq = numLiquidacion;

                //Mirar si existe la liquidación en BD, para obtener el número de línea mayor y obtener también el estado
                sql = "SELECT L2.FechaLiquidacion, L2.FechaDesde, L2.FechaHasta, L2.DescLiquidacion, L2.Status, MAX(L.NumLinea) " +
                        " FROM LiquidacionesAcuerdosDetalle L " +
                        " LEFT JOIN LiquidacionesAcuerdos L2 " +
                        "   ON L.IdcFabricante=L2.IdcFabricante " +
                        "   AND L.IdcDistribuidor=L2.IdcDistribuidor " +
                        "   AND L.NumLiquidacion=L2.NumLiquidacion " +
                        " WHERE " + sWhereLiq +
                        " GROUP BY L2.FechaLiquidacion, L2.FechaDesde, L2.FechaHasta, L2.DescLiquidacion, L2.Status";
                reader = db.GetDataReader(sql);
                bool existeLiq = false;
                existeLiq = reader.Read();
                if (!existeLiq || Utils.IsBlankField(db.GetFieldValue(reader, 0))) //si la fecha liquidación està vacia significa que aun no tenemos la cabecera
                {
                    cl.fecLiq = DateTime.Now.ToShortDateString();
                    cl.fecDesde = DateTime.Parse(fecFacturaString).Year.ToString() + "/" + DateTime.Parse(fecFacturaString).Month.ToString().PadLeft(2, '0') + "/01";
                    cl.fecHasta = DateTime.Parse(fecFacturaString).Year.ToString() + "/" + DateTime.Parse(fecFacturaString).Month.ToString().PadLeft(2, '0') + "/" + DateTime.DaysInMonth(DateTime.Parse(fecFacturaString).Year, DateTime.Parse(fecFacturaString).Month).ToString();
                    cl.descLiq = "Liquidación " + numLiquidacion;
                    cl.numLineaMax = (!existeLiq ? 0 : Utils.StringToDouble(db.GetFieldValue(reader, 5))); //si no existe inicializamos numlinea
                    cl.statusLiq = Constants.ESTADO_PENDIENTE; //si no existe inicializamos estado a pendiente
                }
                else
                {
                    cl.fecLiq = db.GetFieldValue(reader, 0);
                    cl.fecDesde = db.GetFieldValue(reader, 1);
                    cl.fecHasta = db.GetFieldValue(reader, 2);
                    cl.descLiq = db.GetFieldValue(reader, 3);
                    cl.statusLiq = db.GetFieldValue(reader, 4);
                    cl.numLineaMax = Utils.StringToDouble(db.GetFieldValue(reader, 5));
                }
                reader.Close();
                reader = null;
                
                htLiquidaciones.Add(key, cl);
            }

            //I en los datos de cabecera anotar el numero de línea mayor para saberlo para las líneas que se deban insertar
            CabeceraLiquidacion clAux = (CabeceraLiquidacion)htLiquidaciones[key];
            numLineaLiq = Math.Floor(clAux.numLineaMax);

            //I verificar que se puede grabar la liquidación en función de su estado i otras condiciones quizas.
            if (clAux.statusLiq != Constants.ESTADO_PENDIENTE && clAux.statusLiq != Constants.ESTADO_REVISAR && clAux.statusLiq != Constants.ESTADO_CONFIRMADO)
            {
                bLiqOk = false;
                clAux.ok = false;
                htLiquidaciones[key] = clAux;
            }
        }

        if (bLiqOk)
        {
            //Obtenemos algunos datos del producto que nos interesaran mas adelante.
            sql = "SELECT P.UMc, PA.Codigo, PA.CodigoAgrup, P.PuntoVerde, P.UMPuntoVerde, P.IndCalculaCantidades FROM Productos P " +
                " INNER JOIN ProductosAgentes PA " +
                " ON PA.IdcAgente = " + idcCodigoFabricante +
                "   AND P.IdcProducto = PA.IdcProducto " +
                " WHERE P.IdcProducto = " + codigoProducto;
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                UMc = db.GetFieldValue(reader, 0);
                codProdFab = db.GetFieldValue(reader, 1);
                codProdFabAgrup = db.GetFieldValue(reader, 2);
                puntoVerde = db.GetFieldValue(reader, 3);
                umPuntoVerde = db.GetFieldValue(reader, 4);
            }
            reader.Close();
            reader = null;

            //Convertir la CantidadVenta a la UM que quiere el fabricante
            if (pProdCalculaCantidades)
            {
                //TODO SVO: (ESTO DE MOMENTO LO DEJAMOS HARDCODED, MAS ADELANTE YA ENCONTRAREMOS UN MÉTODO CONFIGURABLE)
                if (idcCodigoFabricante == "112") //Si es fabricante es CODORNIU
                {
                    LiqCantidad = CantidadEstadistica2;
                    LiqUM = UMEstadistica2;
                }
                else
                {
                    LiqCantidad = CantidadEstadistica;
                    LiqUM = UMEstadistica;
                }
            }
            else
            {
                LiqCantidad = linea.Cantidad;
                LiqUM = UMProducto;
            }
            Double.TryParse(LiqCantidad, out cantVtaWrk);

            //Calcular la CantidadEstadistica a la UM que quiere el fabricante
            if (pProdCalculaCantidades)
            {
                LiqUMEstadistica = fab.FABLiqUMEstadistica;
                if (!Utils.IsBlankField(LiqUMEstadistica))
                {
                    if (!Utils.IsBlankField(UMc))
                    {
                        LiqCantidadEstadistica = CantidadConvertida(db, linea.Cantidad, UMProducto, idcCodigoFabricante, UMc, LiqUMEstadistica);
                        if (LiqCantidadEstadistica == "")
                        {
                            //Si la cantidad devuelta es vacía significa que no ha sabido encontrar la conversión.
                            //Se genera un aviso y se continua.
                            string myAlertMsg = "Aviso en " + slaveTitle + ". Conversión a UM estadística liquidación {0} erronea en factura {1} línea {2} producto {3} unidad medida {4}.";
                            string[] aValores = new string[] { UMEstadistica, linea.NumFactura, linea.NumLinea, linea.CodigoProducto, UMProducto };
                            string[] aClaves = new string[] { idcCodigoFabricante, codigoDistribuidor, codigoProducto, UMProducto, UMEstadistica };
                            string[] aClavesExt = new string[] { idcCodigoFabricante, codigoDistribuidor, codigoProducto, UMProducto, UMEstadistica, linea.NumFactura, linea.NumLinea };
                            Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "FADI0033", myAlertMsg, aValores, aClaves, aClavesExt);
                        }
                    }
                }
            }
            else
            {
                LiqCantidadEstadistica = linea.Cantidad;
                LiqUMEstadistica = UMProducto;
            }

            //Obtener el importe de venta
            Double.TryParse(linea.PrecioBrutoTotal, out impVtaWrk);

            //Ajustar el importe de venta con el punto verder si el distribuidor tiene activado el falg
            if (fabDistLiquidacionesAcuerdosAjustarPorPuntoVerde && linea.TipoDatoRegistro != "L")
            {
                //Tenemos cantVtaWrk que es la cantidad venta ja convertida a la um que queremos (que es la LiqUM).
                //Ahora tenemos que convertir el puntoVerde que està expresado en umPuntoVerde a LiqUM
                //Después multiplicar cantVtaWrk por puntoVerdeConvertido i restar el resultat de impVtaWrk
                double pvWrk = 0;
                Double.TryParse(puntoVerde, out pvWrk);
                if (pvWrk != 0)
                {
                    double umConvertidas = Double.Parse(CantidadConvertida(db, "1", LiqUM, idcCodigoFabricante, UMc, umPuntoVerde));
                    pvWrk = pvWrk * umConvertidas;
                    if (impVtaWrk != 0) impVtaWrk = impVtaWrk - (pvWrk * cantVtaWrk); //solo lo hacemos si el importe venta es diferente de 0.
                }
            }

            //Ahora que tenemos "normalizado" el importe aplicandole el coeficiente de punto verde, tratamos la cantidad regalada (si el importe es 0 es regalo)
            //La cantidad regalada la guardamos en ValorLibre7Liq
            if (impVtaWrk <= 0.1 && impVtaWrk >= -0.1)
            {
                LiqCantidadRegalada = LiqCantidad;
            }
            else
            {
                LiqCantidadRegalada = linea.ValorLibre7Liq;
                if (pProdCalculaCantidades)
                {
                    if (!Utils.IsBlankField(UMc))
                    {
                        double cantRegaladaWrk = 0;
                        Double.TryParse(linea.ValorLibre7Liq, out cantRegaladaWrk);
                        if (cantRegaladaWrk != 0)
                        { 
                            LiqCantidadRegalada = "";
                            LiqCantidadRegalada = CantidadConvertida(db, linea.ValorLibre7Liq, UMProducto, idcCodigoFabricante, UMc, LiqUM);
                            if (LiqCantidadRegalada == "")
                            {
                                //Si la cantidad devuelta es vacía significa que no ha sabido encontrar la conversión. Se genera un aviso y se continua.
                                string myAlertMsg = "Aviso en " + slaveTitle + ". Conversión cantidad regalada liquidación a UM {0} erronea en factura {1} línea {2} producto {3} unidad medida {4}.";
                                string[] aValores = new string[] { LiqUM, linea.NumFactura, linea.NumLinea, linea.CodigoProducto, UMProducto };
                                string[] aClaves = new string[] { idcCodigoFabricante, codigoDistribuidor, codigoProducto, UMProducto, LiqUM };
                                string[] aClavesExt = new string[] { idcCodigoFabricante, codigoDistribuidor, codigoProducto, UMProducto, LiqUM, linea.NumFactura, linea.NumLinea };
                                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "FADI0038", myAlertMsg, aValores, aClaves, aClavesExt);
                            }
                        }
                    }
                }
            }

            //Obtener el precio compra (ValorLibre1).
            LiqPrecioCompra = linea.ValorLibre1Liq;
            if (Utils.IsBlankField(LiqPrecioCompra) || LiqPrecioCompra.Trim() == "0")
            {
                sql = "SELECT p.Precio FROM Precios p " +
                    " LEFT JOIN ClasifInterAgentes cia " +
                    " ON p.IdcFabricante = cia.IdcAgenteOrigen " +
                    "   AND p.IdcDistribuidor = cia .IdcAgenteDestino " +
                    "   AND p.IdPrecio = cia.IdPrecio " +
                    " LEFT JOIN clasifinteragentes ciaAlt ON ciaAlt.idcagenteorigen = p.idcfabricante " +
                    "        AND ciaAlt.CodigoAlt = cia.Codigo " +
                    "        AND p.IdPrecio = ciaAlt.IdPrecio " +
                    " WHERE p.IdcFabricante = " + idcCodigoFabricante +
                    "   AND (p.IdcFabricante = cia.IdcAgenteOrigen OR (ciaAlt.idcagenteorigen is not null and ciaAlt.idcagenteorigen = p.idcfabricante)) " +
                    "   AND (p.IdPrecio = cia.IdPrecio OR (ciaAlt.IdPrecio is not null and ciaAlt.IdPrecio=p.IdPrecio)) " + 
                    "   AND p.TipoPrecio = 'TAR' " +
                    "   AND p.FechaIniVigencia <= " + db.DateForSql(fecFacturaString) +
                    "   AND (p.FechaFinVigencia is null or p.FechaFinVigencia >= " + db.DateForSql(fecFacturaString) + ") " +
                    "   AND (p.EjeProducto = '*' OR p.EjeProducto = " + db.ValueForSql(codProdFab) + " OR p.EjeProducto = " + db.ValueForSql(codProdFabAgrup) + ") " +
                    "   AND ((p.IdcDistribuidor = 0 OR p.IdcDistribuidor = " + codigoDistribuidor + ") " +                    
                    "        OR (ciaAlt.IdcAgenteDestino is not null and " + codigoDistribuidor + " = ciaAlt.IdcAgenteDestino)) ";
                reader = db.GetDataReader(sql);
                if (reader.Read())
                {
                    LiqPrecioCompra = db.GetFieldValue(reader, 0);
                }
                reader.Close();
                reader = null;
            }

            //Obtener y calcular el tipo de acuerdo
            idcTipoAcuerdo = "";
            TipoAcuerdoFab = "";
            ObtenerTipoAcuerdo(db, idcCodigoFabricante, linea.TipoAcuerdoLiq.Trim().ToLower(), out TipoAcuerdoFab, out idcTipoAcuerdo, 1);

            idcTipoAcuerdo2 = "";
            TipoAcuerdoFab2 = "";
            ObtenerTipoAcuerdo(db, idcCodigoFabricante, linea.TipoAcuerdoLiq2.Trim().ToLower(), out TipoAcuerdoFab2, out idcTipoAcuerdo2, 2);

            idcTipoAcuerdo3 = "";
            TipoAcuerdoFab3 = "";
            ObtenerTipoAcuerdo(db, idcCodigoFabricante, linea.TipoAcuerdoLiq3.Trim().ToLower(), out TipoAcuerdoFab3, out idcTipoAcuerdo3, 3);

            //Obtener y/o calcular el ValorLibre2 (margen)
            double impLiqWrk = 0;
            double precompWrk = 0;
            LiqMargen = linea.ValorLibre2Liq;
            LiqMargen2 = LiqMargen;
            LiqMargen3 = LiqMargen;
            if (Utils.IsBlankField(LiqMargen) || LiqMargen.Trim() == "0")
            {
                //TODO SVO: (ESTO DE MOMENTO LO DEJAMOS HARDCODED, MAS ADELANTE YA ENCONTRAREMOS UN MÉTODO CONFIGURABLE)
                if (idcCodigoFabricante == "112") //Si es fabricante es CODORNIU
                {
                    LiqMargen = "0";
                }
                else
                {
                    //La manera de calcular el margen serà a partir de precio venta, precio compra e importe a liquidar
                    //Podemos llegar a tener hasta 3 margenes
                    LiqMargen = "0";
                    impLiqWrk = Utils.StringToDouble(linea.ImporteLiq);
                    if (impLiqWrk != 0)
                    {
                        if (cantVtaWrk != 0)
                        {
                            precompWrk = Utils.StringToDouble(LiqPrecioCompra);
                            if (precompWrk != 0)
                            {
                                //[Margen] = [A liquidar unitario] + ([Precio venta unitario] - [Precio compra unitario])
                                LiqMargen = ((impLiqWrk / cantVtaWrk) + ((impVtaWrk / cantVtaWrk) - precompWrk)).ToString();
                            }
                        }
                    }

                    LiqMargen2 = "0";
                    impLiqWrk = Utils.StringToDouble(linea.ImporteLiq2);
                    if (impLiqWrk != 0)
                    {
                        if (cantVtaWrk != 0)
                        {
                            precompWrk = Utils.StringToDouble(LiqPrecioCompra);
                            if (precompWrk != 0)
                            {
                                //[Margen] = [A liquidar unitario] + ([Precio venta unitario] - [Precio compra unitario])
                                LiqMargen2 = ((impLiqWrk / cantVtaWrk) + ((impVtaWrk / cantVtaWrk) - precompWrk)).ToString();
                            }
                        }
                    }

                    LiqMargen3 = "0";
                    impLiqWrk = Utils.StringToDouble(linea.ImporteLiq3);
                    if (impLiqWrk != 0)
                    {
                        if (cantVtaWrk != 0)
                        {
                            precompWrk = Utils.StringToDouble(LiqPrecioCompra);
                            if (precompWrk != 0)
                            {
                                //[Margen] = [A liquidar unitario] + ([Precio venta unitario] - [Precio compra unitario])
                                LiqMargen3 = ((impLiqWrk / cantVtaWrk) + ((impVtaWrk / cantVtaWrk) - precompWrk)).ToString();
                            }
                        }
                    }
                }
            }

            //Insertar o actualizar las líneas de liquidacion (podemos tener hasta 3 líneas de liquidación diferentes por cada línea de factura).
            impLiqWrk = Utils.StringToDouble(linea.ImporteLiq);
            if (impLiqWrk != 0)
            {
                string sWhereLiqDet = sWhereLiq +
                    " AND TipoAcuerdoFab = " + db.ValueForSql(TipoAcuerdoFab) +
                    " AND NumFactura = " + db.ValueForSql(linea.NumFactura) +
                    " AND EjercicioFactura = " + db.ValueForSql(linea.Ejercicio) +
                    " AND NumLineaFactura = " + db.ValueForSqlAsNumeric(linea.NumLinea) +
                    " AND (NumLinea - FLOOR(NumLinea))*10=0 "; //posición 1 tendrá parte decimal igual a 0

                sql = "SELECT 1 FROM LiquidacionesAcuerdosDetalle L " +
                    " WHERE " + sWhereLiqDet;

                numLineaLiq = numLineaLiq + 1;

                string numLineaAux = numLineaLiq.ToString();
                numLineaAux = numLineaAux + ",0";

                if (Utils.RecordExist(sql))
                {
                    sql = "UPDATE LiquidacionesAcuerdosDetalle SET" +
                        " IdcClienteFinal = " + db.ValueForSqlAsNumeric(idcClienteFinal) + ", CodigoCliente = " + db.ValueForSql(codigoCliente) +
                        ", IdcTipoAcuerdo = " + db.ValueForSql(idcTipoAcuerdo) + ", TipoAcuerdo = " + db.ValueForSql(linea.TipoAcuerdoLiq) + ", SubTipoAcuerdo = " + db.ValueForSql(linea.SubtipoAcuerdoLiq) +
                        ", FechaFactura = " + db.DateForSql(fechaFactura) +
                        ", IdcProducto = " + codigoProducto + ", CodigoProducto = " + db.ValueForSql(codProdFab) + ", CodigoProductoDist = " + db.ValueForSql(linea.CodigoProducto) +
                        ", CantidadVenta = " + db.ValueForSqlAsNumeric(LiqCantidad) + ", UMc = " + db.ValueForSql(LiqUM) +
                        ", CantEstadistica = " + db.ValueForSqlAsNumeric(LiqCantidadEstadistica) + ", UMEstadistica = " + db.ValueForSql(LiqUMEstadistica) +
                        ", PrecioVenta = " + db.ValueForSqlAsNumeric((cantVtaWrk == 0) ? "0" : (impVtaWrk / cantVtaWrk).ToString()) + ", ImporteVenta = " + db.ValueForSqlAsNumeric(linea.PrecioBrutoTotal) +
                        ", ValorLibre1 = " + db.ValueForSqlAsNumeric(LiqPrecioCompra) + ", ValorLibre2 = " + db.ValueForSqlAsNumeric(LiqMargen) +
                        ", ValorLibre3 = " + db.ValueForSqlAsNumeric(linea.ValorLibre3Liq) + ", ValorLibre4 = " + db.ValueForSqlAsNumeric(linea.ValorLibre4Liq) +
                        ", ValorLibre5 = " + db.ValueForSqlAsNumeric(linea.ValorLibre5Liq) + ", ValorLibre6 = " + db.ValueForSqlAsNumeric(LiqCantidad) +
                        ", ValorLibre7 = " + db.ValueForSqlAsNumeric(LiqCantidadRegalada) + ", ValorLibre8 = " + db.ValueForSqlAsNumeric((cantVtaWrk == 0) ? "0" : (impVtaWrk / cantVtaWrk).ToString()) +
                        ", ValorLibre9 = " + db.ValueForSqlAsNumeric(LiqMargen) + ", ValorLibre10 = " + db.ValueForSqlAsNumeric(linea.ImporteLiq) +
                        ", IndLiquidar = " + db.ValueForSql("S") + ", ImporteLiquidar = " + db.ValueForSqlAsNumeric(linea.ImporteLiq) +
                        ", Observaciones = " + db.ValueForSql(linea.ObservacionesLiq) +
                        ", FechaModificacion = " + db.SysDate() + ", UsuarioModificacion = " + db.ValueForSql(Constants.PREFIJO_USUARIO_AGENTE + agent) +
                        " WHERE " + sWhereLiqDet.Replace("L.", " ");
                    db.ExecuteSql(sql, agent, GetSipTypeName());
                }
                else
                {                    
                    sql = "INSERT INTO LiquidacionesAcuerdosDetalle " +
                        "(IdcFabricante,IdcDistribuidor,NumLiquidacion,NumLinea,IdcClienteFinal,CodigoCliente" +
                        ",IdcTipoAcuerdo,TipoAcuerdo,SubTipoAcuerdo,TipoAcuerdoFab,NumFactura,EjercicioFactura,NumLineaFactura,FechaFactura" +
                        ",IdcProducto,CodigoProducto,CodigoProductoDist,CantidadVenta,UMc,CantEstadistica,UMEstadistica,PrecioVenta,ImporteVenta" +
                        ",ValorLibre1,ValorLibre2,ValorLibre3,ValorLibre4,ValorLibre5,ValorLibre6,ValorLibre7,ValorLibre8,ValorLibre9,ValorLibre10" +
                        ",IndLiquidar,ImporteLiquidar,Observaciones,UsuarioInsercion, UsuarioModificacion) values " +
                        "(" + idcCodigoFabricante + "," + codigoDistribuidor + "," + db.ValueForSql(numLiquidacion) + "," + db.ValueForSqlAsNumeric(numLineaAux) +
                        "," + idcClienteFinal + "," + db.ValueForSql(codigoCliente) +
                        "," + db.ValueForSql(idcTipoAcuerdo) + "," + db.ValueForSql(linea.TipoAcuerdoLiq) + "," + db.ValueForSql(linea.SubtipoAcuerdoLiq) + "," + db.ValueForSql(TipoAcuerdoFab) +
                        "," + db.ValueForSql(linea.NumFactura) + "," + db.ValueForSql(linea.Ejercicio) + "," + db.ValueForSqlAsNumeric(linea.NumLinea) + "," + db.DateForSql(fechaFactura) +
                        "," + codigoProducto + "," + db.ValueForSql(codProdFab) + "," + db.ValueForSql(linea.CodigoProducto) + "," + db.ValueForSqlAsNumeric(LiqCantidad) + "," + db.ValueForSql(LiqUM) +
                        "," + db.ValueForSqlAsNumeric(LiqCantidadEstadistica) + "," + db.ValueForSql(LiqUMEstadistica) + "," + db.ValueForSqlAsNumeric((cantVtaWrk == 0) ? "0" : (impVtaWrk / cantVtaWrk).ToString()) + "," + db.ValueForSqlAsNumeric(linea.PrecioBrutoTotal) +
                        "," + db.ValueForSqlAsNumeric(LiqPrecioCompra) + "," + db.ValueForSqlAsNumeric(LiqMargen) + "," + db.ValueForSqlAsNumeric(linea.ValorLibre3Liq) + "," + db.ValueForSqlAsNumeric(linea.ValorLibre4Liq) + "," + db.ValueForSqlAsNumeric(linea.ValorLibre5Liq) +
                        "," + db.ValueForSqlAsNumeric(LiqCantidad) + "," + db.ValueForSqlAsNumeric(LiqCantidadRegalada) + "," + db.ValueForSqlAsNumeric((cantVtaWrk == 0) ? "0" : (impVtaWrk / cantVtaWrk).ToString()) + "," + db.ValueForSqlAsNumeric(LiqMargen) + "," + db.ValueForSqlAsNumeric(linea.ImporteLiq) +
                        "," + db.ValueForSql("S") + "," + db.ValueForSqlAsNumeric(linea.ImporteLiq) + "," + db.ValueForSql(linea.ObservacionesLiq) + "," + db.ValueForSql(Constants.PREFIJO_USUARIO_AGENTE + agent) + "," + db.ValueForSql(Constants.PREFIJO_USUARIO_AGENTE + agent) + " )";
                    db.ExecuteSql(sql, agent, GetSipTypeName());

                    CabeceraLiquidacion clAux = (CabeceraLiquidacion)htLiquidaciones[key];
                    ++clAux.numLineaMax;
                    htLiquidaciones[key] = clAux;
                }
            }

            impLiqWrk = Utils.StringToDouble(linea.ImporteLiq2);
            if (impLiqWrk != 0)
            {
                string sWhereLiqDet = sWhereLiq +
                    " AND TipoAcuerdoFab = " + db.ValueForSql(TipoAcuerdoFab2) +
                    " AND NumFactura = " + db.ValueForSql(linea.NumFactura) +
                    " AND EjercicioFactura = " + db.ValueForSql(linea.Ejercicio) +
                    " AND NumLineaFactura = " + db.ValueForSqlAsNumeric(linea.NumLinea) +
                    " AND (NumLinea - FLOOR(NumLinea))*10=2 "; //posición 2 tendrá parte decimal igual a 2
                    //sWhereLiqDetAux;

                sql = "SELECT 1 FROM LiquidacionesAcuerdosDetalle L " +
                      " WHERE " + sWhereLiqDet;

                numLineaLiq = numLineaLiq + 1;

                string numLineaAux = numLineaLiq.ToString();
                numLineaAux = numLineaAux + ",2";

                if (Utils.RecordExist(sql))
                {
                    sql = "UPDATE LiquidacionesAcuerdosDetalle SET" +
                        " IdcClienteFinal = " + db.ValueForSqlAsNumeric(idcClienteFinal) + ", CodigoCliente = " + db.ValueForSql(codigoCliente) +
                        ", IdcTipoAcuerdo = " + db.ValueForSql(idcTipoAcuerdo2) + ", TipoAcuerdo = " + db.ValueForSql(linea.TipoAcuerdoLiq2) + ", SubTipoAcuerdo = " + db.ValueForSql(linea.SubtipoAcuerdoLiq2) +
                        ", FechaFactura = " + db.DateForSql(fechaFactura) +
                        ", IdcProducto = " + codigoProducto + ", CodigoProducto = " + db.ValueForSql(codProdFab) + ", CodigoProductoDist = " + db.ValueForSql(linea.CodigoProducto) +
                        ", CantidadVenta = " + db.ValueForSqlAsNumeric(LiqCantidad) + ", UMc = " + db.ValueForSql(LiqUM) +
                        ", CantEstadistica = " + db.ValueForSqlAsNumeric(LiqCantidadEstadistica) + ", UMEstadistica = " + db.ValueForSql(LiqUMEstadistica) +
                        ", PrecioVenta = " + db.ValueForSqlAsNumeric((cantVtaWrk == 0) ? "0" : (impVtaWrk / cantVtaWrk).ToString()) + ", ImporteVenta = " + db.ValueForSqlAsNumeric(linea.PrecioBrutoTotal) +
                        ", ValorLibre1 = " + db.ValueForSqlAsNumeric(LiqPrecioCompra) + ", ValorLibre2 = " + db.ValueForSqlAsNumeric(LiqMargen2) +
                        ", ValorLibre3 = " + db.ValueForSqlAsNumeric(linea.ValorLibre3Liq) + ", ValorLibre4 = " + db.ValueForSqlAsNumeric(linea.ValorLibre4Liq) +
                        ", ValorLibre5 = " + db.ValueForSqlAsNumeric(linea.ValorLibre5Liq) + ", ValorLibre6 = " + db.ValueForSqlAsNumeric(LiqCantidad) +
                        ", ValorLibre7 = " + db.ValueForSqlAsNumeric(LiqCantidadRegalada) + ", ValorLibre8 = " + db.ValueForSqlAsNumeric((cantVtaWrk == 0) ? "0" : (impVtaWrk / cantVtaWrk).ToString()) +
                        ", ValorLibre9 = " + db.ValueForSqlAsNumeric(LiqMargen2) + ", ValorLibre10 = " + db.ValueForSqlAsNumeric(linea.ImporteLiq2) +
                        ", IndLiquidar = " + db.ValueForSql("S") + ", ImporteLiquidar = " + db.ValueForSqlAsNumeric(linea.ImporteLiq2) +
                        ", Observaciones = " + db.ValueForSql(linea.ObservacionesLiq) +
                        ", FechaModificacion = " + db.SysDate() + ", UsuarioModificacion = " + db.ValueForSql(Constants.PREFIJO_USUARIO_AGENTE + agent) +
                        " WHERE " + sWhereLiqDet.Replace("L.", " ");
                    db.ExecuteSql(sql, agent, GetSipTypeName());
                }
                else
                {                    
                    sql = "INSERT INTO LiquidacionesAcuerdosDetalle " +
                        "(IdcFabricante,IdcDistribuidor,NumLiquidacion,NumLinea,IdcClienteFinal,CodigoCliente" +
                        ",IdcTipoAcuerdo,TipoAcuerdo,SubTipoAcuerdo,TipoAcuerdoFab,NumFactura,EjercicioFactura,NumLineaFactura,FechaFactura" +
                        ",IdcProducto,CodigoProducto,CodigoProductoDist,CantidadVenta,UMc,CantEstadistica,UMEstadistica,PrecioVenta,ImporteVenta" +
                        ",ValorLibre1,ValorLibre2,ValorLibre3,ValorLibre4,ValorLibre5,ValorLibre6,ValorLibre7,ValorLibre8,ValorLibre9,ValorLibre10" +
                        ",IndLiquidar,ImporteLiquidar,Observaciones,UsuarioInsercion, UsuarioModificacion) values " +
                        "(" + idcCodigoFabricante + "," + codigoDistribuidor + "," + db.ValueForSql(numLiquidacion) + "," + db.ValueForSqlAsNumeric(numLineaAux) +
                        "," + idcClienteFinal + "," + db.ValueForSql(codigoCliente) +
                        "," + db.ValueForSql(idcTipoAcuerdo2) + "," + db.ValueForSql(linea.TipoAcuerdoLiq2) + "," + db.ValueForSql(linea.SubtipoAcuerdoLiq2) + "," + db.ValueForSql(TipoAcuerdoFab2) +
                        "," + db.ValueForSql(linea.NumFactura) + "," + db.ValueForSql(linea.Ejercicio) + "," + db.ValueForSqlAsNumeric(linea.NumLinea) + "," + db.DateForSql(fechaFactura) +
                        "," + codigoProducto + "," + db.ValueForSql(codProdFab) + "," + db.ValueForSql(linea.CodigoProducto) + "," + db.ValueForSqlAsNumeric(LiqCantidad) + "," + db.ValueForSql(LiqUM) +
                        "," + db.ValueForSqlAsNumeric(LiqCantidadEstadistica) + "," + db.ValueForSql(LiqUMEstadistica) + "," + db.ValueForSqlAsNumeric((cantVtaWrk == 0) ? "0" : (impVtaWrk / cantVtaWrk).ToString()) + "," + db.ValueForSqlAsNumeric(linea.PrecioBrutoTotal) +
                        "," + db.ValueForSqlAsNumeric(LiqPrecioCompra) + "," + db.ValueForSqlAsNumeric(LiqMargen2) + "," + db.ValueForSqlAsNumeric(linea.ValorLibre3Liq) + "," + db.ValueForSqlAsNumeric(linea.ValorLibre4Liq) + "," + db.ValueForSqlAsNumeric(linea.ValorLibre5Liq) +
                        "," + db.ValueForSqlAsNumeric(LiqCantidad) + "," + db.ValueForSqlAsNumeric(LiqCantidadRegalada) + "," + db.ValueForSqlAsNumeric((cantVtaWrk == 0) ? "0" : (impVtaWrk / cantVtaWrk).ToString()) + "," + db.ValueForSqlAsNumeric(LiqMargen2) + "," + db.ValueForSqlAsNumeric(linea.ImporteLiq2) +
                        "," + db.ValueForSql("S") + "," + db.ValueForSqlAsNumeric(linea.ImporteLiq2) + "," + db.ValueForSql(linea.ObservacionesLiq) + "," + db.ValueForSql(Constants.PREFIJO_USUARIO_AGENTE + agent) + "," + db.ValueForSql(Constants.PREFIJO_USUARIO_AGENTE + agent) + " )";
                    db.ExecuteSql(sql, agent, GetSipTypeName());

                    CabeceraLiquidacion clAux = (CabeceraLiquidacion)htLiquidaciones[key];
                    ++clAux.numLineaMax;
                    htLiquidaciones[key] = clAux;
                }
            }

            impLiqWrk = Utils.StringToDouble(linea.ImporteLiq3);
            if (impLiqWrk != 0)
            {
                string sWhereLiqDet = sWhereLiq +
                    " AND TipoAcuerdoFab = " + db.ValueForSql(TipoAcuerdoFab3) +
                    " AND NumFactura = " + db.ValueForSql(linea.NumFactura) +
                    " AND EjercicioFactura = " + db.ValueForSql(linea.Ejercicio) +
                    " AND NumLineaFactura = " + db.ValueForSqlAsNumeric(linea.NumLinea) +
                    " AND (NumLinea - FLOOR(NumLinea))*10=3 "; //posición 3 tendrá parte decimal igual a 3
                //sWhereLiqDetAux;

                sql = "SELECT 1 FROM LiquidacionesAcuerdosDetalle L " +
                      " WHERE " + sWhereLiqDet;

                numLineaLiq = numLineaLiq + 1;

                string numLineaAux = numLineaLiq.ToString();
                numLineaAux = numLineaAux + ",3";

                if (Utils.RecordExist(sql))
                {
                    sql = "UPDATE LiquidacionesAcuerdosDetalle SET" +
                        " IdcClienteFinal = " + db.ValueForSqlAsNumeric(idcClienteFinal) + ", CodigoCliente = " + db.ValueForSql(codigoCliente) +
                        ", IdcTipoAcuerdo = " + db.ValueForSql(idcTipoAcuerdo3) + ", TipoAcuerdo = " + db.ValueForSql(linea.TipoAcuerdoLiq3) + ", SubTipoAcuerdo = " + db.ValueForSql(linea.SubtipoAcuerdoLiq3) +
                        ", FechaFactura = " + db.DateForSql(fechaFactura) +
                        ", IdcProducto = " + codigoProducto + ", CodigoProducto = " + db.ValueForSql(codProdFab) + ", CodigoProductoDist = " + db.ValueForSql(linea.CodigoProducto) +
                        ", CantidadVenta = " + db.ValueForSqlAsNumeric(LiqCantidad) + ", UMc = " + db.ValueForSql(LiqUM) +
                        ", CantEstadistica = " + db.ValueForSqlAsNumeric(LiqCantidadEstadistica) + ", UMEstadistica = " + db.ValueForSql(LiqUMEstadistica) +
                        ", PrecioVenta = " + db.ValueForSqlAsNumeric((cantVtaWrk == 0) ? "0" : (impVtaWrk / cantVtaWrk).ToString()) + ", ImporteVenta = " + db.ValueForSqlAsNumeric(linea.PrecioBrutoTotal) +
                        ", ValorLibre1 = " + db.ValueForSqlAsNumeric(LiqPrecioCompra) + ", ValorLibre2 = " + db.ValueForSqlAsNumeric(LiqMargen3) +
                        ", ValorLibre3 = " + db.ValueForSqlAsNumeric(linea.ValorLibre3Liq) + ", ValorLibre4 = " + db.ValueForSqlAsNumeric(linea.ValorLibre4Liq) +
                        ", ValorLibre5 = " + db.ValueForSqlAsNumeric(linea.ValorLibre5Liq) + ", ValorLibre6 = " + db.ValueForSqlAsNumeric(LiqCantidad) +
                        ", ValorLibre7 = " + db.ValueForSqlAsNumeric(LiqCantidadRegalada) + ", ValorLibre8 = " + db.ValueForSqlAsNumeric((cantVtaWrk == 0) ? "0" : (impVtaWrk / cantVtaWrk).ToString()) +
                        ", ValorLibre9 = " + db.ValueForSqlAsNumeric(LiqMargen3) + ", ValorLibre10 = " + db.ValueForSqlAsNumeric(linea.ImporteLiq3) +
                        ", IndLiquidar = " + db.ValueForSql("S") + ", ImporteLiquidar = " + db.ValueForSqlAsNumeric(linea.ImporteLiq3) +
                        ", Observaciones = " + db.ValueForSql(linea.ObservacionesLiq) +
                        ", FechaModificacion = " + db.SysDate() + ", UsuarioModificacion = " + db.ValueForSql(Constants.PREFIJO_USUARIO_AGENTE + agent) +
                        " WHERE " + sWhereLiqDet.Replace("L.", " ");
                    db.ExecuteSql(sql, agent, GetSipTypeName());
                }
                else
                {                    
                    sql = "INSERT INTO LiquidacionesAcuerdosDetalle " +
                        "(IdcFabricante,IdcDistribuidor,NumLiquidacion,NumLinea,IdcClienteFinal,CodigoCliente" +
                        ",IdcTipoAcuerdo,TipoAcuerdo,SubTipoAcuerdo,TipoAcuerdoFab,NumFactura,EjercicioFactura,NumLineaFactura,FechaFactura" +
                        ",IdcProducto,CodigoProducto,CodigoProductoDist,CantidadVenta,UMc,CantEstadistica,UMEstadistica,PrecioVenta,ImporteVenta" +
                        ",ValorLibre1,ValorLibre2,ValorLibre3,ValorLibre4,ValorLibre5,ValorLibre6,ValorLibre7,ValorLibre8,ValorLibre9,ValorLibre10" +
                        ",IndLiquidar,ImporteLiquidar,Observaciones,UsuarioInsercion, UsuarioModificacion) values " +
                        "(" + idcCodigoFabricante + "," + codigoDistribuidor + "," + db.ValueForSql(numLiquidacion) + "," + db.ValueForSqlAsNumeric(numLineaAux) +
                        "," + idcClienteFinal + "," + db.ValueForSql(codigoCliente) +
                        "," + db.ValueForSql(idcTipoAcuerdo3) + "," + db.ValueForSql(linea.TipoAcuerdoLiq3) + "," + db.ValueForSql(linea.SubtipoAcuerdoLiq3) + "," + db.ValueForSql(TipoAcuerdoFab3) +
                        "," + db.ValueForSql(linea.NumFactura) + "," + db.ValueForSql(linea.Ejercicio) + "," + db.ValueForSqlAsNumeric(linea.NumLinea) + "," + db.DateForSql(fechaFactura) +
                        "," + codigoProducto + "," + db.ValueForSql(codProdFab) + "," + db.ValueForSql(linea.CodigoProducto) + "," + db.ValueForSqlAsNumeric(LiqCantidad) + "," + db.ValueForSql(LiqUM) +
                        "," + db.ValueForSqlAsNumeric(LiqCantidadEstadistica) + "," + db.ValueForSql(LiqUMEstadistica) + "," + db.ValueForSqlAsNumeric((cantVtaWrk == 0) ? "0" : (impVtaWrk / cantVtaWrk).ToString()) + "," + db.ValueForSqlAsNumeric(linea.PrecioBrutoTotal) +
                        "," + db.ValueForSqlAsNumeric(LiqPrecioCompra) + "," + db.ValueForSqlAsNumeric(LiqMargen3) + "," + db.ValueForSqlAsNumeric(linea.ValorLibre3Liq) + "," + db.ValueForSqlAsNumeric(linea.ValorLibre4Liq) + "," + db.ValueForSqlAsNumeric(linea.ValorLibre5Liq) +
                        "," + db.ValueForSqlAsNumeric(LiqCantidad) + "," + db.ValueForSqlAsNumeric(LiqCantidadRegalada) + "," + db.ValueForSqlAsNumeric((cantVtaWrk == 0) ? "0" : (impVtaWrk / cantVtaWrk).ToString()) + "," + db.ValueForSqlAsNumeric(LiqMargen3) + "," + db.ValueForSqlAsNumeric(linea.ImporteLiq3) +
                        "," + db.ValueForSql("S") + "," + db.ValueForSqlAsNumeric(linea.ImporteLiq3) + "," + db.ValueForSql(linea.ObservacionesLiq) + "," + db.ValueForSql(Constants.PREFIJO_USUARIO_AGENTE + agent) + "," + db.ValueForSql(Constants.PREFIJO_USUARIO_AGENTE + agent) + " )";
                    db.ExecuteSql(sql, agent, GetSipTypeName());

                    CabeceraLiquidacion clAux = (CabeceraLiquidacion)htLiquidaciones[key];
                    ++clAux.numLineaMax;
                    htLiquidaciones[key] = clAux;
                }
            }
        }                

        return;
    }

    /// <summary>
    /// Obtener código de producto según el fabricante
    /// </summary>
    /// <param name="db">base de datos</param>    
    private void ObtenerCodigoProductoFabricante(Database db)
    {
        Producto prodFab = new Producto();
        codigoProdFab = "";        
        prodFab.ObtenerCodigoProductoFabricante(db, idcCodigoFabricante, codigoProducto);
        codigoProdFab = prodFab.CodigoProdFab;        
    }

    /// <summary>
    /// Obtener importe total componentes Kit
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="RecordLineasFacturasDistribuidor">record lineas facturas</param>    
    /// <returns>importe total del kit</returns>
    private bool ObtenerImporteNumeroTotalComponentesKit(Database db, RecordLineasFacturasDistribuidor linea)
    {        
        DbDataReader cursor = null;
        bool isOk = false;
        proporcionImporteTotalKit = "";
        numeroComponentesKit = "";
        try
        {
            string sql = "SELECT sum(ProporcionImporteComponente), count(1) " +                      
                         "  FROM ProductosAgentesKits " +
                         //" WHERE IdcAgente = " + agent +
                         " WHERE IdcAgente = " + (EsKitFabricante() ? idcCodigoFabricante : agent) +
                         "   AND CodigoProductoKit = " + (EsKitFabricante() ? db.ValueForSql(codigoProdFab) : db.ValueForSql(linea.CodigoProducto));                
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
            {
                proporcionImporteTotalKit = db.GetFieldValue(cursor, 0);
                numeroComponentesKit = db.GetFieldValue(cursor, 1);
                isOk = true;
            }
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
        return isOk;
    }

    private bool EsKit()
    {
        return (productoEsKit == "F" || productoEsKit == "D");
    }

    private bool EsKitFabricante()
    {
        return (productoEsKit == "F");
    }

    private void ObtenerDireccion(Database db, RecordLineasFacturasDistribuidor linea)
    {
        if (!Utils.IsBlankField(linea.Direccion) || (!Utils.IsBlankField(linea.TipoCalle) && !Utils.IsBlankField(linea.Calle) && !Utils.IsBlankField(linea.Numero)) || !Utils.IsBlankField(codPostal.CPostal))
        {
            Direccion = linea.Direccion;
            CodigoPostal = codPostal.CPostal;
            TipoCalle = linea.TipoCalle;
            Calle = linea.Calle;
            Numero = linea.Numero;
            Provincia = Utils.ObtenerProvincia(CodigoPostal, agentLocation);
            CodigoPais = Utils.ObtenerPais(linea.CodigoPais, agent, GetSipTypeName());
        }
        else
        {
            RecuperarDireccionCliente(db, idcClienteFinal, linea);
            if (datosDireccion.direccionEncontrada)
            {
                Direccion = datosDireccion.Direccion;
                CodigoPostal = datosDireccion.CodigoPostal;
                TipoCalle = datosDireccion.TipoCalle;
                Calle = datosDireccion.Calle;
                Numero = datosDireccion.Numero;
                Provincia = datosDireccion.Provincia;
                if (Utils.IsBlankField(Provincia))
                    Provincia = Utils.ObtenerProvincia(CodigoPostal, agentLocation);
                CodigoPais = datosDireccion.CodigoPais;
                if (Utils.IsBlankField(CodigoPais))
                    CodigoPais = Utils.ObtenerPais(CodigoPais, agent, GetSipTypeName());
            }
            else
            {
                Direccion = linea.Direccion;
                CodigoPostal = codPostal.CPostal;
                TipoCalle = linea.TipoCalle;
                Calle = linea.Calle;
                Numero = linea.Numero;
                Provincia = Utils.ObtenerProvincia(CodigoPostal, agentLocation);
                CodigoPais = Utils.ObtenerPais(linea.CodigoPais, agent, GetSipTypeName());
            }
        }
    }

    /// <summary>
    /// Convertir la cantidad complementaria 
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="linea">linea de factura</param>
    private string ConvertirCantidadComplementaria(Database db, RecordLineasFacturasDistribuidor linea)  
    {
        string sCantResult = linea.Cantidad;

        string cant = "";
        double nCant = 0;
        double nCant2 = 0;

        if (UMProducto.Equals(UMProducto2))
        {
            cant = linea.Cantidad2;
        }
        else if (Double.TryParse(linea.Cantidad2, out nCant) && nCant == 0)
        {
            cant = "0";
        }
        else
        {
            DbDataReader cursor = null;
            string sql = "SELECT Productos.UMc, Productos.IdcFabricante " +
                       "FROM Productos WHERE Productos.IdcProducto = " + codigoProducto;
            try
            {
                bool ok = false;
                string UMc = "", IdcFabricante = "";
                cursor = db.GetDataReader(sql);
                if (cursor.Read())
                {
                    UMc = db.GetFieldValue(cursor, 0);
                    IdcFabricante = db.GetFieldValue(cursor, 1);
                    ok = true;
                }
                if (ok)
                {
                    //Buscar conversión directa entre UM de producto y UM Estadística.
                    CalcUnidadMedida cm = unidadMedida.ConversionUnidadesMedida(db, IdcFabricante, codigoProducto, linea.Cantidad2, UMProducto2, UMProducto);
                    if (cm.isOK)
                    {
                        cant = cm.cantidad + "";
                    }
                    else
                    {
                        //Buscaremos conversión indirecta: primero entre la UMcProducto y la UMc, y después entre la UMc y la UMEstadistica
                        cm = unidadMedida.ConversionUnidadesMedida(db, IdcFabricante, codigoProducto, linea.Cantidad2, UMProducto2, UMc);
                        if (cm.isOK)
                        {
                            double cantTemp = cm.cantidad;
                            cm = unidadMedida.ConversionUnidadesMedida(db, IdcFabricante, codigoProducto, cantTemp + "", UMc, UMProducto);
                            if (cm.isOK)
                                cant = cm.cantidad + "";
                        }
                    }
                }
            }
            finally
            {
                if (cursor != null)
                    cursor.Close();
            }
        }
        if (cant == "")
        {
            //Si la cantidad devuelta es vacía significa que no ha sabido encontrar la conversión.
            //Se genera un aviso y se continua.
            //Globals.GetInstance().GetLog().Warning(agent, GetSipTypeName(), "Aviso en " + slaveTitle + ". Conversión de UM complementaria " + UMProducto2 + " a UM principal " + UMProducto + " erronea en factura " + linea.NumFactura + " línea " + linea.NumLinea + " producto " + linea.CodigoProducto + " unidad medida " + linea.UM);
            string myAlertMsg = "Aviso en " + slaveTitle + ". Conversión de UM complementaria {0} a UM principal {1} erronea en factura {2} línea {3} producto {4} unidad medida {5}.";
            Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "FADI0023", myAlertMsg, UMProducto2, UMProducto, linea.NumFactura, linea.NumLinea, linea.CodigoProducto, linea.UM);
        }
        else
        {
            Double.TryParse(sCantResult, out nCant);
            Double.TryParse(cant, out nCant2);
            nCant = nCant + nCant2;
            sCantResult = nCant.ToString();
        }
        return sCantResult;
    }

    /// <summary>
    /// Convertir la cantidad complementaria 
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="linea">linea de factura</param>
    private string ConvertirPrecioBaseComplementario(Database db, RecordLineasFacturasDistribuidor linea)
    {
        string sPrecResult = linea.PrecioBase;

        string cant = "";
        double nCant = 0;
        string prec = "";
        double nPrec = 0;

        if (UMProducto.Equals(UMProducto2))
        {
            prec = linea.PrecioBase2;
            cant = "1";
        }
        else if (Double.TryParse(linea.PrecioBase2, out nPrec) && nPrec == 0)
        {
            prec = "0";
            cant = "1";
        }
        else
        {
            prec = linea.PrecioBase2;

            DbDataReader cursor = null;
            string sql = "SELECT Productos.UMc, Productos.IdcFabricante " +
                       "FROM Productos WHERE Productos.IdcProducto = " + codigoProducto;
            try
            {
                bool ok = false;
                string UMc = "", IdcFabricante = "";
                cursor = db.GetDataReader(sql);
                if (cursor.Read())
                {
                    UMc = db.GetFieldValue(cursor, 0);
                    IdcFabricante = db.GetFieldValue(cursor, 1);
                    ok = true;
                }
                if (ok)
                {
                    //Buscar conversión directa entre UM de producto y UM Estadística.
                    CalcUnidadMedida cm = unidadMedida.ConversionUnidadesMedida(db, IdcFabricante, codigoProducto, "1", UMProducto2, UMProducto);
                    if (cm.isOK)
                    {
                        cant = cm.cantidad + "";
                    }
                    else
                    {
                        //Buscaremos conversión indirecta: primero entre la UMcProducto y la UMc, y después entre la UMc y la UMEstadistica
                        cm = unidadMedida.ConversionUnidadesMedida(db, IdcFabricante, codigoProducto, "1", UMProducto2, UMc);
                        if (cm.isOK)
                        {
                            double cantTemp = cm.cantidad;
                            cm = unidadMedida.ConversionUnidadesMedida(db, IdcFabricante, codigoProducto, cantTemp + "", UMc, UMProducto);
                            if (cm.isOK)
                                cant = cm.cantidad + "";
                        }
                    }
                }
            }
            finally
            {
                if (cursor != null)
                    cursor.Close();
            }
        }
        if (cant == "")
        {
            //Si la cantidad devuelta es vacía significa que no ha sabido encontrar la conversión.
            //Se genera un aviso y se continua.
            //Globals.GetInstance().GetLog().Warning(agent, GetSipTypeName(), "Aviso en " + slaveTitle + ". Conversión de UM complementaria " + UMProducto2 + " a UM principal " + UMProducto + " erronea en factura " + linea.NumFactura + " línea " + linea.NumLinea + " producto " + linea.CodigoProducto + " unidad medida " + linea.UM);
            string myAlertMsg = "Aviso en " + slaveTitle + ". Conversión de UM complementaria {0} a UM principal {1} erronea en factura {2} línea {3} producto {4} unidad medida {5}.";
            Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "FADI0023", myAlertMsg, UMProducto2, UMProducto, linea.NumFactura, linea.NumLinea, linea.CodigoProducto, linea.UM);
        }
        else
        {
            Double.TryParse(prec, out nPrec);
            Double.TryParse(cant, out nCant);
            if (nCant != 0)
            {
                nPrec = nPrec / nCant;
                sPrecResult = nPrec.ToString();
            }
        }
        return sPrecResult;
    }

    /// <summary>
    /// Buscar unidades de medida estadística
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="linea">linea de factura</param>
    private void BuscarUnidadesMedidaEstadistica(Database db, RecordLineasFacturasDistribuidor linea, bool calcularCantidades)
    {
        BuscarUnidadesMedidaEstadistica(db, linea.NumFactura, linea.NumLinea, linea.CodigoProducto, UMProducto, linea.Cantidad, calcularCantidades);
    }

    private void BuscarUnidadesMedidaEstadistica(Database db, string pNumFactura, string pNumLinea, string pCodigoProducto, string pUMProducto, string pCantidad, bool calcularCantidades)
    {
        UMEstadistica = "";
        CantidadEstadistica = "";
        UMEstadistica2 = "";
        CantidadEstadistica2 = "";
        UMEstadistica3 = "";
        CantidadEstadistica3 = "";

        //Si se ha activado el flag de NO calcular cantidades salimos sin calcular.
        if (!calcularCantidades)
            return;

        bool ok = false;            
        string UMc = "", IdcFabricante = "";

        if (prod.htProductos.ContainsKey(pCodigoProducto))
        {
            ConnectaLib.Producto.sProductos p = (ConnectaLib.Producto.sProductos)prod.htProductos[pCodigoProducto];
            UMc = p.UMc;
            UMEstadistica = p.UMEstadistica;
            UMEstadistica2 = p.UMEstadistica2;
            UMEstadistica3 = p.UMEstadistica3;
            IdcFabricante = p.idcFabricante;
            ok = true;
        }

        DbDataReader cursor = null;

        try
        {
            if (!ok)
            {                
                string sql = "SELECT Productos.UMc, Productos.UMEstadistica, Productos.UMEstadistica2, Productos.UMEstadistica3, Productos.IdcFabricante " +
                           "FROM Productos WHERE Productos.IdcProducto = " + codigoProducto;
                cursor = db.GetDataReader(sql);
                if (cursor.Read())
                {
                    UMc = db.GetFieldValue(cursor, 0);
                    UMEstadistica = db.GetFieldValue(cursor, 1);
                    UMEstadistica2 = db.GetFieldValue(cursor, 2);
                    UMEstadistica3 = db.GetFieldValue(cursor, 3);
                    IdcFabricante = db.GetFieldValue(cursor, 4);
                    ok = true;
                }
            }
            if (ok)
            {
                if (!Utils.IsBlankField(UMEstadistica))
                {
                    CantidadEstadistica = CantidadConvertida(db, pCantidad, pUMProducto, IdcFabricante, UMc, UMEstadistica);
                    if (CantidadEstadistica == "")
                    {
                        //Si la cantidad devuelta es vacía significa que no ha sabido encontrar la conversión.
                        //Se genera un aviso y se continua.
                        string myAlertMsg = "Aviso en " + slaveTitle + ". Conversión a UM estadística {0} erronea en factura {1} línea {2} producto {3} unidad medida {4}.";
                        string[] aValores = new string[] { UMEstadistica, pNumFactura, pNumLinea, pCodigoProducto, pUMProducto };
                        string[] aClaves = new string[] { IdcFabricante, codigoDistribuidor, codigoProducto, pUMProducto, UMEstadistica };
                        string[] aClavesExt = new string[] { IdcFabricante, codigoDistribuidor, codigoProducto, pUMProducto, UMEstadistica, pNumFactura, pNumLinea };
                        Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "FADI0024", myAlertMsg, aValores, aClaves, aClavesExt);
                    }
                }
                if (!Utils.IsBlankField(UMEstadistica2))
                {
                    CantidadEstadistica2 = CantidadConvertida(db, pCantidad, pUMProducto, IdcFabricante, UMc, UMEstadistica2);
                    if (CantidadEstadistica2 == "")
                    {
                        //Si la cantidad devuelta es vacía significa que no ha sabido encontrar la conversión.
                        //Se genera un aviso y se continua.
                        string myAlertMsg = "Aviso en " + slaveTitle + ". Conversión a UM estadística 2 {0} erronea en factura {1} línea {2} producto {3} unidad medida {4}.";
                        string[] aValores = new string[] { UMEstadistica2, pNumFactura, pNumLinea, pCodigoProducto, pUMProducto };
                        string[] aClaves = new string[] { IdcFabricante, codigoDistribuidor, codigoProducto, pUMProducto, UMEstadistica2 };
                        string[] aClavesExt = new string[] { IdcFabricante, codigoDistribuidor, codigoProducto, pUMProducto, UMEstadistica2, pNumFactura, pNumLinea };
                        Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "FADI0025", myAlertMsg, aValores, aClaves, aClavesExt);
                    }
                }
                if (!Utils.IsBlankField(UMEstadistica3))
                {
                    CantidadEstadistica3 = CantidadConvertida(db, pCantidad, pUMProducto, IdcFabricante, UMc, UMEstadistica3);
                    if (CantidadEstadistica3 == "")
                    {
                        //Si la cantidad devuelta es vacía significa que no ha sabido encontrar la conversión.
                        //Se genera un aviso y se continua.
                        string myAlertMsg = "Aviso en " + slaveTitle + ". Conversión a UM estadística 3 {0} erronea en factura {1} línea {2} producto {3} unidad medida {4}.";
                        string[] aValores = new string[] { UMEstadistica3, pNumFactura, pNumLinea, pCodigoProducto, pUMProducto };
                        string[] aClaves = new string[] { IdcFabricante, codigoDistribuidor, codigoProducto, pUMProducto, UMEstadistica3 };
                        string[] aClavesExt = new string[] { IdcFabricante, codigoDistribuidor, codigoProducto, pUMProducto, UMEstadistica3, pNumFactura, pNumLinea };
                        Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "FADI0026", myAlertMsg, aValores, aClaves, aClavesExt);
                    }
                }
            }
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
    }

    private void BuscarUnidadesMedidaEstadisticaKit_OLD(Database db, string pNumFactura, string pNumLinea, string pCodigoProducto, string pUMProducto, string pCantidad, bool calcularCantidades)
    {
        string UMEstadisticaKit = "";
        cantEstadisticaKit = "";

        //Si se ha activado el flag de NO calcular cantidades salimos sin calcular.
        if (!calcularCantidades)
            return;

        DbDataReader cursor = null;
        string sql = "SELECT Productos.UMc, 'UDK', Productos.IdcFabricante " +
                   "FROM Productos WHERE Productos.IdcProducto = " + codigoProducto;
        try
        {
            if (EsKit() && !EsKitFabricante())
            {
                cantEstadisticaKit = pCantidad;
            }
            else
            {
                bool ok = false;
                string UMc = "", IdcFabricante = "";
                cursor = db.GetDataReader(sql);
                if (cursor.Read())
                {
                    UMc = db.GetFieldValue(cursor, 0);
                    UMEstadisticaKit = db.GetFieldValue(cursor, 1);
                    IdcFabricante = db.GetFieldValue(cursor, 2);
                    ok = true;
                }
                if (ok)
                {
                    if (!Utils.IsBlankField(UMEstadisticaKit))
                    {
                        cantEstadisticaKit = CantidadConvertida(db, pCantidad, pUMProducto, IdcFabricante, UMc, UMEstadisticaKit);
                        if (cantEstadisticaKit == "")
                        {
                            //Si la cantidad devuelta es vacía significa que no ha sabido encontrar la conversión.
                            //Se genera un aviso y se continua.
                            string myAlertMsg = "Aviso en " + slaveTitle + ". Conversión a UM estadística kit {0} erronea en factura {1} línea {2} producto {3} unidad medida {4}.";
                            string[] aValores = new string[] { UMEstadisticaKit, pNumFactura, pNumLinea, pCodigoProducto, pUMProducto };
                            string[] aClaves = new string[] { IdcFabricante, codigoDistribuidor, codigoProducto, pUMProducto, UMEstadisticaKit };
                            string[] aClavesExt = new string[] { IdcFabricante, codigoDistribuidor, codigoProducto, pUMProducto, UMEstadisticaKit, pNumFactura, pNumLinea };
                            Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "FADI0027", myAlertMsg, aValores, aClaves, aClavesExt);
                        }
                    }
                }
            }
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
    }

    private void BuscarUnidadesMedidaEstadisticaKit(Database db, string pNumFactura, string pNumLinea, string pCodigoProducto, string pUMProducto, string pCantidad)
    {
        DbDataReader cursor = null;
        string UMEstadisticaKit = "";
        cantEstadisticaKit = "";

        try
        {
            if (!EsKitFabricante())
            {
                cantEstadisticaKit = pCantidad;
                UMEstadisticaKit = pUMProducto;
            }
            else
            {
                bool ok = false;
                string IdcFabricante = "";
                string sql = "SELECT Productos.UMc, Productos.IdcFabricante FROM Productos WHERE Productos.IdcProducto = " + codigoProducto;
                cursor = db.GetDataReader(sql);
                if (cursor.Read())
                {
                    UMEstadisticaKit = db.GetFieldValue(cursor, 0);
                    IdcFabricante = db.GetFieldValue(cursor, 1);
                    ok = true;
                }
                if (ok)
                {
                    if (!Utils.IsBlankField(UMEstadisticaKit))
                    {
                        cantEstadisticaKit = CantidadConvertida(db, pCantidad, pUMProducto, IdcFabricante, UMEstadisticaKit, UMEstadisticaKit); //es redundante llamar con el mismo parametro dos veces, pero es para aprovechar la función
                        if (cantEstadisticaKit == "")
                        {
                            //Si la cantidad devuelta es vacía significa que no ha sabido encontrar la conversión. Se genera un aviso y se continua.
                            string myAlertMsg = "Aviso en " + slaveTitle + ". Conversión a UM estadística kit {0} erronea en factura {1} línea {2} producto {3} unidad medida {4}.";
                            string[] aValores = new string[] { UMEstadisticaKit, pNumFactura, pNumLinea, pCodigoProducto, pUMProducto };
                            string[] aClaves = new string[] { IdcFabricante, codigoDistribuidor, codigoProducto, pUMProducto, UMEstadisticaKit };
                            string[] aClavesExt = new string[] { IdcFabricante, codigoDistribuidor, codigoProducto, pUMProducto, UMEstadisticaKit, pNumFactura, pNumLinea };
                            Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "FADI0027", myAlertMsg, aValores, aClaves, aClavesExt);
                        }
                    }
                }
                if (Utils.IsBlankField(UMEstadisticaKit))
                {
                    UMEstadisticaKit = pUMProducto;
                    cantEstadisticaKit = pCantidad;
                }
            }
        }
        finally
        {
            UMProductoKit = UMEstadisticaKit;
            if (cursor != null)
                cursor.Close();
        }
    }

    /// <summary>
    /// Cálculo de la cantidad convertida
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="linea">línea de factura</param>
    /// <param name="IdcFabricante">fabricante</param>
    /// <param name="UMc">UM</param>
    /// <param name="UMEst">UM estadística</param>
    /// <returns></returns>
    private string CantidadConvertida(Database db, string pCantidad, string pUMProducto, string IdcFabricante, string UMc, string UMConv)
    {
        string cant = "";
        double nCant = 0;
        if (UMConv.Equals(pUMProducto))
        {
            cant = pCantidad;
        }
        else if (Double.TryParse(pCantidad, out nCant) && nCant == 0)
        {
            cant = "0";
        }
        else 
        {
            //Buscar conversión directa entre UM de producto y UM Estadística.
            CalcUnidadMedida cm = unidadMedida.ConversionUnidadesMedida(db, IdcFabricante, codigoProducto, pCantidad, pUMProducto, UMConv);
            if (cm.isOK)
            {
                cant = cm.cantidad + "";
            }
            else
            {
                //Buscaremos conversión indirecta: primero entre la UMcProducto y la UMc, y después entre la UMc y la UMEstadistica
                cm = unidadMedida.ConversionUnidadesMedida(db, IdcFabricante, codigoProducto, pCantidad, pUMProducto, UMc);
                if (cm.isOK)
                {
                    double cantTemp = cm.cantidad;
                    cm = unidadMedida.ConversionUnidadesMedida(db, IdcFabricante, codigoProducto, cantTemp + "", UMc, UMConv);
                    if (cm.isOK)
                        cant = cm.cantidad + "";
                }
            }
        }
        return cant;
    }

    /// <summary>
    /// Busca el identificador Connect@ del fabricante a partir de su codigo
    /// </summary>
    /// <param name="pCodigoFab">código del fabricante</param>
    /// <returns>identificdor del fabricante</returns>
    private string BuscaFabricante(string pCodigoFab)
    {
        string fabricante = null;
        Database db = Globals.GetInstance().GetDatabase();
        DbDataReader cursor = null;
        try
        {
            string sql = "Select IdcAgenteDestino From ClasifInterAgentes Where IdcAgenteOrigen = " + agent + " And Codigo = '" + pCodigoFab + "'";
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
            {
                fabricante = db.GetFieldValue(cursor, 0);
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
    /// Obtener el tipo de acuerdo
    /// </summary>
    private void ObtenerTipoAcuerdo(Database db, string pAgente, string pValorBusqueda, out string pTipoAcuerdo, out string pIdcTipoAcuerdo, int pPosicion)
    {
        DbDataReader cursor = null;
        pTipoAcuerdo = "";
        pIdcTipoAcuerdo = "";
        try
        {
            string sql = "";
            if (pPosicion != 0) //Hemos quitado flexibilidad a la búsqueda del tipo de acuerdo, por convenio, ahora fijamos que lo que tenemos :
                                //    en la posición 1 es Base
                                //    en la posición 2 es Promo
                                //    en la posición 3 es Otros
                                // Si queremos regresar al método anterior debemos modificar la llamada a estas funciones poniendo un 0 en el parametro pPosicion
            {
                if (pPosicion == 1)
                {
                    pIdcTipoAcuerdo = "B";
                    sql = "select Codigo from ClasificacionAgente where EntidadClasificada = 'TIPOLIAC' and IdcAgente = " + pAgente + " and Libre1 ='" + pIdcTipoAcuerdo + "'";
                    cursor = db.GetDataReader(sql);
                    if (cursor.Read()) pTipoAcuerdo = db.GetFieldValue(cursor, 0);
                    if (Utils.IsBlankField(pTipoAcuerdo)) pTipoAcuerdo = "B";
                    if (cursor != null) cursor.Close();
                }
                else if (pPosicion == 2)
                {
                    pIdcTipoAcuerdo = "P";
                    sql = "select Codigo from ClasificacionAgente where EntidadClasificada = 'TIPOLIAC' and IdcAgente = " + pAgente + " and Libre1 ='" + pIdcTipoAcuerdo + "'";
                    cursor = db.GetDataReader(sql);
                    if (cursor.Read()) pTipoAcuerdo = db.GetFieldValue(cursor, 0);
                    if (Utils.IsBlankField(pTipoAcuerdo)) pTipoAcuerdo = "P";
                    if (cursor != null) cursor.Close();
                }
                else if (pPosicion == 3)
                {
                    pIdcTipoAcuerdo = "O";
                    sql = "select Codigo from ClasificacionAgente where EntidadClasificada = 'TIPOLIAC' and IdcAgente = " + pAgente + " and Libre1 ='" + pIdcTipoAcuerdo + "'";
                    cursor = db.GetDataReader(sql);
                    if (cursor.Read()) pTipoAcuerdo = db.GetFieldValue(cursor, 0);
                    if (Utils.IsBlankField(pTipoAcuerdo)) pTipoAcuerdo = "O";
                    if (cursor != null) cursor.Close();
                }
            }
            else
            {
                if (!Utils.IsBlankField(pValorBusqueda))
                {
                    string sLibreAdd = "";
                    bool encontrado = false;
                    sql = "select LibreAdd, Codigo, Libre1 from ClasificacionAgente where EntidadClasificada = 'TIPOLIAC' and IdcAgente = " + pAgente;
                    cursor = db.GetDataReader(sql);
                    while (cursor.Read() && !encontrado)
                    {
                        sLibreAdd = db.GetFieldValue(cursor, 0);
                        string[] sLibreAddList = sLibreAdd.Split(';');
                        for (int i = 0; i < sLibreAddList.Length; i++)
                        {
                            string sCompare = sLibreAddList[i].ToString().ToLower();
                            if (!Utils.IsBlankField(sCompare))
                            {
                                if (pValorBusqueda.ToLower().Contains(sCompare))
                                {
                                    encontrado = true;
                                    break;
                                }
                            }
                        }
                        if (encontrado)
                        {
                            pTipoAcuerdo = db.GetFieldValue(cursor, 1);
                            pIdcTipoAcuerdo = db.GetFieldValue(cursor, 2);
                        }
                    }
                    cursor.Close();
                    cursor = null;

                    if (Utils.IsBlankField(pIdcTipoAcuerdo))
                    {
                        if (cursor != null) cursor.Close();

                        pIdcTipoAcuerdo = "O";
                        sql = "select Codigo from ClasificacionAgente where EntidadClasificada = 'TIPOLIAC' and IdcAgente = " + pAgente + " and Libre1 ='" + pIdcTipoAcuerdo + "'";
                        cursor = db.GetDataReader(sql);
                        if (cursor.Read())
                        {
                            pTipoAcuerdo = db.GetFieldValue(cursor, 0);
                        }
                        if (Utils.IsBlankField(pTipoAcuerdo)) pTipoAcuerdo = "O";
                    }
                }
                else
                {
                    pIdcTipoAcuerdo = "B";
                    sql = "select Codigo from ClasificacionAgente where EntidadClasificada = 'TIPOLIAC' and IdcAgente = " + pAgente + " and Libre1 ='" + pIdcTipoAcuerdo + "'";
                    cursor = db.GetDataReader(sql);
                    if (cursor.Read())
                    {
                        pTipoAcuerdo = db.GetFieldValue(cursor, 0);
                    }
                }
            }
            if (Utils.IsBlankField(pTipoAcuerdo)) pTipoAcuerdo = "B";
        }
        finally
        {
            if (cursor != null) cursor.Close();
        }
    }

    /// <summary>
    /// Comprobar entidad clasificada
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="pEntidadClasificada">nombre de la entidad clasificada</param>
    /// <param name="pCodigo">Código a comprobar</param>
    /// <returns>true si es correcto</returns>
    private bool ComprobarEntidadClasificada(Database db, string pEntidadClasificada, string pCodigo)
    {
        bool isOK = false;
        DbDataReader cursor = null;
        idcEntidadClasificada = "";
        try
        {
            if(htClasificadores.ContainsKey(pEntidadClasificada + ";" + pCodigo))
            {
                idcEntidadClasificada = htClasificadores[pEntidadClasificada + ";" + pCodigo].ToString();
                isOK = true;
            }            
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
        return isOK;
    }

    /// <summary>
    /// Insertar entidad clasificada
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="pEntidadClasificada">nombre de la entidad clasificada</param>
    /// <param name="pCodigo">Código a comprobar</param>
    /// <returns>true si es correcto</returns>
    private void InsertarEntidadClasificada(Database db, string pEntidadClasificada, string pCodigo, string pDescripcion)
    {
        string sql = "INSERT INTO CLASIFICACIONAGENTE " +
                    " (ENTIDADCLASIFICADA, IDCAGENTE, DESCRIPCION, CODIGO, FECHAINSERCION, FECHAMODIFICACION) " +
                    " values " +
                    "(" +
                    db.ValueForSql(pEntidadClasificada) +
                    "," + agent +
                    "," + db.ValueForSql(pDescripcion) +
                    "," + db.ValueForSql(pCodigo) +
                    "," + db.SysDate() +
                    "," + db.SysDate() +
                    ")";
        db.ExecuteSql(sql, agent, GetSipTypeName());
    }

    /// <summary>
    /// Insertar un productos facturas con errores
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="lf">registro</param>
    private void InsertarProductoFacturaConErrores(Database db, RecordLineasFacturasDistribuidor lf)
    {
        string sql = "";

        //Primero miramos si el producto ya existe, si ya existe lo actualitzamos si no existe lo insertamos
        sql = "select 1 from ProductosFacturasConErrores " +
                " WHERE IdcAgente=" + codigoDistribuidor +
                " and NumFactura=" + db.ValueForSql(lf.NumFactura) +
                " and Ejercicio=" + db.ValueForSql(lf.Ejercicio) +
                " and NumLinea=" + db.ValueForSqlAsNumeric(lf.NumLinea) +
                " ";
        if (!Utils.RecordExist(sql))
        {
            sql = "insert into ProductosFacturasConErrores " +
                        "(IdcAgente,NumFactura,Ejercicio,NumLinea,CodigoProducto,Cantidad,UM,Peso,UMPeso,Volumen,UMVolumen,PrecioBase,Descuentos,PrecioBrutoTotal" +
                        " ,FechaEntrega,Almacen,Lote,FechaCaducidad,CodigoPostal,Direccion,TipoCalle,Calle,Numero,CodigoPais,Ruta,CodigoComercial,EjercicioEntrega,NumEntrega,NumLineaEntrega" +
                        ",CosteDistribuidor,DocRelacionado,CodigoCliente,FechaFactura,UM2,Cantidad2,PrecioBase2,TipoVenta,MotivoAbono" +
                        ",CodigoPromocion,Libre1,Libre2,Libre3,CantLibre1,CantLibre2,CantLibre3" +
                        ",TipoAcuerdo1,SubtipoAcuerdo1,ImporteLiquidar1,TipoAcuerdo2,SubtipoAcuerdo2,ImporteLiquidar2,TipoAcuerdo3,SubtipoAcuerdo3,ImporteLiquidar3" +
                        ",ValorLibre1Liq,ValorLibre2Liq,ValorLibre3Liq,ValorLibre4Liq,ValorLibre5Liq,ValorLibre6Liq,ValorLibre7Liq,ValorLibre8Liq,ValorLibre9Liq,ValorLibre10Liq" +
                        ",ObservacionesLiq,TipoRegistro ) " +
                        "values (" + codigoDistribuidor +
                        "," + db.ValueForSql(lf.NumFactura) +
                        "," + db.ValueForSql(lf.Ejercicio) +
                        "," + db.ValueForSqlAsNumeric(lf.NumLinea) +
                        "," + db.ValueForSql(lf.CodigoProducto) +
                        "," + db.ValueForSqlAsNumeric(lf.Cantidad) +
                        "," + db.ValueForSql(lf.UM) +
                        "," + db.ValueForSqlAsNumeric(lf.Peso) +
                        "," + db.ValueForSql(lf.UMPeso) +
                        "," + db.ValueForSqlAsNumeric(lf.Volumen) +
                        "," + db.ValueForSql(lf.UMVolumen) +
                        "," + db.ValueForSqlAsNumeric(lf.PrecioBase) +
                        "," + db.ValueForSqlAsNumeric(lf.Descuentos) +
                        "," + db.ValueForSqlAsNumeric(lf.PrecioBrutoTotal) +
                        "," + db.DateForSql(lf.FechaEntrega) +
                        "," + db.ValueForSql(lf.Almacen) +
                        "," + db.ValueForSql(lf.Lote) +
                        "," + db.DateForSql(lf.FechaCaducidad) +
                        "," + db.ValueForSql(lf.CodigoPostal) +
                        "," + db.ValueForSql(lf.Direccion) +
                        "," + db.ValueForSql(lf.TipoCalle) +
                        "," + db.ValueForSql(lf.Calle) +
                        "," + db.ValueForSql(lf.Numero) +
                        "," + db.ValueForSql(!string.IsNullOrEmpty(CodigoPais) ? CodigoPais : lf.CodigoPais) +
                        "," + db.ValueForSql(lf.Ruta) +
                        "," + db.ValueForSql(lf.CodigoComercial) +
                        "," + db.ValueForSql(lf.EjercicioEntrega) +
                        "," + db.ValueForSql(lf.NumEntrega) +
                        "," + db.ValueForSqlAsNumeric(lf.NumLineaEntrega) +
                        "," + db.ValueForSqlAsNumeric(lf.CosteDistribuidor) +
                        "," + db.ValueForSql(lf.DocRelacionado) +
                        "," + db.ValueForSql(lf.CodigoCliente) +
                        "," + db.DateForSql(lf.FechaFra) +
                        "," + db.ValueForSql(lf.UM2) +
                        "," + db.ValueForSqlAsNumeric(lf.Cantidad2) +
                        "," + db.ValueForSqlAsNumeric(lf.PrecioBase2) +
                        "," + db.ValueForSql(lf.TipoVenta) +
                        "," + db.ValueForSql(lf.MotivoAbono) +
                        "," + db.ValueForSql(lf.CodigoPromocion) +
                        "," + db.ValueForSql(lf.Libre1) +
                        "," + db.ValueForSql(lf.Libre2) +
                        "," + db.ValueForSql(lf.Libre3) +
                        "," + db.ValueForSqlAsNumeric(lf.CantLibre1) +
                        "," + db.ValueForSqlAsNumeric(lf.CantLibre2) +
                        "," + db.ValueForSqlAsNumeric(lf.CantLibre3) +
                        "," + db.ValueForSql(lf.TipoAcuerdoLiq) +
                        "," + db.ValueForSql(lf.SubtipoAcuerdoLiq) +
                        "," + db.ValueForSqlAsNumeric(lf.ImporteLiq) +
                        "," + db.ValueForSql(lf.TipoAcuerdoLiq2) +
                        "," + db.ValueForSql(lf.SubtipoAcuerdoLiq2) +
                        "," + db.ValueForSqlAsNumeric(lf.ImporteLiq2) +
                        "," + db.ValueForSql(lf.TipoAcuerdoLiq3) +
                        "," + db.ValueForSql(lf.SubtipoAcuerdoLiq3) +
                        "," + db.ValueForSqlAsNumeric(lf.ImporteLiq3) +
                        "," + db.ValueForSqlAsNumeric(lf.ValorLibre1Liq) +
                        "," + db.ValueForSqlAsNumeric(lf.ValorLibre2Liq) +
                        "," + db.ValueForSqlAsNumeric(lf.ValorLibre3Liq) +
                        "," + db.ValueForSqlAsNumeric(lf.ValorLibre4Liq) +
                        "," + db.ValueForSqlAsNumeric(lf.ValorLibre5Liq) +
                        "," + db.ValueForSqlAsNumeric(lf.ValorLibre6Liq) +
                        "," + db.ValueForSqlAsNumeric(lf.ValorLibre7Liq) +
                        "," + db.ValueForSqlAsNumeric(lf.ValorLibre8Liq) +
                        "," + db.ValueForSqlAsNumeric(lf.ValorLibre9Liq) +
                        "," + db.ValueForSqlAsNumeric(lf.ValorLibre10Liq) +
                        "," + db.ValueForSql(lf.ObservacionesLiq) +
                        "," + db.ValueForSql(lf.TipoDatoRegistro) +
                        ") ";
            string key = db.ValueForSql(lf.NumFactura) + ";" + db.ValueForSql(lf.Ejercicio);
            if (!htFacturasConErrores.ContainsKey(key))
                htFacturasConErrores.Add(key, key);
        }
        else
        {
            sql = "update ProductosFacturasConErrores SET " +
                    "CodigoProducto=" + db.ValueForSql(lf.CodigoProducto) +
                    ",Cantidad=" + db.ValueForSqlAsNumeric(lf.Cantidad) +
                    ",UM=" + db.ValueForSql(lf.UM) +
                    ",Peso=" + db.ValueForSqlAsNumeric(lf.Peso) +
                    ",UMPeso=" + db.ValueForSql(lf.UMPeso) +
                    ",Volumen=" + db.ValueForSqlAsNumeric(lf.Volumen) +
                    ",UMVolumen=" + db.ValueForSql(lf.UMVolumen) +
                    ",PrecioBase=" + db.ValueForSqlAsNumeric(lf.PrecioBase) +
                    ",Descuentos=" + db.ValueForSqlAsNumeric(lf.Descuentos) +
                    ",PrecioBrutoTotal=" + db.ValueForSqlAsNumeric(lf.PrecioBrutoTotal) +
                    ",FechaEntrega=" + db.DateForSql(lf.FechaEntrega) +
                    ",Almacen=" + db.ValueForSql(lf.Almacen) +
                    ",Lote=" + db.ValueForSql(lf.Lote) +
                    ",FechaCaducidad=" + db.DateForSql(lf.FechaCaducidad) +
                    ",CodigoPostal=" + db.ValueForSql(lf.CodigoPostal) +
                    ",Direccion=" + db.ValueForSql(lf.Direccion) +
                    ",TipoCalle=" + db.ValueForSql(lf.TipoCalle) +
                    ",Calle=" + db.ValueForSql(lf.Calle) +
                    ",Numero=" + db.ValueForSql(lf.Numero) +
                    ",CodigoPais=" + db.ValueForSql(!string.IsNullOrEmpty(CodigoPais) ? CodigoPais : lf.CodigoPais) +
                    ",Ruta=" + db.ValueForSql(lf.Ruta) +
                    ",CodigoComercial=" + db.ValueForSql(lf.CodigoComercial) +
                    ",EjercicioEntrega=" + db.ValueForSql(lf.EjercicioEntrega) +
                    ",NumEntrega=" + db.ValueForSql(lf.NumEntrega) +
                    ",NumLineaEntrega=" + db.ValueForSqlAsNumeric(lf.NumLineaEntrega) +
                    ",CosteDistribuidor=" + db.ValueForSqlAsNumeric(lf.CosteDistribuidor) +
                    ",DocRelacionado=" + db.ValueForSql(lf.DocRelacionado) +
                    ",CodigoCliente=" + db.ValueForSql(lf.CodigoCliente) + " " +
                    ",FechaFactura=" + db.DateForSql(lf.FechaFra) +
                    ",UM2=" + db.ValueForSql(lf.UM2) +
                    ",Cantidad2=" + db.ValueForSqlAsNumeric(lf.Cantidad2) +
                    ",PrecioBase2=" + db.ValueForSqlAsNumeric(lf.PrecioBase2) +
                    ",TipoVenta=" + db.ValueForSql(lf.TipoVenta) + " " +
                    ",MotivoAbono=" + db.ValueForSql(lf.MotivoAbono) + " " +
                    ",CodigoPromocion=" + db.ValueForSql(lf.CodigoPromocion) + " " +
                    ",Libre1=" + db.ValueForSql(lf.Libre1) + " " +
                    ",Libre2=" + db.ValueForSql(lf.Libre2) + " " +
                    ",Libre3=" + db.ValueForSql(lf.Libre3) + " " +
                    ",CantLibre1=" + db.ValueForSqlAsNumeric(lf.CantLibre1) + " " +
                    ",CantLibre2=" + db.ValueForSqlAsNumeric(lf.CantLibre2) + " " +
                    ",CantLibre3=" + db.ValueForSqlAsNumeric(lf.CantLibre3) + " " +
                    ",TipoAcuerdo1=" + db.ValueForSql(lf.TipoAcuerdoLiq) + " " +
                    ",SubtipoAcuerdo1=" + db.ValueForSql(lf.SubtipoAcuerdoLiq) + " " +
                    ",ImporteLiquidar1=" + db.ValueForSqlAsNumeric(lf.ImporteLiq) + " " +
                    ",TipoAcuerdo2=" + db.ValueForSql(lf.TipoAcuerdoLiq2) + " " +
                    ",SubtipoAcuerdo2=" + db.ValueForSql(lf.SubtipoAcuerdoLiq2) + " " +
                    ",ImporteLiquidar2=" + db.ValueForSqlAsNumeric(lf.ImporteLiq2) + " " +
                    ",TipoAcuerdo3=" + db.ValueForSql(lf.TipoAcuerdoLiq3) + " " +
                    ",SubtipoAcuerdo3=" + db.ValueForSql(lf.SubtipoAcuerdoLiq3) + " " +
                    ",ImporteLiquidar3=" + db.ValueForSqlAsNumeric(lf.ImporteLiq3) + " " +
                    ",ValorLibre1Liq=" + db.ValueForSqlAsNumeric(lf.ValorLibre1Liq) + " " +
                    ",ValorLibre2Liq=" + db.ValueForSqlAsNumeric(lf.ValorLibre2Liq) + " " +
                    ",ValorLibre3Liq=" + db.ValueForSqlAsNumeric(lf.ValorLibre3Liq) + " " +
                    ",ValorLibre4Liq=" + db.ValueForSqlAsNumeric(lf.ValorLibre4Liq) + " " +
                    ",ValorLibre5Liq=" + db.ValueForSqlAsNumeric(lf.ValorLibre5Liq) + " " +
                    ",ValorLibre6Liq=" + db.ValueForSqlAsNumeric(lf.ValorLibre6Liq) + " " +
                    ",ValorLibre7Liq=" + db.ValueForSqlAsNumeric(lf.ValorLibre7Liq) + " " +
                    ",ValorLibre8Liq=" + db.ValueForSqlAsNumeric(lf.ValorLibre8Liq) + " " +
                    ",ValorLibre9Liq=" + db.ValueForSqlAsNumeric(lf.ValorLibre9Liq) + " " +
                    ",ValorLibre10Liq=" + db.ValueForSqlAsNumeric(lf.ValorLibre10Liq) + " " +
                    ",ObservacionesLiq=" + db.ValueForSql(lf.ObservacionesLiq) + " " +
                    ",TipoRegistro=" + db.ValueForSql(lf.TipoDatoRegistro) + " " +
                    ",FechaModificacion=" + db.SysDate() + " " +
                    " WHERE IdcAgente=" + codigoDistribuidor +
                    " and NumFactura=" + db.ValueForSql(lf.NumFactura) +
                    " and Ejercicio=" + db.ValueForSql(lf.Ejercicio) +
                    " and NumLinea=" + db.ValueForSqlAsNumeric(lf.NumLinea) +
                    " ";
        }
        db.ExecuteSql(sql, agent, GetSipTypeName());
    }

    /// <summary>
    /// Eliminar un productos facturas con errores
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="lf">registro</param>
    private void BorrarProductoFacturaConErrores(Database db, RecordLineasFacturasDistribuidor lf)
    {
        if (htFacturasConErrores.ContainsKey(db.ValueForSql(lf.NumFactura) + ";" + db.ValueForSql(lf.Ejercicio)))
        {
            string sql = "Delete from ProductosFacturasConErrores " +
                    " where IdcAgente=" + codigoDistribuidor +
                    " and NumFactura=" + db.ValueForSql(lf.NumFactura) +
                    " and Ejercicio=" + db.ValueForSql(lf.Ejercicio) +
                    " and NumLinea=" + db.ValueForSqlAsNumeric(lf.NumLinea) +
                    " ";
            db.ExecuteSql(sql, agent, GetSipTypeName());
        }
    }

    private void BorrarLineasFacturasPorPeriodo(Database db, string pIdcAgente, string pFecIni, string pFecFin, string pIdcFabricante)
    {
        BorrarLineasFacturasPorPeriodo(db, pIdcAgente, pFecIni, pFecFin, pIdcFabricante, string.Empty);
    }

    private void BorrarLineasFacturasPorPeriodo(Database db, string pIdcAgente, string pFecIni, string pFecFin, string pIdcFabricante, string tipo)
    {
        BorrarLineasFacturasPorPeriodo(db, pIdcAgente, pFecIni, pFecFin, pIdcFabricante, DateTime.MinValue, tipo);
    }

    private void BorrarLineasFacturasPorPeriodo(Database db, string pIdcAgente, string pFecIni, string pFecFin, string pIdcFabricante, DateTime inicioIntegracion)
    {
        BorrarLineasFacturasPorPeriodo(db, pIdcAgente, pFecIni, pFecFin, pIdcFabricante, inicioIntegracion, string.Empty);
    }

    private void BorrarLineasFacturasPorPeriodo(Database db, string pIdcAgente, string pFecIni, string pFecFin, string pIdcFabricante, DateTime inicioIntegracion, string tipo)
    {
        string sql = "";
        //Controlamos si no se recibe fabricante y el distribuidor está conectado para más de uno dejamos log informando pero no realizamos RESET
        bool continuar = true;
        if (Utils.IsBlankField(pIdcFabricante))
        {
            DbDataReader reader = null;
            sql = "SELECT Codigo " +
                  "  FROM ClasifInterAgentes  " +
                  " WHERE IdcAgenteOrigen = " + pIdcAgente;
            reader = db.GetDataReader(sql);
            int count = 0;
            while (reader.Read())
                count++;            
            reader.Close();
            reader = null;
            if (count > 1)
            {
                continuar = false;
                Log2 log = Globals.GetInstance().GetLog2();
                string myAlertMsg = "Error en línea factura distribuidor. No se ha aplicado el RESET de facturas porque el agente {0} está conectado con más de un fabricante, y no se ha indicado por cual se quiere realizar.";
                string[] aValores = new string[] { pIdcAgente };
                log.Trace(agent, GetSipTypeName(), "FADI0036", myAlertMsg, aValores);
            }
        }
        if (continuar)
        {
            string sqlWhere2 = " where IdcAgente=" + pIdcAgente;

            if (!Utils.IsBlankField(tipo))
            {
                sqlWhere2 += " AND NumFactura like '" + tipo + "%' ";
            }

            if (!Utils.IsBlankField(pFecIni))
            {
                if (db.GetDate(pFecIni) != null)
                {
                    sqlWhere2 += " AND FechaFactura >=  " + db.DateForSql(db.GetDate(pFecIni).ToString()) + " ";
                }
            }
            if (!Utils.IsBlankField(pFecFin))
            {
                if (db.GetDate(pFecFin) != null)
                {
                    sqlWhere2 += " AND FechaFactura <=  " + db.DateForSql(db.GetDate(pFecFin).ToString()) + " ";
                }
            }

            if (inicioIntegracion > DateTime.MinValue)
                sqlWhere2 += " AND FechaModificacion < " + db.DateTimeForSql(inicioIntegracion.ToString()) + " ";

            string sqlWhere = sqlWhere2;
            if (!Utils.IsBlankField(pIdcFabricante))
                sqlWhere += " AND IdcFabricante=" + pIdcFabricante + " ";

            //----------------------------------------------------------------------------------------------------------------------------
            //Movemos las líneas de facturas que se van a eliminar en caso de que se hayan enviado previamente al fabricante
            // De ésta forma podremos enviar mediante el sipout también información de éstas facturas eliminadas
            //----------------------------------------------------------------------------------------------------------------------------      
            bool ctrlFacturasEliminadas = true;
            if (!Utils.IsBlankField(pIdcFabricante))
            {
                Fabricante fabAux = new Fabricante();
                fabAux.ObtenerParametros(db, pIdcFabricante);
                ctrlFacturasEliminadas = fabAux.FABCtrlFacturasEliminadas == "S";
            }
            if (ctrlFacturasEliminadas)
            {
                sql = "INSERT INTO ProductosFacturasEliminadas " +
                                      " (IdcFabricante " +
                                      " ,IdcAgente " +
                                      " ,NumFactura " +
                                      " ,Ejercicio " +
                                      " ,NumLinea " +
                                      " ,IdcClienteFinal " +
                                      " ,FechaFactura " +
                                      " ,IdcProducto " +
                                      " ,Cantidad " +
                                      " ,UMc " +
                                      " ,Peso " +
                                      " ,UMcPeso " +
                                      " ,Volumen " +
                                      " ,UMcVolumen " +
                                      " ,PrecioBase " +
                                      " ,Descuentos " +
                                      " ,PrecioBrutoTotal " +
                                      " ,FechaEntrega " +
                                      " ,Almacen " +
                                      " ,Lote " +
                                      " ,FechaCaducidad " +
                                      " ,CodigoPostal " +
                                      " ,TipoCalle " +
                                      " ,Calle " +
                                      " ,Numero " +
                                      " ,Provincia " +
                                      " ,CodigoPais " +
                                      " ,Ruta " +
                                      " ,CodigoComercial " +
                                      " ,EjercicioEntrega " +
                                      " ,NumEntrega " +
                                      " ,NumLineaEntrega " +
                                      " ,CosteDistribuidor " +
                                      " ,DocRelacionado " +
                                      " ,Direccion " +
                                      " ,CantEstadistica " +
                                      " ,UMEstadistica " +
                                      " ,ProductoDistribuidor " +
                                      " ,UMDistribuidor " +
                                      " ,Poblacion " +
                                      " ,Status " +
                                      " ,FechaInsercion " +
                                      " ,FechaSalida " +
                                      " ,CantEstadistica2 " +
                                      " ,UMEstadistica2 " +
                                      " ,CantEstadistica3 " +
                                      " ,UMEstadistica3 " +
                                      " ,FechaModificacion " +
                                      " ,CodigoCliente " +
                                      " ,TipoVenta " +
                                      " ,IdcTipoVenta " +
                                      " ,MotivoAbono " +
                                      " ,IdcMotivoAbono " +
                                      " ,CodigoPromocion " +
                                      " ,IdcCodigoPromocion " +
                                      " ,Libre1 " +
                                      " ,Libre2 " +
                                      " ,Libre3 " +
                                      " ,CantLibre1 " +
                                      " ,CantLibre2 " +
                                      " ,CantLibre3 " +
                                      " ,IndLinFacturaImputada " +
                                      " ,ProductoKitDistribuidor " +
                                      " ,CantEstadisticaKit " +
                                      " ,PrecioBaseKit " +
                                      " ,DescuentosKit " +
                                      " ,PrecioBrutoTotalKit " +
                                      " ,PesoCalculado " +
                                      " ,VolumenCalculado) ";
                sql += " SELECT   IdcFabricante " +
                                  " ,IdcAgente " +
                                  " ,NumFactura " +
                                  " ,Ejercicio " +
                                  " ,NumLinea " +
                                  " ,IdcClienteFinal " +
                                  " ,FechaFactura " +
                                  " ,IdcProducto " +
                                  " ,Cantidad " +
                                  " ,UMc " +
                                  " ,Peso " +
                                  " ,UMcPeso " +
                                  " ,Volumen " +
                                  " ,UMcVolumen " +
                                  " ,PrecioBase " +
                                  " ,Descuentos " +
                                  " ,PrecioBrutoTotal " +
                                  " ,FechaEntrega " +
                                  " ,Almacen " +
                                  " ,Lote " +
                                  " ,FechaCaducidad " +
                                  " ,CodigoPostal " +
                                  " ,TipoCalle " +
                                  " ,Calle " +
                                  " ,Numero " +
                                  " ,Provincia " +
                                  " ,CodigoPais " +
                                  " ,Ruta " +
                                  " ,CodigoComercial " +
                                  " ,EjercicioEntrega " +
                                  " ,NumEntrega " +
                                  " ,NumLineaEntrega " +
                                  " ,CosteDistribuidor " +
                                  " ,DocRelacionado " +
                                  " ,Direccion " +
                                  " ,CantEstadistica " +
                                  " ,UMEstadistica " +
                                  " ,ProductoDistribuidor " +
                                  " ,UMDistribuidor " +
                                  " ,Poblacion " +
                                  " ,Status " +
                                  " ,FechaInsercion " +
                                  " ,FechaSalida " +
                                  " ,CantEstadistica2 " +
                                  " ,UMEstadistica2 " +
                                  " ,CantEstadistica3 " +
                                  " ,UMEstadistica3 " +
                                  " ,FechaModificacion " +
                                  " ,CodigoCliente " +
                                  " ,TipoVenta " +
                                  " ,IdcTipoVenta " +
                                  " ,MotivoAbono " +
                                  " ,IdcMotivoAbono " +
                                  " ,CodigoPromocion " +
                                  " ,IdcCodigoPromocion " +
                                  " ,Libre1 " +
                                  " ,Libre2 " +
                                  " ,Libre3 " +
                                  " ,CantLibre1 " +
                                  " ,CantLibre2 " +
                                  " ,CantLibre3 " +
                                  " ,IndLinFacturaImputada " +
                                  " ,ProductoKitDistribuidor " +
                                  " ,CantEstadisticaKit " +
                                  " ,PrecioBaseKit " +
                                  " ,DescuentosKit " +
                                  " ,PrecioBrutoTotalKit " +
                                  " ,PesoCalculado " +
                                  " ,VolumenCalculado " +
                                  " FROM ProductosFacturas ";
                sql += sqlWhere;

                sql += " AND Status='" + Constants.ESTADO_ENVIADO + "'";

                db.ExecuteSql(sql, agent, GetSipTypeName());
            }

            sql = "Delete from ProductosFacturas " + sqlWhere;

            db.ExecuteSql(sql, agent, GetSipTypeName());

            //Eliminamos también facturas con errrores
            sql = "Delete from ProductosFacturasConErrores " + sqlWhere2;

            if (!Utils.IsBlankField(pIdcFabricante))
                sql += " AND exists " +
                        "     (SELECT 1 " +
                        "        FROM ProductosAgentes " +
                        "        LEFT JOIN Productos on ProductosAgentes.IdcProducto=Productos.IdcProducto " +
                        "       WHERE ProductosFacturasConErrores.IdcAgente=ProductosAgentes.IdcAgente " +
                        "         AND ProductosFacturasConErrores.CodigoProducto=ProductosAgentes.Codigo " +
                        "         AND Productos.IdcFabricante=" + pIdcFabricante +
                        "       UNION " +
                        "      SELECT 1 " +
                        "        FROM ProductosAgentesNoExistentes " +
                        "        LEFT JOIN Productos on ProductosAgentesNoExistentes.EAN13=Productos.EAN13 " +
                        "        LEFT JOIN ClasifInterAgentes on ClasifInterAgentes.IdcAgenteOrigen = ProductosAgentesNoExistentes.IdcAgente " +
                        "                                    And ClasifInterAgentes.Codigo = ProductosAgentesNoExistentes.Fabricante " +
                        "       WHERE ProductosFacturasConErrores.IdcAgente = ProductosAgentesNoExistentes.IdcAgente " +
                        "         AND ProductosFacturasConErrores.CodigoProducto = ProductosAgentesNoExistentes.CodigoProducto " +
                        "         AND (Productos.IdcFabricante=" + pIdcFabricante + " OR ClasifInterAgentes.IdcAgenteDestino=" + pIdcFabricante + ")" +
                        "     ) ";
            db.ExecuteSql(sql, agent, GetSipTypeName());
        }
    }

    private void BorrarLineasLiquidacionesPorPeriodo(Database db, string pIdcAgente, string pFecIni, string pFecFin, string pIdcFabricante)
    {
        string sql = "";
        string sWhere = "IdcFabricante = " + pIdcFabricante + " AND IdcDistribuidor = " + pIdcAgente + " ";
        string sWhereCab = sWhere;
        
        if (!Utils.IsBlankField(pFecIni))
        {
            if (db.GetDate(pFecIni) != null)
            {
                sWhereCab += " AND FechaDesde >=  " + db.DateForSql(db.GetDate(pFecIni).ToString()) + " ";
            }
        }
        if (!Utils.IsBlankField(pFecFin))
        {
            if (db.GetDate(pFecFin) != null)
            {
                sWhereCab += " AND FechaHasta <=  " + db.DateForSql(db.GetDate(pFecFin).ToString()) + " ";
            }
        }

        sql = "DELETE FROM LiquidacionesAcuerdosDetalle WHERE " + sWhere +
            " AND NumLiquidacion IN (SELECT NumLiquidacion FROM LiquidacionesAcuerdos WHERE " + sWhereCab + ")";
        db.ExecuteSql(sql, agent, GetSipTypeName());

        sql = "DELETE FROM LiquidacionesAcuerdos WHERE " + sWhereCab;
        db.ExecuteSql(sql, agent, GetSipTypeName());
    }


    private void BorrarLineasFacturasPorNumFactura(Database db, string pIdcFabricante, string pIdcAgente, string pNumFactura, string pEjercicio)
    {
        string sql = "";
        string sWhere = "IdcFabricante = " + pIdcFabricante + " AND IdcAgente = " + pIdcAgente + " AND NumFactura = " + db.ValueForSql(pNumFactura) + " AND Ejercicio = " + db.ValueForSql(pEjercicio);
        string sWhere2 = "IdcAgente = " + pIdcAgente + " AND NumFactura = " + db.ValueForSql(pNumFactura) + " AND Ejercicio = " + db.ValueForSql(pEjercicio);

        sql = "DELETE FROM ProductosFacturas WHERE " + sWhere;
        db.ExecuteSql(sql, agent, GetSipTypeName());

        //Eliminamos también facturas con errrores
        sql = "DELETE FROM ProductosFacturasConErrores WHERE " + sWhere2;
        sql += " AND EXISTS " +
                "     (SELECT 1 " +
                "        FROM ProductosAgentes " +
                "        LEFT JOIN Productos on ProductosAgentes.IdcProducto=Productos.IdcProducto " +
                "       WHERE ProductosFacturasConErrores.IdcAgente=ProductosAgentes.IdcAgente " +
                "         AND ProductosFacturasConErrores.CodigoProducto=ProductosAgentes.Codigo " +
                "         AND Productos.IdcFabricante=" + pIdcFabricante +
                "       UNION " +
                "      SELECT 1 " +
                "        FROM ProductosAgentesNoExistentes " +
                "        LEFT JOIN Productos on ProductosAgentesNoExistentes.EAN13=Productos.EAN13 " +
                "        LEFT JOIN ClasifInterAgentes on ClasifInterAgentes.IdcAgenteOrigen = ProductosAgentesNoExistentes.IdcAgente " +
                "                                    And ClasifInterAgentes.Codigo = ProductosAgentesNoExistentes.Fabricante " +
                "       WHERE ProductosFacturasConErrores.IdcAgente = ProductosAgentesNoExistentes.IdcAgente " +
                "         AND ProductosFacturasConErrores.CodigoProducto = ProductosAgentesNoExistentes.CodigoProducto " +
                "         AND (Productos.IdcFabricante=" + pIdcFabricante + " OR ClasifInterAgentes.IdcAgenteDestino=" + pIdcFabricante + ")" +
                "     ) ";
        db.ExecuteSql(sql, agent, GetSipTypeName());
    }

    /// <summary>
    /// Validar el formato de un registro
    /// </summary>
    /// <param name="rec">registro</param>
    public bool ValidateMaster(CommonRecord rec)
    {
        return true;
    }

    /// <summary>
    /// Validar el formato de un registro
    /// </summary>
    /// <param name="rec">registro</param>
    public bool ValidateSlave(CommonRecord rec)
    {
        return true;
    }

    /// <summary>
    /// Procesar un fichero de manera masiva
    /// </summary>
    /// <param name="rec">registro</param>
    public void ProcessBulk(string filename, CommonRecord rec)
    {
        modeBulk = true;
        //Preparamos la tabla donde insertar los datos BULK
        //-------------------------------------------------
        Database db = Globals.GetInstance().GetDatabase();
        string table = "ProductosFacturas";
        string sql;
        try
        {            
            //Hacemos la inserción BULK
            //-------------------------------------------------            
            DataTable dt = new DataTable();
            string line = null;
            int ix = 0;

            //Número campos tabla final para montar con la misma estructura el datatable intermedio
            sql = "SELECT column_name FROM information_schema.columns WHERE table_name='" + table + "' order by ordinal_position";
            DbDataReader cursor = db.GetDataReader(sql);
            int numCol = 0;
            string column = "";
            Hashtable htColumns = new Hashtable(10);
            Hashtable htColumnsNum = new Hashtable(10);
            while (cursor.Read())
            {
                column = db.GetFieldValue(cursor, 0).ToLower().Trim();
                if (!htColumns.ContainsKey(numCol))
                    htColumns.Add(numCol, column);
                if (!htColumnsNum.ContainsKey(column))
                    htColumnsNum.Add(column, numCol);
                numCol++;
            }
            using (StreamReader sr = File.OpenText(filename))
            {
                while ((line = sr.ReadLine()) != null)
                {                    
                    rec.MapRow(line);
                    ProcessSlave(rec); //Llamada a la función comuna de procesado línea a línea
                    if (resultadoSlave && doBulk)
                    {
                        if (!(ix == 0 && mbulk.NumFactura.StartsWith(Constants.INDICADOR_RESET)))
                        {
                            if (ix == 0)
                            {
                                for (int i = 0; i < numCol; i++)
                                {
                                    dt.Columns.Add(new DataColumn());
                                }
                                ix++;
                            }
                            DataRow r = dt.NewRow();
                            for (int i = 0; i < numCol; i++)
                            {
                                if (htColumns[i].ToString() == "idcagente")
                                    r[i] = codigoDistribuidor;
                                else if (htColumns[i].ToString() == "numlinea" || htColumns[i].ToString() == "cantidad" || htColumns[i].ToString() == "preciobase" ||
                                         htColumns[i].ToString() == "descuentos" || htColumns[i].ToString() == "preciobrutototal" || htColumns[i].ToString() == "numlineaentrega" ||
                                         htColumns[i].ToString() == "costedistribuidor" || htColumns[i].ToString() == "cantlibre1" || htColumns[i].ToString() == "cantlibre2" ||
                                         htColumns[i].ToString() == "cantlibre3")
                                {
                                    double c;
                                    string s = mbulk.GetNumericValue(htColumns[i].ToString());
                                    if (s.ToUpper().Contains("E-"))
                                        r[i] = 0;
                                    else
                                    {
                                        if (double.TryParse(s, out c))
                                            r[i] = c;
                                        else
                                            r[i] = null;
                                    }
                                }
                                else if (htColumns[i].ToString() == "idcclientefinal")
                                    r[i] = idcClienteFinal;
                                else if (htColumns[i].ToString() == "fechafactura")
                                {
                                    string dtStr = db.DateForSql(fechaFactura).Replace("'", "");
                                    if (!string.IsNullOrEmpty(dtStr.Trim()) && dtStr.Trim().ToUpper() != "NULL")
                                        r[i] = dtStr;
                                    else
                                        r[i] = null;
                                }
                                else if (htColumns[i].ToString() == "idcproducto")
                                    r[i] = codigoProducto;
                                else if (htColumns[i].ToString() == "umc")
                                    r[i] = UMProducto;
                                else if (htColumns[i].ToString() == "peso")
                                {
                                    r[i] = peso.PesoTotal;
                                }
                                else if (htColumns[i].ToString() == "umcpeso")
                                    r[i] = peso.UMcPeso;
                                else if (htColumns[i].ToString() == "volumen")
                                {
                                    r[i] = volumen.VolumenTotal;
                                }
                                else if (htColumns[i].ToString() == "umcvolumen")
                                    r[i] = volumen.UMcVolumen;
                                else if (htColumns[i].ToString() == "fechaentrega" || htColumns[i].ToString() == "fechacaducidad")
                                {
                                    string dtStr = db.DateForSql(mbulk.GetValue(htColumns[i].ToString())).Replace("'", "");
                                    if (!string.IsNullOrEmpty(dtStr.Trim()) && dtStr.Trim().ToUpper() != "NULL")
                                        r[i] = dtStr;
                                    else
                                        r[i] = null;
                                }
                                else if (htColumns[i].ToString() == "codigopostal")
                                    r[i] = CodigoPostal;
                                else if (htColumns[i].ToString() == "tipocalle")
                                    r[i] = TipoCalle;
                                else if (htColumns[i].ToString() == "calle")
                                    r[i] = Calle.ToUpper();
                                else if (htColumns[i].ToString() == "numero")
                                    r[i] = Numero;
                                else if (htColumns[i].ToString() == "provincia")
                                    r[i] = Provincia;
                                else if (htColumns[i].ToString() == "codigopais")
                                    r[i] = CodigoPais;
                                else if (htColumns[i].ToString() == "direccion")
                                    r[i] = Direccion.ToUpper();
                                else if (htColumns[i].ToString() == "cantestadistica")
                                {
                                    double c;
                                    if (double.TryParse(CantidadEstadistica, out c))
                                        r[i] = c;
                                    else
                                        r[i] = null;
                                }
                                else if (htColumns[i].ToString() == "umestadistica")
                                    r[i] = UMEstadistica;
                                else if (htColumns[i].ToString() == "cantestadistica2")
                                {
                                    double c;
                                    if (double.TryParse(CantidadEstadistica2, out c))
                                        r[i] = c;
                                    else
                                        r[i] = null;
                                }
                                else if (htColumns[i].ToString() == "umestadistica2")
                                    r[i] = UMEstadistica2;
                                else if (htColumns[i].ToString() == "cantestadistica3")
                                {
                                    double c;
                                    if (double.TryParse(CantidadEstadistica3, out c))
                                        r[i] = c;
                                    else
                                        r[i] = null;
                                }
                                else if (htColumns[i].ToString() == "umestadistica3")
                                    r[i] = UMEstadistica3;
                                else if (htColumns[i].ToString() == "productodistribuidor")
                                    r[i] = mbulk.GetValue("codigoproducto");
                                else if (htColumns[i].ToString() == "umdistribuidor")
                                    r[i] = mbulk.GetValue("um");
                                else if (htColumns[i].ToString() == "poblacion")
                                    r[i] = datosDireccion.Poblacion.ToUpper();
                                else if (htColumns[i].ToString() == "status")
                                    r[i] = Constants.ESTADO_ACTIVO;
                                else if (htColumns[i].ToString() == "idcfabricante")
                                    r[i] = idcCodigoFabricante;
                                else if (htColumns[i].ToString() == "codigocliente")
                                    r[i] = codigoCliente;
                                else if (htColumns[i].ToString() == "pesocalculado")
                                    r[i] = (peso.HaSidoCalculado ? "S" : "N");
                                else if (htColumns[i].ToString() == "volumencalculado")
                                    r[i] = (volumen.HaSidoCalculado ? "S" : "N");
                                else if (htColumns[i].ToString() == "fechainsercion" || htColumns[i].ToString() == "fechamodificacion")
                                {
                                    string dtStr = db.DateForSql(DateTime.Now.ToString()).Replace("'", "");
                                    if (!string.IsNullOrEmpty(dtStr.Trim()) && dtStr.Trim().ToUpper() != "NULL")
                                        r[i] = dtStr;
                                    else
                                        r[i] = null;
                                }
                                else if (htColumns[i].ToString() == "fechasalida")
                                    r[i] = null;                                
                                else
                                {
                                    string value = mbulk.GetValue(htColumns[i].ToString());
                                    if (!string.IsNullOrEmpty(value))
                                        r[i] = value;
                                }
                            }
                            dt.Rows.Add(r);
                        }
                    }
                    doBulk = false;
                }
            }
            if (dt.Rows.Count > 0)
            {
                string dns = Globals.GetSQLConnectionString();
                SqlBulkCopy copy = new SqlBulkCopy(dns, SqlBulkCopyOptions.Default);
                copy.DestinationTableName = table;
                copy.WriteToServer(dt);
                copy.Close();
                dt.Clear();
            }
        }
        finally { }
    }
  }
}
