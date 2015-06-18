using System;
using System.IO;
using System.Collections;
using System.Net;
using System.Net.Mail;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Data.Common;
using System.Data;
using OfficeOpenXml;
using Microsoft.JScript;
using Microsoft.JScript.Vsa;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using HtmlAgilityPack;
using System.Linq;
using NCalc;

namespace ConnectaLib
{
    /// <summary>
    /// Clase para realizar la conversión UNIVERSAL.
    /// </summary>
    public class conversorUniversal
    {
        static IEnumerable<string> RunQuery(IEnumerable<string> source, int num1)
        {
            // Split the string and sort on field[num]
            var scoreQuery = from line in source
                             let fields = line.Split('\u2021')
                             orderby fields[num1] ascending
                             select line;
            return scoreQuery;
        }

        static IEnumerable<string> RunQuery(IEnumerable<string> source, int num1, int num2)
        {
            // Split the string and sort on field[num]
            var scoreQuery = from line in source
                             let fields = line.Split('\u2021')
                             orderby fields[num1], fields[num2] ascending
                             select line;
            return scoreQuery;
        }

        static IEnumerable<string> RunQuery(IEnumerable<string> source, int num1, int num2, int num3)
        {
            // Split the string and sort on field[num]
            var scoreQuery = from line in source
                             let fields = line.Split('\u2021')
                             orderby fields[num1], fields[num2], fields[num3] ascending
                             select line;
            return scoreQuery;
        }

        static IEnumerable<string> RunQuery(IEnumerable<string> source, int num1, int num2, int num3, int num4)
        {
            // Split the string and sort on field[num]
            var scoreQuery = from line in source
                             let fields = line.Split('\u2021')
                             orderby fields[num1], fields[num2], fields[num3], fields[num4] ascending
                             select line;
            return scoreQuery;
        }

        private class CustomComparer : IComparer
        {
            Comparer _comparer = new Comparer(System.Globalization.CultureInfo.CurrentCulture);

            public int Compare(object x, object y)
            {
                // Convert string comparisons to int
                return _comparer.Compare(System.Convert.ToInt32(x), System.Convert.ToInt32(y));
            }
        }

        public bool bResult = false;

        private string sSipTypeName = "Conversor.Universal";

        static readonly List<string> _extensionsTXT = new List<string> { ".csv", ".txt", ".blk", ".dat", ".rtf", ".htm", ".html", ".mht", ".mhtml", ".lot", ".seg" };
        static readonly List<string> _extensionsEXCEL = new List<string> { ".xls", ".xlsx" };

        public const string INDICADOR_VALORFIJO = "@VF";
        public const string INDICADOR_POSICIONORIGEN = "@PO";
        public const string INDICADOR_POSICIONDESTINO = "@PD";
        public const string INDICADOR_FORMULA = "@F";
        public const string INDICADOR_PARAM_1 = "@PARAM1";

        public const string CHAR_SUMA = "\u2021";
        public const string CHAR_REST = "\u2021\u2021";
        public const string CHAR_MULT = "\u2021\u2021\u2021";
        public const string CHAR_DIV = "\u2021\u2021\u2021\u2021";

        System.Collections.Hashtable hashRowCounter = new System.Collections.Hashtable(10);
        System.Collections.Hashtable hashValoresAnteriores = new System.Collections.Hashtable(10);

        System.Collections.Hashtable hashTranscribe = new System.Collections.Hashtable(10);
        System.Collections.Hashtable hashFileOut = new System.Collections.Hashtable(10);

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
                //Mirar si existen ficheros que cumplan el nomenclator esperado                                
                DataTable dtFicheros = ObtenerFicherosOrigenConversorUniversal(db, pAgent);

                Hashtable htFileName = new Hashtable();
                Hashtable htFileNameBD = new Hashtable();

                if (System.IO.Directory.Exists(pPath))
                {
                    //Obtener la lista de ficheros de la carpeta
                    string[] filesInFolder = null;
                    foreach (DataRow dr in dtFicheros.Rows)
                    {
                        filesInFolder = System.IO.Directory.GetFiles(pPath, dr["FicheroOrigen"].ToString());
                        if (filesInFolder != null && filesInFolder.Length > 0)
                        {
                            string f = Utils.WithoutPath(filesInFolder[0]);
                            htFileName.Add(f, f);
                            htFileNameBD.Add(f, dr["FicheroOrigen"].ToString());
                        }
                    }
                }

                if (htFileName.Count > 0)
                {
                    int i = 0;
                    foreach (DictionaryEntry de in htFileName)
                    {
                        string fileName = de.Key.ToString();
                        string fileNameBD = "";
                        if (htFileNameBD.ContainsKey(fileName))
                            fileNameBD = htFileNameBD[fileName].ToString();
                        Globals.GetInstance().GetLog2().Info(pAgent, sSipTypeName, "Conversor Universal se inicia para el fichero recibido " + fileName + ".");

                        //Procesar fichero
                        //---------------------------                    
                        //Primero comprobamos que exista el fichero 
                        if (!String.IsNullOrEmpty(fileName) && System.IO.File.Exists(pPath + "\\" + fileName))
                        {
                            //Obtenemos la configuración del conversor universal para el agente y el fichero a procesar
                            DataTable dtValores = ObtenerValoresConversorUniversal(db, pAgent, fileNameBD);

                            //Hacer copia backup al backupbox
                            Backup bk = new Backup();
                            ok = bk.DoBackup(pAgent, sSipTypeName, pPath + "\\" + fileName);
                            backupFilename = bk.BackupFilenamePath();

                            //Si el estado del conversor es igual a BK sólo hacemos copia del fichero de entrada al backupbox sin tratarlo ni integrarlo a connecta.
                            if ((dtValores.Rows.Count > 0) && dtValores.Rows[0]["Status"].ToString() != Constants.ESTADO_BACKUP)
                            {
                                //Generar ficheros salida
                                string error = GenerarFicheros(backupFilename, pPath, dtValores, pAgent, fileNameBD, db);
                                if (!string.IsNullOrEmpty(error))
                                {
                                    string msg = "Error al generar ficheros: {0}";
                                    Globals.GetInstance().GetLog2().Trace(pAgent, sSipTypeName, "UNIV0001", msg, error);
                                    if (File.Exists(pPath + "\\" + fileName + ".bad")) File.Delete(pPath + "\\" + fileName + ".bad");
                                    File.Move(backupFilename, pPath + "\\" + fileName + ".bad");
                                }
                            }

                            //Eliminamos el fichero del path de entrada
                            bool eliminarFicheroOrigen = true;
                            //Excepciones: Si contiene algunas fórmulas de cabecera y aún existe el fichero no lo eliminamos del path de entrada
                            if ((dtValores.Rows.Count > 0) && dtValores.Rows[0]["FormulaCabecera"].ToString().Contains("DELETEIFEMPTYFILE"))
                                eliminarFicheroOrigen = false;
                            if (eliminarFicheroOrigen)
                            {
                                if (File.Exists(pPath + "\\" + fileName))
                                    File.Delete(pPath + "\\" + fileName);
                            }
                        }
                        i++;
                    }
                    Globals.GetInstance().GetLog2().Info(pAgent, sSipTypeName, "Conversor Universal finaliza satisfactoriamente.");
                }
                bResult = true;
            }
            catch (Exception e)
            {
                Globals.GetInstance().GetLog2().Error(pAgent, sSipTypeName, e);
            }
        }

        /// <summary>
        /// Obtener nombre fichero salida
        /// </summary>
        private string ObtenerNombreFicheroDestino(DataRow dr, string origen)
        {
            string destino = dr["FicheroDestino"].ToString();
            //Si no hay lineas configuradas a BD dejamos el nombre tal y cómo nos lo han definido sin el sufijo del nombre origen, por si se quiere sólo un cambio de nombre..
            if (!string.IsNullOrEmpty(dr["PosicionDestino"].ToString().Trim()))
            {
                string ext = destino.LastIndexOf(".") > -1 ? destino.Substring(destino.LastIndexOf(".")) : "";
                destino = (destino.LastIndexOf(".") > -1 ? destino.Substring(0, destino.LastIndexOf(".")) : destino) + "." + origen + ext;
            }
            //Quitamos posibles carácteres raros...
            if (destino.EndsWith("\t")) destino = destino.Substring(0, destino.Length - 1);
            if (destino.EndsWith("\n")) destino = destino.Substring(0, destino.Length - 1);
            if (destino.EndsWith("\r")) destino = destino.Substring(0, destino.Length - 1);
            //Si el estado de la conversión es test TS generamos el fichero con prefijo test.*.txt para evitar que se integre a connecta
            return (dr["Status"].ToString() == Constants.ESTADO_TEST) ? "test." + destino : destino;
        }

        /// <summary>
        /// Procesar una línea del fichero origen (si es formato txt)
        /// </summary>
        private void ProcesarLineaFicheroTxt(ref int numLinea, ref int numLineaInicial, ref string textoAnteriorPrimeraLinea, ref string val, ref bool procesar,
            ref DataTable dtValores, ref string origen, ref string ficheroSalidaAnterior, ref bool cumpleCondicion, ref Hashtable htSalida, ref StreamWriter sw,
            ref string line, ref string separador, ref string separadorDestino, ref string fileIn, ref string ficheroOrigen, ref string agent, ref string pathOut, ref string idcAgenteDestino,
            ref string idcAgenteDestinoAnt, ref Encoding enc, ref Hashtable htFiles, ref string append, ref Hashtable htAppend, ref string ficheroSalidaAnteriorAppend, ref Hashtable htDelete,
            ref Hashtable htFuncionesCabecera, ref string lineAnt, Database db)
        {
            if (numLinea >= numLineaInicial)
            {
                if (textoAnteriorPrimeraLinea.Trim().ToLower() == val.Trim().ToLower())
                    procesar = true;
                if (string.IsNullOrEmpty(textoAnteriorPrimeraLinea) || procesar)
                {
                    int i = 0;
                    foreach (DataRow dr in dtValores.Rows)
                    {
                        string destino = ObtenerNombreFicheroDestino(dr, origen);

                        if (ficheroSalidaAnterior == "" || ficheroSalidaAnterior != destino)
                        {
                            //Comprobamos si cumple condición porcesado por cada fichero de salida posible
                            string condicionProcesado = dr["CondicionProcesado"].ToString();
                            if (!string.IsNullOrEmpty(condicionProcesado))
                                cumpleCondicion = CumpleCondicion(condicionProcesado, line, separador, separadorDestino, true, fileIn, ficheroOrigen, null, null, 0, agent, db);
                            else
                                cumpleCondicion = true;

                            if (htSalida != null && htSalida.Count > 0)
                            {
                                sw.WriteLine(ObtenerLinea(htSalida, separadorDestino));
                                htSalida.Clear();
                            }
                            if (sw != null)
                                sw.Close();

                            //Eliminamos los ficheros generados si estan vacíos
                            if (Utils.DeleteIfEmptyFile(pathOut + "\\" + ficheroSalidaAnterior))
                            {
                                if (!hashFileOut.ContainsKey(pathOut + "\\" + ficheroSalidaAnterior))
                                    hashFileOut.Add(pathOut + "\\" + ficheroSalidaAnterior, pathOut + "\\" + ficheroSalidaAnterior);
                            }

                            //Abrimos fichero actual
                            //Si idcAgenteDestino no es vacío modificamos path out
                            idcAgenteDestinoAnt = idcAgenteDestino;
                            idcAgenteDestino = dr["IdcAgenteDestino"].ToString();
                            if (!string.IsNullOrEmpty(idcAgenteDestino))
                            {
                                if (!string.IsNullOrEmpty(idcAgenteDestinoAnt))
                                    pathOut = pathOut.Replace(Constants.AGENT_ID + idcAgenteDestinoAnt, Constants.AGENT_ID + idcAgenteDestino);
                                else
                                    pathOut = pathOut.Substring(0, pathOut.IndexOf(Constants.AGENT_ID)) + Constants.AGENT_ID + idcAgenteDestino;
                            }
                            sw = new StreamWriter(pathOut + "\\" + destino, (!htFiles.ContainsKey(pathOut + "\\" + destino) && dr["FormulaCabecera"].ToString().Trim().ToUpper().Contains("REPLACEFILEIFEXIST") ? false : true), enc);

                            if (!htFiles.ContainsKey(pathOut + "\\" + destino))
                                htFiles.Add(pathOut + "\\" + destino, pathOut + "\\" + destino);

                            //Hacemos append a otro fichero si param activado                                                                                                                                                
                            append = dr["Append"].ToString().Trim().ToUpper();
                            if (append == "S" && !htAppend.ContainsKey(pathOut + "\\" + destino))
                                htAppend.Add(pathOut + "\\" + destino, pathOut + "\\" + ficheroSalidaAnteriorAppend);

                            //Eliminamos líneas repetidas fichero salida si param activado                                                                                                              
                            if (dr["EliminarLineasRepetidas"].ToString().Trim().ToUpper() == "S" && !htDelete.ContainsKey(pathOut + "\\" + destino))
                                htDelete.Add(pathOut + "\\" + destino, pathOut + "\\" + destino);

                            //Guardamos fichero y función a aplicar al final de todo por todo el fichero generado (por ejemplo: order...)
                            if (dr["FormulaCabecera"].ToString().Trim().ToUpper() != "" && !htFuncionesCabecera.ContainsKey(dr["FormulaCabecera"].ToString() + "\u2021" + pathOut + "\\" + destino))
                                htFuncionesCabecera.Add(dr["FormulaCabecera"].ToString() + "\u2021" + pathOut + "\\" + destino, dr["FormulaCabecera"].ToString() + "\u2021" + pathOut + "\\" + destino);
                        }
                        else if (i == 0) //Sólo revisamos condición por cada cambio de línea (una sóla vez)
                        {
                            string condicionProcesado = dr["CondicionProcesado"].ToString();
                            if (!string.IsNullOrEmpty(condicionProcesado))
                                cumpleCondicion = CumpleCondicion(condicionProcesado, line, separador, separadorDestino, true, fileIn, ficheroOrigen, null, null, 0, agent, db);
                            else
                                cumpleCondicion = true;
                        }

                        //Obtenemos valores entrada con la correcta posición de salida                                                                     
                        if (cumpleCondicion)
                        {
                            string valor = ObtenerValor(dr["Formula"].ToString(), line, separador, separadorDestino, true, fileIn, ficheroOrigen, null, null, 0, agent, db);
                            htSalida.Add(dr["PosicionDestino"].ToString(), valor);
                        }
                        else
                        {
                            if (dr["Formula"].ToString().Contains(INDICADOR_FORMULA + "=ANT") || dr["Formula"].ToString().Contains(INDICADOR_FORMULA + "ANT"))
                            {
                                string valor = ObtenerValor(dr["Formula"].ToString(), line, separador, separadorDestino, true, fileIn, ficheroOrigen, null, null, 0, agent, db);
                            }
                        }
                        ficheroSalidaAnterior = destino;
                        if (append != "S") ficheroSalidaAnteriorAppend = destino;
                        i++;
                    }
                    if (htSalida != null && htSalida.Count > 0)
                    {
                        sw.WriteLine(ObtenerLinea(htSalida, separadorDestino));
                        htSalida.Clear();
                    }
                }
            }
            val = Utils.tokenize(line, separador, 1);
            lineAnt = line;
            numLinea++;
        }

        /// <summary>
        /// Generar los ficheros de salida a partir del fichero de entrada 
        /// </summary>
        /// <param name="fileIn">fichero de entrada</param>        
        /// <param name="pathOut">path de salida de los ficheros resultantes</param>    
        /// <param name="dtValores">Valores configuración conversor universal para el agente y el fichero de entrada</param>    
        private string GenerarFicheros(string fileIn, string pathOut, DataTable dtValores, string agent, string ficheroOrigen, Database db)
        {
            //Ponemos el nombre del fichero de entrada como sufijo del fichero de salida
            string origenExt = "";
            string origen = Utils.WithoutPath(fileIn);
            origen = (origen.IndexOf("_") > -1 ? origen.Substring(origen.IndexOf("_") + 1) : origen);
            origenExt = origen;
            origen = (origen.LastIndexOf(".") > -1 ? origen.Substring(0, origen.LastIndexOf(".")) : origen);

            Hashtable htFiles = new Hashtable(10);

            string error = "";
            StreamWriter sw = null;
            StreamReader sr = null;
            try
            {
                bool isXLS = false;
                bool isTXT = false;
                foreach (string ext in _extensionsEXCEL)
                {
                    if (fileIn.ToLower().EndsWith(ext))
                        isXLS = true;
                }
                if (!isXLS)
                {
                    foreach (string ext in _extensionsTXT)
                    {
                        if (fileIn.ToLower().EndsWith(ext))
                            isTXT = true;
                    }
                }
                if (!isXLS && !isTXT)
                    error = "Formato de fichero desconocido " + fileIn;
                else
                {
                    hashFileOut.Clear();
                    //Obtenemos valores configuración a nivel de cabecera
                    int numLineaInicial = 1;
                    string pestanya = "";
                    string textoAnteriorPrimeraLinea = "";
                    string separador = "";
                    string separadorDestino = "";
                    string idcAgenteDestino = "";
                    string idcAgenteDestinoAnt = "";
                    string posicionDestino = "";
                    string formulaCabecera = "";
                    foreach (DataRow dr in dtValores.Rows)
                    {
                        pestanya = dr["Pestanya"].ToString();
                        numLineaInicial = int.TryParse(dr["NumeroLineaInicial"].ToString(), out numLineaInicial) ? numLineaInicial : 1;
                        textoAnteriorPrimeraLinea = dr["TextoAnteriorPrimeraLinea"].ToString();
                        separador = dr["SeparadorFicheroOrigen"].ToString();
                        separadorDestino = dr["SeparadorFicheroDestino"].ToString();
                        idcAgenteDestino = dr["IdcAgenteDestino"].ToString();
                        posicionDestino = dr["PosicionDestino"].ToString();
                        formulaCabecera = dr["FormulaCabecera"].ToString();
                        break;
                    }
                    if (separador.Equals("\\t"))
                        separador = "\t";
                    if (string.IsNullOrEmpty(separadorDestino.Trim()))
                    {
                        if (!string.IsNullOrEmpty(posicionDestino.Trim()))
                            separadorDestino = Constants.FIELD_SEPARATOR;
                        else if (!string.IsNullOrEmpty(separador))
                            separadorDestino = separador;
                    }
                    //Variables comunes                                        
                    Hashtable htSalida = new Hashtable();
                    string ficheroSalidaAnterior = "";
                    string ficheroSalidaAnteriorAppend = "";
                    string append = "";
                    Hashtable htAppend = new Hashtable(10);
                    Hashtable htDelete = new Hashtable(10);
                    Hashtable htFuncionesCabecera = new Hashtable(10);
                    bool procesar = false;
                    string val = "";
                    //Si idcAgenteDestino no es vacío modificamos path out
                    idcAgenteDestinoAnt = agent;
                    string pathOutAnt = pathOut;
                    if (!string.IsNullOrEmpty(idcAgenteDestino))
                    {
                        if (!string.IsNullOrEmpty(idcAgenteDestinoAnt))
                            pathOut = pathOut.Replace(Constants.AGENT_ID + idcAgenteDestinoAnt, Constants.AGENT_ID + idcAgenteDestino);
                        else
                            pathOut = pathOut.Substring(0, pathOut.IndexOf(Constants.AGENT_ID)) + Constants.AGENT_ID + idcAgenteDestino;
                    }
                    bool continuar = true;
                    //Aplicar funciones de cabecera que no implican acceder ni tratar el fichero origen                    
                    string idcAgenteDestinoAntAux = idcAgenteDestinoAnt;
                    string pathOutAux = pathOut;
                    foreach (DataRow dr in dtValores.Rows)
                    {
                        formulaCabecera = dr["FormulaCabecera"].ToString();
                        if (!string.IsNullOrEmpty(formulaCabecera.Trim()))
                        {
                            string idcAgenteDestinoAux = dr["IdcAgenteDestino"].ToString();
                            if (!string.IsNullOrEmpty(idcAgenteDestinoAux))
                            {
                                if (!string.IsNullOrEmpty(idcAgenteDestinoAntAux))
                                    pathOutAux = pathOutAux.Replace(Constants.AGENT_ID + idcAgenteDestinoAntAux, Constants.AGENT_ID + idcAgenteDestinoAux);
                                else
                                    pathOutAux = pathOutAux.Substring(0, pathOutAux.IndexOf(Constants.AGENT_ID)) + Constants.AGENT_ID + idcAgenteDestinoAux;
                            }
                            if (formulaCabecera.ToUpper().Contains("RENAME"))
                            {
                                string destino = ObtenerNombreFicheroDestino(dr, origen);
                                string clave = formulaCabecera + "\u2021" + pathOutAux + "\\" + origenExt + "\u2021" + pathOutAux + "\\" + destino;
                                if (formulaCabecera.Trim().ToUpper() != "" && !htFuncionesCabecera.ContainsKey(clave))
                                    htFuncionesCabecera.Add(clave, clave);
                                continuar = false;
                            }
                            else if (formulaCabecera.ToUpper().Contains("COPY") || formulaCabecera.ToUpper().Contains("MOVE") ||
                                formulaCabecera.ToUpper().Contains("DELETE") || formulaCabecera.ToUpper().Contains("DELETEIFEMPTYFILE"))
                            {
                                string clave = formulaCabecera + "\u2021" + pathOutAnt + "\\" + origenExt + "\u2021" + pathOutAux + "\\" + origenExt;
                                if (formulaCabecera.Trim().ToUpper() != "" && !htFuncionesCabecera.ContainsKey(clave))
                                    htFuncionesCabecera.Add(clave, clave);
                                continuar = false;
                            }
                            idcAgenteDestinoAntAux = idcAgenteDestinoAux;
                        }
                    }
                    if (continuar)
                    {
                        if (isTXT)
                        {
                            if (fileIn.ToLower().Trim().EndsWith("mht") || fileIn.ToLower().Trim().EndsWith("mhtml") ||
                                fileIn.ToLower().Trim().EndsWith("htm") || fileIn.ToLower().Trim().EndsWith("html"))
                            {
                                //Procesamos fichero html               
                                Encoding enc = Utils.GetFileEncoding(fileIn);
                                HtmlDocument doc = new HtmlDocument();
                                doc.Load(fileIn);
                                var tr = doc.DocumentNode.SelectNodes("//tr[td]");
                                int numLinea = 1;
                                string line = "";
                                string lineAnt = "";
                                bool cumpleCondicion = true;
                                separador = Constants.FIELD_SEPARATOR;
                                foreach (var row in tr)
                                {
                                    var td = row.SelectNodes("td");
                                    line = "";
                                    foreach (var cell in td)
                                    {
                                        line += cell.InnerText.Replace("&nbsp;", "") + separador;
                                    }
                                    ProcesarLineaFicheroTxt(ref numLinea, ref numLineaInicial, ref textoAnteriorPrimeraLinea, ref val, ref procesar,
                                                            ref dtValores, ref origen, ref ficheroSalidaAnterior, ref cumpleCondicion, ref htSalida, ref sw,
                                                            ref line, ref separador, ref separadorDestino, ref fileIn, ref ficheroOrigen, ref agent, ref pathOut, ref idcAgenteDestino,
                                                            ref idcAgenteDestinoAnt, ref enc, ref htFiles, ref append, ref htAppend, ref ficheroSalidaAnteriorAppend,
                                                            ref htDelete, ref htFuncionesCabecera, ref lineAnt, db);
                                }
                            }
                            else
                            {
                                //Procesamos fichero de texto plano
                                Encoding enc = Utils.GetFileEncoding(fileIn);
                                sr = new StreamReader(fileIn, enc);
                                string line = "";
                                string lineAnt = "";
                                int numLinea = 1;
                                if (string.IsNullOrEmpty(separador) && !string.IsNullOrEmpty(posicionDestino.Trim()))
                                    error = "No se ha configurado ningún separador para el fichero " + fileIn;
                                else
                                {
                                    //Procesamos líneas
                                    bool cumpleCondicion = true;
                                    while ((line = sr.ReadLine()) != null)
                                    {
                                        if (!line.EndsWith(separador)) line = line + separador;
                                        ProcesarLineaFicheroTxt(ref numLinea, ref numLineaInicial, ref textoAnteriorPrimeraLinea, ref val, ref procesar,
                                                                ref dtValores, ref origen, ref ficheroSalidaAnterior, ref cumpleCondicion, ref htSalida, ref sw,
                                                                ref line, ref separador, ref separadorDestino, ref fileIn, ref ficheroOrigen, ref agent, ref pathOut, ref idcAgenteDestino,
                                                                ref idcAgenteDestinoAnt, ref enc, ref htFiles, ref append, ref htAppend, ref ficheroSalidaAnteriorAppend, ref htDelete,
                                                                ref htFuncionesCabecera, ref lineAnt, db);
                                    }
                                }
                                sr.Close();
                            }
                        }
                        else
                        {
                            //Procesamos fichero Excel                                              
                            //Inicializamos objetos
                            HSSFWorkbook hssfwb = null;
                            ISheet worksheetXLS = null;
                            ExcelPackage xlPackage = null;
                            ExcelWorksheet worksheetXLSX = null;
                            if (fileIn.ToLower().Trim().EndsWith("xls"))
                            {
                                using (FileStream file = new FileStream(fileIn, FileMode.Open, FileAccess.Read))
                                {
                                    hssfwb = new HSSFWorkbook(file);
                                }
                            }
                            else
                            {
                                FileInfo existingFile = new FileInfo(fileIn);
                                xlPackage = new ExcelPackage(existingFile);
                            }
                            DataTable dtPestanyas = ObtenerFicheroPestanya(db, agent, ficheroOrigen);
                            foreach (DataRow drPestanyas in dtPestanyas.Rows)
                            {
                                pestanya = drPestanyas["Pestanya"].ToString();
                                numLineaInicial = int.TryParse(drPestanyas["NumeroLineaInicial"].ToString(), out numLineaInicial) ? numLineaInicial : 1;
                                if (fileIn.ToLower().Trim().EndsWith("xls"))
                                {
                                    // Process the file                                    
                                    try
                                    {
                                        int i;
                                        if (int.TryParse(pestanya, out i))
                                            worksheetXLS = hssfwb.GetSheetAt(i - 1);
                                        else
                                        {
                                            for (i = 0; i <= 10; i++)
                                            {
                                                worksheetXLS = hssfwb.GetSheetAt(i);
                                                if (worksheetXLS.SheetName.Trim() == pestanya.Trim())
                                                    break;
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        throw new Exception("No se ha podido acceder a la pestaña " + pestanya + " del fichero " + pathOutAnt + "\\" + ficheroOrigen);
                                    }

                                    //Read all the rows...                                                   
                                    int cnt = worksheetXLS.LastRowNum;
                                    bool cumpleCondicion = true;
                                    for (int row = numLineaInicial; row <= cnt + 1; row++)
                                    {
                                        if (textoAnteriorPrimeraLinea.Trim().ToLower() == val.Trim().ToLower())
                                            procesar = true;
                                        if (string.IsNullOrEmpty(textoAnteriorPrimeraLinea) || procesar)
                                        {
                                            int x = 0;
                                            foreach (DataRow dr in dtValores.Rows)
                                            {
                                                if (dr["Pestanya"].ToString() != pestanya)
                                                    continue;

                                                string destino = ObtenerNombreFicheroDestino(dr, origen);

                                                if (ficheroSalidaAnterior == "" || ficheroSalidaAnterior != destino)
                                                {
                                                    //Comprobamos si cumple condición procesado por cada fichero de salida posible
                                                    string condicionProcesado = dr["CondicionProcesado"].ToString();
                                                    if (!string.IsNullOrEmpty(condicionProcesado))
                                                        cumpleCondicion = CumpleCondicion(condicionProcesado, "", "", separadorDestino, false, fileIn, ficheroOrigen, worksheetXLS, null, row, agent, db);
                                                    else
                                                        cumpleCondicion = true;

                                                    if (htSalida != null && htSalida.Count > 0)
                                                    {
                                                        sw.WriteLine(ObtenerLinea(htSalida, separadorDestino));
                                                        htSalida.Clear();
                                                    }
                                                    if (sw != null)
                                                        sw.Close();

                                                    //Eliminamos los ficheros generados si estan vacíos
                                                    if (Utils.DeleteIfEmptyFile(pathOut + "\\" + ficheroSalidaAnterior))
                                                    {
                                                        if (!hashFileOut.ContainsKey(pathOut + "\\" + ficheroSalidaAnterior))
                                                            hashFileOut.Add(pathOut + "\\" + ficheroSalidaAnterior, pathOut + "\\" + ficheroSalidaAnterior);
                                                    }

                                                    //Abrimos fichero actual
                                                    //Si idcAgenteDestino no es vacío modificamos path out
                                                    idcAgenteDestinoAnt = idcAgenteDestino;
                                                    idcAgenteDestino = dr["IdcAgenteDestino"].ToString();
                                                    if (!string.IsNullOrEmpty(idcAgenteDestino))
                                                    {
                                                        if (!string.IsNullOrEmpty(idcAgenteDestinoAnt))
                                                            pathOut = pathOut.Replace(Constants.AGENT_ID + idcAgenteDestinoAnt, Constants.AGENT_ID + idcAgenteDestino);
                                                        else
                                                            pathOut = pathOut.Substring(0, pathOut.IndexOf(Constants.AGENT_ID)) + Constants.AGENT_ID + idcAgenteDestino;
                                                    }
                                                    sw = new StreamWriter(pathOut + "\\" + destino, (!htFiles.ContainsKey(pathOut + "\\" + destino) && dr["FormulaCabecera"].ToString().Trim().ToUpper().Contains("REPLACEFILEIFEXIST") ? false : true));

                                                    if (!htFiles.ContainsKey(pathOut + "\\" + destino))
                                                        htFiles.Add(pathOut + "\\" + destino, pathOut + "\\" + destino);

                                                    //Hacemos append a otro fichero si param activado                                                                                                                                                
                                                    append = dr["Append"].ToString().Trim().ToUpper();
                                                    if (append == "S" && !htAppend.ContainsKey(pathOut + "\\" + destino))
                                                        htAppend.Add(pathOut + "\\" + destino, pathOut + "\\" + ficheroSalidaAnteriorAppend);

                                                    //Eliminamos líneas repetidas fichero salida si param activado                                                                                                              
                                                    if (dr["EliminarLineasRepetidas"].ToString().Trim().ToUpper() == "S" && !htDelete.ContainsKey(pathOut + "\\" + destino))
                                                        htDelete.Add(pathOut + "\\" + destino, pathOut + "\\" + destino);

                                                    //Guardamos fichero y función a aplicar al final de todo por todo el fichero generado (por ejemplo: order...)
                                                    if (dr["FormulaCabecera"].ToString().Trim().ToUpper() != "" && !htFuncionesCabecera.ContainsKey(dr["FormulaCabecera"].ToString() + "\u2021" + pathOut + "\\" + destino))
                                                        htFuncionesCabecera.Add(dr["FormulaCabecera"].ToString() + "\u2021" + pathOut + "\\" + destino, dr["FormulaCabecera"].ToString() + "\u2021" + pathOut + "\\" + destino);
                                                }
                                                else if (x == 0) //Sólo revisamos condición por cada cambio de línea (una sóla vez)
                                                {
                                                    string condicionProcesado = dr["CondicionProcesado"].ToString();
                                                    if (!string.IsNullOrEmpty(condicionProcesado))
                                                        cumpleCondicion = CumpleCondicion(condicionProcesado, "", "", separadorDestino, false, fileIn, ficheroOrigen, worksheetXLS, null, row, agent, db);
                                                    else
                                                        cumpleCondicion = true;
                                                }

                                                //Obtenemos valores entrada con la correcta posición de salida                                                                     
                                                if (cumpleCondicion)
                                                {
                                                    string valor = ObtenerValor(dr["Formula"].ToString(), "", "", separadorDestino, false, fileIn, ficheroOrigen, worksheetXLS, null, row, agent, db);
                                                    htSalida.Add(dr["PosicionDestino"].ToString(), valor);
                                                }
                                                else
                                                {
                                                    if (dr["Formula"].ToString().Contains(INDICADOR_FORMULA + "=ANT") || dr["Formula"].ToString().Contains(INDICADOR_FORMULA + "ANT"))
                                                    {
                                                        string valor = ObtenerValor(dr["Formula"].ToString(), "", "", separadorDestino, false, fileIn, ficheroOrigen, worksheetXLS, null, row, agent, db);
                                                    }
                                                }
                                                ficheroSalidaAnterior = destino;
                                                if (append != "S") ficheroSalidaAnteriorAppend = destino;
                                                x++;
                                            }
                                            if (htSalida != null && htSalida.Count > 0)
                                            {
                                                sw.WriteLine(ObtenerLinea(htSalida, separadorDestino));
                                                htSalida.Clear();
                                            }
                                        }
                                        val = GetCellValue(worksheetXLS, row, 1).Trim();
                                    }
                                }
                                else
                                {
                                    try
                                    {
                                        int i;
                                        if (int.TryParse(pestanya, out i))
                                            worksheetXLSX = xlPackage.Workbook.Worksheets[i];
                                        else
                                        {
                                            for (i = 1; i <= 10; i++)
                                            {
                                                worksheetXLSX = xlPackage.Workbook.Worksheets[i];
                                                if (worksheetXLSX.Name.Trim() == pestanya.Trim())
                                                    break;
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        throw new Exception("No se ha podido acceder a la pestaña " + pestanya + " del fichero " + pathOutAnt + "\\" + ficheroOrigen);
                                    }

                                    int row = numLineaInicial;
                                    bool cumpleCondicion = true;
                                    while (hayDatos(worksheetXLSX, row))
                                    {
                                        if (textoAnteriorPrimeraLinea.Trim().ToLower() == val.Trim().ToLower())
                                            procesar = true;
                                        if (string.IsNullOrEmpty(textoAnteriorPrimeraLinea) || procesar)
                                        {
                                            int x = 0;
                                            foreach (DataRow dr in dtValores.Rows)
                                            {
                                                if (dr["Pestanya"].ToString() != pestanya)
                                                    continue;

                                                string destino = ObtenerNombreFicheroDestino(dr, origen);

                                                if (ficheroSalidaAnterior == "" || ficheroSalidaAnterior != destino)
                                                {
                                                    //Comprobamos si cumple condición porcesado por cada fichero de salida posible                                            
                                                    string condicionProcesado = dr["CondicionProcesado"].ToString();
                                                    if (!string.IsNullOrEmpty(condicionProcesado))
                                                        cumpleCondicion = CumpleCondicion(condicionProcesado, "", "", separadorDestino, false, fileIn, ficheroOrigen, null, worksheetXLSX, row, agent, db);
                                                    else
                                                        cumpleCondicion = true;

                                                    if (htSalida != null && htSalida.Count > 0)
                                                    {
                                                        sw.WriteLine(ObtenerLinea(htSalida, separadorDestino));
                                                        htSalida.Clear();
                                                    }
                                                    if (sw != null)
                                                        sw.Close();

                                                    //Eliminamos los ficheros generados si estan vacíos
                                                    if (Utils.DeleteIfEmptyFile(pathOut + "\\" + ficheroSalidaAnterior))
                                                    {
                                                        if (!hashFileOut.ContainsKey(pathOut + "\\" + ficheroSalidaAnterior))
                                                            hashFileOut.Add(pathOut + "\\" + ficheroSalidaAnterior, pathOut + "\\" + ficheroSalidaAnterior);
                                                    }

                                                    //Abrimos fichero actual
                                                    //Si idcAgenteDestino no es vacío modificamos path out
                                                    idcAgenteDestinoAnt = idcAgenteDestino;
                                                    idcAgenteDestino = dr["IdcAgenteDestino"].ToString();
                                                    if (!string.IsNullOrEmpty(idcAgenteDestino))
                                                    {
                                                        if (!string.IsNullOrEmpty(idcAgenteDestinoAnt))
                                                            pathOut = pathOut.Replace(Constants.AGENT_ID + idcAgenteDestinoAnt, Constants.AGENT_ID + idcAgenteDestino);
                                                        else
                                                            pathOut = pathOut.Substring(0, pathOut.IndexOf(Constants.AGENT_ID)) + Constants.AGENT_ID + idcAgenteDestino;
                                                    }
                                                    sw = new StreamWriter(pathOut + "\\" + destino, (!htFiles.ContainsKey(pathOut + "\\" + destino) && dr["FormulaCabecera"].ToString().Trim().ToUpper().Contains("REPLACEFILEIFEXIST") ? false : true));

                                                    if (!htFiles.ContainsKey(pathOut + "\\" + destino))
                                                        htFiles.Add(pathOut + "\\" + destino, pathOut + "\\" + destino);

                                                    //Hacemos append a otro fichero si param activado                                                                                                                                                
                                                    append = dr["Append"].ToString().Trim().ToUpper();
                                                    if (append == "S" && !htAppend.ContainsKey(pathOut + "\\" + destino))
                                                        htAppend.Add(pathOut + "\\" + destino, pathOut + "\\" + ficheroSalidaAnteriorAppend);

                                                    //Eliminamos líneas repetidas fichero salida si param activado                                                                                                              
                                                    if (dr["EliminarLineasRepetidas"].ToString().Trim().ToUpper() == "S" && !htDelete.ContainsKey(pathOut + "\\" + destino))
                                                        htDelete.Add(pathOut + "\\" + destino, pathOut + "\\" + destino);

                                                    //Guardamos fichero y función a aplicar al final de todo por todo el fichero generado (por ejemplo: order...)
                                                    if (dr["FormulaCabecera"].ToString().Trim().ToUpper() != "" && !htFuncionesCabecera.ContainsKey(dr["FormulaCabecera"].ToString() + "\u2021" + pathOut + "\\" + destino))
                                                        htFuncionesCabecera.Add(dr["FormulaCabecera"].ToString() + "\u2021" + pathOut + "\\" + destino, dr["FormulaCabecera"].ToString() + "\u2021" + pathOut + "\\" + destino);
                                                }
                                                else if (x == 0) //Sólo revisamos condición por cada cambio de línea (una sóla vez)
                                                {
                                                    string condicionProcesado = dr["CondicionProcesado"].ToString();
                                                    if (!string.IsNullOrEmpty(condicionProcesado))
                                                        cumpleCondicion = CumpleCondicion(condicionProcesado, "", "", separadorDestino, false, fileIn, ficheroOrigen, null, worksheetXLSX, row, agent, db);
                                                    else
                                                        cumpleCondicion = true;
                                                }

                                                //Obtenemos valores entrada con la correcta posición de salida                                                                     
                                                if (cumpleCondicion)
                                                {
                                                    string valor = ObtenerValor(dr["Formula"].ToString(), "", "", separadorDestino, false, fileIn, ficheroOrigen, null, worksheetXLSX, row, agent, db);
                                                    htSalida.Add(dr["PosicionDestino"].ToString(), valor);
                                                }
                                                else
                                                {
                                                    if (dr["Formula"].ToString().Contains(INDICADOR_FORMULA + "=ANT") || dr["Formula"].ToString().Contains(INDICADOR_FORMULA + "ANT"))
                                                    {
                                                        string valor = ObtenerValor(dr["Formula"].ToString(), "", "", separadorDestino, false, fileIn, ficheroOrigen, null, worksheetXLSX, row, agent, db);
                                                    }
                                                }
                                                ficheroSalidaAnterior = destino;
                                                if (append != "S") ficheroSalidaAnteriorAppend = destino;
                                                x++;
                                            }
                                            if (htSalida != null && htSalida.Count > 0)
                                            {
                                                sw.WriteLine(ObtenerLinea(htSalida, separadorDestino));
                                                htSalida.Clear();
                                            }
                                        }
                                        val = GetCellValue(worksheetXLSX, row, 1).Trim();
                                        row++;
                                    }
                                }
                            }
                        }
                    }
                    if (sw != null)
                        sw.Close();

                    //Eliminamos los ficheros generados si estan vacíos
                    if (Utils.DeleteIfEmptyFile(pathOut + "\\" + ficheroSalidaAnterior))
                    {
                        if (isTXT)
                            Globals.GetInstance().GetLog2().Error(agent, sSipTypeName, "Conversor Universal no ha generado ninguna línea en el fichero " + pathOut + "\\" + ficheroSalidaAnterior + " a partir del fichero " + pathOutAnt + "\\" + ficheroOrigen);
                        else
                            Globals.GetInstance().GetLog2().Error(agent, sSipTypeName, "Conversor Universal no ha generado ninguna línea en el fichero " + pathOut + "\\" + ficheroSalidaAnterior + " a partir del fichero " + pathOutAnt + "\\" + ficheroOrigen + " pestanya " + pestanya);
                        if (!hashFileOut.ContainsKey(pathOut + "\\" + ficheroSalidaAnterior))
                            hashFileOut.Add(pathOut + "\\" + ficheroSalidaAnterior, pathOut + "\\" + ficheroSalidaAnterior);
                    }

                    //Dejamos log ficheros vacíos eliminados
                    if (hashFileOut != null && hashFileOut.Count > 0)
                    {
                        foreach (DictionaryEntry file in hashFileOut)
                        {
                            string f = file.Key.ToString();
                            if (!File.Exists(f))
                                Globals.GetInstance().GetLog2().Error(agent, sSipTypeName, "Conversor Universal elimina fichero vacío " + f);
                        }
                    }
                    hashFileOut.Clear();

                    //Hacemos append a otro fichero si param activado
                    AppendFile(htAppend);
                    //Eliminamos líneas repetidas si param activado
                    EliminarLineasRepetidas(htDelete);
                    //Aplicamos funciones a nivel cabecera, por todo el fichero generado
                    AplicarFuncionesCabecera(htFuncionesCabecera);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                if (sw != null)
                    sw.Close();
                if (sr != null)
                    sr.Close();
            }
            return error;
        }

        /// <summary>
        /// Hacemos append a otro fichero si param activado
        /// </summary>
        private void AppendFile(Hashtable htAppend)
        {
            if (htAppend != null && htAppend.Count > 0)
            {
                foreach (DictionaryEntry ap in htAppend)
                {
                    string file = ap.Key.ToString();
                    string appendFile = ap.Value.ToString();
                    if (File.Exists(appendFile) && File.Exists(file))
                    {
                        StreamWriter sw = File.AppendText(appendFile);
                        string[] lines = File.ReadAllLines(file);
                        foreach (string line in lines) sw.WriteLine(line);
                        if (sw != null)
                            sw.Close();
                        if (File.Exists(file))
                            File.Delete(file);
                    }
                }
            }
        }

        /// <summary>
        /// Eliminamos líneas repetidas si param activado
        /// </summary>
        private void EliminarLineasRepetidas(Hashtable htDelete)
        {
            if (htDelete != null && htDelete.Count > 0)
            {
                foreach (DictionaryEntry de in htDelete)
                {
                    string file = de.Key.ToString();
                    if (File.Exists(file))
                    {
                        var sr = new StreamReader(File.OpenRead(file));
                        var sw = new StreamWriter(File.OpenWrite(file + ".tmp"));
                        var lines = new HashSet<int>();
                        while (!sr.EndOfStream)
                        {
                            string line = sr.ReadLine();
                            int hc = line.GetHashCode();
                            if (lines.Contains(hc))
                                continue;

                            lines.Add(hc);
                            sw.WriteLine(line);
                        }
                        sw.Flush();
                        sw.Close();
                        sr.Close();
                        if (File.Exists(file))
                            File.Delete(file);
                        if (File.Exists(file + ".tmp"))
                            File.Move(file + ".tmp", file);
                    }
                }
            }
        }

        /// <summary>
        /// Eliminamos líneas repetidas si param activado
        /// </summary>
        private void AplicarFuncionesCabecera(Hashtable htFuncionesCabecera)
        {
            if (htFuncionesCabecera != null && htFuncionesCabecera.Count > 0)
            {
                foreach (DictionaryEntry f in htFuncionesCabecera)
                {
                    string[] k = f.Key.ToString().Split('\u2021');
                    string formula = k[0].ToString();
                    string file = k[1].ToString();
                    if (File.Exists(file))
                    {
                        if (formula.Contains(INDICADOR_FORMULA))
                        {
                            string formulaAux = formula;
                            int numFormulas = formula.Replace(";", "").Replace(INDICADOR_FORMULA, ";").Split(';').Count() - 1;
                            for (int i = 1; i <= numFormulas; i++)
                            {
                                //Dejamos sólo la parte de la fórmula                    
                                formula = formula.Substring(formula.IndexOf(INDICADOR_FORMULA) + INDICADOR_FORMULA.Length,
                                        formula.Length - (formula.IndexOf(INDICADOR_FORMULA) + INDICADOR_FORMULA.Length));

                                //Si hay más de una fórmula
                                if (formula.Contains(";" + INDICADOR_FORMULA))
                                {
                                    formulaAux = formula;
                                    formula = formula.Substring(0, formula.IndexOf(INDICADOR_FORMULA) - 1);
                                }

                                //Dejamos sólo la fórmula sin el indicador 
                                formula = formula.Replace(INDICADOR_FORMULA, "");

                                if (formula.ToUpper().Trim().StartsWith("ORDER"))
                                {
                                    int sortField1 = -1, sortField2 = -1, sortField3 = -1, sortField4 = -1;
                                    string[] pos = formula.Substring("ORDER".Length, formula.Length - "ORDER".Length).Replace("(", "").Replace(")", "").Split(',');
                                    if (pos.Length > 0 && int.TryParse(pos[0], out sortField1))
                                        sortField1 = sortField1 - 1;
                                    if (pos.Length > 1 && int.TryParse(pos[1], out sortField2))
                                        sortField2 = sortField2 - 1;
                                    if (pos.Length > 2 && int.TryParse(pos[2], out sortField3))
                                        sortField3 = sortField3 - 1;
                                    if (pos.Length > 3 && int.TryParse(pos[3], out sortField4))
                                        sortField4 = sortField4 - 1;
                                    string[] lineas = System.IO.File.ReadAllLines(file);
                                    int ix = 0;
                                    foreach (string l in lineas)
                                    {
                                        lineas[ix] = l.Replace(Constants.FIELD_SEPARATOR, "\u2021");
                                        ix++;
                                    }
                                    System.IO.StreamWriter sw = new System.IO.StreamWriter(file + ".tmp");
                                    if (sortField2 < 0)
                                        foreach (string str in RunQuery(lineas, sortField1)) sw.WriteLine(str.Replace("\u2021", Constants.FIELD_SEPARATOR));
                                    else if (sortField3 < 0)
                                        foreach (string str in RunQuery(lineas, sortField1, sortField2)) sw.WriteLine(str.Replace("\u2021", Constants.FIELD_SEPARATOR));
                                    else if (sortField4 < 0)
                                        foreach (string str in RunQuery(lineas, sortField1, sortField2, sortField3)) sw.WriteLine(str.Replace("\u2021", Constants.FIELD_SEPARATOR));
                                    else
                                        foreach (string str in RunQuery(lineas, sortField1, sortField2, sortField3, sortField4)) sw.WriteLine(str.Replace("\u2021", Constants.FIELD_SEPARATOR));
                                    sw.Close();
                                    if (File.Exists(file))
                                        File.Delete(file);
                                    if (File.Exists(file + ".tmp"))
                                        File.Move(file + ".tmp", file);
                                }
                                else if (formula.ToUpper().Trim().StartsWith(Constants.INDICADOR_RESET))
                                {
                                    string[] param = formula.Replace(Constants.INDICADOR_RESET, "").Replace("(", "").Replace(")", "").Split(',');
                                    StreamReader sr = new StreamReader(file);
                                    StreamWriter sw = new StreamWriter(file + ".tmp");
                                    string temp = sr.ReadToEnd();
                                    if (param.Length == 1 && string.IsNullOrEmpty(param[0].Trim()))
                                    {
                                        //Reset de todo                                        
                                        sw.WriteLine(Constants.INDICADOR_RESET + Constants.FIELD_SEPARATOR);
                                    }
                                    else
                                    {
                                        //Reset por fechas, fabricante y o tipo
                                        int pos = -1;
                                        string fab = "";
                                        string tipo = "";
                                        string tipoReset = "";
                                        string tipoLinea = "";
                                        if (param.Length >= 1)
                                            int.TryParse(param[0].Replace(INDICADOR_POSICIONDESTINO, ""), out pos);
                                        if (param.Length >= 2)
                                            fab = param[1].Replace(INDICADOR_VALORFIJO, "").Replace("=", "");
                                        if (param.Length >= 3)
                                            tipo = param[2].Replace(INDICADOR_VALORFIJO, "").Replace("=", "");
                                        if (param.Length >= 4)
                                            tipoReset = param[3].Replace(INDICADOR_VALORFIJO, "").Replace("=", "");
                                        if (param.Length >= 5)
                                            tipoLinea = param[4].Replace(INDICADOR_VALORFIJO, "").Replace("=", "");

                                        DateTime dtIni = DateTime.MinValue;
                                        DateTime dtFin = DateTime.MinValue;
                                        if (pos > 0)
                                        {
                                            string[] lineas = System.IO.File.ReadAllLines(file);
                                            Database db = Globals.GetInstance().GetDatabase();
                                            foreach (string l in lineas)
                                            {
                                                string fechaFacturaStr = db.GetDate(Utils.tokenize(l, Constants.FIELD_SEPARATOR, pos).Trim()).ToString();
                                                DateTime dtAct = DateTime.Parse(fechaFacturaStr);
                                                if (dtIni == DateTime.MinValue || dtAct < dtIni)
                                                    dtIni = dtAct;
                                                if (dtFin == DateTime.MinValue || dtAct > dtFin)
                                                    dtFin = dtAct;
                                            }
                                            if (tipoReset.Trim().ToUpper() != "D")
                                            {
                                                dtIni = new DateTime(dtIni.Year, dtIni.Month, 1);
                                                dtFin = new DateTime(dtFin.Year, dtFin.Month, DateTime.DaysInMonth(dtFin.Year, dtFin.Month));
                                            }
                                        }
                                        sw.WriteLine(Constants.INDICADOR_RESET + tipo + Constants.FIELD_SEPARATOR +
                                                     (dtIni != DateTime.MinValue ? dtIni.ToString("dd/MM/yyyy") : "") + Constants.FIELD_SEPARATOR +
                                                     (dtFin != DateTime.MinValue ? dtFin.ToString("dd/MM/yyyy") : "") + Constants.FIELD_SEPARATOR +
                                                     fab + Constants.FIELD_SEPARATOR +
                                                     tipoLinea + Constants.FIELD_SEPARATOR);
                                    }
                                    sw.Write(temp);
                                    sw.Close();
                                    sr.Close();
                                    if (File.Exists(file))
                                        File.Delete(file);
                                    if (File.Exists(file + ".tmp"))
                                        File.Move(file + ".tmp", file);
                                }
                                else if (formula.ToUpper().Trim().StartsWith("RENAME") || formula.ToUpper().Trim().StartsWith("MOVE"))
                                {
                                    string fileOut = k[2].ToString();
                                    if (!File.Exists(fileOut))
                                        File.Move(file, fileOut);
                                }
                                else if (formula.ToUpper().Trim().StartsWith("COPY"))
                                {
                                    string fileOut = k[2].ToString();
                                    if (!File.Exists(fileOut))
                                        File.Copy(file, fileOut);
                                }
                                else if (formula.ToUpper().Trim().StartsWith("DELETEIFEMPTYFILE"))
                                {
                                    Utils.DeleteIfEmptyFile(file);
                                }
                                else if (formula.ToUpper().Trim().StartsWith("DELETE"))
                                {
                                    if (!File.Exists(file))
                                        File.Delete(file);
                                }
                                formula = formulaAux;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Obtener línea salida
        /// </summary>
        private string ObtenerLinea(Hashtable ht, string separadorDestino)
        {
            //Si sólo contiene un registro y la clave está vacía significa que estamos generando toda la línea de entrada cómo salida sin ninguna transformación / conversión.
            if (ht.Count == 1 && ht.ContainsKey(""))
                return ht[""].ToString();

            string line = "";
            ArrayList keys = new ArrayList();
            keys.AddRange(ht.Keys);
            keys.Sort(new CustomComparer());
            for (int i = 0; i <= int.Parse(keys[keys.Count - 1].ToString()); i++)
            {
                if (ht[(i + 1).ToString()] != null)
                    line += ht[(i + 1).ToString()].ToString() + separadorDestino;
                else
                    line += "" + separadorDestino;
            }
            return line;
        }

        /// <summary>
        /// Tratar fórmulas que no aplican a ningún otro valor
        /// </summary>
        private string TratarFormulaSola(ref string formula, string fileIn, string line, string separador, bool isTXT, ISheet worksheetXLS, ExcelWorksheet worksheetXLSX, int row, Database db)
        {
            string valor = "";
            //Dejamos sólo la parte de la fórmula, por si hay algun indicador de posición origen primero
            //No se puede dar pero por si acaso...
            formula = formula.Substring(formula.IndexOf(INDICADOR_FORMULA) + INDICADOR_FORMULA.Length,
                formula.Length - (formula.IndexOf(INDICADOR_FORMULA) + INDICADOR_FORMULA.Length));

            formula = formula.Replace(";@F", '\u2021'.ToString()).Replace("; @F", '\u2021'.ToString());

            foreach (string f in formula.Split('\u2021'))
            {
                //Dejamos sólo la fórmula sin el indicador                         
                formula = f.Replace(INDICADOR_FORMULA, "");

                if (formula == "MONTH")
                {
                    if (!string.IsNullOrEmpty(valor)) //Si hay fórmulas concatenadas se puede dar que primero se obtenga fecha actual - X y a posteriori se quiera cojer el mes de ésta fecha calculada previamente
                    {
                        DateTime dt;
                        if (DateTime.TryParse(valor, out dt))
                            valor = dt.Month.ToString();
                    }
                    else
                        valor = DateTime.Now.Month.ToString();
                }
                else if (formula.StartsWith("MONTH-"))
                {
                    int month = DateTime.Now.Month;
                    int numAux;
                    int num = int.TryParse(formula.Substring(formula.IndexOf("-") + 1), out numAux) ? numAux : 0;
                    valor = (month - num).ToString();
                }
                else if (formula == "YEAR")
                {
                    if (!string.IsNullOrEmpty(valor)) //Si hay fórmulas concatenadas se puede dar que primero se obtenga fecha actual - X y a posteriori se quiera cojer el año de ésta fecha calculada previamente
                    {
                        DateTime dt;
                        if (DateTime.TryParse(valor, out dt))
                            valor = dt.Year.ToString();
                    }
                    else
                        valor = DateTime.Now.Year.ToString();
                }
                else if (formula.StartsWith("YEAR-"))
                {
                    int year = DateTime.Now.Year;
                    int numAux;
                    int num = int.TryParse(formula.Substring(formula.IndexOf("-") + 1), out numAux) ? numAux : 0;
                    valor = (year - num).ToString();
                }
                else if (formula == "GETDATE")
                    valor = DateTime.Now.ToString("dd/MM/yyyy");
                else if (formula.StartsWith("GETDATE-M"))
                {
                    DateTime dt = DateTime.Now;
                    int numAux;
                    int num = int.TryParse(formula.Substring(formula.IndexOf("-M") + 2), out numAux) ? numAux : 0;
                    valor = dt.AddMonths(-num).ToString("dd/MM/yyyy");
                }
                else if (formula.StartsWith("GETDATE-Y"))
                {
                    DateTime dt = DateTime.Now;
                    int numAux;
                    int num = int.TryParse(formula.Substring(formula.IndexOf("-Y") + 2), out numAux) ? numAux : 0;
                    valor = dt.AddYears(-num).ToString("dd/MM/yyyy");
                }
                else if (formula == "GETDATEFILENAME")
                {
                    string nombreFichero = Utils.WithoutPath(fileIn);
                    nombreFichero = nombreFichero.Substring(nombreFichero.IndexOf("_"), nombreFichero.Length - nombreFichero.IndexOf("_"));
                    Regex ex = new Regex("(0[1-9]|1[012])[-/](0[1-9]|[12][0-9]|3[01])[-/](19|20)\\d\\d");
                    Match m = ex.Match(nombreFichero);
                    if (m.Success)
                        valor = m.Value.ToString();
                    if (string.IsNullOrEmpty(valor))
                    {
                        ex = new Regex("(0[1-9]|[12][0-9]|3[01])[-/](19|20)\\d\\d");
                        m = ex.Match(nombreFichero);
                        if (m.Success)
                        {
                            valor = m.Value.ToString();
                            if (valor.Length < 7)
                                valor = "0" + valor;
                            int year = int.Parse(valor.Substring(valor.Length - 4, valor.Length - (valor.Length - 4)));
                            int month = int.Parse(valor.Substring(0, 2));
                            int day = DateTime.DaysInMonth(year, month);
                            DateTime dt = new DateTime(year, month, day);
                            valor = dt.ToString("dd/MM/yyyy");
                        }
                        if (string.IsNullOrEmpty(valor))
                        {
                            //Ejemplo: Mes = FEBRERO
                            string mes = string.Empty;
                            string strRegex = @"\bxxx|enero|febrero|marzo|abril|mayo|junio|julio|agosto|septiembre|octubre|noviembre|diciembre|xxx\b";
                            ex = new Regex(strRegex);
                            m = ex.Match(nombreFichero.ToLower());
                            if (m.Success)
                                mes = m.Value.Trim();
                            mes = Utils.MesToInt(mes).ToString().PadLeft(2, '0');

                            //Ejemplo: Año = 2014 o 14
                            string periodo = string.Empty;
                            strRegex = @"(?<Year>(?:\d{4}|\d{2}))";
                            ex = new Regex(strRegex);
                            m = ex.Match(nombreFichero);
                            if (m.Success)
                                periodo = m.Value.Trim();
                            if (periodo.Length == 2)
                                periodo = "20" + periodo;
                            DateTime dt = new DateTime(int.Parse(periodo), int.Parse(mes), DateTime.DaysInMonth(int.Parse(periodo), int.Parse(mes)));
                            valor = dt.ToString("dd/MM/yyyy");
                        }
                    }
                }
                else
                {
                    //Otras fórmulas que requieren valor anterior
                    if (!string.IsNullOrEmpty(valor))
                        valor = ObtenerValorFormula(valor, INDICADOR_FORMULA + formula.Replace("(", "").Replace(")", ""), fileIn, line, separador, isTXT, worksheetXLS, worksheetXLSX, row, db, INDICADOR_FORMULA + formula);
                }
            }
            return valor;
        }

        /// <summary>
        /// Obtener valor fichero entrada de la posición indicada
        /// </summary>
        private string ObtenerValorEntradaFinal(string formula, bool isTXT, string line, string separador, string fileIn, ISheet worksheetXLS, ExcelWorksheet worksheetXLSX, int row, ref int pos)
        {
            string valor = "";
            int.TryParse(formula.Replace(INDICADOR_POSICIONORIGEN, ""), out pos);
            if (isTXT)
                valor = Utils.tokenize(line, separador, pos, true).Trim();
            else if (fileIn.ToLower().Trim().EndsWith("xls"))
                valor = GetCellValue(worksheetXLS, row, pos).Trim();
            else
                valor = GetCellValue(worksheetXLSX, row, pos).Trim();
            return valor;
        }

        /// <summary>
        /// Obtener valor salida para cada posición de la fórmula
        /// </summary>
        private string ObtenerValorFormula(string valor, string formula, string fileIn, string line, string separador, bool isTXT,
            ISheet worksheetXLS, ExcelWorksheet worksheetXLSX, int row, Database db, string formulaNoFormateada)
        {
            int pos = -1;

            string formulaAux = formula;

            if (string.IsNullOrEmpty(valor))
            {
                //Si contiene posición origen o valor fijo + fórmula  cogemos sólo parte posición origen o valor fijo
                if (formula.Contains(";"))
                    formula = formula.Substring(0, formula.IndexOf(";"));

                if (formula.Contains(INDICADOR_VALORFIJO))
                {
                    valor = formula.Replace(INDICADOR_VALORFIJO, "");
                    if (valor.StartsWith("'") && valor.EndsWith("'"))
                        valor = valor.Substring(1, valor.Length - 2);
                }
                else
                {
                    foreach (string f in formula.Replace("||", "|").Replace("&&", "&").Split('&', '|'))
                    {
                        if (string.IsNullOrEmpty(valor.Trim()) || !formula.Contains("||"))
                        {
                            valor += ObtenerValorEntradaFinal(f, isTXT, line, separador, fileIn, worksheetXLS, worksheetXLSX, row, ref pos);
                        }
                    }
                }
            }

            formula = formulaAux;
            //Si lleva asociada una fórmula la ejecutamos
            if (formula.Contains(INDICADOR_FORMULA))
            {
                int numFormulas = formula.Replace(";", "").Replace(INDICADOR_FORMULA, ";").Split(';').Count() - 1;
                for (int i = 1; i <= numFormulas; i++)
                {
                    //Dejamos sólo la parte de la fórmula                    
                    if ((formula.ToUpper().Contains("@FSQL") || formula.ToUpper().Contains("@F=SQL") ||
                        formula.ToUpper().Contains("@FSQLIFEXIST") || formula.ToUpper().Contains("@F=SQLIFEXIST") ||
                        formula.ToUpper().Contains("@FREPLACE") || formula.ToUpper().Contains("@F=REPLACE")) && i == 1)
                        formula = formulaNoFormateada;

                    formula = formula.Substring(formula.IndexOf(INDICADOR_FORMULA) + INDICADOR_FORMULA.Length,
                            formula.Length - (formula.IndexOf(INDICADOR_FORMULA) + INDICADOR_FORMULA.Length));

                    //Si hay más de una fórmula
                    if (formula.Contains(";" + INDICADOR_FORMULA))
                    {
                        formulaAux = formula;
                        formula = formula.Substring(0, formula.IndexOf(INDICADOR_FORMULA) - 1);
                    }

                    //Dejamos sólo la fórmula sin el indicador 
                    formula = formula.Replace(INDICADOR_FORMULA, "");

                    if (formula.StartsWith("ANT"))
                    {
                        //Éstas fórmulas aplican tanto si hemos encontrado un valor previamente cómo si no..
                        if (pos != -1)
                        {
                            if (!string.IsNullOrEmpty(valor) && !hashValoresAnteriores.ContainsKey(pos.ToString()))
                                hashValoresAnteriores.Add(pos.ToString(), valor);
                            else if (hashValoresAnteriores != null && hashValoresAnteriores.ContainsKey(pos.ToString()))
                            {
                                if (!string.IsNullOrEmpty(valor))
                                    hashValoresAnteriores[pos.ToString()] = valor;
                                else
                                    valor = hashValoresAnteriores[pos.ToString()].ToString();
                            }
                        }
                    }
                    else
                    {
                        //Éstas fórmulas sólo aplican si hemos encontrado un valor en el fichero de entrada previamente
                        if (!string.IsNullOrEmpty(valor))
                        {
                            if (formula.StartsWith("PADLEFT"))
                            {
                                string sf = formula.Replace("PADLEFT", "");
                                if (sf.StartsWith("(") && sf.EndsWith(")"))
                                    sf = sf.Substring(1, sf.Length - 2);
                                int p = int.Parse(sf.Substring(0, sf.IndexOf(",")));
                                char s = ' ';
                                if (formula.IndexOf("'") > -1)
                                    s = formula.Substring(formula.IndexOf("'") + 1, 1)[0];
                                else
                                    s = formula.Substring(formula.IndexOf(",") + 1, 1)[0];
                                valor = valor.PadLeft(p, s);
                            }
                            else if (formula.StartsWith("PADRIGHT"))
                            {
                                int p = int.Parse(formula.Replace("PADRIGHT", "").Substring(0, formula.Replace("PADRIGHT", "").IndexOf(",")));
                                char s = ' ';
                                if (formula.IndexOf("'") > -1)
                                    s = formula.Substring(formula.IndexOf("'") + 1, 1)[0];
                                else
                                    s = formula.Substring(formula.IndexOf(",") + 1, 1)[0];
                                valor = valor.PadRight(p, s);
                            }
                            else if (formula.StartsWith("LEFT"))
                            {
                                int p = int.Parse(formula.Replace("LEFT", ""));
                                if (valor.Length >= p)
                                    valor = valor.Substring(0, p);
                            }
                            else if (formula.StartsWith("RIGHT"))
                            {
                                int p = int.Parse(formula.Replace("RIGHT", ""));
                                if (valor.Length >= p)
                                    valor = valor.Substring(valor.Length - p, valor.Length - (valor.Length - p));
                            }
                            else if (formula.StartsWith("ROUND"))
                            {
                                int decimales = int.Parse(formula.Replace("ROUND", ""));
                                double d;
                                if (double.TryParse(valor, out d))
                                    valor = Math.Round(d, decimales).ToString();
                            }
                            else if (formula.StartsWith("CODE"))
                            {
                                int l = 0;
                                int.TryParse(formula.Replace("CODE", ""), out l);
                                if (l > 0)
                                    valor = ObtenerCodigoUnicoLongitud(valor, l);
                                else
                                    valor = ObtenerCodigoUnico(valor);
                            }
                            else if (formula.StartsWith("AUTOINC"))
                            {
                                valor = GetRowNum(valor);
                            }
                            else if (formula.StartsWith("MONTHNUMBER"))
                            {
                                valor = ObtenerNumeroMes(valor);
                            }
                            else if (formula.StartsWith("ONLYNUMBERS"))
                            {
                                string pattern = @"\d";
                                StringBuilder sb = new StringBuilder();
                                foreach (Match m in Regex.Matches(valor, pattern))
                                    sb.Append(m);
                                valor = sb.ToString();
                            }
                            else if (formula.StartsWith("ONLYCHARS"))
                            {
                                string pattern = @"[a-zA-ZáéíóúÁÉÍÓÚàèìòùÀÈÌÒÙÑñ]";
                                StringBuilder sb = new StringBuilder();
                                foreach (Match m in Regex.Matches(valor, pattern))
                                    sb.Append(m);
                                valor = sb.ToString();
                            }
                            else if (formula.StartsWith("EXTRACT"))
                            {
                                char separadorCampo = char.Parse(formula.Replace("EXTRACT", "").Substring(0, 1));
                                int p = int.Parse(formula.Replace("EXTRACT", "").Substring(formula.Replace("EXTRACT", "").LastIndexOf(";") + 1, 1));
                                try
                                {
                                    valor = valor.Split(separadorCampo)[p - 1].Trim();
                                }
                                catch { valor = ""; }
                            }
                            else if (formula.StartsWith("SUBSTRING"))
                            {
                                int start = int.Parse(formula.Replace("SUBSTRING", "").Substring(0, formula.Replace("SUBSTRING", "").IndexOf(",")));
                                int len = int.Parse(formula.Replace("SUBSTRING", "").Substring(formula.Replace("SUBSTRING", "").IndexOf(",") + 1, formula.Replace("SUBSTRING", "").Length - (formula.Replace("SUBSTRING", "").IndexOf(",") + 1)));
                                if (start + len > valor.Length)
                                    valor = valor.Substring(start, valor.Length - start);
                                else
                                    valor = valor.Substring(start, len);
                            }
                            else if (formula.StartsWith("TRANSCRIBE"))
                            {
                                //Cargamos transcripciones sólo una vez
                                if (hashTranscribe == null || hashTranscribe.Count <= 0)
                                    CargarTranscripciones();
                                string valAnt = valor;
                                string val = valor;
                                valor = "";
                                foreach (char c in val)
                                {
                                    if (hashTranscribe.ContainsKey(c.ToString()))
                                        valor += hashTranscribe[c.ToString()].ToString();
                                    else
                                        valor += c.ToString();
                                }
                            }
                            else if (formula.StartsWith("REPLACE"))
                            {
                                string s = formula.Replace("REPLACE", "");
                                if (s.StartsWith("(") && s.EndsWith(")"))
                                    s = s.Substring(1, s.Length - 2);
                                string s1 = s.Substring(1, s.Length - 1);
                                s1 = s1.Substring(0, s1.IndexOf("'"));
                                string s2 = s.Replace(s1, "").Replace("'", "");
                                s2 = (s2.Length > 1 && s2.StartsWith(",")) ? s2.Substring(1, s2.Length - 1) : s2;
                                s2 = (s2.Length == 1 && s2 == ",") ? "" : s2;
                                valor = valor.Replace(s1, s2);
                            }
                            else if (formula.StartsWith("SQLIFEXIST"))
                            {
                                string s = formula.Replace("SQLIFEXIST", "").Trim();
                                s = s.Replace(INDICADOR_PARAM_1, "'" + valor.Replace("'", "''") + "'");
                                string valorAux = EjecutarSQL(db, s);
                                if (!string.IsNullOrEmpty(valorAux.Trim()))
                                    valor = valorAux;
                            }
                            else if (formula.StartsWith("SQL"))
                            {
                                string s = formula.Replace("SQL", "").Trim();
                                s = s.Replace(INDICADOR_PARAM_1, "'" + valor.Replace("'", "''") + "'");
                                valor = EjecutarSQL(db, s);
                            }
                            else if (formula.StartsWith("TRIMSTART"))
                            {
                                string s = formula.Replace("TRIMSTART", "");
                                if (s.StartsWith("(") && s.EndsWith(")"))
                                    s = s.Substring(1, s.Length - 2);
                                char c = s[0];
                                valor = valor.TrimStart(c);
                            }
                            else if (formula.StartsWith("TRIMEND"))
                            {
                                char c = formula.Replace("TRIMEND", "")[0];
                                valor = valor.TrimEnd(c);
                            }
                            else if (formula.StartsWith("TRIM"))
                            {
                                valor = valor.Trim();
                            }
                            else if (formula.StartsWith("LEN"))
                            {
                                valor = valor.Trim().Length.ToString();
                            }
                        }
                        //Fórmulas que pueden aplicarse aunque el valor obtenido esté en blanco
                        if (formula.StartsWith("CASE"))
                        {
                            string formulaFormat = formula.Replace("CASE", "");
                            if (formulaFormat.StartsWith("(") && formulaFormat.EndsWith(")"))
                                formulaFormat = formulaFormat.Substring(1, formulaFormat.Length - 2);
                            string[] cases = formulaFormat.Split(';');
                            foreach (string c in cases)
                            {
                                if (c.Contains(","))
                                {
                                    if (c.Substring(0, c.IndexOf(",")).Replace(",", "").Trim().ToUpper() == valor.Trim().ToUpper())
                                    {
                                        valor = c.Substring(c.IndexOf(",") + 1, c.Length - (c.IndexOf(",") + 1));
                                        break;
                                    }
                                }
                                else
                                {
                                    valor = c;
                                    break;
                                }
                            }
                            if (valor.Contains(INDICADOR_POSICIONORIGEN))
                                valor = ObtenerValorEntradaFinal(valor, isTXT, line, separador, fileIn, worksheetXLS, worksheetXLSX, row, ref pos);
                        }
                    }
                    formula = formulaAux;
                }
            }
            return valor;
        }

        /// <summary>
        /// Cargar transcripciones
        /// </summary>
        private void CargarTranscripciones()
        {
            DbDataReader cursor = null;
            Database db = Globals.GetInstance().GetDatabase();
            try
            {
                string sql = "select CaracterCirilico, TranscripcionLatin from Alfabetos";
                cursor = db.GetDataReader(sql);
                hashTranscribe.Clear();
                string clave = "";
                while (cursor.Read())
                {
                    clave = db.GetFieldValue(cursor, 0);
                    if (!hashTranscribe.ContainsKey(clave))
                        hashTranscribe.Add(clave, db.GetFieldValue(cursor, 1));
                }
            }
            finally
            {
                if (cursor != null)
                    cursor.Close();
            }
        }

        /// <summary>
        /// Validar incompatibilidad con operaciones aritméticas
        /// </summary>
        private string IncompatibilidadAritmeticaSimbolos(char c)
        {
            string simbolo = "";
            if (c == '+')
                simbolo = CHAR_SUMA;
            else if (c == '-')
                simbolo = CHAR_REST;
            else if (c == '*')
                simbolo = CHAR_MULT;
            else if (c == '/')
                simbolo = CHAR_DIV;
            return simbolo;
        }

        private string IncompatibilidadAritmetica(string formula, ref bool incompatibilidadAritmetica)
        {
            //Sustituimos operadores si estan dentro de una fórmula o dentro de un valor fijo para que no aplique..
            int inA = 0;
            int inF = 0;
            int row = 0;
            string formulaLast = "";
            bool guardar;
            //Primero tratamos posibles excepciones
            // EJEMPLO : @VF=01-01-2013
            //           Puede ser que indiquemos símbolo aritmético en un valor fijo.
            //           En éste caso tampoco tiene sentido que éste valor aplique una operación con otro campo o valor.            
            if (!formula.Contains(INDICADOR_POSICIONORIGEN) &&
                !formula.Contains(INDICADOR_FORMULA) &&
                 formula.Contains(INDICADOR_VALORFIJO) &&
                 formula.Replace(INDICADOR_VALORFIJO, '\u2021'.ToString()).Split('\u2021').Length - 1 == 1 &&
                ((formula.Contains("+") || formula.Contains("-") || formula.Contains("*") || formula.Contains("/"))))
            {
                foreach (char c in formula)
                {
                    guardar = true;
                    string simbolo = IncompatibilidadAritmeticaSimbolos(c);
                    if (!string.IsNullOrEmpty(simbolo))
                    {
                        formulaLast += simbolo;
                        guardar = false;
                    }
                    if (guardar)
                        formulaLast += c;
                }
            }
            else //Otros casos (si hay operadores dentro de una fórmula)
            {
                foreach (char c in formula)
                {
                    guardar = true;
                    if (inF == 1)
                    {
                        string simbolo = IncompatibilidadAritmeticaSimbolos(c);
                        if (!string.IsNullOrEmpty(simbolo))
                        {
                            formulaLast += simbolo;
                            guardar = false;
                        }
                    }
                    if (c == ')') //Termina una fórmula
                        inF = 0;
                    if (c == '@')
                    {
                        inA = row;
                        inF = 0;
                    }
                    if (inA + 1 == row && c == 'F') //Empieza una fórmula
                        inF = 1;
                    if (guardar)
                        formulaLast += c;
                    row++;
                }
            }
            incompatibilidadAritmetica = !formulaLast.Contains("+") && !formulaLast.Contains("-") && !formulaLast.Contains("*") && !formulaLast.Contains("/");
            return formulaLast;
        }

        /// <summary>
        /// Obtener valor salida
        /// </summary>
        private string ObtenerValor(string formula, string line, string separador, string separadorDestino, bool isTXT, string fileIn,
            string ficheroOrigen, ISheet worksheetXLS, ExcelWorksheet worksheetXLSX, int row, string agent, Database db)
        {
            string valor = "";
            try
            {
                if (!string.IsNullOrEmpty(formula))
                {
                    //Limpiamos fórmula                          
                    if (!(formula.ToUpper().Contains("@FSQL") || formula.ToUpper().Contains("@F=SQL") ||
                          formula.ToUpper().Contains("@FSQLIFEXIST") || formula.ToUpper().Contains("@F=SQLIFEXIST") ||
                          formula.ToUpper().Contains("@FREPLACE") || formula.ToUpper().Contains("@F=REPLACE")))
                    {
                        if (formula.StartsWith(INDICADOR_VALORFIJO) && formula.Substring(1, formula.Length - 1).IndexOf("@") == -1)
                            formula = formula.Replace("=", "").Trim();
                        else
                            formula = formula.Replace("=", "").Trim().ToUpper();
                    }

                    //Si la fórmula contiene ;; aplicaremos última fórmula a todo lo calculado anteriormente..
                    //Ej: @PO1;@FLEFT(2)&&@PO2;@FLEFT(2);;@FRIGHT(3)
                    string ultimaFormula = "";
                    if (formula.Contains(";;"))
                    {
                        ultimaFormula = formula.Substring(formula.IndexOf(";;") + ";;".Length, formula.Length - (formula.IndexOf(";;") + ";;".Length));
                        formula = formula.Substring(0, formula.IndexOf(";;"));
                    }

                    //Posición fichero origen o valores fijos
                    if (formula.ToUpper().Contains(INDICADOR_POSICIONORIGEN) || formula.ToUpper().Contains(INDICADOR_VALORFIJO))
                    {
                        string[] po_vf_list = null;
                        string operador = "";

                        //Analizamos fórmula
                        if (formula.Contains("&&"))       //Si contiene "&&" concatenamos valores
                        {
                            //Si el símbolo && está entre parentesis quiere decir que primero debemos aplicar AND y después aplicar la fórmula asociada al valor obtenido
                            if ((formula.IndexOf("&&") > -1 && formula.IndexOf("(") > -1 && formula.IndexOf(")") > -1) &&
                                (formula.IndexOf("&&") >= formula.IndexOf("(") && formula.IndexOf("&&") <= formula.IndexOf(")")) && (formula.Contains(INDICADOR_FORMULA)))
                                po_vf_list = formula.Replace("||", "|").Split('|'); //Para evitar split lo cojemos todo
                            else
                            {
                                operador = "&&";
                                po_vf_list = formula.Replace("&&", "&").Split('&');
                            }
                        }
                        else                              //En caso contrario consideramos operador "||" excluimos (un único valor de varios posibles según orden) (nos sirve por todos los otros)
                        {
                            //Si el símbolo || está entre parentesis quiere decir que primero debemos aplicar OR y después aplicar la fórmula asociada al valor obtenido
                            if ((formula.IndexOf("||") > -1 && formula.IndexOf("(") > -1 && formula.IndexOf(")") > -1) &&
                                (formula.IndexOf("||") >= formula.IndexOf("(") && formula.IndexOf("||") <= formula.IndexOf(")")) && (formula.Contains(INDICADOR_FORMULA)))
                                po_vf_list = formula.Replace("&&", "&").Split('&'); //Para evitar split lo cojemos todo
                            else
                            {
                                operador = "||";
                                po_vf_list = formula.Replace("||", "|").Split('|');
                            }
                        }

                        if (po_vf_list != null)
                        {
                            string pAux;
                            foreach (string p in po_vf_list)
                            {
                                //Controlamos los posibles casos incompatibles con operaciones aritmeticas para sustituir sus valores antes de hacer el split.
                                bool incompatibilidadAritmetica = false;
                                if (p.Trim().StartsWith(INDICADOR_VALORFIJO) && p.Trim().Substring(1, p.Trim().Length - 1).IndexOf("@") == -1)
                                    pAux = IncompatibilidadAritmetica(p.Trim(), ref incompatibilidadAritmetica);
                                else
                                    pAux = IncompatibilidadAritmetica(p.Trim().ToUpper(), ref incompatibilidadAritmetica);
                                string[] vals;
                                vals = pAux.Split('+', '-', '*', '/');
                                string[] valsCalc = new string[vals.Length];
                                if (string.IsNullOrEmpty(valor.Trim()) || operador != "||")
                                {
                                    int i = 0;
                                    bool dejarParentesis = ((!pAux.Contains(INDICADOR_FORMULA)) && (pAux.Contains("(") || pAux.Contains(")"))); //La posible operación aritmética con parentesis es incompatible por ahora con una o más fórmulas..
                                    foreach (string v in vals)
                                    {
                                        if (v.Contains(INDICADOR_VALORFIJO) || v.Contains(INDICADOR_POSICIONORIGEN) || v.Contains(INDICADOR_FORMULA))
                                        {
                                            string vAux = v.Replace("=", "").Replace("(", "").Replace(")", "").Replace(CHAR_DIV, "/").Replace(CHAR_MULT, "*").Replace(CHAR_REST, "-").Replace(CHAR_SUMA, "+");
                                            if (vAux.StartsWith(INDICADOR_FORMULA) && !vAux.Contains(INDICADOR_VALORFIJO) && !vAux.Contains(INDICADOR_POSICIONORIGEN))
                                                valsCalc[i] = TratarFormulaSola(ref vAux, fileIn, line, separador, isTXT, worksheetXLS, worksheetXLSX, row, db);
                                            else
                                                valsCalc[i] = ObtenerValorFormula(string.Empty, vAux, fileIn, line, separador, isTXT, worksheetXLS, worksheetXLSX, row, db, pAux.Replace(CHAR_DIV, "/").Replace(CHAR_MULT, "*").Replace(CHAR_REST, "-").Replace(CHAR_SUMA, "+"));
                                            //Controlamos números terminados en . (por ejemplo 24. -> 24.0 -> 24)
                                            if ((!incompatibilidadAritmetica) &&
                                                (p.Contains("+") || p.Contains("-") || p.Contains("*") || p.Contains("/")))
                                            {
                                                if (valsCalc[i].Trim().EndsWith("."))
                                                {
                                                    valsCalc[i] = valsCalc[i].Substring(0, valsCalc[i].Length - 1);
                                                    if (string.IsNullOrEmpty(valsCalc[i]))
                                                        valsCalc[i] = "0";
                                                }
                                            }
                                            if (!incompatibilidadAritmetica && dejarParentesis)
                                                pAux = pAux.Replace(v.Replace("(", "").Replace(")", ""), valsCalc[i]);
                                            else
                                                pAux = pAux.Replace(vals[i], valsCalc[i]);
                                        }
                                        i++;
                                    }
                                    if ((!incompatibilidadAritmetica) &&
                                         (p.Contains("+") || p.Contains("-") || p.Contains("*") || p.Contains("/")))
                                    {
                                        Expression e = new Expression(pAux.Replace(",", "."));
                                        Object d = e.Evaluate();
                                        valor += d.ToString();
                                    }
                                    else
                                        valor += pAux;
                                }
                            }
                        }
                    }
                    else if (formula.Contains(INDICADOR_FORMULA)) //Aquí sólo tratamos fórmulas sólas. Las fórmulas que aplican a un valor obtenido ya se han tratado en la condición anterior.
                    {
                        valor = TratarFormulaSola(ref formula, fileIn, line, separador, isTXT, worksheetXLS, worksheetXLSX, row, db);
                    }
                    if (!string.IsNullOrEmpty(ultimaFormula))
                    {
                        valor = ObtenerValorFormula(valor, ultimaFormula, fileIn, line, separador, isTXT, worksheetXLS, worksheetXLSX, row, db, ultimaFormula);
                    }
                }
                else //En caso de que no contenga fórmula cojemos todo la línea del fichero de entrada o con cambios a nivel cabecera
                {
                    if (separador != separadorDestino && isTXT)
                    {
                        if (separador.Length == 1)
                        {
                            int num = line.Split(separador[0]).Length;
                            for (int i = 1; i <= num; i++)
                                valor += (Utils.tokenize(line, separador, i).Trim() + separadorDestino);
                        }
                        else
                            valor = line.Replace(separador, separadorDestino);
                    }
                    else
                        valor = line;
                }
            }
            catch
            {
                string msg = "Error al intentar obtener el valor del fichero de entrada " + fileIn + " a partir de la fórmula: " + formula;
                throw new Exception(msg);
            }
            if (valor == "NeuN")
                valor = "";
            return valor;
        }

        /// <summary>
        /// Comprobar que cumpla la condición para procesar la línea en curso
        /// </summary>
        private bool CumpleCondicion(string condicionProcesado, string line, string separador, string separadorDestino, bool isTXT, string fileIn,
            string ficheroOrigen, ISheet worksheetXLS, ExcelWorksheet worksheetXLSX, int row, string agent, Database db)
        {
            bool cumpleCondicion = true;
            bool cumpleCondicionAND = true;

            string valor = "";
            string valorFichero = "";

            string[] condicionesAND = condicionProcesado.Replace("&&", "&").Split('&');

            //De moment fem que el símbol predominant sigui l'&&, si més endavant és necessari podríem definir nou camp per indicar quina condició a de predominar (and o or)                    
            foreach (string c in condicionesAND)
            {
                string[] subcondicionesOR = c.Replace("||", "|").Split('|');
                int i = 0;
                foreach (string cAux in subcondicionesOR)
                {
                    if (cAux.Contains("!="))
                    {
                        valor = cAux.Substring(cAux.IndexOf("!=") + 2, cAux.Length - (cAux.IndexOf("!=") + 2));
                        if (valor.Contains("''") || valor.Contains("\"\""))
                            valor = valor.Replace("''", "").Replace("\"\"", "");
                        valorFichero = ObtenerValor(cAux.Substring(0, cAux.IndexOf("!=")), line, separador, separadorDestino, isTXT, fileIn, ficheroOrigen, worksheetXLS, worksheetXLSX, row, agent, db);
                        if (valor.ToUpper().Contains(INDICADOR_FORMULA))
                            valor = ObtenerValor(valor, line, separador, separadorDestino, isTXT, fileIn, ficheroOrigen, worksheetXLS, worksheetXLSX, row, agent, db);
                        if (i == 0)
                            cumpleCondicion = (valorFichero.Trim() != valor.Trim());
                        else
                            cumpleCondicion = cumpleCondicion || (valorFichero.Trim() != valor.Trim());
                    }
                    else if (cAux.Contains("="))
                    {
                        valor = cAux.Substring(cAux.IndexOf("=") + 1, cAux.Length - (cAux.IndexOf("=") + 1));
                        if (valor.Contains("''") || valor.Contains("\"\""))
                            valor = valor.Replace("''", "").Replace("\"\"", "");
                        valorFichero = ObtenerValor(cAux.Substring(0, cAux.IndexOf("=")), line, separador, separadorDestino, isTXT, fileIn, ficheroOrigen, worksheetXLS, worksheetXLSX, row, agent, db);
                        if (valor.ToUpper().Contains(INDICADOR_FORMULA))
                            valor = ObtenerValor(valor, line, separador, separadorDestino, isTXT, fileIn, ficheroOrigen, worksheetXLS, worksheetXLSX, row, agent, db);
                        if (i == 0)
                            cumpleCondicion = (valorFichero.Trim() == valor.Trim());
                        else
                            cumpleCondicion = cumpleCondicion || (valorFichero.Trim() == valor.Trim());
                    }
                    else if (cAux.Contains(">") || cAux.Contains("<"))
                    {
                        string simbolo = cAux.Contains(">") ? ">" : "<";
                        valor = cAux.Substring(cAux.IndexOf(simbolo) + 1, cAux.Length - (cAux.IndexOf(simbolo) + 1));
                        if (valor.Contains("''") || valor.Contains("\"\""))
                            valor = valor.Replace("''", "").Replace("\"\"", "");
                        valorFichero = ObtenerValor(cAux.Substring(0, cAux.IndexOf(simbolo)), line, separador, separadorDestino, isTXT, fileIn, ficheroOrigen, worksheetXLS, worksheetXLSX, row, agent, db);
                        if (valor.ToUpper().Contains(INDICADOR_FORMULA))
                            valor = ObtenerValor(valor, line, separador, separadorDestino, isTXT, fileIn, ficheroOrigen, worksheetXLS, worksheetXLSX, row, agent, db);
                        try
                        {
                            if (i == 0)
                            {
                                if (simbolo == ">")
                                    cumpleCondicion = (double.Parse(valorFichero.Trim()) > double.Parse(valor.Trim()));
                                else
                                    cumpleCondicion = (double.Parse(valorFichero.Trim()) < double.Parse(valor.Trim()));
                            }
                            else
                            {
                                if (simbolo == ">")
                                    cumpleCondicion = cumpleCondicion || (double.Parse(valorFichero.Trim()) > double.Parse(valor.Trim()));
                                else
                                    cumpleCondicion = cumpleCondicion || (double.Parse(valorFichero.Trim()) < double.Parse(valor.Trim()));
                            }
                        }
                        catch { cumpleCondicion = false; }
                    }
                    else if (cAux.ToLower().Contains("!isnumeric"))
                    {
                        valorFichero = ObtenerValor(cAux.ToLower().Replace("!isnumeric", "").Replace("(", "").Replace(")", "").Trim(), line, separador, separadorDestino, isTXT, fileIn, ficheroOrigen, worksheetXLS, worksheetXLSX, row, agent, db);
                        double d;
                        if (i == 0)
                            cumpleCondicion = !double.TryParse(valorFichero, out d);
                        else
                            cumpleCondicion = cumpleCondicion || !double.TryParse(valorFichero, out d);
                    }
                    else if (cAux.ToLower().Contains("isnumeric"))
                    {
                        valorFichero = ObtenerValor(cAux.ToLower().Replace("isnumeric", "").Replace("(", "").Replace(")", "").Trim(), line, separador, separadorDestino, isTXT, fileIn, ficheroOrigen, worksheetXLS, worksheetXLSX, row, agent, db);
                        double d;
                        if (i == 0)
                            cumpleCondicion = double.TryParse(valorFichero, out d);
                        else
                            cumpleCondicion = cumpleCondicion || double.TryParse(valorFichero, out d);
                    }
                    i++;
                }
                cumpleCondicionAND = cumpleCondicionAND && cumpleCondicion;
            }
            cumpleCondicion = cumpleCondicionAND;

            return cumpleCondicion;
        }

        /// <summary>
        /// Get a cell value
        /// </summary>
        /// <param name="range"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        private static string GetCellValue(ISheet worksheet, int row, string col)
        {
            int letter = (int)'A';
            int iCol = 0;
            if (col.Length == 1)
            {
                iCol = (int)col[0];
            }
            else
            {
                int desfase = 0;
                if (col.Substring(0, 1) == "A") desfase = 26;
                if (col.Substring(0, 1) == "B") desfase = 52;
                if (col.Substring(0, 1) == "C") desfase = 78;
                if (col.Substring(0, 1) == "D") desfase = 104;
                if (col.Substring(0, 1) == "E") desfase = 130;
                if (col.Substring(0, 1) == "F") desfase = 156;
                if (col.Substring(0, 1) == "G") desfase = 182;
                iCol = desfase + (int)col[1];
            }
            return GetCellValue(worksheet, row, iCol - letter + 1);
        }
        /// <summary>
        /// Get a cell value
        /// </summary>
        /// <param name="range"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        private static string GetCellValue(ISheet worksheet, int row, int col)
        {
            if (worksheet.GetRow(row - 1) == null)
                return "";
            else
            {
                if (worksheet.GetRow(row - 1).GetCell(col - 1) == null)
                    return "";
                else
                {
                    if (worksheet.GetRow(row - 1).GetCell(col - 1).CellType == CellType.FORMULA)
                    {
                        if (worksheet.GetRow(row - 1).GetCell(col - 1).CachedFormulaResultType == CellType.NUMERIC)
                            return worksheet.GetRow(row - 1).GetCell(col - 1).NumericCellValue.ToString();
                        else if (worksheet.GetRow(row - 1).GetCell(col - 1).CachedFormulaResultType == CellType.BOOLEAN)
                            return worksheet.GetRow(row - 1).GetCell(col - 1).BooleanCellValue.ToString();
                        else
                            return worksheet.GetRow(row - 1).GetCell(col - 1).StringCellValue;
                    }
                    else
                    {
                        if (worksheet.GetRow(row - 1).GetCell(col - 1).CellType == CellType.NUMERIC)
                        {
                            if (worksheet.GetRow(row - 1).GetCell(col - 1).ToString().Contains("/") ||
                                    (worksheet.GetRow(row - 1).GetCell(col - 1).ToString().Contains("-") &&
                                    !worksheet.GetRow(row - 1).GetCell(col - 1).ToString().StartsWith("-") &&
                                    !worksheet.GetRow(row - 1).GetCell(col - 1).ToString().EndsWith("-")))
                                return worksheet.GetRow(row - 1).GetCell(col - 1).DateCellValue.ToShortDateString();
                            else
                                return worksheet.GetRow(row - 1).GetCell(col - 1).NumericCellValue.ToString();
                        }
                        else
                            return worksheet.GetRow(row - 1).GetCell(col - 1).ToString();
                    }
                }
            }
        }

        //IDEM for xlsx files
        private static string GetCellValue(ExcelWorksheet worksheet, int row, string col)
        {
            int letter = (int)'A';
            int iCol = 0;
            if (col.Length == 1)
            {
                iCol = (int)col[0];
            }
            else
            {
                int desfase = 0;
                if (col.Substring(0, 1) == "A") desfase = 26;
                if (col.Substring(0, 1) == "B") desfase = 52;
                if (col.Substring(0, 1) == "C") desfase = 78;
                if (col.Substring(0, 1) == "D") desfase = 104;
                if (col.Substring(0, 1) == "E") desfase = 130;
                if (col.Substring(0, 1) == "F") desfase = 156;
                if (col.Substring(0, 1) == "G") desfase = 182;
                iCol = desfase + (int)col[1];
            }
            return GetCellValue(worksheet, row, iCol - letter + 1);
        }

        private static string GetCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            string v = (worksheet.Cells[row, col].Value == null ? "" : worksheet.Cells[row, col].Value.ToString());
            int i;
            if (!string.IsNullOrEmpty(v))
            {
                if ((worksheet.Cells[row, col].Style.Numberformat.Format.ToString() == "dd\\-mm\\-yyyy" ||
                     worksheet.Cells[row, col].Style.Numberformat.Format.ToString() == "dd\\-mmm\\-yy"
                    ) && int.TryParse(v, out i))
                {
                    DateTime dateOfReference = new DateTime(1900, 1, 1);
                    if (i > 60d)
                        i = i - 2;
                    else
                        i = i - 1;
                    v = dateOfReference.AddDays(i).ToShortDateString();
                }
            }
            return v;
        }

        /// <summary>
        /// Comprueba si hay datos en las siguientes filas
        /// </summary>
        /// <param name="worksheet">worksheet</param>        
        /// <param name="row">row</param>       
        private static bool hayDatos(ExcelWorksheet worksheet, int row)
        {
            bool datos = false;
            for (int i = row; i <= row + 50; i++)
            {
                for (int j = 1; j <= 15; j++)
                {
                    datos = datos || worksheet.Cells[i, j].Value != null;
                    if (datos) break;
                }
                if (datos) break;
            }
            return datos;
        }

        /// <summary>
        /// Obtener código único a partir de una descripción
        /// </summary>
        /// <param name="desc">descripción</param>        
        private string ObtenerCodigoUnico(string cadena)
        {
            string codigo = "";
            int sum = 0;
            for (int i = 0; i < cadena.Length; i++)
                sum += Encoding.ASCII.GetBytes(cadena[i].ToString())[0];
            codigo = cadena.Length >= 3 ? cadena.Trim().Substring(0, 3).Trim() + sum.ToString() : cadena.Trim().Substring(0, 1).Trim() + sum.ToString();
            return codigo;
        }

        /// <summary>
        /// Obtener código único de la longitud deseada a partir de una descripción 
        /// </summary>
        /// <param name="desc">descripción</param>   
        private string ObtenerCodigoUnicoLongitud(string cadena, int longitud)
        {
            string codigo = "";
            if (!string.IsNullOrEmpty(cadena.Trim()))
            {
                int sum = 0;
                string prefijo = "";
                string prefijoFinal = "";
                string prefijoAnt = "";
                string prefijo3 = "";
                string prefijoFinal3 = "";
                string prefijoAnt3 = "";
                int palabras = 1;
                for (int i = 0; i < cadena.Length; i++)
                {
                    if (string.IsNullOrEmpty(prefijo) || prefijoAnt.Length < 1)
                    {
                        prefijoFinal += cadena[i].ToString().ToUpper().Trim();
                        prefijo += cadena[i].ToString().ToUpper().Trim();
                        prefijoAnt = prefijo;
                    }
                    if (string.IsNullOrEmpty(prefijo3) || prefijoAnt3.Length < 3)
                    {
                        prefijoFinal3 += cadena[i].ToString().ToUpper().Trim();
                        prefijo3 += cadena[i].ToString().ToUpper().Trim();
                        prefijoAnt3 = prefijo3;
                    }
                    if (cadena[i] == ' ')
                    {
                        prefijo = "";
                        prefijoAnt = "";
                        prefijo3 = "";
                        prefijoAnt3 = "";
                        palabras++;
                    }
                    sum += (Encoding.ASCII.GetBytes(cadena[i].ToString())[0] + i);
                }
                if (palabras <= 4)
                    prefijoFinal = prefijoFinal3;
                else
                    prefijoFinal = cadena.Trim().ToUpper().Substring(0, 3) + cadena.Trim().ToUpper().Substring(cadena.Trim().Length - 3, cadena.Trim().Length - (cadena.Trim().Length - 3)) + prefijoFinal;
                codigo = prefijoFinal + sum.ToString();
                if (codigo.Length > longitud)
                    codigo = prefijoFinal.Substring(0, longitud - sum.ToString().Length) + sum.ToString();
            }
            return codigo;
        }

        /// <summary>
        /// Obtener número de mes a partir de una descripción
        /// </summary>
        /// <param name="desc">descripción</param>        
        private string ObtenerNumeroMes(string desc)
        {
            string numero = "";
            desc = desc.Trim().ToLower();
            if (desc.Contains("jan") || desc.Contains("ene") || desc.Contains("gen")) numero = "01";
            else if (desc.Contains("feb")) numero = "02";
            else if (desc.Contains("mar")) numero = "03";
            else if (desc.Contains("apr") || desc.Contains("abr")) numero = "04";
            else if (desc.Contains("may") || desc.Contains("mai")) numero = "05";
            else if (desc.Contains("jun")) numero = "06";
            else if (desc.Contains("jul")) numero = "07";
            else if (desc.Contains("aug") || desc.Contains("ago")) numero = "08";
            else if (desc.Contains("sep") || desc.Contains("set")) numero = "09";
            else if (desc.Contains("oct")) numero = "10";
            else if (desc.Contains("nov")) numero = "11";
            if (desc.Contains("dec") || desc.Contains("dic") || desc.Contains("des")) numero = "12";
            return numero;
        }

        /// <summary>
        /// Get the row number
        /// </summary>
        /// <param name="id">order id</param>
        /// <returns>row number</returns>
        private string GetRowNum(string id)
        {
            string val = "1";
            if (hashRowCounter.ContainsKey(id))
            {
                val = (string)hashRowCounter[id];
                Int32 x = Int32.Parse(val);
                x++;
                val = x.ToString();
                hashRowCounter[id] = val;
            }
            else
                hashRowCounter.Add(id, val);
            return val;
        }
        /// <summary>
        /// Ejecutar consulta SQL
        /// </summary>
        /// <param name="db">database</param>        
        /// <param name="sql">sql</param>                
        private string EjecutarSQL(Database db, string sql)
        {
            string s = "";
            DbDataReader cursor = null;
            try
            {
                cursor = db.GetDataReader(sql);
                if (cursor.Read())
                    s = db.GetFieldValue(cursor, 0);
            }
            finally
            {
                if (cursor != null)
                    cursor.Close();
            }
            return s;
        }
        /// <summary>
        /// Inicializar ficheros origen esperados para el agente
        /// </summary>
        /// <param name="db">database</param>        
        /// <param name="agent">agente</param>                
        private DataTable ObtenerFicherosOrigenConversorUniversal(Database db, string agent)
        {
            DataTable files = new DataTable();
            DbDataReader cursor = null;
            try
            {
                string sql = "select distinct FicheroOrigen from CfgConversores " +
                             " where IdcAgente = " + agent;
                cursor = db.GetDataReader(sql);
                files.Load(cursor);
            }
            finally
            {
                if (cursor != null)
                    cursor.Close();
            }
            return files;
        }

        /// <summary>
        /// Inicializar valores de configuración del conversor universal para el agente y el fichero origen recibido
        /// </summary>
        /// <param name="db">database</param>        
        /// <param name="agent">agente</param>      
        /// <param name="agent">file</param>      
        private DataTable ObtenerValoresConversorUniversal(Database db, string agent, string file)
        {
            DataTable values = new DataTable();
            DbDataReader cursor = null;
            try
            {
                string sql = "SELECT cu.FicheroOrigen " +
                             "      ,cu.FicheroDestino " +
                             "      ,cu.Pestanya " +
                             "      ,cu.NumeroLineaInicial " +
                             "      ,cu.TextoAnteriorPrimeraLinea " +
                             "      ,cu.SeparadorFicheroOrigen " +
                             "      ,cu.SeparadorFicheroDestino " +
                             "      ,cu.CondicionProcesado " +
                             "      ,cu.Status " +
                             "      ,cu.IdcAgenteDestino " +
                             "      ,cu.Append " +
                             "      ,cu.EliminarLineasRepetidas " +
                             "      ,cu.Formula as FormulaCabecera " +
                             "      ,cud.Formula " +
                             "      ,cud.PosicionDestino " +
                             "  FROM CfgConversores cu " +
                             "  LEFT JOIN CfgConversoresDetalle cud ON cu.ID = cud.ID " +
                             " WHERE cu.IdcAgente = " + agent +
                             "   AND cu.FicheroOrigen = '" + file + "'" +
                             " ORDER BY cu.Orden ASC ";
                cursor = db.GetDataReader(sql);
                values.Load(cursor);
            }
            finally
            {
                if (cursor != null)
                    cursor.Close();
            }
            return values;
        }

        /// <summary>
        /// Obtener por un fichero origen tipo Excel el número de pestañas que trata
        /// </summary>
        /// <param name="db">database</param>        
        /// <param name="agent">agente</param>      
        /// <param name="agent">file</param>      
        private DataTable ObtenerFicheroPestanya(Database db, string agent, string file)
        {
            DataTable values = new DataTable();
            DbDataReader cursor = null;
            try
            {
                string sql = "SELECT cu.Pestanya, coalesce(min(cu.NumeroLineaInicial),1) as NumeroLineaInicial " +
                             "  FROM CfgConversores cu " +
                             " WHERE cu.IdcAgente = " + agent +
                             "   AND cu.FicheroOrigen = '" + file + "'" +
                             " GROUP BY cu.Pestanya";
                cursor = db.GetDataReader(sql);
                values.Load(cursor);
            }
            finally
            {
                if (cursor != null)
                    cursor.Close();
            }
            return values;
        }
    }
}
