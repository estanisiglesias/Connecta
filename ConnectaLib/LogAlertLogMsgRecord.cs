using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectaLib
{
  /// <summary>
  /// Clase que contiene un registro de datos de configuración de un mensaje de AlertLog
  /// </summary>
  public class LogAlertLogMsgRecord
  {
      public string sCodigoAlerta = "";
      public string sDescripcion = "";
      public string sNivelAlerta = "";
      public string sOrquestacion = "";
      public string sUsuarioResolucion = "";
      public string sResuelto = "";

    /// <summary>
    /// Constructor
    /// </summary>
    public LogAlertLogMsgRecord(string codalerta, string descrip, string nivalerta, string orquest, string usuresol, string resuelto) 
    {
        sCodigoAlerta = codalerta;
        sDescripcion = descrip;
        sNivelAlerta = nivalerta;
        sOrquestacion = orquest;
        sUsuarioResolucion = usuresol;
        sResuelto = resuelto;
    }
  }
}
