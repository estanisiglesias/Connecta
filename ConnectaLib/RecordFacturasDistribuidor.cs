using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Xml;

namespace ConnectaLib
{
  /// <summary>
  /// Clase interna que representa un registro
  /// del SIP Facturas distribuidor. Se utiliza como almacenamiento
  /// de cada uno de los registros en formato XML o delimited. De alguna manera
  /// independiza la fuente de entrada de datos a la hora de realizar el
  /// algoritmo de traspaso.
  /// </summary>
  public class RecordFacturasDistribuidor : CommonRecord
  {
    /// <summary>
    /// Mapea fila de formato delimited a valores
    /// </summary>
    /// <param name="row">row</param>
    public override void MapRow(string row) 
    {
      base.MapRow(row);

      StringTokenizer st = new StringTokenizer(row, Globals.GetInstance().GetFieldSeparator(row));
      if (st.HasMoreTokens()) 
      {
        PutValue("NumFactura", st.NextToken());
        PutValue("Ejercicio", st.NextToken());
        PutValue("CodigoCliente", st.NextToken());
        PutValue("FechaFra", st.NextToken());
        PutValue("ImporteBruto", st.NextToken());
        PutValue("Impuestos", st.NextToken());
        PutValue("ImporteTotal", st.NextToken());
        PutValue("CodigoMoneda", st.NextToken());
      }
    }

    //Getters de cada uno de los campos de la entidad   
    public string NumFactura { get { return GetValue("NumFactura"); } }
    public string Ejercicio { get { return GetValue("Ejercicio"); } }
    public string CodigoCliente { get { return GetValue("CodigoCliente"); } }
    public string FechaFra { get { return GetValue("FechaFra"); } }
    public string ImporteBruto { get { return GetValue("ImporteBruto"); } }
    public string Impuestos { get { return GetValue("Impuestos"); } }
    public string ImporteTotal { get { return GetValue("ImporteTotal"); } }
    public string CodigoMoneda { get { return GetValue("CodigoMoneda"); } }
	}
}
