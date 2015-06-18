using System;
using System.Collections.Generic;
using System.Text;
using System.Collections; 
using System.Xml;

namespace ConnectaLib
{
  /// <summary>
  /// Clase interna que representa un registro
  /// del SIP Data. Se utiliza como almacenamiento
  /// de cada uno de los registros en formato XML o delimited. De alguna manera
  /// independiza la fuente de entrada de datos a la hora de realizar el
  /// algoritmo de traspaso.
  /// </summary>
  public class RecordActividadesAgente : CommonRecord
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
          PutValue("CodigoComercial", st.NextToken());
          PutValue("FechaActividad", st.NextToken());
          PutValue("CodigoCliente", st.NextToken());          
          PutValue("TipoActividad", st.NextToken());
          PutValue("Formato", st.NextToken());
          PutValue("Resultado", st.NextToken());
          PutValue("Notas", st.NextToken());
          PutValue("FechaFinActividad", st.NextToken());
          PutValue("Status", st.NextToken());  
      }
    }

    //Getters de cada uno de los campos de la entidad   
    public string CodigoComercial
    {
        get { return GetValue("CodigoComercial"); }
    }
    public string FechaActividad
    {
        get { return GetValue("FechaActividad"); }
    }
    public string CodigoCliente
    {
        get { return GetValue("CodigoCliente"); }
    }
    public string TipoActividad
    {
        get { return GetValue("TipoActividad"); }
    }    
    public string Formato
    {
        get { return GetValue("Formato"); }
    }
    public string Resultado
    {
        get { return GetValue("Resultado"); }
    }
    public string Notas
    {
        get { return GetValue("Notas"); }
    }
    public string FechaFinActividad
    {
        get { return GetValue("FechaFinActividad"); }
    }
    public string Status
    {
        get { return GetValue("Status"); }
    }    
  }
}
