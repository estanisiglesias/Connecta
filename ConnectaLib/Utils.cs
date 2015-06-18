using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data.OleDb;
using System.Data;
using System.Xml;
using System.Web;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Diagnostics;
using System.IO;
using Ionic.Zip;
using System.Text.RegularExpressions;
using System.Globalization;

namespace ConnectaLib
{
  /// <summary>
  /// Clase que contiene métodos de uso común
  /// </summary>
  public class Utils
  {

    /// <summary>
    /// Clase interna que almacena la relación de sip con los ficheros a procesar
    /// </summary>
    public class NomenclatorFile
    {
        public string sip = "";
        public string masterFile = null;
        public string slaveFile = null;
        public ArrayList aMasterFiles = new ArrayList(5);
        public ArrayList aSlaveFiles = new ArrayList(5);
        public int priorityDelay = 0;
    }

    /// <summary>
    /// Comprueba si una query devuelve resultados
    /// </summary>    
    /// <param name="sql">sql</param>
    /// <returns>true si devuelve resultados</returns>
    public static bool RecordExist(string sql)
    {
        return RecordExist(null, sql);
    }

    /// <summary>
    /// Comprueba si una query devuelve resultados
    /// </summary>
    /// <param name="db">Database</param>
    /// <param name="sql">sql</param>
    /// <returns>true si devuelve resultados</returns>
    public static bool RecordExist(Database db, string sql)
    {
      bool exist = false;
      if (db == null)
          db = Globals.GetInstance().GetDatabase();      

      DbDataReader cursor = null;
      try
      {
        cursor = db.GetDataReader(sql);
        if (cursor.Read())
        {
          exist = true;
        }
      }
      finally
      {
        if (cursor != null)
          cursor.Close();
      }
      return exist;
    }

    /// <summary>
    /// Comprueba si una query devuelve resultados (debe ser del tipo "select count(...)")
    /// </summary>
    /// <param name="sql">sql</param>
    /// <returns>true si hay registros</returns>
    public static bool HasRecords(string sql)
    {
      bool exist = false;
      Database db = Globals.GetInstance().GetDatabase();
      DbDataReader cursor = null;
      try
      {
        cursor = db.GetDataReader(sql);
        if (cursor.Read())
        {
          if (cursor.GetInt32(0) > 0)
            exist = true;
        }
      }
      finally
      {
        if (cursor != null)
          cursor.Close();
      }
      return exist;
    }

    /// <summary>
    /// Normalizar NIF
    /// </summary>
    /// <param name="nif">NIF</param>
    /// <returns>NIF normalizado</returns>
    public static string NIFNormalizado(string nif)
    {
        string normalizado = null;
        if (nif != null && !nif.Trim().Equals(""))
        {
            string temp = "";
            char c;
            int start = 0;

            //Eliminamos caràcteres no válidos
            for (int i = start; i < nif.Length; i++)
            {
                c = nif[i];
                if (CaracterValidoParaCIF(c))
                {
                    temp += c;
                }
            }
            normalizado = temp;

            //Eliminar 2 primeros caracteres si son alfabéticos
            if (normalizado.Length > 0)
            {
                c = normalizado[0];
                if (Char.IsLetter(c))
                {
                    if (normalizado.Length > 1)
                    {
                        c = normalizado[1];
                        if (Char.IsLetter(c))
                        {
                            normalizado = normalizado.Remove(0, 2);
                        }
                    }
                }
            }
        }
        else
            normalizado = "";

        return normalizado;
    }    

    private static bool CaracterValidoParaCIF(char c) 
    {
      return !Char.IsWhiteSpace(c) && c != '-' && c != '.' && c != '\0';
    }

    /// <summary>
    /// Determina si un valor se puede considerar vacío
    /// </summary>
    /// <param name="s">valor</param>
    /// <returns>true si se considera vacío</returns>
    public static bool IsBlankField(string s) 
    {
      if(s==null || (s!=null && s.Trim().Equals("")))
        return true;
      else
        return false;
    }

    /// <summary>
    /// Determina si un valor tiene un formato de hora válido
    /// </summary>
    /// <param name="s">valor</param>
    /// <returns>true si es correcto</returns>
    public static bool IsValidTime(string s)
    {
        DateTime dt = DateTime.Now;
        if (DateTime.TryParse(s, out dt))
            return true;
        else
            return false;
    }

    /// <summary>
    /// Determina si un valor tiene un formato de número entero válido
    /// </summary>
    /// <param name="s">valor</param>
    /// <returns>true si es correcto</returns>
    public static bool IsValidInteger(string s)
    {
        Int64 entero;
        if (Int64.TryParse(s, out entero))
            return true;
        else
            return false;
    }


    /// <summary>
    /// Encode the given number into a Base36 string
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string IntToBase36(Int64 pInput)
    {
        string CharList = "0123456789abcdefghijklmnopqrstuvwxyz";

        if (pInput < 0) return "0";

        char[] clistarr = CharList.ToCharArray();
        var result = new Stack<char>();
        while (pInput != 0)
        {
            result.Push(clistarr[pInput % 36]);
            pInput /= 36;
        }
        return new string(result.ToArray());
    }    

    /// <summary>
    /// Obtener país
    /// </summary>
    /// <param name="cm">codigoPais</param>
    /// <param name="sipagent">agente</param>
    /// <param name="siptype">tipo de SIP</param>
    /// <returns>país</returns>
    public static string ObtenerPais(string codigoPais,string sipagent,string siptypename)
    {
        string pais = "34";   //Valor por defecto

        //Si cmPais es blanco, o tiene valor ‘ESP’, ‘ES’ o ‘34’ devolver ‘34’.
        if (IsBlankField(codigoPais) || codigoPais.Equals("ESP") || codigoPais.Equals("ES") || codigoPais.Equals("34"))
            return pais;

        //Sino buscamos en la tabla PaisesAgente por:
        //IdcAgente 		= CodigoAgente
        //CodigoPaisAgente 	= INTER.pais
        // Si se encuentra, devolver el CodigoPaisConnecta encontrado. Si no se encuentra 
        //Registro -> (Nivel 30) LOG, código de país desconocido y devolver ‘34’.

        Database db = Globals.GetInstance().GetDatabase();
        DbDataReader cursor = null;
        try
        {
            string sql = "select CodigoPaisConnecta from PaisesAgente " +
                     "where IdcAgente=" + sipagent + " and CodigoPaisAgente = '" + codigoPais + "'";
            cursor = db.GetDataReader(sql);
            if (cursor.Read())
                pais = cursor.GetString(0);
            else
            {
                Globals.GetInstance().GetLog2().Trace(sipagent, siptypename, "UTIL0001", "Código de país {0} desconocido.", codigoPais);
            }
        }
        finally
        {
            if (cursor != null)
                cursor.Close();
        }
        return pais;
    }

    /// <summary>
    /// Obtener provincia
    /// </summary>
    /// <param name="codigoPostal">Codigo Postal</param>
    /// <returns>provincia</returns>
    public static string ObtenerProvincia(string codigoPostal, string pUbicacion)
    {
        string prov = "";

        if (pUbicacion.ToUpper() == "ITA")
        {
        }
        else
        {
            if (codigoPostal.ToLower().StartsWith("ad"))
            {
                prov = "AD";
            }
            else
            {
                if (codigoPostal != null && codigoPostal.Length >= 2)
                {
                    Int32 nCodPos = 0;
                    if (Int32.TryParse(codigoPostal.Trim(), out nCodPos))
                        prov = codigoPostal.Substring(0, 2);
                }
            }
        }

        return prov;
    }

    /// <summary>
    /// Obtener moneda
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="agent">agent</param>
    /// <param name="sipTypeName">tipo de sip</param>
    /// <param name="codMoneda">código de moneda</param>
    /// <returns>moneda</returns>
    public static string ObtenerMoneda(Database db, string agent, string sipTypeName, string codMoneda)
    {
      const string MONEDAxDEFECTO = "EUR";
      if (Utils.IsBlankField(codMoneda) || (codMoneda != null && codMoneda.ToUpper().Equals(MONEDAxDEFECTO)))
        return MONEDAxDEFECTO;

      //Buscar en MonedasAgente
      string moneda = "";
      DbDataReader cursor = null;
      try
      {
        string sql = "select CodigoMonedaConnecta from MonedasAgente " +
                     "where IdcAgente = " + agent + " and CodigoMonedaAgente = '" + codMoneda + "'";
        cursor = db.GetDataReader(sql);
        if (cursor.Read())
          moneda = db.GetFieldValue(cursor, 0);
        else
        {
          Globals.GetInstance().GetLog2().Trace(agent, sipTypeName, "UTIL0002", "Código de moneda {0} desconocida.", codMoneda);
          moneda = MONEDAxDEFECTO;
        }
      }
      finally
      {
        if (cursor != null)
          cursor.Close();
      }
      return moneda;
    }    

    /// <summary>
    /// Generar una SoapException
    /// </summary>
    /// <param name="msg">mensaje</param>
    /// <param name="context">contexto</param>
    /// <param name="e">exception</param>
    /// <returns>una nueva instancia de la clase SoapException</returns>
    public SoapException ThrowSoapException(String msg, HttpContext context, Exception e)
    {
      // Build the detail element of the SOAP fault.
      System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
      System.Xml.XmlNode node = doc.CreateNode(XmlNodeType.Element,
           SoapException.DetailElementName.Name, SoapException.DetailElementName.Namespace);

      // Build specific details for the SoapException.
      // Add first child of detail XML element.
      System.Xml.XmlNode details = doc.CreateNode(XmlNodeType.Element, "mySpecialInfo1",
                       e.ToString());
      System.Xml.XmlNode detailsChild =
        doc.CreateNode(XmlNodeType.Element, "childOfSpecialInfo",
                       e.Message);
      details.AppendChild(detailsChild);

      // Add second child of detail XML element with an attribute.
      System.Xml.XmlNode details2 =
        doc.CreateNode(XmlNodeType.Element, "mySpecialInfo2",
                       "http://tempuri.org/");
      XmlAttribute attr = doc.CreateAttribute("t", "attrName",
                          "http://tempuri.org/");
      attr.Value = e.ToString();
      details2.Attributes.Append(attr);

      node.AppendChild(details);
      node.AppendChild(details2);

      if (Globals.GetInstance() != null && Globals.GetInstance().GetLog2() != null)
        Globals.GetInstance().GetLog2().Error("", "", e);

      SoapException se = new SoapException("Fault occurred",
        SoapException.ClientFaultCode,
        context.Request.Url.AbsoluteUri,
        node);
      return se;
    }

    /// <summary>
    /// Obtener un nodo para incluirlo en un documento XML
    /// </summary>
    /// <param name="xmlDoc">documento de destino</param>
    /// <param name="nodename">nombre del nodo</param>
    /// <param name="value">valor del nodo</param>
    /// <returns>nodo</returns>
    public static XmlElement Node(XmlDocument xmlDoc, string nodename, string value)
    {
      XmlElement element = xmlDoc.CreateElement(nodename);
      XmlText txt = xmlDoc.CreateTextNode(value);
      element.AppendChild(txt);
      return element;
    }

    /// <summary>
    /// Obtener un nodo para incluirlo en un documento XML
    /// </summary>
    /// <param name="xmlDoc">documento de destino</param>
    /// <param name="nodename">nombre del nodo</param>
    /// <param name="db">base de datos</param>
    /// <param name="rs">data reader</param>
    /// <param name="position">ordinal del valor (zero based)</param>
    /// <returns>nodo</returns>
    public static XmlElement NodeFromDb(XmlDocument xmlDoc, string nodename, Database db, DbDataReader rs, int position)
    {
      XmlElement element = xmlDoc.CreateElement(nodename);
      XmlText txt = xmlDoc.CreateTextNode(db.GetFieldValue(rs, position));
      element.AppendChild(txt);
      return element;
    }

    /// <summary>
    /// Convertir string a double
    /// </summary>
    /// <param name="val">valor</param>
    /// <returns>double</returns>
    public static double StringToDouble(string val) 
    {
        Double dRes = 0;
        if (IsBlankField(val))
            dRes = 0;
        else
        {
            string valOld = val;
            val = val.Replace(".", ",");
            if (!Double.TryParse(val, out dRes))
            {
                //Si tiene separador de miles y de decimales
                if (valOld.Contains(".") && valOld.Contains(",") && valOld.IndexOf(".") < valOld.IndexOf(","))
                {
                    valOld = valOld.Replace(".", "");
                    Double.TryParse(valOld, out dRes);
                }
            }
        }
      return dRes;
    }

    /// <summary>
    /// Convertir string a int
    /// </summary>
    /// <param name="val">valor</param>
    /// <returns>int</returns>
    public static int StringToInt(string val)
    {
        if (IsBlankField(val)) val = "0";
        return int.Parse(val);
    }

    /// <summary>
    /// Recortar una cadena a una determinada longitud
    /// </summary>
    /// <param name="val">valor cadena</param>
    /// <param name="length">longitud cadena</param>
    /// <returns>double</returns>
    public static string StringTruncate(string val,int length)
    {
        val = val.Trim();
        if (IsBlankField(val))
        {
            val = "";
        }
        else
        {
            if (val.Length > length)
            {
                val = val.Substring(0, length);
            }
        }
        return val;
    }

    /// <summary>
    /// Recortar de una cadena un determinado número de caracteres de la izquierda
    /// </summary>
    /// <param name="val">valor cadena</param>
    /// <param name="length">longitud a recortar</param>
    /// <returns>double</returns>
      public static string StringTruncateLeft(string val, int length)
    {
        val = val.Trim();
        if (IsBlankField(val))
        {
            val = "";
        }
        else
        {
            if (val.Length <= length)
            {
                val = "";
            }
            else
            {
                val = val.Substring(length);
            }
        }
        return val;
    }

    public static string StringToPrintable(string s)
    {
        string result = "";
        char c;
        for (int i = 0; i < s.Length; i++)
        {
            c = s[i];

            if (Char.IsNumber(c) ||
                Char.IsLetterOrDigit(c) ||
                Char.IsPunctuation(c) ||
                Char.IsWhiteSpace(c) ||
                Char.IsSeparator(c) ||
                Char.IsSymbol(c))
            {
                  result += c.ToString();
            }
        }
        return result;
    }

    /// <summary>
    /// Comprueba si un fichero está bloqueado
    /// </summary>
    /// <param name="filename">fichero</param>
    /// <returns>true si bloqueado</returns>
    private static bool FileIsLocked(string filename)
    {
        bool locked = false;
        FileStream fs = null;
        try
        {
            FileInfo fi = new FileInfo(filename);

            // Abrir el fichero para comprobar si está bloqueado...
            fs = fi.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (Exception)
        {
            locked = true;
        }
        finally
        {
            if (fs != null)
                fs.Close();
        }
        return locked;
    }

    const int WAIT_TIME_FOLDER_SIZE = 500; // miliseconds (1000 milisegons = 1 segon, 500 milisegons = 0,5 segons
    /// <summary>
    /// Determinar si alguno de los ficheros de la carpeta està bloqueado
    /// </summary>
    /// <param name="filesInFolder">lista de ficheros</param>
    /// <returns>id. de tipo</returns>
    public static bool someFileIsLocked(string agent, string[] filesInFolder, string path)
    {
        //bool result = false;
        //if (filesInFolder != null && filesInFolder.Length > 0)
        //{
        //    foreach (string file in filesInFolder)
        //    {
        //        //Comprobar que el fichero no está cogido por otro proceso
        //        if (FileIsLocked(file))
        //            result= true;
        //    }
        //}
        //return result;
        bool result = false;
        float actFolderSize = 0.0f;
        if (filesInFolder != null && filesInFolder.Length > 0)
        {
            foreach (string file in filesInFolder)
            {
                //Comprobar que el fichero no está cogido por otro proceso
                if (FileIsLocked(file))
                {
                    return true; // no hace falta esperar, ya sabemos que algun fichero está bloqueado
                }
                try
                {
                    FileInfo finfo = new FileInfo(file);
                    actFolderSize += finfo.Length;
                }
                catch (Exception)
                {
                    //ignorar
                }
            }

            System.Threading.Thread.Sleep(WAIT_TIME_FOLDER_SIZE); // espera X segundos
            if (actFolderSize < CalculateFolderSize(agent, path)) // si el directorio es mas grande de lo calculado result=true
            {
                result = true;
            }
        }
        return result;
    }

    protected static float CalculateFolderSize(string agent, string folder)
    {
        float folderSize = 0.0f;
        try
        {
            if (!Directory.Exists(folder))
                return folderSize;
            else
            {
                try
                {
                    foreach (string file in Directory.GetFiles(folder, "*.*"))
                    {
                        if (File.Exists(file))
                        {
                            try
                            {
                                FileInfo finfo = new FileInfo(file);
                                folderSize += finfo.Length;
                            }
                            catch (Exception)
                            {
                                //ignorar
                            }
                        }
                    }
                }
                catch (NotSupportedException e)
                {
                    Globals.GetInstance().GetLog2().Error(agent, "CalculateFolderSize", e);
                }
                catch (Exception e)
                {
                    Globals.GetInstance().GetLog2().Error(agent, "CalculateFolderSize", e);
                }
            }
        }
        catch (UnauthorizedAccessException e)
        {
            Globals.GetInstance().GetLog2().Error(agent, "CalculateFolderSize", e);
        }
        return folderSize;
    }

    public static bool someFileIsNewerThan(string agent, string[] filesInFolder, string path, DateTime limitDate)
    {
        bool result = false;
        if (filesInFolder != null && filesInFolder.Length > 0)
        {
            foreach (string file in filesInFolder)
            {
                //Obtener la información del fichero y comprobar la fecha
                FileInfo finfo = new FileInfo(file);
                if (finfo.CreationTime >= limitDate)
                {
                    return true; // no hace falta esperar, ya sabemos que algun fichero está bloqueado
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Obtener tipo de fichero
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    /// <returns>id. de tipo</returns>
    public static int FileType(string filename)
    {
      string s = filename.ToLower();
      if (s.EndsWith(".xml"))
        return SipManager.FILE_XML;
      else if (s.EndsWith(".txt"))
        return SipManager.FILE_DELIMITED;
      else if (s.EndsWith(".csv"))
          return SipManager.FILE_DELIMITED;
      else if (s.EndsWith(".blk"))
          return SipManager.FILE_BULK;
      else
        return SipManager.FILE_UNKNOWN;
    }

    /// <summary>
    /// Comprobar si el fichero tiene 0 bytes
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    /// <returns>true/false</returns>
    public static bool IsEmptyFile(string filename)
    {
        if (System.IO.File.Exists(filename))
        {
            System.IO.FileInfo fi = new System.IO.FileInfo(filename);
            if (fi.Length == 0)
                return true;
            else
                return false;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Obtener el tamaño del fichero
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    /// <returns>tamaño fichero en bytes</returns>
    public static Int64 GetFileSize(string filename)
    {
        if (System.IO.File.Exists(filename))
        {
            System.IO.FileInfo fi = new System.IO.FileInfo(filename);
            return fi.Length;
        }
        else
        {
            return 0;
        }
    }

    /// <summary>
    /// Obtener el encoding de un fichero (ver http://www.west-wind.com/WebLog/posts/197245.aspx)
    /// </summary>
    /// <param name="srcFile">fichero</param>
    /// <returns>encoding</returns>
    public static Encoding GetFileEncoding(string srcFile)
    {
        //// *** Use Default of Encoding.Default (Ansi CodePage)
        //Encoding enc = Encoding.Default;

        //// *** Detect byte order mark if any - otherwise assume default
        //byte[] buffer = new byte[5];
        //FileStream file = new FileStream(srcFile, FileMode.Open);
        //file.Read(buffer, 0, 5);
        //file.Close();

        //if (buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
        //  enc = Encoding.UTF8;
        //else if (buffer[0] == 0xfe && buffer[1] == 0xff)
        //  enc = Encoding.Unicode;
        //else if (buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0xfe && buffer[3] == 0xff)
        //  enc = Encoding.UTF32;
        //else if (buffer[0] == 0x2b && buffer[1] == 0x2f && buffer[2] == 0x76)
        //  enc = Encoding.UTF7;

        //return enc;

        // *** Use Default of Encoding.Default (Ansi CodePage)
        Encoding enc = Encoding.Default;

        FileStream file = null;
        try
        {
            // *** Detect byte order mark if any - otherwise assume default
            byte[] buffer = new byte[5];
            file = new FileStream(srcFile, FileMode.Open);
            int len = (int)file.Length;
            file.Read(buffer, 0, 5);
            file.Close();
            file = null;

            if (buffer[0] == 0xef && buffer[1] == 0xbb && buffer[2] == 0xbf)
                enc = Encoding.UTF8;
            else if (buffer[0] == 0xfe && buffer[1] == 0xff)
                enc = Encoding.Unicode;
            else if (buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0xfe && buffer[3] == 0xff)
                enc = Encoding.UTF32;
            else if (buffer[0] == 0x2b && buffer[1] == 0x2f && buffer[2] == 0x76)
                enc = Encoding.UTF7;

            if (enc == Encoding.Default)
            {
                int rv = 1;
                int ASCII7only = 1;
                byte complement;
                buffer = new byte[len];
                file = new FileStream(srcFile, FileMode.Open);
                file.Read(buffer, 0, len);
                for (int i = 0; i < len; )
                {
                    byte b = buffer[i];
                    if (b == 0)
                        break;
                    else if (b < 0x80)
                    {
                        // 0nnnnnnn If the byte's first hex code begins with 0-7, it is an ASCII character.
                        i++;
                    }
                    else if (b < (0x80 + 0x40))
                    {											  // 10nnnnnn 8 through B cannot be first hex codes
                        ASCII7only = 0;
                        rv = 0;
                        break;
                    }
                    else if (b < (0x80 + 0x40 + 0x20))
                    {					  // 110xxxvv 10nnnnnn  If it begins with C or D, it is an 11 bit character
                        ASCII7only = 0;
                        if (i >= len - 1)
                            break;

                        complement = (byte)~((b & (byte)0x1F));
                        if ((complement == 1) || (buffer[i + 1] & (0x80 + 0x40)) != 0x80)
                        {
                            rv = 0;
                            break;
                        }
                        i += 2;
                    }
                    else if (b < (0x80 + 0x40 + 0x20 + 0x10))
                    {								// 1110qqqq 10xxxxvv 10nnnnnn If it begins with E, it is 16 bit
                        ASCII7only = 0;
                        if (i >= len - 2)
                            break;
                        complement = (byte)~((b & 0xF));
                        if ((complement == 1) || (buffer[i + 1] & (0x80 + 0x40)) != 0x80 || (buffer[i + 2] & (0x80 + 0x40)) != 0x80)
                        {
                            rv = 0;
                            break;
                        }
                        i += 3;
                    }
                    else
                    {													  // more than 16 bits are not allowed here
                        ASCII7only = 0;
                        rv = 0;
                        break;
                    }
                }
                if (ASCII7only == 1)
                    enc = Encoding.ASCII;
                if (rv == 1)
                    enc = Encoding.UTF8;
            }
        }
        finally
        {
            if (file != null)
                file.Close();
            file = null;
        }

        return enc;
    }

    /// <summary>
    /// Get a date
    /// </summary>
    /// <param name="s">value</param>
    /// <returns>null if date is wrong</returns>
    public static object GetDate(string s)
    {
        // Date formats        
        string[] dateformats ={ 
        "dd-MM-yyyy",
        "dd-MM-yy",
        "yyyy-MM-dd",
        "dd/MM/yy",
        "dd/MM/yyyy",
        "yyyy/MM/dd",
        "ddMMyyyy",
        "ddMMyy",
        };
        DateTimeFormatInfo myDTFI = new DateTimeFormatInfo();
        for (int i = 0; i < dateformats.Length; i++)
        {
            try
            {
                myDTFI.ShortDatePattern = dateformats[i];
                DateTime dt;
                if (myDTFI.ShortDatePattern.Length <= 6) dt = DateTime.ParseExact(s, myDTFI.ShortDatePattern, CultureInfo.InvariantCulture);
                else dt = DateTime.Parse(s, myDTFI);
                return dt;
            }
            catch (Exception)
            {
                //ignore
            }
        }
        return null;
    }

    public static string GetTimeStamp(Database db, string pDelay, string pSlotIni, string pSlotEnd)
    {
        string sNowTime = DateTime.Now.ToShortTimeString();
        string sNowDate = DateTime.Now.ToShortDateString();
        string sTimeStamp = "";
        string sSlotIni = "";
        string sSlotEnd = "";
        if (String.IsNullOrEmpty(pSlotIni))
            sSlotIni = DateTime.Now.ToShortDateString() + " 00:00:00";
        else
            sSlotIni = DateTime.Now.ToShortDateString() + " " + pSlotIni;
        if (String.IsNullOrEmpty(pSlotEnd))
            sSlotEnd = DateTime.Now.ToShortDateString() + " 23:59:59";
        else
            sSlotEnd = DateTime.Now.ToShortDateString() + " " + pSlotEnd;

        //Si estoy dentro de la franja horaria especificada, ajustamos el timestamp, si no lo dejamos vacío
        if (DateTime.Parse(sSlotIni) < DateTime.Parse(sNowTime) && DateTime.Parse(sSlotEnd) > DateTime.Parse(sNowTime))
        {
            //El timestamp se ajusta a la hora de retraso del dia actual
            sTimeStamp = sNowDate + " " + pDelay;
            //Si el timestamp ya ajustado es anterior a la hora actual, no hace falta retrasar (ya va tarde!!!), dejamos el timestamp vacío
            if (DateTime.Parse(sTimeStamp) < DateTime.Parse(sNowTime)) sTimeStamp = "";
        }
        return sTimeStamp;
    }
    /// <summary>
    /// Comprobar si se trata de un fichero zip
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    /// <returns>true/false</returns>
    public static bool IsZipFileType(string filename)
    {
        string s = filename.ToLower();
        if (s.EndsWith(".zip"))
            return true;
        else
            return false;
    }

    /// <summary>
    /// Descomprimir un fichero zip
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    public static void UnZipFile(string agent, string sipType, string filename)
    {
        string s = filename.ToLower();
        string p = System.IO.Path.GetDirectoryName(s);

        SipCore sipWrk = new SipCore();
        string sipTypeName = sipWrk.ConvertSipTypeToSipTypeName(sipType);

        //Registrar en el log
        Globals.GetInstance().GetLog2().Info(agent, sipTypeName, "Descompresión ZIP del fichero: " + s );
        //Guardar previamente en backup box...
        Backup bk = new Backup();
        if (!bk.DoBackup(agent, sipTypeName, s))
        {
            throw new Exception("Backup failed! agent=" + agent + ",filename=" + s);
        }
        try
        {
            //Deszipear el fichero
            if (System.IO.File.Exists(s))
            {
                using (ZipFile zip = ZipFile.Read(s))
                {
                    foreach (ZipEntry zipEnt in zip)
                    {
                        zipEnt.ExtractExistingFile = ExtractExistingFileAction.OverwriteSilently;
                        zipEnt.Extract(p);
                    }
                }
                //Eliminar el fichero zip
                if (System.IO.File.Exists(s)) System.IO.File.Delete(s);
            }
        }
        catch (Exception e)
        {
            Globals.GetInstance().GetLog2().Error(agent, sipTypeName, e);
            //Renombrar el fichero .zip a .bad
            if (System.IO.File.Exists(s))
            {
                if (System.IO.File.Exists(s + ".bad")) System.IO.File.Delete(s + ".bad");
                System.IO.File.Move(s, s + ".bad");
            }

        }
        finally
        {
        }
    }

    /// <summary>
    /// Partir un fichero en partes de n filas cada una.
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    //public static ArrayList BreakFile(string agent, string sipType, string masterFileName, string slaveFileName, string partitionNumLines, string pivotFieldPos)
    //{
    //    ArrayList resultList = new ArrayList();
        
    //    string sFile1 = "";
    //    string sFile2 = "";
    //    int masterslaveCase = 0;

    //    SipCore sipWrk = new SipCore();
    //    string sipTypeName = sipWrk.ConvertSipTypeToSipTypeName(sipType);
        
    //    if (masterFileName != null && slaveFileName != null)
    //    {
    //        sFile1 = slaveFileName.ToLower();
    //        sFile2 = masterFileName.ToLower();
    //        masterslaveCase = 0;
    //    }
    //    else if (masterFileName != null && slaveFileName == null)
    //    {
    //        sFile1 = masterFileName.ToLower();
    //        sFile2 = "";
    //        masterslaveCase = 1;
    //    }
    //    else if (masterFileName == null && slaveFileName != null)
    //    {
    //        sFile1 = slaveFileName.ToLower();
    //        sFile2 = "";
    //        masterslaveCase = 2;
    //    }

    //    //Registrar en el log
    //    Globals.GetInstance().GetLog2().Info(agent, sipTypeName, "Partición FILEBREAKER del fichero: " + sFile1);

    //    //Guardar previamente en backup box...
    //    Backup bk = new Backup();
    //    if (!bk.DoBackup(agent, sipTypeName, sFile1))
    //    {
    //        throw new Exception("Backup failed! agent=" + agent + ",filename=" + sFile1);
    //    }

    //    TextReader tr = null;
    //    TextWriter tw = null;

    //    try
    //    {
    //        //Partir el fichero
    //        if (System.IO.File.Exists(sFile1))
    //        {
    //            string sBrokenFile1 = "";

    //            tr = new StreamReader(sFile1);
    //            string sRow = "";
    //            string lastLineReaded = "";
    //            int count = 0;
    //            bool partitionNumLinesReached = false;
    //            bool canCut = false;
                
    //            int filenum = 1;

    //            int ix = sFile1.LastIndexOf(".");
    //            string outputFilenameBase = sFile1.Substring(0, ix);
    //            string outputFilenameExt = sFile1.Substring(ix + 1);

    //            string fieldSeparator = Globals.GetInstance().GetFieldSeparator();
    //            string lastToken = "";
    //            string token = "";

    //            //Si tenemos dos ficheros, el que se partirà es el slave, y añadimos el master a la lista de resultados
    //            if (masterslaveCase == 0)
    //            {
    //                AddBrokenFileToResultList(sipType, resultList, sFile2, 1);
    //                masterslaveCase = 2; //Canviamos el indicador porque a partir de la segunda parte el master lo pondremos vacio.                
    //            }

    //            while ((sRow = tr.ReadLine()) != null)
    //            {
    //                count++;
    //                if (count > int.Parse(partitionNumLines))
    //                {
    //                    //Set a signal to "cut" the file...
    //                    partitionNumLinesReached = true;
    //                }

    //                //Keep the current line
    //                lastLineReaded = sRow;

    //                //Set the flag to verify if the the file can be cutted...
    //                canCut = true;
    //                if (int.Parse(pivotFieldPos) != 0)
    //                {
    //                    StringTokenizer st = new StringTokenizer(sRow, fieldSeparator);
    //                    token = st.GetToken(int.Parse(pivotFieldPos));
    //                    if (token.Equals(lastToken)) canCut = false;
    //                    lastToken = token;
    //                }
    //                //Cut if necessary
    //                if (canCut && partitionNumLinesReached)
    //                {
    //                    tw.Close();
    //                    tw = null;

    //                    //Añadir el fichero partido a la lista de resultados
    //                    AddBrokenFileToResultList(sipType, resultList, sBrokenFile1, masterslaveCase);

    //                    partitionNumLinesReached = false;
    //                    canCut = false;
    //                    filenum++;
    //                    count = 0;
    //                }
    //                if (tw == null)
    //                {
    //                    String fmt = String.Format("{0:000}", filenum);
    //                    sBrokenFile1 = outputFilenameBase + ".Part" + fmt + "." + outputFilenameExt;
    //                    tw = new StreamWriter(sBrokenFile1);
    //                }
                    
    //                //Write the line in the current output file
    //                tw.WriteLine(sRow);
    //            }

    //            //Write pending lines...
    //            if (partitionNumLinesReached && tw != null) tw.WriteLine(lastLineReaded);

    //            if (tw != null)
    //            {
    //                tw.Close();
    //                tw = null;

    //                //Añadir el fichero partido a la lista de resultados
    //                AddBrokenFileToResultList(sipType, resultList, sBrokenFile1, masterslaveCase);
    //            }

    //            tr.Close();
    //            tr = null;

    //            //Eliminar el fichero origen
    //            if (System.IO.File.Exists(sFile1)) System.IO.File.Delete(sFile1);
    //        }
    //    }
    //    catch (Exception e)
    //    {
    //        Globals.GetInstance().GetLog2().Error(agent, sipTypeName, e);
    //        if (tr != null)
    //            tr.Close();
    //        //Renombrar el fichero origen a .bad
    //        if (System.IO.File.Exists(sFile1))
    //        {
    //            if (System.IO.File.Exists(sFile1 + ".bad")) System.IO.File.Delete(sFile1 + ".bad");
    //            System.IO.File.Move(sFile1, sFile1 + ".bad");
    //        }
    //        if (!Utils.IsBlankField(sFile2))
    //        {
    //            if (System.IO.File.Exists(sFile2))
    //            {
    //                if (System.IO.File.Exists(sFile2 + ".bad")) System.IO.File.Delete(sFile2 + ".bad");
    //                System.IO.File.Move(sFile2, sFile2 + ".bad");
    //            }
    //        }

    //        //Eliminar todos los posibles ficheros que ya puedan haberse partido
    //        foreach (NomenclatorFile bnf in resultList)
    //        {
    //            if (!Utils.IsBlankField(bnf.masterFile))
    //            {
    //                if (System.IO.File.Exists(bnf.masterFile)) System.IO.File.Delete(bnf.masterFile);
    //            }
    //            if (!Utils.IsBlankField(bnf.slaveFile))
    //            {
    //                if (System.IO.File.Exists(bnf.slaveFile)) System.IO.File.Delete(bnf.slaveFile);
    //            }
    //        }
    //        //Vaciamos la lista de resultados
    //        resultList.Clear();
    //    }
    //    finally
    //    {
    //        if (tr != null)
    //            tr.Close();
    //        if (tw != null)
    //            tw.Close();
    //    }
    //    return resultList;
    //}
    public static ArrayList BreakFile(string agent, string sipType, string masterFileName, string slaveFileName, string partitionNumLines, string pivotFieldPos)
    {
        ArrayList resultList = new ArrayList();

        string sFile1 = "";
        string sFile2 = "";
        string sMsg1 = "";

        SipCore sipWrk = new SipCore();
        string sipTypeName = sipWrk.ConvertSipTypeToSipTypeName(sipType);

        if (masterFileName != null && slaveFileName != null)
        {
            sFile1 = masterFileName.ToLower();
            sFile2 = slaveFileName.ToLower();
            sMsg1 = "Partición FILEBREAKER de los ficheros: " + sFile1 + " y " + sFile2;
        }
        else if (masterFileName != null && slaveFileName == null)
        {
            sFile1 = masterFileName.ToLower();
            sFile2 = "";
            sMsg1 = "Partición FILEBREAKER del fichero: " + sFile1;
        }
        else if (masterFileName == null && slaveFileName != null)
        {
            sFile1 = "";
            sFile2 = slaveFileName.ToLower(); 
            sMsg1 = "Partición FILEBREAKER del fichero: " + sFile1;
        }

        //Registrar en el log
        Globals.GetInstance().GetLog2().Info(agent, sipTypeName, sMsg1);

        //Guardar previamente en backup box...
        Backup bk = new Backup();
        if (!String.IsNullOrEmpty(sFile1))
        {
            if (!bk.DoBackup(agent, sipTypeName, sFile1))
            {
                throw new Exception("Backup failed! agent=" + agent + ",filename=" + sFile1);
            }
        }
        if (!String.IsNullOrEmpty(sFile2))
        {
            if (!bk.DoBackup(agent, sipTypeName, sFile2))
            {
                throw new Exception("Backup failed! agent=" + agent + ",filename=" + sFile2);
            }
        }

        TextReader tr = null;
        TextWriter tw = null;

        try
        {
            int filenum = 1;

            //Partir el fichero 1
            BreakOneFile(sipType, partitionNumLines, pivotFieldPos, resultList, sFile1, 1, ref tr, ref tw, ref filenum);

            //Partir el fichero 2
            BreakOneFile(sipType, partitionNumLines, pivotFieldPos, resultList, sFile2, 2, ref tr, ref tw, ref filenum);

        }
        catch (Exception e)
        {
            Globals.GetInstance().GetLog2().Error(agent, sipTypeName, e);
            if (tr != null)
                tr.Close();
            //Renombrar el fichero origen a .bad
            if (System.IO.File.Exists(sFile1))
            {
                if (System.IO.File.Exists(sFile1 + ".bad")) System.IO.File.Delete(sFile1 + ".bad");
                System.IO.File.Move(sFile1, sFile1 + ".bad");
            }
            if (!Utils.IsBlankField(sFile2))
            {
                if (System.IO.File.Exists(sFile2))
                {
                    if (System.IO.File.Exists(sFile2 + ".bad")) System.IO.File.Delete(sFile2 + ".bad");
                    System.IO.File.Move(sFile2, sFile2 + ".bad");
                }
            }

            //Eliminar todos los posibles ficheros que ya puedan haberse partido
            foreach (NomenclatorFile bnf in resultList)
            {
                if (!Utils.IsBlankField(bnf.masterFile))
                {
                    if (System.IO.File.Exists(bnf.masterFile)) System.IO.File.Delete(bnf.masterFile);
                }
                if (!Utils.IsBlankField(bnf.slaveFile))
                {
                    if (System.IO.File.Exists(bnf.slaveFile)) System.IO.File.Delete(bnf.slaveFile);
                }
            }
            //Vaciamos la lista de resultados
            resultList.Clear();
        }
        finally
        {
            if (tr != null)
                tr.Close();
            if (tw != null)
                tw.Close();
        }
        return resultList;
    }

    private static void BreakOneFile(string sipType, string partitionNumLines, string pivotFieldPos, ArrayList resultList, string sFile, int masterslaveCase, ref TextReader tr, ref TextWriter tw, ref int filenum)
    {
        if (System.IO.File.Exists(sFile))
        {
            string sBrokenFile1 = "";

            tr = new StreamReader(sFile);
            string sRow = "";
            string lastLineReaded = "";
            string lastLineWrited = "";
            int count = 0;
            bool partitionNumLinesReached = false;
            bool canCut = false;

            int ix = sFile.LastIndexOf(".");
            string outputFilenameBase = sFile.Substring(0, ix);
            string outputFilenameExt = sFile.Substring(ix + 1);

            string fieldSeparator = Globals.GetInstance().GetFieldSeparator();
            string lastToken = "";
            string token = "";

            filenum = 1;

            while ((sRow = tr.ReadLine()) != null)
            {
                count++;
                if (count > int.Parse(partitionNumLines))
                {
                    //Set a signal to "cut" the file...
                    partitionNumLinesReached = true;
                }

                //Keep the current line                
                lastLineReaded = sRow;

                //Set the flag to verify if the the file can be cutted...
                canCut = true;
                if (int.Parse(pivotFieldPos) != 0)
                {
                    StringTokenizer st = new StringTokenizer(sRow, fieldSeparator);
                    token = st.GetToken(int.Parse(pivotFieldPos));
                    if (token.Equals(lastToken)) canCut = false;
                    lastToken = token;
                }
                //Cut if necessary
                if (canCut && partitionNumLinesReached)
                {
                    tw.Close();
                    tw = null;

                    //Añadir el fichero partido a la lista de resultados
                    AddBrokenFileToResultList(sipType, resultList, sBrokenFile1, masterslaveCase);

                    partitionNumLinesReached = false;
                    canCut = false;
                    filenum++;
                    count = 0;
                }
                if (tw == null)
                {
                    String fmt = String.Format("{0:000}", filenum);
                    sBrokenFile1 = outputFilenameBase + ".Part" + fmt + "." + (filenum == 1 ? "FIRST." : "") + outputFilenameExt;
                    tw = new StreamWriter(sBrokenFile1);
                }

                //Write the line in the current output file
                tw.WriteLine(sRow);
                lastLineWrited = sRow;
            }

            //Write pending lines...
            if (partitionNumLinesReached && tw != null)
            {
                if (lastLineReaded != lastLineWrited)
                    tw.WriteLine(lastLineReaded);
            }

            if (tw != null)
            {
                tw.Close();
                tw = null;

                //A la última parte para identificarlo como la última le queremos poner la marca LAST. 
                string sWrkBrokenFile = sBrokenFile1;
                String fmt = String.Format("{0:000}", filenum);
                sBrokenFile1 = sBrokenFile1.Replace(".Part" + fmt + ".", ".Part" + fmt + ".LAST.");
                if (System.IO.File.Exists(sWrkBrokenFile) && !System.IO.File.Exists(sBrokenFile1)) System.IO.File.Move(sWrkBrokenFile, sBrokenFile1);

                //Añadir el fichero partido a la lista de resultados
                AddBrokenFileToResultList(sipType, resultList, sBrokenFile1, masterslaveCase);
            }

            tr.Close();
            tr = null;

            //Eliminar el fichero origen
            if (System.IO.File.Exists(sFile)) System.IO.File.Delete(sFile);
        }
    }

    private static void AddBrokenFileToResultList(string sipType, ArrayList resultList, string pFile, int masterslaveCase)
    {
        NomenclatorFile nf = new NomenclatorFile();
        nf.sip = sipType;
        if (masterslaveCase == 1)
        {
            nf.masterFile = pFile.ToLower();
            nf.slaveFile = null;
            nf.priorityDelay = 1000;
        }
        else if (masterslaveCase == 2)
        {
            nf.masterFile = null;
            nf.slaveFile = pFile.ToLower();
            nf.priorityDelay = 1001;
        }
        resultList.Add(nf);
    }

    /// <summary>
    /// Invocar a un proceso externo
    /// </summary>
    /// <param name="sipid">sip id</param>
    /// <param name="sipTypeName">sip name</param>
    /// <param name="agente">agente</param> 
    public static void InvokeExternalProgram(string sipid, string sipTypeName, string agent)
    {
        //Buscar en el fichero de configuración el nombre del proceso.
        //Si no está definido, no hacer nada.
        string pgm = IniManager.INIGetKeyValue(IniManager.CFG_SECTION_EXTERNAL_PROGRAMS, sipid, IniManager.GetIniFile());
        if (pgm != null && !pgm.Equals(""))
        {
            try
            {
                Globals.GetInstance().GetLog2().Info(agent, sipTypeName, "Invocación del proceso externo: " + pgm + " con los parámetros: " + agent);
                
                Utils.InvokeExternalProgram(pgm, agent);
            }
            catch (Exception e)
            {
                Globals.GetInstance().GetLog2().Error(agent, sipTypeName, e);
                throw e;
            }
        }
    }

    /// <summary>
    /// Invocar a un proceso externo
    /// </summary>
    /// <param name="sipid">sip id</param>
    /// <param name="sipTypeName">sip name</param>
    /// <param name="agente">agente</param> 
    public static void InvokeExternalProgramPOST(string sipid, string sipTypeName, string agent)
    {
        //Buscar en el fichero de configuración el nombre del proceso.
        //Si no está definido, no hacer nada.
        string pgm = IniManager.INIGetKeyValue(IniManager.CFG_SECTION_EXTERNAL_PROGRAMS, sipid + "POST", IniManager.GetIniFile());
        if (pgm != null && !pgm.Equals(""))
        {
            try
            {
                if (System.IO.File.Exists(pgm))
                {
                    Globals.GetInstance().GetLog2().Info(agent, sipTypeName, "Invocación del proceso externo POST: " + pgm + " con los parámetros: " + agent);

                    Utils.InvokeExternalProgram(pgm, agent);
                }
            }
            catch (Exception e)
            {
                Globals.GetInstance().GetLog2().Error(agent, sipTypeName, e);
                throw e;
            }
        }
    }

    /// <summary>
    /// Invocar a un proceso externo
    /// </summary>
    /// <param name="pgm">proceso</param> 
    /// <param name="agente">agente</param> 
    public static void InvokeExternalProgram(string pgm, string agent)
    {
        if (pgm != null && !pgm.Equals(""))
        {
            Process myProc = null;
            //Realizar la llamada al proceso
            try
            {
                // Start the process.
                myProc = new System.Diagnostics.Process();
                myProc.StartInfo.FileName = pgm;
                myProc.StartInfo.Arguments = agent;
                myProc.StartInfo.UseShellExecute = false;
                myProc.StartInfo.RedirectStandardOutput = false;
                myProc.Start();
                myProc.WaitForExit();
            }
            finally
            {
                // Stop the process.
                if (myProc != null && !myProc.HasExited)
                {
                    if (pgm.ToLower().EndsWith(".bat"))
                        myProc.Kill();
                    else
                        myProc.CloseMainWindow();
                }
            }
        }
    }

    /// <summary>
    /// Invocar a un proceso externo
    /// </summary>
    /// <param name="pgm">proceso</param> 
    /// <param name="agente">agente</param> 
    public static void InvokeExternalProgramAsync(string pgm, string agent)
    {
        if (pgm != null && !pgm.Equals(""))
        {
            Process myProc = null;
            //Realizar la llamada al proceso
            try
            {
                // Start the process.
                myProc = new System.Diagnostics.Process();
                myProc.StartInfo.FileName = pgm;
                myProc.StartInfo.Arguments = agent;
                myProc.StartInfo.UseShellExecute = false;
                myProc.StartInfo.RedirectStandardOutput = false;
                myProc.Exited += (sender, args) =>
                {
                    // Stop the process.
                    if (myProc != null && !myProc.HasExited)
                    {
                        if (pgm.ToLower().EndsWith(".bat"))
                            myProc.Kill();
                        else
                            myProc.CloseMainWindow();
                    }
                };
                myProc.Start();                
            }
            catch
            {
                // Stop the process.
                if (myProc != null && !myProc.HasExited)
                {
                    if (pgm.ToLower().EndsWith(".bat"))
                        myProc.Kill();
                    else
                        myProc.CloseMainWindow();
                }
            }
        }
    }

    /// <summary>
    /// Formatear cadena con 'x' valores
    /// </summary>
    /// <param name="descripcion">descripcion a formatear</param> 
    /// <param name="valores">valores</param> 
    public static string MyFormat(string descripcion, string[] valores)
    {
        MatchCollection matches;
        Regex regex;
        string[] arrayVacio = { };
        string s = string.Empty;
        // cuenta el número total de carácteres "{" en la variable descripcion            
        // para poder asignar correctamente los parámetros del array valores
        regex = new Regex(@"([{])");
        matches = regex.Matches(descripcion);
        if (valores == null) valores = arrayVacio;
        if (matches.Count == valores.Length) s = string.Format(descripcion, valores);
        else if (matches.Count > valores.Length)
        {
            string[] n = new string[matches.Count];
            for (int i = 0; i < n.Length; i++)
            {
                if (i < valores.Length) n[i] = valores[i];
                else n[i] = "/param " + (i + 1) + "/";
            }
            s = string.Format(descripcion, n);
        }
        else s = string.Format(descripcion, valores);
        return s;
    }

    /// <summary>
    /// MyParseString
    /// </summary>
    /// <param name="pString">string</param>   
    /// <returns>string modified</returns>
    public static string MyParseString(string pString)
    {
        if (String.IsNullOrEmpty(pString)) return pString;

        string wrkString = pString;
        string resString = "";
        bool parsear = true;
        int i;
        int ii;

        while (parsear)
        {
            i = wrkString.IndexOf("{");
            if (i == -1)
            {
                //si no existe el tag inicial devolvemos lo que queda de string i paramos
                resString += wrkString;
                parsear = false;
            }
            else
            {
                ii = wrkString.IndexOf("}");
                if (ii == -1)
                {
                    //si no existe el tag final devolvemos lo que queda de string i paramos
                    resString += wrkString;
                    parsear = false;
                }
                else if (i > ii || (ii - i) == 1)
                {
                    //si existe el tag final pero està antes que el inicial o no hay ningun caracter entre ellos, añadimos al resultado la parte hasta el tag inicial y ajustamos la string de trabajo sin la parte añadida al resultado
                    resString += wrkString.Substring(0, i);
                    wrkString = wrkString.Substring(i);
                }
                else
                {
                    //si existe el tag al final i está después del tag inicial, añadimos al resultado la parte anterior al tag inicial, convertimos la parte entre el tag inicial i el tag final y la añadimos al resultado, y finalmente ajustamos el string de trabajo la parte posterior al tag final
                    resString += wrkString.Substring(0, i);
                    string strWrk = wrkString.Substring(i + 1, ii - i - 1);
                    int nWrk = 0;
                    if (int.TryParse(strWrk, out nWrk))
                    {
                        resString += Convert.ToChar(nWrk);
                    }
                    else
                    {
                        resString += wrkString.Substring(i, ii - i + 1);
                    }
                    if (ii + 1 < wrkString.Length)
                    {
                        wrkString = wrkString.Substring(ii + 1);
                    }
                    else
                    {
                        wrkString = "";
                        parsear = false;
                    }
                }
            }
        }
        return resString;
    }
    /// <summary>
    /// FromHex
    /// </summary>
    /// <param name="hexData">byte[]</param>   
    /// <returns>byte[]</returns>
    public static byte[] FromHex(byte[] hexData)
    {
        if (hexData == null)
        {
            throw new ArgumentNullException("hexData");
        }

        if (hexData.Length < 2 || (hexData.Length / (double)2 != Math.Floor(hexData.Length / (double)2)))
        {
            throw new Exception("Illegal hex data, hex data must be in two bytes pairs, for example: 0F,FF,A3,... .");
        }

        MemoryStream retVal = new MemoryStream(hexData.Length / 2);
        // Loop hex value pairs
        for (int i = 0; i < hexData.Length; i += 2)
        {
            byte[] hexPairInDecimal = new byte[2];
            // We need to convert hex char to decimal number, for example F = 15
            for (int h = 0; h < 2; h++)
            {
                if (((char)hexData[i + h]) == '0')
                {
                    hexPairInDecimal[h] = 0;
                }
                else if (((char)hexData[i + h]) == '1')
                {
                    hexPairInDecimal[h] = 1;
                }
                else if (((char)hexData[i + h]) == '2')
                {
                    hexPairInDecimal[h] = 2;
                }
                else if (((char)hexData[i + h]) == '3')
                {
                    hexPairInDecimal[h] = 3;
                }
                else if (((char)hexData[i + h]) == '4')
                {
                    hexPairInDecimal[h] = 4;
                }
                else if (((char)hexData[i + h]) == '5')
                {
                    hexPairInDecimal[h] = 5;
                }
                else if (((char)hexData[i + h]) == '6')
                {
                    hexPairInDecimal[h] = 6;
                }
                else if (((char)hexData[i + h]) == '7')
                {
                    hexPairInDecimal[h] = 7;
                }
                else if (((char)hexData[i + h]) == '8')
                {
                    hexPairInDecimal[h] = 8;
                }
                else if (((char)hexData[i + h]) == '9')
                {
                    hexPairInDecimal[h] = 9;
                }
                else if (((char)hexData[i + h]) == 'A' || ((char)hexData[i + h]) == 'a')
                {
                    hexPairInDecimal[h] = 10;
                }
                else if (((char)hexData[i + h]) == 'B' || ((char)hexData[i + h]) == 'b')
                {
                    hexPairInDecimal[h] = 11;
                }
                else if (((char)hexData[i + h]) == 'C' || ((char)hexData[i + h]) == 'c')
                {
                    hexPairInDecimal[h] = 12;
                }
                else if (((char)hexData[i + h]) == 'D' || ((char)hexData[i + h]) == 'd')
                {
                    hexPairInDecimal[h] = 13;
                }
                else if (((char)hexData[i + h]) == 'E' || ((char)hexData[i + h]) == 'e')
                {
                    hexPairInDecimal[h] = 14;
                }
                else if (((char)hexData[i + h]) == 'F' || ((char)hexData[i + h]) == 'f')
                {
                    hexPairInDecimal[h] = 15;
                }
            }

            // Join hex 4 bit(left hex cahr) + 4bit(right hex char) in bytes 8 it
            retVal.WriteByte((byte)((hexPairInDecimal[0] << 4) | hexPairInDecimal[1]));
        }

        return retVal.ToArray();
    }
    /// <summary>
    /// QuotedPrintableDecode
    /// </summary>
    /// <param name="data">byte[]</param>   
    /// <returns>byte[]</returns>
    public static byte[] QuotedPrintableDecode(byte[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        MemoryStream msRetVal = new MemoryStream();
        MemoryStream msSourceStream = new MemoryStream(data);

        int b = msSourceStream.ReadByte();
        while (b > -1)
        {
            // Encoded 8-bit byte(=XX) or soft line break(=CRLF)
            if (b == '=')
            {
                byte[] buffer = new byte[2];
                int nCount = msSourceStream.Read(buffer, 0, 2);
                if (nCount == 2)
                {
                    // Soft line break, line splitted, just skip CRLF
                    if (buffer[0] == '\r' && buffer[1] == '\n')
                    {
                    }
                    // This must be encoded 8-bit byte
                    else
                    {
                        try
                        {
                            msRetVal.Write(FromHex(buffer), 0, 1);
                        }
                        catch
                        {
                            // Illegal value after =, just leave it as is
                            msRetVal.WriteByte((byte)'=');
                            msRetVal.Write(buffer, 0, 2);
                        }
                    }
                }
                // Illegal =, just leave as it is
                else
                {
                    msRetVal.Write(buffer, 0, nCount);
                }
            }
            // Just write back all other bytes
            else
            {
                msRetVal.WriteByte((byte)b);
            }
            // Read next byte
            b = msSourceStream.ReadByte();
        }
        return msRetVal.ToArray();
    }

    /// <summary>
    /// QuotedPrintableDecode
    /// </summary>
    /// <param name="input">string</param>   
    /// <param name="charSet">string</param>   
    /// <returns>string</returns>
    public static string QuotedPrintableDecodeString(string input, string charSet)
    {
        if (string.IsNullOrEmpty(charSet))
        {
            var charSetOccurences = new Regex(@"=\?.*\?Q\?", RegexOptions.IgnoreCase);
            var charSetMatches = charSetOccurences.Matches(input);
            foreach (Match match in charSetMatches)
            {
                charSet = match.Groups[0].Value.Replace("=?", "").Replace("?Q?", "");
                input = input.Replace(match.Groups[0].Value, "").Replace("?=", "");
            }
        }

        Encoding enc = new ASCIIEncoding();
        if (!string.IsNullOrEmpty(charSet))
        {
            try
            {
                enc = Encoding.GetEncoding(charSet);
            }
            catch
            {
                enc = new ASCIIEncoding();
            }
        }

        //decode iso-8859-[0-9]
        var occurences = new Regex(@"=[0-9A-Z]{2}", RegexOptions.Multiline);
        var matches = occurences.Matches(input);
        foreach (Match match in matches)
        {
            try
            {
                byte[] b = new byte[] { byte.Parse(match.Groups[0].Value.Substring(1), System.Globalization.NumberStyles.AllowHexSpecifier) };
                char[] hexChar = enc.GetChars(b);
                input = input.Replace(match.Groups[0].Value, hexChar[0].ToString());
            }
            catch
            { ;}
        }

        //decode base64String (utf-8?B?)
        occurences = new Regex(@"\?utf-8\?B\?.*\?", RegexOptions.IgnoreCase);
        matches = occurences.Matches(input);
        foreach (Match match in matches)
        {
            byte[] b = Convert.FromBase64String(match.Groups[0].Value.Replace("?utf-8?B?", "").Replace("?UTF-8?B?", "").Replace("?", ""));
            string temp = Encoding.UTF8.GetString(b);
            input = input.Replace(match.Groups[0].Value, temp);
        }

        input = input.Replace("=\r\n", "");
        input = input.Replace("\0=", "");

        return input;
    }

    /// <summary>
    /// Compute the distance between two strings with Levenshtein Algorithm.
    /// </summary>
    public static int ComputeDistanceWithLevenshtein(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        // Step 1
        if (n == 0)
        {
            return m;
        }

        if (m == 0)
        {
            return n;
        }

        // Step 2
        for (int i = 0; i <= n; d[i, 0] = i++)
        {
        }

        for (int j = 0; j <= m; d[0, j] = j++)
        {
        }

        // Step 3
        for (int i = 1; i <= n; i++)
        {
            //Step 4
            for (int j = 1; j <= m; j++)
            {
                // Step 5
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                // Step 6
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        // Step 7
        return d[n, m];
    }

    /// <summary>
    /// Quita los ceros de la izquierda de una cadena
    /// </summary>    
    /// <param name="valor">valor</param>        
    /// <returns>cadena sin ceros a la izquierda</returns>
    public static string QuitaCerosIzq(string valor)
    {
        valor = valor.Trim();
        for (int j = 0; j < valor.Length; j++)
        {
            if (valor.Substring(j, 1) != "0")
            {
                valor = valor.Substring(j, valor.Length - j);
                break;
            }
        }
        if (valor.StartsWith(",") || valor.StartsWith("."))
            valor = "0" + valor;
        return valor;
    }

    /// <summary>
    /// Obtener nombre de fichero/directorio SIN el path
    /// </summary>
    /// <param name="fullpath"></param>
    /// <returns>nombre de fichero/directorio SIN el path</returns>
    public static string WithoutPath(string fullpath)
    {
        string tmp = fullpath;
        int ix = tmp.LastIndexOf("\\");   //coger el nombre SIN el path
        if (ix != -1)
            tmp = tmp.Substring(ix + 1);
        return tmp;
    }

    /// <summary>
    /// Tokenize a value
    /// </summary>
    /// <param name="val">value</param>
    /// <param name="delim">delimiter</param>
    /// <param name="position">tokem to reach (1=first)</param>
    /// <returns>the token</returns>
    public static string tokenize(string val, string delim, int position)
    {
        return tokenize(val, delim, position, false);
    }
    public static string tokenize(string val, string delim, int position, bool emptyNonExist)
    {        
        int ix = val.IndexOf(delim);
        int item = 0;
        int delimLen = delim.Length;
        string token = "";
        while (ix != -1)
        {
            if (val.StartsWith("\""))
            {
                if (val.Substring(1, val.Length - 1).IndexOf("\"") > -1)
                    token = val.Substring(1, val.Substring(1, val.Length - 1).IndexOf("\""));
                else
                    token = val.Substring(0, ix);
            }
            else
                token = val.Substring(0, ix);
            item++;
            if (item == position)
                break;
            if (val.StartsWith("\"") && token.Contains(delim))
                val = val.Substring(token.Length + 3);
            else
                val = val.Substring(ix + delimLen);
            ix = val.IndexOf(delim);
        }
        if (!string.IsNullOrEmpty(val) && !val.Contains(delim)) token = val;
        if (token.EndsWith(delim)) token = token.Substring(0, token.Length - delim.Length);
        if (emptyNonExist)
        {
            if (item < position)
                token = "";
        }
        return token;
    }
    
    /// <summary>
    /// Tokenize a value and replace it
    /// </summary>
    /// <param name="val">value</param>
    /// <param name="delim">delimiter</param>
    /// <param name="position">tokem to reach (1=first)</param>
    /// <param name="replace">replace value</param>
    /// <returns>the token</returns>
    public static string tokenizeReplace(string val, string delim, int position, string replaceVal)
    {
        int ix = val.IndexOf(delim);
        int item = 0;
        int delimLen = delim.Length;
        string token = "";
        string result = "";
        while (ix != -1)
        {
            token = val.Substring(0, ix);
            item++;
            if (item == position)
            {
                result = result + replaceVal + delim + val.Substring(ix + delimLen);
                break;
            }
            else
            {
                result = result + token + delim;
                val = val.Substring(ix + delimLen);
                ix = val.IndexOf(delim);
            }
        }
        return result;
    }

    /// <summary>
    /// Eliminar un fichero si el fichero tiene 0 bytes
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    /// <returns>true/false</returns>
    public static bool DeleteIfEmptyFile(string filename)
    {
        bool deleted = false;
        if (System.IO.File.Exists(filename))
        {
            System.IO.FileInfo fi = new System.IO.FileInfo(filename);
            if (fi.Length == 0)
            {
                System.IO.File.Delete(filename);
                deleted = true;
            }
        }
        return deleted;
    }

    /// <summary>
    /// Convierte el nombre de un mes a número de mes
    /// </summary>
    /// <param name="mes">nombre mes</param>
    /// <returns>mes</returns>
    public static int MesToInt(string mes)
    {
        if (mes.Trim().ToLower().StartsWith("ene")) return 1;
        else if (mes.Trim().ToLower().StartsWith("feb")) return 2;
        else if (mes.Trim().ToLower().StartsWith("mar")) return 3;
        else if (mes.Trim().ToLower().StartsWith("abr")) return 4;
        else if (mes.Trim().ToLower().StartsWith("may")) return 5;
        else if (mes.Trim().ToLower().StartsWith("jun")) return 6;
        else if (mes.Trim().ToLower().StartsWith("jul")) return 7;
        else if (mes.Trim().ToLower().StartsWith("ago")) return 8;
        else if (mes.Trim().ToLower().StartsWith("sep")) return 9;
        else if (mes.Trim().ToLower().StartsWith("oct")) return 10;
        else if (mes.Trim().ToLower().StartsWith("nov")) return 11;
        else if (mes.Trim().ToLower().StartsWith("dic")) return 12;
        else return 0;
    }
  }
}
