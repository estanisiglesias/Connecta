using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Data.Common;

namespace ConnectaLib
{
  /// <summary>
  /// Gestión de backups de ficheros recibidos en inbox
  /// </summary>
  public class Backup
  {
    private string backupFilename = "";

    /// <summary>
    /// Realiza el backup de un fichero
    /// </summary>
    /// <param name="agent">agent</param>
    /// <param name="sipTypeName">tipo de sip</param>
    /// <param name="filename">filename</param>
    /// <returns>true si todo es correcto</returns>
    public bool DoBackup(string agent, string sipTypeName, string filename) 
    {
        bool ok = true;
        string agentName = "";
        Database db = Globals.GetInstance().GetDatabase();
        DbDataReader reader = null;
        string sql = "";

        try
        {
            //Obtener el directorio de backup
            string dir = Globals.GetInstance().GetBackupBoxPath();
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            dir += "\\" + Constants.AGENT_ID + agent;

            sql = "SELECT Nombre FROM Agentes Where IdcAgente = " + agent;
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                agentName = db.GetFieldValue(reader, 0);
            }
            reader.Close();
            reader = null;
            if (agentName.Equals(""))
                agentName = IniManager.INIGetKeyValue(IniManager.CFG_SECTION_AGENTS, Constants.AGENT_ID + agent, IniManager.GetIniFile());
            if (agentName.Equals(""))
                agentName = IniManager.INIGetKeyValue(IniManager.CFG_SECTION_AGENTS, "idAgent" + agent, IniManager.GetIniFile());
            if (agentName.Equals(""))
                agentName = "unknown";

            dir = dir + " (" + agentName + ")";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string singleFileName = filename;
            int ix = singleFileName.LastIndexOf("\\");
            if (ix != -1)
                singleFileName = singleFileName.Substring(ix + 1);

            DateTime dt = DateTime.Now;
            string now = dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
            now = now.Replace(" ", "-");
            now = now.Replace(":", "-");

            backupFilename = dir + "\\" + now + "_" + singleFileName;
            if (System.IO.File.Exists(filename)) File.Copy(filename, backupFilename);
        }
        catch (Exception e)
        {
            ok = false;
            Globals.GetInstance().GetLog2().Error(agent, sipTypeName, e);
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return ok;
    }

    /// <summary>
    /// Realiza el backup de un documento XML
    /// </summary>
    /// <param name="agent">agent</param>
    /// <param name="sipTypeName">tipo de sip</param>
    /// <param name="xmlDoc">documento XML</param>
    /// <returns>true si todo es correcto</returns>
    public bool DoBackup(string agent, string sipTypeName, string sipId, System.Xml.XmlDocument xmlDoc)
    {
      //Creamos un fichero temporal
      string tempFile = null;

      bool ok = true;
      try
      {
        tempFile = System.IO.Path.GetTempPath() + "\\" + sipId + ".xml"; 
        StreamWriter writer = new StreamWriter(tempFile);
        writer.Write(xmlDoc.OuterXml);
        writer.Close();

        ok = DoBackup(agent, sipTypeName, tempFile);
      }
      catch (Exception e) 
      {
        ok = false;
        throw e;
      }
	    finally
	    {
        if (tempFile != null && File.Exists(tempFile))
          File.Delete(tempFile);
	    }
      return ok;
    }

    /// <summary>
    /// Get backup filename
    /// </summary>
    /// <returns>backup filename</returns>
    public string BackupFilename() 
    {
      string s = backupFilename;
      int ix = s.LastIndexOf("\\");
      if(ix!=-1)
        s=s.Substring(ix+1);
      return s;
    }

    /// <summary>
    /// Get backup filename with path
    /// </summary>
    /// <returns>backup filename</returns>
    public string BackupFilenamePath()
    {
        return backupFilename;
    }
  }
}
