using System;
using System.Collections;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.IO;
using System.Data.OleDb;

namespace ConnectaLib
{
  /// <summary>
  /// Clase padre para todos los tipos de Sip. 
  /// </summary>
  public class SipManager 
  {
    public const int FILE_XML = 1;
    public const int FILE_DELIMITED = 2;
    public const int FILE_BULK = 3;
    public const int FILE_UNKNOWN = 4;
    public ArrayList filesToDelete = new ArrayList();
    public static DateTime dtInicioIntegracion;
    public static DateTime dtLogsIntegrator;
    public static DateTime dtLogsMailAgent;
    public static DateTime dtLogsFtpAgent;
    public static int contAttempts = 0;

    /// <summary>
    /// Constructor
    /// </summary>
    public SipManager() 
    { 
    }

    /// <summary>
    /// Obtener tipo de fichero (XML, delimited, etc.)
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    /// <returns>tipo de fichero</returns>
    public int GetType(string filename) 
    {
        return Utils.FileType(filename);
    }

    /// <summary>
    /// Obtener un XMLReader para procesar el fichero de entrada, aplicando
    /// un XSD para validar que sea correcto (al menos desde el punto de vista
    /// del formato esperado)
    /// </summary>
    /// <param name="filename">fichero XML</param>
    /// <param name="xsd">XSD de validación</param>
    /// <returns>XMLReader</returns>
    private XmlReader GetXMLReader(string filename,string xsd) 
    {
        XmlSchemaSet sc = new XmlSchemaSet();
        if (xsd != null) 
        {
            // Añadir esquema de validación
            StreamReader reader = new StreamReader(xsd);
            XmlSchema xmlSchema = XmlSchema.Read(reader, new ValidationEventHandler(ValidationCallBack));
            sc.Add(xmlSchema);
            reader.Close();
        }

        // Ajustar propiedades de lectura
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.ValidationType = ValidationType.Schema;
        settings.Schemas = sc;
        settings.CloseInput = true;   //Necesario para borrar el stream creado con el encoding...

        // Crear el objeto
        StreamReader stream = new StreamReader(filename, new UTF7Encoding(true));
        return XmlReader.Create(stream, settings);
    }

    // Display any validation errors.
    private static void ValidationCallBack(object sender, ValidationEventArgs e)
    {
        Console.WriteLine("Validation Error: {0}", e.Message);
    }

    /// <summary>
    /// Procesar un fichero de entrada (se delega en la clase correspondiente)
    /// </summary>
    /// <param name="agent">agente</param>
    /// <param name="sip">instancia de sip</param>
    /// <param name="filename">nombre de fichero</param>
    /// <return>true si se procesa correctamente</return>
    public bool ProcFile(string agent, Object sip, string filename)
    {
        bool ok = true;
        string fname1 = "";
        string fname2 = "";
        string strLog = "";
        Database db = Globals.GetInstance().GetDatabase();
        ISipInInterface mySip = (ISipInInterface)sip;
        string sipTypeName = mySip.GetSipTypeName();     
        try
        {
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
          
            Log2 log = Globals.GetInstance().GetLog2();
            strLog = "Proceso de integración iniciado, fichero = " + fname1;
            if (!String.IsNullOrEmpty(fname2))
                strLog += " y " + fname2;
            log.Info(agent, sipTypeName, strLog);

            //Iniciar transacción
            //SVO: (20-01-2010) Decidimos trabajar sin transacciones,nos bloquea a otros procesos concurrentes.
            //db.BeginTransaction();

            //Pre-proceso              
            mySip.PreProcess(filename);

            //Distinguir el tipo de entrada(XML,delimited,etc.)
            switch (GetType(filename))
            {
                case SipManager.FILE_DELIMITED:
                    ok = ProcessDelimited(mySip, agent, filename);
                    break;
                case SipManager.FILE_XML:
                    ok = ProcessXML(mySip, agent, fname1);
                    if (ok)
                    {
                        //Si existe el segundo fichero se procesa
                        if (!String.IsNullOrEmpty(fname2))
                        {
                            log.Info(agent, sipTypeName, "Procesando >> " + fname2);

                            log.Info(agent, sipTypeName, "Inicio de backup de (" + fname2 + ") ...");

                            //Copia de seguridad del fichero a procesar. (de inBox a backupBox).
                            InBox inbox = new InBox(agent);
                            string backupFileName = inbox.Backup(agent, sipTypeName, fname2);

                            log.InfoBackup(agent, sipTypeName, "Backup de (" + backupFileName + ") finalizado");

                            ok = ProcessXML(mySip, agent, fname2);
                        }
                    }
                    break;
                case SipManager.FILE_BULK:
                    ok = ProcessBulk(mySip, agent, filename);
                    break;
                default:
                    //Cualquier otro tipo se ignora...
                    ok = true;
                    break;
            }

            //Post-proceso
            mySip.PostProcess(filename);

            //Commit a la base de datos
            //SVO: (20-01-2010) Decidimos trabajar sin transacciones,nos bloquea a otros procesos concurrentes.
            //db.Commit();

            //Borrado de ficheros
            string fname = "";
            for (int i = 0; i < filesToDelete.Count; i++)
            {
                fname = (string)filesToDelete[i];
                if (File.Exists(fname))
                {
                    log.Info(agent, sipTypeName, "Borrando >> " + fname);
                    File.Delete(fname);
                }
            }

            if (ok)
            {
                strLog = "Proceso de integración de (" + fname1;
                if (!String.IsNullOrEmpty(fname2))
                    strLog += " y " + fname2;
                strLog += ") terminado con éxito";
                log.Info(agent, sipTypeName, strLog);
            }
            else
            {
                strLog = "Proceso de integración de (" + fname1;
                if (!String.IsNullOrEmpty(fname2))
                    strLog += " y " + fname2;
                strLog += ") terminado con ERRORES";
                log.Info(agent, sipTypeName, strLog);
            }
        }
        catch (Exception e) 
        {
            //Aunque parezca raro, se hace un commit aunque haya un problema de base de datos.
            //De momento esta estrategia se considera correcta (SVO design) para que el proceso 
            //de incorporación incluya el mayor número de registros posibles.
            //SVO: (20-01-2010) Decidimos trabajar sin transacciones,nos bloquea a otros procesos concurrentes.
            //db.Commit();
            throw e;
        }
        return ok;
    }

    /// <summary>
    /// Procesar fichero en formato delimited
    /// </summary>
    /// <param name="mySip">sip</param>
    /// <param name="agent">agente</param>
    /// <param name="filename">nombre de fichero</param>
    /// <return>true si se procesa correctamente</return>
    private bool ProcessDelimited(ISipInInterface mySip, string agent, string filename)
    {
        bool ok = true;
        string filename2 = "";
        Log2 log = Globals.GetInstance().GetLog2();
        try
        {
            String sipType = mySip.GetId();
            
            string masterNomenclator = mySip.GetMasterNomenclator();
            if (Utils.IsBlankField(masterNomenclator))
                throw new Exception("Nomenclator mal definido. Revise fichero de configuración. SIP="+mySip.GetSipTypeName());
            
            string slaveNomenclator = mySip.GetSlaveNomenclator();
            CommonRecord rec = null;

            Nomenclator n = new Nomenclator(sipType);

            //Comprobar si en el nombre del fichero llegan más de uno...
            if (filename.IndexOf(";") != -1)
            {
                StringTokenizer st = new StringTokenizer(filename, ";");
                filename = st.NextToken();
                filename2 = st.NextToken();
            }

            //Si sólo hay un nivel, no hay problema
            if ((slaveNomenclator == null) && n.IsMasterFile(sipType, filename))
            {
                rec = mySip.GetMasterRecord();
                ProcessDelimitedFile(mySip, rec, agent, filename, true);
            }
            else if(slaveNomenclator!=null)
            {
                //Obtener nombre de fichero "master" y "slave". El orden de proceso es importante porque hay verificaciones
                //del slave en función de la existencia de un registro del master. No podemos garantizar que el S.O. devuelva
                //la lista de ficheros en el orden correcto.
                //Además, debemos tener en cuenta que puede que no exista el fichero "slave" o el master o que existan los dos.

                //Obtener nombres de fichero master y slave
                string master = "";
                string slave = "";
                //int ix = -1;
                //string fnameLower = filename.ToLower();
                if (n.IsMasterFile(sipType, filename))
                {
                    //El fichero que viene es un "master". Obtener el nombre del "slave"
                    master = filename;
                    //ix = fnameLower.IndexOf(masterNomenclator.ToLower());
                    //slave = filename.Substring(0, ix);
                    //slave += slaveNomenclator+filename.Substring(ix + masterNomenclator.Length);
                    slave = filename2;
                }
                else
                {
                    //El fichero que viene es un "slave". Obtener el nombre del master
                    slave = filename;
                    //ix = fnameLower.IndexOf(slaveNomenclator.ToLower());
                    //master = filename.Substring(0, ix);
                    //master += masterNomenclator+filename.Substring(ix + slaveNomenclator.Length);
                    master = filename2;
                }

                if (!master.Equals("") && File.Exists(master))
                {
                    rec = mySip.GetMasterRecord();
                    ProcessDelimitedFile(mySip, rec, agent, master, true);
                    //Añadir a la lista de ficheros a borrar si el proceso finaliza
                    filesToDelete.Add(master);

                    //Comprobar si existe el "slave". Si existe, lo trata y lo borra 
                    //para que no vuelva a ser procesado (ver bucle de fichero en SipCore, método Start)
                    //El nombre del fichero "slave" será el mismo que el master pero
                    //cambiando sólo el nomenclator (respetando todo lo demás).
                    if (!slave.Equals("") && File.Exists(slave))
                    {
                        log.Info(agent, mySip.GetSipTypeName(), "Procesando >> " + slave);

                        log.Info(agent, mySip.GetSipTypeName(), "Inicio de backup de (" + slave + ") ...");

                        //Copia de seguridad del fichero a procesar. (de inBox a backupBox).
                        InBox inbox = new InBox(agent);
                        string backupFileName = inbox.Backup(agent, mySip.GetSipTypeName(), slave);

                        log.InfoBackup(agent, mySip.GetSipTypeName(), "Backup de (" + backupFileName + ") finalizado");

                        rec = mySip.GetSlaveRecord();
                        ProcessDelimitedFile(mySip, rec, agent, slave, false);
                        //Añadir a la lista de ficheros a borrar...
                        filesToDelete.Add(slave);
                    }
                }
                else if (!slave.Equals("") && File.Exists(slave))
                {
                    //En este caso, llega sólo el fichero "slave". Se procesa de manera "normal".
                    rec = mySip.GetSlaveRecord();
                    ProcessDelimitedFile(mySip, rec, agent, slave, false);
                }
            }
        }
        catch (Exception e)
        {
            ok = false;
            log.Error(agent, mySip.GetSipTypeName(), e);
            throw e;
        }
        finally
        {
        }
        return ok;
    }

    /// <summary>
    /// Procesa un fichero delimited
    /// </summary>
    /// <param name="mySip">sip</param>
    /// <param name="rec">registro asociado</param>
    /// <param name="agent">agente</param>
    /// <param name="filename">nombre de fichero</param>
    /// <param name="isMaster">true si es un registro maestro</param>
    private void ProcessDelimitedFile(ISipInInterface mySip, CommonRecord rec, string agent, string filename, bool isMaster)
    {
        StreamReader reader = null; 
        try
        {
            string row;
            Encoding enc = Utils.GetFileEncoding(filename);
            reader = new StreamReader(filename, enc);
            row = reader.ReadLine();

            //Leer fichero
            while (row != null)
            {
                //Solución provisional para tratar uno de los posibles casos de caracteres 
                //estraños en los ficheros. El tema está en que al final de fichero puede existir un 
                //caracter de fin de fichero. Dado que el programador desconoce en la actualidad como 
                //identificar esta situación, la solución adoptada temporalmente es la siguiente.
                if (row.Trim().Length == 1) 
                    row = "";
                
                //En algunos casos nos llega el delimitador final incompleto (cuando se trata del delimitador "|#|")
                //Podemos hacer algo para solucionar esto. Se trata de completar el delimitador: si nos llega "|" añadirle "#|"
                //y si nos llega "|#" añadirle "|".
                if (Globals.GetInstance().GetFieldSeparator(row) == "|#|")
                {
                    if (!row.Trim().EndsWith("|#|"))
                    {
                        string tmpRow = row.Trim();
                        if (tmpRow.EndsWith("|#"))
                            row = tmpRow + "|";
                        else if (tmpRow.EndsWith("|"))
                            row = tmpRow + "#|";
                    }
                }

                if (!row.Trim().Equals(""))
                {
                    //Mapear a objeto
                    rec.MapRow(row);
                    if(isMaster)
                        mySip.ProcessMaster(rec);
                    else
                        mySip.ProcessSlave(rec);
                }
                row = reader.ReadLine();
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
    }


    /// <summary>
    /// Procesar fichero de manera masiva
    /// </summary>
    /// <param name="mySip">sip</param>
    /// <param name="agent">agente</param>
    /// <param name="filename">nombre de fichero</param>
    /// <return>true si se procesa correctamente</return>
    private bool ProcessBulk(ISipInInterface mySip, string agent, string filename)
    {
        bool ok = true;
        string filename2 = "";
        Log2 log = Globals.GetInstance().GetLog2();
        try
        {
            String sipType = mySip.GetId();

            string masterNomenclator = mySip.GetMasterNomenclator();
            if (Utils.IsBlankField(masterNomenclator))
                throw new Exception("Nomenclator mal definido. Revise fichero de configuración. SIP=" + mySip.GetSipTypeName());

            string slaveNomenclator = mySip.GetSlaveNomenclator();
            CommonRecord rec = null;

            Nomenclator n = new Nomenclator(sipType);

            //Comprobar si en el nombre del fichero llegan más de uno...
            if (filename.IndexOf(";") != -1)
            {
                StringTokenizer st = new StringTokenizer(filename, ";");
                filename = st.NextToken();
                filename2 = st.NextToken();
            }

            //Si sólo hay un nivel, no hay problema
            if ((slaveNomenclator == null) && n.IsMasterFile(sipType, filename))
            {
                rec = mySip.GetMasterRecord();
                ProcessBulkFile(mySip, rec, agent, filename);
            }
            else if (slaveNomenclator != null)
            {
                //Obtener nombre de fichero "master" y "slave". El orden de proceso es importante porque hay verificaciones
                //del slave en función de la existencia de un registro del master. No podemos garantizar que el S.O. devuelva
                //la lista de ficheros en el orden correcto.
                //Además, debemos tener en cuenta que puede que no exista el fichero "slave" o el master o que existan los dos.

                //Obtener nombres de fichero master y slave
                string master = "";
                string slave = "";
                //int ix = -1;
                //string fnameLower = filename.ToLower();
                if (n.IsMasterFile(sipType, filename))
                {
                    //El fichero que viene es un "master". Obtener el nombre del "slave"
                    master = filename;
                    //ix = fnameLower.IndexOf(masterNomenclator.ToLower());
                    //slave = filename.Substring(0, ix);
                    //slave += slaveNomenclator+filename.Substring(ix + masterNomenclator.Length);
                    slave = filename2;
                }
                else
                {
                    //El fichero que viene es un "slave". Obtener el nombre del master
                    slave = filename;
                    //ix = fnameLower.IndexOf(slaveNomenclator.ToLower());
                    //master = filename.Substring(0, ix);
                    //master += masterNomenclator+filename.Substring(ix + slaveNomenclator.Length);
                    master = filename2;
                }

                if (!master.Equals("") && File.Exists(master))
                {
                    rec = mySip.GetMasterRecord();
                    ProcessBulkFile(mySip, rec, agent, master);
                    //Añadir a la lista de ficheros a borrar si el proceso finaliza
                    filesToDelete.Add(master);

                    //Comprobar si existe el "slave". Si existe, lo trata y lo borra 
                    //para que no vuelva a ser procesado (ver bucle de fichero en SipCore, método Start)
                    //El nombre del fichero "slave" será el mismo que el master pero
                    //cambiando sólo el nomenclator (respetando todo lo demás).
                    if (!slave.Equals("") && File.Exists(slave))
                    {
                        log.Info(agent, mySip.GetSipTypeName(), "Procesando >> " + slave);

                        log.Info(agent, mySip.GetSipTypeName(), "Inicio de backup de (" + slave + ") ...");

                        //Copia de seguridad del fichero a procesar. (de inBox a backupBox).
                        InBox inbox = new InBox(agent);
                        string backupFileName = inbox.Backup(agent, mySip.GetSipTypeName(), slave);

                        log.InfoBackup(agent, mySip.GetSipTypeName(), "Backup de (" + backupFileName + ") finalizado");

                        rec = mySip.GetSlaveRecord();
                        ProcessBulkFile(mySip, rec, agent, slave);
                        //Añadir a la lista de ficheros a borrar...
                        filesToDelete.Add(slave);
                    }
                }
                else if (!slave.Equals("") && File.Exists(slave))
                {
                    //En este caso, llega sólo el fichero "slave". Se procesa de manera "normal".
                    rec = mySip.GetSlaveRecord();
                    ProcessBulkFile(mySip, rec, agent, slave);
                }
            }
        }
        catch (Exception e)
        {
            ok = false;
            log.Error(agent, mySip.GetSipTypeName(), e);
            throw e;
        }
        finally
        {
        }
        return ok;
    }

    /// <summary>
    /// Procesa un fichero de manera masiva
    /// </summary>
    /// <param name="mySip">sip</param>
    /// <param name="rec">registro asociado</param>
    /// <param name="agent">agente</param>
    /// <param name="filename">nombre de fichero</param>
    /// <param name="isMaster">true si es un registro maestro</param>
    private void ProcessBulkFile(ISipInInterface mySip, CommonRecord rec, string agent, string filename)
    {
        StreamReader reader = null;
        try
        {
            mySip.ProcessBulk(filename, rec);
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
    }

    /// <summary>
    /// Método común de lectura de todos los SIP
    /// </summary>
    /// <param name="mySip">sip</param>
    /// <param name="agent">agente</param>
    /// <param name="filename">fichero</param>
    /// <returns>true si proceso correcto</returns>
    private bool ProcessXML(ISipInInterface mySip, string agent, string filename)
    {
        bool ok = true;
        XmlReader reader = null;

        string sipType = mySip.GetId();
        Nomenclator n = new Nomenclator(sipType);

        CommonRecord masterRecord = mySip.GetMasterRecord();
        CommonRecord slaveRecord = null;
        //CommonRecord currentRecord = masterRecord;
        //******STAR
        CommonRecord currentRecord = null; //OJO: se ha modificado así para que cuando solo llegan líneas, no intente procesar la cabecera.
        if (n.IsMasterFile(sipType, filename))
            currentRecord = masterRecord;
        //*******END
        
        string masterXMLSection = mySip.GetMasterXMLSection().ToLower();
        string slaveXMLSection = mySip.GetSlaveXMLSection();
        if (slaveXMLSection != null)
            slaveXMLSection = slaveXMLSection.ToLower();
        
        string xsdFile = mySip.GetXSD();


        try
        {
            string xsd = null;
            if(xsdFile!=null)
                xsd =Globals.GetInstance().GetXSDPath() + "\\" + xsdFile;

            reader = GetXMLReader(filename, xsd);

            // Parsear fichero. El fichero se recorre nodo por nodo.
            // El motor de XML permite obtener el identificador del nodo y el valor, 
            // pero no al mismo tiempo. Por ello se mantiene una copia del último 
            // elemento obtenido (lastElement).
            // En el caso de master/slave, se busca los finales de nodo. Los 
            // nodos hijo, se añaden al master record. Cuando finaliza un master record,
            // se procesa.
            // El proceso de parsing "vigila" los inicios y final de nodo y realiza las
            // acciones oportunas en cada caso.
            string lastElement = "";
            int slaveCount = 0;
            while (reader.Read())
            {
                reader.MoveToElement();
                if (reader.NodeType == XmlNodeType.Element)
                    lastElement = reader.LocalName;

                if (reader.NodeType == XmlNodeType.Text)
                {
                    //Añadir el valor al registro actual (puede ser master o slave)
                    currentRecord.PutValue(lastElement, reader.Value);
                    lastElement = "";
                }
                else if ((reader.NodeType == XmlNodeType.EndElement) && reader.LocalName.ToLower().Equals(masterXMLSection))
                {
                    //Si sólo hay un nivel, procesar el registro maestro ahora. En el caso de más de un nivel, se espera a que 
                    //se procese el primer "slave". Si no hay registros slave, se procesa.
                    if ((slaveXMLSection == null) || (slaveCount == 0))
                        mySip.ProcessMaster(masterRecord);

                    //Inicializar objeto para la próxima ejecución(necesario para eliminar claves,valores internos,etc.)
                    masterRecord.Reset();
                    currentRecord = masterRecord;
                    slaveCount = 0;
                }
                else if ((reader.NodeType == XmlNodeType.Element) && (slaveXMLSection != null) && reader.LocalName.ToLower().Equals(slaveXMLSection))
                {
                    slaveCount++;
                    //Inicio de sección "slave"
                    if (currentRecord == masterRecord)
                    {
                        //Procesar "master" antes que los hijos. Esto es necesario porque quizá deba verificar si el 
                        //registro de base de datos asociados al proceso maestro (ej. cabecera de pedidos) se ha insertado en la 
                        //base de datos.
                        mySip.ProcessMaster(masterRecord);
                    }

                    //Inicio de elemento "slave". Cambiar el currentRecord para que los attributos se añadan al nodo hijo
                    slaveRecord = mySip.GetSlaveRecord();
                    slaveRecord.SetParent(masterRecord);
                    currentRecord = slaveRecord;
                }
                else if ((reader.NodeType == XmlNodeType.EndElement) && (slaveXMLSection != null) && reader.LocalName.ToLower().Equals(slaveXMLSection))
                {
                    //Fin de elemento "slave" (añadirlo al nodo padre)
                    mySip.ProcessSlave(slaveRecord);
                    if (masterRecord != null)
                        masterRecord.AddChild(slaveRecord);
                }
            }

            //Añadir a la lista de ficheros a borrar...
            filesToDelete.Add(filename);
        }
        catch (Exception e)
        {
            ok = false;
            Globals.GetInstance().GetLog2().Error(agent, mySip.GetSipTypeName(), e);
            throw e;
        }
        finally
        {
            if (reader != null)
            {
                reader.Close();
            }
            reader = null;
        }
        return ok;
    }

    /// <summary>
    /// Método común de cálculo de número de filas y validez de formato para fichero XML
    /// </summary>
    /// <param name="mySip">sip</param>
    /// <param name="agent">agente</param>
    /// <param name="filename">fichero</param>
    /// <param name="checkFormat">flag de validación de formato</param>
    /// <returns>instancia de XMLParseResults con el resultado</returns>
    public XMLParseResults ParseXML(ISipInInterface mySip, string agent, string filename, bool checkFormat, bool isMain)
    {
        XMLParseResults results = new XMLParseResults();
        XmlReader reader = null;
        try
        {
            string xsdFile = mySip.GetXSD();
            string controlXMLSection = "";
            if (isMain)
                controlXMLSection = mySip.GetMasterXMLSection().ToLower();
            else
                controlXMLSection = mySip.GetSlaveXMLSection().ToLower();
            int numRows = 0;
            string xsd = null;
            if (checkFormat && xsdFile != null)
                xsd = Globals.GetInstance().GetXSDPath() + "\\" + xsdFile;
            reader = GetXMLReader(filename, xsd);

            // Parsear fichero.
            while (reader.Read())
            {
                reader.MoveToElement();
                if ((reader.NodeType == XmlNodeType.EndElement) && reader.LocalName.ToLower().Equals(controlXMLSection))
                {
                    numRows++;
                }
            }
            //Si no hay filas, se considera un fichero erróneo.
            //También puede ser porque el formato no sea correcto, aunque, teóricamente,
            //al aplicar el XSD debería detectarlo.
            if(numRows == 0)
                results.FormatOK = false;
            else
                results.FormatOK = true;
            results.NumRows = numRows;            
        }
        catch (Exception e)
        {
            results.FormatOK = false;
            Globals.GetInstance().GetLog2().Error(agent, mySip.GetSipTypeName(), e);
        }
        finally
        {
            if (reader != null)
            {
                reader.Close();
            }
            reader = null;
        }
        return results;
    }

    /// <summary>
    /// Método común de verificación de formato para fichero DELIMITED
    /// </summary>
    /// <param name="mySip">sip</param>
    /// <param name="agent">agente</param>
    /// <param name="filename">fichero</param>
    /// <param name="checkFormat">flag de validación de formato</param>
    /// <returns>true si formato correcto</returns>
    public bool ParseDelimited(ISipInInterface mySip, string agent, string filename, bool checkFormat)
    {
        bool result = true;

        StreamReader reader = null;

        try
        {
            if (checkFormat)
            {
                String sipType = mySip.GetId();
                string masterNomenclator = mySip.GetMasterNomenclator();
                string slaveNomenclator = mySip.GetSlaveNomenclator();
                Nomenclator n = new Nomenclator(sipType);
                CommonRecord rec = null;
                bool isMaster = false;
                if (n.IsMasterFile(sipType, filename))
                {
                    rec = mySip.GetMasterRecord();
                    isMaster = true;
                }
                else
                {
                    rec = mySip.GetSlaveRecord();
                    isMaster = false;
                }

                string row;
                if (File.Exists(filename))
                {
                    reader = File.OpenText(filename);
                    row = reader.ReadLine();
                    //Leer fichero
                    while (row != null)
                    {
                        //Solución provisional para tratar uno de los posibles casos de caracteres 
                        //estraños en los ficheros. El tema está en que al final de fichero puede existir un 
                        //caracter de fin de fichero. Dado que el programador desconoce en la actualidad como 
                        //identificar esta situación, la solución adoptada temporalmente es la siguiente.
                        if (row.Trim().Length == 1)
                            row = "";

                        //En algunos casos nos llega el delimitador final incompleto (cuando se trata del delimitador "|#|")
                        //Podemos hacer algo para solucionar esto. Se trata de completar el delimitador: si nos llega "|" añadirle "#|"
                        //y si nos llega "|#" añadirle "|".
                        if (Globals.GetInstance().GetFieldSeparator(row) == "|#|")
                        {
                            if (!row.Trim().EndsWith("|#|"))
                            {
                                string tmpRow = row.Trim();
                                if (tmpRow.EndsWith("|#"))
                                    row = tmpRow + "|";
                                else if (tmpRow.EndsWith("|"))
                                    row = tmpRow + "#|";
                            }
                        }

                        if (!row.Trim().Equals(""))
                        {
                            rec.MapRow(row);

                            if (isMaster)
                                result = mySip.ValidateMaster(rec);
                            else
                                result = mySip.ValidateSlave(rec);
                            if (!result) break;
                        }
                        row = reader.ReadLine();
                    }
                }
                else
                {
                    //si el fichero ha desaparecido se considera que no tienen formato bueno
                    result = false;
                }
            }
        }
        catch (Exception e)
        {
            result = false;
            Globals.GetInstance().GetLog2().Error(agent, mySip.GetSipTypeName(), e);
        }
        finally
        {
            if (reader != null)
                reader.Close();
            reader = null;
        }
        return result;
    }

    /// <summary>
    /// Método común de verificación de formato para fichero BULK
    /// </summary>
    /// <param name="mySip">sip</param>
    /// <param name="agent">agente</param>
    /// <param name="filename">fichero</param>
    /// <param name="checkFormat">flag de validación de formato</param>
    /// <returns>true si formato correcto</returns>
    public bool ParseBulk(ISipInInterface mySip, string agent, string filename, bool checkFormat)
    {
        bool result = true;

        StreamReader reader = null;

        try
        {
            if (checkFormat)
            {
                String sipType = mySip.GetId();
                string masterNomenclator = mySip.GetMasterNomenclator();
                string slaveNomenclator = mySip.GetSlaveNomenclator();
                Nomenclator n = new Nomenclator(sipType);
                CommonRecord rec = null;
                bool isMaster = false;
                if (n.IsMasterFile(sipType, filename))
                {
                    rec = mySip.GetMasterRecord();
                    isMaster = true;
                }
                else
                {
                    rec = mySip.GetSlaveRecord();
                    isMaster = false;
                }

                string row;
                if (File.Exists(filename))
                {
                    reader = File.OpenText(filename);
                    row = reader.ReadLine();
                    //Solo leeremos la primera línea del fichero
                    if (row != null)
                    {
                        //En algunos casos nos llega el delimitador final incompleto (cuando se trata del delimitador "|#|")
                        //Podemos hacer algo para solucionar esto. Se trata de completar el delimitador: si nos llega "|" añadirle "#|"
                        //y si nos llega "|#" añadirle "|".
                        if (Globals.GetInstance().GetFieldSeparator(row) == "|#|")
                        {
                            if (!row.Trim().EndsWith("|#|"))
                            {
                                string tmpRow = row.Trim();
                                if (tmpRow.EndsWith("|#"))
                                    row = tmpRow + "|";
                                else if (tmpRow.EndsWith("|"))
                                    row = tmpRow + "#|";
                            }
                        }

                        if (!row.Trim().Equals(""))
                        {
                            rec.MapRow(row);

                            if (isMaster)
                                result = mySip.ValidateMaster(rec);
                            else
                                result = mySip.ValidateSlave(rec);
                        }
                    }
                }
                else
                {
                    //si el fichero ha desaparecido se considera que no tienen formato bueno
                    result = false;
                }
            }
        }
        catch (Exception e)
        {
            result = false;
            Globals.GetInstance().GetLog2().Error(agent, mySip.GetSipTypeName(), e);
        }
        finally
        {
            if (reader != null)
                reader.Close();
            reader = null;
        }
        return result;
    }

  }
}
