using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using ConnectaLib;

namespace uveIntegrator
{
  /// <summary>
  /// Clase que gestiona las peticiones de la cola de Sip's y la invocación desde línea de comandos
  /// </summary>
  public class uveIntegrator
  {
    private bool stopAuto = false;
    private bool stopAutoError = false;  //Flag de parada del proceso (para paradas forzadas por error de programa)
    private bool stopAutoForced = false;  //Flag de parada del proceso (para paradas forzadas por error de programa)
    private TcpListener myListener = null;

    /// <summary>
    /// Constructor
    /// </summary>
    public uveIntegrator() 
    { 
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
            string siptype = null;
            string sipagent = null;
            string sipfilename = null;
            bool autosettask = false;
            string s = "";
            for (int i = 0; i < args.Length; i++)
            {
                s = args[i].ToLower();
                if (s.StartsWith("-siptype="))
                    siptype = args[i].Substring("-siptype=".Length);
                else if (s.StartsWith("-sipagent="))
                    sipagent = args[i].Substring("-sipagent=".Length);
                else if (s.StartsWith("-sipfilename="))
                    sipfilename = args[i].Substring("-sipfilename=".Length);
                else if (s.StartsWith("-autosettask"))
                    autosettask = true;
            }
            if (siptype == null || sipagent == null)
                usage();
            else if (autosettask)
            {
                //Obtener instancia de globals
                g = Globals.GetInstance();

                runAutoTask(g, siptype, sipagent, sipfilename, args);

                g.Close();
                g = null;
            }
            else
            {
                //Obtener instancia de globals
                g = Globals.GetInstance();

                //Crear un Sip y lanzar el proceso
                SipCore sip = new SipCore();
                sip.Start(g, siptype, sipagent, sipfilename, args);

                g.Close();
                g = null;
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

    public void runAutoTask(Globals g, string siptype, string agent, string filename, string[] args)
    {
        Database db = g.GetDatabase();
        Log2 log = g.GetLog2();
        SipCore sipWrk = new SipCore();
        
        int numRows = 0;

        //Inicio creación automática de tarea
        log.TraceToFile(Log2.TYPE_INFO, Log2.LEVEL_LOW, "Creación automática de tarea " + siptype + (Utils.IsBlankField(filename) ? "" : ", fichero=" + filename) + ", agent=" + agent, agent, sipWrk.ConvertSipTypeToSipTypeName(siptype));

        SipQueue queue = new SipQueue();
        if (siptype.ToLower().StartsWith("sipout"))
        {
            string sipFileFormat = SipOut.FORMAT_XML;
            string sipDealer = "";
            string sipFromDate = "";
            string sipToDate = "";
            string sipFilter = null;
            string sqlId = "";
            string sipFieldSeparator = "";

            bool sipExcludeLastFieldSeparator = false;
            bool sipIncludeHeaders = false;
            string sipEncoding = "";
            string s = "";
            for (int i = 0; i < args.Length; i++)
            {
                s = args[i].ToLower();
                if (s.StartsWith("-sipfileformat="))
                    sipFileFormat = args[i].Substring("-sipfileformat=".Length);
                else if (s.StartsWith("-sipdealer="))
                    sipDealer = args[i].Substring("-sipdealer=".Length).Trim();
                else if (s.StartsWith("-sipfromdate="))
                    sipFromDate = args[i].Substring("-sipfromdate=".Length);
                else if (s.StartsWith("-siptodate="))
                    sipToDate = args[i].Substring("-siptodate=".Length);
                else if (s.StartsWith("-sipfilter="))
                    sipFilter = args[i].Substring("-sipfilter=".Length);
                else if (s.StartsWith("-sipsqlid="))
                    sqlId = args[i].Substring("-sipsqlid=".Length);
                else if (s.StartsWith("-sipfieldseparator="))
                    sipFieldSeparator = args[i].Substring("-sipfieldseparator=".Length);
                else if (s.StartsWith("-sipexcludelastfieldseparator"))
                    sipExcludeLastFieldSeparator = true;
                else if (s.StartsWith("-sipincludeheaders"))
                    sipIncludeHeaders = true;
                else if (s.StartsWith("-sipencoding="))
                    sipEncoding = args[i].Substring("-sipencoding=".Length);
            }
            queue.AddToQueue_sipOut(db, agent, siptype, filename, sipDealer, sipFromDate, sipToDate, sipFileFormat.ToLower(), sipFilter, sqlId, sipFieldSeparator, sipExcludeLastFieldSeparator, sipIncludeHeaders, sipEncoding, SipQueue.STATUS_PENDING);
        }
        else if (siptype.ToLower().StartsWith("sipupd"))
        {
            string sipToDate = "";
            string sipFromDate = "";
            string sipDealer = "";
            string sipProvider = "";
            string sipFilter = null;
            string sipClassifier = "";
            string sipLockCode = "";
            string sipProductCode = "";
            string sipDataSource = "";
            string sqlId = "";
            string s = "";
            for (int i = 0; i < args.Length; i++)
            {
                s = args[i].ToLower();
                if (s.StartsWith("-sipdealer="))
                    sipDealer = args[i].Substring("-sipdealer=".Length).Trim();
                else if (s.StartsWith("-sipprovider="))
                    sipProvider = args[i].Substring("-sipprovider=".Length).Trim();
                else if (s.StartsWith("-sipfromdate="))
                    sipFromDate = args[i].Substring("-sipfromdate=".Length);
                else if (s.StartsWith("-siptodate="))
                    sipToDate = args[i].Substring("-siptodate=".Length);
                else if (s.StartsWith("-sipfilter="))
                    sipFilter = args[i].Substring("-sipfilter=".Length);
                else if (s.StartsWith("-sipsqlid="))
                    sqlId = args[i].Substring("-sipsqlid=".Length);
                else if (s.StartsWith("-sipclassifier="))
                    sipClassifier = args[i].Substring("-sipclassifier=".Length);
                else if (s.StartsWith("-siplockcode="))
                    sipLockCode = args[i].Substring("-siplockcode=".Length);
                else if (s.StartsWith("-sipproductcode="))
                    sipProductCode = args[i].Substring("-sipproductcode=".Length);
                else if (s.StartsWith("-sipdatasource="))
                    sipDataSource = args[i].Substring("-sipdatasource=".Length);                
            }
            queue.AddToQueue_sipUpd(db, agent, siptype, sipFromDate, sipToDate, sipDealer, sipProvider, sipFilter, sqlId, sipClassifier, sipLockCode, sipProductCode, sipDataSource, SipQueue.STATUS_PENDING);
        }
        else if (siptype.ToLower().StartsWith("sipdel"))
        {
            string sipToDate = "";
            string sipFromDate = "";
            string sipToUpdDate = "";
            string sipFromUpdDate = "";
            string sipToInsDate = "";
            string sipFromInsDate = "";
            string sipDealer = "";
            string sipProvider = "";
            string sipUser = "";
            string sipFilter = null;
            string sqlId = "";
            string s = "";
            for (int i = 0; i < args.Length; i++)
            {
                s = args[i].ToLower();
                if (s.StartsWith("-sipdealer="))
                    sipDealer = args[i].Substring("-sipdealer=".Length).Trim();
                else if (s.StartsWith("-sipprovider="))
                    sipProvider = args[i].Substring("-sipprovider=".Length).Trim();
                else if (s.StartsWith("-sipuser="))
                    sipUser = args[i].Substring("-sipuser=".Length).Trim();
                else if (s.StartsWith("-sipfromdate="))
                    sipFromDate = args[i].Substring("-sipfromdate=".Length);
                else if (s.StartsWith("-siptodate="))
                    sipToDate = args[i].Substring("-siptodate=".Length);
                else if (s.StartsWith("-sipfilter="))
                    sipFilter = args[i].Substring("-sipfilter=".Length);
                else if (s.StartsWith("-sipsqlid="))
                    sqlId = args[i].Substring("-sipsqlid=".Length);
                else if (s.StartsWith("-sipfromupddate="))
                    sipFromUpdDate = args[i].Substring("-sipfromupddate=".Length);
                else if (s.StartsWith("-siptoupddate="))
                    sipToUpdDate = args[i].Substring("-siptoupddate=".Length);
                else if (s.StartsWith("-sipfrominsdate="))
                    sipFromInsDate = args[i].Substring("-sipfrominsdate=".Length);
                else if (s.StartsWith("-siptoinsdate="))
                    sipToInsDate = args[i].Substring("-siptoinsdate=".Length);                
            }
            queue.AddToQueue_sipDel(db, agent, siptype, sipFromDate, sipToDate, sipDealer, sipProvider, sipUser, sipFilter, sqlId, sipFromUpdDate, sipToUpdDate, sipFromInsDate, sipToInsDate, SipQueue.STATUS_PENDING);
        }
        else if (siptype.ToLower().StartsWith("sipexe"))
        {
            string sipExeId= "";
            string sipProcessName= "";
            string sipPath = "";
            string sipParams = "";
            string s = "";
            for (int i = 0; i < args.Length; i++)
            {
                s = args[i].ToLower();
                if (s.StartsWith("-sipexeid="))
                    sipExeId = args[i].Substring("-sipexeid=".Length).Trim();
                else if (s.StartsWith("-sipprocessname="))
                    sipProcessName = args[i].Substring("-sipprocessname=".Length).Trim();
                else if (s.StartsWith("-sippath="))
                    sipPath = args[i].Substring("-sippath=".Length);
                else if (s.StartsWith("-sipparams="))
                    sipParams = args[i].Substring("-sipparams=".Length);
            }
            queue.AddToQueue_sipExe(db, agent, siptype, sipExeId, sipProcessName, sipPath, sipParams, SipQueue.STATUS_PENDING);
        }
        else if (siptype.ToLower().StartsWith("sipin"))
        {
            queue.AddToQueue(db, agent, siptype, filename, SipQueue.STATUS_PENDING, numRows, "", 0);
        }
        else
        {
            throw new Exception("Tipo de SIP incorrecto >> " + siptype);
        }

    }

    /// <summary>
    /// Arrancar en modo automático. Normalmente esto se utilzará desde un servicio de Windows
    /// </summary>
    public void runAuto(string[] args) 
    {
        //Ver http://www.codeproject.com/KB/system/WindowsService.aspx

        //Procesar la cola de SIP's...
        Globals g = null;
        DbDataReader rs = null;

        string errorMsg = "";

        try
        {
            string iniFile = null;
            if (args != null && args.Length > 0)
            {
                foreach (string s in args)
                {
                    iniFile += s + " ";
                }
            }

            //Obtener instancia de globals
            g = Globals.GetInstance(iniFile);

            ////Arrancar el proceso que permitirá monitorizar el sistema
            ////Lo protegemos con un try/catch porque no es algo fundamental
            //try
            //{
            //  int port = Int32.Parse(IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_ADMINPORT, IniManager.GetIniFile(), "5050"));
            //  g.GetLog().TraceToFile(Log.TYPE_INFO, Log.LEVEL_HIGH, "Inicio del servicio de monitorización en el puerto :" + port);

            //  IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            //  myListener = new TcpListener(localAddr, port);
            //  myListener.Start();

            //  //start the thread which calls the method 'StartListen'

            //  Thread th = new Thread(new ThreadStart(StartListen));
            //  th.Start();
            //}
            //catch (Exception e)
            //{
            //  g.GetLog().TraceToFile(Log.TYPE_ERROR, Log.LEVEL_HIGH, "Inicio del servicio de monitorización con errores :"+e.Message); 
            //}

            int sleepTime = Int32.Parse(IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_AUTOMODESLEEPTIME, IniManager.GetIniFile(), "5"));
            int nCont = 1;
            bool bProc = false;
            int nErrorCount = 0;

            g.GetLog2().TraceToFile(Log2.TYPE_INFO, Log2.LEVEL_LOW, "Inicio de uveIntegrator en modo automático...(delay=" + sleepTime + " segundos).", "", "");            
            System.Diagnostics.EventLog.WriteEntry(Constants.SERVICE_NAME_INT, "Inicio de uveIntegrator en modo automático...");
        
            sleepTime = sleepTime * 1000;    //pasamos a milisegundos...

            //Crear un Sip y lanzar el proceso
            string msgId = "";
            string msgData = "";
            string sipTypeName = "";
            string agent = "";
            Database db = g.GetDatabase();
            SipCore sip = new SipCore();
            string strSql = "";

            while (!stopAuto)
            {
                if (!bProc) nCont = 1;
                if (nCont == 1)
                    //Solo procesamos TODAS las tareas cada 2 vueltas
                    strSql = SipQueue.GetSqlPendings();
                else
                    //Si no es la vuelta 1 del bucle, procesamos solo las tareas que no son sipIn
                    strSql = SipQueue.GetSqlPendings_NOTsipIn();
                nCont++;
                if (nCont == 3)
                    nCont = 1;

                try
                {
                    rs = db.GetDataReader(strSql);
                    
                    bProc = false;
                    while (rs.Read())
                    {
                        nErrorCount = 0;
                        bProc = true;   //la idea es que si hay alguna tarea pendiente (ha entrado) ponemos el flag a true para que continue la secuencia
                        //de nCont de 1 a 5, si no ha entrado se inicia la secuencia a 1 y de esta manera aseguramos que si no hay sipOuts o sipUpds o sipDels o sipExes,
                        //los sipIn se pueden ir procesando con la frecuencia de los sipOut.

                        msgId = db.GetFieldValue(rs, 0);
                        msgData = db.GetFieldValue(rs, 1);
                        sipTypeName = db.GetFieldValue(rs, 2);
                        agent = db.GetFieldValue(rs, 3);

                        //Ajustar el identificador global del mensaje
                        //En la clase de log, se recuperará para añadir los registros 
                        //con este identificador.
                        g.SetTaskId(msgId);

                        sip.StartAuto(msgId, msgData, sipTypeName, agent);

                        //Dar otra oportunidad de salir del proceso...
                        if (stopAuto)
                            break;
                    }
                    if (rs != null)
                        rs.Close();
                    rs = null;

                    //Delay...
                    g.GetLog2().TraceToFile(Log2.TYPE_INFO, Log2.LEVEL_LOW, "Esperando para volver a iniciar tarea pendiente...(delay=" + sleepTime / 1000 + " segundos).", "", "", true);
                    System.Threading.Thread.Sleep(sleepTime);
                }
                catch (Exception e)
                {
                    errorMsg = "*** ERROR:" + e.Message;
                    if (e.InnerException != null) errorMsg += " (" + e.InnerException.Message + ").";
                    errorMsg += "\n" + e.StackTrace;
                    if (nErrorCount > 4)
                    {
                        stopAuto = true;
                        stopAutoError = true;
                    }
                    else
                    {
                        errorMsg += "\n" + "Se realizará otro intento...aun no se aborta uveIntegrator.";
                    }
                    nErrorCount++;
                    //if (g != null) g.GetLog2().TraceToFile(Log.TYPE_ERROR, Log.LEVEL_CRITICAL, errorMsg, "", "");
                    if (g != null) g.GetLog2().Error("", "", errorMsg);
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
                finally
                {
                    if (stopAutoError)
                    {
                        stopAutoForced = true;
                    }
                    if (rs != null)
                        rs.Close();
                    rs = null;
                }
            }
        }
        catch (Exception e)
        {
            System.Diagnostics.EventLog.WriteEntry(Constants.SERVICE_NAME_INT, "*** ERROR:" + e.Message);
            errorMsg = "*** ERROR:" + e.Message;
            if (e.InnerException != null) errorMsg += " (" + e.InnerException.Message + ").";
            errorMsg += "\n" + e.StackTrace;
            //if (g != null) g.GetLog2().TraceToFile(Log.TYPE_ERROR, Log.LEVEL_CRITICAL, errorMsg, "", "");
            if (g != null) g.GetLog2().Error("", "", errorMsg);
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
            stopAutoForced = true;
        }
        finally
        {
            System.Diagnostics.EventLog.WriteEntry(Constants.SERVICE_NAME_INT, "Cierre uveIntegrator en modo automático...");
            if (g != null) g.GetLog2().TraceToFile(Log2.TYPE_INFO, Log2.LEVEL_LOW, "Cierre uveIntegrator en modo automático...", "", "");
            //Si se ha forzado la salida del bucle, también enviamos un mensaje al administrador.
            if (stopAutoForced)
            {
                SendStopAutoEMailAlert(errorMsg);
            }
            //Cerrar todo...
            if (rs != null)
                rs.Close();
            if (g != null)
                g.Close();
        }
    }

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
        oSMTPSender.sSUBJECT = "ALERTA ConnectA: El servicio uveIntegrator ha terminado.";

        oSMTPSender.sBODY = "El día " + DateTime.Now.ToString("yyyy/MM/dd") + " a las " + DateTime.Now.ToString("HH:mm:ss") + " horas el servicio uveIntegrator ha terminado su ejecución en modo automático." +
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
    /// Detener el modo automático. Normalmente esto se utilzará desde un servicio de Windows
    /// </summary>
    public void StopAuto()
    {
      stopAuto = true;
    }

    /// <summary>
    /// Escucha peticiones del socket de administración
    /// </summary>
    public void StartListen()
    {
      Globals g = Globals.GetInstance();
      string sErrorMessage = null;
      while (!stopAuto)
      {
        //Accept a new connection
        Socket mySocket = myListener.AcceptSocket();
        try 
        {
          sErrorMessage = null;
          if (mySocket.Connected)
          {
            //make a byte array and receive data from the client 
            Byte[] bReceive = new Byte[1024];
            int i = mySocket.Receive(bReceive, bReceive.Length, 0);

            //Convert Byte to String
            string sBuffer = Encoding.ASCII.GetString(bReceive);

            //At present we will only deal with GET type
            if (sBuffer.Substring(0, 3) != "GET")
            {
              Console.WriteLine("Only Get Method is supported..");
              mySocket.Close();
              return;
            }

            // Look for HTTP request
            int iStartPos = sBuffer.IndexOf("HTTP", 1);

            // Get the HTTP text and version e.g. it will return "HTTP/1.1"
            string sHttpVersion = sBuffer.Substring(iStartPos, 8);

            // Extract the Requested Type and Requested file/directory
            string sRequest = sBuffer.Substring(0, iStartPos - 1);

            //Replace backslash with Forward Slash, if Any
            sRequest.Replace("\\", "/");

            int ix = sRequest.ToLower().IndexOf("cmd=");
            sErrorMessage = "Comando desconocido";
            if (ix != -1)
            {
              string cmd = sRequest.Substring(ix + "cmd=".Length);
              cmd.ToLower();
              if (cmd.Equals("status"))
              {
                string msg = GetStatusAsHTML(g);
                SendHeader(sHttpVersion, "text/html", msg.Length, " 200 OK", ref mySocket);

                SendToBrowser(msg, ref mySocket);
                sErrorMessage = null;
              }
            }
            g.GetLog2().TraceToFile(Log2.TYPE_INFO, Log2.LEVEL_LOW, "Recibido " + sRequest + ".", "", "");
            if (sErrorMessage != null)
            {
              SendHeader(sHttpVersion, "", sErrorMessage.Length, " 404 Not Found", ref mySocket);

              //Send to the browser
              SendToBrowser(sErrorMessage, ref mySocket);
            }
          }
        }
        finally 
        {
          mySocket.Close();
        }
      }
    }

    /// <summary>
    /// Envía cabecera de respuesta al servidor
    /// </summary>
    /// <param name="sHttpVersion"></param>
    /// <param name="sMIMEHeader"></param>
    /// <param name="iTotBytes"></param>
    /// <param name="sStatusCode"></param>
    /// <param name="mySocket"></param>
    public void SendHeader(string sHttpVersion, string sMIMEHeader, int iTotBytes, string sStatusCode, ref Socket mySocket)
    {
      String sBuffer = "";

      // if Mime type is not provided set default to text/html

      if (sMIMEHeader.Length == 0)
      {
        sMIMEHeader = "text/html";  // Default Mime Type is text/html

      }

      sBuffer = sBuffer + sHttpVersion + sStatusCode + "\r\n";
      sBuffer = sBuffer + "Server: cx1193719-b\r\n";
      sBuffer = sBuffer + "Content-Type: " + sMIMEHeader + "\r\n";
      sBuffer = sBuffer + "Accept-Ranges: bytes\r\n";
      sBuffer = sBuffer + "Content-Length: " + iTotBytes + "\r\n\r\n";

      Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);

      SendToBrowser(bSendData, ref mySocket);
    }

    /// <summary>
    /// Envía al browser
    /// </summary>
    /// <param name="sData">datos</param>
    /// <param name="mySocket">socket</param>
    public void SendToBrowser(string sData, ref Socket mySocket)
    {
        SendToBrowser(Encoding.ASCII.GetBytes(sData), ref mySocket);
    }

    /// <summary>
    /// Envía al browser
    /// </summary>
    /// <param name="bSendData"></param>
    /// <param name="mySocket"></param>
    public void SendToBrowser(Byte[] bSendData, ref Socket mySocket)
    {
        int numBytes = 0;
        try
        {
            if (mySocket.Connected)
            {
                if ((numBytes = mySocket.Send(bSendData, bSendData.Length, 0)) == -1) Console.WriteLine("Socket Error cannot Send Packet");
            }
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Mostrar info de uso
    /// </summary>
    void usage()
    {
        System.Console.WriteLine("Parámetros incorrectos. Use -autosettask -siptype=<type> -sipagent=<agent> -sipFileName=[<filename>]");
    }

    /// <summary>
    /// Obtener estado de mensajes en formato HTML
    /// </summary>
    /// <param name="g">parámetros globales</param>
    /// <returns>estado en formato HTML</returns>
    private string GetStatusAsHTML(Globals g) 
    {
      string strHTML = "";
      Database db = g.GetDatabase();
      DbDataReader rs = null;
      try
      {
        HTMLTemplateManager t = new HTMLTemplateManager();
        string templateDir = IniManager.INIGetKeyValue(IniManager.CFG_SECTION, IniManager.CFG_TEMPLATEDIR, IniManager.GetIniFile(), "");
        string template = templateDir+"\\status.template.html";
        if (System.IO.File.Exists(template))
        {
          rs = db.GetDataReader(SipQueue.GetSqlAll());

          t.LoadTemplate(template);
          string strBase = template + ".";
          strHTML = t.GetTemplate(strBase);
          string linea = "";
          StringBuilder status = new StringBuilder("");
          string strLin = t.GetTemplate(strBase + "LINEAS");
          status.Append(strLin);

          while(rs.Read())
          {
            linea = t.GetTemplate(strBase + "LINEA");
            linea = t.Replace(linea, "|ID|", db.GetFieldValue(rs,0));
            linea = t.Replace(linea, "|DATOS|", db.GetFieldValue(rs, 1));
            linea = t.Replace(linea, "|ORQUESTACION|", db.GetFieldValue(rs, 2));
            linea = t.Replace(linea, "|AGENTE|", db.GetFieldValue(rs, 3));
            linea = t.Replace(linea, "|ESTADO|", db.GetFieldValue(rs, 4));
            linea = t.Replace(linea, "|FECHACREA|", db.GetFieldValue(rs, 5));
            linea = t.Replace(linea, "|FECHAUPD|", db.GetFieldValue(rs, 6));
            linea = t.Replace(linea, "|FECHASTARTPROC|", db.GetFieldValue(rs, 7));
            linea = t.Replace(linea, "|ROWS|", db.GetFieldValue(rs, 8));
            status.Append(linea);
          }
          status.Append(t.GetTemplate(strBase + "FINLINEAS"));
          strHTML = t.Replace(strHTML, "|MENSAJES|", status.ToString());
        }
        else
          strHTML = "Fichero " + template + " no existe.";
      }
      finally
      {
        if (rs != null)
          rs.Close();
      } 
      return strHTML;
    }
  }
}
