using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectaLib
{
  /// <summary>
  /// Resultados del cálculo de la unidad de medida
  /// </summary>
  public class CalcUnidadMedida
  {
    public bool isOK = false;
    public double cantidad = 0;

    public CalcUnidadMedida(bool res, double cnt)
    {
      isOK = res;
      cantidad = cnt;
    }
  }
}
