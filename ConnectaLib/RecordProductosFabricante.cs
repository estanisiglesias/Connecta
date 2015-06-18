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
  public class RecordProductosFabricante : CommonRecord
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
            PutValue("CodigoProducto", st.NextToken());
            PutValue("Descripcion", st.NextToken());
            PutValue("Abreviatura", st.NextToken());
            PutValue("DescrLarga", st.NextToken());
            PutValue("Status", st.NextToken());
            PutValue("UM", st.NextToken());
            PutValue("EAN13", st.NextToken());
            PutValue("PesoNeto", st.NextToken());
            PutValue("PesoNetoEscurrido", st.NextToken());
            PutValue("PesoBruto", st.NextToken());
            PutValue("UMPeso", st.NextToken());
            PutValue("Longitud", st.NextToken());
            PutValue("Ancho", st.NextToken());
            PutValue("Alto", st.NextToken());
            PutValue("Volumen", st.NextToken());
            PutValue("UMLongitud", st.NextToken());
            PutValue("UMAncho", st.NextToken());
            PutValue("UMAlto", st.NextToken());
            PutValue("UMVolumen", st.NextToken());
            PutValue("InstrucManipulacion", st.NextToken());
            PutValue("EmpresaFabricante", st.NextToken());
            PutValue("CEP", st.NextToken());
            PutValue("PartidaArancelaria", st.NextToken());
            PutValue("Tarifa", st.NextToken());
            PutValue("Clasificacion1", st.NextToken());
            PutValue("Clasificacion2", st.NextToken());
            PutValue("Clasificacion3", st.NextToken());
            PutValue("Clasificacion4", st.NextToken());
            PutValue("Clasificacion5", st.NextToken());
            PutValue("Clasificacion6", st.NextToken());
            PutValue("Clasificacion7", st.NextToken());
            PutValue("Clasificacion8", st.NextToken());
            PutValue("Clasificacion9", st.NextToken());
            PutValue("Clasificacion10", st.NextToken());
            PutValue("Clasificacion11", st.NextToken());
            PutValue("Clasificacion12", st.NextToken());
            PutValue("Clasificacion13", st.NextToken());
            PutValue("Clasificacion14", st.NextToken());
            PutValue("Jerarquia", st.NextToken());
            PutValue("FechaBloqueo", st.NextToken());
            PutValue("FechaBaja", st.NextToken());
            PutValue("CodigoProductoAlt", st.NextToken());
            PutValue("IndCalculaCantidades", st.NextToken());
            PutValue("IndFijaUMGestion", st.NextToken());
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
            PutValue("EsKit", st.NextToken());
            PutValue("CodigoProductoAgrup", st.NextToken());
            PutValue("PropietarioProducto", st.NextToken());            
            PutValue("EsTotalizador", st.NextToken());
            PutValue("NivelProducto", st.NextToken());
            PutValue("UMEstadistica1", st.NextToken());
            PutValue("UMEstadistica2", st.NextToken());
            PutValue("UMEstadistica3", st.NextToken());
            PutValue("PuntoVerde", st.NextToken());
            PutValue("UMPuntoVerde", st.NextToken());
            PutValue("Accion", st.NextToken());
            PutValue("DescripcionAdd", st.NextToken());
        }
    }

    //Getters de cada uno de los campos de la entidad
    public string CodigoProducto { get { return GetValue("CodigoProducto"); } }
    public string Descripcion { get { return GetValue("Descripcion"); } }
    public string Abreviatura { get { return GetValue("Abreviatura"); } }
    public string DescrLarga { get { return GetValue("DescrLarga"); } }
    public string Status { get { return GetValue("Status"); } }
    public string UM { get { return GetValue("UM"); } }
    public string EAN13 { get { return GetValue("EAN13"); } }
    public string PesoNeto { get { return GetValue("PesoNeto"); } }
    public string PesoNetoEscurrido { get { return GetValue("PesoNetoEscurrido"); } }
    public string PesoBruto { get { return GetValue("PesoBruto"); } }
    public string UMPeso { get { return GetValue("UMPeso"); } }
    public string Longitud { get { return GetValue("Longitud"); } }
    public string Ancho { get { return GetValue("Ancho"); } }
    public string Alto { get { return GetValue("Alto"); } }
    public string Volumen { get { return GetValue("Volumen"); } }
    public string UMLongitud { get { return GetValue("UMLongitud"); } }
    public string UMAncho { get { return GetValue("UMAncho"); } }
    public string UMAlto { get { return GetValue("UMAlto"); } }
    public string UMVolumen { get { return GetValue("UMVolumen"); } }
    public string InstrucManipulacion { get { return GetValue("InstrucManipulacion"); } }
    public string EmpresaFabricante { get { return GetValue("EmpresaFabricante"); } }
    public string CEP { get { return GetValue("CEP"); } }
    public string PartidaArancelaria { get { return GetValue("PartidaArancelaria"); } }
    public string Tarifa { get { return GetValue("Tarifa"); } }
    public string Clasificacion1 { get { return GetValue("Clasificacion1"); } }
    public string Clasificacion2 { get { return GetValue("Clasificacion2"); } }
    public string Clasificacion3 { get { return GetValue("Clasificacion3"); } }
    public string Clasificacion4 { get { return GetValue("Clasificacion4"); } }
    public string Clasificacion5 { get { return GetValue("Clasificacion5"); } }
    public string Clasificacion6 { get { return GetValue("Clasificacion6"); } }
    public string Clasificacion7 { get { return GetValue("Clasificacion7"); } }
    public string Clasificacion8 { get { return GetValue("Clasificacion8"); } }
    public string Clasificacion9 { get { return GetValue("Clasificacion9"); } }
    public string Clasificacion10 { get { return GetValue("Clasificacion10"); } }
    public string Clasificacion11 { get { return GetValue("Clasificacion11"); } }
    public string Clasificacion12 { get { return GetValue("Clasificacion12"); } }
    public string Clasificacion13 { get { return GetValue("Clasificacion13"); } }
    public string Clasificacion14 { get { return GetValue("Clasificacion14"); } }
    public string Jerarquia { get { return GetValue("Jerarquia"); } }
    public string FechaBloqueo { get { return GetValue("FechaBloqueo"); } }
    public string FechaBaja { get { return GetValue("FechaBaja"); } }
    public string CodigoProductoAlt { get { return GetValue("CodigoProductoAlt"); } }
    public string IndCalculaCantidades { get { return GetValue("IndCalculaCantidades"); } }
    public string IndFijaUMGestion { get { return GetValue("IndFijaUMGestion"); } }
    public string Clasificacion15 { get { return GetValue("Clasificacion15"); } }
    public string Clasificacion16 { get { return GetValue("Clasificacion16"); } }
    public string Clasificacion17 { get { return GetValue("Clasificacion17"); } }
    public string Clasificacion18 { get { return GetValue("Clasificacion18"); } }
    public string Clasificacion19 { get { return GetValue("Clasificacion19"); } }
    public string Clasificacion20 { get { return GetValue("Clasificacion20"); } }
    public string Clasificacion21 { get { return GetValue("Clasificacion21"); } }
    public string Clasificacion22 { get { return GetValue("Clasificacion22"); } }
    public string Clasificacion23 { get { return GetValue("Clasificacion23"); } }
    public string Clasificacion24 { get { return GetValue("Clasificacion24"); } }
    public string Clasificacion25 { get { return GetValue("Clasificacion25"); } }
    public string Clasificacion26 { get { return GetValue("Clasificacion26"); } }
    public string Clasificacion27 { get { return GetValue("Clasificacion27"); } }
    public string Clasificacion28 { get { return GetValue("Clasificacion28"); } }
    public string Clasificacion29 { get { return GetValue("Clasificacion29"); } }
    public string Clasificacion30 { get { return GetValue("Clasificacion30"); } }
    public string EsKit { get { return GetValue("EsKit"); } }
    public string CodigoProductoAgrup { get { return GetValue("CodigoProductoAgrup"); } }
    public string PropietarioProducto { get { return GetValue("PropietarioProducto"); } set { PutValue("PropietarioProducto", value); } }    
    public string EsTotalizador { get { return GetValue("EsTotalizador"); } set { PutValue("EsTotalizador", value); } }
    public string NivelProducto { get { return GetValue("NivelProducto"); } }
    public string UMEstadistica1 { get { return GetValue("UMEstadistica1"); } }
    public string UMEstadistica2 { get { return GetValue("UMEstadistica2"); } }
    public string UMEstadistica3 { get { return GetValue("UMEstadistica3"); } }
    public string PuntoVerde { get { return GetValue("PuntoVerde"); } }
    public string UMPuntoVerde { get { return GetValue("UMPuntoVerde"); } }
    public string Accion { get { return GetValue("Accion"); } }
    public string DescripcionAdd { get { return GetValue("DescripcionAdd"); } }
  }
}
