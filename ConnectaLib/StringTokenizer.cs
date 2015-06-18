using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ConnectaLib
{
  using System.Collections;
  using System.Text;

  /// <summary>
  /// Tokenizer
  /// </summary>
  public class StringTokenizer
  {
    private string[] parts;
    private int i = 0;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="str">string</param>
    /// <param name="delimiters">delimitadores</param>
    public StringTokenizer(string str, string delimiters)
    {
        /**
        char[] chars = Encoding.UTF8.GetChars(Encoding.UTF8.GetBytes(delimiters));
        string[] splitted = str.Split(chars, StringSplitOptions.None);
        int delimLen = delimiters.Length;
        ArrayList list = new ArrayList();
        for (int i = 0; i < splitted.Length; i++)
        {
        if (splitted[i] != "")
          list.Add(splitted[i]);
        else if ((splitted[i] == "") && (i > 0) && ((i % delimLen) == 0))
          list.Add(" ");
        }
        */
        ArrayList list = new ArrayList();
        string[] items = str.Split(new string[] { delimiters }, StringSplitOptions.None);
        foreach (string item in items)
        {
            list.Add(item);
        }
        parts = (string[])list.ToArray(typeof(string));
    }

    /// <summary>
    /// Comprueba si hay más tokens
    /// </summary>
    /// <returns>true si hay más tokens</returns>
    public bool HasMoreTokens()
    {
        return (i < parts.Length);
    }

    /// <summary>
    /// Avanza hasta el siguiente token
    /// </summary>
    /// <returns>siguiente token o blanco si no lo encuentra o fuera de límites</returns>
    public string NextToken()
    {
        if (i >= parts.Length)
            return "";
        else
            return parts[i++];
    }

    /// <summary>
    /// Obtiene el token del indice especificado
    /// </summary>
    /// <returns>valor del token</returns>
    public string GetToken(int pInd)
    {
        if (pInd < 1) return "";
        if (pInd > parts.Length) return "";
        return parts[pInd-1];
    }

    /// <summary>
    /// Obtener número de tokens
    /// </summary>
    /// <returns>número de tokens</returns>
    public int CountTokens()
    {
        return parts.Length;
    }
  }
}
