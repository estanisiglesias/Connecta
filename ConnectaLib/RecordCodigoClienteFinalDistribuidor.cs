using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Xml;

namespace ConnectaLib
{
  /// <summary>
  /// Clase interna que representa un registro
  /// del SIP ClienteFinalDistribuidor. Se utiliza como almacenamiento
  /// de cada uno de los registros en formato XML o delimited. De alguna manera
  /// independiza la fuente de entrada de datos a la hora de realizar el
  /// algoritmo de traspaso.
  /// </summary>
  public class RecordCodigoClienteFinalDistribuidor : CommonRecord
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
        PutValue("CodigoCliente", st.NextToken());
        PutValue("CodigoFabricante", st.NextToken());
        PutValue("CodigoCliFab", st.NextToken());
        PutValue("CIF", st.NextToken());
        PutValue("Clasificacion1", st.NextToken());
        PutValue("Clasificacion2", st.NextToken());
        PutValue("Clasificacion3", st.NextToken());
        PutValue("Clasificacion4", st.NextToken());
        PutValue("Libre1", st.NextToken());
      }
    }

    //Getters de cada uno de los campos de la entidad   
    public string CodigoCliente
    {
      get { return GetValue("CodigoCliente"); }
    }

    public string CodigoFabricante 
    {
      get { return GetValue("CodigoFabricante"); }
    }

    public string CodigoCliFab    
    {
      get { return GetValue("CodigoCliFab"); }
    }

    public string CIF    
    {
      get { return GetValue("CIF"); }
    }

    public string Clasificacion1  
    {
      get { return GetValue("Clasificacion1"); }
    }

    public string Clasificacion2   
    {
      get { return GetValue("Clasificacion2"); }
    }

    public string Clasificacion3 
    {
      get { return GetValue("Clasificacion3"); }
    }

    public string Clasificacion4
    {
      get { return GetValue("Clasificacion4"); }
    }

    public string Libre1
    {
      get { return GetValue("Libre1"); }
    }
  }
}
