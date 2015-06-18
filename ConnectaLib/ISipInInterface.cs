using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectaLib
{
  /// <summary>
  /// Un interfaz a implementar por las clases Sip de entrada (SipIn*). Al usar este interface, todo el proceso de an�lisis del fichero
  /// se puede independizar ya que se delega cada uno de los pasos al SIP correspondiente.
  /// </summary>
  public interface ISipInInterface
  {
    /// <summary>
    /// Obtener id de sip
    /// </summary>
    /// <returns>id de sip</returns>
    string GetId();

    /// <summary>
    /// Obtener XSD de validaci�n de formato XML
    /// </summary>
    /// <returns>XSD de validaci�n de formato XML</returns>
    string GetXSD();

    /// <summary>
    /// Procesar de un objeto interno que representa un registro de un
    /// fichero delimited o una secci�n XML
    /// </summary>
    /// <param name="rec">registro</param>
    void ProcessMaster(CommonRecord rec);

    /// <summary>
    /// Procesar de un objeto interno que representa un registro de un
    /// fichero delimited o una secci�n XML
    /// </summary>
    /// <param name="rec">registro</param>
    void ProcessSlave(CommonRecord rec);

    /// <summary>
    /// Procesar de un fichero de forma masiva
    /// </summary>
    /// <param name="rec">registro</param>
    void ProcessBulk(string filename, CommonRecord rec);

      /// <summary>
    /// Pre-proceso
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    void PreProcess(string filename);    

    /// <summary>
    /// Post-proceso
    /// </summary>
    /// <param name="filename">nombre de fichero</param>
    void PostProcess(string filename);

    /// <summary>
    /// Obtener nomenclator del sip que act�a como maestro (el primero en la
    /// definici�n del ".ini")
    /// </summary>
    /// <returns>nomenclator</returns>
    string GetMasterNomenclator();

    /// <summary>
    /// Obtener nomenclator del sip que act�a como esclavo (el segundo en la
    /// definici�n del ".ini")
    /// </summary>
    /// <returns>nomenclator</returns>
    string GetSlaveNomenclator();

    /// <summary>
    /// Obtener registro maestro
    /// </summary>
    /// <returns>registro maestro</returns>
    CommonRecord GetMasterRecord();

    /// <summary>
    /// Obtener registro esclavo
    /// </summary>
    /// <returns>registro esclavo</returns>
    CommonRecord GetSlaveRecord();

    /// <summary>
    /// Obtener secci�n de XML para el registro maestro
    /// </summary>
    /// <returns>secci�n de XML</returns>
    string GetMasterXMLSection();

    /// <summary>
    /// Obtener secci�n de XML para el registro esclavo
    /// </summary>
    /// <returns>secci�n de XML</returns>
    string GetSlaveXMLSection();

    /// <summary>
    /// Obtener nombre de Sip (b�sicamente a efectos de log)
    /// </summary>
    /// <returns>nombre de sip</returns>
    string GetSipTypeName();

    /// <summary>
    /// Ajustar el nomenclator. Puede ser variable ya que el fichero de configuraci�n
    /// permite m�s de un valor para cada sip.
    /// </summary>
    /// <param name="nomenclator">nomenclator</param>
    void SetNomenclator(string nomenclator);

    /// <summary>
    /// Validar el formato de un registro
    /// </summary>
    /// <param name="rec">registro</param>
    bool ValidateMaster(CommonRecord rec);

    /// <summary>
    /// Validar el formato de un registro
    /// </summary>
    /// <param name="rec">registro</param>
    bool ValidateSlave(CommonRecord rec);

  }
}
