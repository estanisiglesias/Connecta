using System;
using System.Collections.Generic;
using System.Text;

namespace uveFTPAgent
{
  class Program
  {
    /// <summary>
    /// Punto de entrada para la ejecuci�n desde l�nea de comandos
    /// </summary>
    static void Main(string[] args)
    {
      uveFTPAgent ftpAgent = new uveFTPAgent();
      ftpAgent.runManual(args);
    }
  }
}
