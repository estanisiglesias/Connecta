using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data.OleDb;

namespace ConnectaLib
{
  public class Peso
  {
    private double pesoTotal = 0;
    private string umcPeso = "";
    private bool haSidoCalculado = false;

    //Getters/setters
    public double PesoTotal { get { return pesoTotal; } set { pesoTotal = value; } }
    public string UMcPeso { get { return umcPeso; } set { umcPeso = value; } }
    public bool HaSidoCalculado { get { return haSidoCalculado; } set { haSidoCalculado = value; } }

    /// <summary>
    /// Tratar peso
    /// </summary>
    /// <param name="db">base de datos</param>
    /// ...
    /// <returns>true si es correcto</returns>
    public bool TratarPeso(Database db, string codigoDistribuidor, string codigoProducto, string lineaPeso, string lineaUMPeso, string sipTypeName, string cantidad, string UMProducto, UnidadMedida um, bool calcularCantidades)
    {
        DbDataReader reader = null;
        bool pesoCalculado = false;
        string sql = "";

        pesoTotal = 0;
        UMcPeso = "";
        haSidoCalculado = false;

        //Si se ha activado el flag de NO calcular cantidades salimos con ok pero con peso a 0 y UM vacia.
        if (!calcularCantidades)
            return true;
        
        try
        {
            double dblCantidad = Double.Parse(cantidad);
            string p = lineaPeso;
            if (Utils.IsBlankField(p)) p = "0";
            double peso = Utils.StringToDouble(p);
            if (peso != 0)
            {
                if (!Utils.IsBlankField(lineaUMPeso))
                {
                    //Verificamos si existe conversión de Unidad de Medida entre la UMPeso del distribuidor y la de Connect@
                    sql = "Select UMc from UMAgente Where IdcAgente = " + codigoDistribuidor + " and UMAgente='" + lineaUMPeso + "'";
                    reader = db.GetDataReader(sql);
                    if (reader.Read())
                    {
                        UMcPeso = db.GetFieldValue(reader, 0);
                        pesoCalculado = true;
                        pesoTotal = peso;
                    }
                    reader.Close();
                    reader = null;

                    if (!pesoCalculado)
                    {
                        //Si no existe, comprobamos si la unidad de medida que nos ha pasado coincide con una UM de Connect@.
                        sql = "Select UMc from UnidadesMedida where UMc='" + lineaUMPeso + "'";
                        reader = db.GetDataReader(sql);
                        if (reader.Read())
                        {
                            UMcPeso = db.GetFieldValue(reader, 0);
                            pesoCalculado = true;
                            pesoTotal = peso;
                        }
                        else
                        {
                            //Globals.GetInstance().GetLog().DetailedError(codigoDistribuidor, sipTypeName, "Unidad de medida de peso no existe " + lineaUMPeso);
                            string myAlertMsg = "Unidad de medida de peso {0} no existe.";
                            Globals.GetInstance().GetLog2().Trace(codigoDistribuidor, sipTypeName, "PESO0001", myAlertMsg, lineaUMPeso);
                            UMcPeso = "";
                            return false;
                        }
                        reader.Close();
                        reader = null;
                    }
                }
            }
            
            if (pesoCalculado == false)
            {
                UMcPeso = "";

                //Buscaremos la UM de peso por defecto del producto
                string idcFabricante = "";
                string UMPesoEnMaestro = "";
                string PesoMaestroProducto = "0";
                string UMBaseProducto = "";
                sql = "Select UMcPeso,IdcFabricante,PesoBruto,UMc from Productos where IdcProducto = " + codigoProducto;
                reader = db.GetDataReader(sql);
                if (reader.Read())
                {
                    UMPesoEnMaestro = db.GetFieldValue(reader, 0);
                    idcFabricante = db.GetFieldValue(reader, 1);
                    PesoMaestroProducto = db.GetFieldValue(reader, 2);
                    UMBaseProducto = db.GetFieldValue(reader, 3);
                }
                reader.Close();
                reader = null;

                //Si la UM que llega en la línea es la misma que la UM del maestro de productos se asigna directo
                if (UMPesoEnMaestro.Equals(UMProducto))
                {
                    pesoTotal = dblCantidad;
                    UMcPeso = UMPesoEnMaestro;
                    pesoCalculado = true;
                }

                if (pesoCalculado == false)
                {
                    //Buscaremos a ver si hay conversión a través de la tabla ConvUMProducto
                    if (idcFabricante != "" && UMProducto != "")
                    {
                        string UMPesoEnConvUM = "";
                        double pesoEnConvUM = 0;
                        double cantEnConvUM = 0;
                        sql = "Select UMPeso, Peso, Cantidad From ConvUMProducto Where IdcAgente = " + idcFabricante +
                                " And IdcProducto = " + codigoProducto +
                                " And UMc2 = '" + UMProducto + "'";
                        reader = db.GetDataReader(sql);
                        while (reader.Read())
                        {
                            //Puede devolver más de un registro. Tomamos el primero.
                            UMPesoEnConvUM = db.GetFieldValue(reader, 0);
                            pesoEnConvUM = Utils.StringToDouble(db.GetFieldValue(reader, 1));
                            cantEnConvUM = Utils.StringToDouble(db.GetFieldValue(reader, 2));
                            if (cantEnConvUM != 0)
                            {
                                pesoEnConvUM = pesoEnConvUM / cantEnConvUM;
                                break;
                            }
                        }
                        reader.Close();
                        reader = null;

                        if (UMPesoEnConvUM.Trim() != "" && pesoEnConvUM != 0)
                        {
                            pesoTotal = dblCantidad * pesoEnConvUM;
                            UMcPeso = UMPesoEnConvUM;
                            pesoCalculado = true;
                        }
                    }
                }

                if (pesoCalculado == false)
                {
                    //Si UMPesoEnMaestro no es blanco, comprobamos si existe conversión entre unidades de medida
                    if (!Utils.IsBlankField(UMPesoEnMaestro))
                    {
                        CalcUnidadMedida cum = um.ConversionUnidadesMedida(db, idcFabricante, codigoProducto, cantidad, UMProducto, UMPesoEnMaestro);
                        if (cum.isOK)
                        {
                            pesoTotal = cum.cantidad;
                            UMcPeso = UMPesoEnMaestro;
                            pesoCalculado = true;
                        }
                    }
                }

                if (pesoCalculado == false)
                {
                    //Ahora vamos a intentar encontrar la conversión a través de la información que se guarda en el maestro de productos
                    double dblPesoMaestroProducto = Double.Parse(PesoMaestroProducto);
                    if (dblPesoMaestroProducto != 0)
                    {
                        if (UMBaseProducto.Equals(UMProducto))
                        {
                            pesoTotal = dblPesoMaestroProducto * dblCantidad;
                            UMcPeso = UMPesoEnMaestro;
                            pesoCalculado = true;
                        }
                        else
                        {
                            //Sino...Buscamos conversión entre unidades de medida
                            CalcUnidadMedida cum = um.ConversionUnidadesMedida(db, idcFabricante, codigoProducto, cantidad, UMProducto, UMBaseProducto);
                            if (cum.isOK)
                            {
                                pesoTotal = dblPesoMaestroProducto * cum.cantidad;
                                UMcPeso = UMPesoEnMaestro;
                                pesoCalculado = true;
                            }
                        }
                    }
                }

                if (pesoCalculado == false)
                {
                    pesoTotal = 0;
                    UMcPeso = "";
                }
                else
                {
                    haSidoCalculado = true;
                }
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return pesoCalculado;
    }
  }
}
