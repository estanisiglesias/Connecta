using System;
using System.Collections.Generic;
using System.Text;

namespace uveFTPAgent
{
  class Program
  {
    /// <summary>
    /// Punto de entrada para la ejecución desde línea de comandos
    /// </summary>
    static void Main(string[] args)
    {
      uveFTPAgent ftpAgent = new uveFTPAgent();
      ftpAgent.runManual(args);
    }
  }
}
