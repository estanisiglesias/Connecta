using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectaLib
{
  /// <summary>
  /// Clase que contiene información resumida de las facturas recibidas. 
  /// Desde el SIP de facturas de distribuidor
  /// se mantiene una lista de objetos de esta clase para que en el post-proceso 
  /// se pueda construir un resumen que será enviado por e-mail.
  /// </summary>
  public class FacturaRecibida
  {
    public string idcDistribuidor = "";
    public string idcFabricante = "";
    public string codFabricante = "";
    public string nomFabricante = "";
    public int contador = 0;

    /// <summary>
    /// Constructor
    /// </summary>
    public FacturaRecibida(string dist, string fab, string codFab, string nomFab) 
    {
        idcDistribuidor = dist;
        idcFabricante = fab;
        codFabricante = codFab;
        nomFabricante = nomFab;
        contador = 1;
    }
  }
}
