using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data.OleDb;


namespace ConnectaLib
{
  /// <summary>
  /// Clase para la gestión de fabricantes
  /// </summary>
  public class Fabricante
  {
    private string umEstadistica = "";
    private string umEstadistica2 = "";
    private string umEstadistica3 = "";
    private string umGestion = "";
    private string fabAsignaUMDefecto = "";
    private string fabTiempoEsperaReservaDocs = "0";
    private string fabFacCtrlFacturasImputadas = "";
    private string fabAlbAdmiteProdNoExist = "";
    private string fabAlbRevisaConCantNeg = "";
    private string fabAlbRevisaEsteTipoAlbaran = "";
    private string fabAlbBloqueaClieSinCodific = "";
    private string fabAlbBloqueaClieBloqueado = "";
    private string fabAlbBloqueaProdNoexist = "";
    private string fabAlbBloqueaNumPedClieVacio = "";
    private string fabAlbBloqueaDireccionVacia = "";
    private string fabAlbBloqueaProdFueraSurtido = "";
    private string fabAlbBloqueaProdFueraSurtidoDist = "";
    private string fabAlbBloqueaProdBloqueadoBaja = "";
    private string fabAlbBloqueaProdCantidadExcesiva = "";
    private string fabAlbBloqueaSolicPrGNoExist = "";
    private string fabAlbBloqueaPeticionNoConciliada = "";
    private string fabAlbBloqueaAlbaranSinLineas = "";
    private string fabAlbBloqueaFechaEntregaAntigua = "";
    private string fabAlbModifEnviados = "";
    private string fabAlbRequiereResumen= "";
    private string fabAlbResumenEMailFrom = "";
    private string fabAlbResumenEMailTo = "";
    private string fabAlbMascaraNumAlb = "";
    private string fabAlbForzarSignoCantidad = "";
    private string fabAlbCtrlPeticiones = "";
    private string fabAlbCtrlExpediciones = "";
    private string fabCliValidaCodificacionClientes = "";
    private string fabCliValidaDistribuidor = "";
    private string fabCliEliminaReferenciados = "";
    private string fabCliNormalizaDirDist = "";
    private string fabCliRequiereAvisoNuevo = "";
    private string fabCliAvisoNuevoEMailFrom = "";
    private string fabCliAvisoNuevoEMailTo = "";
    private string fabCliAvisoNuevoEMailFirma = "";
    private string fabCliDeduplicacionActiva = "";
    private string fabProdEliminaReferenciados = "";
    private string fabProdKitsActivos = "";
    private string fabReaprModifEnviados = "";
    private string fabReaprForzarSignoCantidad = "";
    private string fabReaprAdmiteProdNoExist = "";
    private string fabReaprMascaraNumPed = "";
    private string fabIncMesesFechaBaja = "8"; //Por ahora dejamos de forma fija para todos los fabricantes un incremento de 8 meses
    private string fabFacFabValidarCliente = "";
    private string fabVtaFabValidarCliente = "";
    private string fabVtaFabValidarProducto = "";
    private string fabIndDataActivo = "";
    private string fabLiqUMEstadistica = "";
    private string fabLiqPrefijoContadorLiquidacion = "";
    private string fabCtrlFacturasEliminadas = "";
    private string fabVtaFabResetAutomatico = "";
    private string fabVtaFabCalcularCantidades = "";
    private string fabModoContingenciaActivo = "";
    private string fabModoContingenciaFecActivacion = "";
    private string fabModoContingenciaFecDesactivacion = "";
    private string fabPosPdVBulkUpd = "";
    private string fabProdNoTratarKits = "";

    //Getters
    public string UMEstadistica { get { return umEstadistica; } }
    public string UMEstadistica2 { get { return umEstadistica2; } }
    public string UMEstadistica3 { get { return umEstadistica3; } }
    public string UMGestion { get { return umGestion; } }
    public string FABAsignaUMDefecto { get { return fabAsignaUMDefecto; } }
    public string FABTiempoEsperaReservaDocs { get { return fabTiempoEsperaReservaDocs; } }
    public string FABFacCtrlFacturasImputadas { get { return fabFacCtrlFacturasImputadas; } }
    public string FABAlbAdmiteProdNoExist { get { return fabAlbAdmiteProdNoExist; } }
    public string FABAlbRevisaConCantNeg { get { return fabAlbRevisaConCantNeg; } }
    public string FABAlbRevisaEsteTipoAlbaran { get { return fabAlbRevisaEsteTipoAlbaran; } }
    public string FABAlbBloqueaClieSinCodific { get { return fabAlbBloqueaClieSinCodific; } }
    public string FABAlbBloqueaClieBloqueado { get { return fabAlbBloqueaClieBloqueado; } }
    public string FABAlbBloqueaProdNoexist { get { return fabAlbBloqueaProdNoexist; } }
    public string FABAlbBloqueaNumPedClieVacio { get { return fabAlbBloqueaNumPedClieVacio; } }
    public string FABAlbBloqueaDireccionVacia { get { return fabAlbBloqueaDireccionVacia; } }
    public string FABAlbBloqueaProdFueraSurtido { get { return fabAlbBloqueaProdFueraSurtido; } }
    public string FABAlbBloqueaProdFueraSurtidoDist { get { return fabAlbBloqueaProdFueraSurtidoDist; } }
    public string FABAlbBloqueaProdBloqueadoBaja { get { return fabAlbBloqueaProdBloqueadoBaja; } }
    public string FABAlbBloqueaProdCantidadExcesiva { get { return fabAlbBloqueaProdCantidadExcesiva; } }
    public string FABAlbBloqueaSolicPrGNoExist { get { return fabAlbBloqueaSolicPrGNoExist; } }
    public string FABAlbBloqueaPeticionNoConciliada { get { return fabAlbBloqueaPeticionNoConciliada; } }
    public string FABAlbBloqueaAlbaranSinLineas { get { return fabAlbBloqueaAlbaranSinLineas; } }
    public string FABAlbBloqueaFechaEntregaAntigua { get { return fabAlbBloqueaFechaEntregaAntigua; } }
    public string FABAlbModifEnviados { get { return fabAlbModifEnviados; } }
    public string FABAlbRequiereResumen { get { return fabAlbRequiereResumen; } }
    public string FABAlbResumenEMailFrom { get { return fabAlbResumenEMailFrom; } }
    public string FABAlbResumenEMailTo { get { return fabAlbResumenEMailTo; } }
    public string FABAlbMascaraNumAlb { get { return fabAlbMascaraNumAlb; } }
    public string FABAlbForzarSignoCantidad { get { return fabAlbForzarSignoCantidad; } }
    public string FABAlbCtrlPeticiones { get { return fabAlbCtrlPeticiones; } }
    public string FABAlbCtrlExpediciones { get { return fabAlbCtrlExpediciones; } }
    public string FABCliValidaCodificacionClientes { get { return fabCliValidaCodificacionClientes; } }
    public string FABCliValidaDistribuidor { get { return fabCliValidaDistribuidor; } }
    public string FABCliEliminaReferenciados { get { return fabCliEliminaReferenciados; } }
    public string FABCliNormalizaDirDist { get { return fabCliNormalizaDirDist; } }
    public string FABCliRequiereAvisoNuevo { get { return fabCliRequiereAvisoNuevo; } }
    public string FABCliAvisoNuevoEMailFrom { get { return fabCliAvisoNuevoEMailFrom; } }
    public string FABCliAvisoNuevoEMailTo { get { return fabCliAvisoNuevoEMailTo; } }
    public string FABCliAvisoNuevoEMailFirma { get { return fabCliAvisoNuevoEMailFirma; } }
    public string FABCliDeduplicacionActiva { get { return fabCliDeduplicacionActiva; } }
    public string FABProdEliminaReferenciados { get { return fabProdEliminaReferenciados; } }
    public string FABProdKitsActivos { get { return fabProdKitsActivos; } }
    public string FABReaprModifEnviados { get { return fabReaprModifEnviados; } }
    public string FABReaprForzarSignoCantidad { get { return fabReaprForzarSignoCantidad; } }
    public string FABReaprAdmiteProdNoExist { get { return fabReaprAdmiteProdNoExist; } }
    public string FABReaprMascaraNumPed { get { return fabReaprMascaraNumPed; } }
    public string FABIncMesesFechaBaja { get { return fabIncMesesFechaBaja; } }
    public string FABFacFabValidarCliente { get { return fabFacFabValidarCliente; } }
    public string FABVtaFabValidarCliente { get { return fabVtaFabValidarCliente; } }
    public string FABVtaFabValidarProducto { get { return fabVtaFabValidarProducto; } }
    public string FABIndDataActivo { get { return fabIndDataActivo; } }
    public string FABLiqUMEstadistica { get { return fabLiqUMEstadistica; } }
    public string FABLiqPrefijoContadorLiquidacion { get { return fabLiqPrefijoContadorLiquidacion; } }
    public string FABCtrlFacturasEliminadas { get { return fabCtrlFacturasEliminadas; } }
    public string FABVtaFabResetAutomatico { get { return fabVtaFabResetAutomatico; } }
    public string FABVtaFabCalcularCantidades { get { return fabVtaFabCalcularCantidades; } }
    public string FABModoContingenciaActivo { get { return fabModoContingenciaActivo; } }
    public string FABModoContingenciaFecActivacion { get { return fabModoContingenciaFecActivacion; } }
    public string FABModoContingenciaFecDesactivacion { get { return fabModoContingenciaFecDesactivacion; } }
    public string FABPosPdVBulkUpd { get { return fabPosPdVBulkUpd; } }
    public string FABProdNoTratarKits { get { return fabProdNoTratarKits; } }

    /// <summary>
    /// Obtener parámetros del fabricante
    /// </summary>
    /// <param name="db"></param>
    /// <param name="agente"></param>
    public bool ObtenerParametros(Database db, string agente)
    { 
        bool isOK = false;
        DbDataReader reader = null;
        umEstadistica = "";
        umEstadistica2 = "";
        umEstadistica3 = "";
        umGestion = "";
        fabAsignaUMDefecto = "";
        fabTiempoEsperaReservaDocs = "20";
        fabProdEliminaReferenciados = "";
        fabAlbAdmiteProdNoExist = "";
        fabAlbRevisaConCantNeg = "";
        fabAlbRevisaEsteTipoAlbaran = "";
        fabAlbBloqueaClieSinCodific = "";
        fabAlbBloqueaClieBloqueado = "";
        fabAlbBloqueaProdNoexist = "";
        fabAlbBloqueaNumPedClieVacio = "";
        fabAlbBloqueaDireccionVacia = "";
        fabAlbBloqueaProdFueraSurtido = "";
        fabAlbBloqueaProdFueraSurtidoDist = "";
        fabAlbBloqueaProdBloqueadoBaja = "";
        fabAlbBloqueaProdCantidadExcesiva = "";
        fabAlbBloqueaSolicPrGNoExist = "";
        fabAlbBloqueaPeticionNoConciliada = "";
        fabAlbBloqueaAlbaranSinLineas = "";
        fabAlbBloqueaFechaEntregaAntigua = "";
        fabAlbModifEnviados = "";
        fabAlbRequiereResumen = "";
        fabAlbResumenEMailFrom = "";
        fabAlbResumenEMailTo = "";
        fabAlbMascaraNumAlb = "";
        fabAlbForzarSignoCantidad = "";
        fabAlbCtrlPeticiones = "";
        fabAlbCtrlExpediciones = "";
        fabCliValidaCodificacionClientes = "";
        fabCliValidaDistribuidor = "";
        fabCliEliminaReferenciados = "";
        fabCliNormalizaDirDist = "";
        fabCliRequiereAvisoNuevo = "";
        fabCliAvisoNuevoEMailFrom = "";
        fabCliAvisoNuevoEMailTo = "";
        fabCliAvisoNuevoEMailFirma = "";
        fabCliDeduplicacionActiva = "";
        fabProdEliminaReferenciados = "";
        fabProdKitsActivos = "";
        fabReaprModifEnviados = "";
        fabReaprForzarSignoCantidad = "";
        fabReaprAdmiteProdNoExist = "";
        fabReaprMascaraNumPed = "";
        fabFacCtrlFacturasImputadas = "";
        fabFacFabValidarCliente = "";
        fabVtaFabValidarCliente = "";
        fabVtaFabValidarProducto = "";
        fabIndDataActivo = "";
        fabLiqUMEstadistica = "";
        fabLiqPrefijoContadorLiquidacion = "";
        fabCtrlFacturasEliminadas = "";
        fabVtaFabResetAutomatico = "";
        fabVtaFabCalcularCantidades = "";
        fabModoContingenciaActivo = "N";
        fabModoContingenciaFecActivacion = "";
        fabModoContingenciaFecDesactivacion = "";
        fabPosPdVBulkUpd = "";
        fabProdNoTratarKits = "";

        try
        {
            if (Utils.IsBlankField(agente)) return isOK;

            string sql = "Select UMEstadistica" +
                                ", UMEstadistica2" +
                                ", UMEstadistica3" +
                                ", UMGestion" +
                                ", Alb_AdmiteProductosNoExistentes" +
                                ", Alb_RevisarNegativos" +
                                ", Alb_RevisarEsteTipoAlbaran" +
                                ", Alb_CodificClieOblig" +
                                ", Alb_CtrlEstadoClie" +
                                ", Alb_CodificProdOblig" +
                                ", Alb_NumPedClieOblig" +
                                ", Alb_DireccionOblig" +
                                ", Alb_CtrlSurtido" +
                                ", Alb_CtrlSurtidoDist" +
                                ", Alb_CtrlEstadoProd" +
                                ", Alb_CantidadExcesiva" +
                                ", Alb_CtrlSolicitudesPrG" +
                                ", Alb_CtrlAlbaranesSinLineas" +
                                ", Alb_CtrlFecEntregaAntigua" +
                                ", AsignaUMDefecto" +
                                ", TiempoEsperaReservaDocs" +
                                ", Alb_ModifEnviados" +
                                ", Alb_RequiereResumen" +
                                ", Alb_ResumenEMailFrom" +
                                ", Alb_ResumenEMailTo" +
                                ", Alb_MascaraNumAlb" +
                                ", Alb_ForzarSignoCantidad" +
                                ", Cli_ValidaCodificacion" +
                                ", Cli_ValidaDistribuidor" +
                                ", Cli_EliminaReferenciados" +
                                ", Cli_NormalizaDirDist" +
                                ", Prod_EliminaReferenciados" +
                                ", Prod_KitsActivos" +
                                ", Reapr_ModifEnviados" +
                                ", Reapr_ForzarSignoCantidad" +
                                ", Reapr_AdmiteProductosNoExistentes" +
                                ", Reapr_MascaraNumPed" +
                                ", Fac_CtrlFacturasImputadas" +
                                ", Cli_RequiereAvisoNuevo" +
                                ", Cli_AvisoNuevoEMailFrom" +
                                ", Cli_AvisoNuevoEMailTo" +
                                ", Cli_AvisoNuevoEMailFirma" +
                                ", FacFab_ValidarCliente" +
                                ", VtaFab_ValidarCliente" +
                                ", VtaFab_ValidarProducto" +
                                ", IndDataActivo" +
                                ", Acue_Liq_UMEstadistica" +
                                ", Acue_Liq_PrefijoContadorLiquidacion" +
                                ", FacFab_CtrlFacturasEliminadas" +
                                ", ModoContingenciaActivo " +
                                ", ModoContingenciaFecActivacion " +
                                ", ModoContingenciaFecDesactivacion " +
                            " From Fabricantes " +
                            " Where IdcAgente = " + agente;
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                umEstadistica = db.GetFieldValue(reader, 0);
                umEstadistica2 = db.GetFieldValue(reader, 1);
                umEstadistica3 = db.GetFieldValue(reader, 2);
                umGestion = db.GetFieldValue(reader, 3);
                fabAlbAdmiteProdNoExist = db.GetFieldValue(reader, 4);
                fabAlbRevisaConCantNeg = db.GetFieldValue(reader, 5);
                fabAlbRevisaEsteTipoAlbaran = db.GetFieldValue(reader, 6);
                fabAlbBloqueaClieSinCodific = db.GetFieldValue(reader, 7);
                fabAlbBloqueaClieBloqueado = db.GetFieldValue(reader, 8);
                fabAlbBloqueaProdNoexist = db.GetFieldValue(reader, 9);
                fabAlbBloqueaNumPedClieVacio = db.GetFieldValue(reader, 10);
                fabAlbBloqueaDireccionVacia = db.GetFieldValue(reader, 11);
                fabAlbBloqueaProdFueraSurtido = db.GetFieldValue(reader, 12);
                fabAlbBloqueaProdFueraSurtidoDist = db.GetFieldValue(reader, 13);
                fabAlbBloqueaProdBloqueadoBaja = db.GetFieldValue(reader, 14);
                fabAlbBloqueaProdCantidadExcesiva = db.GetFieldValue(reader, 15);
                fabAlbBloqueaSolicPrGNoExist = db.GetFieldValue(reader, 16);
                fabAlbBloqueaAlbaranSinLineas = db.GetFieldValue(reader, 17);
                fabAlbBloqueaFechaEntregaAntigua = db.GetFieldValue(reader, 18);
                fabAsignaUMDefecto = db.GetFieldValue(reader, 19);
                fabTiempoEsperaReservaDocs = db.GetFieldValue(reader, 20);
                fabAlbModifEnviados = db.GetFieldValue(reader, 21);
                fabAlbRequiereResumen = db.GetFieldValue(reader, 22);
                fabAlbResumenEMailFrom = db.GetFieldValue(reader, 23);
                fabAlbResumenEMailTo = db.GetFieldValue(reader, 24);
                fabAlbMascaraNumAlb = db.GetFieldValue(reader, 25);
                fabAlbForzarSignoCantidad = db.GetFieldValue(reader, 26);
                fabCliValidaCodificacionClientes = db.GetFieldValue(reader, 27);
                fabCliValidaDistribuidor = db.GetFieldValue(reader, 28);
                fabCliEliminaReferenciados = db.GetFieldValue(reader, 29);
                fabCliNormalizaDirDist = db.GetFieldValue(reader, 30);
                fabProdEliminaReferenciados = db.GetFieldValue(reader, 31);
                fabProdKitsActivos = db.GetFieldValue(reader, 32);
                fabReaprModifEnviados = db.GetFieldValue(reader, 33);
                fabReaprForzarSignoCantidad = db.GetFieldValue(reader, 34);
                fabReaprAdmiteProdNoExist = db.GetFieldValue(reader, 35);
                fabReaprMascaraNumPed = db.GetFieldValue(reader, 36);
                fabFacCtrlFacturasImputadas = db.GetFieldValue(reader, 37);
                fabCliRequiereAvisoNuevo = db.GetFieldValue(reader, 38);
                fabCliAvisoNuevoEMailFrom = db.GetFieldValue(reader, 39);
                fabCliAvisoNuevoEMailTo = db.GetFieldValue(reader, 40);
                fabCliAvisoNuevoEMailFirma = db.GetFieldValue(reader, 41);
                fabFacFabValidarCliente = db.GetFieldValue(reader, 42);
                fabVtaFabValidarCliente = db.GetFieldValue(reader, 43);
                fabVtaFabValidarProducto = db.GetFieldValue(reader, 44);
                fabIndDataActivo = db.GetFieldValue(reader, 45);
                fabLiqUMEstadistica =  db.GetFieldValue(reader, 46);
                fabLiqPrefijoContadorLiquidacion = db.GetFieldValue(reader, 47);
                fabCtrlFacturasEliminadas = db.GetFieldValue(reader, 48);
                fabModoContingenciaActivo = db.GetFieldValue(reader, 49);
                fabModoContingenciaFecActivacion = db.GetFieldValue(reader, 50);
                fabModoContingenciaFecDesactivacion = db.GetFieldValue(reader, 51);
                isOK = true;
            }
            if (Utils.IsBlankField(fabTiempoEsperaReservaDocs) || (!Utils.IsBlankField(fabTiempoEsperaReservaDocs) && Int32.Parse(fabTiempoEsperaReservaDocs) == 0))
                fabTiempoEsperaReservaDocs = "20";
            
            //Parametros generales
            sql = "Select Parametro, Valor " +
                  "  From ParametrosAgentes " +
                  " Where IdcAgenteOrigen = " + agente + " And IdcAgenteDestino = 0";
            reader = db.GetDataReader(sql);
            while (reader.Read())
            {
                if (db.GetFieldValue(reader, 0) == "VtaFab_ResetAutomatico")
                    fabVtaFabResetAutomatico = db.GetFieldValue(reader, 1);
                else if (db.GetFieldValue(reader, 0) == "VtaFab_CalcularCantidades")
                    fabVtaFabCalcularCantidades = db.GetFieldValue(reader, 1);
                else if (db.GetFieldValue(reader, 0) == "Alb_CtrlPeticiones")
                {
                    //Un mismo parametro en bd lo utilizamos para dos propositos diferentes en el programa
                    fabAlbCtrlPeticiones = db.GetFieldValue(reader, 1);
                    fabAlbBloqueaPeticionNoConciliada = db.GetFieldValue(reader, 1);
                }
                else if (db.GetFieldValue(reader, 0) == "Alb_CtrlExpediciones")
                    fabAlbCtrlExpediciones = db.GetFieldValue(reader, 1);
                else if (db.GetFieldValue(reader, 0) == "Cli_DeduplicacionActiva")
                    fabCliDeduplicacionActiva = db.GetFieldValue(reader, 1);
                else if (db.GetFieldValue(reader, 0) == "PosPdVFab_BulkUpd")
                    fabPosPdVBulkUpd = db.GetFieldValue(reader, 1);
                else if (db.GetFieldValue(reader, 0) == "PosPdVFab_BulkUpd")
                    fabPosPdVBulkUpd = db.GetFieldValue(reader, 1);
                else if (db.GetFieldValue(reader, 0) == "PosPdVFab_BulkUpd")
                    fabPosPdVBulkUpd = db.GetFieldValue(reader, 1);
                else if (db.GetFieldValue(reader, 0) == "Prod_NoTratarKit")
                    fabProdNoTratarKits = db.GetFieldValue(reader, 1);
                isOK = isOK && true;
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return isOK;
    }

    /// <summary>
    /// Obtener nombre del fabricante
    /// </summary>
    /// <param name="db"></param>
    /// <param name="agente"></param>
    public string ObtenerNombre(Database db, string agente)
    {
        string sNom = "";
        DbDataReader reader = null;

        try
        {
            if (Utils.IsBlankField(agente)) return sNom;

            string sql = "Select Nombre " +
                            " From Fabricantes " +
                            " Where IdcAgente = " + agente;
            reader = db.GetDataReader(sql);
            if (reader.Read())
            {
                sNom = db.GetFieldValue(reader, 0);
            }
        }
        finally
        {
            if (reader != null)
                reader.Close();
        }
        return sNom;
    }

  }
}
