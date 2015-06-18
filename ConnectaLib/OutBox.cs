using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ConnectaLib
{
  /// <summary>
  /// Clase gestora de la carpeta outbox
  /// </summary>
  public class OutBox
  {
    private string agent = "";
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="agent">agente</param>
    public OutBox(string agent)
    {
      this.agent = agent;
    }
    
    /// <summary>
    /// Backup de un fichero
    /// </summary>
    /// <param name="agent">agente</param>
    /// <param name="sipTypeName">nombre de sip</param>
    /// <param name="filename">nombre de fichero</param>
    public void Backup(string agent, string sipTypeName, string filename) 
    {
      if (!new Backup().DoBackup(agent, sipTypeName, filename))
        throw new Exception("Backup failed! agent=" + agent + ",filename=" + filename);
    }

    /// <summary>
    /// Carpeta de outbox
    /// </summary>
    /// <param name="agent">agent</param>
    /// <returns>carpeta de outbiox</returns>
    public string GetOutBoxFolder(string agent)
    {
      string path = Globals.GetInstance().GetOutBoxPath();
      if (!Directory.Exists(path))
        Directory.CreateDirectory(path);

      string outbox = path + "\\" + Constants.AGENT_ID + agent;
      if (!Directory.Exists(outbox))
        Directory.CreateDirectory(outbox);
      return outbox;
    }
  }
}
