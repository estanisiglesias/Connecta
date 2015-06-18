using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectaLib
{
  /// <summary>
  /// Clase que contiene información de los pedidos erróneos. Desde el SIP de PedidosFabricante
  /// se mantiene una lista de objetos de esta clase para que en el post-proceso puedan ser
  /// borrados los pedidos (cabeceras+líneas) que no cumplen las condiciones de validez.
  /// </summary>
  public class PedidosErroneos
  {
    public string NumPedido = "";
    public string Ejercicio = "";
    public string Contador = "";
    public string Fabricante = "";

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="nump">número de pedido</param>
    /// <param name="ejer">ejercicio</param>
    public PedidosErroneos(string nump, string ejer, string cont) 
    {
        NumPedido = nump;
        Ejercicio = ejer;
        Contador = cont;
        Fabricante = "";
    }
    public PedidosErroneos(string nump, string ejer)
    {
        NumPedido = nump;
        Ejercicio = ejer;
        Contador = "";
        Fabricante = "";
    }
    public PedidosErroneos(string nump, int fab)
    {
        NumPedido = nump;
        Ejercicio = "";
        Contador = "";
        Fabricante = fab.ToString();
    }
  }
}
