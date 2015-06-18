using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectaLib
{
  /// <summary>
  /// Un interfaz a implementar por las clases Sip de actualización (SipUpd*).
  /// </summary>
  public interface ISipUpdInterface
  {
    /// <summary>
    /// Obtener id de sip
    /// </summary>
    /// <returns>id de sip</returns>
    string GetId();

    /// <summary>
    /// Obtener nombre de Sip (básicamente a efectos de log)
    /// </summary>
    /// <returns>nombre de sip</returns>
    string GetSipTypeName();

    /// <summary>
    /// Lanzar proceso de salida
    /// </summary>
    /// <param name="args">argumentos</param>
    /// <returns>resultados del proceso</returns>
    string Process(string[] args);

    /// <summary>
    /// Invocar a post proceso
    /// </summary>
    /// <param name="agente">agente</param> 
    void PreProcess(string agent);

    /// <summary>
    /// Invocar a post proceso
    /// </summary>
    /// <param name="agente">agente</param> 
    void PostProcess(string agent);
  }
}
