using System;
using System.Collections.Generic;
using System.Text;

namespace uveIntegrator
{
  /// <summary>
  /// Punto de entrada para la ejecuci�n desde l�nea de comandos
  /// </summary>
  class Program
  {
    static void Main(string[] args)
    {
        uveIntegrator u = new uveIntegrator();        
        u.runManual(args);
        //u.runAuto(args); // para debugar la ejecuci�n autom�tica
    }
  }
}
