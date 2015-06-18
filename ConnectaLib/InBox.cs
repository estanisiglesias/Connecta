using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ConnectaLib
{
  /// <summary>
  /// Clase gestora de la carpeta inbox
  /// </summary>
  public class InBox
  {
    private string agent = "";
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="agent">agente</param>
    public InBox(string agent)
    {
      this.agent = agent;
    }
    
    /// <summary>
    /// Backup de un fichero del inbox
    /// </summary>
    /// <param name="agent">agente</param>
    /// <param name="sipTypeName">nombre de sip</param>
    /// <param name="filename">nombre de fichero</param>
    /// <returns>nombre de fichero de backuo</returns>
    public string Backup(string agent, string sipTypeName, string filename) 
    {
      Backup backup = new Backup();
      if (!backup.DoBackup(agent, sipTypeName, filename))
        throw new Exception("Backup failed! agent=" + agent + ",filename=" + filename);
      return backup.BackupFilename();
    }

    /// <summary>
    /// Obtener lista de ficheros de la carpeta
    /// </summary>
    /// <returns></returns>
    public string[] GetFilesToUpload()
    {
      //En caso contrario, se recorren todos los ficheros del agente
      string folder = GetInBoxFolder(agent);
      string[] files = null;
      if (Directory.Exists(folder))
        files = Directory.GetFiles(folder, "*.*");
      return files;
    }

    /// <summary>
    /// Get the inbox folder name
    /// </summary>
    /// <param name="agent">agent</param>
    /// <returns>Inbox folder name</returns>
    public string GetInBoxFolder(string agent)
    {
      return Globals.GetInstance().GetInBoxPath() + "\\" + Constants.AGENT_ID + agent;
    }

    /// <summary>
    /// Borrar un fichero del inbox
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    public void Delete(string filename)
    {
      File.Delete(filename);
    }

    /// <summary>
    /// Copiar un fichero externo
    /// </summary>
    /// <param name="filename">nombre de fichero (ruta completa)</param>
    /// <return>nombre de fichero en la carpeta inbox</return>
    public string CopyToInbox(string filename)
    {
      string folder = GetInBoxFolder(agent);
      DateTime dt = DateTime.Now;
      string now = dt.ToString("yyyyMMddHHmmss");
      int ix = filename.LastIndexOf("\\");
      string target = filename;
      if (ix != -1)
      {
        target = target.Substring(ix + 1);
      }
      string inboxFile = "";
      ix = target.LastIndexOf(".");
      if (ix != -1)
        inboxFile = target.Substring(0, ix) + "."+ now + target.Substring(ix);
      else
        inboxFile = now + "." + target;

      if (!Directory.Exists(folder))
        Directory.CreateDirectory(folder);
      File.Copy(filename, folder + "\\" +inboxFile);

      //Devolver el nombre del fichero SIN la carpeta de Inbox
      return inboxFile;
    }
  }
}
