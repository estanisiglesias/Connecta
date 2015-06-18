using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ConnectaLib
{
  /// <summary>
  /// Clase gestora de la carpeta workbox
  /// </summary>
  public class WorkBox
  {
    /// <summary>
    /// Carpeta de workbox
    /// </summary>
    /// <returns>carpeta de workbox</returns>
    public string GetWorkBoxFolder()
    {
        string path = Globals.GetInstance().GetWorkBoxPath();
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }
  }
}
