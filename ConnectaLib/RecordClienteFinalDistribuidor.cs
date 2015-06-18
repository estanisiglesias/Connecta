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
  public class RecordClienteFinalDistribuidor : CommonRecord
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
        PutValue("Nombre", st.NextToken());
        PutValue("RazonSocial", st.NextToken());
        PutValue("CIF", st.NextToken());
        PutValue("Direccion", st.NextToken());
        PutValue("Tipo Calle", st.NextToken());
        PutValue("Calle", st.NextToken());
        PutValue("Numero", st.NextToken());
        PutValue("Poblacion", st.NextToken());
        PutValue("CodigoPostal", st.NextToken());
        PutValue("CodigoPais", st.NextToken());
        PutValue("PaginaWEB", st.NextToken());
        PutValue("Telefono1", st.NextToken());
        PutValue("Telefono2", st.NextToken());
        PutValue("FAX", st.NextToken());
        PutValue("Email", st.NextToken());
        PutValue("PersonaContacto", st.NextToken());
        PutValue("Status", st.NextToken());
        PutValue("Clasificacion1", st.NextToken());
        PutValue("Clasificacion2", st.NextToken());
        PutValue("Clasificacion3", st.NextToken());
        PutValue("Clasificacion4", st.NextToken());
        PutValue("Clasificacion5", st.NextToken());
        PutValue("Clasificacion6", st.NextToken());
        PutValue("Clasificacion7", st.NextToken());
        PutValue("CodigoMoneda", st.NextToken());
        PutValue("PuntoOperacional", st.NextToken());
        PutValue("CodigoINE", st.NextToken());
        PutValue("Barrio", st.NextToken());
        PutValue("FormaPago", st.NextToken());
        PutValue("RecargoEquivalencia", st.NextToken());
        PutValue("HorarioApertura", st.NextToken());
        PutValue("HorarioVisita", st.NextToken());
        PutValue("Vacaciones", st.NextToken());
        PutValue("FechaAlta", st.NextToken());
        PutValue("FechaBaja", st.NextToken());
        PutValue("MotivoBaja", st.NextToken());
        PutValue("Clasificacion8", st.NextToken());
        PutValue("Clasificacion9", st.NextToken());
        PutValue("Clasificacion10", st.NextToken());
        PutValue("Clasificacion11", st.NextToken());
        PutValue("Clasificacion12", st.NextToken());
        PutValue("Clasificacion13", st.NextToken());
        PutValue("Clasificacion14", st.NextToken());
        PutValue("Clasificacion15", st.NextToken());
        PutValue("Clasificacion16", st.NextToken());
        PutValue("Clasificacion17", st.NextToken());
        PutValue("Clasificacion18", st.NextToken());
        PutValue("Clasificacion19", st.NextToken());
        PutValue("Clasificacion20", st.NextToken());
        PutValue("Clasificacion21", st.NextToken());
        PutValue("Clasificacion22", st.NextToken());
        PutValue("Clasificacion23", st.NextToken());
        PutValue("Clasificacion24", st.NextToken());
        PutValue("Clasificacion25", st.NextToken());
        PutValue("Clasificacion26", st.NextToken());
        PutValue("Clasificacion27", st.NextToken());
        PutValue("Clasificacion28", st.NextToken());
        PutValue("Clasificacion29", st.NextToken());
        PutValue("Clasificacion30", st.NextToken());
      }
    }

    //Getters de cada uno de los campos de la entidad   
    public string CodigoCliente
    {
      get { return GetValue("CodigoCliente"); }
    }

    public string Nombre 
    {
      get { return GetValue("Nombre").ToUpper(); }
    }

    public string RazonSocial 
    {
      get { return GetValue("RazonSocial").ToUpper(); }
    }

    public string CIF    
    {
      get { return GetValue("CIF"); }
    }

    public string Direccion    
    {
      get { return GetValue("Direccion").ToUpper(); }
    }

    public string TipoCalle     
    {
      get { return GetValue("Tipo Calle"); }
    }

    public string Calle    
    {
      get { return GetValue("Calle").ToUpper(); }
    }

    public string Numero    
    {
      get { return GetValue("Numero"); }
    }

    public string Poblacion    
    {
      get { return GetValue("Poblacion").ToUpper(); }
    }

    public string CodigoPostal    
    {
      get { return GetValue("CodigoPostal"); }
    }

    public string CodigoPais
    {
      get { return GetValue("CodigoPais"); }
    }

    public string PaginaWEB    
    {
      get { return GetValue("PaginaWEB"); }
    }
    
    public string Telefono1     
    {
      get { return GetValue("Telefono1"); }
    }

    public string Telefono2    
    {
      get { return GetValue("Telefono2"); }
    }

    public string FAX     
    {
      get { return GetValue("FAX"); }
    }

    public string Email     
    {
      get { return GetValue("Email"); }
    }

    public string PersonaContacto     
    {
      get { return GetValue("PersonaContacto"); }
    }

    public string Status   
    {
      get { return GetValue("Status"); }
    }

    public string Clasificacion1  
    {
      get { return GetValueTruncating("Clasificacion1", 15); }
    }

    public string Clasificacion2   
    {
      get { return GetValueTruncating("Clasificacion2", 15); }
    }

    public string Clasificacion3 
    {
      get { return GetValueTruncating("Clasificacion3", 15); }
    }

    public string Clasificacion4
    {
      get { return GetValueTruncating("Clasificacion4", 15); }
    }

    public string Clasificacion5
    {
      get { return GetValueTruncating("Clasificacion5", 15); }
    }

    public string Clasificacion6
    {
      get { return GetValueTruncating("Clasificacion6", 15); }
    }

    public string Clasificacion7 
    {
      get { return GetValueTruncating("Clasificacion7", 15); }
    }

    public string CodigoMoneda  
    {
      get { return GetValue("CodigoMoneda"); }
    }

    public string PuntoOperacional  
    {
      get { return GetValue("PuntoOperacional"); }
    }


      public string CodigoINE
      {
          get { return GetValue("CodigoINE"); }
      }

      public string Barrio
      {
          get { return GetValue("Barrio"); }
      }

      public string FormaPago
      {
          get { return GetValue("FormaPago"); }
      }

      public string RecargoEquivalencia
      {
          get { return GetValue("RecargoEquivalencia"); }
      }

      public string HorarioApertura
      {
          get { return GetValue("HorarioApertura"); }
      }

      public string HorarioVisita
      {
          get { return GetValue("HorarioVisita"); }
      }

      public string Vacaciones
      {
          get { return GetValue("Vacaciones"); }
      }

      public string FechaAlta
      {
          get { return GetValue("FechaAlta"); }
      }

      public string FechaBaja
      {
          get { return GetValue("FechaBaja"); }
      }

      public string MotivoBaja
      {
          get { return GetValue("MotivoBaja"); }
      }

      public string Clasificacion8 { get { return GetValueTruncating("Clasificacion8", 15); } }
      public string Clasificacion9 { get { return GetValueTruncating("Clasificacion9", 15); } }
      public string Clasificacion10 { get { return GetValueTruncating("Clasificacion10", 15); } }
      public string Clasificacion11 { get { return GetValueTruncating("Clasificacion11", 15); } }
      public string Clasificacion12 { get { return GetValueTruncating("Clasificacion12", 15); } }
      public string Clasificacion13 { get { return GetValueTruncating("Clasificacion13", 15); } }
      public string Clasificacion14 { get { return GetValueTruncating("Clasificacion14", 15); } }
      public string Clasificacion15 { get { return GetValueTruncating("Clasificacion15", 15); } }
      public string Clasificacion16 { get { return GetValueTruncating("Clasificacion16", 15); } }
      public string Clasificacion17 { get { return GetValueTruncating("Clasificacion17", 15); } }
      public string Clasificacion18 { get { return GetValueTruncating("Clasificacion18", 15); } }
      public string Clasificacion19 { get { return GetValueTruncating("Clasificacion19", 15); } }
      public string Clasificacion20 { get { return GetValueTruncating("Clasificacion20", 15); } }
      public string Clasificacion21 { get { return GetValueTruncating("Clasificacion21", 15); } }
      public string Clasificacion22 { get { return GetValueTruncating("Clasificacion22", 15); } }
      public string Clasificacion23 { get { return GetValueTruncating("Clasificacion23", 15); } }
      public string Clasificacion24 { get { return GetValueTruncating("Clasificacion24", 15); } }
      public string Clasificacion25 { get { return GetValueTruncating("Clasificacion25", 15); } }
      public string Clasificacion26 { get { return GetValueTruncating("Clasificacion26", 15); } }
      public string Clasificacion27 { get { return GetValueTruncating("Clasificacion27", 15); } }
      public string Clasificacion28 { get { return GetValueTruncating("Clasificacion28", 15); } }
      public string Clasificacion29 { get { return GetValueTruncating("Clasificacion29", 15); } }
      public string Clasificacion30 { get { return GetValueTruncating("Clasificacion30", 15); } }

  }
}
