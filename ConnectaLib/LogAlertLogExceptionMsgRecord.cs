using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectaLib
{
  /// <summary>
  /// Clase que contiene un registro de datos de configuración de un mensaje de AlertLog
  /// </summary>
  public class LogAlertLogExceptionMsgRecord
  {
      public string sIdcAgenteOrigen = "";
      public string sIdcAgenteDestino = "";
      public string sCodigoAlerta = "";     
      public string sNivelAlerta = "";      
      public string sUsuarioResolucion = "";
      public string sResuelto = "";

    /// <summary>
    /// Constructor
    /// </summary>
      public LogAlertLogExceptionMsgRecord(string idcagenteorigen, string idcagentedestino, string codalerta, string nivalerta, string usuresol, string resuelto) 
    {
        sIdcAgenteOrigen = idcagenteorigen;
        sIdcAgenteDestino = idcagentedestino;
        sCodigoAlerta = codalerta;        
        sNivelAlerta = nivalerta;        
        sUsuarioResolucion = usuresol;
        sResuelto = resuelto;
    }
  }
}
