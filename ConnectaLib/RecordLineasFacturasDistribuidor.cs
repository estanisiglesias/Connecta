using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Xml;

namespace ConnectaLib
{
    /// <summary>
    /// Clase interna que representa un registro
    /// del SIP Facturas distribuidor (líneas). Se utiliza como almacenamiento
    /// de cada uno de los registros en formato XML o delimited. De alguna manera
    /// independiza la fuente de entrada de datos a la hora de realizar el
    /// algoritmo de traspaso.
    /// </summary>
    public class RecordLineasFacturasDistribuidor : CommonRecord
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
                PutValue("NumLinea", st.NextToken());
                PutValue("CodigoProducto", st.NextToken());
                PutValue("Cantidad", st.NextToken());
                PutValue("UM", st.NextToken());
                PutValue("Peso", st.NextToken());
                PutValue("UMPeso", st.NextToken());
                PutValue("Volumen", st.NextToken());
                PutValue("UMVolumen", st.NextToken());
                PutValue("PrecioBase", st.NextToken());
                PutValue("Descuentos", st.NextToken());
                PutValue("PrecioBrutoTotal", st.NextToken());
                PutValue("FechaEntrega", st.NextToken());
                PutValue("Almacen", st.NextToken());
                PutValue("Lote", st.NextToken());
                PutValue("FechaCaducidad", st.NextToken());
                PutValue("CodigoPostal", st.NextToken());
                PutValue("Direccion", st.NextToken());
                PutValue("TipoCalle", st.NextToken());
                PutValue("Calle", st.NextToken());
                PutValue("Numero", st.NextToken());
                PutValue("CodigoPais", st.NextToken());
                PutValue("Ruta", st.NextToken());
                PutValue("CodigoComercial", st.NextToken());
                PutValue("EjercicioEntrega", st.NextToken());
                PutValue("NumEntrega", st.NextToken());
                PutValue("NumLineaEntrega", st.NextToken());
                PutValue("CosteDistribuidor", st.NextToken());
                PutValue("DocRelacionado", st.NextToken());
                PutValue("CodigoCliente", st.NextToken());
                PutValue("FechaFra", st.NextToken());
                PutValue("UM2", st.NextToken());
                PutValue("Cantidad2", st.NextToken());
                PutValue("PrecioBase2", st.NextToken());
                PutValue("TipoVenta", st.NextToken());
                PutValue("MotivoAbono", st.NextToken());
                PutValue("CodigoPromocion", st.NextToken());
                PutValue("Libre1", st.NextToken());
                PutValue("Libre2", st.NextToken());
                PutValue("Libre3", st.NextToken());
                PutValue("CantLibre1", st.NextToken());
                PutValue("CantLibre2", st.NextToken());
                PutValue("CantLibre3", st.NextToken());
                PutValue("TipoAcuerdoLiq", st.NextToken());
                PutValue("SubtipoAcuerdoLiq", st.NextToken());
                PutValue("ImporteLiq", st.NextToken());
                PutValue("TipoAcuerdoLiq2", st.NextToken());
                PutValue("SubtipoAcuerdoLiq2", st.NextToken());
                PutValue("ImporteLiq2", st.NextToken());
                PutValue("TipoAcuerdoLiq3", st.NextToken());
                PutValue("SubtipoAcuerdoLiq3", st.NextToken());
                PutValue("ImporteLiq3", st.NextToken());
                PutValue("ValorLibre1Liq", st.NextToken());
                PutValue("ValorLibre2Liq", st.NextToken());
                PutValue("ValorLibre3Liq", st.NextToken());
                PutValue("ValorLibre4Liq", st.NextToken());
                PutValue("ValorLibre5Liq", st.NextToken());
                PutValue("ValorLibre6Liq", st.NextToken());
                PutValue("ValorLibre7Liq", st.NextToken());
                PutValue("ValorLibre8Liq", st.NextToken());
                PutValue("ValorLibre9Liq", st.NextToken());
                PutValue("ValorLibre10Liq", st.NextToken());
                PutValue("ObservacionesLiq", st.NextToken());
                PutValue("TipoDatoRegistro", st.NextToken());
            }
        }

        //Getters de cada uno de los campos de la entidad   
        public string NumFactura { get { return GetValue("NumFactura"); } set { PutValue("NumFactura", value); } }
        //public string Ejercicio { get { return GetNumericValueAsInt("Ejercicio"); } set { PutValue("Ejercicio", value); } }
        public string Ejercicio { get { return GetValue("Ejercicio"); } set { PutValue("Ejercicio", value); } }
        public string NumLinea { get { return GetNumericValue("NumLinea"); } }
        public string CodigoProducto { get { return GetValue("CodigoProducto"); } set { PutValue("CodigoProducto", value); } }
        public string Cantidad { get { return GetNumericValue("Cantidad"); } set { PutValue("Cantidad", value); } }
        public string UM { get { return GetValue("UM"); } }
        public string Peso { get { return GetNumericValue("Peso"); } }
        public string UMPeso { get { return GetValue("UMPeso"); } }
        public string Volumen { get { return GetNumericValue("Volumen"); } }
        public string UMVolumen { get { return GetValue("UMVolumen"); } }
        public string PrecioBase { get { return GetNumericValue("PrecioBase"); } set { PutValue("PrecioBase", value); } }
        public string Descuentos { get { return GetNumericValue("Descuentos"); } }
        public string PrecioBrutoTotal { get { return GetNumericValue("PrecioBrutoTotal"); } }
        public string FechaEntrega { get { return GetValue("FechaEntrega"); } }
        public string Almacen { get { return GetValue("Almacen"); } }    
        public string Lote { get { return GetValue("Lote"); } }    
        public string FechaCaducidad { get { return GetValue("FechaCaducidad"); } }
        public string CodigoPostal { get { return GetValue("CodigoPostal"); } }    
        public string Direccion { get { return GetValue("Direccion"); } }    
        public string TipoCalle { get { return GetValue("TipoCalle"); } }    
        public string Calle { get { return GetValue("Calle"); } }    
        public string Numero { get { return GetValue("Numero"); } }    
        public string CodigoPais { get { return GetValue("CodigoPais"); } }    
        public string Ruta { get { return GetValue("Ruta"); } }    
        public string CodigoComercial { get { return GetValue("CodigoComercial"); } }    
        public string EjercicioEntrega { get { return GetValue("EjercicioEntrega"); } }    
        public string NumEntrega { get { return GetValue("NumEntrega"); } }
        public string NumLineaEntrega { get { return GetNumericValue("NumLineaEntrega"); } }
        public string CosteDistribuidor { get { return GetNumericValue("CosteDistribuidor"); } }
        public string DocRelacionado { get { return GetValue("DocRelacionado"); } }
        public string CodigoCliente { get { return GetValue("CodigoCliente"); } }
        public string FechaFra { get { return GetValue("FechaFra"); } }
        public string UM2 { get { return GetValue("UM2"); } }
        public string Cantidad2 { get { return GetNumericValue("Cantidad2"); } }
        public string PrecioBase2 { get { return GetNumericValue("PrecioBase2"); } }
        public string TipoVenta { get { return GetValue("TipoVenta"); } }
        public string MotivoAbono { get { return GetValue("MotivoAbono"); } }
        public string CodigoPromocion { get { return GetValue("CodigoPromocion"); } }
        public string Libre1 { get { return GetValue("Libre1"); } }
        public string Libre2 { get { return GetValue("Libre2"); } }
        public string Libre3 { get { return GetValue("Libre3"); } }
        public string CantLibre1 { get { return GetValue("CantLibre1"); } }
        public string CantLibre2 { get { return GetValue("CantLibre2"); } }
        public string CantLibre3 { get { return GetValue("CantLibre3"); } }
        public string TipoAcuerdoLiq { get { return GetValue("TipoAcuerdoLiq"); } }
        public string SubtipoAcuerdoLiq { get { return GetValue("SubtipoAcuerdoLiq"); } }
        public string ImporteLiq { get { return GetValue("ImporteLiq"); } }
        public string TipoAcuerdoLiq2 { get { return GetValue("TipoAcuerdoLiq2"); } }
        public string SubtipoAcuerdoLiq2 { get { return GetValue("SubtipoAcuerdoLiq2"); } }
        public string ImporteLiq2 { get { return GetValue("ImporteLiq2"); } }
        public string TipoAcuerdoLiq3 { get { return GetValue("TipoAcuerdoLiq3"); } }
        public string SubtipoAcuerdoLiq3 { get { return GetValue("SubtipoAcuerdoLiq3"); } }
        public string ImporteLiq3 { get { return GetValue("ImporteLiq3"); } }
        public string ValorLibre1Liq { get { return GetValue("ValorLibre1Liq"); } }
        public string ValorLibre2Liq { get { return GetValue("ValorLibre2Liq"); } }
        public string ValorLibre3Liq { get { return GetValue("ValorLibre3Liq"); } }
        public string ValorLibre4Liq { get { return GetValue("ValorLibre4Liq"); } }
        public string ValorLibre5Liq { get { return GetValue("ValorLibre5Liq"); } }
        public string ValorLibre6Liq { get { return GetValue("ValorLibre6Liq"); } }
        public string ValorLibre7Liq { get { return GetValue("ValorLibre7Liq"); } }
        public string ValorLibre8Liq { get { return GetValue("ValorLibre8Liq"); } }
        public string ValorLibre9Liq { get { return GetValue("ValorLibre9Liq"); } }
        public string ValorLibre10Liq { get { return GetValue("ValorLibre10Liq"); } }
        public string ObservacionesLiq { get { return GetValue("ObservacionesLiq"); } }
        public string TipoDatoRegistro { get { return GetValue("TipoDatoRegistro"); } set { PutValue("TipoDatoRegistro", value); } }
    }
}
