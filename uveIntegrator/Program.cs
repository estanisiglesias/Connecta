using System;
using System.Collections.Generic;
using System.Text;

namespace uveIntegrator
{
  /// <summary>
  /// Punto de entrada para la ejecución desde línea de comandos
  /// </summary>
  class Program
  {
    static void Main(string[] args)
    {
        uveIntegrator u = new uveIntegrator();        
        u.runManual(args);
        //u.runAuto(args); // para debugar la ejecución automàtica
    }
  }
}
