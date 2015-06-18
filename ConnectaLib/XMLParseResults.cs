using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectaLib
{
  /// <summary>
  /// Clase que retorna los resultados de aplicar el proceso de
  /// parsing sobre un fichero XML.
  /// </summary>
  public class XMLParseResults
  {
    private bool formatOK = true;
    private int numRows = 0;
    
    /// <summary>
    /// Formato OK
    /// </summary>
    public bool FormatOK
    {
      get { return formatOK; }
      set { formatOK = value; }
    }

    /// <summary>
    /// Número de filas
    /// </summary>
    public int NumRows
    {
      get { return numRows; }
      set { numRows = value; }
    }
  }
}
