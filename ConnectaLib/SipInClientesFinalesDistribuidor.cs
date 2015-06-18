using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.IO;
using System.Data.Common;
using System.Data.OleDb;
using System.Data;
using System.Collections;

namespace ConnectaLib
{
  /// <summary>
  /// Clase que gestiona las entradas de clientes finales del distribuidor
  /// </summary>
  public class SipInClientesFinalesDistribuidor : SipIn, ISipInInterface
  {
    private int numFicheros = 0;
    
    private Distribuidor dist = new Distribuidor();
    private Fabricante fab = new Fabricante();
    private CodigoPostal codPostal = new CodigoPostal();
    private Cliente cli = new Cliente();

    private ArrayList clientesNuevosRecibidos = new ArrayList();

    private string idcEntidadClasificada = "";
    private string idcClasificacion1 = "";
    private string idcClasificacion2 = "";
    private string idcClasificacion3 = "";
    private string idcClasificacion4 = "";
    private string idcClasificacion5 = "";
    private string idcClasificacion6 = "";
    private string idcClasificacion7 = "";
    private string idcClasificacion8 = "";
    private string idcClasificacion9 = "";
    private string idcClasificacion10 = "";
    private string idcClasificacion11 = "";
    private string idcClasificacion12 = "";
    private string idcClasificacion13 = "";
    private string idcClasificacion14 = "";
    private string idcClasificacion15 = "";
    private string idcClasificacion16 = "";
    private string idcClasificacion17 = "";
    private string idcClasificacion18 = "";
    private string idcClasificacion19 = "";
    private string idcClasificacion20 = "";
    private string idcClasificacion21 = "";
    private string idcClasificacion22 = "";
    private string idcClasificacion23 = "";
    private string idcClasificacion24 = "";
    private string idcClasificacion25 = "";
    private string idcClasificacion26 = "";
    private string idcClasificacion27 = "";
    private string idcClasificacion28 = "";
    private string idcClasificacion29 = "";
    private string idcClasificacion30 = "";
    private string idcFormaPago = "";
    private string idcMotivoBaja = "";
    private string fechaAlta = "";
    private string fechaBaja = "";

    private string areaNielsen = "";
    private string canalNielsen = "";
    private string mercadoNielsen1 = "";
    private string mercadoNielsen2 = "";
    private string mercadoNielsen3 = "";
    private string mercadoNielsen4 = "";
    private string mercadoNielsen5 = "";

    private Hashtable htAreas = new Hashtable();
    private Hashtable htMercado1 = new Hashtable();
    private Hashtable htMercado2 = new Hashtable();
    private Hashtable htMercado3 = new Hashtable();
    private Hashtable htMercado4 = new Hashtable();
    private Hashtable htMercado5 = new Hashtable();

    private Hashtable htCFAgentes = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
    private Hashtable htCFAgentesNum = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
    private Hashtable htClientesFinales = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
    private Hashtable htClasificaciones = new Hashtable(StringComparer.InvariantCultureIgnoreCase);

    private struct sCFAgente
    {
        public Int32 idcClienteFinal;
        public string codigoCF;
        public string canal;        
    }

    private struct sClientesFinales
    {
        public string direccion;
        public string poblacion;
        public string codigoPostal;
        public string N_IndFia;
        public string provincia;            
    }

    // Id de SIP
    public const string ID_SIP = "SipInClientesFinalesDistribuidor";

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="agent">agente</param>
    public SipInClientesFinalesDistribuidor(string agent) : base(agent, ID_SIP)
    {
    }

    /// <summary>
    /// Pre-proceso
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    public void PreProcess(string filename)
    {
      InvokeExternalProgram(this, agent);

      //Cargar una serie de parametros del fabricante
      dist.ObtenerParametros(Globals.GetInstance().GetDatabase(), agent);

      //Cargar una variable que nos indicará si han llegado dos ficheros padre e hijos, o solo uno de ellos.
      numFicheros = 1;
      if (filename.IndexOf(";") != -1)
      {
          numFicheros = 2;
      }

      //Cargamos tablas en memoria con las áreas y los mercados 
      Nomenclator n2 = new Nomenclator(GetId());
      if (numFicheros == 2 || n2.IsMasterFile(GetId(), filename))
      {
          CargarAreasMercadosNielsen(Globals.GetInstance().GetDatabase());
          CargarClientesActuales(Globals.GetInstance().GetDatabase());
          CargarClasificacionesAgente(Globals.GetInstance().GetDatabase());
      }
    }

    /// <summary>
    /// Post-proceso
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    public void PostProcess(string filename) 
    {
        //Tratar el envío del resumen de clientes nuevos vía mail al distribuidor.
        //////////////////////////////////////////////////////
        Nomenclator n2 = new Nomenclator(GetId());
        //Si filename contiene dos nombres de ficheros entonces se debe tratar el resumen
        //Y si no, solo se trata el resumen si filename es un proceso master.
        if (filename.IndexOf(";") != -1 || n2.IsMasterFile(GetId(), filename))
        {
            if (clientesNuevosRecibidos.Count > 0)
            {
                GenerarResumenClientes();
                //Limpiamos el array de clientes recibidos por si acaso.
                clientesNuevosRecibidos.Clear();
            }
        }

        //Borramos datos memoria
        htAreas.Clear();
        htMercado1.Clear();
        htMercado2.Clear();
        htMercado3.Clear();
        htMercado4.Clear();
        htMercado5.Clear();
        htCFAgentes.Clear();
        htCFAgentesNum.Clear();
        htClientesFinales.Clear();
        htClasificaciones.Clear();
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
      return "schClientesFinalesDistribuidor.xsd";
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
      return new RecordClienteFinalDistribuidor();
    }

    /// <summary>
    /// Obtener registro esclavo
    /// </summary>
    /// <returns>registro esclavo</returns>
    public CommonRecord GetSlaveRecord()
    {
      return new RecordCodigoClienteFinalDistribuidor();
    }

    /// <summary>
    /// Obtener sección de XML para el registro maestro
    /// </summary>
    /// <returns>sección de XML</returns>
    public string GetMasterXMLSection()
    {
      return "ClienteFinalDistribuidor";
    }

    /// <summary>
    /// Obtener sección de XML para el registro esclavo
    /// </summary>
    /// <returns>sección de XML</returns>
    public string GetSlaveXMLSection()
    {
      return "CodigoClienteFinalDistribuidor";
    }

    /// <summary>
    /// Obtener nombre de Sip (básicamente a efectos de log)
    /// </summary>
    /// <returns>nombre de sip</returns>
    public string GetSipTypeName()
    {
      return "Distribuidor.ClientesFinales.In";
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
    /// Cargar clasificaciones del agente
    /// </summary>
    /// <param name="db">db</param>
    private void CargarClasificacionesAgente(Database db)
    {
        htClasificaciones.Clear();
        DbDataReader cursor = null;
        string sql = "select ltrim(rtrim(EntidadClasificada)) + ';' + ltrim(rtrim(Codigo)) as clave, Libre1 from ClasificacionAgente " +
                     " where IdcAgente = " + agent + " and entidadClasificada like 'AGENTE%'";
        cursor = db.GetDataReader(sql);
        string clave;
        while (cursor.Read())
        {
            clave = db.GetFieldValue(cursor, 0);
            if (!htClasificaciones.ContainsKey(clave))
                htClasificaciones.Add(clave, db.GetFieldValue(cursor, 1));
        }
        cursor.Close();
        cursor = null;
    }

    /// <summary>
    /// Cargar información necesaria de los clientes actuales del agente
    /// </summary>
    /// <param name="db">db</param>
    private void CargarClientesActuales(Database db)
    {
        htCFAgentes.Clear();
        htCFAgentesNum.Clear();
        htClientesFinales.Clear();
        DbDataReader cursor = null;
        string sql = "select cf.IdcClienteFinal, " +
                     "       cf.CodigoCF, " +
                     "       cf.CanalNielsen, " +
                     "       case when (patindex('%[^0-9,.]%',ltrim(rtrim(cf.CodigoCF)))=0 and left(ltrim(cf.CodigoCF),1) not in ('.',',') and right(rtrim(cf.CodigoCF),1) not in ('.',',')) " +
                     "            then cast(replace(cf.CodigoCF,',','.') as numeric(25,0)) else null end as CodigoCFNum, " +
                     "       c.IdcAgente, c.Direccion, c.Poblacion, c.CodigoPostal, c.N_IndFia, c.Provincia " +
                     "  from CFAgentes cf " +
                     "  left join clientesFinales c on cf.idcClienteFinal=c.idcAgente " +
                     " where cf.IdcAgente = " + agent;
        cursor = db.GetDataReader(sql);            
        while (cursor.Read()) 
        {
            sCFAgente cf = new sCFAgente();
            cf.idcClienteFinal = Int32.Parse(db.GetFieldValue(cursor, 0));
            cf.codigoCF = db.GetFieldValue(cursor, 1);
            cf.canal = db.GetFieldValue(cursor, 2);
            if (!htCFAgentes.ContainsKey(cf.codigoCF))
                htCFAgentes.Add(cf.codigoCF, cf);
            Double codigoCFNum = 0;
            Double.TryParse(db.GetFieldValue(cursor, 3), out codigoCFNum);
            if (!htCFAgentesNum.ContainsKey(codigoCFNum))
                htCFAgentesNum.Add(codigoCFNum, cf);
            sClientesFinales c = new sClientesFinales();
            int idcAgente = int.Parse(db.GetFieldValue(cursor, 4));
            c.direccion = db.GetFieldValue(cursor, 5);
            c.poblacion = db.GetFieldValue(cursor, 6);
            c.codigoPostal = db.GetFieldValue(cursor, 7);
            c.N_IndFia = db.GetFieldValue(cursor, 8);
            c.provincia = db.GetFieldValue(cursor, 9);
            if (!htClientesFinales.ContainsKey(idcAgente))
                htClientesFinales.Add(idcAgente, c);
        }
        cursor.Close();
        cursor = null;
    }

    /// <summary>
    /// Actualizar agentes
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="cfd">registro</param>
    /// <param name="codigoCliente">cliente</param>
    private void ActualizarAgentes(Database db, RecordClienteFinalDistribuidor cfd, string codigoCliente)
    {
        //Asignar la razón social si el nombre está vacío
        string nomCli = cfd.Nombre;
        if ((nomCli.Trim() == "." || nomCli.Trim() == "#")) nomCli = "";
        if (Utils.IsBlankField(nomCli)) nomCli = cfd.RazonSocial;
        //Recortar el nombre si supera la longitud de 50
        if (nomCli.Trim().Length > 50)
            nomCli = nomCli.Trim().Remove(50);
        else
            nomCli= nomCli.Trim();

        string sql = "update CFAgentes set ";
        sql += "NombreCF = " + (Utils.IsBlankField(nomCli) ? "NombreCF" : (nomCli.ToUpper().Contains(Constants.CLIENTES_DISTR_SIN_DESCRIPCION + " " + codigoCliente) ? "NombreCF " : db.ValueForSql(nomCli.ToUpper()) + " "));
        if (!Utils.IsBlankField(cfd.Status)) sql += ",Status=" + db.ValueForSql(cfd.Status) + "";
        if (!Utils.IsBlankField(cfd.Clasificacion1)) sql += ",Clasificacion1=" + db.ValueForSql(cfd.Clasificacion1);
        if (!Utils.IsBlankField(cfd.Clasificacion2)) sql += ",Clasificacion2=" + db.ValueForSql(cfd.Clasificacion2);
        if (!Utils.IsBlankField(cfd.Clasificacion3)) sql += ",Clasificacion3=" + db.ValueForSql(cfd.Clasificacion3);
        if (!Utils.IsBlankField(cfd.Clasificacion4)) sql += ",Clasificacion4=" + db.ValueForSql(cfd.Clasificacion4);
        if (!Utils.IsBlankField(cfd.Clasificacion5)) sql += ",Clasificacion5=" + db.ValueForSql(cfd.Clasificacion5);
        if (!Utils.IsBlankField(cfd.Clasificacion6)) sql += ",Clasificacion6=" + db.ValueForSql(cfd.Clasificacion6);
        if (!Utils.IsBlankField(cfd.Clasificacion7)) sql += ",Clasificacion7=" + db.ValueForSql(cfd.Clasificacion7);
        if (!Utils.IsBlankField(cfd.Clasificacion8)) sql += ",Clasificacion8=" + db.ValueForSql(cfd.Clasificacion8);
        if (!Utils.IsBlankField(cfd.Clasificacion9)) sql += ",Clasificacion9=" + db.ValueForSql(cfd.Clasificacion9);
        if (!Utils.IsBlankField(cfd.Clasificacion10)) sql += ",Clasificacion10=" + db.ValueForSql(cfd.Clasificacion10);
        if (!Utils.IsBlankField(cfd.Clasificacion11)) sql += ",Clasificacion11=" + db.ValueForSql(cfd.Clasificacion11);
        if (!Utils.IsBlankField(cfd.Clasificacion12)) sql += ",Clasificacion12=" + db.ValueForSql(cfd.Clasificacion12);
        if (!Utils.IsBlankField(cfd.Clasificacion13)) sql += ",Clasificacion13=" + db.ValueForSql(cfd.Clasificacion13);
        if (!Utils.IsBlankField(cfd.Clasificacion14)) sql += ",Clasificacion14=" + db.ValueForSql(cfd.Clasificacion14);
        if (!Utils.IsBlankField(cfd.Clasificacion15)) sql += ",Clasificacion15=" + db.ValueForSql(cfd.Clasificacion15);
        if (!Utils.IsBlankField(cfd.Clasificacion16)) sql += ",Clasificacion16=" + db.ValueForSql(cfd.Clasificacion16);
        if (!Utils.IsBlankField(cfd.Clasificacion17)) sql += ",Clasificacion17=" + db.ValueForSql(cfd.Clasificacion17);
        if (!Utils.IsBlankField(cfd.Clasificacion18)) sql += ",Clasificacion18=" + db.ValueForSql(cfd.Clasificacion18);
        if (!Utils.IsBlankField(cfd.Clasificacion19)) sql += ",Clasificacion19=" + db.ValueForSql(cfd.Clasificacion19);
        if (!Utils.IsBlankField(cfd.Clasificacion20)) sql += ",Clasificacion20=" + db.ValueForSql(cfd.Clasificacion20);
        if (!Utils.IsBlankField(cfd.Clasificacion21)) sql += ",Clasificacion21=" + db.ValueForSql(cfd.Clasificacion21);
        if (!Utils.IsBlankField(cfd.Clasificacion22)) sql += ",Clasificacion22=" + db.ValueForSql(cfd.Clasificacion22);
        if (!Utils.IsBlankField(cfd.Clasificacion23)) sql += ",Clasificacion23=" + db.ValueForSql(cfd.Clasificacion23);
        if (!Utils.IsBlankField(cfd.Clasificacion24)) sql += ",Clasificacion24=" + db.ValueForSql(cfd.Clasificacion24);
        if (!Utils.IsBlankField(cfd.Clasificacion25)) sql += ",Clasificacion25=" + db.ValueForSql(cfd.Clasificacion25);
        if (!Utils.IsBlankField(cfd.Clasificacion26)) sql += ",Clasificacion26=" + db.ValueForSql(cfd.Clasificacion26);
        if (!Utils.IsBlankField(cfd.Clasificacion27)) sql += ",Clasificacion27=" + db.ValueForSql(cfd.Clasificacion27);
        if (!Utils.IsBlankField(cfd.Clasificacion28)) sql += ",Clasificacion28=" + db.ValueForSql(cfd.Clasificacion28);
        if (!Utils.IsBlankField(cfd.Clasificacion29)) sql += ",Clasificacion29=" + db.ValueForSql(cfd.Clasificacion29);
        if (!Utils.IsBlankField(cfd.Clasificacion30)) sql += ",Clasificacion30=" + db.ValueForSql(cfd.Clasificacion30);
        if (!Utils.IsBlankField(idcClasificacion1)) sql += ",IdcClasificacion1=" + db.ValueForSql(idcClasificacion1);
        if (!Utils.IsBlankField(idcClasificacion2)) sql += ",IdcClasificacion2=" + db.ValueForSql(idcClasificacion2);
        if (!Utils.IsBlankField(idcClasificacion3)) sql += ",IdcClasificacion3=" + db.ValueForSql(idcClasificacion3);
        if (!Utils.IsBlankField(idcClasificacion4)) sql += ",IdcClasificacion4=" + db.ValueForSql(idcClasificacion4);
        if (!Utils.IsBlankField(idcClasificacion5)) sql += ",IdcClasificacion5=" + db.ValueForSql(idcClasificacion5);
        if (!Utils.IsBlankField(idcClasificacion6)) sql += ",IdcClasificacion6=" + db.ValueForSql(idcClasificacion6);
        if (!Utils.IsBlankField(idcClasificacion7)) sql += ",IdcClasificacion7=" + db.ValueForSql(idcClasificacion7);
        if (!Utils.IsBlankField(idcClasificacion8)) sql += ",IdcClasificacion8=" + db.ValueForSql(idcClasificacion8);
        if (!Utils.IsBlankField(idcClasificacion9)) sql += ",IdcClasificacion9=" + db.ValueForSql(idcClasificacion9);
        if (!Utils.IsBlankField(idcClasificacion10)) sql += ",IdcClasificacion10=" + db.ValueForSql(idcClasificacion10);
        if (!Utils.IsBlankField(idcClasificacion11)) sql += ",IdcClasificacion11=" + db.ValueForSql(idcClasificacion11);
        if (!Utils.IsBlankField(idcClasificacion12)) sql += ",IdcClasificacion12=" + db.ValueForSql(idcClasificacion12);
        if (!Utils.IsBlankField(idcClasificacion13)) sql += ",IdcClasificacion13=" + db.ValueForSql(idcClasificacion13);
        if (!Utils.IsBlankField(idcClasificacion14)) sql += ",IdcClasificacion14=" + db.ValueForSql(idcClasificacion14);
        if (!Utils.IsBlankField(idcClasificacion15)) sql += ",IdcClasificacion15=" + db.ValueForSql(idcClasificacion15);
        if (!Utils.IsBlankField(idcClasificacion16)) sql += ",IdcClasificacion16=" + db.ValueForSql(idcClasificacion16);
        if (!Utils.IsBlankField(idcClasificacion17)) sql += ",IdcClasificacion17=" + db.ValueForSql(idcClasificacion17);
        if (!Utils.IsBlankField(idcClasificacion18)) sql += ",IdcClasificacion18=" + db.ValueForSql(idcClasificacion18);
        if (!Utils.IsBlankField(idcClasificacion19)) sql += ",IdcClasificacion19=" + db.ValueForSql(idcClasificacion19);
        if (!Utils.IsBlankField(idcClasificacion20)) sql += ",IdcClasificacion20=" + db.ValueForSql(idcClasificacion20);
        if (!Utils.IsBlankField(idcClasificacion21)) sql += ",IdcClasificacion21=" + db.ValueForSql(idcClasificacion21);
        if (!Utils.IsBlankField(idcClasificacion22)) sql += ",IdcClasificacion22=" + db.ValueForSql(idcClasificacion22);
        if (!Utils.IsBlankField(idcClasificacion23)) sql += ",IdcClasificacion23=" + db.ValueForSql(idcClasificacion23);
        if (!Utils.IsBlankField(idcClasificacion24)) sql += ",IdcClasificacion24=" + db.ValueForSql(idcClasificacion24);
        if (!Utils.IsBlankField(idcClasificacion25)) sql += ",IdcClasificacion25=" + db.ValueForSql(idcClasificacion25);
        if (!Utils.IsBlankField(idcClasificacion26)) sql += ",IdcClasificacion26=" + db.ValueForSql(idcClasificacion26);
        if (!Utils.IsBlankField(idcClasificacion27)) sql += ",IdcClasificacion27=" + db.ValueForSql(idcClasificacion27);
        if (!Utils.IsBlankField(idcClasificacion28)) sql += ",IdcClasificacion28=" + db.ValueForSql(idcClasificacion28);
        if (!Utils.IsBlankField(idcClasificacion29)) sql += ",IdcClasificacion29=" + db.ValueForSql(idcClasificacion29);
        if (!Utils.IsBlankField(idcClasificacion30)) sql += ",IdcClasificacion30=" + db.ValueForSql(idcClasificacion30);
        if (!Utils.IsBlankField(cfd.FormaPago)) sql += ",FormaPago=" + db.ValueForSql(cfd.FormaPago);
        if (!Utils.IsBlankField(idcFormaPago)) sql += ",IdcFormaPago=" + db.ValueForSql(idcFormaPago);
        if (!Utils.IsBlankField(cfd.RecargoEquivalencia)) sql += ",RecargoEquivalencia=" + db.ValueForSql(cfd.RecargoEquivalencia);
        if (!Utils.IsBlankField(cfd.HorarioApertura)) sql += ",HorarioApertura=" + db.ValueForSql(Utils.StringTruncate(cfd.HorarioApertura,20));
        if (!Utils.IsBlankField(cfd.HorarioVisita)) sql += ",HorarioVisita=" + db.ValueForSql(Utils.StringTruncate(cfd.HorarioVisita,20));
        if (!Utils.IsBlankField(cfd.Vacaciones)) sql += ",Vacaciones=" + db.ValueForSql(cfd.Vacaciones);
        if (!Utils.IsBlankField(fechaAlta)) sql += ",FechaAltaAgente=" + db.DateForSql(fechaAlta);
        if (!Utils.IsBlankField(fechaBaja)) sql += ",FechaBajaAgente=" + db.DateForSql(fechaBaja);
        if (!Utils.IsBlankField(cfd.MotivoBaja)) sql += ",MotivoBaja=" + db.ValueForSql(cfd.MotivoBaja);
        if (!Utils.IsBlankField(idcMotivoBaja)) sql += ",IdcMotivoBaja=" + db.ValueForSql(idcMotivoBaja);
        sql += ",IndCreadoAutomatico=" + db.ValueForSql(string.Empty) +
            ((Utils.IsBlankField(areaNielsen)) ? "" : ",AreaNielsen=" + db.ValueForSql(areaNielsen)) +
            ((Utils.IsBlankField(canalNielsen)) ? "" : ",CanalNielsen=" + db.ValueForSql(canalNielsen)) +
            ((Utils.IsBlankField(mercadoNielsen1)) ? "" : ",MercadoNielsen1=" + db.ValueForSql(mercadoNielsen1)) +
            ((Utils.IsBlankField(mercadoNielsen2)) ? "" : ",MercadoNielsen2=" + db.ValueForSql(mercadoNielsen2)) +
            ((Utils.IsBlankField(mercadoNielsen3)) ? "" : ",MercadoNielsen3=" + db.ValueForSql(mercadoNielsen3)) +
            ((Utils.IsBlankField(mercadoNielsen4)) ? "" : ",MercadoNielsen4=" + db.ValueForSql(mercadoNielsen4)) +
            ((Utils.IsBlankField(mercadoNielsen5)) ? "" : ",MercadoNielsen5=" + db.ValueForSql(mercadoNielsen5));

        sql += ",FechaModificacion=" + db.SysDate() + " " +
                " where IdcAgente = " + agent + " and CodigoCF = '" + codigoCliente + "'";
        db.ExecuteSql(sql, agent, GetSipTypeName());
    }

    /// <summary>
    /// Actualizar cliente final
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="cfd">registro</param>
    /// <param name="clienteFinal">cliente</param>
    /// <param name="cif">cif</param>
    /// <param name="direccion">direccion</param>
    private void ActualizarClienteFinal(Database db, RecordClienteFinalDistribuidor cfd, int clienteFinal, string cif, string direccion, string codigoPostal, string codigoCliente)
    {
        string nomCli = cfd.Nombre;
        if (!Utils.IsBlankField(nomCli)) //si no nos llega el nombre de cliente significa que actualizaremos nada
        {            
            string direccionOld = "";
            string poblacionOld = "";
            string codigopostalOld = "";
            string provinciaOld = "";
            string indFiaOld = "";
            if (htClientesFinales.ContainsKey(clienteFinal))
            {
                sClientesFinales c = (sClientesFinales)htClientesFinales[clienteFinal];
                direccionOld = c.direccion;
                poblacionOld = c.poblacion;
                codigopostalOld = c.codigoPostal;
                indFiaOld = c.N_IndFia;
                provinciaOld = c.provincia;

                string sql = "UPDATE ClientesFinales SET ";
                sql += "Nombre = " + ((Utils.IsBlankField(nomCli) || nomCli.ToUpper().Contains(Constants.CLIENTES_DISTR_SIN_DESCRIPCION + " " + codigoCliente)) ? "Nombre " : ((nomCli.Trim() == "." || nomCli.Trim() == "#") ? "''" : db.ValueForSql(nomCli.ToUpper())) + " ");
                sql += ",RazonSocial=" + db.ValueForSql(cfd.RazonSocial.ToUpper()) +
                  ",CIF=" + db.ValueForSql(cif) +
                  ",TipoCalle=" + db.ValueForSql(Utils.StringTruncate(cfd.TipoCalle, 3)) +
                  ",Calle=" + db.ValueForSql(Utils.StringTruncate(cfd.Calle, 85)) +
                  ",Numero=" + db.ValueForSql(Utils.StringTruncate(cfd.Numero, 20)) +
                  ",Direccion=" + (string.IsNullOrEmpty(direccionOld) && !string.IsNullOrEmpty(direccion) ? db.ValueForSqlOK(direccion) : ((direccion.ToUpper().Replace("Ó", "O").Contains(Constants.CLIENTES_DISTR_SIN_DIRECCION + " " + codigoCliente) || string.IsNullOrEmpty(direccion)) ? "Direccion " : db.ValueForSqlOK(Utils.StringTruncate(direccion.ToUpper(), 100)))) +
                    //ATENCIÓN: ANTES cuando la dirección era una dirección "construida o camuflada" tampoco actualizabamos la población, ni el código postal ni la provincia              
                    //          AHORA aunque la dirección sea "camuflada" si la población, el código postal y la privincia está vacío a la base de datos, lo actualizamos.
                  ",Poblacion=" + (string.IsNullOrEmpty(poblacionOld) && !string.IsNullOrEmpty(cfd.Poblacion) ? db.ValueForSqlOK(cfd.Poblacion) : ((direccion.ToUpper().Replace("Ó", "O").Contains(Constants.CLIENTES_DISTR_SIN_DIRECCION + " " + codigoCliente) || string.IsNullOrEmpty(cfd.Poblacion)) ? "Poblacion " : db.ValueForSqlOK(cfd.Poblacion))) +
                  ",CodigoPostal=" + (string.IsNullOrEmpty(codigopostalOld) && !string.IsNullOrEmpty(codigoPostal) ? db.ValueForSql(codigoPostal) : ((direccion.ToUpper().Replace("Ó", "O").Contains(Constants.CLIENTES_DISTR_SIN_DIRECCION + " " + codigoCliente) || string.IsNullOrEmpty(codigoPostal)) ? "CodigoPostal " : db.ValueForSql(codigoPostal))) +
                  ",Provincia=" + (string.IsNullOrEmpty(provinciaOld) && !string.IsNullOrEmpty(Utils.ObtenerProvincia(codigoPostal, agentLocation)) ? db.ValueForSql(Utils.ObtenerProvincia(codigoPostal, agentLocation)) : (direccion.ToUpper().Replace("Ó", "O").Contains(Constants.CLIENTES_DISTR_SIN_DIRECCION + " " + codigoCliente) ? "Provincia " : db.ValueForSql(Utils.ObtenerProvincia(codigoPostal, agentLocation)))) +
                  ",CodigoPais=" + db.ValueForSql(Utils.ObtenerPais(cfd.CodigoPais, agent, GetSipTypeName()));
                if (!Utils.IsBlankField(cfd.PaginaWEB))
                    sql += ",PaginaWEB=" + db.ValueForSql(Utils.StringTruncate(cfd.PaginaWEB, 60));
                if (!Utils.IsBlankField(cfd.Telefono1))
                    sql += ",Telefono1=" + db.ValueForSql(Utils.StringTruncate(cfd.Telefono1, 20));
                if (!Utils.IsBlankField(cfd.Telefono2))
                    sql += ",Telefono2=" + db.ValueForSql(Utils.StringTruncate(cfd.Telefono2, 20));
                if (!Utils.IsBlankField(cfd.FAX))
                    sql += ",FAX=" + db.ValueForSql(Utils.StringTruncate(cfd.FAX, 20));
                if (!Utils.IsBlankField(cfd.Email))
                    sql += ",Email=" + db.ValueForSql(Utils.StringTruncate(cfd.Email, 50));
                if (!Utils.IsBlankField(cfd.PersonaContacto))
                    sql += ",PersonaContacto=" + db.ValueForSql(Utils.StringTruncate(cfd.PersonaContacto, 100));
                sql += ",CodigoMoneda=" + db.ValueForSql(Utils.ObtenerMoneda(db, agent, GetSipTypeName(), cfd.CodigoMoneda));
                if (!Utils.IsBlankField(cfd.PuntoOperacional))
                    sql += ",PuntoOperacional=" + db.ValueForSql(cfd.PuntoOperacional);
                if (!Utils.IsBlankField(cfd.CodigoINE))
                    sql += ",CodigoINE=" + db.ValueForSql(cfd.CodigoINE);
                if (!Utils.IsBlankField(cfd.Barrio))
                    sql += ",Barrio=" + db.ValueForSql(cfd.Barrio);

                if (!Utils.IsBlankField(indFiaOld)) //si el indicador de fiabilidad no está vacío significa que ya ha sido pasado por el proceso de normalización
                {
                    if (!direccion.ToUpper().Replace("Ó", "O").Contains(Constants.CLIENTES_DISTR_SIN_DIRECCION + " " + codigoCliente) && !string.IsNullOrEmpty(direccion) && direccion.ToUpper().Trim() != direccionOld.ToUpper().Trim()) 
                    {
                        //si la direccion no es la dirección "camuflada", la dirección que llega no es vacía y la dirección ha cambiado se marca como indicador de fiablidad desconocido para que se normalice de nuevo
                        sql += ",N_IndFia='X'";
                        string myAlertMsg = "Aviso en cliente distribuidor. Cambio en dirección ya normalizada en el cliente {0} (Dir. ant.: {1} -- Dir. act.: {2}).";
                        Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0001", myAlertMsg, cfd.CodigoCliente, direccionOld.ToUpper().Trim(), direccion.ToUpper().Trim());
                    }
                    else if (!direccion.ToUpper().Replace("Ó", "O").Contains(Constants.CLIENTES_DISTR_SIN_DIRECCION + " " + codigoCliente) && !string.IsNullOrEmpty(cfd.Poblacion) && cfd.Poblacion.ToUpper().Trim() != poblacionOld.ToUpper().Trim()) 
                    {
                        //si la direccion no es la dirección "camuflada" y la población ha cambiado se marca como indicador de fiablidad desconocido para que se normalice de nuevo
                        sql += ",N_IndFia='X'";
                        string myAlertMsg = "Aviso en cliente distribuidor. Cambio en población ya normalizada en el cliente {0} (Pob. ant.: {1} -- Pob. act.: {2}).";
                        Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0002", myAlertMsg, cfd.CodigoCliente, poblacionOld.ToUpper().Trim(), cfd.Poblacion.ToUpper().Trim());
                    }
                    else if (!direccion.ToUpper().Replace("Ó", "O").Contains(Constants.CLIENTES_DISTR_SIN_DIRECCION + " " + codigoCliente) && !string.IsNullOrEmpty(codigoPostal) && codigoPostal.ToUpper().Trim() != codigopostalOld.ToUpper().Trim()) 
                    {
                        //si la direccion no es la dirección "camuflada" y el código postal ha cambiado se marca como indicador de fiablidad desconocido para que se normalice de nuevo
                        sql += ",N_IndFia='X'";
                        string myAlertMsg = "Aviso en cliente distribuidor. Cambio en código postal ya normalizado en el cliente {0} (CP. ant.: {1} -- CP. act.: {2}).";
                        Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0003", myAlertMsg, cfd.CodigoCliente, codigopostalOld.ToUpper().Trim(), codigoPostal.ToUpper().Trim());
                    }
                }
                sql += " where IdcAgente = " + clienteFinal;
                db.ExecuteSql(sql, agent, GetSipTypeName());

                //Además borramos los registros de salidas para que pueda volver a enviarse en un sipOut.
                sql = "Delete SalidasClientesFinales where IdcAgente=" + agent + " And IdcClienteFinal=" + clienteFinal + " And CodigoCF='" + codigoCliente + "'";
                db.ExecuteSql(sql, agent, GetSipTypeName());
            }            
        }
    }

    /// <summary>
    /// Proceso del objeto maestro (que puede venir de cualquier fuente de información) y 
    /// lógica de negocio asociada.
    /// </summary>
    /// <param name="pd">objeto</param>
    public void ProcessMaster(CommonRecord rec)
    {
        RecordClienteFinalDistribuidor cfd = (RecordClienteFinalDistribuidor)rec;
        DbDataReader cursor = null;
        try
        {
            Database db = Globals.GetInstance().GetDatabase();

            DateTime dtWork = DateTime.Now;
            
            //Tratar CIF
            string cif = Utils.NIFNormalizado(cfd.CIF);
            bool cifInformado = false;
            if (!Utils.IsBlankField(cif))
                cifInformado = true;

            string codigoCliente = cfd.CodigoCliente;
            if (Utils.IsBlankField(codigoCliente))
            {
                codigoCliente = cif;
            }

            //Comprobar código de cliente
            if (Utils.IsBlankField(codigoCliente) && Utils.IsBlankField(cif))
            {
                string myAlertMsg = "Error en cliente distribuidor. Cliente con código y cif/nif vacíos.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0004", myAlertMsg);

                //Abandonar el tratamiento de este registro...
                return;
            }

            //Comprobar dirección
            string direccion = cfd.Direccion;
            if (Utils.IsBlankField(direccion))
            {
                direccion = cfd.TipoCalle + " " + cfd.Calle + " " + cfd.Numero;
            }
            //Validar direccion
            if (((direccion.StartsWith("X") || direccion.StartsWith("x")) && (direccion.EndsWith("X") || direccion.EndsWith("x"))))
            {
                string myAlertMsg = "Aviso en cliente distribuidor. Dirección ({0}) erronea para el cliente {1}.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0005", myAlertMsg, direccion, codigoCliente);
                direccion = "";
            }

            //Comprobar el código postal
            if (!codPostal.TratarCodigoPostal(cfd.CodigoPostal, agentLocation))
            {
                string myAlertMsg = "Aviso en cliente distribuidor. Código postal ({0}) erroneo para el cliente {1}.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0006", myAlertMsg, cfd.CodigoPostal, codigoCliente);
            }
            
            //Comprobar clasificaciones
            ComprobarClasificaciones(db, cfd, codigoCliente);
            
            //Obtener el área Nielsen calculada a partir del código postal               
            if (!string.IsNullOrEmpty(codPostal.CPostal))
            {
                areaNielsen = ObtenerAreaNielsen(db, codPostal.CPostal);
            }

            //Comprobar forma de pago
            idcFormaPago = "";
            if (!Utils.IsBlankField(cfd.FormaPago))
            {
                if (ComprobarEntidadClasificada(db, "FORPAG", cfd.FormaPago))
                {
                    idcFormaPago = idcEntidadClasificada;
                }
                else
                {
                    InsertarEntidadClasificada(db, "FORPAG", cfd.FormaPago, "Forma de pago " + cfd.FormaPago);
                    string myAlertMsg = "Aviso en cliente distribuidor. Forma de pago ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                    Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0007", myAlertMsg, cfd.FormaPago, codigoCliente);
                }
            }

            //Comprobar motivo de baja
            idcMotivoBaja = "";
            if (!Utils.IsBlankField(cfd.MotivoBaja))
            {
                if (ComprobarEntidadClasificada(db, "MTBJCL", cfd.MotivoBaja))
                {
                    idcMotivoBaja = idcEntidadClasificada;
                }
                else
                {
                    InsertarEntidadClasificada(db, "MTBJCL", cfd.MotivoBaja, "Motivo baja " + cfd.MotivoBaja);
                    string myAlertMsg = "Aviso en cliente distribuidor. Motivo de baja ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                    Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0008", myAlertMsg, cfd.MotivoBaja, codigoCliente);
                }
            }

            //Comprobar fecha alta
            fechaAlta = "";
            if (!Utils.IsBlankField(cfd.FechaAlta))
            {
                if (db.GetDate(cfd.FechaAlta) != null)
                {
                    fechaAlta = cfd.FechaAlta;
                }
                else
                {
                    string myAlertMsg = "Aviso en cliente distribuidor. Fecha alta ({0}) incorrecta para el cliente {1}.";
                    Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0009", myAlertMsg, cfd.FechaAlta, codigoCliente);
                }
            }

            //Comprobar fecha baja
            fechaBaja = "";
            if (!Utils.IsBlankField(cfd.FechaBaja))
            {
                if (db.GetDate(cfd.FechaBaja) != null)
                {
                    fechaBaja = cfd.FechaBaja;
                }
                else
                {
                    string myAlertMsg = "Aviso en cliente distribuidor. Fecha baja ({0}) incorrecta para el cliente {1}.";
                    Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0010", myAlertMsg, cfd.FechaBaja, codigoCliente);
                }
            }

            //Comprobaremos si el cliente ya está dado de alta para ese distribuidor
            Int32 clienteFinal = 0;            
            Double numCodigoCliente = 0;
            //Obtener código de cliente final
            if (Double.TryParse(codigoCliente, out numCodigoCliente))
            {                
                if (htCFAgentes.ContainsKey(codigoCliente))
                {
                    sCFAgente cf = (sCFAgente)htCFAgentes[codigoCliente];
                    clienteFinal = cf.idcClienteFinal;
                    codigoCliente = cf.codigoCF;
                    canalNielsen = cf.canal;
                }
                else if (htCFAgentesNum.ContainsKey(numCodigoCliente))
                {
                    sCFAgente cf = (sCFAgente)htCFAgentesNum[numCodigoCliente];
                    clienteFinal = cf.idcClienteFinal;
                    codigoCliente = cf.codigoCF;
                    canalNielsen = cf.canal;
                }
            }
            else
            {
                if (htCFAgentes.ContainsKey(codigoCliente))
                {
                    sCFAgente cf = (sCFAgente)htCFAgentes[codigoCliente];
                    clienteFinal = cf.idcClienteFinal;
                    codigoCliente = cf.codigoCF;
                    canalNielsen = cf.canal;
                }
            }
            if (clienteFinal != 0)
            {                
                //Si tenemos informada la área Nielsen y el canal Nielsen, y los mercados Nielsen estan vacíos los calculamos
                mercadoNielsen1 = ""; mercadoNielsen2 = ""; mercadoNielsen3 = ""; mercadoNielsen4 = ""; mercadoNielsen5 = "";
                if ((!string.IsNullOrEmpty(areaNielsen)) && (!string.IsNullOrEmpty(canalNielsen)))
                {
                    ObtenerMercadosNielsen(db, areaNielsen, canalNielsen);
                }

                //Actualizar tabla CFAgentes
                ActualizarAgentes(db, cfd, codigoCliente);

                //Actualizar tabla ClientesFinales
                ActualizarClienteFinal(db, cfd, clienteFinal, cif, direccion, codPostal.CPostal, codigoCliente);
            }
            else 
            { 
                //Cliente NO dado de alta        
                bool encontrado = false;           
                if (dist.DISTCliBuscarPorDatosIguales.Equals("S"))
                {
                    //Buscar si está dado de alta en Connecta
                    string sql = "select IdcAgente from ClientesFinales where Nombre = " + db.ValueForSql(cfd.Nombre) +
                        " AND " + (Utils.IsBlankField(direccion)? "Direccion IS NULL" : "(Direccion = " + db.ValueForSql(direccion) + " OR Direccion = " + db.ValueForSqlOK(direccion) +")") + 
                        " AND " + (Utils.IsBlankField(codPostal.CPostal)? "CodigoPostal IS NULL" : "CodigoPostal = " + db.ValueForSql(codPostal.CPostal));
                    if (cifInformado)
                    {
                        //Si CIF informado, lo incluimos en la búsqueda.
                        sql += " and CIF=" + db.ValueForSql(cif);
                    }
                    cursor = db.GetDataReader(sql);
                    if (cursor.Read())
                    {
                        //Dado de alta en Connecta.
                        encontrado = true;
                        clienteFinal = Int32.Parse(db.GetFieldValue(cursor, 0));
                        cursor.Close();
                        cursor = null;

                        canalNielsen = "";
                        //Si tenemos informada la área Nielsen y el canal Nielsen, y los mercados Nielsen estan vacíos los calculamos
                        //Pero el canal Nielsen no lo tenemos
                        mercadoNielsen1 = ""; mercadoNielsen2 = ""; mercadoNielsen3 = ""; mercadoNielsen4 = ""; mercadoNielsen5 = "";
                        if ((!string.IsNullOrEmpty(areaNielsen)) && (!string.IsNullOrEmpty(canalNielsen)))
                        {
                            ObtenerMercadosNielsen(db, areaNielsen, canalNielsen);
                        }

                        //Actualizar tabla ClientesFinales
                        ActualizarClienteFinal(db, cfd, clienteFinal, cif, direccion, codPostal.CPostal, codigoCliente);

                        //Daremos de alta un nuevo registro en CFAgentes        
                        cli.CreaCFAgentes(db, clienteFinal, codigoCliente, cfd.Nombre, cfd.RazonSocial, cfd.Status,
                            cfd.Clasificacion1, cfd.Clasificacion2, cfd.Clasificacion3, cfd.Clasificacion4, cfd.Clasificacion5, cfd.Clasificacion6, cfd.Clasificacion7, cfd.Clasificacion8, cfd.Clasificacion9, cfd.Clasificacion10,
                            cfd.Clasificacion11, cfd.Clasificacion12, cfd.Clasificacion13, cfd.Clasificacion14, cfd.Clasificacion15, cfd.Clasificacion16, cfd.Clasificacion17, cfd.Clasificacion18, cfd.Clasificacion19, cfd.Clasificacion20,
                            cfd.Clasificacion21, cfd.Clasificacion22, cfd.Clasificacion23, cfd.Clasificacion24, cfd.Clasificacion25, cfd.Clasificacion26, cfd.Clasificacion27, cfd.Clasificacion28, cfd.Clasificacion29, cfd.Clasificacion30,
                            idcClasificacion1, idcClasificacion2, idcClasificacion3, idcClasificacion4, idcClasificacion5, idcClasificacion6, idcClasificacion7, idcClasificacion8, idcClasificacion9, idcClasificacion10,
                            idcClasificacion11, idcClasificacion12, idcClasificacion13, idcClasificacion14, idcClasificacion15, idcClasificacion16, idcClasificacion17, idcClasificacion18, idcClasificacion19, idcClasificacion20,
                            idcClasificacion21, idcClasificacion22, idcClasificacion23, idcClasificacion24, idcClasificacion25, idcClasificacion26, idcClasificacion27, idcClasificacion28, idcClasificacion29, idcClasificacion30,
                            cfd.FormaPago, idcFormaPago, cfd.RecargoEquivalencia,
                            cfd.HorarioApertura, cfd.HorarioVisita, cfd.Vacaciones, fechaAlta, fechaBaja, cfd.MotivoBaja,
                            idcMotivoBaja, agent, GetSipTypeName(), string.Empty,
                            areaNielsen, canalNielsen, mercadoNielsen1, mercadoNielsen2, mercadoNielsen3, mercadoNielsen4, mercadoNielsen5);
                    }                    
                }
                if (!encontrado)
                {
                    if (!Utils.IsBlankField(cfd.Nombre) || !Utils.IsBlankField(cfd.RazonSocial)) //si no nos llega el nombre de cliente significa que insertamos el registro
                    {

                        //Añadir el cliente a la lista de clientes nuevos recibidos 
                        //Si está activado el resumen de clientes nuevos para el distribuidor se enviará un email con esta información
                        if (dist.DISTCliRequiereAvisoNuevo == "S")
                            MarcarClienteNuevoRecibido(codigoCliente, cfd.Nombre, cfd.RazonSocial, direccion, cfd.Poblacion, codPostal.CPostal);

                        //No está dado de alta en Connecta...
                        /////////////////////////////Crearemos un nuevo registro en la tabla Agentes
                        /////////////////////////////Obtenemos el identificador que le ha asignado
                        ///////////////////////////clienteFinal = cli.CreaAgente(db, agent, GetSipTypeName());                                

                        canalNielsen = "";
                        //Si tenemos informada la área Nielsen y el canal Nielsen, y los mercados Nielsen estan vacíos los calculamos
                        //Pero el canal Nielsen no lo tenemos
                        mercadoNielsen1 = ""; mercadoNielsen2 = ""; mercadoNielsen3 = ""; mercadoNielsen4 = ""; mercadoNielsen5 = "";
                        if ((!string.IsNullOrEmpty(areaNielsen)) && (!string.IsNullOrEmpty(canalNielsen)))
                        {
                            ObtenerMercadosNielsen(db, areaNielsen, canalNielsen);
                        }

                        //Crearemos un nuevo registro en ClientesFinales                    
                        cli.CreaClienteFinal(db, cfd.Nombre, cfd.RazonSocial, cif, direccion,
                            cfd.TipoCalle, cfd.Calle, cfd.Numero, cfd.Poblacion, codPostal.CPostal, cfd.CodigoPais,
                            cfd.PaginaWEB, cfd.Telefono1, cfd.Telefono2, cfd.FAX, cfd.Email, cfd.PersonaContacto,
                            cfd.CodigoMoneda, cfd.PuntoOperacional, cfd.CodigoINE, cfd.Barrio, agent, agentLocation, GetSipTypeName());

                        //Obtener código de cliente...
                        //Obtenemos el identificador que le ha asignado
                        clienteFinal = cli.ObtenerUltimoIdcClienteFinal(db);

                        sClientesFinales c = new sClientesFinales();
                        c.direccion = Utils.StringTruncate(direccion, 100);
                        c.poblacion = cfd.Poblacion;
                        c.codigoPostal = codPostal.CPostal;
                        c.N_IndFia = "";
                        c.provincia = Utils.ObtenerProvincia(codPostal.CPostal, agentLocation);
                        if (!htClientesFinales.ContainsKey(clienteFinal))
                            htClientesFinales.Add(clienteFinal, c);

                        //Daremos de alta un nuevo registro en CFAgentes        
                        cli.CreaCFAgentes(db, clienteFinal, codigoCliente, cfd.Nombre, cfd.RazonSocial, cfd.Status,
                            cfd.Clasificacion1, cfd.Clasificacion2, cfd.Clasificacion3, cfd.Clasificacion4, cfd.Clasificacion5, cfd.Clasificacion6, cfd.Clasificacion7, cfd.Clasificacion8, cfd.Clasificacion9, cfd.Clasificacion10,
                            cfd.Clasificacion11, cfd.Clasificacion12, cfd.Clasificacion13, cfd.Clasificacion14, cfd.Clasificacion15, cfd.Clasificacion16, cfd.Clasificacion17, cfd.Clasificacion18, cfd.Clasificacion19, cfd.Clasificacion20,
                            cfd.Clasificacion21, cfd.Clasificacion22, cfd.Clasificacion23, cfd.Clasificacion24, cfd.Clasificacion25, cfd.Clasificacion26, cfd.Clasificacion27, cfd.Clasificacion28, cfd.Clasificacion29, cfd.Clasificacion30,
                            idcClasificacion1, idcClasificacion2, idcClasificacion3, idcClasificacion4, idcClasificacion5, idcClasificacion6, idcClasificacion7, idcClasificacion8, idcClasificacion9, idcClasificacion10,
                            idcClasificacion11, idcClasificacion12, idcClasificacion13, idcClasificacion14, idcClasificacion15, idcClasificacion16, idcClasificacion17, idcClasificacion18, idcClasificacion19, idcClasificacion20,
                            idcClasificacion21, idcClasificacion22, idcClasificacion23, idcClasificacion24, idcClasificacion25, idcClasificacion26, idcClasificacion27, idcClasificacion28, idcClasificacion29, idcClasificacion30,
                            cfd.FormaPago, idcFormaPago, cfd.RecargoEquivalencia,
                            cfd.HorarioApertura, cfd.HorarioVisita, cfd.Vacaciones, fechaAlta, fechaBaja, cfd.MotivoBaja,
                            idcMotivoBaja, agent, GetSipTypeName(), string.Empty,
                            areaNielsen, canalNielsen, mercadoNielsen1, mercadoNielsen2, mercadoNielsen3, mercadoNielsen4, mercadoNielsen5);

                        sCFAgente cf = new sCFAgente();
                        cf.idcClienteFinal = clienteFinal;
                        cf.codigoCF = codigoCliente;
                        cf.canal = canalNielsen;
                        if (!htCFAgentes.ContainsKey(cf.codigoCF))
                            htCFAgentes.Add(cf.codigoCF, cf);
                    }
                    else
                    {
                        string myAlertMsg = "Aviso en cliente distribuidor. Nuevo cliente {0} con el nombre vacío. El cliente no será insertado.";
                        Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0046", myAlertMsg, codigoCliente);
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

    /// <summary>
    /// Proceso del objeto esclavo (que puede venir de cualquier fuente de información) y 
    /// lógica de negocio asociada.
    /// </summary>
    /// <param name="pd">objeto</param>
    public void ProcessSlave(CommonRecord rec)
    {
        Database db = Globals.GetInstance().GetDatabase();
        DbDataReader cursor = null;

        string sUsuario = Constants.PREFIJO_USUARIO_AGENTE + agent;

        try
        {
            //Si se desea hacer referencia al padre:
            //RecordClienteFinalDistribuidor parent = (RecordClienteFinalDistribuidor)rec.GetParent();

            RecordCodigoClienteFinalDistribuidor ccfd = (RecordCodigoClienteFinalDistribuidor)rec;

            //Comprobaremos si el fabricante existe en Connecta
            string sql = "";
            bool codFabricanteErroneo = false;
            int codigoFabricante = 0;

            if (Utils.IsBlankField(ccfd.CodigoFabricante))
            {
                codFabricanteErroneo = true;
            }
            else
            {
                sql = "Select IdcAgenteDestino From ClasifInterAgentes Where IdcAgenteOrigen = " + agent + " And Codigo = '" + ccfd.CodigoFabricante + "'";
                cursor = db.GetDataReader(sql);
                if (cursor.Read())
                {
                    codigoFabricante = Int32.Parse(db.GetFieldValue(cursor, 0));
                }
                else
                    codFabricanteErroneo = true;
                cursor.Close();
                cursor = null;
            }
            if (codFabricanteErroneo)
            {
                string myAlertMsg = "Error en codificación cliente. El fabricante {0} no existe en ConnectA para el distribuidor {1}.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0011", myAlertMsg, ccfd.CodigoFabricante, agent);
                //Abandonar el tratamiento de este registro...
                return;
            }

            //Comprobaremos si el cliente ya está dado de alta para ese distribuidor
            int clienteFinal = 0;
            bool existeClienteFinal = false;
            sql = "Select IdcClienteFinal From CFAgentes Where IdcAgente = " + agent + " And CodigoCF = '" + ccfd.CodigoCliente + "'";
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
            {
                clienteFinal = Int32.Parse(db.GetFieldValue(cursor, 0));
                existeClienteFinal = true;
            }
            cursor.Close();
            cursor = null;
            if (!existeClienteFinal) 
            {
                //Globals.GetInstance().GetLog().DetailedError(agent, GetSipTypeName(), "Error en codificación cliente. Cliente " + ccfd.CodigoCliente + " no existe en ConnectA para el distribuidor  "+agent);
                string myAlertMsg = "Error en codificación cliente. Cliente {0} no existe en ConnectA para el distribuidor {1}.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0012", myAlertMsg, ccfd.CodigoCliente, agent);
                //Abandonar el tratamiento de este registro...
                return;
            }

            //Comprobaremos si el código de cliente del fabricante viene rellenado o no.
            string codigoCliFab = ccfd.CodigoCliFab.Trim();
            if (Utils.IsBlankField(codigoCliFab)) 
            {
                //Globals.GetInstance().GetLog().DetailedError(agent, GetSipTypeName(), "Error en codificación cliente. " + "Código de cliente del fabricante sin informar en el cliente " + ccfd.CodigoCliente + " para el distribuidor " + agent + " y el fabricante " + codigoFabricante);
                string myAlertMsg = "Error en codificación cliente. Código de cliente del fabricante sin informar en el cliente {0} para el distribuidor {1} y el fabricante {2}";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0013", myAlertMsg, ccfd.CodigoCliente, agent, codigoFabricante.ToString());
                //Abandonar el tratamiento de este registro...
                return;
            }

            //Comprobaremos si el cliente ya está dado de alta para el fabricante
            //Pero solo si el fabricante tiene el flag correspondiente activado
            //Además si el flag tiene valor F o E, entonces se valida que exista para el fabricante y además
            //que este lo haya asignado al distribuidor
            fab.ObtenerParametros(Globals.GetInstance().GetDatabase(), codigoFabricante.ToString());
            if (fab.FABCliValidaCodificacionClientes == "S" || fab.FABCliValidaCodificacionClientes == "F" || fab.FABCliValidaCodificacionClientes == "E")
            {
                //Debemos verificar si ese código de cliente existe en CFAgentes para el fabricante
                //pero haremos una búsqueda un poco avanzada: si el dato es numérico, además lo buscaremos
                //convertido a número
                bool existeClienteParaElFabricante = false;
                sql = "Select CFAgentes.CodigoCF From CFAgentes";
                if (fab.FABCliValidaCodificacionClientes == "E")
                {
                    sql += ",CFDistribuidoresAgentes ";
                }
                sql += " Where CFAgentes.IdcAgente = " + codigoFabricante;

                Double numCodigoCliFab = 0;
                if (Double.TryParse(codigoCliFab, out numCodigoCliFab))
                {
                    sql += " And ";
                    sql += "   ( ";
                    sql += "     ( ";
                    sql += "      CFAgentes.CodigoCF = '" + codigoCliFab + "' ";
                    sql += "      OR (patindex('%[^0-9,.]%',ltrim(rtrim(CFAgentes.CodigoCF)))=0 and left(ltrim(CFAgentes.CodigoCF),1) not in ('.',',') and right(rtrim(CFAgentes.CodigoCF),1) not in ('.',',') ";
                    sql += "          and cast(replace(CFAgentes.CodigoCF,',','.') as numeric(25,0)) = " + db.ValueForSqlAsNumeric(numCodigoCliFab.ToString()) + ") ";
                    sql += "     ) ";
                    sql += "     OR ";
                    sql += "     ( ";
                    sql += "      CFAgentes.CodigoCFAlt = '" + codigoCliFab + "' ";
                    sql += "      OR (patindex('%[^0-9,.]%',ltrim(rtrim(CFAgentes.CodigoCFAlt)))=0 and left(ltrim(CFAgentes.CodigoCFAlt),1) not in ('.',',') and right(rtrim(CFAgentes.CodigoCFAlt),1) not in ('.',',') ";
                    sql += "          and cast(replace(CFAgentes.CodigoCFAlt,',','.') as numeric(25,0)) = " + db.ValueForSqlAsNumeric(numCodigoCliFab.ToString()) + ") ";
                    sql += "     ) ";
                    sql += "   ) ";
                }
                else
                {
                    sql += " And ";
                    sql += "   ( ";
                    sql += "     CFAgentes.CodigoCF = '" + codigoCliFab + "' ";
                    sql += "     OR ";
                    sql += "     CFAgentes.CodigoCFAlt = '" + codigoCliFab + "' ";
                    sql += "   ) ";
                }

                if (fab.FABCliValidaCodificacionClientes == "F")
                {
                    //Necesitamos obtener el código de distribuidor según el fabricante
                    string codigoDistFab = "";
                    string sqlAux = "Select Codigo From ClasifInterAgentes Where IdcAgenteOrigen = " + codigoFabricante + " And IdcAgenteDestino = " + agent ;
                    cursor = db.GetDataReader(sqlAux);
                    if (cursor.Read())
                    {
                        codigoDistFab = db.GetFieldValue(cursor, 0);
                    }
                    cursor.Close();
                    cursor = null;
                    if (!Utils.IsBlankField(codigoDistFab))
                    {
                        sql += " And CFAgentes.CodigoDistribuidor = '" + codigoDistFab + "'";
                    }
                }
                if (fab.FABCliValidaCodificacionClientes == "E")
                {
                    sql += " And CFAgentes.IdcAgente = CFDistribuidoresAgentes.IdcAgente ";
                    sql += " And CFAgentes.CodigoCF = CFDistribuidoresAgentes.CodigoCF ";
                    sql += " And CFDistribuidoresAgentes.IdcDistribuidor = " + agent;
                }
                
                cursor = db.GetDataReader(sql);
                if (cursor.Read())
                {
                    //Si existe, como puede ser ligeramente diferente (con 0's delante o sin 0's delante etc..)
                    //lo que hacemos es asumir que el valor bueno es el que indica el fabricante y por lo tanto
                    //lo cargamos en la variable y es el que grabaremos como dato recibido
                    codigoCliFab = db.GetFieldValue(cursor, 0);
                    existeClienteParaElFabricante = true;
                }
                cursor.Close();
                cursor = null;
                if (!existeClienteParaElFabricante)
                {
                    //si no es un cliente autorizado por el fabricante para el distribuidor, aun queda una posibilidad
                    //para permitir alinear este cliente, si se trata de un cliente ficticio lo alinearemos.
                    sql = "SELECT CA.Codigo "
                            + " FROM ClasificacionAgente CA"
                            + " WHERE CA.EntidadClasificada = 'PRESCLFI' "
                            + " AND CA.IdcAgente = " + codigoFabricante
                            + " AND CA.Codigo = '" + codigoCliFab + "'";
                    cursor = db.GetDataReader(sql);
                    if (cursor.Read())
                    {
                        //Si existe, como puede ser ligeramente diferente (con 0's delante o sin 0's delante etc..)
                        //lo que hacemos es asumir que el valor bueno es el que indica el fabricante y por lo tanto
                        //lo cargamos en la variable y es el que grabaremos como dato recibido
                        codigoCliFab = db.GetFieldValue(cursor, 0);
                        existeClienteParaElFabricante = true;
                    }
                    cursor.Close();
                    cursor = null;
                }
                if (!existeClienteParaElFabricante)
                {
                    //Globals.GetInstance().GetLog().DetailedError(agent, GetSipTypeName(), "Error en codificación cliente. Cliente " + codigoCliFab + " no existe en ConnectA para el fabricante  " + codigoFabricante);
                    string myAlertMsg = "Error en codificación cliente. Cliente {0} no existe en ConnectA para el fabricante {1}.";
                    Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0014", myAlertMsg, codigoCliFab, codigoFabricante.ToString());
                    //Abandonar el tratamiento de este registro...
                    return;
                }
            }

            //Comprobaremos si esa codificación de cliente final ya está dado de alta para ese distribuidor y fabricante
            string where = " Where IdcAgente = " + agent +
                            " And IdcFabricante = " + codigoFabricante +
                            " And CodigoCF = '" + ccfd.CodigoCliente + "' ";
            sql = "Select IndAlineacionManual From CFCodificacionAgentes " + where;
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
            {
                //Solo actualizamos el registro si el IndAlineacionManual no es S, lo que significaria que ha sido modificado des de GestionA manualmente
                //y por lo tanto no pude modificarse con la información que nos viene de un interface.
                if (db.GetFieldValue(cursor, 0) != "S")
                {
                    //Actualizar tabla CFCodificacionAgentes
                    sql = "update CFCodificacionAgentes set IdcClienteFinal=" + clienteFinal +
                            ",CodigoFab=" + db.ValueForSql(ccfd.CodigoFabricante) +
                            ",CodigoCliFab=" + db.ValueForSql(codigoCliFab) +
                            ",CIF=" + db.ValueForSql(ccfd.CIF) +
                            ",Clasificacion1=" + db.ValueForSql(ccfd.Clasificacion1) +
                            ",Clasificacion2=" + db.ValueForSql(ccfd.Clasificacion2) +
                            ",Clasificacion3=" + db.ValueForSql(ccfd.Clasificacion3) +
                            ",Clasificacion4=" + db.ValueForSql(ccfd.Clasificacion4) +
                            ",Libre1=" + db.ValueForSql(ccfd.Libre1) + " " +
                            ",FechaModificacion=" + db.SysDate() +
                            ",UsuarioModificacion=" + db.ValueForSql(sUsuario) +
                            ",IndAlineacionManual=" + db.ValueForSql("N") +
                    where;
                }
            }
            else
            { 
                //Daremos de alta un nuevo registro en CFCodificacionAgentes 
                sql = "insert into CFCodificacionAgentes (IdcAgente,IdcClienteFinal,IdcFabricante,CodigoCF,CodigoFab"+
                        ",CodigoCliFab,CIF,Clasificacion1,Clasificacion2,Clasificacion3,Clasificacion4,Libre1,FechaInsercion,FechaModificacion,UsuarioInsercion,UsuarioModificacion,IndAlineacionManual) " +
                        "values ("+agent+","+clienteFinal+","+codigoFabricante+
                        ","+db.ValueForSql(ccfd.CodigoCliente) +
                        ","+db.ValueForSql(ccfd.CodigoFabricante) +
                        "," + db.ValueForSql(codigoCliFab) +
                        ","+db.ValueForSql(ccfd.CIF) +
                        ","+db.ValueForSql(ccfd.Clasificacion1) +
                        "," + db.ValueForSql(ccfd.Clasificacion2) +
                        "," + db.ValueForSql(ccfd.Clasificacion3) +
                        "," + db.ValueForSql(ccfd.Clasificacion4) +
                        "," + db.ValueForSql(ccfd.Libre1)+
                        "," + db.SysDate() +
                        "," + db.SysDate() +
                        "," + db.ValueForSql(sUsuario) +
                        "," + db.ValueForSql(sUsuario) +
                        "," + db.ValueForSql("N") +
                        ")";
            }
            cursor.Close();
            cursor = null;

            //Ejecutar insert o update
            db.ExecuteSql(sql, agent, GetSipTypeName());
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
    }

    private void ComprobarClasificaciones(Database db, RecordClienteFinalDistribuidor cfd, string pCodigoCliente)
    {
        idcClasificacion1 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion1))
        {
            if (htClasificaciones.ContainsKey("AGENTE001;" + cfd.Clasificacion1.Trim()))                
            {
                idcClasificacion1 = htClasificaciones["AGENTE001;" + cfd.Clasificacion1.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE001", cfd.Clasificacion1, "Tipo cliente " + cfd.Clasificacion1);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 1 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0015", myAlertMsg, cfd.Clasificacion1, pCodigoCliente);                
                string clave = "AGENTE001" + ";" + cfd.Clasificacion1.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion2 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion2))
        {
            if (htClasificaciones.ContainsKey("AGENTE002;" + cfd.Clasificacion2.Trim()))
            {
                idcClasificacion2 = htClasificaciones["AGENTE002;" + cfd.Clasificacion2.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE002", cfd.Clasificacion2, "Clasif. " + cfd.Clasificacion2);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 2 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0016", myAlertMsg, cfd.Clasificacion2, pCodigoCliente);
                string clave = "AGENTE002" + ";" + cfd.Clasificacion2.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion3 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion3))
        {
            if (htClasificaciones.ContainsKey("AGENTE003;" + cfd.Clasificacion3.Trim()))
            {
                idcClasificacion3 = htClasificaciones["AGENTE003;" + cfd.Clasificacion3.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE003", cfd.Clasificacion3, "Clasif. " + cfd.Clasificacion3);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 3 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0017", myAlertMsg, cfd.Clasificacion3, pCodigoCliente);
                string clave = "AGENTE003" + ";" + cfd.Clasificacion3.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion4 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion4))
        {
            if (htClasificaciones.ContainsKey("AGENTE004;" + cfd.Clasificacion4.Trim()))
            {
                idcClasificacion4 = htClasificaciones["AGENTE004;" + cfd.Clasificacion4.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE004", cfd.Clasificacion4, "Clasif. " + cfd.Clasificacion4);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 4 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0018", myAlertMsg, cfd.Clasificacion4, pCodigoCliente);
                string clave = "AGENTE004" + ";" + cfd.Clasificacion4.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion5 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion5))
        {
            if (htClasificaciones.ContainsKey("AGENTE005;" + cfd.Clasificacion5.Trim()))
            {
                idcClasificacion5 = htClasificaciones["AGENTE005;" + cfd.Clasificacion5.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE005", cfd.Clasificacion5, "Clasif. " + cfd.Clasificacion5);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 5 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0019", myAlertMsg, cfd.Clasificacion5, pCodigoCliente);
                string clave = "AGENTE005" + ";" + cfd.Clasificacion5.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion6 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion6))
        {
            if (htClasificaciones.ContainsKey("AGENTE006;" + cfd.Clasificacion6.Trim()))
            {
                idcClasificacion6 = htClasificaciones["AGENTE006;" + cfd.Clasificacion6.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE006", cfd.Clasificacion6, "Clasif. " + cfd.Clasificacion6);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 6 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0020", myAlertMsg, cfd.Clasificacion6, pCodigoCliente);
                string clave = "AGENTE006" + ";" + cfd.Clasificacion6.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion7 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion7))
        {
            if (htClasificaciones.ContainsKey("AGENTE007;" + cfd.Clasificacion7.Trim()))
            {
                idcClasificacion7 = htClasificaciones["AGENTE007;" + cfd.Clasificacion7.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE007", cfd.Clasificacion7, "Clasif. " + cfd.Clasificacion7);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 7 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0021", myAlertMsg, cfd.Clasificacion7, pCodigoCliente);
                string clave = "AGENTE007" + ";" + cfd.Clasificacion7.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion8 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion8))
        {
            if (htClasificaciones.ContainsKey("AGENTE008;" + cfd.Clasificacion8.Trim()))
            {
                idcClasificacion8 = htClasificaciones["AGENTE008;" + cfd.Clasificacion8.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE008", cfd.Clasificacion8, "Tipo cliente " + cfd.Clasificacion8);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 8 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0023", myAlertMsg, cfd.Clasificacion8, pCodigoCliente);
                string clave = "AGENTE008" + ";" + cfd.Clasificacion8.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion9 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion9))
        {
            if (htClasificaciones.ContainsKey("AGENTE009;" + cfd.Clasificacion9.Trim()))
            {
                idcClasificacion9 = htClasificaciones["AGENTE009;" + cfd.Clasificacion9.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE009", cfd.Clasificacion9, "Tipo cliente " + cfd.Clasificacion9);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 9 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0024", myAlertMsg, cfd.Clasificacion9, pCodigoCliente);
                string clave = "AGENTE009" + ";" + cfd.Clasificacion9.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion10 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion10))
        {
            if (htClasificaciones.ContainsKey("AGENTE010;" + cfd.Clasificacion10.Trim()))
            {
                idcClasificacion10 = htClasificaciones["AGENTE010;" + cfd.Clasificacion10.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE010", cfd.Clasificacion10, "Tipo cliente " + cfd.Clasificacion10);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 10 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0025", myAlertMsg, cfd.Clasificacion10, pCodigoCliente);
                string clave = "AGENTE010" + ";" + cfd.Clasificacion10.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion11 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion11))
        {
            if (htClasificaciones.ContainsKey("AGENTE011;" + cfd.Clasificacion11.Trim()))
            {
                idcClasificacion11 = htClasificaciones["AGENTE011;" + cfd.Clasificacion11.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE011", cfd.Clasificacion11, "Tipo cliente " + cfd.Clasificacion11);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 11 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0026", myAlertMsg, cfd.Clasificacion11, pCodigoCliente);
                string clave = "AGENTE011" + ";" + cfd.Clasificacion11.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion12 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion12))
        {
            if (htClasificaciones.ContainsKey("AGENTE012;" + cfd.Clasificacion12.Trim()))
            {
                idcClasificacion12 = htClasificaciones["AGENTE012;" + cfd.Clasificacion12.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE012", cfd.Clasificacion12, "Tipo cliente " + cfd.Clasificacion12);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 12 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0027", myAlertMsg, cfd.Clasificacion12, pCodigoCliente);
                string clave = "AGENTE012" + ";" + cfd.Clasificacion12.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion13 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion13))
        {
            if (htClasificaciones.ContainsKey("AGENTE013;" + cfd.Clasificacion13.Trim()))
            {
                idcClasificacion13 = htClasificaciones["AGENTE013;" + cfd.Clasificacion13.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE013", cfd.Clasificacion13, "Tipo cliente " + cfd.Clasificacion13);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 13 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0028", myAlertMsg, cfd.Clasificacion13, pCodigoCliente);
                string clave = "AGENTE013" + ";" + cfd.Clasificacion13.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion14 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion14))
        {
            if (htClasificaciones.ContainsKey("AGENTE014;" + cfd.Clasificacion14.Trim()))
            {
                idcClasificacion14 = htClasificaciones["AGENTE014;" + cfd.Clasificacion14.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE014", cfd.Clasificacion14, "Tipo cliente " + cfd.Clasificacion14);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 14 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0029", myAlertMsg, cfd.Clasificacion14, pCodigoCliente);
                string clave = "AGENTE014" + ";" + cfd.Clasificacion14.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion15 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion15))
        {
            if (htClasificaciones.ContainsKey("AGENTE015;" + cfd.Clasificacion15.Trim()))
            {
                idcClasificacion15 = htClasificaciones["AGENTE015;" + cfd.Clasificacion15.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE015", cfd.Clasificacion15, "Tipo cliente " + cfd.Clasificacion15);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 15 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0030", myAlertMsg, cfd.Clasificacion15, pCodigoCliente);
                string clave = "AGENTE015" + ";" + cfd.Clasificacion15.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion16 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion16))
        {
            if (htClasificaciones.ContainsKey("AGENTE016;" + cfd.Clasificacion16.Trim()))
            {
                idcClasificacion16 = htClasificaciones["AGENTE016;" + cfd.Clasificacion16.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE016", cfd.Clasificacion16, "Tipo cliente " + cfd.Clasificacion16);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 16 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0031", myAlertMsg, cfd.Clasificacion16, pCodigoCliente);
                string clave = "AGENTE016" + ";" + cfd.Clasificacion16.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion17 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion17))
        {
            if (htClasificaciones.ContainsKey("AGENTE017;" + cfd.Clasificacion17.Trim()))
            {
                idcClasificacion17 = htClasificaciones["AGENTE017;" + cfd.Clasificacion17.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE017", cfd.Clasificacion17, "Tipo cliente " + cfd.Clasificacion17);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 17 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0032", myAlertMsg, cfd.Clasificacion17, pCodigoCliente);
                string clave = "AGENTE017" + ";" + cfd.Clasificacion17.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion18 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion18))
        {
            if (htClasificaciones.ContainsKey("AGENTE018;" + cfd.Clasificacion18.Trim()))
            {
                idcClasificacion18 = htClasificaciones["AGENTE018;" + cfd.Clasificacion18.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE018", cfd.Clasificacion18, "Tipo cliente " + cfd.Clasificacion18);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 18 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0033", myAlertMsg, cfd.Clasificacion18, pCodigoCliente);
                string clave = "AGENTE018" + ";" + cfd.Clasificacion18.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion19 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion19))
        {
            if (htClasificaciones.ContainsKey("AGENTE019;" + cfd.Clasificacion19.Trim()))
            {
                idcClasificacion19 = htClasificaciones["AGENTE019;" + cfd.Clasificacion19.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE019", cfd.Clasificacion19, "Tipo cliente " + cfd.Clasificacion19);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 19 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0034", myAlertMsg, cfd.Clasificacion19, pCodigoCliente);
                string clave = "AGENTE019" + ";" + cfd.Clasificacion19.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion20 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion20))
        {
            if (htClasificaciones.ContainsKey("AGENTE020;" + cfd.Clasificacion20.Trim()))
            {
                idcClasificacion20 = htClasificaciones["AGENTE020;" + cfd.Clasificacion20.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE020", cfd.Clasificacion20, "Tipo cliente " + cfd.Clasificacion20);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 20 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0035", myAlertMsg, cfd.Clasificacion20, pCodigoCliente);
                string clave = "AGENTE020" + ";" + cfd.Clasificacion20.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion21 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion21))
        {
            if (htClasificaciones.ContainsKey("AGENTE021;" + cfd.Clasificacion21.Trim()))
            {
                idcClasificacion21 = htClasificaciones["AGENTE021;" + cfd.Clasificacion21.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE021", cfd.Clasificacion21, "Tipo cliente " + cfd.Clasificacion21);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 21 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0036", myAlertMsg, cfd.Clasificacion21, pCodigoCliente);
                string clave = "AGENTE021" + ";" + cfd.Clasificacion21.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion22 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion22))
        {
            if (htClasificaciones.ContainsKey("AGENTE022;" + cfd.Clasificacion22.Trim()))
            {
                idcClasificacion22 = htClasificaciones["AGENTE022;" + cfd.Clasificacion22.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE022", cfd.Clasificacion22, "Tipo cliente " + cfd.Clasificacion22);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 22 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0037", myAlertMsg, cfd.Clasificacion22, pCodigoCliente);
                string clave = "AGENTE022" + ";" + cfd.Clasificacion22.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion23 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion23))
        {
            if (htClasificaciones.ContainsKey("AGENTE023;" + cfd.Clasificacion23.Trim()))
            {
                idcClasificacion23 = htClasificaciones["AGENTE023;" + cfd.Clasificacion23.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE023", cfd.Clasificacion23, "Tipo cliente " + cfd.Clasificacion23);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 23 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0038", myAlertMsg, cfd.Clasificacion23, pCodigoCliente);
                string clave = "AGENTE023" + ";" + cfd.Clasificacion23.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion24 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion24))
        {
            if (htClasificaciones.ContainsKey("AGENTE024;" + cfd.Clasificacion24.Trim()))
            {
                idcClasificacion24 = htClasificaciones["AGENTE024;" + cfd.Clasificacion24.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE024", cfd.Clasificacion24, "Tipo cliente " + cfd.Clasificacion24);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 24 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0039", myAlertMsg, cfd.Clasificacion24, pCodigoCliente);
                string clave = "AGENTE024" + ";" + cfd.Clasificacion24.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion25 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion25))
        {
            if (htClasificaciones.ContainsKey("AGENTE025;" + cfd.Clasificacion25.Trim()))
            {
                idcClasificacion25 = htClasificaciones["AGENTE025;" + cfd.Clasificacion25.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE025", cfd.Clasificacion25, "Tipo cliente " + cfd.Clasificacion25);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 25 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0040", myAlertMsg, cfd.Clasificacion25, pCodigoCliente);
                string clave = "AGENTE025" + ";" + cfd.Clasificacion25.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion26 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion26))
        {
            if (htClasificaciones.ContainsKey("AGENTE026;" + cfd.Clasificacion26.Trim()))
            {
                idcClasificacion26 = htClasificaciones["AGENTE026;" + cfd.Clasificacion26.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE026", cfd.Clasificacion26, "Tipo cliente " + cfd.Clasificacion26);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 26 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0041", myAlertMsg, cfd.Clasificacion26, pCodigoCliente);
                string clave = "AGENTE026" + ";" + cfd.Clasificacion26.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion27 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion27))
        {
            if (htClasificaciones.ContainsKey("AGENTE027;" + cfd.Clasificacion27.Trim()))
            {
                idcClasificacion27 = htClasificaciones["AGENTE027;" + cfd.Clasificacion27.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE027", cfd.Clasificacion27, "Tipo cliente " + cfd.Clasificacion27);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 27 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0042", myAlertMsg, cfd.Clasificacion27, pCodigoCliente);
                string clave = "AGENTE027" + ";" + cfd.Clasificacion27.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion28 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion28))
        {
            if (htClasificaciones.ContainsKey("AGENTE028;" + cfd.Clasificacion28.Trim()))
            {
                idcClasificacion28 = htClasificaciones["AGENTE028;" + cfd.Clasificacion28.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE028", cfd.Clasificacion28, "Tipo cliente " + cfd.Clasificacion28);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 28 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0043", myAlertMsg, cfd.Clasificacion28, pCodigoCliente);
                string clave = "AGENTE028" + ";" + cfd.Clasificacion28.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion29 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion29))
        {
            if (htClasificaciones.ContainsKey("AGENTE029;" + cfd.Clasificacion29.Trim()))
            {
                idcClasificacion29 = htClasificaciones["AGENTE029;" + cfd.Clasificacion29.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE029", cfd.Clasificacion29, "Tipo cliente " + cfd.Clasificacion29);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 29 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0044", myAlertMsg, cfd.Clasificacion29, pCodigoCliente);
                string clave = "AGENTE029" + ";" + cfd.Clasificacion29.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
        }

        idcClasificacion30 = "";
        if (!Utils.IsBlankField(cfd.Clasificacion30))
        {
            if (htClasificaciones.ContainsKey("AGENTE030;" + cfd.Clasificacion30.Trim()))
            {
                idcClasificacion30 = htClasificaciones["AGENTE030;" + cfd.Clasificacion30.Trim()].ToString();
            }
            else
            {
                InsertarEntidadClasificada(db, "AGENTE030", cfd.Clasificacion30, "Tipo cliente " + cfd.Clasificacion30);
                string myAlertMsg = "Aviso en cliente distribuidor. Clasificación 30 ({0}) no encontrada para el cliente {1}. Ha sido insertado de forma automática como entidad clasificada.";
                Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0045", myAlertMsg, cfd.Clasificacion30, pCodigoCliente);                
                string clave = "AGENTE030" + ";" + cfd.Clasificacion30.Trim();
                if (!htClasificaciones.ContainsKey(clave))
                    htClasificaciones.Add(clave, "");
            }
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
            string sql = "select Libre1 from ClasificacionAgente " +
                         "where EntidadClasificada = '" + pEntidadClasificada + "' and IdcAgente = " + agent + " and Codigo='" + pCodigo + "'";
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
            {
                idcEntidadClasificada = db.GetFieldValue(cursor, 0);
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
                    db.ValueForSql(pEntidadClasificada)+
                    "," + agent +
                    "," + db.ValueForSql(pDescripcion) +
                    "," + db.ValueForSql(pCodigo) +
                    "," + db.SysDate() +
                    "," + db.SysDate() + 
                    ")";
        db.ExecuteSql(sql, agent, GetSipTypeName());
    }

    /// <summary>
    /// Añadir a lista de clientes nuevos recibidos
    /// </summary>
    /// <param name="codigo">codigo</param>
    /// <param name="nombre">nombre</param>
    /// <param name="razonSocial">razonSocial</param>    
    /// <param name="direccion">direccion</param>
    /// <param name="poblacion">poblacion</param>
    /// <param name="cp">cp</param>
    private void MarcarClienteNuevoRecibido(string codigo, string nombre, string razonSocial, string direccion, string poblacion, string cp)
    {
        bool bEncontrado = false;

        foreach (ClienteNuevoRecibido cn in clientesNuevosRecibidos)
        {
            if (codigo == cn.codigo && nombre == cn.nombre && cp == cn.cp)
            {
                bEncontrado = true;
                break;
            }
        }
        if (!bEncontrado)
        {
            //Añadir el cliente a la lista resumen
            ClienteNuevoRecibido cn = new ClienteNuevoRecibido(codigo, nombre, razonSocial, direccion, poblacion, cp);
            clientesNuevosRecibidos.Add(cn);
        }
    }

    /// <summary>
    /// Generar el resumen de clientes para enviarlo por email al distribuidor
    /// </summary>
    private void GenerarResumenClientes()
    {
        //Cargamos la configuración del cliente SMTP
        string sSMTPServer = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_SERVER, IniManager.GetIniFile());
        string sRequiresAuthentication = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_AUTHENTICATION, IniManager.GetIniFile());
        string sUser = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_USER, IniManager.GetIniFile());
        string sPassword = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_PWRD, IniManager.GetIniFile());

        string sNomDist = dist.ObtenerNombre(Globals.GetInstance().GetDatabase(), agent);

        //Instanciamos el objeto para enviar SMTP, preparamos su configuración
        //e invocamos el método que hace el trabajo.
        SMTPSender oSMTPSender = new SMTPSender();
        oSMTPSender.sFROM = dist.DISTCliAvisoNuevoEMailFrom;
        oSMTPSender.sTO = dist.DISTCliAvisoNuevoEMailTo;
        oSMTPSender.sCC = "";
        oSMTPSender.sBCC = "";
        oSMTPSender.sSUBJECT = ObtenerMensajeAsuntoResumen(sNomDist);

        oSMTPSender.sBODY = ObtenerMensajeCuerpoResumen(sNomDist);
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
            string myAlertMsg = "uveSMTPSender no pudo enviar el mensaje al distribuidor: {0}.";
            Globals.GetInstance().GetLog2().Trace(agent, GetSipTypeName(), "CFDI0022", myAlertMsg, agent);
        }
    }

    /// <summary>
    /// Cargamos en memoria los mercados y las áreas nielsen
    /// </summary>
    /// <param name="db">base de datos</param>
    private void CargarAreasMercadosNielsen(Database db)
    {
        htAreas.Clear();
        htMercado1.Clear();
        htMercado2.Clear();
        htMercado3.Clear();
        htMercado4.Clear();
        htMercado5.Clear();
        DbDataReader cursor = null;
        string sql = "";
        sql = "SELECT Codigo, CodigoPosPro FROM AreasNielsen";
        cursor = db.GetDataReader(sql);
        while (cursor.Read()) htAreas.Add(db.GetFieldValue(cursor, 1), db.GetFieldValue(cursor, 0));
        cursor.Close();
        cursor = null;
        sql = "SELECT CodigoMercado, NumeroMercado, AreaNielsen + '-' + CanalNielsen " +
              "  FROM MercadosNielsen " +
              " ORDER BY NumeroMercado";
        cursor = db.GetDataReader(sql);
        while (cursor.Read())
        {
            if (db.GetFieldValue(cursor, 1).Trim() == "1")
                htMercado1.Add(db.GetFieldValue(cursor, 2), db.GetFieldValue(cursor, 0));
            else if (db.GetFieldValue(cursor, 1).Trim() == "2")
                htMercado2.Add(db.GetFieldValue(cursor, 2), db.GetFieldValue(cursor, 0));
            else if (db.GetFieldValue(cursor, 1).Trim() == "3")
                htMercado3.Add(db.GetFieldValue(cursor, 2), db.GetFieldValue(cursor, 0));
            else if (db.GetFieldValue(cursor, 1).Trim() == "4")
                htMercado4.Add(db.GetFieldValue(cursor, 2), db.GetFieldValue(cursor, 0));
            else if (db.GetFieldValue(cursor, 1).Trim() == "5")
                htMercado5.Add(db.GetFieldValue(cursor, 2), db.GetFieldValue(cursor, 0));
        }
        cursor.Close();
        cursor = null;
    }

    /// <summary>
    /// Obtiene el código de la área Nielsen a partir de un código postal
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="cp">código postal</param>
    /// <returns>area nielsen</returns>
    private string ObtenerAreaNielsen(Database db, string cp)
    {
        string area = "";
        if (cp.Length >= 2)
        {
            if (htAreas.Contains(cp.Trim())) area = htAreas[cp.Trim()].ToString();
            else if (htAreas.Contains(cp.Substring(0, 2).Trim())) area = htAreas[cp.Substring(0, 2).Trim()].ToString();
        }
        return area;
    }

    /// <summary>
    /// Obtiene el código de los mercados Nielsen a partir de la área Nielsen y del canal Nielsen
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="cff">Record ClienteFinalFabricante</param>    
    private void ObtenerMercadosNielsen(Database db, string pAN, string pCN)
    {
        if (htMercado1.Contains(pAN.Trim() + "-" + pCN.Trim()))
            mercadoNielsen1 = htMercado1[pAN.Trim() + "-" + pCN.Trim()].ToString();
        if (htMercado2.Contains(pAN.Trim() + "-" + pCN.Trim()))
            mercadoNielsen2 = htMercado2[pAN.Trim() + "-" + pCN.Trim()].ToString();
        if (htMercado3.Contains(pAN.Trim() + "-" + pCN.Trim()))
            mercadoNielsen3 = htMercado3[pAN.Trim() + "-" + pCN.Trim()].ToString();
        if (htMercado4.Contains(pAN.Trim() + "-" + pCN.Trim()))
            mercadoNielsen4 = htMercado4[pAN.Trim() + "-" + pCN.Trim()].ToString();
        if (htMercado5.Contains(pAN.Trim() + "-" + pCN.Trim()))
            mercadoNielsen5 = htMercado5[pAN.Trim() + "-" + pCN.Trim()].ToString();
    }

    /// <summary>
    /// Obtener /crear el asunto de un mensaje e-mail
    /// /// </summary>
    private string ObtenerMensajeAsuntoResumen(string sNomDist)
    {
        string sStr = "Informe ConnectA: Resumen de nuevos clientes recibidos para " + sNomDist;
        return sStr;
    }

    /// <summary>
    /// Obtener /crear el cuerpo de un mensaje e-mail
    /// /// </summary>
    private string ObtenerMensajeCuerpoResumen(string sNomDist)
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
        sBody += "Se han recibido los siguientes clientes nuevos para el distribuidor " + sNomDist;
        sBody += "<br/>"; //Retorno de carro

        //Resumen totales
        int numRec = 0;
        foreach (ClienteNuevoRecibido cn in clientesNuevosRecibidos) numRec++;

        sBody += "<table align=center style='width:50%; font-family:Calibri; font-size:10.0pt'>"; //abrir tabla
        sBody += "<tr>"; //abrir fila
        sBody += "<td style='text-align:left;width:150px'>"; //abrir columna
        sBody += "Total recibidos: ";
        sBody += "</td>"; //cerrar columna
        sBody += "<td style='text-align:left'>"; //abrir columna
        sBody += numRec.ToString();
        sBody += "</td>"; //cerrar columna
        sBody += "</tr>"; //cerrar fila        
        sBody += "</table>"; //cerrar tabla
        sBody += "<br/>"; //Retorno de carro

        //Resumen clientes
        sBody += "<div align=center>"; //Abrir div
        sBody += "<table border=1 cellspacing=1 cellpadding=0 style='width:80%; font-family:Calibri; font-size:10.0pt'>"; //abrir tabla
        sBody += "<tr>"; //abrir fila
        sBody += "<td valign=top style='background:#28A046;padding:3.0pt 3.0pt 3.0pt 3.0pt'>"; //abrir columna
        sBody += "<p align=center style='text-align:center'><b>Código</b></p>";
        sBody += "</td>"; //cerrar columna
        sBody += "<td valign=top style='background:#28A046;padding:3.0pt 3.0pt 3.0pt 3.0pt'>"; //abrir columna
        sBody += "<p align=center style='text-align:center'><b>Nombre</b></p>";
        sBody += "</td>"; //cerrar columna
        sBody += "<td valign=top style='background:#28A046;padding:3.0pt 3.0pt 3.0pt 3.0pt'>"; //abrir columna
        sBody += "<p align=center style='text-align:center'><b>Razón social</b></p>";
        sBody += "</td>"; //cerrar columna
        sBody += "<td valign=top style='background:#28A046;padding:3.0pt 3.0pt 3.0pt 3.0pt'>"; //abrir columna
        sBody += "<p align=center style='text-align:center'><b>Dirección</b></p>";
        sBody += "</td>"; //cerrar columna
        sBody += "<td valign=top style='background:#28A046;padding:3.0pt 3.0pt 3.0pt 3.0pt'>"; //abrir columna
        sBody += "<p align=center style='text-align:center'><b>Población</b></p>";
        sBody += "</td>"; //cerrar columna
        sBody += "<td valign=top style='background:#28A046;padding:3.0pt 3.0pt 3.0pt 3.0pt'>"; //abrir columna
        sBody += "<p align=center style='text-align:center'><b>Código postal</b></p>";
        sBody += "</td>"; //cerrar columna        
        sBody += "</tr>"; //cerrar fila

        foreach (ClienteNuevoRecibido cn in clientesNuevosRecibidos)
        {
            sBody += "<tr>"; //abrir fila
            sBody += "<td style='padding:3.0pt 3.0pt 3.0pt 3.0pt'>"; //abrir columna
            sBody += "<p>" + cn.codigo + "</p>";
            sBody += "</td>"; //cerrar columna
            sBody += "<td style='padding:3.0pt 3.0pt 3.0pt 3.0pt'>"; //abrir columna
            sBody += "<p>" + cn.nombre + "</p>";
            sBody += "</td>"; //cerrar columna
            sBody += "<td style='padding:3.0pt 3.0pt 3.0pt 3.0pt'>"; //abrir columna
            sBody += "<p>" + cn.razonSocial + "</p>";
            sBody += "</td>"; //cerrar columna
            sBody += "<td style='padding:3.0pt 3.0pt 3.0pt 3.0pt'>"; //abrir columna
            sBody += "<p>" + cn.direccion + "</p>";
            sBody += "</td>"; //cerrar columna
            sBody += "<td style='padding:3.0pt 3.0pt 3.0pt 3.0pt'>"; //abrir columna
            sBody += "<p>" + cn.poblacion + "</p>";
            sBody += "</td>"; //cerrar columna
            sBody += "<td style='padding:3.0pt 3.0pt 3.0pt 3.0pt'>"; //abrir columna
            sBody += "<p>" + cn.cp + "</p>";
            sBody += "</td>"; //cerrar columna
            sBody += "</tr>"; //cerrar fila            
        }
        sBody += "</table>"; //cerrar tabla
        sBody += "</div>"; //Cerrar div
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

        if (string.IsNullOrEmpty(dist.DISTCliAvisoNuevoEMailFirma))            
            sBody += "<img width=213 height=49 id=\"_x0000_i1025\" src=\"https://connecta.uvesolutions.com/images/UVE_Powwering.jpg?v=20140715\" alt=\"https://connecta.uvesolutions.com/images/UVE_Powwering.jpg?v=20140715\">";
        else
            sBody += dist.DISTCliAvisoNuevoEMailFirma;

        return sBody;
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
    }
  }
}
