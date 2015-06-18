using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Xml;

namespace ConnectaLib 
{
  /// <summary>
  /// Clase interna que representa un registro
  /// del SIP InProductosFabricante. Se utiliza como almacenamiento
  /// de cada uno de los registros en formato XML o delimited. De alguna manera
  /// independiza la fuente de entrada de datos a la hora de realizar el
  /// algoritmo de traspaso.
  /// </summary>
  public class RecordProductosKitFabricante : CommonRecord
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
          PutValue("CodigoProductoKit", st.NextToken());
          PutValue("CodigoProductoComp", st.NextToken());
          PutValue("CantidadComp", st.NextToken());
          PutValue("UMComp", st.NextToken());
          PutValue("ProporcionImporteComp", st.NextToken());        
      }
    }

    //Getters de cada uno de los campos de la entidad
    public string CodigoProductoKit { get { return GetValue("CodigoProductoKit"); } set { CodigoProductoKit = value; } }
    public string CodigoProductoComp { get { return GetValue("CodigoProductoComp"); } }
    public string CantidadComp { get { return GetValue("CantidadComp"); } }
    public string UMComp { get { return GetValue("UMComp"); } }
    public string ProporcionImporteComp { get { return GetValue("ProporcionImporteComp"); } }   
  }
}
