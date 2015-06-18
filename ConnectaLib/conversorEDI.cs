using System;
using System.IO;
using System.Collections;
using System.Net;
using System.Net.Mail;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Data.Common;

namespace ConnectaLib
{    
	/// <summary>
	/// Clase para realizar la conversión EDI.
	/// </summary>
    public class conversorEDI
    {
        public bool bResult = false;

        private string sSipTypeName = "Conversor.EDI";
        private Hashtable htUMsEDI = new Hashtable();
        private const string FORMATO_ORDERS = "ORDERS";
        private const string FORMATO_SLSRPT = "SLSRPT";
        private const string DELIMITER = "+";
        private const string DELIMITER2 = ":";

        private struct SIMPLIFICADA
        {
            public string numFactura;
            //public string numLinea;
            public string codigoProducto;
            public string descProducto;
            public string fabricante;
            public string codigoProductoFab;
            public string ean13;
            public string cantidad;
            //public string UM;
            public string precioBase;
            //public string descuentos;
            public string precioBrutoTotal;
            public string fechaFactura;
            public string ejercicio;
            public string codigoCliente;
            public string nombreCliente;
            //public string razonSocial;
            //public string cif;
            //public string direccion;
            //public string poblacion;
            //public string cp;
            //public string ruta;
            //public string nombreRuta;
            //public string codigoComercial;
            //public string nombreComercial;
            //public string peso;
            //public string UMPeso;
            //public string tipoCliente;            
        }

        private struct REAPROVISIONAMIENTOSFABRICANTE
        {
            public string CodigoDistribuidor;
            public string NumPedido;
            public string FechaPedido;
            public string FechaEntrega;
            //public string HoraEntregaDesde;
            //public string HoraEntregaHasta;
            //public string FechaEntregaComp;
            public string DireccionEntrega;
            public string Poblacion;
            public string CodigoPostal;
            public string ReferenciaFabricante;
            //public string InfoAdicional;
            //public string ContactoDistribuidor;
            //public string ContactoFabricante;
            //public string IndRecogerEnvases;
            //public string Almacen;
            //public string SituacionPedido;
            //public string PartidaPresupuesto;
            //public string Libre2;
            //public string Libre3;
            //public string Libre4;
            //public string Libre5;
            public string NumLinea;
            //public string NumLineaComp;
            //public string TipoLinea;
            public string CodigoProducto;
            public string Descripcion;
            public string Cantidad;
            public string UM;
            //public string CantidadGestion;
            //public string UMGestion;
            //public string FechaCarga;
            //public string AccionLinea;
            //public string MotivoAnulacion;
            //public string MotivoModificacion;
            //public string NumLineaPadre;
            //public string Libre2b;
            //public string Libre3b;
            //public string Libre4b;
            //public string Libre5b;
            //public string FechaCreacion;
            //public string FechaModificacion;
            //public string UsuarioCreacion;
            //public string UsuarioModificacion;
            //public string Accion;
            public string TipoReaprovisionamiento;
            //public string CodigoCliente;
            //public string NumPedidoCliente;
            //public string TipoPedido;
        }

        //Key = um EDI; value = um ConnectA
        private void LlenarUMsEDI()
        {
            htUMsEDI.Add("8", "PAL");
            htUMsEDI.Add("9", "PAL");
            htUMsEDI.Add("210", "PAL");
            htUMsEDI.Add("211", "PAL");
            htUMsEDI.Add("212", "PAL");
            htUMsEDI.Add("AE", "");
            htUMsEDI.Add("APE", "VB3");
            htUMsEDI.Add("AT", "");
            htUMsEDI.Add("BA", "1");
            htUMsEDI.Add("BC", "BOX");
            htUMsEDI.Add("BE", "BLT");
            htUMsEDI.Add("BO", "BOT");
            htUMsEDI.Add("BS", "BOT");
            htUMsEDI.Add("BX", "BOX");
            htUMsEDI.Add("CA", "LAT");
            htUMsEDI.Add("CR", "BOX");
            htUMsEDI.Add("CS", "BOX");
            htUMsEDI.Add("CT", "BOX");
            htUMsEDI.Add("CU", "LAT");
            htUMsEDI.Add("CWH", "LAT");
            htUMsEDI.Add("CX", "LAT");
            htUMsEDI.Add("PC", "PAQ");
            htUMsEDI.Add("PK", "PAQ");
            htUMsEDI.Add("TWE", "PAQ");
        }

        /// <summary>
        /// Comprobar si el agente es un distribuidor o un fabricante
        /// </summary>
        /// <param name="db">database</param>        
        /// <param name="agent">agente</param>                
        private string TipoAgente(Database db, string agent)
        {
            DbDataReader cursor = null;
            string tipo = "";
            try
            {
                string sql = "select Tipo from Agentes where IdcAgente = " + agent;
                cursor = db.GetDataReader(sql);
                if (cursor.Read())
                    tipo = db.GetFieldValue(cursor, 0);
            }
            finally
            {
                if (cursor != null)
                    cursor.Close();
            }
            return tipo;
        }

        /// <summary>
        /// Do the job
        /// </summary>
        public void DoWork(string pAgent, string pPath)
        {
            bResult = false;
            try
            {
                Database db = Globals.GetInstance().GetDatabase();
                string backupFilename = null;
                bool ok = true;
                //Mirar si existen ficheros que cumplan el nomenclator EDI
                bool hayFicheros = false;
                string sFileName = string.Empty;
                if (System.IO.Directory.Exists(pPath))
                {
                    //Obtener la lista de ficheros de la carpeta
                    string[] filesInFolder = null;
                    filesInFolder = System.IO.Directory.GetFiles(pPath, "*.EDI");
                    if (filesInFolder != null && filesInFolder.Length > 0)
                        sFileName = Utils.WithoutPath(filesInFolder[0]);
                    else
                    {
                        filesInFolder = System.IO.Directory.GetFiles(pPath, "*.LOT");
                        if (filesInFolder != null && filesInFolder.Length > 0)
                            sFileName = Utils.WithoutPath(filesInFolder[0]);
                    }
                    hayFicheros = !string.IsNullOrEmpty(sFileName);
                }

                if (hayFicheros)
                {
                    Globals.GetInstance().GetLog2().Info(pAgent, sSipTypeName, "Conversor EDI se inicia.");

                    //Proceso EDI
                    //---------------------------                    
                    //Comprobamos que exista el fichero EDI
                    if (System.IO.File.Exists(pPath + "\\" + sFileName))
                    {
                        //Hacer copia backup al backupbox
                        Backup bk = new Backup();
                        ok = bk.DoBackup(pAgent, sSipTypeName, pPath + "\\" + sFileName);
                        backupFilename = bk.BackupFilenamePath();
                        //Eliminamos el fichero del path de entrada
                        if (File.Exists(pPath + "\\" + sFileName))
                            File.Delete(pPath + "\\" + sFileName);
                        //Realizar la conversión hacia los ficheros de salida correspondientes según el formato de entrada
                        if (sFileName.ToUpper().Contains(FORMATO_ORDERS))
                        {
                            string tipo = TipoAgente(db, pAgent);
                            if (tipo == Constants.FABRICANTE)
                                GenerarFicherosReaprovisionamientosFabricante(backupFilename, pPath);
                            else if (tipo == Constants.DISTRIBUIDOR)
                                GenerarFicherosReaprovisionamientosDistribuidor(backupFilename, pPath);
                        }
                        else if (sFileName.ToUpper().Contains(FORMATO_SLSRPT))
                            GenerarFicherosVentas(backupFilename, pPath);
                    }
                    Globals.GetInstance().GetLog2().Info(pAgent, sSipTypeName, "Conversor EDI finaliza satisfactoriamente.");
                }

                bResult = true;
            }
            catch (Exception e)
            {
                Globals.GetInstance().GetLog2().Error(pAgent, sSipTypeName, e);
            }
        }        
                          
        /// <summary>
        /// Generar los ficheros de salida a partir del fichero de entrada 
        /// </summary>
        /// <param name="fileIn">fichero de entrada</param>        
        /// <param name="pathOut">path de salida de los ficheros resultantes</param>                
        private void GenerarFicherosReaprovisionamientosFabricante(string fileIn, string pathOut)
        {
            //Generamos fichero reaprovisionamientos fabricante
            string[] lines = File.ReadAllLines(fileIn);
            EANCOMSegmentsD96AS3 ediLib = new EANCOMSegmentsD96AS3();
            REAPROVISIONAMIENTOSFABRICANTE item = new REAPROVISIONAMIENTOSFABRICANTE();
            List<REAPROVISIONAMIENTOSFABRICANTE> list = new List<REAPROVISIONAMIENTOSFABRICANTE>();                 
            string s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14, s15, s16, s17, s18, s19, s20, s21, s22, s23;
            bool primeraLinea = true;            
            foreach (string line in lines)
            {
                s1 = s2 = s3 = s4 = s5 = s6 = s7 = s8 = s9 = s10 = s11 = s12 = s13 = s14 = s15 = s16 = s17 = s18 = s19 = s20 = s21 = s22 = s23 = "";
                if (line.StartsWith("UNB"))
                {
                    ediLib.SegmentUNBRead(line, DELIMITER, ref s1, ref s2, ref s3, ref s4, ref s5, ref s6, ref s7, ref s8, ref s9, ref s10,
                        ref s11, ref s12, ref s13, ref s14, ref s15, ref s16, ref s17, ref s18);
                    item.CodigoDistribuidor = ObtenerValor(s2, 1, DELIMITER2);
                }
                else if (line.StartsWith("BGM"))
                {
                    ediLib.SegmentBGMRead(line, DELIMITER, ref s1, ref s2, ref s3, ref s4, ref s5, ref s6, ref s7);
                    item.TipoReaprovisionamiento = ObtenerValor(s1, 1, DELIMITER2);
                    item.NumPedido = ObtenerValor(s2, 1, DELIMITER2);
                }
                else if (line.StartsWith("DTM"))
                {
                    ediLib.SegmentDTMRead(line, DELIMITER, ref s1, ref s2, ref s3);
                    if (ObtenerValor(s1, 1, DELIMITER2) == "137")
                        item.FechaPedido = ObtenerValor(s1, 2, DELIMITER2);
                    else if (ObtenerValor(s1, 1, DELIMITER2) == "2")
                        item.FechaEntrega = ObtenerValor(s1, 2, DELIMITER2);
                }
                else if (line.StartsWith("NAD"))
                {
                    ediLib.SegmentNADRead(line, DELIMITER, ref s1, ref s2, ref s3, ref s4, ref s5, ref s6, ref s7, ref s8, ref s9, ref s10,
                        ref s11, ref s12, ref s13, ref s14, ref s15, ref s16, ref s17, ref s18, ref s19, ref s20, ref s21, ref s22, ref s23);
                    if ((ObtenerValor(s1, 1, DELIMITER2) == "MS") || (ObtenerValor(s1, 1, DELIMITER2) == "BY"))
                    {
                        item.CodigoDistribuidor = ObtenerValor(s2, 1, DELIMITER2);
                        item.DireccionEntrega = ObtenerValor(s5, 1, DELIMITER2);
                        item.Poblacion = ObtenerValor(s6, 1, DELIMITER2);
                        item.CodigoPostal = ObtenerValor(s8, 1, DELIMITER2);
                    }
                    else if (ObtenerValor(s1, 1, DELIMITER2) == "SU")
                    {
                        item.ReferenciaFabricante = ObtenerValor(s2, 1, DELIMITER2);
                    }
                }
                else if (line.StartsWith("LIN"))
                {
                    //Grabar línea
                    if (!primeraLinea)
                        list.Add(item);                                            
                    ediLib.SegmentLINRead(line, DELIMITER, ref s1, ref s2, ref s3, ref s4, ref s5, ref s6, ref s7, ref s8, ref s9, ref s10);
                    item.NumLinea = ObtenerValor(s1, 1, DELIMITER2);
                    primeraLinea = false;
                }
                else if (line.StartsWith("PIA"))
                {
                    ediLib.SegmentPIARead(line, DELIMITER, ref s1, ref s2, ref s3, ref s4, ref s5, ref s6, ref s7, ref s8, ref s9, ref s10,
                        ref s11, ref s12, ref s13, ref s14, ref s15, ref s16, ref s17, ref s18, ref s19, ref s20, ref s21);
                    item.CodigoProducto = ObtenerValor(s2, 1, DELIMITER2);
                }
                else if (line.StartsWith("IMD"))
                {
                    ediLib.SegmentIMDRead(line, DELIMITER, ref s1, ref s2, ref s3, ref s4, ref s5, ref s6, ref s7, ref s8, ref s9);
                    item.Descripcion = ObtenerValor(s3, 4, DELIMITER2);
                }
                else if (line.StartsWith("QTY"))
                {
                    ediLib.SegmentQTYRead(line, DELIMITER, ref s1, ref s2, ref s3);
                    if (ObtenerValor(s1, 1, DELIMITER2) == "21") //21 - cantidad pedida
                    {
                        item.Cantidad = ObtenerValor(s1, 2, DELIMITER2);
                        string UMEdi = ObtenerValor(s1, 3, DELIMITER2);
                        if (htUMsEDI.ContainsKey(UMEdi))
                            item.UM = htUMsEDI[UMEdi].ToString();
                    }
                }
            }
            if (!primeraLinea)
                list.Add(item);

            StreamWriter sw = new StreamWriter(pathOut + "\\" + "ReaprovisionamientosFabricante.txt");
            foreach (REAPROVISIONAMIENTOSFABRICANTE rf in list)
            {
                sw.WriteLine(
                    rf.CodigoDistribuidor + Constants.FIELD_SEPARATOR +
                    rf.NumPedido + Constants.FIELD_SEPARATOR +
                    rf.FechaPedido + Constants.FIELD_SEPARATOR +
                    rf.FechaEntrega + Constants.FIELD_SEPARATOR +
                    "" + Constants.FIELD_SEPARATOR + //HoraEntregaDesde
                    "" + Constants.FIELD_SEPARATOR + //HoraEntregaHasta
                    "" + Constants.FIELD_SEPARATOR + //FechaEntregaComp
                    rf.DireccionEntrega + Constants.FIELD_SEPARATOR +
                    rf.Poblacion + Constants.FIELD_SEPARATOR +
                    rf.CodigoPostal + Constants.FIELD_SEPARATOR +
                    rf.ReferenciaFabricante + Constants.FIELD_SEPARATOR +
                    "" + Constants.FIELD_SEPARATOR + //InfoAdicional
                    "" + Constants.FIELD_SEPARATOR + //ContactoDistribuidor
                    "" + Constants.FIELD_SEPARATOR + //ContactoFabricante
                    "" + Constants.FIELD_SEPARATOR + //IndRecogerEnvases
                    "" + Constants.FIELD_SEPARATOR + //Almacen
                    "" + Constants.FIELD_SEPARATOR + //SituacionPedido
                    "" + Constants.FIELD_SEPARATOR + //PartidaPresupuesto
                    "" + Constants.FIELD_SEPARATOR + //Libre2
                    "" + Constants.FIELD_SEPARATOR + //Libre3
                    "" + Constants.FIELD_SEPARATOR + //Libre4
                    "" + Constants.FIELD_SEPARATOR + //Libre5
                    rf.NumLinea + Constants.FIELD_SEPARATOR +
                    "" + Constants.FIELD_SEPARATOR + //NumLineaComp
                    "" + Constants.FIELD_SEPARATOR + //TipoLinea
                    rf.CodigoProducto + Constants.FIELD_SEPARATOR +
                    rf.Descripcion + Constants.FIELD_SEPARATOR +
                    rf.Cantidad + Constants.FIELD_SEPARATOR +
                    rf.UM + Constants.FIELD_SEPARATOR + //UM
                    "" + Constants.FIELD_SEPARATOR + //CantidadGestion
                    "" + Constants.FIELD_SEPARATOR + //UMGestion
                    "" + Constants.FIELD_SEPARATOR + //FechaCarga
                    "" + Constants.FIELD_SEPARATOR + //AccionLinea
                    "" + Constants.FIELD_SEPARATOR + //MotivoAnulacion
                    "" + Constants.FIELD_SEPARATOR + //MotivoModificacion
                    "" + Constants.FIELD_SEPARATOR + //NumLineaPadre
                    "" + Constants.FIELD_SEPARATOR + //Libre2b
                    "" + Constants.FIELD_SEPARATOR + //Libre3b
                    "" + Constants.FIELD_SEPARATOR + //Libre4b
                    "" + Constants.FIELD_SEPARATOR + //Libre5b
                    "" + Constants.FIELD_SEPARATOR + //FechaCreacion
                    "" + Constants.FIELD_SEPARATOR + //FechaModificacion
                    "" + Constants.FIELD_SEPARATOR + //UsuarioCreacion
                    "" + Constants.FIELD_SEPARATOR + //UsuarioModificacion
                    "" + Constants.FIELD_SEPARATOR + //Accion
                    rf.TipoReaprovisionamiento + Constants.FIELD_SEPARATOR +
                    "" + Constants.FIELD_SEPARATOR + //CodigoCliente
                    "" + Constants.FIELD_SEPARATOR + //NumPedidoCliente
                    "" + Constants.FIELD_SEPARATOR //TipoPedido
                    );
            }
            sw.Close();
        }
        /// <summary>
        /// Generar los ficheros de salida a partir del fichero de entrada 
        /// </summary>
        /// <param name="fileIn">fichero de entrada</param>        
        /// <param name="pathOut">path de salida de los ficheros resultantes</param>                
        private void GenerarFicherosReaprovisionamientosDistribuidor(string fileIn, string pathOut)
        {
        }

        /// <summary>
        /// Generar los ficheros de salida a partir del fichero de entrada 
        /// </summary>
        /// <param name="fileIn">fichero de entrada</param>        
        /// <param name="pathOut">path de salida de los ficheros resultantes</param>                
        private void GenerarFicherosVentas(string fileIn, string pathOut)
        {
            //Generamos simplificada
            string[] lines = File.ReadAllLines(fileIn);
            EANCOMSegmentsD96AS3 ediLib = new EANCOMSegmentsD96AS3();
            SIMPLIFICADA item = new SIMPLIFICADA();
            List<SIMPLIFICADA> list = new List<SIMPLIFICADA>();
            string s1, s2, s3, s4, s5, s6, s7, s8, s9, s10, s11, s12, s13, s14, s15, s16, s17, s18, s19, s20, s21, s22, s23;
            bool primeraLinea = true;
            bool primerLOC = true;
            foreach (string line in lines)
            {
                string prefijo = line.Substring(0, 6);
                s1 = s2 = s3 = s4 = s5 = s6 = s7 = s8 = s9 = s10 = s11 = s12 = s13 = s14 = s15 = s16 = s17 = s18 = s19 = s20 = s21 = s22 = s23 = "";
                if (prefijo.Contains("BGM"))
                {
                    ediLib.SegmentBGMRead(line, DELIMITER, ref s1, ref s2, ref s3, ref s4, ref s5, ref s6, ref s7);
                    s1 = s1.Trim();
                    if ((ObtenerValor(s1, 0, 3) == "73E"))  //Identificador informe de ventas
                    {
                        item.numFactura = ObtenerValor(s1, 40, 35).Trim();
                        if (item.numFactura.Length > 20)
                            item.numFactura = item.numFactura.Substring(3, item.numFactura.Length - 3);
                    }
                }
                else if (prefijo.Contains("NAD"))
                {
                    ediLib.SegmentNADRead(line, DELIMITER, ref s1, ref s2, ref s3, ref s4, ref s5, ref s6, ref s7, ref s8, ref s9, ref s10,
                        ref s11, ref s12, ref s13, ref s14, ref s15, ref s16, ref s17, ref s18, ref s19, ref s20, ref s21, ref s22, ref s23);
                    s1 = s1.Trim();
                    if ((ObtenerValor(s1, 0, 3).Trim() == "MR"))  //Receptor / fabricante                   
                        item.fabricante = ObtenerValor(s1, 3, 35).Trim();                                                                
                }
                else if (prefijo.Contains("LOC"))
                {
                    //Grabar línea
                    if (!primeraLinea && !primerLOC)
                    {
                        if (string.IsNullOrEmpty(item.descProducto))
                            item.descProducto = "Producto " + item.codigoProducto;
                        if (string.IsNullOrEmpty(item.nombreCliente))
                            item.nombreCliente = "Cliente " + item.codigoCliente;
                        list.Add(item);
                        item.descProducto = "";
                        item.nombreCliente = "";
                        primeraLinea = true;                        
                    }
                    ediLib.SegmentLOCRead(line, DELIMITER, ref s1, ref s2, ref s3, ref s4, ref s5, ref s6, ref s7, ref s8, ref s9, ref s10, ref s11, ref s12, ref s13, ref s14);
                    s1 = s1.Trim();
                    if ((ObtenerValor(s1, 0, 3) == "162")) //Ubicación venta
                    {
                        item.codigoCliente = ObtenerValor(s1, 3, 25).Trim();
                    }
                    primerLOC = false;
                }
                else if (prefijo.Contains("DTM"))
                {
                    ediLib.SegmentDTMRead(line, DELIMITER, ref s1, ref s2, ref s3);
                    s1 = s1.Trim();
                    if (ObtenerValor(s1, 0, 3) == "356") // Período de venta
                        item.fechaFactura = ObtenerValor(s1, 3, 35).Trim();                    
                }
                else if (prefijo.Contains("LIN"))
                {
                    //Grabar línea
                    if (!primeraLinea)
                    {
                        if (string.IsNullOrEmpty(item.descProducto))
                            item.descProducto = "Producto " + item.codigoProducto;
                        if (string.IsNullOrEmpty(item.nombreCliente))
                            item.nombreCliente = "Cliente " + item.codigoCliente;
                        list.Add(item);
                        item.descProducto = "";
                        item.nombreCliente = "";
                    }
                    ediLib.SegmentLINRead(line, DELIMITER, ref s1, ref s2, ref s3, ref s4, ref s5, ref s6, ref s7, ref s8, ref s9, ref s10);
                    s1 = s1.Trim();                    
                    item.ejercicio = item.fechaFactura.Substring(0, 4);
                    item.codigoProducto = ObtenerValor(s1, 11, 35).Trim();
                    item.codigoProductoFab = item.codigoProducto;
                    item.ean13 = item.codigoProducto;
                    primeraLinea = false;
                }
                else if (prefijo.Contains("PIA"))
                {
                    ediLib.SegmentPIARead(line, DELIMITER, ref s1, ref s2, ref s3, ref s4, ref s5, ref s6, ref s7, ref s8, ref s9, ref s10,
                        ref s11, ref s12, ref s13, ref s14, ref s15, ref s16, ref s17, ref s18, ref s19, ref s20, ref s21);
                    s1 = s1.Trim();
                    if (ObtenerValor(s1, 0, 3).Trim() == "5") //Identificación del producto
                        item.descProducto = ObtenerValor(s1, 3, 35).Trim();
                }
                else if (prefijo.Contains("MOA"))
                {
                    ediLib.SegmentMOARead(line, DELIMITER, ref s1, ref s2, ref s3, ref s4, ref s5);
                    s1 = s1.Trim();
                    if (ObtenerValor(s1, 0, 3) == "203") //Importe de la línia del artículo
                    {
                        item.precioBrutoTotal = ObtenerValor(s1, 5, 18).Trim();
                        item.precioBrutoTotal = Utils.QuitaCerosIzq(item.precioBrutoTotal).Replace(".", ",");
                    }
                }
                else if (prefijo.Contains("QTY"))
                {
                    ediLib.SegmentQTYRead(line, DELIMITER, ref s1, ref s2, ref s3);
                    s1 = s1.Trim();
                    if (ObtenerValor(s1, 0, 3) == "153") //Cantidad de ventas estadística
                    {
                        item.cantidad = ObtenerValor(s1, 5, 15).Trim();
                        item.cantidad = Utils.QuitaCerosIzq(item.cantidad).Replace(".", ",");
                        double c, pbt;
                        if (double.TryParse(item.cantidad, out c) && double.TryParse(item.precioBrutoTotal, out pbt))
                            item.precioBase = c != 0 ? (pbt / c).ToString() : pbt.ToString();
                    }
                }
            }
            if (!primeraLinea)  
            {
                if (string.IsNullOrEmpty(item.descProducto))
                    item.descProducto = "Producto " + item.codigoProducto;
                if (string.IsNullOrEmpty(item.nombreCliente))
                    item.nombreCliente = "Cliente " + item.codigoCliente;
                list.Add(item);
                item.descProducto = "";
                item.nombreCliente = "";
            }

            StreamWriter sw = new StreamWriter(pathOut + "\\" + "VentasDistribuidorEDI.txt");
            foreach (SIMPLIFICADA s in list)
            {
                sw.WriteLine(
                    s.numFactura + Constants.FIELD_SEPARATOR +
                    "" + Constants.FIELD_SEPARATOR + //s.numLinea
                    s.codigoProducto + Constants.FIELD_SEPARATOR +
                    s.descProducto + Constants.FIELD_SEPARATOR +
                    s.fabricante + Constants.FIELD_SEPARATOR +
                    s.codigoProductoFab + Constants.FIELD_SEPARATOR +
                    s.ean13 + Constants.FIELD_SEPARATOR +
                    s.cantidad + Constants.FIELD_SEPARATOR +
                    "" + Constants.FIELD_SEPARATOR + //s.UM
                    s.precioBase + Constants.FIELD_SEPARATOR +
                    "" + Constants.FIELD_SEPARATOR + //s.descuentos
                    s.precioBrutoTotal + Constants.FIELD_SEPARATOR +
                    s.fechaFactura + Constants.FIELD_SEPARATOR +
                    s.ejercicio + Constants.FIELD_SEPARATOR +
                    s.codigoCliente + Constants.FIELD_SEPARATOR +
                    s.nombreCliente + Constants.FIELD_SEPARATOR +
                    "" + Constants.FIELD_SEPARATOR + //s.razonSocial
                    "" + Constants.FIELD_SEPARATOR + //s.cif
                    "" + Constants.FIELD_SEPARATOR + //s.direccion
                    "" + Constants.FIELD_SEPARATOR + //s.poblacion
                    "" + Constants.FIELD_SEPARATOR + //s.cp
                    "" + Constants.FIELD_SEPARATOR + //s.ruta
                    "" + Constants.FIELD_SEPARATOR + //s.nombreRuta
                    "" + Constants.FIELD_SEPARATOR + //s.codigoComercial
                    "" + Constants.FIELD_SEPARATOR + //s.nombreComercial
                    "" + Constants.FIELD_SEPARATOR + //s.peso
                    "" + Constants.FIELD_SEPARATOR + //s.UMPeso
                    "" + Constants.FIELD_SEPARATOR //s.tipoCliente
                    );
            }
            sw.Close();
        }

        /// <summary>
        /// Devuelve valor según posición y separador informado
        /// </summary>
        /// <param name="s">cadena de entrada</param>        
        /// <param name="pos">posición de la cadena que queremos devolver (1...N)</param>   
        /// <param name="delimiter">separador</param>   
        private string ObtenerValor(string s, int pos, string delimiter)
        {
            string[] valores = s.Split(delimiter[0]);
            if (valores.Length > pos - 1)
                return (valores[pos - 1]).Replace("'","");
            else
                return "";
        }

        /// <summary>
        /// Devuelve valor según posiciones fijas
        /// </summary>
        /// <param name="s">cadena de entrada</param>        
        /// <param name="pos">posición inicial de la cadena que queremos devolver (1...N)</param>   
        /// <param name="longitud">longitud cadena a devolver</param>           
        private string ObtenerValor(string s, int posIni, int longitud)
        {
            string v = "";
            try
            {
                v = s.Substring(posIni, longitud);
            }
            catch
            {
                if (s.Length < (posIni + longitud))
                    v = s.Substring(posIni, (s.Length - posIni));
            }
            return v;
        }
    }

    /// <summary>
    /// Funciones para leer y escribir segmentos EDI
    /// </summary>
    public class EANCOMSegmentsD96AS3
    {
        public string JuegoCaracteres = "";
        public string Document;
        public string DocumentOfSegments;

        private void WriteSegments(string line)
        {
            using (StreamWriter sw = new StreamWriter(DocumentOfSegments))
            {
                sw.WriteLine(line);
            }
        }

        public long ReadSegments()
        {
            StreamReader objReader = new StreamReader(DocumentOfSegments);
            string line = objReader.ReadLine();
            objReader.Close();
            long num;
            return long.TryParse(line, out num) ? num : 0;
        }

        private void DeleteSegments()
        {
            if (File.Exists(DocumentOfSegments))
                File.Delete(DocumentOfSegments);
        }

        private void WriteDocument(string line)
        {
            FileStream fs = new FileStream(Document, FileMode.Append, FileAccess.Write);
            using (StreamWriter sw = new StreamWriter(fs))
            {
                sw.WriteLine(line);
            }
            fs.Close();
        }

        public void CreateDocument(string DocumentFilePath)
        {
            if ((DocumentFilePath == ""))
            {
                DocumentFilePath = (Environment.CurrentDirectory + ("\\Documentos\\Generados\\APIAECOC"
                            + (DateTime.Now.ToString("yyyyMMddHHmmss") + ("R" + ".edi"))));
            }
            Document = DocumentFilePath;
            DocumentOfSegments = Environment.CurrentDirectory + "\\numsegmentos.dat";
            WriteSegments("0");
        }

        public void SegmentADRWrite(
                string DE3299,
                string DE3131,
                string DE3475,
                string DE3477,
                string DE3286,
                string DE3286_1,
                string DE3286_2,
                string DE3286_3,
                string DE3286_4,
                string DE3164,
                string DE3251,
                string DE3207,
                string DE3229,
                string DE1131,
                string DE3055,
                string DE3228,
                string DE3225,
                string DE1131_1,
                string DE3055_1,
                string DE3224)
        {
            string Segmento;
            Segmento = ("ADR+"
                        + (DEComp(TratarTexto(DE3299, 3), TratarTexto(DE3131, 3), TratarTexto(DE3475, 3))
                        + (DEComp(TratarTexto(DE3477, 3), TratarTexto(DE3286, 70), TratarTexto(DE3286_1, 70), TratarTexto(DE3286_2, 70), TratarTexto(DE3286_3, 70), TratarTexto(DE3286_4, 70))
                        + (DESimp(TratarTexto(DE3164, 35))
                        + (DESimp(TratarTexto(DE3251, 9))
                        + (DESimp(TratarTexto(DE3207, 3))
                        + (DEComp(TratarTexto(DE3229, 9), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE3228, 35))
                        + (DEComp(TratarTexto(DE3225, 25), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE3224, 70)) + "\'"))))))));
            EscribeSegmento(Segmento);
        }

        public void SegmentADRRead(
                    string Segment,
                    string Delimiter,
                    string DE3299,
                    string DE3131,
                    string DE3475,
                    string DE3477,
                    string DE3286,
                    string DE3286_1,
                    string DE3286_2,
                    string DE3286_3,
                    string DE3286_4,
                    string DE3164,
                    string DE3251,
                    string DE3207,
                    string DE3229,
                    string DE1131,
                    string DE3055,
                    string DE3228,
                    string DE3225,
                    string DE1131_1,
                    string DE3055_1,
                    string DE3224)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE3299 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE3131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3475 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3477 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3286 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE3286_1 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE3286_2 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE3286_3 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE3286_4 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE3164 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE3251 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE3207 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE3229 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE1131 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE3055 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE3228 = LecturaSegmento[15];
            }
            if ((LecturaSegmento.Length > 16))
            {
                DE3225 = LecturaSegmento[16];
            }
            if ((LecturaSegmento.Length > 17))
            {
                DE1131_1 = LecturaSegmento[17];
            }
            if ((LecturaSegmento.Length > 18))
            {
                DE3055_1 = LecturaSegmento[18];
            }
            if ((LecturaSegmento.Length > 19))
            {
                DE3224 = LecturaSegmento[19];
            }
        }

        public void SegmentAGRWrite(string DE7431, string DE7433, string DE1131, string DE3055, string DE7434, string DE9419)
        {
            string Segmento;
            Segmento = ("AGR+"
                        + (DEComp(TratarTexto(DE7431, 3), TratarTexto(DE7433, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE7434, 70))
                        + (DESimp(TratarTexto(DE9419, 3)) + "\'")));
            EscribeSegmento(Segmento);
        }

        public void SegmentAGRRead(string Segment, string Delimiter, string DE7431, string DE7433, string DE1131, string DE3055, string DE7434, string DE9419)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7431 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7433 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE7434 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE9419 = LecturaSegmento[5];
            }
        }

        public void SegmentAJTWrite(string DE4465, string DE1082)
        {
            string Segmento;
            if ((DE4465 != ""))
            {
                Segmento = ("AJT+"
                            + (DESimp(TratarTexto(DE4465, 3))
                            + (DESimp(DE1082) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentAJTRead(string Segment, string Delimiter, string DE4465, string DE1082)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4465 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1082 = LecturaSegmento[1];
            }
        }

        public void SegmentALCWrite(string DE5463, string DE1230, string DE5189, string DE4471, string DE1227, string DE7161, string DE1131, string DE3055, string DE7160, string DE7160_1)
        {
            string Segmento;
            if ((DE5463 != ""))
            {
                Segmento = ("ALC+"
                            + (DESimp(TratarTexto(DE5463, 3))
                            + (DEComp(TratarTexto(DE1230, 35), TratarTexto(DE5189, 3))
                            + (DESimp(TratarTexto(DE4471, 3))
                            + (DESimp(TratarTexto(DE1227, 3))
                            + (DEComp(TratarTexto(DE7161, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE7160, 35), TratarTexto(DE7160_1, 35)) + "\'"))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentALCRead(string Segment, string Delimiter, string DE5463, string DE1230, string DE5189, string DE4471, string DE1227, string DE7161, string DE1131, string DE3055, string DE7160, string DE7160_1)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE5463 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1230 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE5189 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE4471 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1227 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE7161 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE1131 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE3055 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE7160 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE7160_1 = LecturaSegmento[9];
            }
        }

        public void SegmentALIWrite(string DE3239, string DE9213, string DE4183, string DE4183_1, string DE4183_2, string DE4183_3, string DE4183_4)
        {
            string Segmento;
            Segmento = ("ALI+"
                        + (DESimp(TratarTexto(DE3239, 3))
                        + (DESimp(TratarTexto(DE9213, 3))
                        + (DESimp(TratarTexto(DE4183, 3))
                        + (DESimp(TratarTexto(DE4183_1, 3))
                        + (DESimp(TratarTexto(DE4183_2, 3))
                        + (DESimp(TratarTexto(DE4183_3, 3))
                        + (DESimp(TratarTexto(DE4183_4, 3)) + "\'"))))))));
            EscribeSegmento(Segmento);
        }

        public void SegmentALIRead(string Segment, string Delimiter, string DE3239, string DE9213, string DE4183, string DE4183_1, string DE4183_2, string DE4183_3, string DE4183_4)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE3239 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE9213 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE4183 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE4183_1 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE4183_2 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE4183_3 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4183_4 = LecturaSegmento[6];
            }
        }

        public void SegmentAPRWrite(string DE4043, string DE5394, string DE5393, string DE4295, string DE1131, string DE3055, string DE4294)
        {
            string Segmento;
            Segmento = ("APR+"
                        + (DESimp(TratarTexto(DE4043, 3))
                        + (DEComp(DE5394, TratarTexto(DE5393, 3))
                        + (DEComp(TratarTexto(DE4295, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE4294, 35)) + "\'"))));
            EscribeSegmento(Segmento);
        }

        public void SegmentAPRRead(string Segment, string Delimiter, string DE4043, string DE5394, string DE5393, string DE4295, string DE1131, string DE3055, string DE4294)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4043 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE5394 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE5393 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE4295 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1131 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE3055 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4294 = LecturaSegmento[6];
            }
        }

        public void SegmentARDWrite(string DE5007, string DE1131, string DE3055)
        {
            string Segmento;
            Segmento = ("ARD+"
                        + (DEComp(TratarTexto(DE5007, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3)) + "\'"));
            EscribeSegmento(Segmento);
        }

        public void SegmentARDRead(string Segment, string Delimiter, string DE5007, string DE1131, string DE3055)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE5007 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
        }

        public void SegmentARRWrite(string DE7164, string DE1050, string DE9424)
        {
            string Segmento;
            Segmento = ("ARR+"
                        + (DEComp(TratarTexto(DE7164, 12), TratarTexto(DE1050, 6))
                        + (DEComp(TratarTexto(DE9424, 35)) + "\'")));
            EscribeSegmento(Segmento);
        }

        public void SegmentARRRead(string Segment, string Delimiter, string DE7164, string DE1050, string DE9424)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7164 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1050 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE9424 = LecturaSegmento[2];
            }
        }

        public void SegmentASIWrite(string DE9428, string DE7405, string DE3039, string DE1131, string DE3055, string DE4405, string DE4513)
        {
            string Segmento;
            if ((DE9428 != ""))
            {
                Segmento = ("ASI+"
                            + (DEComp(TratarTexto(DE9428, 35), TratarTexto(DE7405, 3))
                            + (DEComp(TratarTexto(DE3039, 35), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                            + (DESimp(TratarTexto(DE4405, 3))
                            + (DESimp(TratarTexto(DE4513, 3)) + "\'")))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentASIRead(string Segment, string Delimiter, string DE9428, string DE7405, string DE3039, string DE1131, string DE3055, string DE4405, string DE4513)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9428 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7405 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3039 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1131 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3055 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE4405 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4513 = LecturaSegmento[6];
            }
        }

        public void SegmentATTWrite(string DE9017, string DE9021, string DE1131, string DE3055, string DE9019, string DE1131_1, string DE3055_1, string DE9018)
        {
            string Segmento;
            if ((DE9017 != ""))
            {
                Segmento = ("ATT+"
                            + (DESimp(TratarTexto(DE9017, 3))
                            + (DEComp(TratarTexto(DE9021, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                            + (DEComp(TratarTexto(DE9019, 3), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE9018, 35)) + "\'"))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentATTRead(string Segment, string Delimiter, string DE9017, string DE9021, string DE1131, string DE3055, string DE9019, string DE1131_1, string DE3055_1, string DE9018)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9017 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE9021 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE9019 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE1131_1 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE3055_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE9018 = LecturaSegmento[7];
            }
        }

        public void SegmentAUTWrite(string DE9280, string DE9282)
        {
            string Segmento;
            if ((DE9280 != ""))
            {
                Segmento = ("AUT+"
                            + (DESimp(TratarTexto(DE9280, 35))
                            + (DESimp(TratarTexto(DE9282, 35)) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentAUTRead(string Segment, string Delimiter, string DE9280, string DE9282)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9280 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE9282 = LecturaSegmento[1];
            }
        }

        public void SegmentBGMWrite(string DE1001, string DE1131, string DE3055, string DE1000, string DE1004, string DE1225, string DE4343)
        {
            string Segmento;
            Segmento = ("BGM+"
                        + (DEComp(TratarTexto(DE1001, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE1000, 35))
                        + (DESimp(TratarTexto(DE1004, 35))
                        + (DESimp(TratarTexto(DE1225, 3))
                        + (DESimp(TratarTexto(DE4343, 3)) + "\'")))));
            EscribeSegmento(Segmento);
        }

        public void SegmentBGMRead(string Segment, string Delimiter, ref string DE1001, ref string DE1131, ref string DE3055, ref string DE1000, ref string DE1004, ref string DE1225, ref string DE4343)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE1001 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1000 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1004 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE1225 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4343 = LecturaSegmento[6];
            }
        }

        public void SegmentBIIWrite(string DE7429, string DE7436, string DE7438, string DE7440, string DE7442, string DE7444, string DE7446, string DE7140)
        {
            string Segmento;
            if ((DE7429 != ""))
            {
                Segmento = ("BII+"
                            + (DESimp(TratarTexto(DE7429, 3))
                            + (DEComp(TratarTexto(DE7436, 17), TratarTexto(DE7438, 17), TratarTexto(DE7440, 17), TratarTexto(DE7442, 17), TratarTexto(DE7444, 17), TratarTexto(DE7446, 17))
                            + (DESimp(TratarTexto(DE7140, 35)) + "\'"))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentBIIRead(string Segment, string Delimiter, string DE7429, string DE7436, string DE7438, string DE7440, string DE7442, string DE7444, string DE7446, string DE7140)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7429 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7436 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE7438 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE7440 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE7442 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE7444 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE7446 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE7140 = LecturaSegmento[7];
            }
        }

        public void SegmentBUSWrite(string DE4027, string DE4025, string DE1131, string DE3055, string DE4022, string DE3279, string DE4487, string DE4383, string DE1131_1, string DE3055_1, string DE4463)
        {
            string Segmento;
            Segmento = ("BUS+"
                        + (DEComp(TratarTexto(DE4027, 3), TratarTexto(DE4025, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE4022, 70))
                        + (DESimp(TratarTexto(DE3279, 3))
                        + (DESimp(TratarTexto(DE4487, 3))
                        + (DEComp(TratarTexto(DE4383, 3), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3))
                        + (DESimp(TratarTexto(DE4463, 3)) + "\'"))))));
            EscribeSegmento(Segmento);
        }

        public void SegmentBUSRead(string Segment, string Delimiter, string DE4027, string DE4025, string DE1131, string DE3055, string DE4022, string DE3279, string DE4487, string DE4383, string DE1131_1, string DE3055_1, string DE4463)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4027 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE4025 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE4022 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE3279 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4487 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE4383 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE1131_1 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE3055_1 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE4463 = LecturaSegmento[10];
            }
        }

        public void SegmentCAVWrite(string DE7111, string DE1131, string DE3055, string DE7110, string DE7110_1)
        {
            string Segmento;
            Segmento = ("CAV+"
                        + (DEComp(TratarTexto(DE7111, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE7110, 35), TratarTexto(DE7110_1, 35)) + "\'"));
            EscribeSegmento(Segmento);
        }

        public void SegmentCAVRead(string Segment, string Delimiter, string DE7111, string DE1131, string DE3055, string DE7110, string DE7110_1)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7111 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE7110 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE7110_1 = LecturaSegmento[4];
            }
        }

        public void SegmentCCDWrite(string DE4505, string DE4507, string DE4509)
        {
            string Segmento;
            Segmento = ("CCD+"
                        + (DESimp(TratarTexto(DE4505, 3))
                        + (DESimp(TratarTexto(DE4507, 3))
                        + (DESimp(TratarTexto(DE4509, 3)) + "\'"))));
            EscribeSegmento(Segmento);
        }

        public void SegmentCCDRead(string Segment, string Delimiter, string DE4505, string DE4507, string DE4509)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4505 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE4507 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE4509 = LecturaSegmento[2];
            }
        }

        public void SegmentCCIWrite(string DE7059, string DE6313, string DE6321, string DE6155, string DE6154, string DE7037, string DE1131, string DE3055, string DE7036, string DE7036_1)
        {
            string Segmento;
            Segmento = ("CCI+"
                        + (DESimp(TratarTexto(DE7059, 3))
                        + (DEComp(TratarTexto(DE6313, 3), TratarTexto(DE6321, 3), TratarTexto(DE6155, 3), TratarTexto(DE6154, 70))
                        + (DEComp(TratarTexto(DE7037, 17), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE7036, 35), TratarTexto(DE7036_1, 35)) + "\'"))));
            EscribeSegmento(Segmento);
        }

        public void SegmentCCIRead(string Segment, string Delimiter, string DE7059, string DE6313, string DE6321, string DE6155, string DE6154, string DE7037, string DE1131, string DE3055, string DE7036, string DE7036_1)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7059 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE6313 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE6321 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE6155 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE6154 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE7037 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE1131 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE3055 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE7036 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE7036_1 = LecturaSegmento[9];
            }
        }

        public void SegmentCDIWrite(string DE7001, string DE7007, string DE1131, string DE3055, string DE7006)
        {
            string Segmento;
            if ((DE7001 != ""))
            {
                Segmento = ("CDI+"
                            + (DESimp(TratarTexto(DE7001, 3))
                            + (DEComp(TratarTexto(DE7007, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE7006, 35)) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentCDIRead(string Segment, string Delimiter, string DE7001, string DE7007, string DE1131, string DE3055, string DE7006)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7001 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7007 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE7006 = LecturaSegmento[4];
            }
        }

        public void SegmentCDSWrite(string DE9150, string DE1131, string DE3055, string DE1507, string DE4513)
        {
            string Segmento;
            Segmento = ("CDS+"
                        + (DEComp(TratarTexto(DE9150, 4), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                        + (DESimp(TratarTexto(DE1507, 3))
                        + (DESimp(TratarTexto(DE4513, 3)) + "\'"))));
            EscribeSegmento(Segmento);
        }

        public void SegmentCDSRead(string Segment, string Delimiter, string DE9150, string DE1131, string DE3055, string DE1507, string DE4513)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9150 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1507 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE4513 = LecturaSegmento[4];
            }
        }

        public void SegmentCDVWrite(string DE9426, string DE9434, string DE4513)
        {
            string Segmento;
            if ((DE9426 != ""))
            {
                Segmento = ("CDV+"
                            + (DESimp(TratarTexto(DE9426, 35))
                            + (DESimp(TratarTexto(DE9434, 70))
                            + (DESimp(TratarTexto(DE4513, 3)) + "\'"))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentCDVRead(string Segment, string Delimiter, string DE9426, string DE9434, string DE4513)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9426 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE9434 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE4513 = LecturaSegmento[2];
            }
        }

        public void SegmentCEDWrite(string DE1501, string DE1511, string DE1131, string DE3055, string DE1510, string DE1056, string DE1058, string DE7402)
        {
            string Segmento;
            if ((DE1501 != ""))
            {
                Segmento = ("CED+"
                            + (DESimp(TratarTexto(DE1501, 3))
                            + (DEComp(TratarTexto(DE1511, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE1510, 35), TratarTexto(DE1056, 9), TratarTexto(DE1058, 9), TratarTexto(DE7402, 35)) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentCEDRead(string Segment, string Delimiter, string DE1501, string DE1511, string DE1131, string DE3055, string DE1510, string DE1056, string DE1058, string DE7402)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE1501 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1511 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1510 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE1056 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE1058 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE7402 = LecturaSegmento[7];
            }
        }

        public void SegmentCMPWrite(string DE9146, string DE1507, string DE4513)
        {
            string Segmento;
            if ((DE9146 != ""))
            {
                Segmento = ("CMP+"
                            + (DESimp(TratarTexto(DE9146, 4))
                            + (DESimp(TratarTexto(DE1507, 3))
                            + (DESimp(TratarTexto(DE4513, 3)) + "\'"))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentCMPRead(string Segment, string Delimiter, string DE9146, string DE1507, string DE4513)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9146 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1507 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE4513 = LecturaSegmento[2];
            }
        }

        public void SegmentCNIWrite(string DE1490, string DE1004, string DE1373, string DE1366, string DE3453, string DE1312)
        {
            string Segmento;
            Segmento = ("CNI+"
                        + (DESimp(DE1490)
                        + (DEComp(TratarTexto(DE1004, 35), TratarTexto(DE1373, 3), TratarTexto(DE1366, 35), TratarTexto(DE3453, 3))
                        + (DESimp(DE1312) + "\'"))));
            EscribeSegmento(Segmento);
        }

        public void SegmentCNIRead(string Segment, string Delimiter, string DE1490, string DE1004, string DE1373, string DE1366, string DE3453, string DE1312)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE1490 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1004 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1373 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1366 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3453 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE1312 = LecturaSegmento[5];
            }
        }

        public void SegmentCNTWrite(string DE6069, string DE6066, string DE6411)
        {
            string Segmento;
            if (((DE6069 != "")
                        && (DE6066 != "")))
            {
                Segmento = ("CNT+"
                            + (DEComp(TratarTexto(DE6069, 3), DE6066, TratarTexto(DE6411, 3)) + "\'"));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentCNTRead(string Segment, string Delimiter, string DE6069, string DE6066, string DE6411)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE6069 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE6066 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE6411 = LecturaSegmento[2];
            }
        }

        public void SegmentCODWrite(string DE7505, string DE1131, string DE3055, string DE7504, string DE7507, string DE1131_1, string DE3055_1, string DE7506)
        {
            string Segmento;
            Segmento = ("COD+"
                        + (DEComp(TratarTexto(DE7505, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE7504, 35))
                        + (DEComp(TratarTexto(DE7507, 3), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE7506, 35)) + "\'")));
            EscribeSegmento(Segmento);
        }

        public void SegmentCODRead(string Segment, string Delimiter, string DE7505, string DE1131, string DE3055, string DE7504, string DE7507, string DE1131_1, string DE3055_1, string DE7506)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7505 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE7504 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE7507 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE1131_1 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE3055_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE7506 = LecturaSegmento[7];
            }
        }

        public void SegmentCOMWrite(string DE3148, string DE3155)
        {
            string Segmento;
            if (((DE3148 != "")
                        && (DE3155 != "")))
            {
                Segmento = ("COM+"
                            + (DEComp(TratarTexto(DE3148, 512), TratarTexto(DE3155, 3)) + "\'"));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentCOMRead(string Segment, string Delimiter, string DE3148, string DE3155)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE3148 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE3155 = LecturaSegmento[1];
            }
        }

        public void SegmentCOTWrite(
                    string DE5047,
                    string DE5049,
                    string DE1131,
                    string DE3055,
                    string DE5048,
                    string DE4403,
                    string DE4401,
                    string DE1131_1,
                    string DE3055_1,
                    string DE4400,
                    string DE5243,
                    string DE1131_2,
                    string DE3055_2,
                    string DE5242,
                    string DE5275,
                    string DE1131_3,
                    string DE3055_3,
                    string DE5275_1,
                    string DE1131_4,
                    string DE3055_4,
                    string DE4295,
                    string DE1131_5,
                    string DE3055_5,
                    string DE4294)
        {
            string Segmento;
            if ((DE5047 != ""))
            {
                Segmento = ("COT+"
                            + (DESimp(TratarTexto(DE5047, 3))
                            + (DEComp(TratarTexto(DE5049, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE5048, 35))
                            + (DEComp(TratarTexto(DE4403, 3), TratarTexto(DE4401, 3), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE4400, 35))
                            + (DEComp(TratarTexto(DE5243, 9), TratarTexto(DE1131_2, 3), TratarTexto(DE3055_2, 3), TratarTexto(DE5242, 35), TratarTexto(DE5275, 6), TratarTexto(DE1131_3, 3), TratarTexto(DE3055_3, 3), TratarTexto(DE5275_1, 6), TratarTexto(DE1131_4, 3), TratarTexto(DE3055_4, 3))
                            + (DEComp(TratarTexto(DE4295, 3), TratarTexto(DE1131_5, 3), TratarTexto(DE3055_5, 3), TratarTexto(DE4294, 35)) + "\'"))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentCOTRead(
                    string Segment,
                    string Delimiter,
                    string DE5047,
                    string DE5049,
                    string DE1131,
                    string DE3055,
                    string DE5048,
                    string DE4403,
                    string DE4401,
                    string DE1131_1,
                    string DE3055_1,
                    string DE4400,
                    string DE5243,
                    string DE1131_2,
                    string DE3055_2,
                    string DE5242,
                    string DE5275,
                    string DE1131_3,
                    string DE3055_3,
                    string DE5275_1,
                    string DE1131_4,
                    string DE3055_4,
                    string DE4295,
                    string DE1131_5,
                    string DE3055_5,
                    string DE4294)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE5047 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE5049 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE5048 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE4403 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4401 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE1131_1 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE3055_1 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE4400 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE5243 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE1131_2 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE3055_2 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE5242 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE5275 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE1131_3 = LecturaSegmento[15];
            }
            if ((LecturaSegmento.Length > 16))
            {
                DE3055_3 = LecturaSegmento[16];
            }
            if ((LecturaSegmento.Length > 17))
            {
                DE5275_1 = LecturaSegmento[17];
            }
            if ((LecturaSegmento.Length > 18))
            {
                DE1131_4 = LecturaSegmento[18];
            }
            if ((LecturaSegmento.Length > 19))
            {
                DE3055_4 = LecturaSegmento[19];
            }
            if ((LecturaSegmento.Length > 20))
            {
                DE4295 = LecturaSegmento[20];
            }
            if ((LecturaSegmento.Length > 21))
            {
                DE1131_5 = LecturaSegmento[21];
            }
            if ((LecturaSegmento.Length > 22))
            {
                DE3055_5 = LecturaSegmento[22];
            }
            if ((LecturaSegmento.Length > 23))
            {
                DE4294 = LecturaSegmento[23];
            }
        }

        public void SegmentCPIWrite(string DE5237, string DE1131, string DE3055, string DE4215, string DE1131_1, string DE3055_1, string DE4237)
        {
            string Segmento;
            Segmento = ("CPI+"
                        + (DEComp(TratarTexto(DE5237, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                        + (DEComp(TratarTexto(DE4215, 3), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3))
                        + (DESimp(TratarTexto(DE4237, 3)) + "\'"))));
            EscribeSegmento(Segmento);
        }

        public void SegmentCPIRead(string Segment, string Delimiter, string DE5237, string DE1131, string DE3055, string DE4215, string DE1131_1, string DE3055_1, string DE4237)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE5237 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE4215 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1131_1 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE3055_1 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4237 = LecturaSegmento[6];
            }
        }

        public void SegmentCPSWrite(string DE7164, string DE7166, string DE7075)
        {
            string Segmento;
            if ((DE7164 != ""))
            {
                Segmento = ("CPS+"
                            + (DESimp(TratarTexto(DE7164, 12))
                            + (DESimp(TratarTexto(DE7166, 12))
                            + (DESimp(TratarTexto(DE7075, 3)) + "\'"))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentCPSRead(string Segment, string Delimiter, string DE7164, string DE7166, string DE7075)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7164 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7166 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE7075 = LecturaSegmento[2];
            }
        }

        public void SegmentCSTWrite(
                    string DE1496,
                    string DE7361,
                    string DE1131,
                    string DE3055,
                    string DE7361_1,
                    string DE1131_1,
                    string DE3055_1,
                    string DE7361_2,
                    string DE1131_2,
                    string DE3055_2,
                    string DE7361_3,
                    string DE1131_3,
                    string DE3055_3,
                    string DE7361_4,
                    string DE1131_4,
                    string DE3055_4)
        {
            string Segmento;
            Segmento = ("CST+"
                        + (DESimp(DE1496)
                        + (DEComp(TratarTexto(DE7361, 18), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                        + (DEComp(TratarTexto(DE7361_1, 18), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3))
                        + (DEComp(TratarTexto(DE7361_2, 18), TratarTexto(DE1131_2, 3), TratarTexto(DE3055_2, 3))
                        + (DEComp(TratarTexto(DE7361_3, 18), TratarTexto(DE1131_3, 3), TratarTexto(DE3055_3, 3))
                        + (DEComp(TratarTexto(DE7361_4, 18), TratarTexto(DE1131_4, 3), TratarTexto(DE3055_4, 3)) + "\'")))))));
            EscribeSegmento(Segmento);
        }

        public void SegmentCSTRead(
                    string Segment,
                    string Delimiter,
                    string DE1496,
                    string DE7361,
                    string DE1131,
                    string DE3055,
                    string DE7361_1,
                    string DE1131_1,
                    string DE3055_1,
                    string DE7361_2,
                    string DE1131_2,
                    string DE3055_2,
                    string DE7361_3,
                    string DE1131_3,
                    string DE3055_3,
                    string DE7361_4,
                    string DE1131_4,
                    string DE3055_4)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE1496 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7361 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE7361_1 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE1131_1 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE3055_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE7361_2 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE1131_2 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE3055_2 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE7361_3 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE1131_3 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE3055_3 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE7361_4 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE1131_4 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE3055_4 = LecturaSegmento[15];
            }
        }

        public void SegmentCTAWrite(string DE3139, string DE3413, string DE3412)
        {
            string Segmento;
            Segmento = ("CTA+"
                        + (DESimp(TratarTexto(DE3139, 3))
                        + (DEComp(TratarTexto(DE3413, 17), TratarTexto(DE3412, 35)) + "\'")));
            EscribeSegmento(Segmento);
        }

        public void SegmentCTARead(string Segment, string Delimiter, string DE3139, string DE3413, string DE3412)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE3139 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE3413 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3412 = LecturaSegmento[2];
            }
        }

        public void SegmentCUXWrite(string DE6347, string DE6345, string DE6343, string DE6348, string DE6347_1, string DE6345_1, string DE6343_1, string DE6348_1, string DE5402, string DE6341)
        {
            string Segmento;
            Segmento = ("CUX+"
                        + (DEComp(TratarTexto(DE6347, 3), TratarTexto(DE6345, 3), TratarTexto(DE6343, 3), DE6348)
                        + (DEComp(TratarTexto(DE6347_1, 3), TratarTexto(DE6345_1, 3), TratarTexto(DE6343_1, 3), DE6348_1)
                        + (DESimp(DE5402)
                        + (DESimp(TratarTexto(DE6341, 3)) + "\'")))));
            EscribeSegmento(Segmento);
        }

        public void SegmentCUXRead(string Segment, string Delimiter, string DE6347, string DE6345, string DE6343, string DE6348, string DE6347_1, string DE6345_1, string DE6343_1, string DE6348_1, string DE5402, string DE6341)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE6347 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE6345 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE6343 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE6348 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE6347_1 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE6345_1 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE6343_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE6348_1 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE5402 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE6341 = LecturaSegmento[9];
            }
        }

        public void SegmentDAMWrite(
                    string DE7493,
                    string DE7501,
                    string DE1131,
                    string DE3055,
                    string DE7500,
                    string DE7503,
                    string DE1131_1,
                    string DE3055_1,
                    string DE7502,
                    string DE7509,
                    string DE1131_2,
                    string DE3055_2,
                    string DE7508,
                    string DE1229,
                    string DE1131_3,
                    string DE3055_3,
                    string DE1228)
        {
            string Segmento;
            if ((DE7493 != ""))
            {
                Segmento = ("DAM+"
                            + (DESimp(TratarTexto(DE7493, 3))
                            + (DEComp(TratarTexto(DE7501, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE7500, 35))
                            + (DEComp(TratarTexto(DE7503, 4), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE7502, 35))
                            + (DEComp(TratarTexto(DE7509, 3), TratarTexto(DE1131_2, 3), TratarTexto(DE3055_2, 3), TratarTexto(DE7508, 35))
                            + (DEComp(TratarTexto(DE1229, 3), TratarTexto(DE1131_3, 3), TratarTexto(DE3055_3, 3), TratarTexto(DE1228, 35)) + "\'"))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentDAMRead(
                    string Segment,
                    string Delimiter,
                    string DE7493,
                    string DE7501,
                    string DE1131,
                    string DE3055,
                    string DE7500,
                    string DE7503,
                    string DE1131_1,
                    string DE3055_1,
                    string DE7502,
                    string DE7509,
                    string DE1131_2,
                    string DE3055_2,
                    string DE7508,
                    string DE1229,
                    string DE1131_3,
                    string DE3055_3,
                    string DE1228)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7493 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7501 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE7500 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE7503 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE1131_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE3055_1 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE7502 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE7509 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE1131_2 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE3055_2 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE7508 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE1229 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE1131_3 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE3055_3 = LecturaSegmento[15];
            }
            if ((LecturaSegmento.Length > 16))
            {
                DE1228 = LecturaSegmento[16];
            }
        }

        public void SegmentDGSWrite(
                    string DE8273,
                    string DE8351,
                    string DE8078,
                    string DE8092,
                    string DE7124,
                    string DE7088,
                    string DE7106,
                    string DE6411,
                    string DE8339,
                    string DE8364,
                    string DE8410,
                    string DE8126,
                    string DE8158,
                    string DE8186,
                    string DE8246,
                    string DE8246_1,
                    string DE8246_2,
                    string DE8255,
                    string DE8325,
                    string DE8211)
        {
            string Segmento;
            Segmento = ("DGS+"
                        + (DESimp(TratarTexto(DE8273, 3))
                        + (DEComp(TratarTexto(DE8351, 7), TratarTexto(DE8078, 7), TratarTexto(DE8092, 10))
                        + (DEComp(DE7124, TratarTexto(DE7088, 8))
                        + (DEComp(DE7106, TratarTexto(DE6411, 3))
                        + (DESimp(TratarTexto(DE8339, 3))
                        + (DESimp(TratarTexto(DE8364, 6))
                        + (DESimp(TratarTexto(DE8410, 4))
                        + (DESimp(TratarTexto(DE8126, 10))
                        + (DEComp(TratarTexto(DE8158, 4), TratarTexto(DE8186, 4))
                        + (DEComp(TratarTexto(DE8246, 4), TratarTexto(DE8246_1, 4), TratarTexto(DE8246_2, 4))
                        + (DESimp(TratarTexto(DE8255, 3))
                        + (DESimp(TratarTexto(DE8325, 3))
                        + (DESimp(TratarTexto(DE8211, 3)) + "\'"))))))))))))));
            EscribeSegmento(Segmento);
        }

        public void SegmentDGSRead(
                    string Segment,
                    string Delimiter,
                    string DE8273,
                    string DE8351,
                    string DE8078,
                    string DE8092,
                    string DE7124,
                    string DE7088,
                    string DE7106,
                    string DE6411,
                    string DE8339,
                    string DE8364,
                    string DE8410,
                    string DE8126,
                    string DE8158,
                    string DE8186,
                    string DE8246,
                    string DE8246_1,
                    string DE8246_2,
                    string DE8255,
                    string DE8325,
                    string DE8211)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE8273 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE8351 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE8078 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE8092 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE7124 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE7088 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE7106 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE6411 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE8339 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE8364 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE8410 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE8126 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE8158 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE8186 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE8246 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE8246_1 = LecturaSegmento[15];
            }
            if ((LecturaSegmento.Length > 16))
            {
                DE8246_2 = LecturaSegmento[16];
            }
            if ((LecturaSegmento.Length > 17))
            {
                DE8255 = LecturaSegmento[17];
            }
            if ((LecturaSegmento.Length > 18))
            {
                DE8325 = LecturaSegmento[18];
            }
            if ((LecturaSegmento.Length > 19))
            {
                DE8211 = LecturaSegmento[19];
            }
        }

        public void SegmentDIIWrite(string DE1056, string DE1058, string DE9148, string DE1476, string DE3453, string DE4513)
        {
            string Segmento;
            if (((DE1056 != "")
                        && (DE1058 != "")))
            {
                Segmento = ("DII+"
                            + (DESimp(TratarTexto(DE1056, 9))
                            + (DESimp(TratarTexto(DE1058, 9))
                            + (DESimp(TratarTexto(DE9148, 3))
                            + (DESimp(TratarTexto(DE1476, 2))
                            + (DESimp(TratarTexto(DE3453, 3))
                            + (DESimp(TratarTexto(DE4513, 3)) + "\'")))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentDIIRead(string Segment, string Delimiter, string DE1056, string DE1058, string DE9148, string DE1476, string DE3453, string DE4513)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE1056 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1058 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE9148 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1476 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3453 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE4513 = LecturaSegmento[5];
            }
        }

        public void SegmentDIMWrite(string DE6145, string DE6411, string DE6168, string DE6140, string DE6008)
        {
            string Segmento;
            if (((DE6145 != "")
                        && (DE6411 != "")))
            {
                Segmento = ("DIM+"
                            + (DESimp(TratarTexto(DE6145, 3))
                            + (DEComp(TratarTexto(DE6411, 3), DE6168, DE6140, DE6008) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentDIMRead(string Segment, string Delimiter, string DE6145, string DE6411, string DE6168, string DE6140, string DE6008)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE6145 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE6411 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE6168 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE6140 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE6008 = LecturaSegmento[4];
            }
        }

        public void SegmentDLIWrite(string DE1073, string DE1082)
        {
            string Segmento;
            if (((DE1073 != "")
                        && (DE1082 != "")))
            {
                Segmento = ("DLI+"
                            + (DESimp(TratarTexto(DE1073, 3))
                            + (DESimp(DE1082) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentDLIRead(string Segment, string Delimiter, string DE1073, string DE1082)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE1073 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1082 = LecturaSegmento[1];
            }
        }

        public void SegmentDLMWrite(string DE4455, string DE4403, string DE4401, string DE1131, string DE3055, string DE4400, string DE7161, string DE1131_1, string DE3055_1, string DE7160, string DE7160_1, string DE4457)
        {
            string Segmento;
            Segmento = ("DLM+"
                        + (DESimp(TratarTexto(DE4455, 3))
                        + (DEComp(TratarTexto(DE4403, 3), TratarTexto(DE4401, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE4400, 35))
                        + (DEComp(TratarTexto(DE7161, 3), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE7160, 35), TratarTexto(DE7160_1, 35))
                        + (DESimp(TratarTexto(DE4457, 3)) + "\'")))));
            EscribeSegmento(Segmento);
        }

        public void SegmentDLMRead(string Segment, string Delimiter, string DE4455, string DE4403, string DE4401, string DE1131, string DE3055, string DE4400, string DE7161, string DE1131_1, string DE3055_1, string DE7160, string DE7160_1, string DE4457)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4455 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE4403 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE4401 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1131 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3055 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE4400 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE7161 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE1131_1 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE3055_1 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE7160 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE7160_1 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE4457 = LecturaSegmento[11];
            }
        }

        public void SegmentDMSWrite(string DE1004, string DE1001, string DE7240)
        {
            string Segmento;
            Segmento = ("DMS+"
                        + (DESimp(TratarTexto(DE1004, 35))
                        + (DESimp(TratarTexto(DE1001, 3))
                        + (DESimp(DE7240) + "\'"))));
            EscribeSegmento(Segmento);
        }

        public void SegmentDMSRead(string Segment, string Delimiter, string DE1004, string DE1001, string DE7240)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE1004 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1001 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE7240 = LecturaSegmento[2];
            }
        }

        public void SegmentDOCWrite(string DE1001, string DE1131, string DE3055, string DE1000, string DE1004, string DE1373, string DE1366, string DE3453, string DE3153, string DE1220, string DE1218)
        {
            string Segmento;
            Segmento = ("DOC+"
                        + (DEComp(TratarTexto(DE1001, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE1000, 35))
                        + (DEComp(TratarTexto(DE1004, 35), TratarTexto(DE1373, 3), TratarTexto(DE1366, 35), TratarTexto(DE3453, 3))
                        + (DESimp(TratarTexto(DE3153, 3))
                        + (DESimp(DE1220)
                        + (DESimp(DE1218) + "\'"))))));
            EscribeSegmento(Segmento);
        }

        public void SegmentDOCRead(string Segment, string Delimiter, string DE1001, string DE1131, string DE3055, string DE1000, string DE1004, string DE1373, string DE1366, string DE3453, string DE3153, string DE1220, string DE1218)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE1001 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1000 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1004 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE1373 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE1366 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE3453 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE3153 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE1220 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE1218 = LecturaSegmento[10];
            }
        }

        public void SegmentDSIWrite(string DE1520, string DE7405, string DE3039, string DE1131, string DE3055, string DE4405, string DE1050, string DE1159, string DE1131_1, string DE3055_1, string DE1060)
        {
            string Segmento;
            if ((DE1520 != ""))
            {
                Segmento = ("DSI+"
                            + (DEComp(TratarTexto(DE1520, 35), TratarTexto(DE7405, 3))
                            + (DEComp(TratarTexto(DE3039, 35), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                            + (DESimp(TratarTexto(DE4405, 3))
                            + (DEComp(TratarTexto(DE1050, 6), TratarTexto(DE1159, 3), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3))
                            + (DESimp(TratarTexto(DE1060, 6)) + "\'"))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentDSIRead(string Segment, string Delimiter, string DE1520, string DE7405, string DE3039, string DE1131, string DE3055, string DE4405, string DE1050, string DE1159, string DE1131_1, string DE3055_1, string DE1060)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE1520 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7405 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3039 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1131 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3055 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE4405 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE1050 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE1159 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE1131_1 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE3055_1 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE1060 = LecturaSegmento[10];
            }
        }

        public void SegmentDTMWrite(string DE2005, string DE2380, string DE2379)
        {
            string Segmento;
            if ((DE2005 != ""))
            {
                Segmento = ("DTM+"
                            + (DEComp(TratarTexto(DE2005, 3), TratarTexto(DE2380, 35), TratarTexto(DE2379, 3)) + "\'"));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentDTMRead(string Segment, string Delimiter, ref string DE2005, ref string DE2380, ref string DE2379)
        {
            string[] LecturaSegmento;
            Segment = Segment.Substring(Segment.IndexOf("DTM"), Segment.Length - Segment.IndexOf("DTM"));
            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE2005 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE2380 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE2379 = LecturaSegmento[2];
            }
        }

        public void SegmentEFIWrite(string DE1508, string DE7008, string DE1516, string DE1056, string DE1503, string DE1502, string DE1050)
        {
            string Segmento;
            Segmento = ("EFI+"
                        + (DEComp(TratarTexto(DE1508, 35), TratarTexto(DE7008, 35))
                        + (DEComp(TratarTexto(DE1516, 17), TratarTexto(DE1056, 9), TratarTexto(DE1503, 3), TratarTexto(DE1502, 35))
                        + (DESimp(TratarTexto(DE1050, 6)) + "\'"))));
            EscribeSegmento(Segmento);
        }

        public void SegmentEFIRead(string Segment, string Delimiter, string DE1508, string DE7008, string DE1516, string DE1056, string DE1503, string DE1502, string DE1050)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE1508 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7008 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1516 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1056 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1503 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE1502 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE1050 = LecturaSegmento[6];
            }
        }

        public void SegmentELMWrite(string DE9150, string DE9153, string DE9155, string DE9156, string DE9158, string DE9161, string DE1507, string DE4513)
        {
            string Segmento;
            if ((DE9150 != ""))
            {
                Segmento = ("ELM+"
                            + (DESimp(TratarTexto(DE9150, 4))
                            + (DESimp(TratarTexto(DE9153, 3))
                            + (DESimp(TratarTexto(DE9155, 3))
                            + (DESimp(DE9156)
                            + (DESimp(DE9158)
                            + (DESimp(TratarTexto(DE9161, 3))
                            + (DESimp(TratarTexto(DE1507, 3))
                            + (DESimp(TratarTexto(DE4513, 3)) + "\'")))))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentELMRead(string Segment, string Delimiter, string DE9150, string DE9153, string DE9155, string DE9156, string DE9158, string DE9161, string DE1507, string DE4513)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9150 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE9153 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE9155 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE9156 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE9158 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE9161 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE1507 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE4513 = LecturaSegmento[7];
            }
        }

        public void SegmentELUWrite(string DE9162, string DE7299, string DE1050, string DE4513)
        {
            string Segmento;
            if ((DE9162 != ""))
            {
                Segmento = ("ELU+"
                            + (DESimp(TratarTexto(DE9162, 4))
                            + (DESimp(TratarTexto(DE7299, 3))
                            + (DESimp(TratarTexto(DE1050, 6))
                            + (DESimp(TratarTexto(DE4513, 3)) + "\'")))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentELURead(string Segment, string Delimiter, string DE9162, string DE7299, string DE1050, string DE4513)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9162 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7299 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1050 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE4513 = LecturaSegmento[3];
            }
        }

        public void SegmentEMPWrite(
                    string DE9003,
                    string DE9005,
                    string DE1131,
                    string DE3055,
                    string DE9004,
                    string DE9009,
                    string DE1131_1,
                    string DE3055_1,
                    string DE9008,
                    string DE9008_1,
                    string DE9007,
                    string DE1131_2,
                    string DE3055_2,
                    string DE9006,
                    string DE9006_1,
                    string DE3494,
                    string DE9035)
        {
            string Segmento;
            if ((DE9003 != ""))
            {
                Segmento = ("EMP+"
                            + (DESimp(TratarTexto(DE9003, 3))
                            + (DEComp(TratarTexto(DE9005, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE9004, 35))
                            + (DEComp(TratarTexto(DE9009, 3), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE9008, 35), TratarTexto(DE9008_1, 35))
                            + (DEComp(TratarTexto(DE9007, 3), TratarTexto(DE1131_2, 3), TratarTexto(DE3055_2, 3), TratarTexto(DE9006, 35), TratarTexto(DE9006_1, 35))
                            + (DESimp(TratarTexto(DE3494, 35))
                            + (DESimp(TratarTexto(DE9035, 3)) + "\'")))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentEMPRead(
                    string Segment,
                    string Delimiter,
                    string DE9003,
                    string DE9005,
                    string DE1131,
                    string DE3055,
                    string DE9004,
                    string DE9009,
                    string DE1131_1,
                    string DE3055_1,
                    string DE9008,
                    string DE9008_1,
                    string DE9007,
                    string DE1131_2,
                    string DE3055_2,
                    string DE9006,
                    string DE9006_1,
                    string DE3494,
                    string DE9035)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9003 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE9005 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE9004 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE9009 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE1131_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE3055_1 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE9008 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE9008_1 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE9007 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE1131_2 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE3055_2 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE9006 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE9006_1 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE3494 = LecturaSegmento[15];
            }
            if ((LecturaSegmento.Length > 16))
            {
                DE9035 = LecturaSegmento[16];
            }
        }

        public void SegmentEQAWrite(string DE8053, string DE8260, string DE1131, string DE3055, string DE3207)
        {
            string Segmento;
            if ((DE8053 != ""))
            {
                Segmento = ("EQA+"
                            + (DESimp(TratarTexto(DE8053, 3))
                            + (DEComp(TratarTexto(DE8260, 17), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE3207, 3)) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentEQARead(string Segment, string Delimiter, string DE8053, string DE8260, string DE1131, string DE3055, string DE3207)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE8053 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE8260 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3207 = LecturaSegmento[4];
            }
        }

        public void SegmentEQDWrite(string DE8053, string DE8260, string DE1131, string DE3055, string DE3207, string DE8155, string DE1131_1, string DE3055_1, string DE8154, string DE8077, string DE8249, string DE8169)
        {
            string Segmento;
            if ((DE8053 != ""))
            {
                Segmento = ("EQD+"
                            + (DESimp(TratarTexto(DE8053, 3))
                            + (DEComp(TratarTexto(DE8260, 17), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE3207, 3))
                            + (DEComp(TratarTexto(DE8155, 10), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE8154, 35))
                            + (DESimp(TratarTexto(DE8077, 3))
                            + (DESimp(TratarTexto(DE8249, 3))
                            + (DESimp(TratarTexto(DE8169, 3)) + "\'")))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentEQDRead(string Segment, string Delimiter, string DE8053, string DE8260, string DE1131, string DE3055, string DE3207, string DE8155, string DE1131_1, string DE3055_1, string DE8154, string DE8077, string DE8249, string DE8169)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE8053 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE8260 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3207 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE8155 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE1131_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE3055_1 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE8154 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE8077 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE8249 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE8169 = LecturaSegmento[11];
            }
        }

        public void SegmentEQNWrite(string DE6350, string DE6353)
        {
            string Segmento;
            Segmento = ("EQN+"
                        + (DEComp(DE6350, TratarTexto(DE6353, 3)) + "\'"));
            EscribeSegmento(Segmento);
        }

        public void SegmentEQNRead(string Segment, string Delimiter, string DE6350, string DE6353)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE6350 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE6353 = LecturaSegmento[1];
            }
        }

        public void SegmentERCWrite(string DE9321, string DE1131, string DE3055)
        {
            string Segmento;
            if ((DE9321 != ""))
            {
                Segmento = ("ERC+"
                            + (DEComp(TratarTexto(DE9321, 8), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3)) + "\'"));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentERCRead(string Segment, string Delimiter, string DE9321, string DE1131, string DE3055)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9321 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
        }

        public void SegmentERPWrite(string DE1049, string DE1052, string DE1054)
        {
            string Segmento;
            if ((DE1049 != ""))
            {
                Segmento = ("ERP+"
                            + (DEComp(TratarTexto(DE1049, 3), TratarTexto(DE1052, 35), DE1054) + "\'"));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentERPRead(string Segment, string Delimiter, string DE1049, string DE1052, string DE1054)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE1049 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1052 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1054 = LecturaSegmento[2];
            }
        }

        public void SegmentFCAWrite(string DE4471, string DE3434, string DE1131, string DE3055, string DE3194, string DE6345)
        {
            string Segmento;
            if ((DE4471 != ""))
            {
                Segmento = ("FCA+"
                            + (DESimp(TratarTexto(DE4471, 3))
                            + (DEComp(TratarTexto(DE3434, 17), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE3194, 35), TratarTexto(DE6345, 3)) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentFCARead(string Segment, string Delimiter, string DE4471, string DE3434, string DE1131, string DE3055, string DE3194, string DE6345)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4471 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE3434 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3194 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE6345 = LecturaSegmento[5];
            }
        }

        public void SegmentFIIWrite(string DE3035, string DE3194, string DE3192, string DE3192_1, string DE6345, string DE3433, string DE1131, string DE3055, string DE3434, string DE1131_1, string DE3055_1, string DE3432, string DE3436, string DE3207)
        {
            string Segmento;
            if ((DE3035 != ""))
            {
                Segmento = ("FII+"
                            + (DESimp(TratarTexto(DE3035, 3))
                            + (DEComp(TratarTexto(DE3194, 35), TratarTexto(DE3192, 35), TratarTexto(DE3192_1, 35), TratarTexto(DE6345, 3))
                            + (DEComp(TratarTexto(DE3433, 11), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE3434, 17), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE3432, 70), TratarTexto(DE3436, 70))
                            + (DESimp(TratarTexto(DE3207, 3)) + "\'")))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentFIIRead(
                    string Segment,
                    string Delimiter,
                    string DE3035,
                    string DE3194,
                    string DE3192,
                    string DE3192_1,
                    string DE6345,
                    string DE3433,
                    string DE1131,
                    string DE3055,
                    string DE3434,
                    string DE1131_1,
                    string DE3055_1,
                    string DE3432,
                    string DE3436,
                    string DE3207)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE3035 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE3194 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3192 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3192_1 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE6345 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE3433 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE1131 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE3055 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE3434 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE1131_1 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE3055_1 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE3432 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE3436 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE3207 = LecturaSegmento[13];
            }
        }

        public void SegmentFNSWrite(string DE9430, string DE7405, string DE3039, string DE1131, string DE3055, string DE4405, string DE4513)
        {
            string Segmento;
            if ((DE9430 != ""))
            {
                Segmento = ("FNS+"
                            + (DEComp(TratarTexto(DE9430, 35), TratarTexto(DE7405, 3))
                            + (DEComp(TratarTexto(DE3039, 35), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                            + (DESimp(TratarTexto(DE4405, 3))
                            + (DESimp(TratarTexto(DE4513, 3)) + "\'")))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentFNSRead(string Segment, string Delimiter, string DE9430, string DE7405, string DE3039, string DE1131, string DE3055, string DE4405, string DE4513)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9430 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7405 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3039 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1131 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3055 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE4405 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4513 = LecturaSegmento[6];
            }
        }

        public void SegmentFNTWrite(string DE9432, string DE7405, string DE3039, string DE1131, string DE3055, string DE4405, string DE4513)
        {
            string Segmento;
            if ((DE9432 != ""))
            {
                Segmento = ("FNT+"
                            + (DEComp(TratarTexto(DE9432, 35), TratarTexto(DE7405, 3))
                            + (DEComp(TratarTexto(DE3039, 35), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                            + (DESimp(TratarTexto(DE4405, 3))
                            + (DESimp(TratarTexto(DE4513, 3)) + "\'")))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentFNTRead(string Segment, string Delimiter, string DE9432, string DE7405, string DE3039, string DE1131, string DE3055, string DE4405, string DE4513)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9432 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7405 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3039 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1131 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3055 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE4405 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4513 = LecturaSegmento[6];
            }
        }

        public void SegmentFTXWrite(string DE4451, string DE4453, string DE4441, string DE1131, string DE3055, string DE4440, string DE4440_1, string DE4440_2, string DE4440_3, string DE4440_4, string DE3453)
        {
            string Segmento;
            if ((DE4451 != ""))
            {
                Segmento = ("FTX+"
                            + (DESimp(TratarTexto(DE4451, 3))
                            + (DESimp(TratarTexto(DE4453, 3))
                            + (DEComp(TratarTexto(DE4441, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                            + (DEComp(TratarTexto(DE4440, 70), TratarTexto(DE4440_1, 70), TratarTexto(DE4440_2, 70), TratarTexto(DE4440_3, 70), TratarTexto(DE4440_4, 70))
                            + (DESimp(TratarTexto(DE3453, 3)) + "\'"))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentFTXRead(string Segment, string Delimiter, string DE4451, string DE4453, string DE4441, string DE1131, string DE3055, string DE4440, string DE4440_1, string DE4440_2, string DE4440_3, string DE4440_4, string DE3453)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4451 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE4453 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE4441 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1131 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3055 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE4440 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4440_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE4440_2 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE4440_3 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE4440_4 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE3453 = LecturaSegmento[10];
            }
        }

        public void SegmentGDSWrite(string DE7085, string DE1131, string DE3055)
        {
            string Segmento;
            Segmento = ("GDS+"
                        + (DEComp(TratarTexto(DE7085, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3)) + "\'"));
            EscribeSegmento(Segmento);
        }

        public void SegmentGDSRead(string Segment, string Delimiter, string DE7085, string DE1131, string DE3055)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7085 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
        }

        public void SegmentGIDWrite(
                    string DE1496,
                    string DE7224,
                    string DE7065,
                    string DE1131,
                    string DE3055,
                    string DE7064,
                    string DE7224_1,
                    string DE7065_1,
                    string DE1131_1,
                    string DE3055_1,
                    string DE7064_1,
                    string DE7224_2,
                    string DE7065_2,
                    string DE1131_2,
                    string DE3055_2,
                    string DE7064_2)
        {
            string Segmento;
            Segmento = ("GID+"
                        + (DESimp(DE1496)
                        + (DEComp(DE7224, TratarTexto(DE7065, 17), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE7064, 35))
                        + (DEComp(DE7224_1, TratarTexto(DE7065_1, 17), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE7064_1, 35))
                        + (DEComp(DE7224_2, TratarTexto(DE7065_2, 17), TratarTexto(DE1131_2, 3), TratarTexto(DE3055_2, 3), TratarTexto(DE7064_2, 35)) + "\'")))));
            EscribeSegmento(Segmento);
        }

        public void SegmentGIDRead(
                    string Segment,
                    string Delimiter,
                    string DE1496,
                    string DE7224,
                    string DE7065,
                    string DE1131,
                    string DE3055,
                    string DE7064,
                    string DE7224_1,
                    string DE7065_1,
                    string DE1131_1,
                    string DE3055_1,
                    string DE7064_1,
                    string DE7224_2,
                    string DE7065_2,
                    string DE1131_2,
                    string DE3055_2,
                    string DE7064_2)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE1496 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7224 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE7065 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1131 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3055 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE7064 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE7224_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE7065_1 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE1131_1 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE3055_1 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE7064_1 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE7224_2 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE7065_2 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE1131_2 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE3055_2 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE7064_2 = LecturaSegmento[15];
            }
        }

        public void SegmentGINWrite(string DE7405, string DE7402, string DE7402_1, string DE7402_2, string DE7402_3, string DE7402_4, string DE7402_5, string DE7402_6, string DE7402_7, string DE7402_8, string DE7402_9)
        {
            string Segmento;
            if (((DE7405 != "")
                        && (DE7402 != "")))
            {
                Segmento = ("GIN+"
                            + (DESimp(TratarTexto(DE7405, 3))
                            + (DEComp(TratarTexto(DE7402, 35), TratarTexto(DE7402_1, 35))
                            + (DEComp(TratarTexto(DE7402_2, 35), TratarTexto(DE7402_3, 35))
                            + (DEComp(TratarTexto(DE7402_4, 35), TratarTexto(DE7402_5, 35))
                            + (DEComp(TratarTexto(DE7402_6, 35), TratarTexto(DE7402_7, 35))
                            + (DEComp(TratarTexto(DE7402_8, 35), TratarTexto(DE7402_9, 35)) + "\'")))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentGINRead(string Segment, string Delimiter, string DE7405, string DE7402, string DE7402_1, string DE7402_2, string DE7402_3, string DE7402_4, string DE7402_5, string DE7402_6, string DE7402_7, string DE7402_8, string DE7402_9)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7405 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7402 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE7402_1 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE7402_2 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE7402_3 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE7402_4 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE7402_5 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE7402_6 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE7402_7 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE7402_8 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE7402_9 = LecturaSegmento[10];
            }
        }

        public void SegmentGIRWrite(
                    string DE7297,
                    string DE7402,
                    string DE7405,
                    string DE4405,
                    string DE7402_1,
                    string DE7405_1,
                    string DE4405_1,
                    string DE7402_2,
                    string DE7405_2,
                    string DE4405_2,
                    string DE7402_3,
                    string DE7405_3,
                    string DE4405_3,
                    string DE7402_4,
                    string DE7405_4,
                    string DE4405_4)
        {
            string Segmento;
            if (((DE7297 != "")
                        && (DE7402 != "")))
            {
                Segmento = ("GIR+"
                            + (DESimp(TratarTexto(DE7297, 3))
                            + (DEComp(TratarTexto(DE7402, 35), TratarTexto(DE7405, 3), TratarTexto(DE4405, 3))
                            + (DEComp(TratarTexto(DE7402_1, 35), TratarTexto(DE7405_1, 3), TratarTexto(DE4405_1, 3))
                            + (DEComp(TratarTexto(DE7402_2, 35), TratarTexto(DE7405_2, 3), TratarTexto(DE4405_2, 3))
                            + (DEComp(TratarTexto(DE7402_3, 35), TratarTexto(DE7405_3, 3), TratarTexto(DE4405_3, 3))
                            + (DEComp(TratarTexto(DE7402_4, 35), TratarTexto(DE7405_4, 3), TratarTexto(DE4405_4, 3)) + "\'")))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentGIRRead(
                    string Segment,
                    string Delimiter,
                    string DE7297,
                    string DE7402,
                    string DE7405,
                    string DE4405,
                    string DE7402_1,
                    string DE7405_1,
                    string DE4405_1,
                    string DE7402_2,
                    string DE7405_2,
                    string DE4405_2,
                    string DE7402_3,
                    string DE7405_3,
                    string DE4405_3,
                    string DE7402_4,
                    string DE7405_4,
                    string DE4405_4)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7297 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7402 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE7405 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE4405 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE7402_1 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE7405_1 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4405_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE7402_2 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE7405_2 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE4405_2 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE7402_3 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE7405_3 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE4405_3 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE7402_4 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE7405_4 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE4405_4 = LecturaSegmento[15];
            }
        }

        public void SegmentGISWrite(string DE7365, string DE1131, string DE3055, string DE7187)
        {
            string Segmento;
            if ((DE7365 != ""))
            {
                Segmento = ("GIS+"
                            + (DEComp(TratarTexto(DE7365, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE7187, 17)) + "\'"));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentGISRead(string Segment, string Delimiter, string DE7365, string DE1131, string DE3055, string DE7187)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7365 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE7187 = LecturaSegmento[3];
            }
        }

        public void SegmentGORWrite(
                    string DE8323,
                    string DE9415,
                    string DE9411,
                    string DE9417,
                    string DE9353,
                    string DE9415_1,
                    string DE9411_1,
                    string DE9417_1,
                    string DE9353_1,
                    string DE9415_2,
                    string DE9411_2,
                    string DE9417_2,
                    string DE9353_2,
                    string DE9415_3,
                    string DE9411_3,
                    string DE9417_3,
                    string DE9353_3)
        {
            string Segmento;
            Segmento = ("GOR+"
                        + (DESimp(TratarTexto(DE8323, 3))
                        + (DEComp(TratarTexto(DE9415, 3), TratarTexto(DE9411, 3), TratarTexto(DE9417, 3), TratarTexto(DE9353, 3))
                        + (DEComp(TratarTexto(DE9415_1, 3), TratarTexto(DE9411_1, 3), TratarTexto(DE9417_1, 3), TratarTexto(DE9353_1, 3))
                        + (DEComp(TratarTexto(DE9415_2, 3), TratarTexto(DE9411_2, 3), TratarTexto(DE9417_2, 3), TratarTexto(DE9353_2, 3))
                        + (DEComp(TratarTexto(DE9415_3, 3), TratarTexto(DE9411_3, 3), TratarTexto(DE9417_3, 3), TratarTexto(DE9353_3, 3)) + "\'"))))));
            EscribeSegmento(Segmento);
        }

        public void SegmentGORRead(
                    string Segment,
                    string Delimiter,
                    string DE8323,
                    string DE9415,
                    string DE9411,
                    string DE9417,
                    string DE9353,
                    string DE9415_1,
                    string DE9411_1,
                    string DE9417_1,
                    string DE9353_1,
                    string DE9415_2,
                    string DE9411_2,
                    string DE9417_2,
                    string DE9353_2,
                    string DE9415_3,
                    string DE9411_3,
                    string DE9417_3,
                    string DE9353_3)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE8323 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE9415 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE9411 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE9417 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE9353 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE9415_1 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE9411_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE9417_1 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE9353_1 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE9415_2 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE9411_2 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE9417_2 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE9353_2 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE9415_3 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE9411_3 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE9417_3 = LecturaSegmento[15];
            }
            if ((LecturaSegmento.Length > 16))
            {
                DE9353_3 = LecturaSegmento[16];
            }
        }

        public void SegmentGRUWrite(string DE9164, string DE7299, string DE6176, string DE4513, string DE1050)
        {
            string Segmento;
            if ((DE9164 != ""))
            {
                Segmento = ("GRU+"
                            + (DESimp(TratarTexto(DE9164, 4))
                            + (DESimp(TratarTexto(DE7299, 3))
                            + (DESimp(DE6176)
                            + (DESimp(TratarTexto(DE4513, 3))
                            + (DESimp(TratarTexto(DE1050, 6)) + "\'"))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentGRURead(string Segment, string Delimiter, string DE9164, string DE7299, string DE6176, string DE4513, string DE1050)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9164 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7299 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE6176 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE4513 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1050 = LecturaSegmento[4];
            }
        }

        public void SegmentHANWrite(string DE4079, string DE1131, string DE3055, string DE4078, string DE7419, string DE1131_1, string DE3055_1)
        {
            string Segmento;
            Segmento = ("HAN+"
                        + (DEComp(TratarTexto(DE4079, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE4078, 70))
                        + (DEComp(TratarTexto(DE7419, 4), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3)) + "\'")));
            EscribeSegmento(Segmento);
        }

        public void SegmentHANRead(string Segment, string Delimiter, string DE4079, string DE1131, string DE3055, string DE4078, string DE7419, string DE1131_1, string DE3055_1)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4079 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE4078 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE7419 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE1131_1 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE3055_1 = LecturaSegmento[6];
            }
        }

        public void SegmentICDWrite(string DE4497, string DE1131, string DE3055, string DE4495, string DE1131_1, string DE3055_1, string DE4494, string DE4494_1)
        {
            string Segmento;
            if ((DE4497 != ""))
            {
                Segmento = ("ICD+"
                            + (DEComp(TratarTexto(DE4497, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                            + (DEComp(TratarTexto(DE4495, 17), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE4494, 35), TratarTexto(DE4494_1, 35)) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentICDRead(string Segment, string Delimiter, string DE4497, string DE1131, string DE3055, string DE4495, string DE1131_1, string DE3055_1, string DE4494, string DE4494_1)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4497 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE4495 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1131_1 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE3055_1 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4494 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE4494_1 = LecturaSegmento[7];
            }
        }

        public void SegmentIDEWrite(
                    string DE7495,
                    string DE7402,
                    string DE7405,
                    string DE4405,
                    string DE3039,
                    string DE1131,
                    string DE3055,
                    string DE4405_1,
                    string DE1222,
                    string DE7164,
                    string DE1050,
                    string DE7037,
                    string DE1131_1,
                    string DE3055_1,
                    string DE7036,
                    string DE7036_1)
        {
            string Segmento;
            if (((DE7495 != "")
                        && (DE7402 != "")))
            {
                Segmento = ("IDE+"
                            + (DESimp(TratarTexto(DE7495, 3))
                            + (DEComp(TratarTexto(DE7402, 35), TratarTexto(DE7405, 3), TratarTexto(DE4405, 3))
                            + (DEComp(TratarTexto(DE3039, 35), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                            + (DESimp(TratarTexto(DE4405_1, 3))
                            + (DESimp(DE1222)
                            + (DEComp(TratarTexto(DE7164, 12), TratarTexto(DE1050, 6))
                            + (DEComp(TratarTexto(DE7037, 17), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE7036, 35), TratarTexto(DE7036_1, 35)) + "\'"))))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentIDERead(
                    string Segment,
                    string Delimiter,
                    string DE7495,
                    string DE7402,
                    string DE7405,
                    string DE4405,
                    string DE3039,
                    string DE1131,
                    string DE3055,
                    string DE4405_1,
                    string DE1222,
                    string DE7164,
                    string DE1050,
                    string DE7037,
                    string DE1131_1,
                    string DE3055_1,
                    string DE7036,
                    string DE7036_1)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7495 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7402 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE7405 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE4405 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3039 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE1131 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE3055 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE4405_1 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE1222 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE7164 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE1050 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE7037 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE1131_1 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE3055_1 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE7036 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE7036_1 = LecturaSegmento[15];
            }
        }

        public void SegmentIHCWrite(string DE3289, string DE3311, string DE1131, string DE3055, string DE3310)
        {
            string Segmento;
            if ((DE3289 != ""))
            {
                Segmento = ("IHC+"
                            + (DESimp(TratarTexto(DE3289, 3))
                            + (DEComp(TratarTexto(DE3311, 8), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE3310, 70)) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentIHCRead(string Segment, string Delimiter, string DE3289, string DE3311, string DE1131, string DE3055, string DE3310)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE3289 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE3311 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3310 = LecturaSegmento[4];
            }
        }

        public void SegmentIMDWrite(string DE7077, string DE7081, string DE7009, string DE1131, string DE3055, string DE7008, string DE7008_1, string DE3453, string DE7383)
        {
            string Segmento;
            Segmento = ("IMD+"
                        + (DESimp(TratarTexto(DE7077, 3))
                        + (DESimp(TratarTexto(DE7081, 3))
                        + (DEComp(TratarTexto(DE7009, 17), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE7008, 35), TratarTexto(DE7008_1, 35), TratarTexto(DE3453, 3))
                        + (DESimp(TratarTexto(DE7383, 3)) + "\'")))));
            EscribeSegmento(Segmento);
        }

        public void SegmentIMDRead(string Segment, string Delimiter, ref string DE7077, ref string DE7081, ref string DE7009, ref string DE1131, ref string DE3055, ref string DE7008, ref string DE7008_1, ref string DE3453, ref string DE7383)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7077 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7081 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE7009 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1131 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3055 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE7008 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE7008_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE3453 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE7383 = LecturaSegmento[8];
            }
        }

        public void SegmentINDWrite(string DE5013, string DE5027, string DE1131, string DE3055, string DE5030, string DE5039)
        {
            string Segmento;
            Segmento = ("IND+"
                        + (DEComp(TratarTexto(DE5013, 3), TratarTexto(DE5027, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                        + (DEComp(DE5030, TratarTexto(DE5039, 3)) + "\'")));
            EscribeSegmento(Segmento);
        }

        public void SegmentINDRead(string Segment, string Delimiter, string DE5013, string DE5027, string DE1131, string DE3055, string DE5030, string DE5039)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE5013 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE5027 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE5030 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE5039 = LecturaSegmento[5];
            }
        }

        public void SegmentINPWrite(string DE3301, string DE3285, string DE4403, string DE4401, string DE1131, string DE3055, string DE4400, string DE4405, string DE3036, string DE1229)
        {
            string Segmento;
            Segmento = ("INP+"
                        + (DEComp(TratarTexto(DE3301, 17), TratarTexto(DE3285, 17))
                        + (DEComp(TratarTexto(DE4403, 3), TratarTexto(DE4401, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE4400, 35))
                        + (DEComp(TratarTexto(DE4405, 3), TratarTexto(DE3036, 35))
                        + (DESimp(TratarTexto(DE1229, 3)) + "\'")))));
            EscribeSegmento(Segmento);
        }

        public void SegmentINPRead(string Segment, string Delimiter, string DE3301, string DE3285, string DE4403, string DE4401, string DE1131, string DE3055, string DE4400, string DE4405, string DE3036, string DE1229)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE3301 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE3285 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE4403 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE4401 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1131 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE3055 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4400 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE4405 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE3036 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE1229 = LecturaSegmento[9];
            }
        }

        public void SegmentINVWrite(string DE4501, string DE7491, string DE4499, string DE4503, string DE4403, string DE4401, string DE1131, string DE3055, string DE4400)
        {
            string Segmento;
            Segmento = ("INV+"
                        + (DESimp(TratarTexto(DE4501, 3))
                        + (DESimp(TratarTexto(DE7491, 3))
                        + (DESimp(TratarTexto(DE4499, 3))
                        + (DESimp(TratarTexto(DE4503, 3))
                        + (DEComp(TratarTexto(DE4403, 3), TratarTexto(DE4401, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE4400, 35)) + "\'"))))));
            EscribeSegmento(Segmento);
        }

        public void SegmentINVRead(string Segment, string Delimiter, string DE4501, string DE7491, string DE4499, string DE4503, string DE4403, string DE4401, string DE1131, string DE3055, string DE4400)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4501 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7491 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE4499 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE4503 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE4403 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE4401 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE1131 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE3055 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE4400 = LecturaSegmento[8];
            }
        }

        public void SegmentIRQWrite(string DE4511, string DE1131, string DE3055, string DE4510)
        {
            string Segmento;
            Segmento = ("IRQ+"
                        + (DEComp(TratarTexto(DE4511, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE4510, 35)) + "\'"));
            EscribeSegmento(Segmento);
        }

        public void SegmentIRQRead(string Segment, string Delimiter, string DE4511, string DE1131, string DE3055, string DE4510)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4511 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE4510 = LecturaSegmento[3];
            }
        }

        public void SegmentLANWrite(string DE3455, string DE3453, string DE3452)
        {
            string Segmento;
            if ((DE3455 != ""))
            {
                Segmento = ("LAN+"
                            + (DESimp(TratarTexto(DE3455, 3))
                            + (DEComp(TratarTexto(DE3453, 3), TratarTexto(DE3452, 35)) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentLANRead(string Segment, string Delimiter, string DE3455, string DE3453, string DE3452)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE3455 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE3453 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3452 = LecturaSegmento[2];
            }
        }

        public void SegmentLINWrite(string DE1082, string DE1229, string DE7140, string DE7143, string DE1131, string DE3055, string DE5495, string DE1082_1, string DE1222, string DE7083)
        {
            string Segmento;
            Segmento = ("LIN+"
                        + (DESimp(DE1082)
                        + (DESimp(TratarTexto(DE1229, 3))
                        + (DEComp(TratarTexto(DE7140, 35), TratarTexto(DE7143, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                        + (DEComp(TratarTexto(DE5495, 3), DE1082_1)
                        + (DESimp(DE1222)
                        + (DESimp(TratarTexto(DE7083, 3)) + "\'")))))));
            EscribeSegmento(Segmento);
        }

        public void SegmentLINRead(string Segment, string Delimiter, ref string DE1082, ref string DE1229, ref string DE7140, ref string DE7143, ref string DE1131, ref string DE3055, ref string DE5495, ref string DE1082_1, ref string DE1222, ref string DE7083)
        {
            string[] LecturaSegmento;
            Segment = Segment.Substring(Segment.IndexOf("LIN"), Segment.Length - Segment.IndexOf("LIN"));
            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE1082 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1229 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE7140 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE7143 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1131 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE3055 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE5495 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE1082_1 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE1222 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE7083 = LecturaSegmento[9];
            }
        }

        public void SegmentLOCWrite(string DE3227, string DE3225, string DE1131, string DE3055, string DE3224, string DE3223, string DE1131_1, string DE3055_1, string DE3222, string DE3233, string DE1131_2, string DE3055_2, string DE3232, string DE5479)
        {
            string Segmento;
            if ((DE3227 != ""))
            {
                Segmento = ("LOC+"
                            + (DESimp(TratarTexto(DE3227, 3))
                            + (DEComp(TratarTexto(DE3225, 25), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE3224, 70))
                            + (DEComp(TratarTexto(DE3223, 25), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE3222, 70))
                            + (DEComp(TratarTexto(DE3233, 25), TratarTexto(DE1131_2, 3), TratarTexto(DE3055_2, 3), TratarTexto(DE3232, 70))
                            + (DESimp(TratarTexto(DE5479, 3)) + "\'"))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentLOCRead(
                    string Segment,
                    string Delimiter,
                    ref string DE3227,
                    ref string DE3225,
                    ref string DE1131,
                    ref string DE3055,
                    ref string DE3224,
                    ref string DE3223,
                    ref string DE1131_1,
                    ref string DE3055_1,
                    ref string DE3222,
                    ref string DE3233,
                    ref string DE1131_2,
                    ref string DE3055_2,
                    ref string DE3232,
                    ref string DE5479)
        {
            string[] LecturaSegmento;
            Segment = Segment.Substring(Segment.IndexOf("LOC"), Segment.Length - Segment.IndexOf("LOC"));
            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE3227 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE3225 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3224 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE3223 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE1131_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE3055_1 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE3222 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE3233 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE1131_2 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE3055_2 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE3232 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE5479 = LecturaSegmento[13];
            }
        }

        public void SegmentMEAWrite(string DE6311, string DE6313, string DE6321, string DE6155, string DE6154, string DE6411, string DE6314, string DE6162, string DE6152, string DE6432, string DE7383)
        {
            string Segmento;
            if ((DE6311 != ""))
            {
                Segmento = ("MEA+"
                            + (DESimp(TratarTexto(DE6311, 3))
                            + (DEComp(TratarTexto(DE6313, 3), TratarTexto(DE6321, 3), TratarTexto(DE6155, 3), TratarTexto(DE6154, 70))
                            + (DEComp(TratarTexto(DE6411, 3), DE6314, DE6162, DE6152, DE6432)
                            + (DESimp(TratarTexto(DE7383, 3)) + "\'")))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentMEARead(string Segment, string Delimiter, string DE6311, string DE6313, string DE6321, string DE6155, string DE6154, string DE6411, string DE6314, string DE6162, string DE6152, string DE6432, string DE7383)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE6311 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE6313 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE6321 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE6155 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE6154 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE6411 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE6314 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE6162 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE6152 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE6432 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE7383 = LecturaSegmento[10];
            }
        }

        public void SegmentMEMWrite(
                    string DE7449,
                    string DE7451,
                    string DE1131,
                    string DE3055,
                    string DE7450,
                    string DE7453,
                    string DE1131_1,
                    string DE3055_1,
                    string DE7452,
                    string DE7455,
                    string DE7457,
                    string DE1131_2,
                    string DE3055_2,
                    string DE7456,
                    string DE5243,
                    string DE1131_3,
                    string DE3055_3,
                    string DE5242,
                    string DE5275,
                    string DE1131_4,
                    string DE3055_4,
                    string DE5275_1,
                    string DE1131_5,
                    string DE3055_5,
                    string DE4295,
                    string DE1131_6,
                    string DE3055_6,
                    string DE4294)
        {
            string Segmento;
            if ((DE7449 != ""))
            {
                Segmento = ("MEM+"
                            + (DESimp(TratarTexto(DE7449, 3))
                            + (DEComp(TratarTexto(DE7451, 4), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE7450, 35))
                            + (DEComp(TratarTexto(DE7453, 3), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE7452, 35))
                            + (DEComp(TratarTexto(DE7455, 3), TratarTexto(DE7457, 9), TratarTexto(DE1131_2, 3), TratarTexto(DE3055_2, 3), TratarTexto(DE7456, 35))
                            + (DEComp(TratarTexto(DE5243, 9), TratarTexto(DE1131_3, 3), TratarTexto(DE3055_3, 3), TratarTexto(DE5242, 35), TratarTexto(DE5275, 6), TratarTexto(DE1131_4, 3), TratarTexto(DE3055_4, 3), TratarTexto(DE5275_1, 6), TratarTexto(DE1131_5, 3), TratarTexto(DE3055_5, 3))
                            + (DEComp(TratarTexto(DE4295, 3), TratarTexto(DE1131_6, 3), TratarTexto(DE3055_6, 3), TratarTexto(DE4294, 35)) + "\'")))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentMEMRead(
                    string Segment,
                    string Delimiter,
                    string DE7449,
                    string DE7451,
                    string DE1131,
                    string DE3055,
                    string DE7450,
                    string DE7453,
                    string DE1131_1,
                    string DE3055_1,
                    string DE7452,
                    string DE7455,
                    string DE7457,
                    string DE1131_2,
                    string DE3055_2,
                    string DE7456,
                    string DE5243,
                    string DE1131_3,
                    string DE3055_3,
                    string DE5242,
                    string DE5275,
                    string DE1131_4,
                    string DE3055_4,
                    string DE5275_1,
                    string DE1131_5,
                    string DE3055_5,
                    string DE4295,
                    string DE1131_6,
                    string DE3055_6,
                    string DE4294)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7449 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7451 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE7450 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE7453 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE1131_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE3055_1 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE7452 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE7455 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE7457 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE1131_2 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE3055_2 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE7456 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE5243 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE1131_3 = LecturaSegmento[15];
            }
            if ((LecturaSegmento.Length > 16))
            {
                DE3055_3 = LecturaSegmento[16];
            }
            if ((LecturaSegmento.Length > 17))
            {
                DE5242 = LecturaSegmento[17];
            }
            if ((LecturaSegmento.Length > 18))
            {
                DE5275 = LecturaSegmento[18];
            }
            if ((LecturaSegmento.Length > 19))
            {
                DE1131_4 = LecturaSegmento[19];
            }
            if ((LecturaSegmento.Length > 20))
            {
                DE3055_4 = LecturaSegmento[20];
            }
            if ((LecturaSegmento.Length > 21))
            {
                DE5275_1 = LecturaSegmento[21];
            }
            if ((LecturaSegmento.Length > 22))
            {
                DE1131_5 = LecturaSegmento[22];
            }
            if ((LecturaSegmento.Length > 23))
            {
                DE3055_5 = LecturaSegmento[23];
            }
            if ((LecturaSegmento.Length > 24))
            {
                DE4295 = LecturaSegmento[24];
            }
            if ((LecturaSegmento.Length > 25))
            {
                DE1131_6 = LecturaSegmento[25];
            }
            if ((LecturaSegmento.Length > 26))
            {
                DE3055_6 = LecturaSegmento[26];
            }
            if ((LecturaSegmento.Length > 27))
            {
                DE4294 = LecturaSegmento[27];
            }
        }

        public void SegmentMKSWrite(string DE7293, string DE3496, string DE1131, string DE3055, string DE1229)
        {
            string Segmento;
            if (((DE7293 != "")
                        && (DE3496 != "")))
            {
                Segmento = ("MKS+"
                            + (DESimp(TratarTexto(DE7293, 3))
                            + (DEComp(TratarTexto(DE3496, 17), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                            + (DESimp(TratarTexto(DE1229, 3)) + "\'"))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentMKSRead(string Segment, string Delimiter, string DE7293, string DE3496, string DE1131, string DE3055, string DE1229)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7293 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE3496 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1229 = LecturaSegmento[4];
            }
        }

        public void SegmentMOAWrite(string DE5025, string DE5004, string DE6345, string DE6343, string DE4405)
        {
            string Segmento;
            if ((DE5025 != ""))
            {
                Segmento = ("MOA+"
                            + (DEComp(TratarTexto(DE5025, 3), DE5004, TratarTexto(DE6345, 3), TratarTexto(DE6343, 3), TratarTexto(DE4405, 3)) + "\'"));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentMOARead(string Segment, string Delimiter, ref string DE5025, ref string DE5004, ref string DE6345, ref string DE6343, ref string DE4405)
        {
            string[] LecturaSegmento;
            Segment = Segment.Substring(Segment.IndexOf("MOA"), Segment.Length - Segment.IndexOf("MOA"));
            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE5025 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE5004 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE6345 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE6343 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE4405 = LecturaSegmento[4];
            }
        }

        public void SegmentMSGWrite(string DE1475, string DE1056, string DE1058, string DE1476, string DE1523, string DE1060, string DE1507, string DE4513)
        {
            string Segmento;
            if (((DE1475 != "")
                        && ((DE1056 != "")
                        && ((DE1058 != "")
                        && (DE1476 != "")))))
            {
                Segmento = ("MSG+"
                            + (DEComp(TratarTexto(DE1475, 6), TratarTexto(DE1056, 9), TratarTexto(DE1058, 9), TratarTexto(DE1476, 2), TratarTexto(DE1523, 6), TratarTexto(DE1060, 6))
                            + (DESimp(TratarTexto(DE1507, 3))
                            + (DESimp(TratarTexto(DE4513, 3)) + "\'"))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentMSGRead(string Segment, string Delimiter, string DE1475, string DE1056, string DE1058, string DE1476, string DE1523, string DE1060, string DE1507, string DE4513)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE1475 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1056 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1058 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1476 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1523 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE1060 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE1507 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE4513 = LecturaSegmento[7];
            }
        }

        public void SegmentNADWrite(
                    string DE3035,
                    string DE3039,
                    string DE1131,
                    string DE3055,
                    string DE3124,
                    string DE3124_1,
                    string DE3124_2,
                    string DE3124_3,
                    string DE3124_4,
                    string DE3036,
                    string DE3036_1,
                    string DE3036_2,
                    string DE3036_3,
                    string DE3036_4,
                    string DE3045,
                    string DE3042,
                    string DE3042_1,
                    string DE3042_2,
                    string DE3042_3,
                    string DE3164,
                    string DE3229,
                    string DE3251,
                    string DE3207)
        {
            string Segmento;
            if ((DE3035 != ""))
            {
                Segmento = ("NAD+"
                            + (DESimp(TratarTexto(DE3035, 3))
                            + (DEComp(TratarTexto(DE3039, 35), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                            + (DEComp(TratarTexto(DE3124, 35), TratarTexto(DE3124_1, 35), TratarTexto(DE3124_2, 35), TratarTexto(DE3124_3, 35), TratarTexto(DE3124_4, 35))
                            + (DEComp(TratarTexto(DE3036, 35), TratarTexto(DE3036_1, 35), TratarTexto(DE3036_2, 35), TratarTexto(DE3036_3, 35), TratarTexto(DE3036_4, 35), TratarTexto(DE3045, 3))
                            + (DEComp(TratarTexto(DE3042, 35), TratarTexto(DE3042_1, 35), TratarTexto(DE3042_2, 35), TratarTexto(DE3042_3, 35))
                            + (DESimp(TratarTexto(DE3164, 35))
                            + (DESimp(TratarTexto(DE3229, 9))
                            + (DESimp(TratarTexto(DE3251, 9))
                            + (DESimp(TratarTexto(DE3207, 3)) + "\'"))))))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentNADRead(
                    string Segment,
                    string Delimiter,
                    ref string DE3035,
                    ref string DE3039,
                    ref string DE1131,
                    ref string DE3055,
                    ref string DE3124,
                    ref string DE3124_1,
                    ref string DE3124_2,
                    ref string DE3124_3,
                    ref string DE3124_4,
                    ref string DE3036,
                    ref string DE3036_1,
                    ref string DE3036_2,
                    ref string DE3036_3,
                    ref string DE3036_4,
                    ref string DE3045,
                    ref string DE3042,
                    ref string DE3042_1,
                    ref string DE3042_2,
                    ref string DE3042_3,
                    ref string DE3164,
                    ref string DE3229,
                    ref string DE3251,
                    ref string DE3207)
        {
            string[] LecturaSegmento;
            Segment = Segment.Substring(Segment.IndexOf("NAD"), Segment.Length - Segment.IndexOf("NAD"));
            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE3035 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE3039 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3124 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE3124_1 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE3124_2 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE3124_3 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE3124_4 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE3036 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE3036_1 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE3036_2 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE3036_3 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE3036_4 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE3045 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE3042 = LecturaSegmento[15];
            }
            if ((LecturaSegmento.Length > 16))
            {
                DE3042_1 = LecturaSegmento[16];
            }
            if ((LecturaSegmento.Length > 17))
            {
                DE3042_2 = LecturaSegmento[17];
            }
            if ((LecturaSegmento.Length > 18))
            {
                DE3042_3 = LecturaSegmento[18];
            }
            if ((LecturaSegmento.Length > 19))
            {
                DE3164 = LecturaSegmento[19];
            }
            if ((LecturaSegmento.Length > 20))
            {
                DE3229 = LecturaSegmento[20];
            }
            if ((LecturaSegmento.Length > 21))
            {
                DE3251 = LecturaSegmento[21];
            }
            if ((LecturaSegmento.Length > 22))
            {
                DE3207 = LecturaSegmento[22];
            }
        }

        public void SegmentNATWrite(string DE3493, string DE3293, string DE1131, string DE3055, string DE3292)
        {
            string Segmento;
            if ((DE3493 != ""))
            {
                Segmento = ("NAT+"
                            + (DESimp(TratarTexto(DE3493, 3))
                            + (DEComp(TratarTexto(DE3293, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE3292, 35)) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentNATRead(string Segment, string Delimiter, string DE3493, string DE3293, string DE1131, string DE3055, string DE3292)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE3493 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE3293 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3292 = LecturaSegmento[4];
            }
        }

        public void SegmentPACWrite(string DE7224, string DE7075, string DE7233, string DE7073, string DE7065, string DE1131, string DE3055, string DE7064, string DE7077, string DE7064_1, string DE7143, string DE7064_2, string DE7143_1, string DE8395, string DE8393)
        {
            string Segmento;
            Segmento = ("PAC+"
                        + (DESimp(DE7224)
                        + (DEComp(TratarTexto(DE7075, 3), TratarTexto(DE7233, 3), TratarTexto(DE7073, 3))
                        + (DEComp(TratarTexto(DE7065, 17), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE7064, 35))
                        + (DEComp(TratarTexto(DE7077, 3), TratarTexto(DE7064_1, 35), TratarTexto(DE7143, 3), TratarTexto(DE7064_2, 35), TratarTexto(DE7143_1, 3))
                        + (DEComp(TratarTexto(DE8395, 3), TratarTexto(DE8393, 3)) + "\'"))))));
            EscribeSegmento(Segmento);
        }

        public void SegmentPACRead(
                    string Segment,
                    string Delimiter,
                    string DE7224,
                    string DE7075,
                    string DE7233,
                    string DE7073,
                    string DE7065,
                    string DE1131,
                    string DE3055,
                    string DE7064,
                    string DE7077,
                    string DE7064_1,
                    string DE7143,
                    string DE7064_2,
                    string DE7143_1,
                    string DE8395,
                    string DE8393)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7224 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7075 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE7233 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE7073 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE7065 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE1131 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE3055 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE7064 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE7077 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE7064_1 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE7143 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE7064_2 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE7143_1 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE8395 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE8393 = LecturaSegmento[14];
            }
        }

        public void SegmentPAIWrite(string DE4439, string DE4431, string DE4461, string DE1131, string DE3055, string DE4435)
        {
            string Segmento;
            Segmento = ("PAI+"
                        + (DEComp(TratarTexto(DE4439, 3), TratarTexto(DE4431, 3), TratarTexto(DE4461, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE4435, 3)) + "\'"));
            EscribeSegmento(Segmento);
        }

        public void SegmentPAIRead(string Segment, string Delimiter, string DE4439, string DE4431, string DE4461, string DE1131, string DE3055, string DE4435)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4439 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE4431 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE4461 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1131 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3055 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE4435 = LecturaSegmento[5];
            }
        }

        public void SegmentPATWrite(string DE4279, string DE4277, string DE1131, string DE3055, string DE4276, string DE4276_1, string DE2475, string DE2009, string DE2151, string DE2152)
        {
            string Segmento;
            if ((DE4279 != ""))
            {
                Segmento = ("PAT+"
                            + (DESimp(TratarTexto(DE4279, 3))
                            + (DEComp(TratarTexto(DE4277, 17), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE4276, 35), TratarTexto(DE4276_1, 35))
                            + (DEComp(TratarTexto(DE2475, 3), TratarTexto(DE2009, 3), TratarTexto(DE2151, 3), DE2152) + "\'"))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentPATRead(string Segment, string Delimiter, string DE4279, string DE4277, string DE1131, string DE3055, string DE4276, string DE4276_1, string DE2475, string DE2009, string DE2151, string DE2152)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4279 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE4277 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE4276 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE4276_1 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE2475 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE2009 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE2151 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE2152 = LecturaSegmento[9];
            }
        }

        public void SegmentPCDWrite(string DE5245, string DE5482, string DE5249, string DE1131, string DE3055)
        {
            string Segmento;
            if ((DE5245 != ""))
            {
                Segmento = ("PCD+"
                            + (DEComp(TratarTexto(DE5245, 3), DE5482, TratarTexto(DE5249, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3)) + "\'"));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentPCDRead(string Segment, string Delimiter, string DE5245, string DE5482, string DE5249, string DE1131, string DE3055)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE5245 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE5482 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE5249 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1131 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3055 = LecturaSegmento[4];
            }
        }

        public void SegmentPCIWrite(string DE4233, string DE7102, string DE7102_1, string DE7102_2, string DE7102_3, string DE7102_4, string DE7102_5, string DE7102_6, string DE7102_7, string DE7102_8, string DE7102_9, string DE8275, string DE7511, string DE1131, string DE3055)
        {
            string Segmento;
            Segmento = ("PCI+"
                        + (DESimp(TratarTexto(DE4233, 3))
                        + (DEComp(TratarTexto(DE7102, 35), TratarTexto(DE7102_1, 35), TratarTexto(DE7102_2, 35), TratarTexto(DE7102_3, 35), TratarTexto(DE7102_4, 35), TratarTexto(DE7102_5, 35), TratarTexto(DE7102_6, 35), TratarTexto(DE7102_7, 35), TratarTexto(DE7102_8, 35), TratarTexto(DE7102_9, 35))
                        + (DESimp(TratarTexto(DE8275, 3))
                        + (DEComp(TratarTexto(DE7511, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3)) + "\'")))));
            EscribeSegmento(Segmento);
        }

        public void SegmentPCIRead(
                    string Segment,
                    string Delimiter,
                    string DE4233,
                    string DE7102,
                    string DE7102_1,
                    string DE7102_2,
                    string DE7102_3,
                    string DE7102_4,
                    string DE7102_5,
                    string DE7102_6,
                    string DE7102_7,
                    string DE7102_8,
                    string DE7102_9,
                    string DE8275,
                    string DE7511,
                    string DE1131,
                    string DE3055)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4233 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7102 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE7102_1 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE7102_2 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE7102_3 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE7102_4 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE7102_5 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE7102_6 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE7102_7 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE7102_8 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE7102_9 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE8275 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE7511 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE1131 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE3055 = LecturaSegmento[14];
            }
        }

        public void SegmentPDIWrite(string DE3499, string DE3479, string DE1131, string DE3055, string DE3478, string DE3483, string DE1131_1, string DE3055_1, string DE3482)
        {
            string Segmento;
            Segmento = ("PDI+"
                        + (DESimp(TratarTexto(DE3499, 3))
                        + (DEComp(TratarTexto(DE3479, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE3478, 35))
                        + (DEComp(TratarTexto(DE3483, 3), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE3482, 35)) + "\'"))));
            EscribeSegmento(Segmento);
        }

        public void SegmentPDIRead(string Segment, string Delimiter, string DE3499, string DE3479, string DE1131, string DE3055, string DE3478, string DE3483, string DE1131_1, string DE3055_1, string DE3482)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE3499 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE3479 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3478 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE3483 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE1131_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE3055_1 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE3482 = LecturaSegmento[8];
            }
        }

        public void SegmentPGIWrite(string DE5379, string DE5389, string DE1131, string DE3055, string DE5388)
        {
            string Segmento;
            if ((DE5379 != ""))
            {
                Segmento = ("PGI+"
                            + (DESimp(TratarTexto(DE5379, 3))
                            + (DEComp(TratarTexto(DE5389, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE5388, 35)) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentPGIRead(string Segment, string Delimiter, string DE5379, string DE5389, string DE1131, string DE3055, string DE5388)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE5379 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE5389 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE5388 = LecturaSegmento[4];
            }
        }

        public void SegmentPIAWrite(
                    string DE4347,
                    string DE7140,
                    string DE7143,
                    string DE1131,
                    string DE3055,
                    string DE7140_1,
                    string DE7143_1,
                    string DE1131_1,
                    string DE3055_1,
                    string DE7140_2,
                    string DE7143_2,
                    string DE1131_2,
                    string DE3055_2,
                    string DE7140_3,
                    string DE7143_3,
                    string DE1131_3,
                    string DE3055_3,
                    string DE7140_4,
                    string DE7143_4,
                    string DE1131_4,
                    string DE3055_4)
        {
            string Segmento;
            if ((DE4347 != ""))
            {
                Segmento = ("PIA+"
                            + (DESimp(TratarTexto(DE4347, 3))
                            + (DEComp(TratarTexto(DE7140, 35), TratarTexto(DE7143, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                            + (DEComp(TratarTexto(DE7140_1, 35), TratarTexto(DE7143_1, 3), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3))
                            + (DEComp(TratarTexto(DE7140_2, 35), TratarTexto(DE7143_2, 3), TratarTexto(DE1131_2, 3), TratarTexto(DE3055_2, 3))
                            + (DEComp(TratarTexto(DE7140_3, 35), TratarTexto(DE7143_3, 3), TratarTexto(DE1131_3, 3), TratarTexto(DE3055_3, 3))
                            + (DEComp(TratarTexto(DE7140_4, 35), TratarTexto(DE7143_4, 3), TratarTexto(DE1131_4, 3), TratarTexto(DE3055_4, 3)) + "\'")))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentPIARead(
                    string Segment,
                    string Delimiter,
                    ref string DE4347,
                    ref string DE7140,
                    ref string DE7143,
                    ref string DE1131,
                    ref string DE3055,
                    ref string DE7140_1,
                    ref string DE7143_1,
                    ref string DE1131_1,
                    ref string DE3055_1,
                    ref string DE7140_2,
                    ref string DE7143_2,
                    ref string DE1131_2,
                    ref string DE3055_2,
                    ref string DE7140_3,
                    ref string DE7143_3,
                    ref string DE1131_3,
                    ref string DE3055_3,
                    ref string DE7140_4,
                    ref string DE7143_4,
                    ref string DE1131_4,
                    ref string DE3055_4)
        {
            string[] LecturaSegmento;
            Segment = Segment.Substring(Segment.IndexOf("PIA"), Segment.Length - Segment.IndexOf("PIA"));
            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4347 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7140 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE7143 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1131 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3055 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE7140_1 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE7143_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE1131_1 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE3055_1 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE7140_2 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE7143_2 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE1131_2 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE3055_2 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE7140_3 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE7143_3 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE1131_3 = LecturaSegmento[15];
            }
            if ((LecturaSegmento.Length > 16))
            {
                DE3055_3 = LecturaSegmento[16];
            }
            if ((LecturaSegmento.Length > 17))
            {
                DE7140_4 = LecturaSegmento[17];
            }
            if ((LecturaSegmento.Length > 18))
            {
                DE7143_4 = LecturaSegmento[18];
            }
            if ((LecturaSegmento.Length > 19))
            {
                DE1131_4 = LecturaSegmento[19];
            }
            if ((LecturaSegmento.Length > 20))
            {
                DE3055_4 = LecturaSegmento[20];
            }
        }

        public void SegmentPITWrite(string DE1082, string DE1229, string DE5377, string DE1131, string DE3055, string DE7011, string DE5495, string DE1222, string DE7083)
        {
            string Segmento;
            Segmento = ("PIT+"
                        + (DESimp(DE1082)
                        + (DESimp(TratarTexto(DE1229, 3))
                        + (DEComp(TratarTexto(DE5377, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                        + (DESimp(TratarTexto(DE7011, 3))
                        + (DESimp(TratarTexto(DE5495, 3))
                        + (DESimp(DE1222)
                        + (DESimp(TratarTexto(DE7083, 3)) + "\'"))))))));
            EscribeSegmento(Segmento);
        }

        public void SegmentPITRead(string Segment, string Delimiter, string DE1082, string DE1229, string DE5377, string DE1131, string DE3055, string DE7011, string DE5495, string DE1222, string DE7083)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE1082 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1229 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE5377 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1131 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3055 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE7011 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE5495 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE1222 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE7083 = LecturaSegmento[8];
            }
        }

        public void SegmentPNAWrite(
                    string DE3035,
                    string DE7402,
                    string DE7405,
                    string DE4405,
                    string DE3039,
                    string DE1131,
                    string DE3055,
                    string DE3403,
                    string DE3397,
                    string DE3405,
                    string DE3398,
                    string DE3401,
                    string DE3295,
                    string DE3405_1,
                    string DE3398_1,
                    string DE3401_1,
                    string DE3295_1,
                    string DE3405_2,
                    string DE3398_2,
                    string DE3401_2,
                    string DE3295_2,
                    string DE3405_3,
                    string DE3398_3,
                    string DE3401_3,
                    string DE3295_3,
                    string DE3405_4,
                    string DE3398_4,
                    string DE3401_4,
                    string DE3295_4)
        {
            string Segmento;
            if ((DE3035 != ""))
            {
                Segmento = ("PNA+"
                            + (DESimp(TratarTexto(DE3035, 3))
                            + (DEComp(TratarTexto(DE7402, 35), TratarTexto(DE7405, 3), TratarTexto(DE4405, 3))
                            + (DEComp(TratarTexto(DE3039, 35), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                            + (DESimp(TratarTexto(DE3403, 3))
                            + (DESimp(TratarTexto(DE3397, 3))
                            + (DEComp(TratarTexto(DE3405, 3), TratarTexto(DE3398, 70), TratarTexto(DE3401, 3), TratarTexto(DE3295, 3))
                            + (DEComp(TratarTexto(DE3405_1, 3), TratarTexto(DE3398_1, 70), TratarTexto(DE3401_1, 3), TratarTexto(DE3295_1, 3))
                            + (DEComp(TratarTexto(DE3405_2, 3), TratarTexto(DE3398_2, 70), TratarTexto(DE3401_2, 3), TratarTexto(DE3295_2, 3))
                            + (DEComp(TratarTexto(DE3405_3, 3), TratarTexto(DE3398_3, 70), TratarTexto(DE3401_3, 3), TratarTexto(DE3295_3, 3))
                            + (DEComp(TratarTexto(DE3405_4, 3), TratarTexto(DE3398_4, 70), TratarTexto(DE3401_4, 3), TratarTexto(DE3295_4, 3)) + "\'")))))))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentPNARead(
                    string Segment,
                    string Delimiter,
                    string DE3035,
                    string DE7402,
                    string DE7405,
                    string DE4405,
                    string DE3039,
                    string DE1131,
                    string DE3055,
                    string DE3403,
                    string DE3397,
                    string DE3405,
                    string DE3398,
                    string DE3401,
                    string DE3295,
                    string DE3405_1,
                    string DE3398_1,
                    string DE3401_1,
                    string DE3295_1,
                    string DE3405_2,
                    string DE3398_2,
                    string DE3401_2,
                    string DE3295_2,
                    string DE3405_3,
                    string DE3398_3,
                    string DE3401_3,
                    string DE3295_3,
                    string DE3405_4,
                    string DE3398_4,
                    string DE3401_4,
                    string DE3295_4)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE3035 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7402 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE7405 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE4405 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3039 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE1131 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE3055 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE3403 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE3397 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE3405 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE3398 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE3401 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE3295 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE3405_1 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE3398_1 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE3401_1 = LecturaSegmento[15];
            }
            if ((LecturaSegmento.Length > 16))
            {
                DE3295_1 = LecturaSegmento[16];
            }
            if ((LecturaSegmento.Length > 17))
            {
                DE3405_2 = LecturaSegmento[17];
            }
            if ((LecturaSegmento.Length > 18))
            {
                DE3398_2 = LecturaSegmento[18];
            }
            if ((LecturaSegmento.Length > 19))
            {
                DE3401_2 = LecturaSegmento[19];
            }
            if ((LecturaSegmento.Length > 20))
            {
                DE3295_2 = LecturaSegmento[20];
            }
            if ((LecturaSegmento.Length > 21))
            {
                DE3405_3 = LecturaSegmento[21];
            }
            if ((LecturaSegmento.Length > 22))
            {
                DE3398_3 = LecturaSegmento[22];
            }
            if ((LecturaSegmento.Length > 23))
            {
                DE3401_3 = LecturaSegmento[23];
            }
            if ((LecturaSegmento.Length > 24))
            {
                DE3295_3 = LecturaSegmento[24];
            }
            if ((LecturaSegmento.Length > 25))
            {
                DE3405_4 = LecturaSegmento[25];
            }
            if ((LecturaSegmento.Length > 26))
            {
                DE3398_4 = LecturaSegmento[26];
            }
            if ((LecturaSegmento.Length > 27))
            {
                DE3401_4 = LecturaSegmento[27];
            }
            if ((LecturaSegmento.Length > 28))
            {
                DE3295_4 = LecturaSegmento[28];
            }
        }

        public void SegmentPRCWrite(string DE7187, string DE1131, string DE3055, string DE7186, string DE7186_1)
        {
            string Segmento;
            if ((DE7187 != ""))
            {
                Segmento = ("PRC+"
                            + (DEComp(TratarTexto(DE7187, 17), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE7186, 35), TratarTexto(DE7186_1, 35)) + "\'"));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentPRCRead(string Segment, string Delimiter, string DE7187, string DE1131, string DE3055, string DE7186, string DE7186_1)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7187 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE7186 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE7186_1 = LecturaSegmento[4];
            }
        }

        public void SegmentPRIWrite(string DE5125, string DE5118, string DE5375, string DE5387, string DE5284, string DE6411, string DE5213)
        {
            string Segmento;
            Segmento = ("PRI+"
                        + (DEComp(TratarTexto(DE5125, 3), DE5118, TratarTexto(DE5375, 3), TratarTexto(DE5387, 3), DE5284, TratarTexto(DE6411, 3))
                        + (DESimp(TratarTexto(DE5213, 3)) + "\'")));
            EscribeSegmento(Segmento);
        }

        public void SegmentPRIRead(string Segment, string Delimiter, string DE5125, string DE5118, string DE5375, string DE5387, string DE5284, string DE6411, string DE5213)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE5125 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE5118 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE5375 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE5387 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE5284 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE6411 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE5213 = LecturaSegmento[6];
            }
        }

        public void SegmentPSDWrite(string DE4407, string DE7039, string DE6071, string DE6072, string DE6411, string DE7045, string DE7047, string DE3237, string DE3236, string DE3237_1, string DE3236_1, string DE3237_2, string DE3236_2)
        {
            string Segmento;
            Segmento = ("PSD+"
                        + (DESimp(TratarTexto(DE4407, 3))
                        + (DESimp(TratarTexto(DE7039, 3))
                        + (DEComp(TratarTexto(DE6071, 3), DE6072, TratarTexto(DE6411, 3))
                        + (DESimp(TratarTexto(DE7045, 3))
                        + (DESimp(TratarTexto(DE7047, 3))
                        + (DEComp(TratarTexto(DE3237, 3), TratarTexto(DE3236, 35))
                        + (DEComp(TratarTexto(DE3237_1, 3), TratarTexto(DE3236_1, 35))
                        + (DEComp(TratarTexto(DE3237_2, 3), TratarTexto(DE3236_2, 35)) + "\'")))))))));
            EscribeSegmento(Segmento);
        }

        public void SegmentPSDRead(string Segment, string Delimiter, string DE4407, string DE7039, string DE6071, string DE6072, string DE6411, string DE7045, string DE7047, string DE3237, string DE3236, string DE3237_1, string DE3236_1, string DE3237_2, string DE3236_2)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4407 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7039 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE6071 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE6072 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE6411 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE7045 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE7047 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE3237 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE3236 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE3237_1 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE3236_1 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE3237_2 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE3236_2 = LecturaSegmento[12];
            }
        }

        public void SegmentPTYWrite(string DE4035, string DE4037, string DE1131, string DE3055, string DE4036)
        {
            string Segmento;
            if ((DE4035 != ""))
            {
                Segmento = ("PTY+"
                            + (DESimp(TratarTexto(DE4035, 3))
                            + (DEComp(TratarTexto(DE4037, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE4036, 35)) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentPTYRead(string Segment, string Delimiter, string DE4035, string DE4037, string DE1131, string DE3055, string DE4036)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4035 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE4037 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE4036 = LecturaSegmento[4];
            }
        }

        public void SegmentQTYWrite(string DE6063, string DE6060, string DE6411)
        {
            string Segmento;
            if (((DE6063 != "")
                        && (DE6060 != "")))
            {
                Segmento = ("QTY+"
                            + (DEComp(TratarTexto(DE6063, 3), DE6060, TratarTexto(DE6411, 3)) + "\'"));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentQTYRead(string Segment, string Delimiter, ref string DE6063, ref string DE6060, ref string DE6411)
        {
            string[] LecturaSegmento;
            Segment = Segment.Substring(Segment.IndexOf("QTY"), Segment.Length - Segment.IndexOf("QTY"));
            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE6063 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE6060 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE6411 = LecturaSegmento[2];
            }
        }

        public void SegmentQVRWrite(string DE6064, string DE6063, string DE4221, string DE4295, string DE1131, string DE3055, string DE4294)
        {
            string Segmento;
            Segmento = ("QVR+"
                        + (DEComp(DE6064, TratarTexto(DE6063, 3))
                        + (DESimp(TratarTexto(DE4221, 3))
                        + (DEComp(TratarTexto(DE4295, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE4294, 35)) + "\'"))));
            EscribeSegmento(Segmento);
        }

        public void SegmentQVRRead(string Segment, string Delimiter, string DE6064, string DE6063, string DE4221, string DE4295, string DE1131, string DE3055, string DE4294)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE6064 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE6063 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE4221 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE4295 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1131 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE3055 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4294 = LecturaSegmento[6];
            }
        }

        public void SegmentRCSWrite(string DE7293, string DE7295, string DE1131, string DE3055, string DE7294, string DE1229)
        {
            string Segmento;
            if ((DE7293 != ""))
            {
                Segmento = ("RCS+"
                            + (DESimp(TratarTexto(DE7293, 3))
                            + (DEComp(TratarTexto(DE7295, 17), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE7294, 35))
                            + (DESimp(TratarTexto(DE1229, 3)) + "\'"))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentRCSRead(string Segment, string Delimiter, string DE7293, string DE7295, string DE1131, string DE3055, string DE7294, string DE1229)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7293 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7295 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE7294 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE1229 = LecturaSegmento[5];
            }
        }

        public void SegmentRELWrite(string DE9141, string DE9143, string DE1131, string DE3055, string DE9142)
        {
            string Segmento;
            if ((DE9141 != ""))
            {
                Segmento = ("REL+"
                            + (DESimp(TratarTexto(DE9141, 3))
                            + (DEComp(TratarTexto(DE9143, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE9142, 35)) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentRELRead(string Segment, string Delimiter, string DE9141, string DE9143, string DE1131, string DE3055, string DE9142)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9141 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE9143 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE9142 = LecturaSegmento[4];
            }
        }

        public void SegmentRFFWrite(string DE1153, string DE1154, string DE1156, string DE4000)
        {
            string Segmento;
            if ((DE1153 != ""))
            {
                Segmento = ("RFF+"
                            + (DEComp(TratarTexto(DE1153, 3), TratarTexto(DE1154, 35), TratarTexto(DE1156, 6), TratarTexto(DE4000, 35)) + "\'"));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentRFFRead(string Segment, string Delimiter, string DE1153, string DE1154, string DE1156, string DE4000)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE1153 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1154 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1156 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE4000 = LecturaSegmento[3];
            }
        }

        public void SegmentRNGWrite(string DE6167, string DE6411, string DE6162, string DE6152)
        {
            string Segmento;
            if ((DE6167 != ""))
            {
                Segmento = ("RNG+"
                            + (DESimp(TratarTexto(DE6167, 3))
                            + (DEComp(TratarTexto(DE6411, 3), DE6162, DE6152) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentRNGRead(string Segment, string Delimiter, string DE6167, string DE6411, string DE6162, string DE6152)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE6167 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE6411 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE6162 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE6152 = LecturaSegmento[3];
            }
        }

        public void SegmentRTEWrite(string DE5419, string DE5420, string DE5284, string DE6411)
        {
            string Segmento;
            if (((DE5419 != "")
                        && (DE5420 != "")))
            {
                Segmento = ("RTE+"
                            + (DEComp(TratarTexto(DE5419, 3), DE5420, DE5284, TratarTexto(DE6411, 3)) + "\'"));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentRTERead(string Segment, string Delimiter, string DE5419, string DE5420, string DE5284, string DE6411)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE5419 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE5420 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE5284 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE6411 = LecturaSegmento[3];
            }
        }

        public void SegmentSALWrite(string DE5315, string DE1131, string DE3055, string DE5314, string DE5314_1)
        {
            string Segmento;
            Segmento = ("SAL+"
                        + (DEComp(TratarTexto(DE5315, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE5314, 35), TratarTexto(DE5314_1, 35)) + "\'"));
            EscribeSegmento(Segmento);
        }

        public void SegmentSALRead(string Segment, string Delimiter, string DE5315, string DE1131, string DE3055, string DE5314, string DE5314_1)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE5315 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE5314 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE5314_1 = LecturaSegmento[4];
            }
        }

        public void SegmentSCCWrite(string DE4017, string DE4493, string DE2013, string DE2015, string DE2017)
        {
            string Segmento;
            if ((DE4017 != ""))
            {
                Segmento = ("SCC+"
                            + (DESimp(TratarTexto(DE4017, 3))
                            + (DESimp(TratarTexto(DE4493, 3))
                            + (DEComp(TratarTexto(DE2013, 3), TratarTexto(DE2015, 3), TratarTexto(DE2017, 3)) + "\'"))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentSCCRead(string Segment, string Delimiter, string DE4017, string DE4493, string DE2013, string DE2015, string DE2017)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4017 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE4493 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE2013 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE2015 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE2017 = LecturaSegmento[4];
            }
        }

        public void SegmentSCDWrite(string DE7497, string DE7512, string DE7405, string DE3039, string DE1131, string DE3055, string DE4405, string DE1222, string DE7164, string DE1050, string DE7037, string DE1131_1, string DE3055_1, string DE7036, string DE7036_1)
        {
            string Segmento;
            if ((DE7497 != ""))
            {
                Segmento = ("SCD+"
                            + (DESimp(TratarTexto(DE7497, 3))
                            + (DEComp(TratarTexto(DE7512, 35), TratarTexto(DE7405, 3))
                            + (DEComp(TratarTexto(DE3039, 35), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                            + (DESimp(TratarTexto(DE4405, 3))
                            + (DESimp(DE1222)
                            + (DEComp(TratarTexto(DE7164, 12), TratarTexto(DE1050, 6))
                            + (DEComp(TratarTexto(DE7037, 17), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE7036, 35), TratarTexto(DE7036_1, 35)) + "\'"))))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentSCDRead(
                    string Segment,
                    string Delimiter,
                    string DE7497,
                    string DE7512,
                    string DE7405,
                    string DE3039,
                    string DE1131,
                    string DE3055,
                    string DE4405,
                    string DE1222,
                    string DE7164,
                    string DE1050,
                    string DE7037,
                    string DE1131_1,
                    string DE3055_1,
                    string DE7036,
                    string DE7036_1)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7497 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7512 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE7405 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3039 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1131 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE3055 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4405 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE1222 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE7164 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE1050 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE7037 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE1131_1 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE3055_1 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE7036 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE7036_1 = LecturaSegmento[14];
            }
        }

        public void SegmentSEGWrite(string DE9166, string DE1507, string DE4513)
        {
            string Segmento;
            if ((DE9166 != ""))
            {
                Segmento = ("SEG+"
                            + (DESimp(TratarTexto(DE9166, 3))
                            + (DESimp(TratarTexto(DE1507, 3))
                            + (DESimp(TratarTexto(DE4513, 3)) + "\'"))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentSEGRead(string Segment, string Delimiter, string DE9166, string DE1507, string DE4513)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9166 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1507 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE4513 = LecturaSegmento[2];
            }
        }

        public void SegmentSELWrite(string DE9308, string DE9303, string DE1131, string DE3055, string DE9302, string DE4517)
        {
            string Segmento;
            if ((DE9308 != ""))
            {
                Segmento = ("SEL+"
                            + (DESimp(TratarTexto(DE9308, 10))
                            + (DEComp(TratarTexto(DE9303, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE9302, 35))
                            + (DESimp(TratarTexto(DE4517, 3)) + "\'"))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentSELRead(string Segment, string Delimiter, string DE9308, string DE9303, string DE1131, string DE3055, string DE9302, string DE4517)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9308 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE9303 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE9302 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE4517 = LecturaSegmento[5];
            }
        }

        public void SegmentSEQWrite(string DE1245, string DE1050, string DE1159, string DE1131, string DE3055)
        {
            string Segmento;
            Segmento = ("SEQ+"
                        + (DESimp(TratarTexto(DE1245, 3))
                        + (DEComp(TratarTexto(DE1050, 6), TratarTexto(DE1159, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3)) + "\'")));
            EscribeSegmento(Segmento);
        }

        public void SegmentSEQRead(string Segment, string Delimiter, string DE1245, string DE1050, string DE1159, string DE1131, string DE3055)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE1245 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1050 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1159 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1131 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3055 = LecturaSegmento[4];
            }
        }

        public void SegmentSFIWrite(string DE7164, string DE4046, string DE4044, string DE4039, string DE1131, string DE3055, string DE4038, string DE4513)
        {
            string Segmento;
            if ((DE7164 != ""))
            {
                Segmento = ("SFI+"
                            + (DESimp(TratarTexto(DE7164, 12))
                            + (DEComp(DE4046, TratarTexto(DE4044, 70))
                            + (DEComp(TratarTexto(DE4039, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE4038, 35))
                            + (DESimp(TratarTexto(DE4513, 3)) + "\'")))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentSFIRead(string Segment, string Delimiter, string DE7164, string DE4046, string DE4044, string DE4039, string DE1131, string DE3055, string DE4038, string DE4513)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE7164 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE4046 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE4044 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE4039 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1131 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE3055 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4038 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE4513 = LecturaSegmento[7];
            }
        }

        public void SegmentSGPWrite(string DE8260, string DE1131, string DE3055, string DE3207, string DE7224)
        {
            string Segmento;
            Segmento = ("SGP+"
                        + (DEComp(TratarTexto(DE8260, 17), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE3207, 3))
                        + (DESimp(DE7224) + "\'")));
            EscribeSegmento(Segmento);
        }

        public void SegmentSGPRead(string Segment, string Delimiter, string DE8260, string DE1131, string DE3055, string DE3207, string DE7224)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE8260 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3207 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE7224 = LecturaSegmento[4];
            }
        }

        public void SegmentSGUWrite(string DE9166, string DE7299, string DE6176, string DE7168, string DE1050, string DE1049, string DE4513)
        {
            string Segmento;
            if ((DE9166 != ""))
            {
                Segmento = ("SGU+"
                            + (DESimp(TratarTexto(DE9166, 3))
                            + (DESimp(TratarTexto(DE7299, 3))
                            + (DESimp(DE6176)
                            + (DESimp(DE7168)
                            + (DESimp(TratarTexto(DE1050, 6))
                            + (DESimp(TratarTexto(DE1049, 3))
                            + (DESimp(TratarTexto(DE4513, 3)) + "\'"))))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentSGURead(string Segment, string Delimiter, string DE9166, string DE7299, string DE6176, string DE7168, string DE1050, string DE1049, string DE4513)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9166 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7299 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE6176 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE7168 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1050 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE1049 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4513 = LecturaSegmento[6];
            }
        }

        public void SegmentSPSWrite(string DE6071, string DE6072, string DE6411, string DE6074, string DE6173, string DE6174, string DE6173_1, string DE6174_1, string DE6173_2, string DE6174_2, string DE6173_3, string DE6174_3, string DE6173_4, string DE6174_4)
        {
            string Segmento;
            Segmento = ("SPS+"
                        + (DEComp(TratarTexto(DE6071, 3), DE6072, TratarTexto(DE6411, 3))
                        + (DESimp(DE6074)
                        + (DEComp(TratarTexto(DE6173, 3), DE6174)
                        + (DEComp(TratarTexto(DE6173_1, 3), DE6174_1)
                        + (DEComp(TratarTexto(DE6173_2, 3), DE6174_2)
                        + (DEComp(TratarTexto(DE6173_3, 3), DE6174_3)
                        + (DEComp(TratarTexto(DE6173_4, 3), DE6174_4) + "\'"))))))));
            EscribeSegmento(Segmento);
        }

        public void SegmentSPSRead(
                    string Segment,
                    string Delimiter,
                    string DE6071,
                    string DE6072,
                    string DE6411,
                    string DE6074,
                    string DE6173,
                    string DE6174,
                    string DE6173_1,
                    string DE6174_1,
                    string DE6173_2,
                    string DE6174_2,
                    string DE6173_3,
                    string DE6174_3,
                    string DE6173_4,
                    string DE6174_4)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE6071 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE6072 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE6411 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE6074 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE6173 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE6174 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE6173_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE6174_1 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE6173_2 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE6174_2 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE6173_3 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE6174_3 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE6173_4 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE6174_4 = LecturaSegmento[13];
            }
        }

        public void SegmentSTAWrite(string DE6331, string DE6314, string DE6411, string DE6313, string DE6321)
        {
            string Segmento;
            if ((DE6331 != ""))
            {
                Segmento = ("STA+"
                            + (DESimp(TratarTexto(DE6331, 3))
                            + (DEComp(DE6314, TratarTexto(DE6411, 3), TratarTexto(DE6313, 3), TratarTexto(DE6321, 3)) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentSTARead(string Segment, string Delimiter, string DE6331, string DE6314, string DE6411, string DE6313, string DE6321)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE6331 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE6314 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE6411 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE6313 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE6321 = LecturaSegmento[4];
            }
        }

        public void SegmentSTCWrite(string DE6434, string DE7405, string DE3039, string DE1131, string DE3055, string DE4405, string DE4513)
        {
            string Segmento;
            if ((DE6434 != ""))
            {
                Segmento = ("STC+"
                            + (DEComp(TratarTexto(DE6434, 35), TratarTexto(DE7405, 3))
                            + (DEComp(TratarTexto(DE3039, 35), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                            + (DESimp(TratarTexto(DE4405, 3))
                            + (DESimp(TratarTexto(DE4513, 3)) + "\'")))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentSTCRead(string Segment, string Delimiter, string DE6434, string DE7405, string DE3039, string DE1131, string DE3055, string DE4405, string DE4513)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE6434 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7405 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3039 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1131 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3055 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE4405 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4513 = LecturaSegmento[6];
            }
        }

        public void SegmentSTGWrite(string DE9421, string DE6426, string DE6428)
        {
            string Segmento;
            if ((DE9421 != ""))
            {
                Segmento = ("STG+"
                            + (DESimp(TratarTexto(DE9421, 3))
                            + (DESimp(DE6426)
                            + (DESimp(DE6428) + "\'"))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentSTGRead(string Segment, string Delimiter, string DE9421, string DE6426, string DE6428)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9421 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE6426 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE6428 = LecturaSegmento[2];
            }
        }

        public void SegmentSTSWrite(
                    string DE9015,
                    string DE1131,
                    string DE3055,
                    string DE9011,
                    string DE1131_1,
                    string DE3055_1,
                    string DE9010,
                    string DE9013,
                    string DE1131_2,
                    string DE3055_2,
                    string DE9012,
                    string DE9013_1,
                    string DE1131_3,
                    string DE3055_3,
                    string DE9012_1,
                    string DE9013_2,
                    string DE1131_4,
                    string DE3055_4,
                    string DE9012_2,
                    string DE9013_3,
                    string DE1131_5,
                    string DE3055_5,
                    string DE9012_3,
                    string DE9013_4,
                    string DE1131_6,
                    string DE3055_6,
                    string DE9012_4)
        {
            string Segmento;
            Segmento = ("STS+"
                        + (DEComp(TratarTexto(DE9015, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                        + (DEComp(TratarTexto(DE9011, 3), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE9010, 35))
                        + (DEComp(TratarTexto(DE9013, 3), TratarTexto(DE1131_2, 3), TratarTexto(DE3055_2, 3), TratarTexto(DE9012, 35))
                        + (DEComp(TratarTexto(DE9013_1, 3), TratarTexto(DE1131_3, 3), TratarTexto(DE3055_3, 3), TratarTexto(DE9012_1, 35))
                        + (DEComp(TratarTexto(DE9013_2, 3), TratarTexto(DE1131_4, 3), TratarTexto(DE3055_4, 3), TratarTexto(DE9012_2, 35))
                        + (DEComp(TratarTexto(DE9013_3, 3), TratarTexto(DE1131_5, 3), TratarTexto(DE3055_5, 3), TratarTexto(DE9012_3, 35))
                        + (DEComp(TratarTexto(DE9013_4, 3), TratarTexto(DE1131_6, 3), TratarTexto(DE3055_6, 3), TratarTexto(DE9012_4, 35)) + "\'"))))))));
            EscribeSegmento(Segmento);
        }

        public void SegmentSTSRead(
                    string Segment,
                    string Delimiter,
                    string DE9015,
                    string DE1131,
                    string DE3055,
                    string DE9011,
                    string DE1131_1,
                    string DE3055_1,
                    string DE9010,
                    string DE9013,
                    string DE1131_2,
                    string DE3055_2,
                    string DE9012,
                    string DE9013_1,
                    string DE1131_3,
                    string DE3055_3,
                    string DE9012_1,
                    string DE9013_2,
                    string DE1131_4,
                    string DE3055_4,
                    string DE9012_2,
                    string DE9013_3,
                    string DE1131_5,
                    string DE3055_5,
                    string DE9012_3,
                    string DE9013_4,
                    string DE1131_6,
                    string DE3055_6,
                    string DE9012_4)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE9015 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE9011 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1131_1 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE3055_1 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE9010 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE9013 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE1131_2 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE3055_2 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE9012 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE9013_1 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE1131_3 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE3055_3 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE9012_1 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE9013_2 = LecturaSegmento[15];
            }
            if ((LecturaSegmento.Length > 16))
            {
                DE1131_4 = LecturaSegmento[16];
            }
            if ((LecturaSegmento.Length > 17))
            {
                DE3055_4 = LecturaSegmento[17];
            }
            if ((LecturaSegmento.Length > 18))
            {
                DE9012_2 = LecturaSegmento[18];
            }
            if ((LecturaSegmento.Length > 19))
            {
                DE9013_3 = LecturaSegmento[19];
            }
            if ((LecturaSegmento.Length > 20))
            {
                DE1131_5 = LecturaSegmento[20];
            }
            if ((LecturaSegmento.Length > 21))
            {
                DE3055_5 = LecturaSegmento[21];
            }
            if ((LecturaSegmento.Length > 22))
            {
                DE9012_3 = LecturaSegmento[22];
            }
            if ((LecturaSegmento.Length > 23))
            {
                DE9013_4 = LecturaSegmento[23];
            }
            if ((LecturaSegmento.Length > 24))
            {
                DE1131_6 = LecturaSegmento[24];
            }
            if ((LecturaSegmento.Length > 25))
            {
                DE3055_6 = LecturaSegmento[25];
            }
            if ((LecturaSegmento.Length > 26))
            {
                DE9012_4 = LecturaSegmento[26];
            }
        }

        public void SegmentTAXWrite(
                    string DE5283,
                    string DE5153,
                    string DE1131,
                    string DE3055,
                    string DE5152,
                    string DE5289,
                    string DE1131_1,
                    string DE3055_1,
                    string DE5286,
                    string DE5279,
                    string DE1131_2,
                    string DE3055_2,
                    string DE5278,
                    string DE5273,
                    string DE1131_3,
                    string DE3055_3,
                    string DE5305,
                    string DE3446)
        {
            string Segmento;
            if ((DE5283 != ""))
            {
                Segmento = ("TAX+"
                            + (DESimp(TratarTexto(DE5283, 3))
                            + (DEComp(TratarTexto(DE5153, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE5152, 35))
                            + (DEComp(TratarTexto(DE5289, 6), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3))
                            + (DESimp(TratarTexto(DE5286, 15))
                            + (DEComp(TratarTexto(DE5279, 7), TratarTexto(DE1131_2, 3), TratarTexto(DE3055_2, 3), TratarTexto(DE5278, 17), TratarTexto(DE5273, 12), TratarTexto(DE1131_3, 3), TratarTexto(DE3055_3, 3))
                            + (DESimp(TratarTexto(DE5305, 3))
                            + (DESimp(TratarTexto(DE3446, 20)) + "\'"))))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentTAXRead(
                    string Segment,
                    string Delimiter,
                    string DE5283,
                    string DE5153,
                    string DE1131,
                    string DE3055,
                    string DE5152,
                    string DE5289,
                    string DE1131_1,
                    string DE3055_1,
                    string DE5286,
                    string DE5279,
                    string DE1131_2,
                    string DE3055_2,
                    string DE5278,
                    string DE5273,
                    string DE1131_3,
                    string DE3055_3,
                    string DE5305,
                    string DE3446)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE5283 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE5153 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE1131 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE3055 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE5152 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE5289 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE1131_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE3055_1 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE5286 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE5279 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE1131_2 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE3055_2 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE5278 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE5273 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE1131_3 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE3055_3 = LecturaSegmento[15];
            }
            if ((LecturaSegmento.Length > 16))
            {
                DE5305 = LecturaSegmento[16];
            }
            if ((LecturaSegmento.Length > 17))
            {
                DE3446 = LecturaSegmento[17];
            }
        }

        public void SegmentTCCWrite(
                    string DE8023,
                    string DE1131,
                    string DE3055,
                    string DE8022,
                    string DE4237,
                    string DE7140,
                    string DE5243,
                    string DE1131_1,
                    string DE3055_1,
                    string DE5242,
                    string DE5275,
                    string DE1131_2,
                    string DE3055_2,
                    string DE5275_1,
                    string DE1131_3,
                    string DE3055_3,
                    string DE7357,
                    string DE1131_4,
                    string DE3055_4,
                    string DE5243_1,
                    string DE1131_5,
                    string DE3055_5)
        {
            string Segmento;
            Segmento = ("TCC+"
                        + (DEComp(TratarTexto(DE8023, 17), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE8022, 26), TratarTexto(DE4237, 3), TratarTexto(DE7140, 35))
                        + (DEComp(TratarTexto(DE5243, 9), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE5242, 35), TratarTexto(DE5275, 6), TratarTexto(DE1131_2, 3), TratarTexto(DE3055_2, 3), TratarTexto(DE5275_1, 6), TratarTexto(DE1131_3, 3), TratarTexto(DE3055_3, 3))
                        + (DEComp(TratarTexto(DE7357, 18), TratarTexto(DE1131_4, 3), TratarTexto(DE3055_4, 3))
                        + (DEComp(TratarTexto(DE5243_1, 9), TratarTexto(DE1131_5, 3), TratarTexto(DE3055_5, 3)) + "\'")))));
            EscribeSegmento(Segmento);
        }

        public void SegmentTCCRead(
                    string Segment,
                    string Delimiter,
                    string DE8023,
                    string DE1131,
                    string DE3055,
                    string DE8022,
                    string DE4237,
                    string DE7140,
                    string DE5243,
                    string DE1131_1,
                    string DE3055_1,
                    string DE5242,
                    string DE5275,
                    string DE1131_2,
                    string DE3055_2,
                    string DE5275_1,
                    string DE1131_3,
                    string DE3055_3,
                    string DE7357,
                    string DE1131_4,
                    string DE3055_4,
                    string DE5243_1,
                    string DE1131_5,
                    string DE3055_5)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE8023 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE8022 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE4237 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE7140 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE5243 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE1131_1 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE3055_1 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE5242 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE5275 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE1131_2 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE3055_2 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE5275_1 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE1131_3 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE3055_3 = LecturaSegmento[15];
            }
            if ((LecturaSegmento.Length > 16))
            {
                DE7357 = LecturaSegmento[16];
            }
            if ((LecturaSegmento.Length > 17))
            {
                DE1131_4 = LecturaSegmento[17];
            }
            if ((LecturaSegmento.Length > 18))
            {
                DE3055_4 = LecturaSegmento[18];
            }
            if ((LecturaSegmento.Length > 19))
            {
                DE5243_1 = LecturaSegmento[19];
            }
            if ((LecturaSegmento.Length > 20))
            {
                DE1131_5 = LecturaSegmento[20];
            }
            if ((LecturaSegmento.Length > 21))
            {
                DE3055_5 = LecturaSegmento[21];
            }
        }

        public void SegmentTDTWrite(
                    string DE8051,
                    string DE8028,
                    string DE8067,
                    string DE8066,
                    string DE8179,
                    string DE8178,
                    string DE3127,
                    string DE1131,
                    string DE3055,
                    string DE3128,
                    string DE8101,
                    string DE8457,
                    string DE8459,
                    string DE7130,
                    string DE8213,
                    string DE1131_1,
                    string DE3055_1,
                    string DE8212,
                    string DE8453,
                    string DE8281)
        {
            string Segmento;
            if ((DE8051 != ""))
            {
                Segmento = ("TDT+"
                            + (DESimp(TratarTexto(DE8051, 3))
                            + (DESimp(TratarTexto(DE8028, 17))
                            + (DEComp(TratarTexto(DE8067, 3), TratarTexto(DE8066, 17))
                            + (DEComp(TratarTexto(DE8179, 8), TratarTexto(DE8178, 17))
                            + (DEComp(TratarTexto(DE3127, 17), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE3128, 35))
                            + (DESimp(TratarTexto(DE8101, 3))
                            + (DEComp(TratarTexto(DE8457, 3), TratarTexto(DE8459, 3), TratarTexto(DE7130, 17))
                            + (DEComp(TratarTexto(DE8213, 9), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE8212, 35), TratarTexto(DE8453, 3))
                            + (DESimp(TratarTexto(DE8281, 3)) + "\'"))))))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentTDTRead(
                    string Segment,
                    string Delimiter,
                    string DE8051,
                    string DE8028,
                    string DE8067,
                    string DE8066,
                    string DE8179,
                    string DE8178,
                    string DE3127,
                    string DE1131,
                    string DE3055,
                    string DE3128,
                    string DE8101,
                    string DE8457,
                    string DE8459,
                    string DE7130,
                    string DE8213,
                    string DE1131_1,
                    string DE3055_1,
                    string DE8212,
                    string DE8453,
                    string DE8281)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE8051 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE8028 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE8067 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE8066 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE8179 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE8178 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE3127 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE1131 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE3055 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE3128 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE8101 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE8457 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE8459 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE7130 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE8213 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE1131_1 = LecturaSegmento[15];
            }
            if ((LecturaSegmento.Length > 16))
            {
                DE3055_1 = LecturaSegmento[16];
            }
            if ((LecturaSegmento.Length > 17))
            {
                DE8212 = LecturaSegmento[17];
            }
            if ((LecturaSegmento.Length > 18))
            {
                DE8453 = LecturaSegmento[18];
            }
            if ((LecturaSegmento.Length > 19))
            {
                DE8281 = LecturaSegmento[19];
            }
        }

        public void SegmentTEMWrite(string DE4415, string DE1131, string DE3055, string DE4416, string DE4419, string DE3077, string DE6311, string DE7188, string DE4425, string DE1131_1, string DE3055_1, string DE4424)
        {
            string Segmento;
            Segmento = ("TEM+"
                        + (DEComp(TratarTexto(DE4415, 17), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE4416, 70))
                        + (DESimp(TratarTexto(DE4419, 3))
                        + (DESimp(TratarTexto(DE3077, 3))
                        + (DESimp(TratarTexto(DE6311, 3))
                        + (DESimp(TratarTexto(DE7188, 30))
                        + (DEComp(TratarTexto(DE4425, 17), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE4424, 35)) + "\'")))))));
            EscribeSegmento(Segmento);
        }

        public void SegmentTEMRead(string Segment, string Delimiter, string DE4415, string DE1131, string DE3055, string DE4416, string DE4419, string DE3077, string DE6311, string DE7188, string DE4425, string DE1131_1, string DE3055_1, string DE4424)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4415 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE4416 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE4419 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE3077 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE6311 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE7188 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE4425 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE1131_1 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE3055_1 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE4424 = LecturaSegmento[11];
            }
        }

        public void SegmentTMDWrite(string DE8335, string DE8334, string DE8332, string DE8341)
        {
            string Segmento;
            Segmento = ("TMD+"
                        + (DEComp(TratarTexto(DE8335, 3), TratarTexto(DE8334, 35))
                        + (DESimp(TratarTexto(DE8332, 26))
                        + (DESimp(TratarTexto(DE8341, 3)) + "\'"))));
            EscribeSegmento(Segmento);
        }

        public void SegmentTMDRead(string Segment, string Delimiter, string DE8335, string DE8334, string DE8332, string DE8341)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE8335 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE8334 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE8332 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE8341 = LecturaSegmento[3];
            }
        }

        public void SegmentTMPWrite(string DE6245, string DE6246, string DE6411)
        {
            string Segmento;
            if ((DE6245 != ""))
            {
                Segmento = ("TMP+"
                            + (DESimp(TratarTexto(DE6245, 3))
                            + (DEComp(DE6246, TratarTexto(DE6411, 3)) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentTMPRead(string Segment, string Delimiter, string DE6245, string DE6246, string DE6411)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE6245 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE6246 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE6411 = LecturaSegmento[2];
            }
        }

        public void SegmentTODWrite(string DE4055, string DE4215, string DE4053, string DE1131, string DE3055, string DE4052, string DE4052_1)
        {
            string Segmento;
            Segmento = ("TOD+"
                        + (DESimp(TratarTexto(DE4055, 3))
                        + (DESimp(TratarTexto(DE4215, 3))
                        + (DEComp(TratarTexto(DE4053, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE4052, 70), TratarTexto(DE4052_1, 70)) + "\'"))));
            EscribeSegmento(Segmento);
        }

        public void SegmentTODRead(string Segment, string Delimiter, string DE4055, string DE4215, string DE4053, string DE1131, string DE3055, string DE4052, string DE4052_1)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4055 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE4215 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE4053 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1131 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3055 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE4052 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE4052_1 = LecturaSegmento[6];
            }
        }

        public void SegmentTPLWrite(string DE8213, string DE1131, string DE3055, string DE8212, string DE8453)
        {
            string Segmento;
            Segmento = ("TPL+"
                        + (DEComp(TratarTexto(DE8213, 9), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3), TratarTexto(DE8212, 35), TratarTexto(DE8453, 3)) + "\'"));
            EscribeSegmento(Segmento);
        }

        public void SegmentTPLRead(string Segment, string Delimiter, string DE8213, string DE1131, string DE3055, string DE8212, string DE8453)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE8213 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE8212 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE8453 = LecturaSegmento[4];
            }
        }

        public void SegmentTSRWrite(string DE4065, string DE1131, string DE3055, string DE7273, string DE1131_1, string DE3055_1, string DE7273_1, string DE1131_2, string DE3055_2, string DE4219, string DE1131_3, string DE3055_3, string DE7085, string DE1131_4, string DE3055_4)
        {
            string Segmento;
            Segmento = ("TSR+"
                        + (DEComp(TratarTexto(DE4065, 3), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                        + (DEComp(TratarTexto(DE7273, 3), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE7273_1, 3), TratarTexto(DE1131_2, 3), TratarTexto(DE3055_2, 3))
                        + (DEComp(TratarTexto(DE4219, 3), TratarTexto(DE1131_3, 3), TratarTexto(DE3055_3, 3))
                        + (DEComp(TratarTexto(DE7085, 3), TratarTexto(DE1131_4, 3), TratarTexto(DE3055_4, 3)) + "\'")))));
            EscribeSegmento(Segmento);
        }

        public void SegmentTSRRead(
                    string Segment,
                    string Delimiter,
                    string DE4065,
                    string DE1131,
                    string DE3055,
                    string DE7273,
                    string DE1131_1,
                    string DE3055_1,
                    string DE7273_1,
                    string DE1131_2,
                    string DE3055_2,
                    string DE4219,
                    string DE1131_3,
                    string DE3055_3,
                    string DE7085,
                    string DE1131_4,
                    string DE3055_4)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE4065 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE1131 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3055 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE7273 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE1131_1 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE3055_1 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE7273_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE1131_2 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE3055_2 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE4219 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE1131_3 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE3055_3 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE7085 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE1131_4 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE3055_4 = LecturaSegmento[14];
            }
        }

        public void SegmentUNAWrite(string DEUNA1, string DEUNA2, string DEUNA3, string DEUNA4, string DEUNA5, string DEUNA6)
        {
            string Segmento;
            if (((DEUNA1 != "")
                        && ((DEUNA2 != "")
                        && ((DEUNA3 != "")
                        && ((DEUNA4 != "")
                        && ((DEUNA5 != "")
                        && (DEUNA6 != "")))))))
            {
                Segmento = ("UNA+"
                            + (DESimp(TratarTexto(DEUNA1, 1))
                            + (DESimp(TratarTexto(DEUNA2, 1))
                            + (DESimp(TratarTexto(DEUNA3, 1))
                            + (DESimp(TratarTexto(DEUNA4, 1))
                            + (DESimp(TratarTexto(DEUNA5, 1))
                            + (DESimp(TratarTexto(DEUNA6, 1)) + "\'")))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentUNARead(string Segment, string Delimiter, string DEUNA1, string DEUNA2, string DEUNA3, string DEUNA4, string DEUNA5, string DEUNA6)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DEUNA1 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DEUNA2 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DEUNA3 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DEUNA4 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DEUNA5 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DEUNA6 = LecturaSegmento[5];
            }
        }

        public void SegmentUNBWrite(
                    string DE0001,
                    string DE0002,
                    string DE0004,
                    string DE0007,
                    string DE0008,
                    string DE0010,
                    string DE0007_1,
                    string DE0014,
                    string DE0017,
                    string DE0019,
                    string DE0020,
                    string DE0022,
                    string DE0025,
                    string DE0026,
                    string DE0029,
                    string DE0031,
                    string DE0032,
                    string DE0035)
        {
            string Segmento;
            if (((DE0001 != "")
                        && ((DE0002 != "")
                        && ((DE0004 != "")
                        && ((DE0010 != "")
                        && ((DE0017 != "")
                        && ((DE0019 != "")
                        && (DE0020 != ""))))))))
            {
                Segmento = ("UNB+"
                            + (DEComp(TratarTexto(DE0001, 4), DE0002)
                            + (DEComp(TratarTexto(DE0004, 35), TratarTexto(DE0007, 4), TratarTexto(DE0008, 14))
                            + (DEComp(TratarTexto(DE0010, 35), TratarTexto(DE0007_1, 4), TratarTexto(DE0014, 14))
                            + (DEComp(DE0017, DE0019)
                            + (DESimp(TratarTexto(DE0020, 14))
                            + (DEComp(TratarTexto(DE0022, 14), TratarTexto(DE0025, 2))
                            + (DESimp(TratarTexto(DE0026, 14))
                            + (DESimp(TratarTexto(DE0029, 1))
                            + (DESimp(DE0031)
                            + (DESimp(TratarTexto(DE0032, 35))
                            + (DESimp(DE0035) + "\'"))))))))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentUNBRead(
                    string Segment,
                    string Delimiter,
                    ref string DE0001,
                    ref string DE0002,
                    ref string DE0004,
                    ref string DE0007,
                    ref string DE0008,
                    ref string DE0010,
                    ref string DE0007_1,
                    ref string DE0014,
                    ref string DE0017,
                    ref string DE0019,
                    ref string DE0020,
                    ref string DE0022,
                    ref string DE0025,
                    ref string DE0026,
                    ref string DE0029,
                    ref string DE0031,
                    ref string DE0032,
                    ref string DE0035)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE0001 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE0002 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE0004 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE0007 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE0008 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE0010 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE0007_1 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE0014 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE0017 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE0019 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE0020 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE0022 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE0025 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE0026 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE0029 = LecturaSegmento[14];
            }
            if ((LecturaSegmento.Length > 15))
            {
                DE0031 = LecturaSegmento[15];
            }
            if ((LecturaSegmento.Length > 16))
            {
                DE0032 = LecturaSegmento[16];
            }
            if ((LecturaSegmento.Length > 17))
            {
                DE0035 = LecturaSegmento[17];
            }
        }

        public void SegmentUNEWrite(string DE0060, string DE0048)
        {
            string Segmento;
            if (((DE0060 != "")
                        && (DE0048 != "")))
            {
                Segmento = ("UNE+"
                            + (DESimp(DE0060)
                            + (DESimp(TratarTexto(DE0048, 14)) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentUNERead(string Segment, string Delimiter, string DE0060, string DE0048)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE0060 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE0048 = LecturaSegmento[1];
            }
        }

        public void SegmentUNGWrite(string DE0038, string DE0040, string DE0007, string DE0044, string DE0007_1, string DE0017, string DE0019, string DE0048, string DE0051, string DE0052, string DE0054, string DE0057, string DE0058)
        {
            string Segmento;
            if (((DE0038 != "")
                        && ((DE0040 != "")
                        && ((DE0044 != "")
                        && ((DE0017 != "")
                        && ((DE0019 != "")
                        && ((DE0048 != "")
                        && ((DE0051 != "")
                        && ((DE0052 != "")
                        && (DE0054 != ""))))))))))
            {
                Segmento = ("UNG+"
                            + (DESimp(TratarTexto(DE0038, 6))
                            + (DEComp(TratarTexto(DE0040, 35), TratarTexto(DE0007, 4))
                            + (DEComp(TratarTexto(DE0044, 35), TratarTexto(DE0007_1, 4))
                            + (DEComp(DE0017, DE0019)
                            + (DESimp(TratarTexto(DE0048, 14))
                            + (DESimp(TratarTexto(DE0051, 2))
                            + (DEComp(TratarTexto(DE0052, 3), TratarTexto(DE0054, 3), TratarTexto(DE0057, 6))
                            + (DESimp(TratarTexto(DE0058, 14)) + "\'")))))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentUNGRead(string Segment, string Delimiter, string DE0038, string DE0040, string DE0007, string DE0044, string DE0007_1, string DE0017, string DE0019, string DE0048, string DE0051, string DE0052, string DE0054, string DE0057, string DE0058)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE0038 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE0040 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE0007 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE0044 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE0007_1 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE0017 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE0019 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE0048 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE0051 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE0052 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE0054 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE0057 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE0058 = LecturaSegmento[12];
            }
        }

        public void SegmentUNHWrite(string DE0062, string DE0065, string DE0052, string DE0054, string DE0051, string DE0057, string DE0068, string DE0070, string DE0073)
        {
            string Segmento;
            if (((DE0062 != "")
                        && ((DE0065 != "")
                        && ((DE0052 != "")
                        && ((DE0054 != "")
                        && (DE0051 != ""))))))
            {
                Segmento = ("UNH+"
                            + (DESimp(TratarTexto(DE0062, 14))
                            + (DEComp(TratarTexto(DE0065, 6), TratarTexto(DE0052, 3), TratarTexto(DE0054, 3), TratarTexto(DE0051, 2), TratarTexto(DE0057, 6))
                            + (DESimp(TratarTexto(DE0068, 35))
                            + (DEComp(DE0070, TratarTexto(DE0073, 1)) + "\'")))));
                EscribeSegmento(Segmento);
            }
            WriteSegments("1");
        }

        public void SegmentUNHRead(string Segment, string Delimiter, string DE0062, string DE0065, string DE0052, string DE0054, string DE0051, string DE0057, string DE0068, string DE0070, string DE0073)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE0062 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE0065 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE0052 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE0054 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE0051 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE0057 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE0068 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE0070 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE0073 = LecturaSegmento[8];
            }
        }

        public void SegmentUNSWrite(string DE0081)
        {
            string Segmento;
            if ((DE0081 != ""))
            {
                Segmento = ("UNS+"
                            + (DESimp(TratarTexto(DE0081, 1)) + "\'"));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentUNSRead(string Segment, string Delimiter, string DE0081)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE0081 = LecturaSegmento[0];
            }
        }

        public void SegmentUNTWrite(string DE0074, string DE0062)
        {
            string Segmento;
            if (((DE0074 != "")
                        && (DE0062 != "")))
            {
                Segmento = ("UNT+"
                            + (DESimp(DE0074)
                            + (DESimp(TratarTexto(DE0062, 14)) + "\'")));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentUNTRead(string Segment, string Delimiter, string DE0074, string DE0062)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE0074 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE0062 = LecturaSegmento[1];
            }
        }

        public void SegmentUNZWrite(string DE0036, string DE0020)
        {
            string Segmento;
            if (((DE0036 != "")
                        && (DE0020 != "")))
            {
                Segmento = ("UNZ+"
                            + (DESimp(DE0036)
                            + (DESimp(TratarTexto(DE0020, 14)) + "\'")));
                EscribeSegmento(Segmento);
            }
            DeleteSegments();
        }

        public void SegmentUNZRead(string Segment, string Delimiter, string DE0036, string DE0020)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE0036 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE0020 = LecturaSegmento[1];
            }
        }

        public void SegmentVLIWrite(string DE1518, string DE7405, string DE3039, string DE1131, string DE3055, string DE4405, string DE1514, string DE1507, string DE1505, string DE7037, string DE1131_1, string DE3055_1, string DE7036, string DE7036_1, string DE4513)
        {
            string Segmento;
            if ((DE1518 != ""))
            {
                Segmento = ("VLI+"
                            + (DEComp(TratarTexto(DE1518, 35), TratarTexto(DE7405, 3))
                            + (DEComp(TratarTexto(DE3039, 35), TratarTexto(DE1131, 3), TratarTexto(DE3055, 3))
                            + (DESimp(TratarTexto(DE4405, 3))
                            + (DESimp(TratarTexto(DE1514, 70))
                            + (DESimp(TratarTexto(DE1507, 3))
                            + (DESimp(TratarTexto(DE1505, 3))
                            + (DEComp(TratarTexto(DE7037, 17), TratarTexto(DE1131_1, 3), TratarTexto(DE3055_1, 3), TratarTexto(DE7036, 35), TratarTexto(DE7036_1, 35))
                            + (DESimp(TratarTexto(DE4513, 3)) + "\'")))))))));
                EscribeSegmento(Segmento);
            }
        }

        public void SegmentVLIRead(
                    string Segment,
                    string Delimiter,
                    string DE1518,
                    string DE7405,
                    string DE3039,
                    string DE1131,
                    string DE3055,
                    string DE4405,
                    string DE1514,
                    string DE1507,
                    string DE1505,
                    string DE7037,
                    string DE1131_1,
                    string DE3055_1,
                    string DE7036,
                    string DE7036_1,
                    string DE4513)
        {
            string[] LecturaSegmento;

            string SegmentoTemporal = Segment.Substring((Segment.Length - (Segment.Length - 4)));
            LecturaSegmento = SegmentoTemporal.Split(char.Parse(Delimiter));
            if ((LecturaSegmento.Length > 0))
            {
                DE1518 = LecturaSegmento[0];
            }
            if ((LecturaSegmento.Length > 1))
            {
                DE7405 = LecturaSegmento[1];
            }
            if ((LecturaSegmento.Length > 2))
            {
                DE3039 = LecturaSegmento[2];
            }
            if ((LecturaSegmento.Length > 3))
            {
                DE1131 = LecturaSegmento[3];
            }
            if ((LecturaSegmento.Length > 4))
            {
                DE3055 = LecturaSegmento[4];
            }
            if ((LecturaSegmento.Length > 5))
            {
                DE4405 = LecturaSegmento[5];
            }
            if ((LecturaSegmento.Length > 6))
            {
                DE1514 = LecturaSegmento[6];
            }
            if ((LecturaSegmento.Length > 7))
            {
                DE1507 = LecturaSegmento[7];
            }
            if ((LecturaSegmento.Length > 8))
            {
                DE1505 = LecturaSegmento[8];
            }
            if ((LecturaSegmento.Length > 9))
            {
                DE7037 = LecturaSegmento[9];
            }
            if ((LecturaSegmento.Length > 10))
            {
                DE1131_1 = LecturaSegmento[10];
            }
            if ((LecturaSegmento.Length > 11))
            {
                DE3055_1 = LecturaSegmento[11];
            }
            if ((LecturaSegmento.Length > 12))
            {
                DE7036 = LecturaSegmento[12];
            }
            if ((LecturaSegmento.Length > 13))
            {
                DE7036_1 = LecturaSegmento[13];
            }
            if ((LecturaSegmento.Length > 14))
            {
                DE4513 = LecturaSegmento[14];
            }
        }

        private void EscribeSegmento(string Segmento)
        {
            string SegmentoAEscribir;
            SegmentoAEscribir = LimpiarSegmento(Segmento);
            WriteDocument(SegmentoAEscribir);
            WriteSegments((ReadSegments() + 1).ToString());
        }

        private string LimpiarSegmento(string Segmento)
        {
            while ((Segmento.IndexOf(":\'") >= 0))
            {
                Segmento = Segmento.Replace(":\'", "\'");
            }
            while ((Segmento.IndexOf(":+") >= 0))
            {
                Segmento = Segmento.Replace(":+", "+");
            }
            while ((Segmento.IndexOf("+\'") >= 0))
            {
                Segmento = Segmento.Replace("+\'", "\'");
            }
            string LimpiarSegmento = Segmento;
            if ((LimpiarSegmento.Length <= 5))
            {
                LimpiarSegmento = "";
            }
            return LimpiarSegmento;
        }

        public string DEComp(string DE1)
        {
            return DEComp(DE1, "", "", "", "", "", "", "", "", "");
        }

        public string DEComp(string DE1, string DE2)
        {
            return DEComp(DE1, DE2, "", "", "", "", "", "", "", "");
        }

        public string DEComp(string DE1, string DE2, string DE3)
        {
            return DEComp(DE1, DE2, DE3, "", "", "", "", "", "", "");
        }

        public string DEComp(string DE1, string DE2, string DE3, string DE4)
        {
            return DEComp(DE1, DE2, DE3, DE4, "", "", "", "", "", "");
        }

        public string DEComp(string DE1, string DE2, string DE3, string DE4, string DE5)
        {
            return DEComp(DE1, DE2, DE3, DE4, DE5, "", "", "", "", "");
        }

        public string DEComp(string DE1, string DE2, string DE3, string DE4, string DE5, string DE6)
        {
            return DEComp(DE1, DE2, DE3, DE4, DE5, DE6, "", "", "", "");
        }

        public string DEComp(string DE1, string DE2, string DE3, string DE4, string DE5, string DE6, string DE7)
        {
            return DEComp(DE1, DE2, DE3, DE4, DE5, DE6, DE7, "", "", "");
        }

        public string DEComp(string DE1, string DE2, string DE3, string DE4, string DE5, string DE6, string DE7, string DE8)
        {
            return DEComp(DE1, DE2, DE3, DE4, DE5, DE6, DE7, DE8, "", "");
        }

        public string DEComp(string DE1, string DE2, string DE3, string DE4, string DE5, string DE6, string DE7, string DE8, string DE9)
        {
            return DEComp(DE1, DE2, DE3, DE4, DE5, DE6, DE7, DE8, DE9, "");
        }

        public string DEComp(string DE1, string DE2, string DE3, string DE4, string DE5, string DE6, string DE7, string DE8, string DE9, string DE10)
        {
            return (DE1 + (":"
                        + (DE2 + (":"
                        + (DE3 + (":"
                        + (DE4 + (":"
                        + (DE5 + (":"
                        + (DE6 + (":"
                        + (DE7 + (":"
                        + (DE8 + (":"
                        + (DE9 + (":"
                        + (DE10 + "+")))))))))))))))))));
        }

        private string DESimp(string DElement)
        {
            return (DElement + "+");
        }

        private string TratarTexto(string Texto, int Longitud)
        {
            string TratarTexto = "";
            if (Texto.Length > Longitud)
                TratarTexto = Texto.Substring(0, Longitud);
            else
                TratarTexto = Texto;
            if ((JuegoCaracteres == "UNOA"))
            {
                TratarTexto = TratarTexto.ToUpper();
                TratarTexto = TratarTexto.Replace("À", "A");
                TratarTexto = TratarTexto.Replace("Á", "A");
                TratarTexto = TratarTexto.Replace("È", "E");
                TratarTexto = TratarTexto.Replace("É", "E");
                TratarTexto = TratarTexto.Replace("Ì", "I");
                TratarTexto = TratarTexto.Replace("Í", "I");
                TratarTexto = TratarTexto.Replace("Ò", "O");
                TratarTexto = TratarTexto.Replace("Ó", "O");
                TratarTexto = TratarTexto.Replace("Ù", "U");
                TratarTexto = TratarTexto.Replace("Ú", "U");
                TratarTexto = TratarTexto.Replace("Ç", "C");
                TratarTexto = TratarTexto.Replace("Ñ", "N");
            }
            TratarTexto = TratarTexto.Replace("?", "??");
            TratarTexto = TratarTexto.Replace("\'", "?\'");
            TratarTexto = TratarTexto.Replace(":", "?:");
            return TratarTexto.Replace("+", "?+");
        }

        private string TratarNumero(string Numero, int Decimales)
        {
            string TratarNumero = "";
            if ((Numero != ""))
            {
                TratarNumero = Math.Round(double.Parse(Numero), Decimales).ToString().Trim();
            }
            return TratarNumero;
        }
    }
}
