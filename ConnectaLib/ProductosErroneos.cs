using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectaLib
{
  /// <summary>
  /// Clase que contiene informaci�n de los productos err�neos. Desde el SIP de ProductosFabricante
  /// se mantiene una lista de objetos de esta clase para que en el post-proceso puedan ser
  /// borrados los pedidos (cabeceras+l�neas) que no cumplen las condiciones de validez.
  /// </summary>
  public class ProductosErroneos
  {
    public string Producto = "";

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="prod">producto</param>
    public ProductosErroneos(string prod) 
    {
      Producto = prod;
    }
  }
}
