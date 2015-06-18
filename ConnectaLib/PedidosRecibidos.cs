using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectaLib
{
  /// <summary>
  /// Clase que contiene información resumida de los pedidos recibidos. 
  /// Desde el SIP de reaprovisionamientos de distribuidor
  /// se mantiene una lista de objetos de esta clase para que en el post-proceso 
  /// se pueda construir un resumen que será enviado por e-mail.
  /// </summary>
  public class PedidoRecibido
  {
    public string idcDistribuidor = "";
    public string Pedido = "";
    public string idcFabricante = "";
    public string Distribuidor = "";
    public string NombreDist = "";
    public string Cliente = "";
    public string NombreCli = "";
    public string Fecha = "";
    public string Estado = "";
    public string EstadoDesc = "";

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="numAlb">número de pedido</param>
    /// <param name="fab">fabricante</param>
    public PedidoRecibido(string dist, string numPed, string fab, string codDist, string nomDist, string codCli, string nomCli, string fec, string estado, string estadoDesc) 
    {
        idcDistribuidor = dist;
        Pedido = numPed;
        idcFabricante = fab;
        Distribuidor = codDist;
        NombreDist = nomDist;
        Cliente = codCli;
        NombreCli = nomCli;
        Fecha = fec;
        Estado = estado;
        EstadoDesc = estadoDesc;
    }
  }
}
