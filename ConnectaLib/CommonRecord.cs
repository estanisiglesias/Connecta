using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace ConnectaLib
{
  /// <summary>
  /// Clase base para los clases que actuan como formato de almacenamiento
  /// de los datos recibidos por XML/txt, etc.
  /// </summary>
  public abstract class CommonRecord
  {
    protected Hashtable values = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
    protected ArrayList children = new ArrayList();
    protected CommonRecord parent = null;
    protected int fieldcount = 0;

    /// <summary>
    /// Mapea fila de formato delimited a valores
    /// </summary>
    /// <param name="row">row</param>
    public virtual void MapRow(string row)
    {
      Reset();
    }

    /// <summary>
    /// Añade clave/valor
    /// </summary>
    /// <param name="id">clave</param>
    /// <param name="value">valor</param>
    public void PutValue(string id, string value)
    {
      //Pueden venir claves repetidas en el caso de un XML con registros anidados (ej. sipInClientesFinalesDistribuidor)
        if (!values.ContainsKey(id))
            values.Add(id, value);
        else
            values[id] = value;
    }

    /// <summary>
    /// Obtener valor
    /// </summary>
    /// <param name="id">clave</param>
    /// <returns>valor</returns>
    public string GetValue(string id)
    {
        string s = ((string)values[id]);
        if (s != null)
        {
            s = s.Trim().Replace("'", ".");
            s = s.Replace("\0", " ");
            s = s.Replace("\n", " ");
            s = s.Replace("\r", " ");
            s = s.Replace("\t", " ");
        }
        else
            s = "";
        return s;
    }

    /// <summary>
    /// Obtener valor
    /// </summary>
    /// <param name="id">clave</param>
    /// <param name="max">número máixmo de caracteres</param>
    /// <returns>valor</returns>
    public string GetValueTruncating(string id, int max)
    {
      string s = GetValue(id);
      if (s.Length > max)
        s = s.Substring(0, max);
      return s;
    }

    /// <summary>
    /// Obtener valor como número enterno
    /// </summary>
    /// <param name="id">clave</param>
    /// <returns>valor numérico</returns>
    public string GetNumericValueAsInt(string id)
    {
        int ix = 0;
        string s = GetValue(id);
        if (s != null)
        {
            if (s.Equals(""))
                s = "0";
            else
            {
                if (s.IndexOf(",") != -1 && s.IndexOf(".") != -1)
                {
                    s = s.Replace(".", "");
                    ix = s.LastIndexOf(",");
                    s = s.Substring(0, ix);
                    s = s.Replace(",", "");
                }
                else if (s.IndexOf(",") != -1)
                {
                    ix = s.LastIndexOf(",");
                    s = s.Substring(0, ix);
                    s = s.Replace(",", "");
                }
                else if (s.IndexOf(".") != -1)
                {
                    ix = s.LastIndexOf(".");
                    s = s.Substring(0, ix);
                    s = s.Replace(".", "");
                }
            }
        }
        return Int32.Parse(s)+"";
    }

    /// <summary>
    /// Obtener valor como número
    /// </summary>
    /// <param name="id">clave</param>
    /// <returns>valor numérico</returns>
    public string GetNumericValue(string id)
    {
        string s = GetValue(id);
        if (s != null) 
        {
            if (s.Equals(""))
                s = "0";
            else
            {
                if (s.IndexOf(",") != -1)
                    s = s.Replace(".", "");
                else
                    s = s.Replace(".", ",");
                if (s.Trim().EndsWith("-"))
                    s = "-" + s.Replace("-", "");
            }
        }
        return s;
    }

    /// <summary>
    /// Inicializar objeto
    /// </summary>
    public void Reset()
    {
        values.Clear();
        children.Clear();
    }

    /// <summary>
    /// Guarda un record como hijo de éste
    /// </summary>
    /// <param name="rec">registro</param>
    public void AddChild(CommonRecord rec) 
    {
        children.Add(rec);    
    }

    /// <summary>
    /// Determina si un registros tiene registros "hijo"
    /// </summary>
    /// <returns>true si tiene</returns>
    public bool HasChildren() 
    {
        return children.Count > 0;
    }

    /// <summary>
    /// Obtiene lista de registros hijo
    /// </summary>
    /// <returns>lista de registros hijo</returns>
    public ArrayList GetChildren() 
    {
        return children;
    }

    /// <summary>
    /// Obtiene registro "padre"
    /// </summary>
    /// <returns>registro padre</returns>
    public CommonRecord GetParent() 
    {
        return parent;
    }

    /// <summary>
    /// Ajusta registro "padre"
    /// </summary>
    /// <param name="rec">registro padre</param>
    public void SetParent(CommonRecord rec)
    {
        parent = rec;
    }

    public int GetFieldCount()
    {
        return fieldcount;
    }

    public void SetFieldCount(int value)
    {
        fieldcount = value;
    }

  }
}
