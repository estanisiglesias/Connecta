using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Xml;

namespace ConnectaLib
{
  /// <summary>
  /// Clase interna que representa un registro
  /// del SIP InProductosDistribuidor. Se utiliza como almacenamiento
  /// de cada uno de los registros en formato XML o delimited. De alguna manera
  /// independiza la fuente de entrada de datos a la hora de realizar el
  /// algoritmo de traspaso.
  /// </summary>
  public class RecordProductosDistribuidor : CommonRecord
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
            string sAux = "";

            PutValue("CodigoProducto", st.NextToken());
            PutValue("Descripcion" , st.NextToken());
            PutValue("Status" , st.NextToken());
            PutValue("UnidadMedida" , st.NextToken());
            PutValue("Clasificacion1" , st.NextToken());
            PutValue("Clasificacion2" , st.NextToken());
            PutValue("Clasificacion3" , st.NextToken());
            PutValue("Clasificacion4" , st.NextToken());
            PutValue("Clasificacion5" , st.NextToken());
            PutValue("Clasificacion6" , st.NextToken());
            PutValue("Clasificacion7" , st.NextToken());
            PutValue("Clasificacion8" , st.NextToken());
            PutValue("Clasificacion9" , st.NextToken());
            PutValue("Clasificacion10" , st.NextToken());
            PutValue("Clasificacion11" , st.NextToken());
            PutValue("Clasificacion12" , st.NextToken());
            PutValue("Clasificacion13" , st.NextToken());
            PutValue("Clasificacion14" , st.NextToken());
            PutValue("Jerarquia" , st.NextToken());
            PutValue("EAN13" , st.NextToken());
            PutValue("Fabricante" , st.NextToken());
            sAux = st.NextToken();
            if (sAux.Length > 18)
                sAux = sAux.Remove(18);
            PutValue("CodigoProdFab" , sAux);
            PutValue("EsKit", st.NextToken());
        }
    }

    //Getters de cada uno de los campos de la entidad
    public string CodigoProducto
    {
      get { return GetValue("CodigoProducto"); }
      set { PutValue("CodigoProducto", value); }
    }

    public string Descripcion 
    {
      get { return GetValue("Descripcion"); }
    }

    public string Status    
    {
      get { return GetValue("Status"); }
    }

    public string UnidadMedida    
    {
      get { return GetValue("UnidadMedida"); }
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

    public string Clasificacion5    
    {
      get { return GetValue("Clasificacion5"); }
    }

    public string Clasificacion6    
    {
      get { return GetValue("Clasificacion6"); }
    }

    public string Clasificacion7     
    {
      get { return GetValue("Clasificacion7"); }
    }

    public string Clasificacion8     
    {
      get { return GetValue("Clasificacion8"); }
    }

    public string Clasificacion9     
    {
      get { return GetValue("Clasificacion9"); }
    }

    public string Clasificacion10    
    {
      get { return GetValue("Clasificacion10"); }
    }

    public string Clasificacion11    
    {
      get { return GetValue("Clasificacion11"); }
    }

    public string Clasificacion12    
    {
      get { return GetValue("Clasificacion12"); }
    }

    public string Clasificacion13     
    {
      get { return GetValue("Clasificacion13"); }
    }

    public string Clasificacion14     
    {
        get { return GetValue("Clasificacion14"); }
    }

    public string Jerarquia    
    {
        get { return GetValue("Jerarquia"); }
    }

    public string EAN13    
    {
        get { return GetValue("EAN13"); }
        set { PutValue("EAN13", value); }
    }

    public string Fabricante    
    {
        get { return GetValue("Fabricante"); }
        set { PutValue("Fabricante", value); }
    }

    public string CodigoProdFab     
    {
        get { return GetValue("CodigoProdFab"); }
    }

    public string EsKit    
    {
        get { return GetValue("EsKit"); }
    }
  }
}
