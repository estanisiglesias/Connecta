using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data.OleDb;

namespace ConnectaLib
{
  public class CodigoPostal
  {
    private string codigoPostal = "";

    //Getters/setters
      public string CPostal { get { return codigoPostal; } set { codigoPostal = value; } }

    /// <summary>
    /// Tratar código postal
    /// </summary>
    /// <param name="db">base de datos</param>
    /// ...
    /// <returns>true si es correcto</returns>
    public bool TratarCodigoPostal(string pCodigoPostal, string pUbicacion)
    {
        bool cpOk = true;
        Int32 nCodPost = 0;

        //Primero eliminamos los puntos y comas
        codigoPostal = pCodigoPostal.Trim().Replace(".","").Replace(",","");
        if (!String.IsNullOrEmpty(codigoPostal))
        {
            if (pUbicacion.ToUpper() == "ITA")
            {
            }
            else
            {
                //Si empieza por "ad" es de andorra
                if (codigoPostal.ToLower().StartsWith("ad"))
                {
                    if (codigoPostal.Length == 5)
                    {
                        //se comprueba si los 3 dígitos finales son numéricos
                        codigoPostal = codigoPostal.Substring(2, 3);
                        if (!Int32.TryParse(codigoPostal, out nCodPost))
                        {
                            codigoPostal = "";
                            cpOk = false;
                        }
                        else
                        {
                            codigoPostal = "AD" + codigoPostal;
                        }
                    }
                    else
                    {
                        codigoPostal = "";
                        cpOk = false;
                    }
                }
                else
                {
                    //se comprueba es numérico
                    if (!Int32.TryParse(codigoPostal, out nCodPost))
                    {
                        codigoPostal = "";
                        cpOk = false;
                    }
                    else
                    {
                        //se rellena a 0 por la izquierda
                        codigoPostal = codigoPostal.PadLeft(5, '0');
                        if (Int32.Parse(codigoPostal.Substring(0, 1)) > 5)
                        {
                            codigoPostal = "";
                            cpOk = false;
                        }
                        else
                        {
                            if (codigoPostal == "00000")
                            {
                                codigoPostal = "";
                                cpOk = false;
                            }
                        }
                    }
                }
            }
        }

        return cpOk;
    }
  }
}
