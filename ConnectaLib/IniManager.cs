using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace ConnectaLib
{
  /// <summary>
  /// Gestor de INI's
  /// </summary>
  public class IniManager
  {
    //Constantes
    public const string CFG_SECTION_PATH = "Path";
    public const string CFG_SECTION_NOMENCLATOR = "Nomenclators";
    public const string CFG_SECTION_AGENTS = "Agents";
    public const string CFG_SECTION_EXTERNAL_PROGRAMS = "ExternalPrograms";
    public const string CFG_SECTION_FTPAGENT = "FtpAgent";
    public const string CFG_SECTION_MAILAGENT = "MailAgent";
    public const string CFG_SECTION_FILEBREAKER = "FileBreaker";
    public const string CFG_SECTION_FILEBREAKERBULK = "FileBreakerBulkMode";
    public const string CFG_SECTION = "Cfg";

    public const string CFG_INBOXPATH = "inBoxPath";
    public const string CFG_OUTBOXPATH = "outBoxPath";
    public const string CFG_WORKBOXPATH = "workBoxPath";
    public const string CFG_BACKUPBOXPATH = "backupBoxPath";
    public const string CFG_LOGBOXPATH = "logBoxPath";

    public const string CFG_DATASOURCE = "Data_Source";
    public const string CFG_USERID = "User_Id";
    public const string CFG_PWD = "Password";
    public const string CFG_CATALOG = "InitialCatalog";
    public const string CFG_XSDPATH = "xsdPath";
    public const string CFG_NOMENCLATORS = "Nomenclators";
    public const string CFG_PROVIDER = "Provider";
    public const string CFG_FIELD_SEPARATOR = "FieldSeparator";
    public const string CFG_FIELD_SEPARATOR2 = "FieldSeparator2";
    public const string CFG_LOGMODE = "LogMode";
    public const string CFG_INSERT_NEW_MESSAGES = "InsertNewMessages";
    public const string CFG_AUTOMODESLEEPTIME = "AutoModeSleepTime";
    public const string CFG_ADMINPORT = "AdminPort";
    public const string CFG_TEMPLATEDIR = "TemplateDir";
    public const string CFG_DATETIMEDIFFFORMAT = "datetimediffformat";
    public const string CFG_DATASOURCEMAPPING = "Data_Source_Mapping";
    public const string CFG_SMTP_SERVER= "smtpServer";
    public const string CFG_SMTP_AUTHENTICATION = "smtpAuthentication";
    public const string CFG_SMTP_USER = "smtpUser";
    public const string CFG_SMTP_PWRD = "smtpPassword";
    public const string CFG_SMTP_ADMIN_FROM = "smtpAdminFromAddress";
    public const string CFG_SMTP_ADMIN_TO = "smtpAdminToAddress";
    public const string CFG_SMTP_ADMIN_CC = "smtpAdminCCAddress";
      
    public const string CFG_ALBPC0_FROM = "ALBPC0emailFrom";
    public const string CFG_ALBPC0_TO = "ALBPC0emailTo";

    public const string CFG_AGENTS_IN_TEST = "InTest";

    public const string CFG_FTPAGENT_SLOTCHECK_PERIOD = "SlotCheckPeriod";
    public const string CFG_FTPAGENT_PATH = "Path";
    public const string CFG_FTPAGENT_CHECKFORMAT = "CheckFormat";
    public const string CFG_FTPAGENT_EXTERNALPGM = "ExternalProgram";
    public const string CFG_FTPAGENT_FILESIZEDELAY = "FileSizeDelay";

    public const string CFG_MAILAGENT_PERIOD = "Period";
    public const string CFG_MAILAGENT_TIMEOUT = "RecieveTimeout";
    public const string CFG_MAILAGENT_SERVER = "popServer";
    public const string CFG_MAILAGENT_PORT = "popPort";
    public const string CFG_MAILAGENT_USER = "popUser";
    public const string CFG_MAILAGENT_PASSWORD = "popPassword";            
    public const string CFG_MAILAGENT_TAGVERIFICATION = "TagVerification";
    public const string CFG_MAILAGENT_FORWARDTO = "ForwardTo";    

    //Localización del fichero de configuración
    private static string iniFile = System.Environment.CurrentDirectory + "\\connecta.ini";

    /// <summary>
    /// Constructor
    /// </summary>
    public IniManager()
    {
    }

    /// <summary>
    /// Get the default ini file
    /// </summary>
    /// <returns>the default ini file</returns>
    public static string GetIniFile()
    {
      return iniFile;
    }

    /// <summary>
    /// Set the default ini file
    /// </summary>
    /// <param name="filename">the default ini file</param>
    public static void SetIniFile(string filename)
    {
      iniFile = filename;
    }

    /// <summary>
    /// Returns the value of a key in a section of an INI file
    /// </summary>
    /// <param name="strSection">String containing section name</param>
    /// <param name="strKeyName">String containing key name</param>
    /// <param name="strFileName">Full path and file name of INI</param>
    /// <param name="defaultValue">default value</param>
    public static string INIGetKeyValue(string strSection, string
      strKeyName, string strFileName, string defaultValue)
    {
      int intReturn = 0;
      int BufferSize = 4096;
      if (strKeyName == null)
      {
        char []lpReturn = new char[BufferSize];
        intReturn = (int)GetPrivateProfileString(strSection, strKeyName, "",
          lpReturn, (uint)BufferSize, strFileName);
        if (intReturn == 0)
        {
          return defaultValue;
        }
        return new string(lpReturn);
      }
      else
      {
        StringBuilder lpReturn = new StringBuilder(BufferSize);
        intReturn = GetPrivateProfileString(strSection, strKeyName, "",
          lpReturn, BufferSize, strFileName);
        if (intReturn == 0)
        {
          return defaultValue;
        }
        return lpReturn.ToString();
      }
    }

    /// <summary>
    /// Returns the value of a key in a section of an INI file
    /// </summary>
    /// <param name="strSection">String containing section name</param>
    /// <param name="strKeyName">String containing key name</param>
    /// <param name="strFileName">Full path and file name of INI
    public static string INIGetKeyValue(string strSection, string
      strKeyName, string strFileName)
    {
      return INIGetKeyValue(strSection, strKeyName, strFileName, "");
    }

    /// <summary>
    /// Writes a key/value pair to a section of an INI file
    /// </summary>
    /// <param name="strSection">String containing section name</param>
    /// <param name="strKeyName">String containing key name</param>
    /// <param name="strKeyValue">String containing key value</param>
    /// <param name="strFileName">Full path and file name of INI
    public static bool INIWriteKey(string strSection, string strKeyName,
      string strKeyValue, string strFileName)
    {
      int intReturn = WritePrivateProfileString(strSection, strKeyName,
        strKeyValue, strFileName);

      if (intReturn == 0)
        return false;
      else
        return true;
    }

    // GetPrivateProfileString
    [DllImport("kernel32.DLL", EntryPoint = "GetPrivateProfileString",
       SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetPrivateProfileString(string
      lpSectionName, string lpKeyName, string lpDefault, StringBuilder
      lpReturnedString, int nSize, string lpFileName);

    [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileString",
       SetLastError = true, CharSet = CharSet.Auto)]
    public static extern uint GetPrivateProfileString(
       string lpAppName,
       string lpKeyName,
       string lpDefault,
       [In, Out] char[] lpReturnedString,
       uint nSize,
       string lpFileName);

    // WritePrivateProfileString
    [DllImport("kernel32.DLL", EntryPoint = "WritePrivateProfileStringA",
       SetLastError = true, CallingConvention = CallingConvention.StdCall)]
    public static extern int WritePrivateProfileString(string
      lpSectionName, string lpKeyName, string lpKeyValue, string lpFileName);

  }
}
