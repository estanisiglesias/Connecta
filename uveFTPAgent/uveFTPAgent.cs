using System; 
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Collections;
using System.Data.Common;
using System.Data.OleDb;
using ConnectaLib;

namespace uveFTPAgent
{
  /// <summary>
  /// Clase que gestiona la cola de ficheros en la carpeta de intercambio de FTP.
  /// </summary>
  public class uveFTPAgent
  {
    private bool stopAuto = false;    //Flag de parada del proceso (lo activa el servicio Windows)
    private bool stopAutoForced = false;  //Flag de parada del proceso (para paradas porzadas por error de programa)
    private bool processing = false;  //Flag de proceso en curso

    /////////////////// <summary>
    /////////////////// Clase interna que almacena la relación de sip con los ficheros a procesar
    /////////////////// </summary>
    ////////////////private class NomenclatorFile
    ////////////////{
    ////////////////    public string sip = "";
    ////////////////    public string masterFile = null;
    ////////////////    public string slaveFile = null;
    ////////////////    public ArrayList aMasterFiles = new ArrayList(5);
    ////////////////    public ArrayList aSlaveFiles = new ArrayList(5);
    ////////////////}

    private class FileId
    {
        public string fullFilename = null;
        public string filenameWithoutPath = null;
        public bool isMaster = false;
        public bool isProcessed = false;
        public string sipid = "";
        public Nomenclator nomenclator = null;
    }

    /// <summary>
    /// Clase interna con información de la agrupación
    /// de ficheros a procesar por un Sip
    /// </summary>
    private class SipWorkingInfo
    {
        public string file = "";
        public string inboxFile = "";
        public bool isMain = false;
        public bool formatOK = true;
        public int numRows = 0;
        public int regSize = 0;
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public uveFTPAgent() 
    { 
    }

    /// <summary>
    /// Obtener periodo de latencia
    /// </summary>
    /// <returns>periodo en ms.</returns>
    private int GetPeriod()
    {
      int period = Int32.Parse(IniManager.INIGetKeyValue(IniManager.CFG_SECTION_FTPAGENT, IniManager.CFG_FTPAGENT_SLOTCHECK_PERIOD, IniManager.GetIniFile(), "5"));
      period = period * 1000;    //pasamos a milisegundos...
      return period;
    }

    /// <summary>
    /// Obtener path donde buscar los ficheros
    /// </summary>
    /// <returns>path</returns>
    private string GetPath()
    { 
      return IniManager.INIGetKeyValue(IniManager.CFG_SECTION_FTPAGENT, IniManager.CFG_FTPAGENT_PATH, IniManager.GetIniFile(), "c:\\Inetpub\\ftproot");
    }

    /// <summary>
    /// Comprobar si debe validar el formato del fichero de entrada
    /// </summary>
    /// <returns>true si debe validador</returns>
    private bool GetCheckFormat()
    {
      return IniManager.INIGetKeyValue(IniManager.CFG_SECTION_FTPAGENT, IniManager.CFG_FTPAGENT_CHECKFORMAT, IniManager.GetIniFile(), "1").Equals("1");
    }

    /// <summary>
    /// Obtener path y nombre de programa externo
    /// </summary>
    /// <returns>programa externo</returns>
    private string ExternalProgram()
    {
      return IniManager.INIGetKeyValue(IniManager.CFG_SECTION_FTPAGENT, IniManager.CFG_FTPAGENT_EXTERNALPGM, IniManager.GetIniFile(), "");
    }

    /// <summary>
    /// Obtener información para retrasar el proceso de un fichero
    /// formato del valor en un fichero ini: {limit num rows};{limit size bytes};{delay hh:mm:ss};{slotIni hh:mm:ss};{slotend hh:mm:ss}
    /// </summary>
    /// <returns>valor de la entrada ini solicitado</returns>
    private string FileSizeDelay(string pWitchValue)
    {
        string sResult = "";
        string fsd = IniManager.INIGetKeyValue(IniManager.CFG_SECTION_FTPAGENT, IniManager.CFG_FTPAGENT_FILESIZEDELAY, IniManager.GetIniFile(), "");
        if (String.IsNullOrEmpty(fsd)) return sResult;
        if (fsd.IndexOf(";") == -1) return sResult;
        string[] afsd = fsd.Split(';');
        if (pWitchValue == "limitnumrows")
        {
            if (afsd.Length < 1) return sResult;
            if (String.IsNullOrEmpty(afsd[0])) return sResult;
            if (!Utils.IsValidInteger(afsd[0])) return sResult;
            sResult = afsd[0];
        }
        else if (pWitchValue == "limitsize")
        {
            if (afsd.Length < 2) return sResult;
            if (String.IsNullOrEmpty(afsd[1])) return sResult;
            if (!Utils.IsValidInteger(afsd[1])) return sResult;
            sResult = afsd[1];
        }
        else if (pWitchValue == "delay")
        {
            if (afsd.Length < 3) return sResult;
            if (String.IsNullOrEmpty(afsd[2])) return sResult;
            if (!Utils.IsValidTime(afsd[2])) return sResult;
            sResult = afsd[2];
        }
        else if (pWitchValue == "slotini")
        {
            if (afsd.Length < 4) return sResult;
            if (String.IsNullOrEmpty(afsd[3])) return sResult;
            if (!Utils.IsValidTime(afsd[3])) return sResult;
            sResult = afsd[3];
        }
        else if (pWitchValue == "slotend")
        {
            if (afsd.Length < 5) return sResult;
            if (String.IsNullOrEmpty(afsd[4])) return sResult;
            if (!Utils.IsValidTime(afsd[4])) return sResult;
            sResult = afsd[4];
        }

        return sResult;
    }

    /// <summary>
    /// Obtener información para partir en varias partes un fichero (FileBreaker)
    /// formato del valor en un fichero ini: {limit size bytes};{partition num lines};{pivot field pos}
    /// </summary>
    /// <returns>valor de la entrada ini solicitado</returns>
    private string FilePartitionInfo(string pWitchSip, string pWitchValue, int pType)
    {
        string sResult = "";
        string fpi = IniManager.INIGetKeyValue(IniManager.CFG_SECTION_FILEBREAKER, pWitchSip, IniManager.GetIniFile(), "");
        if (pType == SipManager.FILE_BULK)
        {
            string fpiblk = IniManager.INIGetKeyValue(IniManager.CFG_SECTION_FILEBREAKERBULK, pWitchSip, IniManager.GetIniFile(), "");
            if (!Utils.IsBlankField(fpiblk))
                fpi = fpiblk;
        }
        if (String.IsNullOrEmpty(fpi)) return sResult;
        if (fpi.IndexOf(";") == -1) return sResult;
        string[] afpi= fpi.Split(';');
        if (pWitchValue == "limitsize")
        {
            if (afpi.Length < 1) return sResult;
            if (String.IsNullOrEmpty(afpi[0])) return sResult;
            if (!Utils.IsValidInteger(afpi[0])) return sResult;
            sResult = afpi[0];
        }
        else if (pWitchValue == "partitionnumlines")
        {
            sResult = "1000";
            if (afpi.Length < 2) return sResult;
            if (String.IsNullOrEmpty(afpi[1])) return sResult;
            if (!Utils.IsValidInteger(afpi[1])) return sResult;
            sResult = afpi[1];
        }
        else if (pWitchValue == "pivotfieldpos")
        {
            sResult = "0";
            if (afpi.Length < 3) return sResult;
            if (String.IsNullOrEmpty(afpi[2])) return sResult;
            if (!Utils.IsValidInteger(afpi[2])) return sResult;
            sResult = afpi[2];
        }

        return sResult;
    }

    /// <summary>
    /// Ejecución en modo manual
    /// </summary>
    /// <param name="args"></param>
    public void runManual(string[] args)
    {
        Globals g = null;
        try
        {
            //Comprobar parámetros
            string iniFile = null;
            string s = "";
            for (int i = 0; i < args.Length; i++)
            {
                s = args[i].ToLower();
                if (s.StartsWith("-inifile="))
                    iniFile = args[i].Substring("-inifile=".Length);
            }
            if (iniFile == null)
                usage();
            else
            {
                //Obtener instancia de globals
                g = Globals.GetInstance(iniFile);
                int period = GetPeriod();
                string ftpPath = GetPath();
                string[] nomeclators = NomenclatorFiles();
                string pgm = ExternalProgram();

                Log2 log = g.GetLog2();
                log.TraceToFile(Log2.TYPE_INFO, Log2.LEVEL_HIGH, "Inicio de uveFTPAgent en modo manual... Path=" + ftpPath + ",period=" + (period/1000), "", "");

                Database db = g.GetDatabase();
                // Bucle 
                while (true)
                {
                    //Procesar...
                    ProcessSlots(db, nomeclators, ftpPath, log, pgm);

                    //Delay...
                    System.Threading.Thread.Sleep(period);
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }
        finally
        {
            if (g != null)
                g.Close();
        }
    }

    /// <summary>
    /// Arrancar en modo automático. Normalmente esto se utilzará desde un servicio de Windows
    /// </summary>
    public void runAuto(string[] args)
    {
        //Ver http://www.codeproject.com/KB/system/WindowsService.aspx

        Globals g = null;
        string errorMsg = "";
        string iniFile = null;

        try
        {
            if (args != null && args.Length > 0)
            {
                foreach (string s in args)
                {
                    iniFile += s + " ";
                }
            }

            //Obtener instancia de globals
            g = Globals.GetInstance(iniFile);

            int period = GetPeriod();

            Log2 log = g.GetLog2();
            log.TraceToFile(Log2.TYPE_INFO, Log2.LEVEL_LOW, "Inicio de uveFTPAgent en modo automático...(delay=" + (period/1000) + " segundos).", "", "");
            System.Diagnostics.EventLog.WriteEntry(Constants.SERVICE_NAME_FTP, "Inicio de uveFTPAgent en modo automático...");

            string ftpPath = GetPath();
            Database db = g.GetDatabase();
            string[] nomeclators = NomenclatorFiles();
            string pgm = ExternalProgram();

            while (!stopAuto && !stopAutoForced)
            {
                //Procesar...
                ProcessSlots(db, nomeclators, ftpPath, log, pgm);

                //Delay...
                log.TraceToFile(Log2.TYPE_INFO, Log2.LEVEL_LOW, "Esperando para volver a interrogar directorios ftp...(delay=" + (period / 1000) + " segundos).", "", "uveFTPAgent", true);
                System.Threading.Thread.Sleep(period);
            }
        }
        catch (Exception e)
        {
            System.Diagnostics.EventLog.WriteEntry(Constants.SERVICE_NAME_FTP, "*** ERROR:" + e.Message);
            errorMsg = "*** ERROR:" + e.Message;
            if (e.InnerException != null) errorMsg += " (" + e.InnerException.Message + ").";
            errorMsg += "\n" + e.StackTrace;
            if (g != null) g.GetLog2().Error("", "", errorMsg);
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
        }
        finally
        {
            System.Diagnostics.EventLog.WriteEntry(Constants.SERVICE_NAME_FTP, "Cierre uveFTPAgent en modo automático...");
            if (g != null) g.GetLog2().TraceToFile(Log2.TYPE_INFO, Log2.LEVEL_LOW, "Cierre uveFTPAgent en modo automático...", "", "");
            //Si se ha forzado la salida del bucle, también enviamos un mensaje al administrador.
            if (stopAutoForced)
            {
                SendStopAutoEMailAlert(errorMsg);
            }
            //Cerrar todo...
            if (g != null)
                g.Close();
        }
    }

    /// <summary>
    /// Mostrar info de uso
    /// </summary>
    private void usage()
    {
        System.Console.WriteLine("Parámetros incorrectos. Use -inifile=<fichero connecta.ini>");
    }
    
    /// <summary>
    /// Detener el modo automático. Normalmente esto se utilzará desde un servicio de Windows
    /// </summary>
    public void StopAuto()
    {
        stopAuto = true;
    }

    /// <summary>
    /// Enviar un e-mail de alerta al administrador.
    /// </summary>
    public void SendStopAutoEMailAlert(string pMsg)
    {
        //Cargamos la configuración del cliente SMTP
        string sSMTPServer = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_SERVER, IniManager.GetIniFile());
        string sRequiresAuthentication = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_AUTHENTICATION, IniManager.GetIniFile());
        string sUser = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_USER, IniManager.GetIniFile());
        string sPassword = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_PWRD, IniManager.GetIniFile());

        //Obtenemos las direcciones e-mail destinatarias
        string emailFrom = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_ADMIN_FROM, IniManager.GetIniFile());
        if (Utils.IsBlankField(emailFrom))
            emailFrom = "soporte@uvesolutions.com";
        string emailTo = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_ADMIN_TO, IniManager.GetIniFile());
        if (Utils.IsBlankField(emailTo))
            emailTo = "soporte@uvesolutions.com";
        string emailCC = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_ADMIN_CC, IniManager.GetIniFile());

        //Instanciamos el objeto para enviar SMTP, preparamos su configuración
        //e invocamos el método que hace el trabajo.
        SMTPSender oSMTPSender = new SMTPSender();
        oSMTPSender.sFROM = emailFrom;
        oSMTPSender.sTO = emailTo;
        oSMTPSender.sCC = emailCC;
        oSMTPSender.sBCC = "";
        oSMTPSender.sSUBJECT = "ALERTA ConnectA: El servicio uveFTPAgent ha terminado.";

        oSMTPSender.sBODY = "El día " + DateTime.Now.ToString("yyyy/MM/dd") + " a las " + DateTime.Now.ToString("HH:mm:ss") + " horas el servicio uveFTPAgente ha terminado su ejecución en modo automático." + 
                            " Revisar la causa del problema y reiniciar el servicio." +
                            "\n" + "\n" + pMsg;
        oSMTPSender.isHTML = true;
        oSMTPSender.sATTACHMENTS = "";

        oSMTPSender.sSMTPServer = sSMTPServer;
        oSMTPSender.sRequiresAuthentication = sRequiresAuthentication;
        oSMTPSender.sUser = sUser;
        oSMTPSender.sPassword = sPassword;
        oSMTPSender.sPathLog = "";
        oSMTPSender.DoWork("", "");
        if (!oSMTPSender.bResult)
        {
            Globals.GetInstance().GetLog2().TraceToFile(Log2.TYPE_WARNING, Log2.LEVEL_MEDIUM, "uveSMTPSender no pudo enviar el mensaje al administrador", "", "");
        }
    }

    /// <summary>
    /// Obtener lista de nomenclators (sólo de entrada)
    /// </summary>
    /// <returns>lista de ficheros</returns>
    private string[] NomenclatorFiles()
    {
        string[]wrkNomenclators = Nomenclator.ListOfNomenclators();

        ArrayList tmpwrkNomenclators = new ArrayList();
        foreach (string item in wrkNomenclators)
        {
            if (!item.Equals(""))
                tmpwrkNomenclators.Add(item);
        }
        string[] allNomenclators;
        allNomenclators = (string[])tmpwrkNomenclators.ToArray(typeof(string));
        string[] inNomenclators = new string[allNomenclators.Length];

        ArrayList allReadyExistsNomenclators = new ArrayList();
        
        int i = 0;
        bool allReadyExists = false;
        foreach (string nomenclator in allNomenclators)
        {
            if (!nomenclator.Equals("") && nomenclator.ToLower().StartsWith("sipin"))
            {
                allReadyExists = false;
                foreach (string possibleNomenclator in allReadyExistsNomenclators)
                {
                    if (nomenclator.ToLower().StartsWith(possibleNomenclator.ToLower()))
                    {
                        allReadyExists = true;
                        break;
                    }
                }
                if (!allReadyExists)
                {
                    inNomenclators[i] = nomenclator;
                    allReadyExistsNomenclators.Add(nomenclator);
                }
                else
                    inNomenclators[i] = "";
            }
            else
                inNomenclators[i] = "";
            i++;
        }
        return inNomenclators;
    }

    /// <summary>
    /// Obtener lista de carpetas a tratar (activas) dentro del path FTP, a partir de la configuración.
    /// </summary>
    /// <param name="pPath">Path de entrada FTP a añadir delante de cada subcarpeta</param>
    /// <returns>lista de ficheros</returns>
    private string[] GetFTPPathSubFolders(Database db, string pPath)
    {
        string sql = "";
        DbDataReader reader = null;
        bool bHayAlgunAgente = false;
        ArrayList tmpAgents = new ArrayList();
        string sAgent = "";
        string[] resultFolders;

        try
        {
            sql = "SELECT IdcAgente FROM Agentes WHERE Status='" + Constants.ESTADO_ACTIVO + "' AND Tipo In ('D','F') Order By 1";
            reader = db.GetDataReader(sql);
            while (reader.Read())
            {
                sAgent = db.GetFieldValue(reader, 0);
                tmpAgents.Add(pPath + "\\" + Constants.AGENT_ID + sAgent.Trim());
                bHayAlgunAgente = true;
            }
            if (bHayAlgunAgente)
            {
                resultFolders = (string[])tmpAgents.ToArray(typeof(string));
                return resultFolders;
            }
            else
                return null;
        }
        finally
        {
        if (reader != null)
          reader.Close();
        }
    }

    /// <summary>
    /// Obtener lista de carpetas a tratar (todas activas i no activas pero marcadas para controlar) dentro del path FTP, a partir de la configuración.
    /// </summary>
    /// <param name="pPath">Path de entrada FTP a añadir delante de cada subcarpeta</param>
    /// <returns>lista de ficheros</returns>
    private string[] GetFTPPathSubFoldersToControl(Database db, string pPath)
    {
        string sql = "";
        DbDataReader reader = null;
        bool bHayAlgunAgente = false;
        ArrayList tmpAgents = new ArrayList();
        string sAgent = "";
        string[] resultFolders;

        try
        {
            sql = "SELECT IdcAgente FROM CfgRecepcionAgentes " +
                " WHERE IndAvisoRecepcionDatos='S' " +
                " Order By 1";
            reader = db.GetDataReader(sql);
            while (reader.Read())
            {
                sAgent = db.GetFieldValue(reader, 0);
                tmpAgents.Add(pPath + "\\" + Constants.AGENT_ID + sAgent.Trim());
                bHayAlgunAgente = true;
            }
            if (bHayAlgunAgente)
            {
                resultFolders = (string[])tmpAgents.ToArray(typeof(string));
                return resultFolders;
            }
            else
                return null;
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
    }

    private string[] GetFTPPathSubFolders(string pPath)
    {
        //Cargamos todos los agentes creados en el ini
        string fullstringFolders = IniManager.INIGetKeyValue(IniManager.CFG_SECTION_AGENTS, null, IniManager.GetIniFile());
        string[] tmpwrkFolders = fullstringFolders.Split('\0');
        ArrayList tmptmpwrkFolders = new ArrayList();
        foreach (string item in tmpwrkFolders)
        {
            if (!item.Equals("") && item.ToLower().StartsWith(Constants.AGENT_ID.ToLower()))
                tmptmpwrkFolders.Add(item);
        }
        string[] wrkFolders;
        wrkFolders = (string[])tmptmpwrkFolders.ToArray(typeof(string));

        //Cargamos todos los agentes que estan en test
        string fullstringInTest = IniManager.INIGetKeyValue(IniManager.CFG_SECTION_AGENTS, IniManager.CFG_AGENTS_IN_TEST, IniManager.GetIniFile());
        string[] testFolders = fullstringInTest.Split(';');

        string[] resultFolders = new string[wrkFolders.Length];
        int i = 0;
        int hayAlgunFolder = 0;
        foreach (string folder in wrkFolders)
        {
            if (!folder.Equals("") && folder.ToLower().StartsWith(Constants.AGENT_ID.ToLower()))
            {
                resultFolders[i] = pPath + "\\" + folder;
                hayAlgunFolder++;
                //si el agente está en la lista de agentes en test, entonces se trata como una excepción
                foreach (string testFolder in testFolders)
                {
                    if (folder.ToLower().Trim().Equals(testFolder.ToLower().Trim()))
                    {
                        resultFolders[i] = "";
                        hayAlgunFolder--;
                        break;
                    }
                }
            }
            else
            {
                resultFolders[i] = "";
            }
            i++;
        }
        if (hayAlgunFolder == 0)
            return null;
        else
            return resultFolders;
    }

    /// <summary>
    /// Obtiene parametros de recepción del agente.
    /// </summary>
    private string GetAgentReceptionFlags(Database db, string pAgent)
    {
        string sql = "";
        DbDataReader reader = null;
        string sResult = "";

        try
        {
            //sql = "SELECT IndDescomprimirZips, IndTratarSimplificada, IndTratarSegmentada, IndAvisoRecepcionDatos FROM CfgRecepcionAgentes WHERE IdcAgente=" + pAgent;
            sql = "SELECT IndDescomprimirZips, IndTratarSimplificada, IndTratarSegmentada, IndTratarALBPC0, " +
                  "       IndTratarNumLineaDup, IndAvisoRecepcionDatos, IndTratarConversorUniversal, IndTratarEDI, " +
                  "       IndTratarConversorUniversalPrimero, IndTratarConversorLongitudFija  " +
                  "  FROM CfgRecepcionAgentes WHERE IdcAgente=" + pAgent;
            reader = db.GetDataReader(sql);
            while (reader.Read())
            {
                sResult = db.GetFieldValue(reader, 0);
                sResult += ";";
                sResult += db.GetFieldValue(reader, 1);
                sResult += ";";
                sResult += db.GetFieldValue(reader, 2);
                sResult += ";";
                sResult += db.GetFieldValue(reader, 3);
                sResult += ";";
                sResult += db.GetFieldValue(reader, 4);
                sResult += ";";
                sResult += db.GetFieldValue(reader, 5);
                sResult += ";";
                sResult += db.GetFieldValue(reader, 6);
                sResult += ";";
                sResult += db.GetFieldValue(reader, 7);
                sResult += ";";
                sResult += db.GetFieldValue(reader, 8);
                sResult += ";";
                sResult += db.GetFieldValue(reader, 9);
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return sResult;
    }    

    /// <summary>
    /// Obtener nombre de fichero sin prefijo 
    /// </summary>
    /// <param name="pFileName"></param>
    /// <returns>nombre de fichero el prefijo</returns>
    private string WithoutPrefix(string pFileName)
    {
        string tmp = pFileName;
        int ix = tmp.IndexOf(".");
        if (ix != -1)
            tmp = tmp.Substring(ix + 1);
        return tmp;
    }

    /// <summary>
    /// Validar fichero XML
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    /// <param name="sipid">sip</param>
    /// <param name="agent">agent</param>
    /// <param name="checkFormat">validar formato</param>
    /// <returns>objeto de la clase XMLParseResults con los resultados</returns>
    private XMLParseResults CheckXML(string filename, string sipid, string agent, bool checkFormat, bool isMain)
    {
      SipManager sm = new SipManager();
      SipCore core = new SipCore();
      ISipInInterface sipIn = core.GetSipIn(sipid, agent);

      return sm.ParseXML(sipIn, agent, filename, checkFormat, isMain);
    }

    /// <summary>
    /// Validar fichero DELIMITED
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    /// <param name="sipid">sip</param>
    /// <param name="agent">agent</param>
    /// <param name="checkFormat">validar formato</param>
    /// <returns>true si formato correcto</returns>
    private bool CheckFormatDelimited(string filename, string sipid, string agent, bool checkFormat)
    {
        SipManager sm = new SipManager();
        SipCore core = new SipCore();
        ISipInInterface sipIn = core.GetSipIn(sipid, agent);

        return sm.ParseDelimited(sipIn, agent, filename, checkFormat);
    }

    /// <summary>
    /// Validar fichero BULK
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    /// <param name="sipid">sip</param>
    /// <param name="agent">agent</param>
    /// <param name="checkFormat">validar formato</param>
    /// <returns>true si formato correcto</returns>
    private bool CheckFormatBulk(string filename, string sipid, string agent, bool checkFormat)
    {
        SipManager sm = new SipManager();
        SipCore core = new SipCore();
        ISipInInterface sipIn = core.GetSipIn(sipid, agent);

        return sm.ParseBulk(sipIn, agent, filename, checkFormat);
    }

    /// <summary>
    /// Número de filas de un fichero delimited
    /// </summary>
    /// <param name="filename"></param>
    /// <returns>número de filas</returns>
    private int NumberOfRows(string filename)
    {
        int rows = 0;
        StreamReader reader = null;
        try
        {
            if (File.Exists(filename))
            {
                string row;
                reader = File.OpenText(filename);
                row = reader.ReadLine();
                //Leer fichero
                while (row != null)
                {
                    rows++;
                    row = reader.ReadLine();
                }
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return rows;
    }

    /// <summary>
    /// Procesar slots
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="nomenclators">lista de nomenclators</param>
    /// <param name="path">path de los slots</param>
    /// <param name="log">log</param>
    /// <param name="pgm">programa externo</param>
    private void ProcessSlots(Database db, string[] nomenclators, string path, Log2 log, string pgm)
    {
        try 
        {
            if (!processing)
            {
                processing = true;  //Para evitar solapamiento de procesos...

                bool checkFormat = GetCheckFormat();

                string agent = "";
                string fld = "";
                int fileType = SipManager.FILE_UNKNOWN;
                bool thereareFilesInFolder = false;

                //------------------------------------------------------------------------------
                //Realizar el control de recepción de ficheros especial
                //------------------------------------------------------------------------------
                ProcessFileReceptionControl(db, path, log);
                //------------------------------------------------------------------------------

                //Obtener lista de directorios de la carpeta ftp
                string[] folders = null;
                folders = GetFTPPathSubFolders(path);
                if (folders == null)
                    folders = GetFTPPathSubFolders(db, path);
                if (folders != null)
                {
                    //Para cada directorio de la carpeta ftp se va a comprobar si tiene ficheros
                    foreach (string folder in folders)
                    {
                        if (folder.Equals(".") || folder.Equals(".."))
                            continue;

                        //Si es un directorio...
                        if (Directory.Exists(folder))
                        {
                            bool found = false;
                            fld = Utils.WithoutPath(folder);
                            //Comprobar el directorio que sea del tipo correcto
                            if (!found && fld.StartsWith(Constants.AGENT_ID))
                            {
                                agent = fld.Substring(Constants.AGENT_ID.Length);

                                string receptionFlags = GetAgentReceptionFlags(db, agent);
                                bool mustUnzipAllZipFiles = (Utils.IsBlankField(receptionFlags) ? false :(receptionFlags.Split(';')[0] == "S"));
                                bool mustProcessSimplificada = (Utils.IsBlankField(receptionFlags) ? false :(receptionFlags.Split(';')[1] == "S"));
                                bool mustProcessSegmentada = (Utils.IsBlankField(receptionFlags) ? false : (receptionFlags.Split(';')[2] == "S"));
                                bool mustProcessALBPC0 = (Utils.IsBlankField(receptionFlags) ? false : (receptionFlags.Split(';')[3] == "S"));
                                bool mustProcessNumLineaDup = (Utils.IsBlankField(receptionFlags) ? false : (receptionFlags.Split(';')[4] == "S"));
                                bool mustAlertOfNewFilesReceived = (Utils.IsBlankField(receptionFlags) ? false : (receptionFlags.Split(';')[5] == "S"));
                                bool mustProcessUniversal = (Utils.IsBlankField(receptionFlags) ? false : (receptionFlags.Split(';')[6] == "S"));
                                bool mustProcessEDI = (Utils.IsBlankField(receptionFlags) ? false : (receptionFlags.Split(';')[7] == "S"));
                                bool mustProcessUniversalFirst = (Utils.IsBlankField(receptionFlags) ? false : (receptionFlags.Split(';')[8] == "S"));
                                bool mustProcessLongitudFija = (Utils.IsBlankField(receptionFlags) ? false : (receptionFlags.Split(';')[9] == "S"));

                                //------------------------------------------------------------------------------
                                //CONTROL DE FICHEROS RECIBIDOS PARA ENVIAR AVISO (SOLO PARA ESTE AGENTE)
                                //------------------------------------------------------------------------------
                                if (mustAlertOfNewFilesReceived)
                                {
                                    MirarSiHayFicherosParaEnviarMailAviso(db, agent, folder, log);
                                }

                                //------------------------------------------------------------------------------
                                //LANZAR PROCESO EXTERNO FTPAGENT.BAT
                                //------------------------------------------------------------------------------
                                //Obtener la lista de ficheros de la carpeta
                                string[] filesInFolder = Directory.GetFiles(folder, "*.*");
                                thereareFilesInFolder = (filesInFolder != null && filesInFolder.Length > 0);
                                //Verificar si hay ficheros bloqueados. Si hay alguno, continuar con otro agente
                                //descartar esta ejecución y esperar a la siguiente.
                                if (Utils.someFileIsLocked(agent, filesInFolder, folder))
                                    continue;
                                //Si no hay ninguno bloqueado, lanzar el proceso externo que, potencialmente
                                //puede "explosionar" uno de los ficheros en otros.
                                if (pgm != null && thereareFilesInFolder)
                                {
                                    //Invocar al programa externo
                                    //(de momento NO, genera demasiados log) log.Info(agent, "uveFTPAgent", "Inicio ejecución proceso externo: " + Utils.WithoutPath(pgm) + " con los parámetros: " + agent);
                                    Utils.InvokeExternalProgram(pgm, agent);
                                    //(de momento NO, genera demasiados log) log.Info(agent, "uveFTPAgent", "Fin ejecución proceso externo: " + Utils.WithoutPath(pgm) + " con los parámetros: " + agent);
                                }
                                //------------------------------------------------------------------------------

                                //------------------------------------------------------------------------------
                                //TRATAMIENTO FICHEROS ZIP RECIBIDOS
                                //------------------------------------------------------------------------------
                                //Despúes de ejecutar el proceso, analizamos de nuevo el contenido de la carpeta
                                //porque es posible que haya cambiado.
                                filesInFolder = Directory.GetFiles(folder, "*.*");
                                thereareFilesInFolder = (filesInFolder != null && filesInFolder.Length > 0);
                                //Verificar si hay ficheros bloqueados. Si hay alguno, continuar con otro agente
                                //descartar esta ejecución y esperar a la siguiente.
                                if (Utils.someFileIsLocked(agent, filesInFolder, folder))
                                    continue;
                                //Si no hay ninguno bloqueado, mirar si podemos deszipear ficheros de los que han llegado
                                if (thereareFilesInFolder)
                                {
                                    //Vamos a deszipear los ficheros con extension.zip y que coincidan con un nomenclator
                                    foreach (string file in filesInFolder)
                                    {
                                        //Comprobar que el fichero sea del tipo zip
                                        if (Utils.IsZipFileType(file) && !Utils.IsEmptyFile(file))
                                        {
                                            if (mustUnzipAllZipFiles)
                                            {
                                                Utils.UnZipFile(agent, "", file);
                                            }
                                            else
                                            {
                                                foreach (string nomenclator in nomenclators)
                                                {
                                                    if (!nomenclator.Equals(""))
                                                    {
                                                        Nomenclator n = new Nomenclator(nomenclator);
                                                        if (n.IsForThisNomenclator(Utils.WithoutPath(file)))
                                                        {
                                                            Utils.UnZipFile(agent, n.GetSip(), file);
                                                        }
                                                        else
                                                        {
                                                            //probamos eliminar el prefijo (si existe) al fichero y ver si así es un nomenclator
                                                            if (n.IsForThisNomenclator(WithoutPrefix(Utils.WithoutPath(file))))
                                                            {
                                                                Utils.UnZipFile(agent, n.GetSip(), file);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                //------------------------------------------------------------------------------

                                //------------------------------------------------------------------------------
                                //TRATAMIENTO CONVERSORES
                                //------------------------------------------------------------------------------
                                //A continuación analizamos de nuevo el contenido de la carpeta porque es posible que haya cambiado.
                                filesInFolder = Directory.GetFiles(folder, "*.*");
                                thereareFilesInFolder = (filesInFolder != null && filesInFolder.Length > 0);
                                //Verificar si hay ficheros bloqueados. Si hay alguno, continuar con otro agente
                                //descartar esta ejecución y esperar a la siguiente.
                                if (Utils.someFileIsLocked(agent, filesInFolder, folder))
                                    continue;

                                if (thereareFilesInFolder)
                                {
                                    if (mustProcessUniversal && mustProcessUniversalFirst)
                                    {
                                        conversorUniversal oUniversal = new conversorUniversal();
                                        oUniversal.DoWork(agent, folder);
                                        if (!oUniversal.bResult)
                                        {
                                            Globals.GetInstance().GetLog2().TraceToFile(Log2.TYPE_ERROR, Log2.LEVEL_CRITICAL, "El conversor Universal finalizó con errores en la carpeta " + folder + ".", agent, "");
                                        }
                                    }
                                    /*if (mustProcessSimplificada)
                                    {
                                        conversorSimplificada oSimplificada = new conversorSimplificada();
                                        oSimplificada.DoWork(agent, folder);
                                        if (!oSimplificada.bResult)
                                        {
                                            Globals.GetInstance().GetLog2().TraceToFile(Log2.TYPE_ERROR, Log2.LEVEL_CRITICAL, "El conversor simplificada finalizó con errores en la carpeta " + folder + ".", agent, "");
                                        }
                                    }
                                    if (mustProcessLongitudFija)
                                    {
                                        conversorLongitudFija oLongitudFija = new conversorLongitudFija();
                                        oLongitudFija.DoWork(agent, folder);
                                        if (!oLongitudFija.bResult)
                                        {
                                            Globals.GetInstance().GetLog2().TraceToFile(Log2.TYPE_ERROR, Log2.LEVEL_CRITICAL, "El conversor de longitud fija finalizó con errores en la carpeta " + folder + ".", agent, "");
                                        }
                                    }
                                    if (mustProcessSegmentada)
                                    {
                                        conversorSegmentada oSegmentada = new conversorSegmentada();
                                        oSegmentada.DoWork(agent, folder);
                                        if (!oSegmentada.bResult)
                                        {
                                            Globals.GetInstance().GetLog2().TraceToFile(Log2.TYPE_ERROR, Log2.LEVEL_CRITICAL, "El conversor segmentada finalizó con errores en la carpeta " + folder + ".", agent, "");
                                        }
                                    }
                                    if (mustProcessALBPC0)
                                    {
                                        conversorALBPC0 oALBPC0 = new conversorALBPC0();
                                        oALBPC0.DoWork(agent, folder);
                                        if (!oALBPC0.bResult)
                                        {
                                            Globals.GetInstance().GetLog2().TraceToFile(Log2.TYPE_ERROR, Log2.LEVEL_CRITICAL, "El conversor ALBPC0 finalizó con errores en la carpeta " + folder + ".", agent, "");
                                        }
                                    }
                                    if (mustProcessNumLineaDup)
                                    {
                                        conversorNumLineaDup oNumLineaDup = new conversorNumLineaDup();
                                        oNumLineaDup.DoWork(agent, folder);
                                        if (!oNumLineaDup.bResult)
                                        {
                                            Globals.GetInstance().GetLog2().TraceToFile(Log2.TYPE_ERROR, Log2.LEVEL_CRITICAL, "El conversor NumLineaDup finalizó con errores en la carpeta " + folder + ".", agent, "");
                                        }
                                    }                                    
                                    if (mustProcessEDI)
                                    {
                                        conversorEDI oEDI = new conversorEDI();
                                        oEDI.DoWork(agent, folder);
                                        if (!oEDI.bResult)
                                        {
                                            Globals.GetInstance().GetLog2().TraceToFile(Log2.TYPE_ERROR, Log2.LEVEL_CRITICAL, "El conversor EDI finalizó con errores en la carpeta " + folder + ".", agent, "");
                                        }
                                    }*/
                                    if (mustProcessUniversal)
                                    {
                                        conversorUniversal oUniversal = new conversorUniversal();
                                        oUniversal.DoWork(agent, folder);
                                        if (!oUniversal.bResult)
                                        {
                                            Globals.GetInstance().GetLog2().TraceToFile(Log2.TYPE_ERROR, Log2.LEVEL_CRITICAL, "El conversor Universal finalizó con errores en la carpeta " + folder + ".", agent, "");
                                        }
                                    }
                                }
                                //------------------------------------------------------------------------------

                                //------------------------------------------------------------------------------
                                //AÑADIR LOS FICHEROS QUE CUMPLEN CON EL NOMENCLATOR A LA COLA DE TAREAS
                                //------------------------------------------------------------------------------
                                //Después de mirar si podemos descompactar posibles zip, analizamos de nuevo el contenido de la carpeta
                                //porque es posible que haya cambiado.
                                filesInFolder = Directory.GetFiles(folder, "*.*");
                                thereareFilesInFolder = (filesInFolder != null && filesInFolder.Length > 0);
                                //Verificar si hay ficheros bloqueados. Si hay alguno, continuar con otro agente
                                //descartar esta ejecución y esperar a la siguiente.
                                if (Utils.someFileIsLocked(agent, filesInFolder, folder))
                                    continue;
                                
                                if (thereareFilesInFolder)
                                {
                                    //La primera pasada nos permite determinar la lista de ficheros, y 
                                    //saber si son maestro o slave.
                                    ArrayList firstPassProcess = new ArrayList();
                                    foreach (string file in filesInFolder)
                                    {
                                        //Comprobar que el fichero sea del tipo permitido (xml,txt,...)
                                        fileType = Utils.FileType(file);
                                        if (fileType != SipManager.FILE_UNKNOWN && !Utils.IsEmptyFile(file))
                                        {
                                            foreach (string nomenclator in nomenclators)
                                            {
                                                if (!nomenclator.Equals(""))
                                                {
                                                    Nomenclator n = new Nomenclator(nomenclator);
                                                    if (n.IsForThisNomenclator(Utils.WithoutPath(file)))
                                                    {
                                                        FileId fi = new FileId();
                                                        fi.fullFilename = file;
                                                        fi.filenameWithoutPath = Utils.WithoutPath(file);
                                                        fi.isMaster = n.IsMasterFile(nomenclator, file);
                                                        fi.isProcessed = false;
                                                        fi.sipid = n.GetSip();
                                                        fi.nomenclator = n;
                                                        firstPassProcess.Add(fi);
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    //La segunda pasada busca para cada fichero no procesado su maestro (o slave)
                                    //dependiendo del tipo de fichero que sea.
                                    //Se ajusta el flag "processed" a true una vez el fichero ha sido tratado para
                                    //evitar tratarlo dos veces.
                                    ArrayList firstListOfNomenclatorsFiles = new ArrayList();
                                    foreach(FileId fi in firstPassProcess)
                                    {
                                        if(!fi.isProcessed)
                                        {
                                            fi.isProcessed = true;
                                            Utils.NomenclatorFile nomenFile = new Utils.NomenclatorFile();
                                            nomenFile.sip = fi.sipid;
                                            if (fi.isMaster)
                                            {
                                                nomenFile.masterFile = fi.fullFilename;

                                                //Buscar slave (puede que no exista)
                                                FileId slave = SearchPair(fi, firstPassProcess, false);
                                                if (slave != null)
                                                {
                                                    nomenFile.slaveFile = slave.fullFilename;
                                                    slave.isProcessed = true;
                                                }
                                            }
                                            else
                                            {
                                                nomenFile.slaveFile = fi.fullFilename;

                                                //Buscar master (puede que no exista)
                                                FileId master = SearchPair(fi, firstPassProcess, true);
                                                if (master != null)
                                                {
                                                    nomenFile.masterFile = master.fullFilename;
                                                    master.isProcessed = true;
                                                }
                                            }

                                            //Añadirlo a la lista
                                            firstListOfNomenclatorsFiles.Add(nomenFile);
                                        }
                                    }

                                    //La siguiente pasada, ahora que ya tenemos solo los ficheros que cumplen con un nomenclator oficial 
                                    //y agrupados en parejas,
                                    //consistirá en partir en varias partes algunos ficheros que puedan tener un volumen demasiado grande.
                                    ArrayList secondListOfNomenclatorsFiles = new ArrayList();
                                    foreach (Utils.NomenclatorFile nf in firstListOfNomenclatorsFiles)
                                    {
                                        bool partir = false;
                                        string sLimitSize = FilePartitionInfo(nf.sip, "limitsize", fileType);
                                        string sPartitionNumLines = FilePartitionInfo(nf.sip, "partitionnumlines", fileType);
                                        string sPivotFieldPos = FilePartitionInfo(nf.sip, "pivotfieldpos", fileType);

                                        //Si este nomenclator tiene configurada la partición de ficheros demasiado grandes y el fichero es tipo TXT
                                        //comprobar si supera los límites de número de filas o tamáño, y en caso afirmativo, partir el fichero
                                        if (!Utils.IsBlankField(sLimitSize) && !Utils.IsBlankField(nf.masterFile) && (Utils.FileType(nf.masterFile) == SipManager.FILE_DELIMITED || Utils.FileType(nf.masterFile) == SipManager.FILE_BULK) && Utils.GetFileSize(nf.masterFile) > Int64.Parse(sLimitSize)) partir = true;
                                        if (!Utils.IsBlankField(sLimitSize) && !Utils.IsBlankField(nf.slaveFile) && (Utils.FileType(nf.slaveFile) == SipManager.FILE_DELIMITED || Utils.FileType(nf.slaveFile) == SipManager.FILE_BULK) && Utils.GetFileSize(nf.slaveFile) > Int64.Parse(sLimitSize)) partir = true;
                                        if (partir)
                                        {
                                            //Partir el fichero
                                            ArrayList listOfBrokenNomenclatorsFiles = new ArrayList();
                                            listOfBrokenNomenclatorsFiles = Utils.BreakFile(agent, nf.sip, nf.masterFile, nf.slaveFile, sPartitionNumLines, sPivotFieldPos);
                                            foreach (Utils.NomenclatorFile bnf in listOfBrokenNomenclatorsFiles)
                                            {
                                                //Añadirlo el fichero a la lista tal y como está
                                                secondListOfNomenclatorsFiles.Add(bnf);
                                            }
                                        }
                                        else
                                        {
                                            //Añadirlo el fichero a la lista tal y como está
                                            secondListOfNomenclatorsFiles.Add(nf);
                                        }
                                    }

                                    //Después de recorrer la lista de ficheros de la carpeta, analizamos la lista
                                    //de nomenclators/ficheros, invocando al proceso de validación, formato, etc.
                                    //para cada uno de ellos. 
                                    //Se puede dar el caso de que dado un nomenclator "complejo" sólo llegue
                                    //una de las partes. El sistema contempla esta circusntancia.
                                    found = false;
                                    foreach (Utils.NomenclatorFile nomenclatorFile in secondListOfNomenclatorsFiles)
                                    {
                                        SipWorkingInfo[] winfo = null;                                      
                                        if (nomenclatorFile.masterFile != null && nomenclatorFile.slaveFile != null)
                                        {
                                            //Hay master+slave
                                            winfo = new SipWorkingInfo[2];
                                            winfo[0] = new SipWorkingInfo();
                                            winfo[0].file = nomenclatorFile.masterFile;
                                            winfo[0].isMain = true;

                                            winfo[1] = new SipWorkingInfo();
                                            winfo[1].file = nomenclatorFile.slaveFile;
                                            winfo[1].isMain = false;
                                        }
                                        else if (nomenclatorFile.masterFile != null && nomenclatorFile.slaveFile == null)
                                        {
                                            //Sólo master
                                            winfo = new SipWorkingInfo[1];
                                            winfo[0] = new SipWorkingInfo();
                                            winfo[0].file = nomenclatorFile.masterFile;
                                            winfo[0].isMain = true;
                                        }
                                        else if (nomenclatorFile.masterFile == null && nomenclatorFile.slaveFile != null)
                                        {
                                            //Sólo slave
                                            winfo = new SipWorkingInfo[1];
                                            winfo[0] = new SipWorkingInfo();
                                            winfo[0].file = nomenclatorFile.slaveFile;
                                            winfo[0].isMain = false;
                                        }
                                        if (winfo != null)
                                        {
                                            AddToQueue(db, winfo, nomenclatorFile.sip, nomenclatorFile.priorityDelay, agent, checkFormat, log); 

                                            //Parar después de procesar
                                            found = true;
                                        }
                                    }

                                    //Cambiar (todo en el mismo instante) el estado de las colas que acabamos de crear
                                    if (found)
                                        UpdateStatusInQueue(db, agent, log);
                                }
                                //------------------------------------------------------------------------------
                            }
                        }

                        //Dar otra oportunidad de salir del proceso...
                        if (stopAuto)
                            break;
                    }
                }
            }
        }
        catch (Exception e)
        {
            System.Diagnostics.EventLog.WriteEntry(Constants.SERVICE_NAME_FTP, "*** ERROR:" + e.Message);
            log.Error("", "", "*** ERROR:" + e.Message + "\n" + e.StackTrace);
            Console.WriteLine(e.StackTrace);
            //Forzamos el cierre de la ejecución automética del programa.
            stopAutoForced = true;
        }
        finally 
        {
            processing = false;
        }
    }

    /// <summary>
    /// Procesar el control de recepcion de ficheros especial
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="path">path</param>
    /// <param name="log">log</param>
    private void ProcessFileReceptionControl(Database db, string path, Log2 log)
    {
        try
        {
            string agent = "";
            string fld = "";
            string[] folders = null;
            folders = GetFTPPathSubFoldersToControl(db, path);
            if (folders != null)
            {
                //Para cada directorio de la carpeta ftp se va a comprobar si tiene ficheros
                foreach (string folder in folders)
                {
                    if (folder.Equals(".") || folder.Equals(".."))
                        continue;

                    //Si es un directorio...
                    if (Directory.Exists(folder))
                    {
                        bool found = false;
                        fld = Utils.WithoutPath(folder);
                        //Comprobar el directorio que sea del tipo correcto
                        if (!found && fld.StartsWith(Constants.AGENT_ID))
                        {
                            agent = fld.Substring(Constants.AGENT_ID.Length);

                            MirarSiHayFicherosParaEnviarMailAviso(db, agent, folder, log);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            System.Diagnostics.EventLog.WriteEntry(Constants.SERVICE_NAME_FTP, "*** ERROR:" + e.Message);
            log.Error("", "", "*** ERROR:" + e.Message + "\n" + e.StackTrace);
            Console.WriteLine(e.StackTrace);
        }
        finally
        {
        }
    }

    private void MirarSiHayFicherosParaEnviarMailAviso(Database db, string agent, string folder, Log2 log)
    {
        bool thereareFilesInFolder = false;

        try
        {
            //Obtener la lista de ficheros de la carpeta
            string[] filesInFolder = Directory.GetFiles(folder, "*.*");
            thereareFilesInFolder = (filesInFolder != null && filesInFolder.Length > 0);

            //---------------------
            //Creemos que toda la verificación de ficheros, sobre todo la comprobación de si tenemos algun fichero bloqueado
            //està generando algun problema (utilizado des de este punto)
            //De momento lo comentamos.
            //---------------------
            //Verificar si hay ficheros bloqueados. Si hay alguno, continuar con otro agente
            //descartar esta ejecución y esperar a la siguiente.
            //if (Utils.someFileIsLocked(agent, filesInFolder, folder))
            //    continue;
            ////Comprobar que alguno de los ficheros no está vacío
            //bool someFileIsNotEmpty = false;
            //foreach (string file in filesInFolder)
            //{
            //    if (!Utils.IsEmptyFile(file)) someFileIsNotEmpty = true;
            //}
            ////Comprobar que alguno de los ficheros no sea un fichero .bad
            //bool someFileIsNotBad = false;
            //foreach (string file in filesInFolder)
            //{
            //    if (!file.ToLower().EndsWith(".bad")) someFileIsNotBad = true;
            //}
            //---------------------

            if (thereareFilesInFolder)
            {
                //Obtener los datos para enviar un mail de aviso
                //------------------------------------
                string eMailTo = "";
                string textEMail = "";
                string fecAviso = "";
                string indAviso = "";
                DateTime dtFecAviso;
                string sql = "";
                DbDataReader reader = null;
                sql = "SELECT AvisoRecepcionDatosEMailTo, TextoAvisoRecepcionDatos, FechaAvisoRecepcionDatos, IndAvisoRecepcionDatos " +
                      "  FROM CfgRecepcionAgentes " +
                      " WHERE IdcAgente = " + agent;
                reader = db.GetDataReader(sql);
                while (reader.Read())
                {
                    eMailTo = db.GetFieldValue(reader, 0);
                    textEMail = db.GetFieldValue(reader, 1);
                    //ATENCIÓN: queremos recuperar la fecha y hora, la función GetFieldValue nos recorta la hora y no nos vale para este caso
                    //fecAviso= db.GetFieldValue(reader, 2);
                    object oDate;
                    oDate = reader.GetValue(2);
                    fecAviso = ((oDate is System.DBNull) ? "" : reader.GetDateTime(2) + "");
                    indAviso = db.GetFieldValue(reader, 3);
                }
                if (reader != null)
                    reader.Close();
                reader = null;

                if (!DateTime.TryParse(fecAviso, out dtFecAviso))
                {
                    dtFecAviso = DateTime.Now.AddDays(-1);
                }
                if (Utils.someFileIsNewerThan(agent, filesInFolder, folder, dtFecAviso))
                {
                    if (indAviso.Trim().ToUpper() == "S")
                    {
                        //Enviar mail de aviso
                        //------------------------------------
                        //Cargamos la configuración del cliente SMTP
                        string sSMTPServer = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_SERVER, IniManager.GetIniFile());
                        string sRequiresAuthentication = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_AUTHENTICATION, IniManager.GetIniFile());
                        string sUser = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_USER, IniManager.GetIniFile());
                        string sPassword = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_PWRD, IniManager.GetIniFile());

                        //Obtenemos las direcciones e-mail destinatarias
                        string eMailFrom = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_SMTP_ADMIN_FROM, IniManager.GetIniFile());
                        if (Utils.IsBlankField(eMailFrom))
                            eMailFrom = "soporte@uvesolutions.com";
                        if (Utils.IsBlankField(eMailTo))
                            eMailTo = "soporte@uvesolutions.com";

                        //Instanciamos el objeto para enviar SMTP, preparamos su configuración
                        //e invocamos el método que hace el trabajo.
                        SMTPSender oSMTPSender = new SMTPSender();
                        oSMTPSender.sFROM = eMailFrom;
                        oSMTPSender.sTO = eMailTo;
                        oSMTPSender.sCC = "";
                        oSMTPSender.sBCC = "";
                        oSMTPSender.sSUBJECT = "AVISO ConnectA: Se han recibido ficheros del agente " + agent;

                        string sFileSet = "";
                        foreach (string file in filesInFolder)
                        {
                            sFileSet += " " + Utils.WithoutPath(file) + " <br/> ";
                        }
                        oSMTPSender.sBODY = "El día " + DateTime.Now.ToString("yyyy/MM/dd") + " a las " + DateTime.Now.ToString("HH:mm:ss") + " horas se han recibido los siguientes ficheros para el agente " + agent + ": <br/> " + sFileSet;
                        if (!Utils.IsBlankField(textEMail))
                        {
                            oSMTPSender.sBODY += " <br/> ";
                            oSMTPSender.sBODY += textEMail;

                        }

                        oSMTPSender.isHTML = true;
                        oSMTPSender.sATTACHMENTS = "";

                        oSMTPSender.sSMTPServer = sSMTPServer;
                        oSMTPSender.sRequiresAuthentication = sRequiresAuthentication;
                        oSMTPSender.sUser = sUser;
                        oSMTPSender.sPassword = sPassword;
                        oSMTPSender.sPathLog = "";
                        oSMTPSender.DoWork("", "");
                        if (!oSMTPSender.bResult)
                        {
                            Globals.GetInstance().GetLog2().TraceToFile(Log2.TYPE_WARNING, Log2.LEVEL_MEDIUM, "uveSMTPSender no pudo enviar el mensaje a " + eMailTo, "", "");
                        }
                        else
                        {
                            //Marcar para que no se vuelva a enviar el aviso hasta pasadas unas horas,
                            //-----------------------------------------------------------
                            sql = "UPDATE CfgRecepcionAgentes SET FechaAvisoRecepcionDatos=" + db.SysDate() + " WHERE IdcAgente = " + agent;
                            db.ExecuteSql(sql);
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            System.Diagnostics.EventLog.WriteEntry(Constants.SERVICE_NAME_FTP, "*** ERROR:" + e.Message);
            log.Error("", "", "*** ERROR:" + e.Message + "\n" + e.StackTrace);
            Console.WriteLine(e.StackTrace);
        }
        finally
        {
        }
    }

    /// <summary>
    /// Buscar la pareja para un fichero maestro o slave
    /// </summary>
    /// <param name="fid">clase con las características del fichero</param>
    /// <param name="firstPassProcess">array de ficheros involucrados</param>
    /// <param name="mustBeMaster">true si el fichero debe ser un master</param>
    /// <returns>el fichero encontrado o null si no lo encuentra</returns>
    private FileId SearchPair(FileId fid, ArrayList firstPassProcess, bool mustSearchMaster)
    {
      //Ejemplo:
      // Si fid.filenameWithoutPath = "Albaranes0101.txt"
      // y estamos buscando una pareja "slave", el fichero a buscar
      // es LineasAlbaranes0101.txt"
      // En fid.nomenclator.GetIdentifiedNomenclator tenemos
      // el nomenclator encontrado. A partir de aqui se pueden deducir los 
      // prefijos y sufijos con los que realizar la búsqueda. 
      FileId id = null;
      int ix = -1;
      string identifiedNomenclator = fid.nomenclator.GetIdentifiedNomenclator();
      string masterPrefix = Nomenclator.GetMaster(identifiedNomenclator);
      string slavePrefix = Nomenclator.GetSlave(identifiedNomenclator);

      string suffix = "dummy";
      string fileToFind = "";
      if(mustSearchMaster)
      {
        //Buscar un master. Partimos del fichero slave buscando el sufijo
        ix = fid.filenameWithoutPath.ToLower().IndexOf(slavePrefix.ToLower());
        if(ix!=-1)
        {
          //Lo encuentra. Ajustamos la variable fileToFind al nombre del fichero a 
          //buscar.
          //En el ejemplo, 0101.txt
          suffix = fid.filenameWithoutPath.Substring(ix + slavePrefix.Length);
          fileToFind = masterPrefix + suffix;
        }
      }
      else if(!slavePrefix.Equals(""))
      {
        //Buscar un slave. Obtenemos el sufijo del master y componemos 
        //el nombre a buscar.
        ix = fid.filenameWithoutPath.ToLower().IndexOf(masterPrefix.ToLower());
        if (ix != -1)
        {
          //En el ejemplo, 0101.txt
          suffix = fid.filenameWithoutPath.Substring(masterPrefix.Length);
          fileToFind = slavePrefix + suffix;
        }
      }
      fileToFind = fileToFind.ToLower();
      
      foreach (FileId f in firstPassProcess)
      {
        //Controlar que no se haya procesado ya...
        if (!f.isProcessed)
        {
          //Si el flag mustSearchMaster es true, debe buscar una pareja que sea master
          if (mustSearchMaster)
          {
            if(f.isMaster)
            {
              if (f.filenameWithoutPath.ToLower().Equals(fileToFind))
              {
                id = f;
                break;
              }
            }
          }
          else
          {
            //Buscar un slave
            if (!f.isMaster)
            {
              if (f.filenameWithoutPath.ToLower().Equals(fileToFind))
              {
                id = f;
                break;
              }  
            }
          }
        }
      }
      return id;
    }

    /// <summary>
    /// Añadir a la cola de mensajes
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="workingInfo">relación de sips a procesar</param>
    /// <param name="sip">sip</param>
    /// <param name="agent">agente</param>
    /// <param name="checkFormat">flag de chequeo de formato</param>
    /// <param name="log">log</param>
    private void AddToQueue(Database db, SipWorkingInfo[] workingInfo, string sipType, int priorityDelay, string agent, bool checkFormat, Log2 log)
    {
        int fileType = 0;
        int numRows = 0;
        bool formatOK = true;

        InBox inbox = new InBox(agent);

        SipCore sipWrk = new SipCore();
        
        foreach (SipWorkingInfo winfo in workingInfo)
        {
            //Encontrado...procesar y parar
            log.TraceToFile(Log2.TYPE_INFO, Log2.LEVEL_LOW, "Procesando >> " + winfo.file + ",agent=" + agent, agent, sipWrk.ConvertSipTypeToSipTypeName(sipType));

            fileType = Utils.FileType(winfo.file);
            numRows = 0;

            log.TraceToFile(Log2.TYPE_INFO, Log2.LEVEL_LOW, "Validación de formato y cálculo del número de filas >> " + winfo.file, agent, sipWrk.ConvertSipTypeToSipTypeName(sipType));

            //Validar formato y número de líneas
            switch (fileType)
            {
            case SipManager.FILE_XML:
                //Validar formato sólo en el caso de XML (configurable) 
                XMLParseResults results = CheckXML(winfo.file, sipType, agent, checkFormat, winfo.isMain);
                winfo.numRows = results.NumRows;
                winfo.formatOK = results.FormatOK;
                break;
            case SipManager.FILE_BULK:
                //Validar formato sólo en el caso de BULK
                winfo.numRows = NumberOfRows(winfo.file);
                winfo.formatOK = CheckFormatBulk(winfo.file, sipType, agent, checkFormat);
                break;
            case SipManager.FILE_DELIMITED:
            default:
                //Obtener número de filas del nomenclator principal
                winfo.numRows = NumberOfRows(winfo.file);
                winfo.formatOK = CheckFormatDelimited(winfo.file, sipType, agent, checkFormat);
                break;
            }

            if (!winfo.formatOK) formatOK = false; //determinamos si el formato es ok en conjunto, para el caso de parejas de ficheros
        }

        //Ahora copiamos al inbox si el formato es correcto
        //y también obtenemos el nombre de fichero total que se insertarà en el la cola de tareas (que puede ser uno o la pareja separada por ";")
        //y también dedicimos el número de filas, que lo determina el nomenclator secundario.
        //**************************************************************************************************************
        //En cuanto al formato correcto (debemos determinar si el formato es correcto en conjunto, principal y esclavo, y teniendo en cuenta si es xml o txt),
        //si falla el que es principal, se entiende que es erróneo, pero si falla el secundario no se 
        //entiende como erróneo. Esto es porqué nos puede llegar el secundario en un fichero aparte y si es en formato xml entonces el esquema
        //xsd no coincide, pero en realidad el formato si es correcto.
        //**************************************************************************************************************
        string inboxFileTotal = "";
        Int64 iFileSize = 0;
        Int64 iFSWrk = 0;
        int ix = 0;

        numRows = 0;
        foreach (SipWorkingInfo rel in workingInfo)
        {
            //si el formato en conjunto es ok o es un xml y el formato particular es ok, se incorpora al nombre de fichero total
            if (formatOK || (Utils.FileType(rel.file) == SipManager.FILE_XML && rel.formatOK))
            {
                //Copiar a inbox
                log.TraceToFile(Log2.TYPE_INFO, Log2.LEVEL_LOW, "Copiar a inbox >> " + rel.file, agent, sipWrk.ConvertSipTypeToSipTypeName(sipType));
                rel.inboxFile = inbox.CopyToInbox(rel.file);

                //En el caso de que haya un nomenclator principal y otro esclavo, el inbox de la cola debe contener los dos.
                if (ix > 0)
                {
                    inboxFileTotal += ";";
                }
                inboxFileTotal += rel.inboxFile;
                iFSWrk = Utils.GetFileSize(rel.file);
                iFileSize += iFSWrk;
                ix++;
            }

            //si tenemos principal y secundario nos quedamos con el número de líneas del secundario
            if (!rel.isMain)
            {
                numRows = rel.numRows;
            }
            else
            {
                if (numRows == 0)
                {
                    numRows = rel.numRows;
                }
            }
        }

        ////Crear la tarea
        string sTimeStamp = "";
        string sLimitNumRows = FileSizeDelay("limitnumrows");
        string sLimitSize = FileSizeDelay("limitsize");
        string sDelay = FileSizeDelay("delay");
        string sSlotIni = FileSizeDelay("slotini");
        string sSlotEnd = FileSizeDelay("slotend");

        //Si el nombre de fichero total tiene valor, significa que podemos insertar en la cola de tareas
        if (!Utils.IsBlankField(inboxFileTotal))
        {
            //Crear la tarea
            log.TraceToFile(Log2.TYPE_INFO, Log2.LEVEL_LOW, "Creación de tarea", agent, sipWrk.ConvertSipTypeToSipTypeName(sipType));

            if ((!Utils.IsBlankField(sLimitNumRows) && numRows > Int64.Parse(sLimitNumRows)) || (!Utils.IsBlankField(sLimitSize) && iFileSize > Int64.Parse(sLimitSize)))
            {
                sTimeStamp = Utils.GetTimeStamp(db, sDelay, sSlotIni, sSlotEnd);
            }
            SipQueue queue = new SipQueue();
            queue.AddToQueue(db, agent, sipType, inboxFileTotal, SipQueue.STATUS_CREATING, numRows, sTimeStamp, priorityDelay);
        }

        //Finalmente, borrar ficheros de entrada o los renombramos a .bad.
        foreach (SipWorkingInfo rel in workingInfo)
        {
            if (formatOK || (Utils.FileType(rel.file) == SipManager.FILE_XML && rel.formatOK))
            {
                //Borrar fichero
                log.TraceToFile(Log2.TYPE_INFO, Log2.LEVEL_LOW, "Borrando >> " + rel.file + ",agent=" + agent, agent, sipWrk.ConvertSipTypeToSipTypeName(sipType));
                if (File.Exists(rel.file)) File.Delete(rel.file);
            }
            else
            {
                //Copiar a fichero .bad
                if (File.Exists(rel.file))
                {
                    if (!rel.formatOK) log.TraceToFile(Log2.TYPE_ERROR, Log2.LEVEL_CRITICAL, "Error en validación formato >> " + rel.file, agent, sipWrk.ConvertSipTypeToSipTypeName(sipType));
                    if (File.Exists(rel.file + ".bad")) File.Delete(rel.file + ".bad");
                    File.Move(rel.file, rel.file + ".bad");
                }
                else
                {
                    if (!rel.formatOK) log.TraceToFile(Log2.TYPE_ERROR, Log2.LEVEL_CRITICAL, "Error en validación formato >> (EL FICHERO NO EXISTE) " + rel.file, agent, sipWrk.ConvertSipTypeToSipTypeName(sipType));
                }
            }
            log.TraceToFile(Log2.TYPE_INFO, Log2.LEVEL_LOW, rel.file + " procesado.", agent, sipWrk.ConvertSipTypeToSipTypeName(sipType));
        }
    }

    /// <summary>
    /// Añadir a la cola de mensajes
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="agent">agente</param>
    /// <param name="log">log</param>
    private void UpdateStatusInQueue(Database db, string agent, Log2 log)
    {
        //Actualizar el estado de las tareas
        log.TraceToFile(Log2.TYPE_INFO, Log2.LEVEL_LOW, "Actualizando el estado de las tareas", agent, "");
        SipQueue queue = new SipQueue();
        queue.Pendiente(db, agent);
    }

  }
}
