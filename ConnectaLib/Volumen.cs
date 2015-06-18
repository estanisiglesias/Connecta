using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data.OleDb;

namespace ConnectaLib
{
  /// <summary>
  /// Tratamiento gen�rico del volumen
  /// </summary>
  public class Volumen
  {
    private double volumenTotal = 0;
    private string umcVolumen = "";
    private bool haSidoCalculado = false;

    //Getters/setters
    public double VolumenTotal { get { return volumenTotal; } set { volumenTotal = value; } }
    public string UMcVolumen { get { return umcVolumen; } set { umcVolumen = value; } }
    public bool HaSidoCalculado { get { return haSidoCalculado; } set { haSidoCalculado = value; } }

    /// <summary>
    /// Tratar volumen
    /// </summary>
    /// <param name="db">base de datos</param>
    /// ...
    /// <returns>true si es correcto</returns>
    public bool TratarVolumen(Database db, string codigoDistribuidor, string codigoProducto, string lineaVolumen, string lineaUMVolumen, string sipTypeName,  string cantidad, string UMProducto, UnidadMedida um, bool calcularCantidades)
    {
        DbDataReader reader = null;
        bool volumenOk = false;
        string sql = "";

        volumenTotal = 0;
        umcVolumen = "";
        haSidoCalculado = false;
        
        //Si se ha activado el flag de NO calcular cantidades salimos con ok pero con volumen a 0 y UM vacia.
        if (!calcularCantidades)
            return true;

        try
        {
            string vol = lineaVolumen;
            if (Utils.IsBlankField(vol)) vol = "0";
            double volumen = Utils.StringToDouble(vol);
            if (volumen != 0)
            {
                if (!Utils.IsBlankField(lineaUMVolumen))
                {
                    //Verificamos si existe conversi�n de Unidad de Medida entre la UMVolumen del distribuidor y la de Connect@
                    sql = "Select UMc from UMAgente Where IdcAgente = " + codigoDistribuidor + " and UMAgente='" + lineaUMVolumen + "'";
                    reader = db.GetDataReader(sql);
                    if (reader.Read())
                    {
                        umcVolumen = db.GetFieldValue(reader, 0);
                        volumenOk = true;
                        volumenTotal = volumen;
                    }
                    reader.Close();
                    reader = null;

                    if (!volumenOk)
                    {
                        //Si no existe, comprobamos si la unidad de medida que nos ha pasado coincide con una UM de Connect@.
                        sql = "Select UMc from UnidadesMedida where UMc='" + lineaUMVolumen + "'";
                        reader = db.GetDataReader(sql);
                        if (reader.Read())
                        {
                            umcVolumen = db.GetFieldValue(reader, 0);
                            volumenOk= true;
                            volumenTotal = volumen;
                        }
                        else
                        {
                            //Globals.GetInstance().GetLog().DetailedError(codigoDistribuidor, sipTypeName, "Unidad de medida de vol�men no existe " + lineaUMVolumen);
                            string myAlertMsg = "Unidad de medida de vol�men {0} no existe.";
                            Globals.GetInstance().GetLog2().Trace(codigoDistribuidor, sipTypeName, "VOLU0001", myAlertMsg, lineaUMVolumen);
                            umcVolumen = "";
                            return false;
                        }
                        reader.Close();
                        reader = null;
                    }
                }
            }
            
            if (!volumenOk)
            {
                string idcFabricante = "";
                string UMVolumenEnMaestro = "";
                string VolumenMaestroProducto = "0";
                string UMBaseProducto = "";

                //Buscaremos la UM de volumen por defecto del producto
                sql = "Select UMVolumen,IdcFabricante,Volumen,UMc from Productos where IdcProducto = " + codigoProducto;
                reader = db.GetDataReader(sql);
                umcVolumen = "";
                if (reader.Read())
                {
                    UMVolumenEnMaestro = db.GetFieldValue(reader, 0);
                    idcFabricante = db.GetFieldValue(reader, 1);
                    VolumenMaestroProducto = db.GetFieldValue(reader, 2);
                    UMBaseProducto = db.GetFieldValue(reader, 3);
                }
                reader.Close();
                reader = null;

                //Si la UM que llega en la l�nea es la misma que la UM del maestro de productos se asigna directo
                if (UMVolumenEnMaestro.Equals(UMProducto))
                {
                    volumenTotal = Double.Parse(cantidad);
                    umcVolumen = UMVolumenEnMaestro;
                    volumenOk = true;
                }

                if (!volumenOk)
                {
                    //Buscaremos a ver si hay conversi�n a trav�s de la tabla ConvUMProducto
                    if (idcFabricante != "" && UMProducto != "")
                    {
                        string UMVolumenEnConvUM = "";
                        double volumenEnConvUM = 0;
                        double cantEnConvUM = 0;
                        sql = "Select UMVolumen, Volumen, Cantidad From ConvUMProducto Where IdcAgente = " + idcFabricante +
                                " And IdcProducto = " + codigoProducto +
                                " And UMc2 = '" + UMProducto + "'";
                        reader = db.GetDataReader(sql);
                        while (reader.Read())
                        {
                            //Puede devolver m�s de un registro. Tomamos el primero.
                            UMVolumenEnConvUM = db.GetFieldValue(reader, 0);
                            volumenEnConvUM = Utils.StringToDouble(db.GetFieldValue(reader, 1));
                            cantEnConvUM = Utils.StringToDouble(db.GetFieldValue(reader, 2));
                            if (cantEnConvUM != 0)
                            {
                                volumenEnConvUM = volumenEnConvUM / cantEnConvUM;
                                break;
                            }
                        }
                        reader.Close();
                        reader = null;

                        if (UMVolumenEnConvUM.Trim() != "" && volumenEnConvUM != 0)
                        {
                            volumenTotal = Double.Parse(cantidad) * volumenEnConvUM;
                            umcVolumen = UMVolumenEnConvUM;
                            volumenOk = true;
                        }
                    }
                }

                if (!volumenOk)
                {
                    //Si UMcVolumen no es blanco, comprobamos si existe conversi�n entre unidades de medida
                    if (!Utils.IsBlankField(UMVolumenEnMaestro))
                    {
                        CalcUnidadMedida cum = um.ConversionUnidadesMedida(db, idcFabricante, codigoProducto, cantidad, UMProducto, UMVolumenEnMaestro);
                        if (cum.isOK)
                        {
                            volumenTotal = cum.cantidad;
                            umcVolumen = UMVolumenEnMaestro;
                            volumenOk = true;
                        }
                    }
                }

                if (!volumenOk)
                {
                    //Ahora vamos a intentar encontrar la conversi�n a trav�s de la informaci�n que se guarda en el maestro de productos
                    //Si VolumenMaestroProducto distinto a 0
                    double dblVolumenMaestroProducto = Double.Parse(VolumenMaestroProducto);
                    if (dblVolumenMaestroProducto != 0)
                    {
                        //Si UMBaseProducto = UMcProducto
                        if (UMBaseProducto.Equals(UMProducto))
                        {
                            volumenTotal = dblVolumenMaestroProducto * Double.Parse(cantidad);
                            umcVolumen = UMVolumenEnMaestro;
                            volumenOk = true;
                        }
                        else
                        {
                            //Sino...Buscamos conversi�n entre unidades de medida
                            CalcUnidadMedida cum = um.ConversionUnidadesMedida(db, idcFabricante, codigoProducto, cantidad, UMProducto, UMBaseProducto);
                            if (cum.isOK)
                            {
                                volumenTotal = cum.cantidad * dblVolumenMaestroProducto;
                                umcVolumen = UMVolumenEnMaestro;
                                volumenOk = true;
                            }
                        }
                    }
                }

                if (!volumenOk)
                {
                    volumenTotal = 0;
                    umcVolumen = "";
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
        return volumenOk;
    }
  }
}
