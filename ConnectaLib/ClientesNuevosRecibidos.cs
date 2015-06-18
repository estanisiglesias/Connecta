using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectaLib
{
  /// <summary>
  /// Clase que contiene información resumida de los albaranes recibidos. 
  /// Desde el SIP de albaranes de distribuidor
  /// se mantiene una lista de objetos de esta clase para que en el post-proceso 
  /// se pueda construir un resumen que será enviado por e-mail.
  /// </summary>
  public class ClienteNuevoRecibido
  {
    public string codigo = "";
    public string nombre = "";
    public string razonSocial = "";    
    public string direccion = "";
    public string poblacion = "";
    public string cp = "";    

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="numAlb">número de albarán</param>
    /// <param name="ejer">ejercicio</param>
    /// <param name="fab">fabricante</param>
    public ClienteNuevoRecibido(string pCodigo, string pNombre, string pRazonSocial, string pDireccion, string pPoblacion, string pCp) 
    {
        codigo = pCodigo;
        nombre = pNombre;
        razonSocial = pRazonSocial;        
        direccion = pDireccion;
        poblacion = pPoblacion;
        cp = pCp;        
    }
  }
}
