using System;
using System.Collections.Generic;
using System.Text;

namespace ConnectaLib
{
    /// <summary>
    /// Constantes
    /// </summary>
    public class Constants
    {
        // Separador de campos delimited
        public const String FIELD_SEPARATOR = "|#|";
        public const String FIELD_SEPARATOR2 = "‡";
        //public const String FIELD_SEPARATOR2 = "\u2021";

        // Identificador de constante de agente 
        public const string AGENT_ID = "Agent-";
        public const string PREFIJO_USUARIO_AGENTE = "ConnectA Agent-";
        public const string PREFIJO_USUARIO_TEMPORAL = "ConnectA tmp-";
        public const string USUARIO_HISTORIFICACION = "HIST ConnectA";

        // Identificador de servicio de Windows
        public const string SERVICE_NAME_INT = "uveIntegrator Program";
        public const string SERVICE_NAME_FTP = "uveFTPAgent Program";
        public const string SERVICE_NAME_MAIL = "uveMailAgent Program";

        // Código fijo de producto ConnectA no existente
        public const string IDCCODIGOPRODUCTO_NO_EXISTENTE = "0";

        // Código fijo de producto ConnectA no existente
        public const string IDCCODIGOCLIENTE_GENERICO = "0";
        public const string CODIGOCLIENTE_GENERICO = "0";
        public const string NOMBRECLIENTE_GENERICO = "GENERIC";

        // Código fijo de producto ConnectA no existente
        public const string INDICADOR_RESET= "RESET";

        // Valor indicador de reset de campo
        public const string VALOR_RESET_CAMPO = "-";

        //Para controlar que no se supere la cantidad máxima que permite ConnectA
        public const double CANTIDAD_MAXIMA = 9999999.999;

        //Número de línea comodín. Por ejemplo para insertar un registro que hace
        //referencia a una cabecera de documento en una estructura de detalle de documento
        public const string NUM_LINEA_COMODIN = "-1";
        public const string NUMLINEA_PARA_BLOQUEOS_CABECERA = "-1";

        //Valor para decir que un documento esta bloqueado
        public const string BLOQUEO_ACTIVO = "S";

        //Valor para decir que un estado es igual a pendiente
        public const string ESTADO_PENDIENTE = "PD";
        //Valor para decir que un estado es igual a activo
        public const string ESTADO_ACTIVO = "AC";
        //Valor para decir que un estado es igual a activo por segunda vez después de que ya ha sido enviado antes.
        public const string ESTADO_REVISADO = "A2";
        //Valor para decir que un estado es igual a pendiente de ser revisado
        public const string ESTADO_REVISAR = "RV";
        //Valor para decir que un estado es igual a bloqueado
        public const string ESTADO_BLOQUEADO = "BL";
        public const string ESTADO_ERRONEO = "ER";
        //Valor para decir que un estado es igual a enviado
        public const string ESTADO_ENVIADO = "EN";
        //Valor para decir que un estado es igual a activo por segunda vez después de que ya ha sido enviado antes, porque el documento relacionado ha sido ajustado.
        public const string ESTADO_AJUSTADO = "A2";
        //Valor para decir que un estado es igual a confirmado
        public const string ESTADO_CONFIRMADO = "CF";
        //Valor para decir que un estado es igual a cartera
        public const string ESTADO_CARTERA = "CA";
        //Valor para decir que un estado es igual a servido
        public const string ESTADO_SERVIDO = "SE";
        //Valor para decir que un estado es igual a propuesta
        public const string ESTADO_PROPUESTA = "PR";
        //Valor para decir que un estado es igual a plantilla
        public const string ESTADO_PLANTILLA = "PL";
        //Valor para decir que un estado es igual a baja
        public const string ESTADO_BAJA = "BJ";
        public const string ESTADO_BAJADEFINITIVA = "BD";
        public const string ESTADO_BAJATEMPORAL = "BT";
        public const string ESTADO_CANCELADO = "NA";
        public const string ESTADO_BACKUP = "BK";
        //Valor para decir que un estado es igual marcado como anulado y listo para ser enviado
        public const string ESTADO_ANULADO = "A3";
        //Valor para decir que un estado es igual a activo
        public const string ESTADO_NOACTIVO = "NA";

        //Valor para decir que un estado es igual a test
        public const string ESTADO_TEST = "TS";        

        //Valores posibles para accion
        public const string ACCION_ALTA = "A";
        public const string ACCION_MODIFICACION = "M";
        public const string ACCION_BAJA = "B";
        public const string ACCION_INSERT = "I";
        public const string ACCION_UPDATE = "U";
        public const string ACCION_DELETE = "D";
        public const string ACCION_MODIFICACION_CABECERA= "C";
        public const string ACCION_MODIFICACION_CLASIFICADORES = "C";
        public const string ACCION_MODIFICACION_CLASIFICADORES_CON_INSERT = "C+";
        public const string ACCION_TRACKING = "T";

        //Valores posibles para los bloqueos
        public const string BLOQUEO_A_REVISAR = "ARevisar";
        public const string BLOQUEO_MANUAL = "Manual";
        public const string BLOQUEO_PROD_NO_EXISTE = "ProductoNoExiste";
        public const string BLOQUEO_CLIE_NO_EXISTE = "ClienteNoExiste";
        public const string BLOQUEO_CLIENTE_BLOQUEADOBAJA = "ClienteBloqueadoBaja";
        public const string BLOQUEO_CLIENTE_RETENIDO = "ClienteRetenido";
        public const string BLOQUEO_NUM_PED_CLIE = "NumPedidoCliente";
        public const string BLOQUEO_DIRECCION_VACIA = "DireccionCliente";
        public const string BLOQUEO_PROD_FUERA_SURTIDO = "ProductoFueraSurtido";
        public const string BLOQUEO_PROD_FUERA_SURTIDO_DIST = "ProductoFueraSurtidoDist";
        public const string BLOQUEO_PRODUCTO_BLOQUEADOBAJA = "ProductoBloqueadoBaja";
        public const string BLOQUEO_PRODUCTO_CANTIDAD_EXCESIVA = "CantidadExcesiva";
        public const string BLOQUEO_SOLICPRG_NO_EXISTE = "SolicitudPrGNoExiste";
        public const string BLOQUEO_SOLICPRG_YA_CONFIRMADA = "SolicitudPrGConfirmada";
        public const string BLOQUEO_ALBARAN_SIN_LINEAS = "AlbaranSinLineas";
        public const string BLOQUEO_FECHA_ENTREGA_ANTIGUA = "FechaEntregaAntigua";
        public const string BLOQUEO_PETICION_NO_CONCILIADA= "PeticionNoConciliada";

        //Valores para indicar si estados de bloqueo, básicamente S y N
        public const string BLOQUEADO_SI = "S";
        public const string BLOQUEADO_NO = "N";

        //Valores para indicar si el agente es un distribuidor o un fabricante
        public const string DISTRIBUIDOR = "D";
        public const string FABRICANTE = "F";  

        //Valores para indicar si el kit es de distribuidor o de fabricante
        public const string KIT_DISTRIBUIDOR = "D";
        public const string KIT_FABRICANTE = "F";        

        //Identificador de clientes y productos sin descripción
        public const string CLIENTES_DISTR_SIN_DESCRIPCION = "CLIENTE";
        public const string CLIENTES_DISTR_SIN_DIRECCION = "DIRECCION";
        public const string PRODUCTOS_DISTR_SIN_DESCRIPCION = "PRODUCTO";

        //CRLF
        public const string CRLF = "\r\n";

        //Valores tipo logs
        public const string LOGS_TRACE = "TRACE";

        //OleDB code errors
        public const int OLEDB_EX_TIMEOUT = -2147217871;
        public const int OLEDB_EX_TRUNCATE = -2147217833;

        //Id sip's personalizados
        public const string SIP_PERSONALIZADO = "PERSONALIZADO";
    }
}
