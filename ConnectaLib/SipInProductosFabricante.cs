using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Data.Common;
using System.Data.OleDb;
using System.Collections;

namespace ConnectaLib
{
  /// <summary>
  /// Clase que gestiona las entradas de productos de fabricante
  /// </summary>
  public class SipInProductosFabricante : SipIn, ISipInInterface
  {
    private ArrayList productosErroneos = new ArrayList(10);
    private string codigoFabricante = "";
    private Producto producto = new Producto();
    private UnidadMedida unidadMedida = new UnidadMedida();
    private Fabricante fabricante = new Fabricante();
    private string codigoProductoKitCentinela = "";
    private string idcProductoKit = "";
    private Hashtable hProductosKits = new Hashtable();
    private Hashtable hProductosKitsNew = new Hashtable();
    private Hashtable hProductosKitsLog = new Hashtable();

    //Clase interna para chequeo de unidades de medida
    private class UMCheck
    {
      public bool isOK = true;
      public string value = "";
    }

    //Id de SIP
    public const string ID_SIP = "SipInProductosFabricante";

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="agent">agente</param>
    public SipInProductosFabricante(string agent) : base(agent, ID_SIP)
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
      return "schProductosFabricante.xsd";
    }

    /// <summary>
    /// Obtener nombre de Sip (básicamente a efectos de log)
    /// </summary>
    /// <returns>nombre de sip</returns>
    public string GetSipTypeName()
    {
      return "Fabricante.Productos.In";
    }

    /// <summary>
    /// Pre-proceso
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    public void PreProcess(string filename)
    {
      InvokeExternalProgram(this, agent);
      fabricante.ObtenerParametros(Globals.GetInstance().GetDatabase(), agent);
      hProductosKits.Clear();
      hProductosKitsNew.Clear();
      hProductosKitsLog.Clear();
    }

    /// <summary>
    /// Post-proceso
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    public void PostProcess(string filename)
    {
        //Eliminar todas las cabeceras / líneas problemáticas
        foreach (ProductosErroneos p in productosErroneos)
        {
            BorrarProductoErroneo(p.Producto);
        }

        //Finalmente ejecutamos un programa externo personalizado
        InvokeExternalProgramPOST(this, agent);
        hProductosKits.Clear();
        hProductosKitsNew.Clear();
        
        //Generamos logs relacionado con los kits a nivel de fichero
        Log2 log = Globals.GetInstance().GetLog2();
        foreach (DictionaryEntry de in hProductosKitsLog)
        {
            double d;
            if (double.TryParse(de.Value.ToString(), out d) && d == 0)
            {
                string myAlertMsg = "Aviso en producto kit fabricante. Todos los componentes tienen proporción de importe a 0 para el producto kit {0}.";
                log.Trace(agent, GetSipTypeName(), "PFAB0021", myAlertMsg, de.Key.ToString());
            }
        }
        hProductosKitsLog.Clear();
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
        return new RecordProductosFabricante();
    }

    /// <summary>
    /// Obtener registro esclavo
    /// </summary>
    /// <returns>registro esclavo</returns>
    public CommonRecord GetSlaveRecord()
    {
        return new RecordProductosKitFabricante();
    }

    /// <summary>
    /// Obtener sección de XML para el registro maestro
    /// </summary>
    /// <returns>sección de XML</returns>
    public string GetMasterXMLSection()
    {
        return "ProductoFabricante";
    }

    /// <summary>
    /// Obtener sección de XML para el registro esclavo
    /// </summary>
    /// <returns>sección de XML</returns>
    public string GetSlaveXMLSection()
    {
        return "ComponenteProductoFabricante";
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
    /// Obtener estado actual de un producto existente
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="prodFab">registro</param>
    /// <returns>instancia de la clase UMCheck</returns>
    private string ObtenerStatusActual(Database db, string idcAgente, string idcProducto, string codigoProducto)
    {      
      DbDataReader reader = null;
      string status = "";
      try
      {        
          string sql = "Select Status " +
                       "  From ProductosAgentes " +
                       " where IdcAgente = " + idcAgente + " and IdcProducto = " + idcProducto + " and Codigo='" + codigoProducto + "'";
          reader = db.GetDataReader(sql);
          if (reader.Read()) status = db.GetFieldValue(reader, 0);                      
          reader.Close();
          reader = null;        
      }
      finally 
      {
        if (reader != null)
          reader.Close();
      }
      return status;
    }

    /// <summary>
    /// Obtener unidad de medida no primaria
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="prodFab">registro</param>
    /// <returns>instancia de la clase UMCheck</returns>
    private UMCheck ObtenerUnidadMedidaNoPrimaria(Database db, string um)
    {
        UMCheck umcheck = new UMCheck();
        umcheck.isOK = false;
        umcheck.value = "";
        DbDataReader reader = null;
        try
        {
            if (!Utils.IsBlankField(um))
            {
                if (um == Constants.VALOR_RESET_CAMPO)
                {
                    umcheck.value = um;
                    umcheck.isOK = true;
                }
                else
                {
                    string sql = "Select UMc From UMAgente Where IdcAgente = " + codigoFabricante + " " +
                                "And UMAgente = '" + um + "'";
                    reader = db.GetDataReader(sql);
                    if (reader.Read())
                    {
                        umcheck.value = db.GetFieldValue(reader, 0);
                        umcheck.isOK = true;
                    }
                    reader.Close();
                    reader = null;

                    if (!umcheck.isOK)
                    {
                        //No existe la conversión de UM. Comprobamos si la unidad de medida que nos ha pasado coincide con una UM de Connect@. 
                        sql = "select UMc from UnidadesMedida where UMc='" + um + "'";
                        if (Utils.RecordExist(sql))
                        {
                            umcheck.value = um;
                            umcheck.isOK = true;
                        }
                        else
                        {
                            string myAlertMsg = "Error en producto fabricante. Unidad de Medida {0} no existe. Se dejará la variable UMc en blanco.";
                            Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "PFAB0001", myAlertMsg, um);
                        }
                    }
                }
            }
        }
        finally 
        {
            if (reader != null)
                reader.Close();
        }
        return umcheck;
    }

    /// <summary>
    /// Procesar de un objeto interno que representa un registro de un
    /// fichero delimited o una sección XML
    /// </summary>
    /// <param name="rec">registro</param>
    public void ProcessMaster(CommonRecord rec)
    {
        string sUsuario = Constants.PREFIJO_USUARIO_AGENTE + agent;

        Log2 log = Globals.GetInstance().GetLog2();
        Database db = Globals.GetInstance().GetDatabase();

        RecordProductosFabricante productosFabricante = (RecordProductosFabricante)rec;
        codigoFabricante = agent;


        //Comprobar y obtener la acción del producto que nos ha pasado el fabricante
        string accion = Constants.ACCION_ALTA;
        if (productosFabricante.Accion.Equals(Constants.ACCION_MODIFICACION_CLASIFICADORES) ||
            productosFabricante.Accion.Equals(Constants.ACCION_MODIFICACION_CLASIFICADORES_CON_INSERT) ||
            productosFabricante.Accion.Equals(Constants.ACCION_MODIFICACION) ||
            productosFabricante.Accion.Equals(Constants.ACCION_BAJA))
        {
            accion = productosFabricante.Accion;
        }

        if (accion.Equals(Constants.ACCION_BAJA))
        {
            //Si la acción es de borrado, borrar el producto y salir
            ///////////////////////////BorrarProducto(codigoFabricante, productosFabricante.CodigoProducto);
            return;
        }

        //Comprobar si la descripción está informada, si no error y saltar este producto.
        if (!accion.Equals(Constants.ACCION_MODIFICACION_CLASIFICADORES) && Utils.IsBlankField(productosFabricante.Descripcion.Trim()))
        {
            //log.DetailedError(agent, GetSipTypeName(), "Error en producto fabricante. Descripción vacía en el producto " + productosFabricante.CodigoProducto);
            string myAlertMsg = "Error en producto fabricante. Descripción vacía en el producto {0}.";
            log.Trace(agent, GetSipTypeName(), "PFAB0002", myAlertMsg, productosFabricante.CodigoProducto); 
            return;
        }

        //Recortar la descripcion si supera la longitud de 60
        string DescProducto = "";
        if (productosFabricante.Descripcion.Trim().Length > 60)
            DescProducto = productosFabricante.Descripcion.Trim().Remove(60);
        else
            DescProducto = productosFabricante.Descripcion.Trim();
      
        //Obtener la Unidad de Medida de Connecta para el Peso, a partir de la inter.UMPeso y el CodigoFabricante y la guardaremos en la variable UMcPeso.
        UMCheck umcheck = ObtenerUnidadMedidaNoPrimaria(db, productosFabricante.UMPeso);
        string UMcPeso = umcheck.value;
        //Obtener la Unidad de Medida de Connecta para la longitud, a partir de la inter.UMLongitud y el CodigoFabricante y la guardaremos en la variable UMcLongitud.
        umcheck = ObtenerUnidadMedidaNoPrimaria(db, productosFabricante.UMLongitud);
        string UMcLongitud = umcheck.value;
        //Obtener la Unidad de Medida de Connecta para el ancho, a partir de la inter.UMAncho y el CodigoFabricante y la guardaremos en la variable UMcAncho.
        umcheck = ObtenerUnidadMedidaNoPrimaria(db, productosFabricante.UMAncho);
        string UMcAncho = umcheck.value;
        //Obtener la Unidad de Medida de Connecta para el alto, a partir de la inter.UMAlto y el CodigoFabricante y la guardaremos en la variable UMcAlto.
        umcheck = ObtenerUnidadMedidaNoPrimaria(db, productosFabricante.UMAlto);
        string UMcAlto = umcheck.value;
        //Obtener la Unidad de Medida de Connecta para el volumen, a partir de la inter.UMVolumen y el CodigoFabricante y la guardaremos en la variable UMcVolumen.
        umcheck = ObtenerUnidadMedidaNoPrimaria(db, productosFabricante.UMVolumen);
        string UMcVolumen = umcheck.value;

        //Tratar la Unidad de medida de gestión
        umcheck = ObtenerUnidadMedidaNoPrimaria(db, productosFabricante.Clasificacion12);
        string UMGestion = umcheck.value;
        //Tratar la Unidad de medida de gestión 2
        umcheck = ObtenerUnidadMedidaNoPrimaria(db, productosFabricante.Clasificacion13);
        string UMGestion2 = umcheck.value;
        //Tratar la Unidad de medida de gestión 3
        umcheck = ObtenerUnidadMedidaNoPrimaria(db, productosFabricante.Clasificacion11);
        string UMGestion3 = umcheck.value;

        //Tratar la Unidad de medida estadística 1
        umcheck = ObtenerUnidadMedidaNoPrimaria(db, productosFabricante.UMEstadistica1);
        string UMEstadistica1 = umcheck.value;
        //if (Utils.IsBlankField(UMEstadistica1))
        //    UMEstadistica1 = fabricante.UMEstadistica;
        //Tratar la Unidad de medida estadística 2
        umcheck = ObtenerUnidadMedidaNoPrimaria(db, productosFabricante.UMEstadistica2);
        string UMEstadistica2 = umcheck.value;
        //if (Utils.IsBlankField(UMEstadistica2))
        //    UMEstadistica2 = fabricante.UMEstadistica2;
        //Tratar la Unidad de medida estadística 3
        umcheck = ObtenerUnidadMedidaNoPrimaria(db, productosFabricante.UMEstadistica3);
        string UMEstadistica3 = umcheck.value;
        //if (Utils.IsBlankField(UMEstadistica3))
        //    UMEstadistica3 = fabricante.UMEstadistica3;

        //Tratar la Unidad de medida punto verde
        umcheck = ObtenerUnidadMedidaNoPrimaria(db, productosFabricante.UMPuntoVerde);
        string UMPuntoVerde = umcheck.value;

        //Comprobar si existe el CodigoProducto en la tabla ProductosAgente
        string codigoProducto = "";
        string sql = "";

        bool existe = producto.ComprobarCodigoProducto(db, codigoFabricante, productosFabricante.CodigoProducto);
        
        //Tratar fecha baja
        string fechaBaja = productosFabricante.FechaBaja;
        if ((Utils.IsBlankField(fechaBaja)) && (!Utils.IsBlankField(productosFabricante.Status)))
        {
            bool tratarFecha = false;
            bool limpiarFecha = false;
            if (existe)
            {
                string statusActual = ObtenerStatusActual(db, codigoFabricante, producto.CodigoProducto, productosFabricante.CodigoProducto);
                if ((statusActual == Constants.ESTADO_ACTIVO) && (productosFabricante.Status != Constants.ESTADO_ACTIVO)) tratarFecha = true;
                if ((statusActual != Constants.ESTADO_ACTIVO) && (productosFabricante.Status == Constants.ESTADO_ACTIVO)) limpiarFecha = true;
            }
            else
            {
                if (productosFabricante.Status != Constants.ESTADO_ACTIVO) tratarFecha = true;
                if (productosFabricante.Status == Constants.ESTADO_ACTIVO) limpiarFecha = true;
            }
            if (tratarFecha)
            {
                DateTime dt = DateTime.Now.AddMonths(int.Parse(fabricante.FABIncMesesFechaBaja));
                fechaBaja = dt.Day.ToString().PadLeft(2, '0') + "/" + dt.Month.ToString().PadLeft(2, '0') + "/" + dt.Year.ToString();
            }
            if (limpiarFecha)
            {
                fechaBaja = "limpiar";
            }
        }

        //Tratar el propietario del producto
        if (String.IsNullOrEmpty(productosFabricante.PropietarioProducto)) productosFabricante.PropietarioProducto = "P";

        //Tratar el indicador de si es totalizador
        if (String.IsNullOrEmpty(productosFabricante.EsTotalizador)) productosFabricante.EsTotalizador = "N";

        if (existe)
        {
            string UnidadMedida = "";

            umcheck = ObtenerUnidadMedidaNoPrimaria(db, productosFabricante.UM);
            UnidadMedida = umcheck.value;
            
            codigoProducto = producto.CodigoProducto;

            bool canModif = (!accion.Equals(Constants.ACCION_MODIFICACION_CLASIFICADORES) && !accion.Equals(Constants.ACCION_MODIFICACION_CLASIFICADORES_CON_INSERT));

            //Actualizar datos de ProductosAgente 
            sql = "update ProductosAgentes set " +
              "FechaModificacion=" + db.SysDate() + " " +
              ",UsuarioModificacion=" + db.ValueForSql(sUsuario) + " " +
              ",IdcFabricante=" + codigoFabricante + " " +
              (Utils.IsBlankField(DescProducto) || !canModif ? "" : ",Descripcion=" + db.ValueForSql(DescProducto)) +
              (Utils.IsBlankField(productosFabricante.DescripcionAdd) || !canModif ? "" : ",DescripcionAdd=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.DescripcionAdd, 100))) +
              (Utils.IsBlankField(productosFabricante.Status) ? "" : ",Status=" + db.ValueForSql(productosFabricante.Status)) +
              (Utils.IsBlankField(UnidadMedida) ? "" : (UnidadMedida == Constants.VALOR_RESET_CAMPO ? ",UMc=''" : ",UMc=" + db.ValueForSql(UnidadMedida))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion1) ? "" : ",Clasificacion1=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion1, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion2) ? "" : ",Clasificacion2=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion2, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion3) ? "" : ",Clasificacion3=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion3, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion4) ? "" : ",Clasificacion4=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion4, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion5) ? "" : ",Clasificacion5=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion5, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion6) ? "" : ",Clasificacion6=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion6, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion7) ? "" : ",Clasificacion7=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion7, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion8) ? "" : ",Clasificacion8=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion8, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion9) ? "" : ",Clasificacion9=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion9, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion10) ? "" : ",Clasificacion10=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion10, 20))) +
              (Utils.IsBlankField(UMGestion3) ? "" : (UMGestion3 == Constants.VALOR_RESET_CAMPO ? ",Clasificacion11=''" : ",Clasificacion11=" + db.ValueForSql(Utils.StringTruncate(UMGestion3, 20)))) +
              (Utils.IsBlankField(UMGestion) ? "" : (UMGestion == Constants.VALOR_RESET_CAMPO ? ",Clasificacion12=''" : ",Clasificacion12=" + db.ValueForSql(Utils.StringTruncate(UMGestion, 20)))) +
              (Utils.IsBlankField(UMGestion2) ? "" : (UMGestion2 == Constants.VALOR_RESET_CAMPO ? ",Clasificacion13=''" : ",Clasificacion13=" + db.ValueForSql(Utils.StringTruncate(UMGestion2, 20)))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion14) ? "" : ",Clasificacion14=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion14, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion15) ? "" : ",Clasificacion15=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion15, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion16) ? "" : ",Clasificacion16=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion16, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion17) ? "" : ",Clasificacion17=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion17, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion18) ? "" : ",Clasificacion18=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion18, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion19) ? "" : ",Clasificacion19=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion19, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion20) ? "" : ",Clasificacion20=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion20, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion21) ? "" : ",Clasificacion21=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion21, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion22) ? "" : ",Clasificacion22=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion22, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion23) ? "" : ",Clasificacion23=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion23, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion24) ? "" : ",Clasificacion24=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion24, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion25) ? "" : ",Clasificacion25=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion25, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion26) ? "" : ",Clasificacion26=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion26, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion27) ? "" : ",Clasificacion27=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion27, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion28) ? "" : ",Clasificacion28=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion28, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion29) ? "" : ",Clasificacion29=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion29, 20))) +
              (Utils.IsBlankField(productosFabricante.Clasificacion30) ? "" : ",Clasificacion30=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion30, 20))) +
              (Utils.IsBlankField(productosFabricante.Jerarquia) ? "" : ",Jerarquia=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Jerarquia, 18))) +
              (Utils.IsBlankField(productosFabricante.FechaBloqueo) ? "" : ",FechaBloqueo=" + db.DateForSql(productosFabricante.FechaBloqueo)) +
              (Utils.IsBlankField(fechaBaja) ? "" : (fechaBaja == "limpiar" ? ", FechaBaja=" + db.DateForSql("") : ", FechaBaja=" + db.DateForSql(fechaBaja))) + " " +
              (Utils.IsBlankField(productosFabricante.CodigoProductoAlt) ? "" : ",CodigoAlt=" + db.ValueForSql(productosFabricante.CodigoProductoAlt)) + " " +
              (Utils.IsBlankField(productosFabricante.CodigoProductoAgrup) ? "" : (productosFabricante.CodigoProductoAgrup == Constants.VALOR_RESET_CAMPO ? ",CodigoAgrup=''" : ",CodigoAgrup=" + db.ValueForSql(productosFabricante.CodigoProductoAgrup))) + " " +
              " where IdcAgente = " + codigoFabricante + " and IdcProducto = " + codigoProducto + " and Codigo='" + productosFabricante.CodigoProducto + "'";
            db.ExecuteSql(sql, agent, GetSipTypeName());

            if (!accion.Equals(Constants.ACCION_MODIFICACION_CLASIFICADORES) && !accion.Equals(Constants.ACCION_MODIFICACION_CLASIFICADORES_CON_INSERT))
            {
                //Actualizar datos de producto
                sql = "update Productos set " +
                  "IdcFabricante=" + codigoFabricante +
                  (Utils.IsBlankField(DescProducto) ? "" : ",Descripcion=" + db.ValueForSql(DescProducto)) +
                  ",Abreviatura=" + db.ValueForSql(Utils.StringTruncate(productosFabricante.Abreviatura, 20)) +
                  ",DescrLarga=" + db.ValueForSql(productosFabricante.DescrLarga) +
                  (Utils.IsBlankField(productosFabricante.Status) ? "" : ",Status=" + db.ValueForSql(productosFabricante.Status)) +
                  (Utils.IsBlankField(UnidadMedida) ? "" : (UnidadMedida == Constants.VALOR_RESET_CAMPO ? ",UMc=''" : ",UMc=" + db.ValueForSql(UnidadMedida))) +
                  ",EAN13=" + db.ValueForSql(productosFabricante.EAN13) +
                  ",PesoNeto=" + db.ValueForSqlAsNumeric(productosFabricante.PesoNeto) +
                  ",PesoNetoEscurrido=" + db.ValueForSqlAsNumeric(productosFabricante.PesoNetoEscurrido) +
                  ",PesoBruto=" + db.ValueForSqlAsNumeric(productosFabricante.PesoBruto) +
                  (Utils.IsBlankField(UMcPeso) ? "" : (UMcPeso == Constants.VALOR_RESET_CAMPO ? ",UMcPeso=''" : ",UMcPeso=" + db.ValueForSql(UMcPeso))) +
                  ",Longitud=" + db.ValueForSqlAsNumeric(productosFabricante.Longitud) +
                  ",Ancho=" + db.ValueForSqlAsNumeric(productosFabricante.Ancho) +
                  ",Alto=" + db.ValueForSqlAsNumeric(productosFabricante.Alto) +
                  ",Volumen=" + db.ValueForSqlAsNumeric(productosFabricante.Volumen) +
                  (Utils.IsBlankField(UMcLongitud) ? "" : (UMcLongitud == Constants.VALOR_RESET_CAMPO ? ",UMLongitud=''" : ",UMLongitud=" + db.ValueForSql(UMcLongitud))) +
                  (Utils.IsBlankField(UMcAncho) ? "" : (UMcAncho == Constants.VALOR_RESET_CAMPO ? ",UMAncho=''" : ",UMAncho=" + db.ValueForSql(UMcAncho))) +
                  (Utils.IsBlankField(UMcAlto) ? "" : (UMcAlto == Constants.VALOR_RESET_CAMPO ? ",UMAlto=''" : ",UMAlto=" + db.ValueForSql(UMcAlto))) +
                  (Utils.IsBlankField(UMcVolumen) ? "" : (UMcVolumen == Constants.VALOR_RESET_CAMPO ? ",UMVolumen=''" : ",UMVolumen=" + db.ValueForSql(UMcVolumen))) +
                  (Utils.IsBlankField(UMEstadistica1) ? "" : (UMEstadistica1 == Constants.VALOR_RESET_CAMPO ? ",UMEstadistica=''" : ",UMEstadistica=" + db.ValueForSql(UMEstadistica1))) +
                  (Utils.IsBlankField(UMEstadistica2) ? "" : (UMEstadistica2 == Constants.VALOR_RESET_CAMPO ? ",UMEstadistica2=''" : ",UMEstadistica2=" + db.ValueForSql(UMEstadistica2))) +
                  (Utils.IsBlankField(UMEstadistica3) ? "" : (UMEstadistica3 == Constants.VALOR_RESET_CAMPO ? ",UMEstadistica3=''" : ",UMEstadistica3=" + db.ValueForSql(UMEstadistica3))) +
                  ",InstrucManipulacion=" + db.ValueForSql(productosFabricante.InstrucManipulacion) +
                  ",Empresa=" + db.ValueForSql(productosFabricante.EmpresaFabricante) +
                  ",Propietario=" + db.ValueForSql(productosFabricante.PropietarioProducto) +
                  ",IndEsTotalizador=" + db.ValueForSql(productosFabricante.EsTotalizador) + " " +
                  ",Nivel=" + db.ValueForSql(productosFabricante.NivelProducto) +
                  ",CEP=" + db.ValueForSql(productosFabricante.CEP) +
                  ",PartidaArancelaria=" + db.ValueForSql(productosFabricante.PartidaArancelaria) +
                  ",Tarifa=" + db.ValueForSqlAsNumeric(productosFabricante.Tarifa) + " " +
                  (Utils.IsBlankField(UMGestion) ? "" : (UMGestion == Constants.VALOR_RESET_CAMPO ? ",UMGestion=''" : ",UMGestion=" + db.ValueForSql(UMGestion))) +
                  (Utils.IsBlankField(UMGestion2) ? "" : (UMGestion2 == Constants.VALOR_RESET_CAMPO ? ",UMGestion2=''" : ",UMGestion2=" + db.ValueForSql(UMGestion2))) +
                  (Utils.IsBlankField(UMGestion3) ? "" : (UMGestion3 == Constants.VALOR_RESET_CAMPO ? ",UMGestion3=''" : ",UMGestion3=" + db.ValueForSql(UMGestion3))) +
                  ",IndCalculaCantidades=" + db.ValueForSql(productosFabricante.IndCalculaCantidades) + " " +
                  ",IndFijaUMGestion=" + db.ValueForSql(productosFabricante.IndFijaUMGestion) + " " +
                  (Utils.IsBlankField(productosFabricante.EsKit) ? "" : (productosFabricante.EsKit == Constants.VALOR_RESET_CAMPO ? ",IndEsKit=''" : ",IndEsKit=" + db.ValueForSql((productosFabricante.EsKit == "S" || productosFabricante.EsKit == "F") ? Constants.KIT_FABRICANTE : ""))) +
                  (db.ValueForSqlAsNumeric(productosFabricante.PuntoVerde) == "NULL" ? "" : ",PuntoVerde=" + db.ValueForSqlAsNumeric(productosFabricante.PuntoVerde)) +
                  (Utils.IsBlankField(UMPuntoVerde) ? "" : (UMPuntoVerde == Constants.VALOR_RESET_CAMPO ? ",UMPuntoVerde=''" : ",UMPuntoVerde=" + db.ValueForSql(UMPuntoVerde))) +
                  " WHERE IdcProducto = " + codigoProducto;
                db.ExecuteSql(sql, agent, GetSipTypeName());

                //Eliminar los componentes para productos kit (lo hacemos sólo si se fuerza el reset)
                if (productosFabricante.EsKit == Constants.VALOR_RESET_CAMPO)
                {
                    sql = "Delete From ProductosAgentesKits where IdcAgente = " + codigoFabricante + " and CodigoProductoKit = '" + productosFabricante.CodigoProducto + "' ";
                    db.ExecuteSql(sql, agent, GetSipTypeName());
                }

                //Eliminar los registros referenciados
                if (fabricante.FABProdEliminaReferenciados == "S")
                {
                    //Todos las conversiones de UM de este producto
                    sql = "Delete From ConvUMProducto where IdcAgente = " + codigoFabricante + " and IdcProducto = " + codigoProducto;
                    db.ExecuteSql(sql, agent, GetSipTypeName());
                }
            }
            else
            {
                //Si solo estamos modificando clasificadores, tambíen tenemos que actualizar los datos de producto però en condiciones diferentes.
                sql = "update Productos set " +
                  "IdcFabricante=" + codigoFabricante +
                  (Utils.IsBlankField(UnidadMedida) ? "" : (UnidadMedida == Constants.VALOR_RESET_CAMPO ? ",UMc=''" : ",UMc=" + db.ValueForSql(UnidadMedida))) +
                  (Utils.IsBlankField(productosFabricante.EAN13) ? "" : (productosFabricante.EAN13 == Constants.VALOR_RESET_CAMPO ? ",EAN13=''" : ",EAN13=" + db.ValueForSql(productosFabricante.EAN13))) +
                  (Utils.IsBlankField(productosFabricante.PesoNeto) ? "" : (productosFabricante.PesoNeto == Constants.VALOR_RESET_CAMPO ? ",PesoNeto=NULL" : ",PesoNeto=" + db.ValueForSqlAsNumeric(productosFabricante.PesoNeto))) +
                  (Utils.IsBlankField(productosFabricante.PesoNetoEscurrido) ? "" : (productosFabricante.PesoNetoEscurrido == Constants.VALOR_RESET_CAMPO ? ",PesoNetoEscurrido=NULL" : ",PesoNetoEscurrido=" + db.ValueForSqlAsNumeric(productosFabricante.PesoNetoEscurrido))) +
                  (Utils.IsBlankField(productosFabricante.PesoBruto) ? "" : (productosFabricante.PesoBruto == Constants.VALOR_RESET_CAMPO ? ",PesoBruto=NULL" : ",PesoBruto=" + db.ValueForSqlAsNumeric(productosFabricante.PesoBruto))) +
                  (Utils.IsBlankField(UMcPeso) ? "" : (UMcPeso == Constants.VALOR_RESET_CAMPO ? ",UMcPeso=''" : ",UMcPeso=" + db.ValueForSql(UMcPeso))) +
                  (Utils.IsBlankField(productosFabricante.Longitud) ? "" : (productosFabricante.Longitud == Constants.VALOR_RESET_CAMPO ? ",Longitud=NULL" : ",Longitud=" + db.ValueForSqlAsNumeric(productosFabricante.Longitud))) +
                  (Utils.IsBlankField(productosFabricante.Ancho) ? "" : (productosFabricante.Ancho == Constants.VALOR_RESET_CAMPO ? ",Ancho=NULL" : ",Ancho=" + db.ValueForSqlAsNumeric(productosFabricante.Ancho))) +
                  (Utils.IsBlankField(productosFabricante.Alto) ? "" : (productosFabricante.Alto == Constants.VALOR_RESET_CAMPO ? ",Alto=NULL" : ",Alto=" + db.ValueForSqlAsNumeric(productosFabricante.Alto))) +
                  (Utils.IsBlankField(productosFabricante.Volumen) ? "" : (productosFabricante.Volumen == Constants.VALOR_RESET_CAMPO ? ",Volumen=NULL" : ",Volumen=" + db.ValueForSqlAsNumeric(productosFabricante.Volumen))) +
                  (Utils.IsBlankField(UMcLongitud) ? "" : (UMcLongitud == Constants.VALOR_RESET_CAMPO ? ",UMLongitud=''" : ",UMLongitud=" + db.ValueForSql(UMcLongitud))) +
                  (Utils.IsBlankField(UMcAncho) ? "" : (UMcAncho == Constants.VALOR_RESET_CAMPO ? ",UMAncho=''" : ",UMAncho=" + db.ValueForSql(UMcAncho))) +
                  (Utils.IsBlankField(UMcAlto) ? "" : (UMcAlto == Constants.VALOR_RESET_CAMPO ? ",UMAlto=''" : ",UMAlto=" + db.ValueForSql(UMcAlto))) +
                  (Utils.IsBlankField(UMcVolumen) ? "" : (UMcVolumen == Constants.VALOR_RESET_CAMPO ? ",UMVolumen=''" : ",UMVolumen=" + db.ValueForSql(UMcVolumen))) +
                  (Utils.IsBlankField(UMEstadistica1) ? "" : (UMEstadistica1 == Constants.VALOR_RESET_CAMPO ? ",UMEstadistica=''" : ",UMEstadistica=" + db.ValueForSql(UMEstadistica1))) +
                  (Utils.IsBlankField(UMEstadistica2) ? "" : (UMEstadistica2 == Constants.VALOR_RESET_CAMPO ? ",UMEstadistica2=''" : ",UMEstadistica2=" + db.ValueForSql(UMEstadistica2))) +
                  (Utils.IsBlankField(UMEstadistica3) ? "" : (UMEstadistica3 == Constants.VALOR_RESET_CAMPO ? ",UMEstadistica3=''" : ",UMEstadistica3=" + db.ValueForSql(UMEstadistica3))) +
                  (Utils.IsBlankField(productosFabricante.InstrucManipulacion) ? "" : (productosFabricante.InstrucManipulacion == Constants.VALOR_RESET_CAMPO ? ",InstrucManipulacion=''" : ",InstrucManipulacion=" + db.ValueForSql(productosFabricante.InstrucManipulacion))) +
                  (Utils.IsBlankField(productosFabricante.EmpresaFabricante) ? "" : (productosFabricante.EmpresaFabricante == Constants.VALOR_RESET_CAMPO ? ",Empresa=''" : ",Empresa=" + db.ValueForSql(productosFabricante.EmpresaFabricante))) +
                  (Utils.IsBlankField(productosFabricante.PropietarioProducto) ? "" : (productosFabricante.PropietarioProducto == Constants.VALOR_RESET_CAMPO ? ",Propietario=''" : ",Propietario=" + db.ValueForSql(productosFabricante.PropietarioProducto))) +
                  (Utils.IsBlankField(productosFabricante.EsTotalizador) ? "" : (productosFabricante.EsTotalizador == Constants.VALOR_RESET_CAMPO ? ",IndEsTotalizador=''" : ",IndEsTotalizador=" + db.ValueForSql(productosFabricante.EsTotalizador))) +
                  (Utils.IsBlankField(productosFabricante.NivelProducto) ? "" : (productosFabricante.NivelProducto == Constants.VALOR_RESET_CAMPO ? ",Nivel=''" : ",Nivel=" + db.ValueForSql(productosFabricante.NivelProducto))) +
                  (Utils.IsBlankField(productosFabricante.CEP) ? "" : (productosFabricante.CEP == Constants.VALOR_RESET_CAMPO ? ",CEP=''" : ",CEP=" + db.ValueForSql(productosFabricante.CEP))) +
                  (Utils.IsBlankField(productosFabricante.PartidaArancelaria) ? "" : (productosFabricante.PartidaArancelaria == Constants.VALOR_RESET_CAMPO ? ",PartidaArancelaria=''" : ",PartidaArancelaria=" + db.ValueForSql(productosFabricante.PartidaArancelaria))) +
                  (Utils.IsBlankField(productosFabricante.Tarifa) ? "" : (productosFabricante.Tarifa == Constants.VALOR_RESET_CAMPO ? ",Tarifa=NULL" : ",Tarifa=" + db.ValueForSqlAsNumeric(productosFabricante.Tarifa))) +
                  (Utils.IsBlankField(UMGestion) ? "" : (UMGestion == Constants.VALOR_RESET_CAMPO ? ",UMGestion=''" : ",UMGestion=" + db.ValueForSql(UMGestion))) +
                  (Utils.IsBlankField(UMGestion2) ? "" : (UMGestion2 == Constants.VALOR_RESET_CAMPO ? ",UMGestion2=''" : ",UMGestion2=" + db.ValueForSql(UMGestion2))) +
                  (Utils.IsBlankField(UMGestion3) ? "" : (UMGestion3 == Constants.VALOR_RESET_CAMPO ? ",UMGestion3=''" : ",UMGestion3=" + db.ValueForSql(UMGestion3))) +
                  (Utils.IsBlankField(productosFabricante.IndCalculaCantidades) ? "" : (productosFabricante.IndCalculaCantidades == Constants.VALOR_RESET_CAMPO ? ",IndCalculaCantidades=''" : ",IndCalculaCantidades=" + db.ValueForSql(productosFabricante.IndCalculaCantidades))) +
                  (Utils.IsBlankField(productosFabricante.IndFijaUMGestion) ? "" : (productosFabricante.IndFijaUMGestion == Constants.VALOR_RESET_CAMPO ? ",IndFijaUMGestion=''" : ",IndFijaUMGestion=" + db.ValueForSql(productosFabricante.IndFijaUMGestion))) +
                  (Utils.IsBlankField(productosFabricante.EsKit) ? "" : (productosFabricante.EsKit == Constants.VALOR_RESET_CAMPO ? ",IndEsKit=''" : ",IndEsKit=" + db.ValueForSql((productosFabricante.EsKit == "S" || productosFabricante.EsKit == "F") ? Constants.KIT_FABRICANTE : ""))) +
                  (Utils.IsBlankField(productosFabricante.PuntoVerde) || db.ValueForSqlAsNumeric(productosFabricante.PuntoVerde) == "NULL" ? "" : (productosFabricante.PuntoVerde == Constants.VALOR_RESET_CAMPO ? ",PuntoVerde=NULL" : ",PuntoVerde=" + db.ValueForSqlAsNumeric(productosFabricante.PuntoVerde))) +
                  (Utils.IsBlankField(UMPuntoVerde) ? "" : (UMPuntoVerde == Constants.VALOR_RESET_CAMPO ? ",UMPuntoVerde=''" : ",UMPuntoVerde=" + db.ValueForSql(UMPuntoVerde))) +
                  " WHERE IdcProducto = " + codigoProducto;
                db.ExecuteSql(sql, agent, GetSipTypeName());
            }
        }
        else if (!accion.Equals(Constants.ACCION_MODIFICACION_CLASIFICADORES))
        {
            umcheck = ObtenerUnidadMedidaNoPrimaria(db, productosFabricante.UM);
            string UnidadMedida = umcheck.value;

            //Se dará de alta en la tabla maestra de Productos
            sql = "insert into Productos (" +
              "Descripcion" +
              ",Abreviatura" +
              ",DescrLarga" +
              (Utils.IsBlankField(productosFabricante.Status) ? "" : ",Status") +
              ",UMc" +
              ",EAN13" +
              ",PesoNeto" +
              ",PesoNetoEscurrido" +
              ",PesoBruto" +
              ",UMcPeso" +
              ",Longitud" +
              ",Ancho" +
              ",Alto" +
              ",Volumen" +
              ",UMLongitud" +
              ",UMAncho" +
              ",UMAlto" +
              ",UMVolumen" +
              ",InstrucManipulacion" +
              ",IdcFabricante" +
              ",Empresa" +
              ",Propietario" +              
              ",IndEsTotalizador" +
              ",Nivel" +
              ",CEP" +
              ",PartidaArancelaria" +
              ",Tarifa" +
              ",UMEstadistica" +
              ",UMEstadistica2" +
              ",UMEstadistica3" +
              ",UMGestion" +
              ",UMGestion2" +
              ",UMGestion3" +
              ",IndCalculaCantidades" +
              ",IndFijaUMGestion" +
              ",IndEsKit" +
              ",PuntoVerde" +
              ",UMPuntoVerde" +
              ") values (" +
              db.ValueForSql(DescProducto) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Abreviatura, 20)) +
              "," + db.ValueForSql(productosFabricante.DescrLarga) +
              (Utils.IsBlankField(productosFabricante.Status) ? "" : "," + db.ValueForSql(productosFabricante.Status)) +
              //"," + db.ValueForSql(UnidadMedida) +
              (UnidadMedida == Constants.VALOR_RESET_CAMPO ? ",''" : "," + db.ValueForSql(UnidadMedida)) +
              "," + db.ValueForSql(productosFabricante.EAN13) +
              "," + db.ValueForSqlAsNumeric(productosFabricante.PesoNeto) +
              "," + db.ValueForSqlAsNumeric(productosFabricante.PesoNetoEscurrido) +
              "," + db.ValueForSqlAsNumeric(productosFabricante.PesoBruto) +
              //"," + db.ValueForSql(UMcPeso) +
              (UMcPeso == Constants.VALOR_RESET_CAMPO ? ",''" : "," + db.ValueForSql(UMcPeso)) +
              "," + db.ValueForSqlAsNumeric(productosFabricante.Longitud) +
              "," + db.ValueForSqlAsNumeric(productosFabricante.Ancho) +
              "," + db.ValueForSqlAsNumeric(productosFabricante.Alto) +
              "," + db.ValueForSqlAsNumeric(productosFabricante.Volumen) +
              //"," + db.ValueForSql(UMcLongitud) +
              //"," + db.ValueForSql(UMcAncho) +
              //"," + db.ValueForSql(UMcAlto) +
              //"," + db.ValueForSql(UMcVolumen) +
              (UMcLongitud == Constants.VALOR_RESET_CAMPO ? ",''" : "," + db.ValueForSql(UMcLongitud)) +
              (UMcAncho == Constants.VALOR_RESET_CAMPO ? ",''" : "," + db.ValueForSql(UMcAncho)) +
              (UMcAlto == Constants.VALOR_RESET_CAMPO ? ",''" : "," + db.ValueForSql(UMcAlto)) +
              (UMcVolumen == Constants.VALOR_RESET_CAMPO ? ",''" : "," + db.ValueForSql(UMcVolumen)) +
              "," + db.ValueForSql(productosFabricante.InstrucManipulacion) +
              "," + db.ValueForSql(codigoFabricante) +
              "," + db.ValueForSql(productosFabricante.EmpresaFabricante) +
              "," + db.ValueForSql(productosFabricante.PropietarioProducto) +              
              "," + db.ValueForSql(productosFabricante.EsTotalizador) +
              "," + db.ValueForSql(productosFabricante.NivelProducto) +
              "," + db.ValueForSql(productosFabricante.CEP) +
              "," + db.ValueForSql(productosFabricante.PartidaArancelaria) +
              "," + db.ValueForSqlAsNumeric(productosFabricante.Tarifa) +
              //"," + db.ValueForSql((Utils.IsBlankField(UMEstadistica1) ? fabricante.UMEstadistica : UMEstadistica1)) +
              //"," + db.ValueForSql((Utils.IsBlankField(UMEstadistica2) ? fabricante.UMEstadistica2 : UMEstadistica2)) +
              //"," + db.ValueForSql((Utils.IsBlankField(UMEstadistica3) ? fabricante.UMEstadistica3 : UMEstadistica3)) +
              (Utils.IsBlankField(UMEstadistica1) ? "," + db.ValueForSql(fabricante.UMEstadistica) : (UMEstadistica1 == Constants.VALOR_RESET_CAMPO ? ",''" : "," + db.ValueForSql(UMEstadistica1))) +
              (Utils.IsBlankField(UMEstadistica2) ? "," + db.ValueForSql(fabricante.UMEstadistica2) : (UMEstadistica2 == Constants.VALOR_RESET_CAMPO ? ",''" : "," + db.ValueForSql(UMEstadistica2))) +
              (Utils.IsBlankField(UMEstadistica3) ? "," + db.ValueForSql(fabricante.UMEstadistica3) : (UMEstadistica3 == Constants.VALOR_RESET_CAMPO ? ",''" : "," + db.ValueForSql(UMEstadistica3))) +
              //"," + db.ValueForSql(UMGestion) +
              //"," + db.ValueForSql(UMGestion2) +
              //"," + db.ValueForSql(UMGestion3) +
              (Utils.IsBlankField(UMGestion) ? "," + db.ValueForSql(fabricante.UMGestion) : (UMGestion == Constants.VALOR_RESET_CAMPO ? ",''" : "," + db.ValueForSql(UMGestion))) +
              (Utils.IsBlankField(UMGestion2) ? "," + db.ValueForSql(fabricante.UMGestion) : (UMGestion2 == Constants.VALOR_RESET_CAMPO ? ",''" : "," + db.ValueForSql(UMGestion2))) +
              (Utils.IsBlankField(UMGestion3) ? "," + db.ValueForSql(fabricante.UMGestion) : (UMGestion3 == Constants.VALOR_RESET_CAMPO ? ",''" : "," + db.ValueForSql(UMGestion3))) +
              "," + db.ValueForSql(productosFabricante.IndCalculaCantidades) +
              "," + db.ValueForSql(productosFabricante.IndFijaUMGestion) +
              "," + db.ValueForSql((productosFabricante.EsKit == "S" || productosFabricante.EsKit == "F") ? Constants.KIT_FABRICANTE : "") +
              "," + db.ValueForSqlAsNumeric(productosFabricante.PuntoVerde) +
              //"," + db.ValueForSql(UMPuntoVerde) +
              (UMPuntoVerde == Constants.VALOR_RESET_CAMPO ? ",''" : "," + db.ValueForSql(UMPuntoVerde)) +
              ")";
            db.ExecuteSql(sql, agent, GetSipTypeName());

            //Obtener código de producto...
            codigoProducto = ObtenerCodigoProducto(db);                                  

            //Y en ProductosAgentes
            sql = "insert into ProductosAgentes (" +
              "IdcAgente" +
              ",IdcProducto" +
              ",Codigo" +
              ",Descripcion" +
              ",DescripcionAdd" +
             (Utils.IsBlankField(productosFabricante.Status) ? "" : ",Status") +
              ",UMc" +
              ",Clasificacion1" +
              ",Clasificacion2" +
              ",Clasificacion3" +
              ",Clasificacion4" +
              ",Clasificacion5" +
              ",Clasificacion6" +
              ",Clasificacion7" +
              ",Clasificacion8" +
              ",Clasificacion9" +
              ",Clasificacion10" +
              ",Clasificacion11" +
              ",Clasificacion12" +
              ",Clasificacion13" +
              ",Clasificacion14" +
              ",Clasificacion15" +
              ",Clasificacion16" +
              ",Clasificacion17" +
              ",Clasificacion18" +
              ",Clasificacion19" +
              ",Clasificacion20" +
              ",Clasificacion21" +
              ",Clasificacion22" +
              ",Clasificacion23" +
              ",Clasificacion24" +
              ",Clasificacion25" +
              ",Clasificacion26" +
              ",Clasificacion27" +
              ",Clasificacion28" +
              ",Clasificacion29" +
              ",Clasificacion30" +
              ",Jerarquia" +
              ",IdcFabricante" +
              ",FechaBloqueo" +
              ",FechaBaja" +
              ",CodigoAlt" +
              ",CodigoAgrup" +
              ",UsuarioInsercion" +
              ",UsuarioModificacion" +
              ") values (" +
              codigoFabricante +
              "," + codigoProducto +
              "," + db.ValueForSql(productosFabricante.CodigoProducto)+
              "," + db.ValueForSql(DescProducto)+
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.DescripcionAdd, 100)) +
              (Utils.IsBlankField(productosFabricante.Status) ? "" : "," + db.ValueForSql(productosFabricante.Status)) +
              //"," + db.ValueForSql(UnidadMedida) +
              (UnidadMedida == Constants.VALOR_RESET_CAMPO ? ",''" : "," + db.ValueForSql(UnidadMedida)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion1,20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion2,20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion3,20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion4,20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion5,20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion6,20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion7,20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion8,20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion9,20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion10,20)) +
              //"," + db.ValueForSql(Utils.StringTruncate(UMGestion3, 15)) +
              //"," + db.ValueForSql(Utils.StringTruncate(UMGestion, 15)) +
              //"," + db.ValueForSql(Utils.StringTruncate(UMGestion2, 15)) +
              (Utils.IsBlankField(UMGestion3) ? "," + db.ValueForSql(fabricante.UMGestion) : (UMGestion3 == Constants.VALOR_RESET_CAMPO ? ",''" : "," + db.ValueForSql(Utils.StringTruncate(UMGestion3, 20)))) +
              (Utils.IsBlankField(UMGestion) ? "," + db.ValueForSql(fabricante.UMGestion) : (UMGestion == Constants.VALOR_RESET_CAMPO ? ",''" : "," + db.ValueForSql(Utils.StringTruncate(UMGestion, 20)))) +
              (Utils.IsBlankField(UMGestion2) ? "," + db.ValueForSql(fabricante.UMGestion) : (UMGestion2 == Constants.VALOR_RESET_CAMPO ? ",''" : "," + db.ValueForSql(Utils.StringTruncate(UMGestion2, 20)))) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion14, 20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion15, 20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion16, 20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion17, 20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion18, 20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion19, 20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion20, 20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion21, 20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion22, 20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion23, 20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion24, 20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion25, 20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion26, 20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion27, 20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion28, 20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion29, 20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Clasificacion30, 20)) +
              "," + db.ValueForSql(Utils.StringTruncate(productosFabricante.Jerarquia, 18)) + " " +
              "," + codigoFabricante +
              "," + db.DateForSql(productosFabricante.FechaBloqueo) + " " +
              "," + db.DateForSql(fechaBaja == "limpiar" ? "" : fechaBaja) + " " +
              "," + db.ValueForSql(productosFabricante.CodigoProductoAlt) + " " +
              "," + db.ValueForSql(productosFabricante.CodigoProductoAgrup) + " " +
              "," + db.ValueForSql(sUsuario) + " " +
              "," + db.ValueForSql(sUsuario) + " " +
              ")";
            db.ExecuteSql(sql, agent, GetSipTypeName());
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
            codigoFabricante = agent;
            string idcProductoComp = "";
            RecordProductosKitFabricante linea = (RecordProductosKitFabricante)rec;
            Log2 log = Globals.GetInstance().GetLog2();
            Database db = Globals.GetInstance().GetDatabase();
            Producto prod = new Producto();

            if (!codigoProductoKitCentinela.Equals(linea.CodigoProductoKit))
            {                
                //Comprobamos que exista el producto kit            
                if (!prod.ComprobarCodigoProducto(db, codigoFabricante, linea.CodigoProductoKit))
                {
                    string myAlertMsg = "Error en producto kit fabricante. No existe el código de producto kit {0}.";
                    log.Trace(agent, GetSipTypeName(), "PFAB0014", myAlertMsg, linea.CodigoProductoKit);
                    return;
                }                

                //Comprobamos si ha habido cambios composición kit
                DbDataReader cursor = null;
                if (hProductosKits.Count > 0)
                {
                    hProductosKitsNew.Clear();                    
                    try
                    {
                        sql = "Select CodigoProductoComponente From ProductosAgentesKits where IdcAgente = " + codigoFabricante + " and CodigoProductoKit = '" + codigoProductoKitCentinela + "' order by CodigoProductoComponente";
                        cursor = db.GetDataReader(sql);
                        string comp = "";
                        while (cursor.Read())
                        {
                            comp = codigoFabricante + "|" + codigoProductoKitCentinela + "|" + db.GetFieldValue(cursor, 0);
                            if (!hProductosKitsNew.ContainsKey(comp))
                                hProductosKitsNew.Add(comp, comp);
                        }
                    }
                    finally
                    {
                        if (cursor != null)
                            cursor.Close();
                    }
                    if (hProductosKitsNew.Count > 0)
                    {
                        IDictionaryEnumerator Enumerator = hProductosKitsNew.GetEnumerator();
                        int cont = 0;
                        while (Enumerator.MoveNext())
                        {
                            if (!hProductosKits.ContainsValue(Enumerator.Value))
                                cont++;
                        }
                        if (cont > 0)
                        {
                            string myAlertMsg = "Aviso en producto kit fabricante. Se han detectado cambios en los componentes para el producto kit {0}.";
                            log.Trace(agent, GetSipTypeName(), "PFAB0020", myAlertMsg, codigoProductoKitCentinela);
                        }
                    }
                }

                //Guardamos componentes kit actuales para poder comparar a posteriori y en caso de cambios generar alerta..
                hProductosKits.Clear();
                cursor = null;
                try
                {
                    sql = "Select CodigoProductoComponente From ProductosAgentesKits where IdcAgente = " + codigoFabricante + " and CodigoProductoKit = '" + linea.CodigoProductoKit + "' order by CodigoProductoComponente";
                    cursor = db.GetDataReader(sql);
                    string comp = "";
                    while (cursor.Read())
                    {
                        comp = codigoFabricante + "|" + linea.CodigoProductoKit + "|" + db.GetFieldValue(cursor, 0);
                        if (!hProductosKits.ContainsKey(comp))
                            hProductosKits.Add(comp, comp);
                    }
                }
                finally
                {
                    if (cursor != null)
                        cursor.Close();
                }
                idcProductoKit = prod.CodigoProducto;
                codigoProductoKitCentinela = linea.CodigoProductoKit;

                //Eliminar los componentes para productos kit
                sql = "Delete From ProductosAgentesKits where IdcAgente = " + codigoFabricante + " and CodigoProductoKit = '" + linea.CodigoProductoKit + "' ";
                db.ExecuteSql(sql, agent, GetSipTypeName());
            }

            //Comprobamos que exista el componente del kit
            //Si existe nos guardamos el código de producto "connecta" idcProducto            
            if (!prod.ComprobarCodigoProducto(db, codigoFabricante, linea.CodigoProductoComp))
            {
                string myAlertMsg = "Error en producto kit fabricante. No existe el código de producto componente {0} para el producto kit {1}.";
                log.Trace(agent, GetSipTypeName(), "PFAB0015", myAlertMsg, linea.CodigoProductoComp, linea.CodigoProductoKit);
                return;
            }
            idcProductoComp = prod.CodigoProducto;            

            //Obtener la Unidad de Medida de Connecta para el componente
            UMCheck umcheck = ObtenerUnidadMedidaNoPrimaria(db, linea.UMComp);
            string UMc = umcheck.value;

            //Generamos log si todos los componentes de un kit tiene proporcion a 0
            double d;
            if (double.TryParse(linea.ProporcionImporteComp, out d))
            {
                if (!hProductosKitsLog.ContainsKey(linea.CodigoProductoKit))
                    hProductosKitsLog.Add(linea.CodigoProductoKit, d);
                else 
                { 
                    double dIn;
                    if (double.TryParse(hProductosKitsLog[linea.CodigoProductoKit].ToString(), out dIn))
                    {
                        d += dIn;
                        hProductosKitsLog[linea.CodigoProductoKit] = d;
                    }
                }
            }

            //Generamos log si un componente tiene proprció cantidad a 0
            if (double.TryParse(linea.CantidadComp, out d) && d == 0)
            {
                string myAlertMsg = "Error en producto kit fabricante. Componente {0} con cantidad a 0 para el producto kit {1}.";
                log.Trace(agent, GetSipTypeName(), "PFAB0022", myAlertMsg, linea.CodigoProductoComp, linea.CodigoProductoKit);
            }

            string where = "WHERE IdcAgente =" + codigoFabricante + " " +
                            " AND CodigoProductoKit = '" + linea.CodigoProductoKit + "'" +
                            " AND IdcProductoComponente = " + idcProductoComp;
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
                              codigoFabricante +
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

    /// <summary>
    /// Obtener identificador del último producto dado de alta
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <returns>identificador del último producto dado de alta</returns>
    private string ObtenerCodigoProducto(Database db)
    {
        int idcProducto = 0;
        DbDataReader cursor = null;
        try
        {
            string sql = "";
            if (db.GetDbType() == Database.DB_SQLSERVER)
                sql = "SELECT SCOPE_IDENTITY() AS [SCOPE_IDENTITY]";   //Obtener contador asignado ya que es un autonumérico
            else
            sql = "select max(IdcProducto) from Productos";
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
                idcProducto = Int32.Parse(db.GetFieldValue(cursor, 0));
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
        return idcProducto+"";
    }

    /// <summary>
    /// Borrar cabeceras y líneas
    /// </summary>
    /// <param name="codigoProducto">producto</param>
    private void BorrarProductoErroneo(string codigoProducto)
    {
      //Globals.GetInstance().GetLog().TraceToFile(Log.TYPE_INFO, Log.LEVEL_MEDIUM, "Borrado de producto erróneo " + codigoProducto);
      string myAlertMsg = "Borrado de producto erróneo {0}.";
      Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "PFAB0003", myAlertMsg, codigoProducto);

      if (!Utils.IsBlankField(codigoFabricante) && !Utils.IsBlankField(codigoProducto))
      {
        string sql = "";
        Database db = Globals.GetInstance().GetDatabase();

        //Borrar líneas actuales (primero deben ser las líneas por la integridad referencial...)
        sql = "DELETE from ProductosAgentes where IdcAgente = " + codigoFabricante + " and IdcProducto = " + codigoProducto;
        db.ExecuteSql(sql, agent, GetSipTypeName());

        //Cabecera
        sql = "delete from Productos where IdcProducto=" + codigoProducto;
        db.ExecuteSql(sql, agent, GetSipTypeName());
      }
    }    
 
    /// <summary>
    /// Validar el formato de un registro
    /// </summary>
    /// <param name="rec">registro</param>
    public bool ValidateMaster(CommonRecord rec)
    {
        RecordProductosFabricante pf = (RecordProductosFabricante)rec;
        Database db = Globals.GetInstance().GetDatabase();
        Log2 log = Globals.GetInstance().GetLog2();

        if (Utils.IsBlankField(pf.CodigoProducto))
        {
            //log.DetailedError(agent, GetSipTypeName(), "Validación formato. Error en producto fabricante. Código de producto vacío.");
            string myAlertMsg = "Validación formato. Error en producto fabricante. Código de producto vacío.";
            log.Trace(agent, GetSipTypeName(), "PFAB0006", myAlertMsg); 
            return false;
        }

        if (!Utils.IsBlankField(pf.FechaBloqueo))
        {
            if (db.GetDate(pf.FechaBloqueo) == null)
            {
                //log.DetailedError(agent, GetSipTypeName(), "Validación formato. Error en producto fabricante. Fecha bloqueo " + pf.FechaBloqueo + " no es una fecha válida para el producto " + pf.CodigoProducto + ".");
                string myAlertMsg = "Validación formato. Error en producto fabricante. Fecha bloqueo {0} no es una fecha válida para el producto {1}.";
                log.Trace(agent, GetSipTypeName(), "PFAB0007", myAlertMsg, pf.FechaBloqueo, pf.CodigoProducto); 
                return false;
            }
        }

        if (!Utils.IsBlankField(pf.FechaBaja))
        {
            if (db.GetDate(pf.FechaBaja) == null)
            {
                //log.DetailedError(agent, GetSipTypeName(), "Validación formato. Error en producto fabricante. Fecha baja " + pf.FechaBaja + " no es una fecha válida para el producto " + pf.CodigoProducto + ".");
                string myAlertMsg = "Validación formato. Error en producto fabricante. Fecha baja {0} no es una fecha válida para el producto {1}.";
                log.Trace(agent, GetSipTypeName(), "PFAB0008", myAlertMsg, pf.FechaBaja, pf.CodigoProducto); 
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
        RecordProductosKitFabricante pfkit = (RecordProductosKitFabricante)rec;
        Log2 log = Globals.GetInstance().GetLog2();
        double cantAux;

        if (Utils.IsBlankField(pfkit.CodigoProductoKit))
        {
            string myAlertMsg = "Validación formato. Error en producto kit fabricante. Código de producto kit vacío.";            
            log.Trace(agent, GetSipTypeName(), "PFAB0016", myAlertMsg);
            return false;
        }

        if (Utils.IsBlankField(pfkit.CodigoProductoComp))
        {                        
            string myAlertMsg = "Validación formato. Error en producto kit fabricante. Código de producto componente vacío para el producto kit {0}.";
            log.Trace(agent, GetSipTypeName(), "PFAB0017", myAlertMsg, pfkit.CodigoProductoKit);
            return false;
        }

        //Comprobamos la cantidad recibida        
        if (Utils.IsBlankField(pfkit.CantidadComp))
        {
            string myAlertMsg = "Validación formato. Error en producto kit fabricante. Cantidad vacía para el componente {0}.";
            log.Trace(agent, GetSipTypeName(), "PFAB0018", myAlertMsg, pfkit.CodigoProductoComp);
            return false;
        }
        if (!Utils.IsBlankField(pfkit.CantidadComp))
        {
            if (!Double.TryParse(pfkit.CantidadComp, out cantAux))
            {
                string myAlertMsg = "Validación formato. Error en producto kit fabricante. Cantidad {0} no es numérico para el componente {1}.";
                log.Trace(agent, GetSipTypeName(), "PFAB0019", myAlertMsg, pfkit.CantidadComp, pfkit.CodigoProductoComp);
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
