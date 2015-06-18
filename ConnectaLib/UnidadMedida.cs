using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data.OleDb;
using System.Collections;

namespace ConnectaLib
{
  /// <summary>
  /// Clase utilizada por diferents SIP para comprobar las unidades de medida
  /// </summary>
  public class UnidadMedida
  {
    public Hashtable htUMsFijas = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
    public Hashtable htUMsAgentesFab = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
    public Hashtable htUMsAgentes = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
    public Hashtable htUMs = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
    public Hashtable htConvUMProd = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
    private bool UMsPreCargadas = false;

    public struct sConvUMProd
    {
        public double cantBase;
        public double cant;        
    }

    private string umProducto = "";

    /// <summary>
    /// Obtener unidad de medida del producto
    /// </summary>
    public string UMProducto { get { return umProducto; } }

    /// <summary>
    /// Cargar información necesaria de las unidades de medida fijas del agente
    /// </summary>
    /// <param name="db">db</param>
    public void CargarUnidadesMedida(Database db, string agent)
    {
        htUMsFijas.Clear();
        htUMsAgentesFab.Clear();
        htUMsAgentes.Clear();
        htUMs.Clear();
        htConvUMProd.Clear();
        DbDataReader cursor = null;
        string sql = "Select Codigo, UMcFija from ProductosAgentesUMsFijas Where IdcAgente = " + agent;
        cursor = db.GetDataReader(sql);
        string codigo = "";
        while (cursor.Read())
        {
            codigo = db.GetFieldValue(cursor, 0);
            if (!htUMsFijas.ContainsKey(codigo))
                htUMsFijas.Add(codigo, db.GetFieldValue(cursor, 1));            
        }
        cursor.Close();
        cursor = null;

        sql = "Select UMAgente, IdcFabricante, UMc from UMAgentePorFabricante Where IdcAgente = " + agent;
        cursor = db.GetDataReader(sql);
        string umAgente = "";
        string idcFabricante = "";        
        while (cursor.Read())
        {
            umAgente = db.GetFieldValue(cursor, 0);
            idcFabricante = db.GetFieldValue(cursor, 1);
            if (!htUMsAgentesFab.ContainsKey(umAgente + ";" + idcFabricante))
                htUMsAgentesFab.Add(umAgente + ";" + idcFabricante, db.GetFieldValue(cursor, 2));
        }
        cursor.Close();
        cursor = null;

        sql = "Select UMAgente, UMc from UMAgente Where IdcAgente = " + agent;
        cursor = db.GetDataReader(sql);
        umAgente = "";        
        while (cursor.Read())
        {
            umAgente = db.GetFieldValue(cursor, 0);           
            if (!htUMsAgentes.ContainsKey(umAgente))
                htUMsAgentes.Add(umAgente, db.GetFieldValue(cursor, 1));
        }
        cursor.Close();
        cursor = null;

        sql = "Select UMc from UnidadesMedida";
        cursor = db.GetDataReader(sql);
        string umc = "";
        while (cursor.Read())
        {
            umc = db.GetFieldValue(cursor, 0);
            if (!htUMs.ContainsKey(umc))
                htUMs.Add(umc, umc);
        }
        cursor.Close();
        cursor = null;

        sql = "Select IdcAgente, IdcProducto, UMc, UMc2, CantidadBase, Cantidad " +
              "  from ConvUMProducto " +
              " Where IdcAgente in (select idcagentedestino from clasifinteragentes where IdcAgenteOrigen=" + agent + ")";
        cursor = db.GetDataReader(sql);
        string idcAgente = "";
        string idcProducto = "";
        umc = "";
        string umc2 = "";        
        while (cursor.Read())
        {
            sConvUMProd conv = new sConvUMProd();
            idcAgente = db.GetFieldValue(cursor, 0);
            idcProducto = db.GetFieldValue(cursor, 1);
            umc = db.GetFieldValue(cursor, 2);
            umc2 = db.GetFieldValue(cursor, 3);
            conv.cantBase = Utils.StringToDouble(db.GetFieldValue(cursor, 4));
            conv.cant = Utils.StringToDouble(db.GetFieldValue(cursor, 5));
            if (!htConvUMProd.ContainsKey(idcAgente + ";" + idcProducto + ";" + umc + ";" + umc2))
                htConvUMProd.Add(idcAgente + ";" + idcProducto + ";" + umc + ";" + umc2, conv);
        }
        cursor.Close();
        cursor = null;       

        UMsPreCargadas = true;
    }

    public void VaciarUnidadesMedida()
    {
        UMsPreCargadas = false;
        htUMsFijas.Clear();
        htUMsAgentesFab.Clear();
        htUMsAgentes.Clear();
        htUMs.Clear();
        htConvUMProd.Clear();
    }

    /// Comprobar unidad de medida sin tener en cuenta producto, ni las UM fijas
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="codigoDistribuidor">código de agente</param>
    /// <param name="UM">Unidad de medida</param>
    /// <returns>true si es correcto</returns>
    public bool ComprobarUnidadMedida(Database db, string codigoAgente, string UM)
    {
        bool isOK = false;
        DbDataReader reader = null;
        umProducto = "";
        try
        {
            string sql = "";
            //Verificamos si existe conversión de Unidad de Medida entre la UM del agente y la de Connect@
            sql = "Select UMc from UMAgente Where IdcAgente = " + codigoAgente + " and UMAgente='" + UM + "'";
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                umProducto = db.GetFieldValue(reader, 0);
                isOK = true;
            }
            reader.Close();
            reader = null;
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return isOK;
    }

    /// Comprobar unidad de medida
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="codigoDistribuidor">código de distribuidor</param>
    /// <param name="codigoProductoDef">código connecta de producto</param>
    /// <param name="UM">Unidad de medida</param>
    /// <param name="codigoProductoLinea">código de producto de la línea</param>
    /// <returns>true si es correcto</returns>
    public bool ComprobarUnidadMedida(Database db, string idcAgente, string codigoProductoDef, string UM, string codigoProductoLinea, bool prodAsignaUMGestion)
    {
        bool isOK = false;
        DbDataReader reader = null;
        umProducto = "";
        try
        {
            string sql = "";

            //Verificamos si este producto para este agente tiene una unidad de medida Connect@ fijada
            sql = "Select UMcFija from ProductosAgentesUMsFijas Where IdcAgente = " + idcAgente +
                  " and Codigo='" + codigoProductoLinea + "'";
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                umProducto = db.GetFieldValue(reader, 0);
                isOK = true;
            }
            reader.Close();
            reader = null;

            if (!isOK)
            {
                if (prodAsignaUMGestion)
                {
                    //Verificamos si este producto tiene una unidad de medida ConnectA fijada
                    sql = "Select UMGestion from Productos Where IdcProducto = " + codigoProductoDef;
                    reader = db.GetDataReader(sql);
                    if (reader.Read())
                    {
                        umProducto = db.GetFieldValue(reader, 0);
                        isOK = true;
                    }
                    reader.Close();
                    reader = null;
                }
            }

            if (!isOK)
            {
                if (Utils.IsBlankField(UM))
                {
                    //Buscaremos la UM por defecto del producto
                    sql = "Select UMc from Productos Where IdcProducto = " + codigoProductoDef;
                    reader = db.GetDataReader(sql);
                    if (reader.Read())
                    {
                        umProducto = db.GetFieldValue(reader, 0);
                        isOK = true;
                    }
                }
                else
                {
                    //Verificamos si existe conversión de Unidad de Medida entre la UM del agente y la de Connect@
                    sql = "Select UMc from UMAgente Where IdcAgente = " + idcAgente + " and UMAgente='" + UM + "'";
                    reader = db.GetDataReader(sql);
                    if (reader.Read())
                    {
                        umProducto = db.GetFieldValue(reader, 0);
                        isOK = true;
                    }
                    reader.Close();
                    reader = null;

                    //Si no existe, comprobamos si la unidad de medida que nos ha pasado coincide con una UM de Connect@
                    if (!isOK)
                    {
                        sql = "Select UMc from UnidadesMedida where UMc='" + UM + "'";
                        reader = db.GetDataReader(sql);
                        if (reader.Read())
                        {
                            umProducto = db.GetFieldValue(reader, 0);
                            isOK = true;
                        }
                    }
                }
            }        
      }
      finally
      {
        if (reader != null)
          reader.Close();
      }
      return isOK;
    }

    /// Comprobar unidad de medida (añadiendo búsqueda por fabricante y UM defecto por fabricante y UM Gestion fija por producto)
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="codigoDistribuidor">código de distribuidor</param>
    /// <param name="codigoProductoDef">código connecta de producto</param>
    /// <param name="UM">Unidad de medida</param>
    /// <param name="codigoProductoLinea">código de producto de la línea</param>
    /// <param name="codigoFabricante">código de fabricante</param>
    /// <returns>true si es correcto</returns>    
    public bool ComprobarUnidadMedida(Database db, string codigoDistribuidor, string codigoProductoDef, string UM, string codigoProductoLinea, string codigoFabricante, string fabAsignaUMDefecto, bool prodAsignaUMGestion)
    {
        bool isOK = false;
        DbDataReader reader = null;
        umProducto = "";

        try
        {
            string sql = "";
            //Verificamos si este producto para este agente tiene una unidad de medida Connect@ fijada
            if (UMsPreCargadas)
            {
                if (htUMsFijas.ContainsKey(codigoProductoLinea))
                {
                    umProducto = htUMsFijas[codigoProductoLinea].ToString();
                    isOK = true;
                }
            }
            else
            {
                sql = "Select UMcFija from ProductosAgentesUMsFijas Where IdcAgente = " + codigoDistribuidor +
                      " and Codigo='" + codigoProductoLinea + "'";
                reader = db.GetDataReader(sql);
                if (reader.Read())
                {
                    umProducto = db.GetFieldValue(reader, 0);
                    isOK = true;
                }
                reader.Close();
                reader = null;
            }
            if (!isOK)
            {
                if (fabAsignaUMDefecto == "S")
                {
                    //Buscaremos la UM por defecto del producto
                    sql = "Select UMc from Productos Where IdcProducto = " + codigoProductoDef;
                    reader = db.GetDataReader(sql);
                    if (reader.Read())
                    {
                        umProducto = db.GetFieldValue(reader, 0);
                        isOK = true;
                    }
                    reader.Close();
                    reader = null;
                }
            }
            if (!isOK)
            {
                if (prodAsignaUMGestion)
                {
                    //Verificamos si este producto tiene una unidad de medida Connect@ fijada
                    sql = "Select UMGestion from Productos Where IdcProducto = " + codigoProductoDef;
                    reader = db.GetDataReader(sql);
                    if (reader.Read())
                    {
                        umProducto = db.GetFieldValue(reader, 0);
                        isOK = true;
                    }
                    reader.Close();
                    reader = null;
                }
            }
            if (!isOK)
            {
                if (Utils.IsBlankField(UM))
                {
                    //Buscaremos la UM por defecto del distribuidor para ese fabricante concreto en ClasifInterAgente, y si no
                    //se encuentra después buscaremos la del fabricante para ese distribuidor también en ClasifInterAgente
                    sql = "Select UMcDefecto from ClasifInterAgentes Where IdcAgenteOrigen = " + codigoDistribuidor + " and IdcAgenteDestino=" + codigoFabricante;
                    reader = db.GetDataReader(sql);
                    if (reader.Read())
                    {
                        umProducto = db.GetFieldValue(reader, 0);
                        if (!Utils.IsBlankField(umProducto))
                            isOK = true;
                    }
                    reader.Close();
                    reader = null;
                    if (!isOK)
                    {
                        sql = "Select UMcDefecto from ClasifInterAgentes Where IdcAgenteOrigen = " + codigoFabricante + " and IdcAgenteDestino=" + codigoDistribuidor;
                        reader = db.GetDataReader(sql);
                        if (reader.Read())
                        {
                            umProducto = db.GetFieldValue(reader, 0);
                            if (!Utils.IsBlankField(umProducto))
                                isOK = true;
                        }
                        reader.Close();
                        reader = null;
                    }
                }
            }
            if (!Utils.IsBlankField(UM))
            {
                if (!isOK)
                {
                    //Verificamos si existe conversión de Unidad de Medida entre la UM del agente distribuidor y la de ConnectA para el fabricante en específico
                    if (UMsPreCargadas)
                    {
                        if (htUMsAgentesFab.ContainsKey(UM + ";" + codigoFabricante))
                        {
                            umProducto = htUMsAgentesFab[UM + ";" + codigoFabricante].ToString();
                            isOK = true;
                        }
                    }
                    else
                    {
                        sql = "Select UMc from UMAgentePorFabricante Where IdcAgente = " + codigoDistribuidor + " and UMAgente='" + UM + "' and IdcFabricante=" + codigoFabricante;
                        reader = db.GetDataReader(sql);
                        if (reader.Read())
                        {
                            umProducto = db.GetFieldValue(reader, 0);
                            isOK = true;
                        }
                        reader.Close();
                        reader = null;
                    }
                }
                if (!isOK)
                {
                    //Verificamos si existe conversión de Unidad de Medida entre la UM del agente y la de ConnectA                                        
                    if (UMsPreCargadas)
                    {
                        if (htUMsAgentes.ContainsKey(UM))
                        {
                            umProducto = htUMsAgentes[UM].ToString();
                            isOK = true;
                        }
                    }
                    else
                    {
                        sql = "Select UMc from UMAgente Where IdcAgente = " + codigoDistribuidor + " and UMAgente='" + UM + "'";
                        reader = db.GetDataReader(sql);
                        if (reader.Read())
                        {
                            umProducto = db.GetFieldValue(reader, 0);
                            isOK = true;
                        }
                        reader.Close();
                        reader = null;
                    }
                }
                if (!isOK)
                {
                    //Comprobamos si la unidad de medida que nos ha pasado coincide con una UM de ConnectA
                    if (UMsPreCargadas)
                    {
                        if (htUMs.ContainsKey(UM))
                        {
                            umProducto = htUMs[UM].ToString();
                            isOK = true;
                        }
                    }
                    else
                    {
                        sql = "Select UMc from UnidadesMedida where UMc='" + UM + "'";
                        reader = db.GetDataReader(sql);
                        if (reader.Read())
                        {
                            umProducto = db.GetFieldValue(reader, 0);
                            isOK = true;
                        }
                    }
                }
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return isOK;
    }

    public string AjustarUnidadMedidaPorPrecio(Database db, string pIdcProducto, string pIdcFabricante, string linCantidad, string linPrecioBase, string linDescuento, string linImporteBruto, string pUMAjustar)
    {
        string umResultado = pUMAjustar;

        double nLinCantidad = 0;
        double nLinPrecioBase = 0;
        double nLinDescuento = 0;
        double nLinImporteBruto = 0;

        if (!Utils.IsBlankField(linCantidad)) Double.TryParse(linCantidad, out nLinCantidad);
        if (!Utils.IsBlankField(linPrecioBase)) Double.TryParse(linPrecioBase, out nLinPrecioBase);
        if (!Utils.IsBlankField(linDescuento)) Double.TryParse(linDescuento, out nLinDescuento);
        if (!Utils.IsBlankField(linImporteBruto)) Double.TryParse(linImporteBruto, out nLinImporteBruto);

        //Obtener el preciobase del producto en la línea factura a integrar:
        //   Si Precio*cantidad – descuento*cantidad=Preciobruto --> coger el preciobase
        //   Si Precio-descuento= Preciobruto --> coger el preciobase/cantidad
        //   Si Precio*cantidad – descuento*cantidad<>Preciobruto and Precio-descuento<>Preciobruto --> Coger el Preciobruto/cantidad
        double nPrecBase = 0;
        if (((nLinPrecioBase * nLinCantidad) - (nLinDescuento * nLinCantidad)) == nLinImporteBruto)
        {
            nPrecBase = nLinPrecioBase;
        }
        else if (((nLinPrecioBase - nLinDescuento) == nLinImporteBruto) && nLinCantidad != 0)
        {
            nPrecBase = nLinPrecioBase / nLinCantidad;
        }
        else if (nLinCantidad != 0)
        {
            nPrecBase = nLinImporteBruto / nLinCantidad;
        }
        if (nPrecBase != 0)
        {
            //Una vez tengo el preciobase, encontramos el % de diferencia entre el preciobase y el preciotarifa según la unidad de medida 
            //que viene en la línea:
            //   Si Um producto = UM1 (la UM1 de la tabla precios) --> ValorComparar=Abs(Preciobase/PreciotarifaUM1-1)
            //   Si um producto=UM2 (la UM2 de la tabla precios) --> ValorComparar=Abs(Preciobase/PreciotarifaUM2-1)
            //   ...
            BDProducto objProd = new BDProducto();
            string precTarifa = "";
            string codigoProductoFab = objProd.ValorCampoProductosAgentes(db, "Codigo", pIdcFabricante, pIdcProducto);
            string jerarquiaProducto = objProd.ValorCampoProductosAgentes(db, "Jerarquia", pIdcFabricante, pIdcProducto);
            if (!Utils.IsBlankField(codigoProductoFab))
            {
                precTarifa = objProd.PrecioUnitarioProducto(db, pIdcFabricante, codigoProductoFab, pUMAjustar, "DI%");
            }
            if (Utils.IsBlankField(precTarifa))
            {
                if (!Utils.IsBlankField(jerarquiaProducto))
                {
                    precTarifa = objProd.PrecioUnitarioProducto(db, pIdcFabricante, jerarquiaProducto, pUMAjustar, "DI%");
                }
            }
            double nPrecTarifa = 0;
            if (!Utils.IsBlankField(precTarifa)) Double.TryParse(precTarifa, out nPrecTarifa);

            if (nPrecTarifa != 0)
            {
                double nDifPrec = Math.Abs(((nPrecTarifa - nPrecBase) * 100) / nPrecTarifa);

                //Una vez tengamos el ValorComparar, si ValorComparar > 65% entonces encontrar una UM 
                //tal que ValorComparar < Abs(Preciobase/PreciotarifaUMdef) < 35% 
                //    Si encontramos cambiamos por la UM que hemos encontrado sino NO.
                if (nDifPrec > 65)
                {
                    string umEncontrada = "";
                    if (!Utils.IsBlankField(codigoProductoFab))
                    {
                        umEncontrada = objProd.UMProductoInferiorA(db, pIdcFabricante, codigoProductoFab, nPrecBase.ToString(), "35", "DI%");
                    }
                    if (Utils.IsBlankField(umEncontrada))
                    {
                        if (!Utils.IsBlankField(jerarquiaProducto))
                        {
                            umEncontrada = objProd.UMProductoInferiorA(db, pIdcFabricante, jerarquiaProducto, nPrecBase.ToString(), "35", "DI%");
                        }
                    }
                    if (!Utils.IsBlankField(umEncontrada)) umResultado = umEncontrada;
                }
            }
        }
        return umResultado;
    }


    /// <summary>
    /// Conversión de unidades de medida
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="codFabricante">fabricante</param>
    /// <param name="codProducto">producto</param>
    /// <param name="cantidadInicial">cantidad inicial</param>
    /// <param name="UMPrimaria">unidad primaria</param>
    /// <param name="UMSecundaria">unidad secundaria</param>
    /// <returns>Objecto de clase CalcUnidadMedida con flag de encontrado y cantidad ajustadas</returns>
    public CalcUnidadMedida ConversionUnidadesMedida(Database db, string codFabricante, string codProducto, string cantidadInicial, string UMPrimaria, string UMSecundaria)
    {
        bool isOK = false;
        double cant = 0;
        if (!Utils.IsBlankField(UMSecundaria))
        {
            if (UMsPreCargadas)
            {
                //Primero buscaremos en la tabla ConvUMProducto
                if (htConvUMProd.ContainsKey(codFabricante + ";" + codProducto + ";" + UMPrimaria + ";" + UMSecundaria))
                {
                    sConvUMProd conv = (sConvUMProd)htConvUMProd[codFabricante + ";" + codProducto + ";" + UMPrimaria + ";" + UMSecundaria];
                    cant = BuscarCantidadConvertida(cantidadInicial, conv.cantBase, conv.cant, false);
                    if (cant != 0)
                    {
                        isOK = true;
                    }                    
                }
                else
                {
                    //La inversa
                    if (htConvUMProd.ContainsKey(codFabricante + ";" + codProducto + ";" + UMSecundaria + ";" + UMPrimaria))
                    {
                        sConvUMProd conv = (sConvUMProd)htConvUMProd[codFabricante + ";" + codProducto + ";" + UMSecundaria + ";" + UMPrimaria];
                        cant = BuscarCantidadConvertida(cantidadInicial, conv.cantBase, conv.cant, true);
                        if (cant != 0)
                        {
                            isOK = true;
                        }
                    }
                }
            }
            else
            {
                //Primero buscaremos en la tabla ConvUMProducto
                string sql = "Select CantidadBase, Cantidad from ConvUMProducto " +
                                "Where IdcAgente = " + codFabricante + " and IdcProducto = " + codProducto + " " +
                                "and UMc = '" + UMPrimaria + "' and UMc2 = '" + UMSecundaria + "'";
                cant = BuscarCantidadConvertida(db, cantidadInicial, sql, false);
                if (cant != 0)
                {
                    isOK = true;
                }
                else
                {
                    //La inversa
                    sql = "Select CantidadBase, Cantidad from ConvUMProducto " +
                        "Where IdcAgente = " + codFabricante + " and IdcProducto = " + codProducto + " " +
                        "and UMc2 = '" + UMPrimaria + "' and UMc = '" + UMSecundaria + "'";
                    cant = BuscarCantidadConvertida(db, cantidadInicial, sql, true);
                    if (cant != 0)
                    {
                        isOK = true;
                    }
                }
            }
        }
        return new CalcUnidadMedida(isOK, cant);
    }

    /// <summary>Conversión de unidades de medida</summary>
    /// <param name="pUM1">unidad primaria</param>
    /// <param name="pUM2">unidad secundaria</param>
    /// <param name="pCantidad">cantidad inicial</param>
    /// <param name="pIdcProducto">producto</param>
    /// <param name="pIdcFabricante">fabricante</param>
    /// <returns>Objecto de clase CalcUnidadMedida con flag de encontrado y cantidad ajustadas</returns>
    public string ConversionEntreUnidadesMedida(Database db, string pUM1, string pUM2, string pCantidad, string pIdcProducto, string pIdcFabricante)
    {
        DbDataReader reader = null;
        string resultado = "";
        string strSql = "";

        double? cantBase = 0;
        double? cant = 0;
        double? cantConv = 0;
        if (pUM1.Trim() != "" && pUM2.Trim() != "")
        {
            //Primero buscaremos en la tabla ConvUMProducto
            strSql = " SELECT CantidadBase, Cantidad" +
                            " FROM ConvUMProducto" +
                            " WHERE IdcAgente        = " + pIdcFabricante +
                            " AND   IdcProducto      = " + pIdcProducto +
                            " AND   UMc              = '" + pUM1 + "'" +
                            " AND   UMc2             = '" + pUM2 + "'";
            reader = db.GetDataReader(strSql);
            while (reader.Read())
            {
                cantBase = Utils.StringToDouble(db.GetFieldValue(reader, 0));
                cant = Utils.StringToDouble(db.GetFieldValue(reader, 1));
                if (cantBase != 0)
                {
                    cantConv = (Utils.StringToDouble(pCantidad) * cant) / cantBase;
                }
            }
            reader.Close();
            reader = null;
            if (cantConv == 0)
            {
                //La inversa
                strSql = " SELECT CantidadBase, Cantidad" +
                                " FROM ConvUMProducto" +
                                " WHERE IdcAgente        = " + pIdcFabricante +
                                " AND   IdcProducto      = " + pIdcProducto +
                                " AND   UMc2             = '" + pUM1 + "'" +
                                " AND   UMc              = '" + pUM2 + "'";
                reader = db.GetDataReader(strSql);
                while (reader.Read())
                {
                    cantBase = Utils.StringToDouble(db.GetFieldValue(reader, 0));
                    cant = Utils.StringToDouble(db.GetFieldValue(reader, 1));
                    if (cant != 0)
                    {
                        cantConv = (Utils.StringToDouble(pCantidad) * cantBase) / cant;
                    }
                }
                reader.Close();
                reader = null;
            }
        }
        if (cantConv != 0)
            resultado = cantConv.ToString().Trim();

        return resultado;
    }

    /// <summary>
    /// Buscar cantidad convertida
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="cantidadInicial">cantidad inicial</param>
    /// <param name="sql">sql de búsqueda</param>
    /// <param name="inverse">true si es cálculo de cantidad inversa</param>
    /// <returns>cantidad convertida o cero si no puede convertir</returns>
    private double BuscarCantidadConvertida(Database db, string cantidadInicial, string sql, bool inverse)
    {
      DbDataReader reader = null;
      double cantidadConv = 0;
      try
      {
        reader = db.GetDataReader(sql);
        if (reader.Read())
        {
          double cantBase = Utils.StringToDouble(db.GetFieldValue(reader, 0));
          double cant = Utils.StringToDouble(db.GetFieldValue(reader, 1));

          if (inverse)
          {
            if (cant != 0)
            {
              cantidadConv = (Utils.StringToDouble(cantidadInicial) * cantBase) / cant;
            }
          }
          else if (cantBase != 0)
          {
            cantidadConv = (Utils.StringToDouble(cantidadInicial) * cant) / cantBase;
          }
        }
      }
      finally
      {
        if (reader != null)
          reader.Close();
      }
      return cantidadConv;
    }
    private double BuscarCantidadConvertida(string cantidadInicial, double cantBase, double cant, bool inverse)
    {        
        double cantidadConv = 0;               
        if (inverse)
        {
            if (cant != 0)
            {
                cantidadConv = (Utils.StringToDouble(cantidadInicial) * cantBase) / cant;
            }
        }
        else if (cantBase != 0)
        {
            cantidadConv = (Utils.StringToDouble(cantidadInicial) * cant) / cantBase;
        }                   
        return cantidadConv;
    }

    /// <summary>
    /// Buscar unidades de medida logística. Ajusta variables pesoTotal, volumenTotal,
    /// UMcVolumen y UMcPeso.
    /// </summary>
    /// <param name="db">base de datos</param>
    /// <param name="linea">registro de línea de albarn</param>
    /// <returns>true si es correcto</returns>
    public bool BuscarUnidadesMedidaLogisticas(Database db, string codigoProducto, string UMProducto, string fabricanteOdistribuidor, Volumen vol, Peso peso, string cantidadLinea)
    {
        return BuscarUnidadesMedidaLogisticas(db, codigoProducto, UMProducto, fabricanteOdistribuidor, vol, peso, cantidadLinea, true, true);
    }

    public bool BuscarUnidadesMedidaLogisticas(Database db, string codigoProducto, string UMProducto, string fabricanteOdistribuidor, Volumen vol, Peso peso, string cantidadLinea, bool volSI, bool pesoSI)
    {
        bool isOK = false;
        DbDataReader reader = null;
        try
        {
            if (pesoSI)
            {
                peso.HaSidoCalculado = false;
            }
            if (volSI)
            {
                vol.HaSidoCalculado = false;
            }

            //Buscar en el maestro de productos
            string sql = "SELECT Productos.PesoBruto, Productos.UMcPeso, Productos.Volumen, Productos.UMVolumen, Productos.UMc, Productos.IdcFabricante "+
                         "FROM Productos WHERE Productos.IdcProducto = "+codigoProducto+ " and Productos.UMc='"+UMProducto+"'";
            reader = db.GetDataReader(sql);
            string umcProd = "";
            string idcFab = fabricanteOdistribuidor;
            if (reader.Read())
            {
                double cantidad = Utils.StringToDouble(cantidadLinea);
                //PesoTotal = Productos.PesoBruto x Cantidad
                //VolumenTotal = Productos.Volumen x Cantidad
                if (pesoSI)
                {
                    peso.PesoTotal = Utils.StringToDouble(db.GetFieldValue(reader, 0)) * cantidad;
                    peso.UMcPeso = db.GetFieldValue(reader, 1);
                }
                if (volSI)
                {
                    vol.VolumenTotal = Utils.StringToDouble(db.GetFieldValue(reader, 2)) * cantidad;
                    vol.UMcVolumen = db.GetFieldValue(reader, 3);
                }
                umcProd = db.GetFieldValue(reader, 4);
                idcFab = db.GetFieldValue(reader, 5);
                isOK = true;
            }
            reader.Close();
            reader = null;

            //Si coinciden, salir del método porque ya tenemos la información que nos interesa.
            if (!umcProd.Equals(UMProducto))
            {
                //No coinciden: buscaremos las unidades logísticas en la tabla de Conversiones de Unidades de Medida por producto
                sql = "Select ConvUMProducto.CantidadBase, ConvUMProducto.Cantidad,ConvUMProducto.Volumen, ConvUMProducto.UMVolumen," +
                        "ConvUMProducto.Peso, ConvUMProducto.UMPeso " +
                        "FROM ConvUMProducto Where IdcAgente =" + idcFab + " " + // (de la consulta anterior) 
                        "and IdcProducto = " + codigoProducto + " and UMc2 = '" + UMProducto + "'";
                double variableTemp = 0;
                double prodCant = 0;
                double cantidad = Utils.StringToDouble(cantidadLinea); 
                reader = db.GetDataReader(sql);
                while (reader.Read())
                {
                    //Puede devolver más de un registro. Tomamos el primero.
                    prodCant = Utils.StringToDouble(db.GetFieldValue(reader,1));
                    if(prodCant>0) 
                    {
                        //VariableTemp = (Cantidad * ConvUMProducto.CantidadBase) / ConvUMProducto.Cantidad
	                    //PesoTotal = ConvUMProducto.Peso x VariableTemp
			            //VolumenTotal = ConvUMProducto.Volumen x VariableTemp
                        variableTemp = (cantidad * Utils.StringToDouble(db.GetFieldValue(reader,0))) / prodCant;
                        if (volSI)
                        {
                            vol.VolumenTotal = Utils.StringToDouble(db.GetFieldValue(reader, 2)) * variableTemp;
                            vol.UMcVolumen = db.GetFieldValue(reader, 3);
                        }
                        if (pesoSI)
                        {
                            peso.PesoTotal = Utils.StringToDouble(db.GetFieldValue(reader, 4)) * variableTemp;
                            peso.UMcPeso = db.GetFieldValue(reader, 5);
                        }
                        isOK = true;
                        break;
                    }
                }

                //Si no hay resultados, todos los valores a 0
                if (!isOK)
                {
                    if (pesoSI)
                    {
                        peso.PesoTotal = 0;
                        peso.UMcPeso = "";
                    }
                    if (volSI)
                    {
                        vol.VolumenTotal = 0;
                        vol.UMcVolumen = "";
                    }
                }
                else
                {
                    if (pesoSI)
                    {
                        peso.HaSidoCalculado = true;
                    }
                    if (volSI)
                    {
                        vol.HaSidoCalculado = true;
                    }
                }
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return isOK;
    }

    public string ObtenerUMcDistribuidorProducto(Database db, string pIdcAgente, string pIdcProducto, string pCodigoProducto, string pUM)
    {
        DbDataReader reader = null;
        string resultado = "";
        string strSql = "";
        if (String.IsNullOrEmpty(pUM))
        {
            strSql = " SELECT UMc" +
                            " FROM Productos" +
                            " WHERE IdcProducto = " + pIdcProducto; 
            reader = db.GetDataReader(strSql);
            while (reader.Read())
            {
                resultado = db.GetFieldValue(reader, 0);
            }
            reader.Close();
            reader = null;
        }
        else
        {
            //Verificamos si este producto para este agente tiene una unidad de medida Connect@ fijada
            strSql = " SELECT UMcFija" +
                    " FROM ProductosAgentesUMsFijas" +
                    " WHERE IdcAgente = " + pIdcAgente +
                    " AND Codigo = '" + pCodigoProducto + "'";
            reader = db.GetDataReader(strSql);
            while (reader.Read())
            {
                resultado = db.GetFieldValue(reader, 0);
            }
            reader.Close();
            reader = null;

            if (String.IsNullOrEmpty(resultado))
            {
                //Verificamos si existe conversión de Unidad de Medida entre la UM del agente y la de Connect@
                strSql = " SELECT UMc" +
                        " FROM UMAgente" +
                        " WHERE IdcAgente = " + pIdcAgente +
                        " AND   UMAgente = '" + pUM + "'";
                reader = db.GetDataReader(strSql);
                while (reader.Read())
                {
                    resultado = db.GetFieldValue(reader, 0);
                }
                reader.Close();
                reader = null;

                //Si no existe, comprobamos si la unidad de medida que nos ha pasado coincide con una UM de Connect@
                if (String.IsNullOrEmpty(resultado))
                {
                    strSql = " SELECT UMc" +
                            " FROM UnidadesMedida" +
                            " WHERE UMc = '" + pUM + "'";
                    reader = db.GetDataReader(strSql);
                    while (reader.Read())
                    {
                        resultado = db.GetFieldValue(reader, 0);
                    }
                    reader.Close();
                    reader = null;
                }
            }
        }
        return resultado;
    }

    /// <summary>Obtiene el valor de un campo concreto de unidades medida de agente</summary>
    public string ValorCampoConvUMProducto(Database db, string pNombreCampo, string pIdcAgente, string pIdcProducto, string pUM, string pUM2)
    {
        if (Utils.IsBlankField(pIdcAgente)) return "";
        if (Utils.IsBlankField(pIdcProducto)) return "";
        DbDataReader reader = null;
        string resultado = "";
        string strSql = " SELECT " + pNombreCampo +
                        " FROM ConvUMProducto" +
                        " WHERE IdcAgente        = " + pIdcAgente +
                        " AND   IdcProducto      = " + pIdcProducto;
        if (!String.IsNullOrEmpty(pUM))
            strSql += " AND UMc                   = '" + pUM + "'";
        if (!String.IsNullOrEmpty(pUM2))
            strSql += " AND UMc2                   = '" + pUM2 + "'";
        reader = db.GetDataReader(strSql);
        while (reader.Read())
        {
            resultado = db.GetFieldValue(reader, 0);
        }
        reader.Close();
        reader = null;
        return resultado;
    }

    /// <summary>Obtener el valor del volumen base de una unidad de pedida</summary>
    /// <param name="pUM2">unidad secundaria</param>
    /// <param name="pCantidad">cantidad inicial</param>
    /// <param name="pIdcProducto">producto</param>
    /// <param name="pIdcFabricante">fabricante</param>
    /// <returns>volumen base</returns>
    public string ObtenerVolumenBaseUnidadMedida(Database db, string pUM2, string pIdcProducto, string pIdcFabricante)
    {
        DbDataReader reader = null;
        string resultado = "";
        string strSql = "";

        double? volumen = 0;
        double? cant = 0;
        double? volumenConv = 0;

        if (pUM2.Trim() != "")
        {
            //Buscaremos en la tabla ConvUMProducto
            strSql = " SELECT Volumen, Cantidad" +
                            " FROM ConvUMProducto" +
                            " WHERE IdcAgente        = " + pIdcFabricante + 
                            " AND   IdcProducto      = " + pIdcProducto +
                            " AND   UMc2             = '" + pUM2 + "'";
            reader = db.GetDataReader(strSql);
            while (reader.Read())
            {
                volumen = Utils.StringToDouble(db.GetFieldValue(reader, 0));
                cant = Utils.StringToDouble(db.GetFieldValue(reader, 1));
                if (cant != 0)
                {
                    volumenConv = volumen / cant;
                    break;
                }
            }
            reader.Close();
            reader = null;
        }
        if (volumenConv != 0)
            resultado = volumenConv.ToString().Trim();

        return resultado;
    }

    /// <summary>Obtener el valor del peso base de una unidad de pedida</summary>
    /// <param name="pUM2">unidad secundaria</param>
    /// <param name="pCantidad">cantidad inicial</param>
    /// <param name="pIdcProducto">producto</param>
    /// <param name="pIdcFabricante">fabricante</param>
    /// <returns>peso base</returns>
    public string ObtenerPesoBaseUnidadMedida(Database db, string pUM2, string pIdcProducto, string pIdcFabricante)
    {
        DbDataReader reader = null;
        string resultado = "";
        string strSql = "";

        double? peso = 0;
        double? cant = 0;
        double? pesoConv = 0;

        if (pUM2.Trim() != "")
        {
            //Buscaremos en la tabla ConvUMProducto
            strSql = " SELECT Peso, Cantidad" +
                            " FROM ConvUMProducto" +
                            " WHERE IdcAgente        = " + pIdcFabricante +
                            " AND   IdcProducto      = " + pIdcProducto +
                            " AND   UMc2             = '" + pUM2 + "'";
            reader = db.GetDataReader(strSql);
            while (reader.Read())
            {
                peso = Utils.StringToDouble(db.GetFieldValue(reader, 0));
                cant = Utils.StringToDouble(db.GetFieldValue(reader, 1));
                if (cant != 0)
                {
                    pesoConv = peso / cant;
                    break;
                }
            }
            reader.Close();
            reader = null;
        }
        if (pesoConv != 0)
            resultado = pesoConv.ToString().Trim();

        return resultado;
    }

  }
}
