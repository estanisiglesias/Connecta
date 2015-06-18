using System;
using System.IO;
using System.Collections;
using System.Net;
using System.Net.Mail;

namespace ConnectaLib
{
	/// <summary>
	/// Clase para efectuar el envío SMTP de mensajes.
	/// </summary>
	public class SMTPSender
	{
        public string sFROM = "";
        public string sTO = "";
        public string sCC = "";
        public string sBCC = "";
        public string sSUBJECT = "";
        public string sBODY = "";
        public string sATTACHMENTS = "";
        public bool isHTML = false;

        public string sSMTPServer = "";
        public string sRequiresAuthentication = "";
        public string sUser = "";
        public string sPassword = "";
        public string sPathLog = "";

        public bool bResult = false;

        /// <summary>
		/// Do the job
		/// </summary>
        public void DoWork(string pAgent, string pSipTypeName)
		{
            ArrayList aTO = new ArrayList(10);
            ArrayList aCC = new ArrayList(10);
            ArrayList aBCC = new ArrayList(10);
            ArrayList aATTACHMENTS = new ArrayList(10);

            bool bUseDefaultCredentials = false;

            bResult = false;

            //if (Utils.IsBlankField(pAgent) || Utils.IsBlankField(pSipTypeName))
            //    Globals.GetInstance().GetLog().TraceToFile(Log.TYPE_INFO, Log.LEVEL_LOW, "uveSMTPSender inicia envío mensaje");
            //else
            //    Globals.GetInstance().GetLog().Info(pAgent, pSipTypeName, "uveSMTPSender inicia envío mensaje");
            Globals.GetInstance().GetLog2().Info(pAgent, pSipTypeName, "uveSMTPSender inicia envío mensaje");
            
            //Comprobamos que esté informado el parámetro FROM
            if (String.IsNullOrEmpty(sFROM))
            {
                //if (Utils.IsBlankField(pAgent) || Utils.IsBlankField(pSipTypeName))
                //    Globals.GetInstance().GetLog().TraceToFile(Log.TYPE_DETAILED_ERROR, Log.LEVEL_CRITICAL,"uveSMTPSender error, parámetro FROM no informado");
                //else
                //    Globals.GetInstance().GetLog().DetailedError(pAgent, pSipTypeName, "uveSMTPSender error, parámetro FROM no informado");
                Globals.GetInstance().GetLog2().Error(pAgent, pSipTypeName, "uveSMTPSender error, parámetro FROM no informado");
                return;
            }
            //Comprobamos que esté informado el parámetro TO y partimos los destinos en un array
            if (!String.IsNullOrEmpty(sTO))
            {
                StringTokenizer stTO = new StringTokenizer(sTO, ";");
                while (stTO.HasMoreTokens())
                {
                    aTO.Add(stTO.NextToken());
                }
            }
            else
            {
                //if (Utils.IsBlankField(pAgent) || Utils.IsBlankField(pSipTypeName))
                //    Globals.GetInstance().GetLog().TraceToFile(Log.TYPE_DETAILED_ERROR, Log.LEVEL_CRITICAL, "uveSMTPSender error, parámetro TO no informado");
                //else
                //    Globals.GetInstance().GetLog().DetailedError(pAgent, pSipTypeName, "uveSMTPSender error, parámetro TO no informado");
                Globals.GetInstance().GetLog2().Error(pAgent, pSipTypeName, "uveSMTPSender error, parámetro TO no informado");
                return;
            }
            //Partimos los destinos CC en un array
            if (!String.IsNullOrEmpty(sCC))
            {
                StringTokenizer stCC = new StringTokenizer(sCC, ";");
                while (stCC.HasMoreTokens())
                {
                    aCC.Add(stCC.NextToken());
                }
            }
            //Partimos los destinos BCC en un array
            if (!String.IsNullOrEmpty(sBCC))
            {
                StringTokenizer stBCC = new StringTokenizer(sBCC, ";");
                while (stBCC.HasMoreTokens())
                {
                    aBCC.Add(stBCC.NextToken());
                }
            }
            //Comprobamos que esté informado el parámetro SUBJECT
            if (String.IsNullOrEmpty(sSUBJECT))
            {
                //if (Utils.IsBlankField(pAgent) || Utils.IsBlankField(pSipTypeName))
                //    Globals.GetInstance().GetLog().TraceToFile(Log.TYPE_DETAILED_ERROR, Log.LEVEL_CRITICAL, "uveSMTPSender error, parámetro SUBJECT no informado");
                //else
                //    Globals.GetInstance().GetLog().DetailedError(pAgent, pSipTypeName, "uveSMTPSender error, parámetro SUBJECT no informado");
                Globals.GetInstance().GetLog2().Error(pAgent, pSipTypeName, "uveSMTPSender error, parámetro SUBJECT no informado");
                return;
            }
            //Comprobamos que esté informado el parámetro BODY
            if (String.IsNullOrEmpty(sBODY))
            {
                //if (Utils.IsBlankField(pAgent) || Utils.IsBlankField(pSipTypeName))
                //    Globals.GetInstance().GetLog().TraceToFile(Log.TYPE_DETAILED_ERROR, Log.LEVEL_CRITICAL, "uveSMTPSender error, parámetro BODY no informado");
                //else
                //    Globals.GetInstance().GetLog().DetailedError(pAgent, pSipTypeName, "uveSMTPSender error, parámetro BODY no informado");
                Globals.GetInstance().GetLog2().Error(pAgent, pSipTypeName, "uveSMTPSender error, parámetro BODY no informado");
                return;
            }
            //Partimos los adjuntos en un array
            if (!String.IsNullOrEmpty(sATTACHMENTS))
            {
                string sToken = "";
                StringTokenizer stATTACHMENTS = new StringTokenizer(sATTACHMENTS, ";");
                while (stATTACHMENTS.HasMoreTokens())
                {
                    sToken = stATTACHMENTS.NextToken();
                    if (System.IO.File.Exists(sToken))
                    {
                        aATTACHMENTS.Add(sToken);
                    }
                    else
                    {
                        //if (Utils.IsBlankField(pAgent) || Utils.IsBlankField(pSipTypeName))
                        //    Globals.GetInstance().GetLog().TraceToFile(Log.TYPE_DETAILED_ERROR, Log.LEVEL_CRITICAL, "uveSMTPSender error, fichero adjunto " + sToken + " no existe");
                        //else
                        //    Globals.GetInstance().GetLog().DetailedError(pAgent, pSipTypeName, "uveSMTPSender error, fichero adjunto " + sToken + " no existe");
                        Globals.GetInstance().GetLog2().Error(pAgent, pSipTypeName, "uveSMTPSender error, fichero adjunto " + sToken + " no existe");
                        return;
                    }
                }
            }

            // Create a message and set up the recipients.
            MailMessage message = new MailMessage();
            message.BodyEncoding = System.Text.Encoding.UTF8;
            message.From = new MailAddress(sFROM);
            foreach (string sWrk in aTO)
            {
                if (!String.IsNullOrEmpty(sWrk))
                    message.To.Add(sWrk);
            }
            foreach (string sWrk in aCC)
            {
                if (!String.IsNullOrEmpty(sWrk))
                    message.CC.Add(sWrk);
            }
            foreach (string sWrk in aBCC)
            {
                if (!String.IsNullOrEmpty(sWrk))
                    message.Bcc.Add(sWrk);
            }
            message.Subject = sSUBJECT;
            message.IsBodyHtml = isHTML;
            message.Body = sBODY;

            // Create  the file attachments for this e-mail message.
            foreach (string file in aATTACHMENTS)
            {
                if (!String.IsNullOrEmpty(file))
                {
                    Attachment data = new Attachment(file);
                    message.Attachments.Add(data);
                }
            }

            // Tratar y crear si hace falta las credenciales
            NetworkCredential myCred = new NetworkCredential("", "", "");
            if (sRequiresAuthentication.ToLower() == "false")
            {
                bUseDefaultCredentials = true;
            }
            else
            {
                bUseDefaultCredentials = false;
                myCred.UserName = sUser;
                myCred.Password = sPassword;
            }

            //Send the message.
            SmtpClient client = new SmtpClient(sSMTPServer);
            client.UseDefaultCredentials = bUseDefaultCredentials;
            if (!bUseDefaultCredentials)
                client.Credentials = myCred;

            try
            {                                
                client.Send(message);                

                //if (Utils.IsBlankField(pAgent) || Utils.IsBlankField(pSipTypeName))
                //    Globals.GetInstance().GetLog().TraceToFile(Log.TYPE_INFO, Log.LEVEL_LOW, "uveSMTPSender finaliza satisfactoriamente el envío del mensaje:" + " TO: " + sTO + " SUBJECT: " + sSUBJECT );
                //else
                //    Globals.GetInstance().GetLog().Info(pAgent, pSipTypeName, "uveSMTPSender finaliza satisfactoriamente el envío del mensaje:" + " TO: " + sTO + " SUBJECT: " + sSUBJECT);
                Globals.GetInstance().GetLog2().Info(pAgent, pSipTypeName, "uveSMTPSender finaliza satisfactoriamente el envío del mensaje:" + " TO: " + sTO + " SUBJECT: " + sSUBJECT);

                bResult = true;
            }
            catch (SmtpFailedRecipientsException ex)
            {
                for (int i = 0; i < ex.InnerExceptions.Length; i++)
                {
                    SmtpStatusCode status = ex.InnerExceptions[i].StatusCode;
                    if (status == SmtpStatusCode.MailboxBusy || status == SmtpStatusCode.MailboxUnavailable)
                    {
                        //if (Utils.IsBlankField(pAgent) || Utils.IsBlankField(pSipTypeName))
                        //    Globals.GetInstance().GetLog().TraceToFile(Log.TYPE_WARNING, Log.LEVEL_MEDIUM, "uveSMTPSender: Entrega del mensaje ha fallado. Reintento en 5 segundos.");
                        //else
                        //    Globals.GetInstance().GetLog().Warning(pAgent, pSipTypeName, "uveSMTPSender: Entrega del mensaje ha fallado. Reintento en 5 segundos.");
                        Globals.GetInstance().GetLog2().Error(pAgent, pSipTypeName, "uveSMTPSender: Entrega del mensaje ha fallado. Reintento en 5 segundos.");
                        
                        System.Threading.Thread.Sleep(5000);
                        
                        client.Send(message);

                        //if (Utils.IsBlankField(pAgent) || Utils.IsBlankField(pSipTypeName))
                        //    Globals.GetInstance().GetLog().TraceToFile(Log.TYPE_INFO, Log.LEVEL_LOW, "uveSMTPSender finaliza satisfactoriamente el envío del mensaje:" + " TO: " + sTO + " SUBJECT: " + sSUBJECT );
                        //else
                        //    Globals.GetInstance().GetLog().Info(pAgent, pSipTypeName, "uveSMTPSender finaliza satisfactoriamente el envío del mensaje:" + " TO: " + sTO + " SUBJECT: " + sSUBJECT);
                        Globals.GetInstance().GetLog2().Info(pAgent, pSipTypeName, "uveSMTPSender finaliza satisfactoriamente el envío del mensaje:" + " TO: " + sTO + " SUBJECT: " + sSUBJECT);

                        bResult = true;
                    }
                    else
                    {
                        //if (Utils.IsBlankField(pAgent) || Utils.IsBlankField(pSipTypeName))
                        //    Globals.GetInstance().GetLog().TraceToFile(Log.TYPE_DETAILED_ERROR, Log.LEVEL_CRITICAL, "uveSMTPSender: Fallo de la entrega del mensaje a " + ex.InnerExceptions[i].FailedRecipient);
                        //else
                        //    Globals.GetInstance().GetLog().DetailedError(pAgent, pSipTypeName, "uveSMTPSender: Fallo de la entrega del mensaje a " + ex.InnerExceptions[i].FailedRecipient);
                        Globals.GetInstance().GetLog2().Error(pAgent, pSipTypeName, "uveSMTPSender: Fallo de la entrega del mensaje a " + ex.InnerExceptions[i].FailedRecipient);
                    }
                }
            }
            catch (Exception e)
            {
                //if (Utils.IsBlankField(pAgent) || Utils.IsBlankField(pSipTypeName))
                //    Globals.GetInstance().GetLog().TraceToFile(Log.TYPE_ERROR, Log.LEVEL_CRITICAL, e.Message + " --> " + e.StackTrace);
                //else
                //    Globals.GetInstance().GetLog().Error(pAgent, pSipTypeName, e);
                Globals.GetInstance().GetLog2().Error(pAgent, pSipTypeName, e);
            }
		}

	}
}
