using System;  
using System.Collections;
using System.Text;
using System.IO;

namespace ConnectaLib
{
  /// <summary>
  /// Gestión de los nomenclators
  /// </summary>
  public class Nomenclator
  {
    private ArrayList list = new ArrayList(5);
    private const int MAX_NOMENCLATOR_ALIAS = 100;
    private const string NOMENCLATOR_SEPARATOR = ";";
    private string identifiedNomenclator = null;
    private string siptype = "";

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="siptype">tipo de sip</param>
    public Nomenclator(string siptype) 
    {
        this.siptype = siptype;

        //Carga todos los posibles valores para este nomenclator...
        string iniFile = IniManager.GetIniFile();
        string nomenclator = "";

        //Buscamos el nomenclator principal.
        for (int x = 0; x < 20; x++)   //ATENCION: Con una vez bastaría, pero probamos varias veces para intentar garantizar 
        {                              //que existen se lee bien del ini ante posibles errores de lectura en disco
            nomenclator = IniManager.INIGetKeyValue(IniManager.CFG_NOMENCLATORS, siptype, iniFile);
            if (nomenclator != null && !nomenclator.Trim().Equals(""))
                break;
        }
        if (nomenclator != null && !nomenclator.Trim().Equals(""))
            list.Add(nomenclator.ToLower());

        //Buscar nomenclator+<ordinales>
        for (int i = 0; i < MAX_NOMENCLATOR_ALIAS; i++) 
        {
            for (int x = 0; x < 20; x++)   //ATENCION: Con una vez bastaría, pero probamos varias veces para intentar garantizar 
            {                              //que existen se lee bien del ini ante posibles errores de lectura en disco
                nomenclator = IniManager.INIGetKeyValue(IniManager.CFG_NOMENCLATORS, siptype + i, iniFile);
                if (nomenclator != null && !nomenclator.Trim().Equals(""))
                    break;
            }
            if (nomenclator == null || nomenclator.Equals(""))
                break;
            list.Add(nomenclator.ToLower());
        }
        if (list.Count==0)
            throw new IOException("Nomenclator no encontrado para SIP >> " + siptype);
    }

    /// <summary>
    /// Determina si un nomenclator debe procesar un fichero
    /// </summary>
    /// <param name="filename">fichero</param>
    /// <returns>true si debe procesarlo</returns>
    public bool IsForThisNomenclator(string filename)
    {
        string fname = FileNameToNomenclator(filename);
        string nomenclator = null;
        identifiedNomenclator = null;
        for (int i = 0; (i < list.Count) && (identifiedNomenclator==null); i++)
        {
            nomenclator = (string)list[i];
            if (nomenclator.IndexOf(NOMENCLATOR_SEPARATOR) != -1)
            {
                StringTokenizer st = new StringTokenizer(nomenclator, NOMENCLATOR_SEPARATOR);
                string s = st.NextToken();
                if (Match(fname,s))
                    identifiedNomenclator = nomenclator;
                else 
                {
                    s = st.NextToken();
                    if (Match(fname,s))
                        identifiedNomenclator = nomenclator;
                }
            }
            else
            {
                if(Match(fname,nomenclator))
                    identifiedNomenclator=nomenclator;
            }
        }
        return (identifiedNomenclator!=null);
    }

    /// <summary>
    /// Valida si un nombre de fichero coincide con el nomenclator
    /// </summary>
    /// <param name="fname">fichero</param>
    /// <param name="s">pattern</param>
    /// <returns>true si coincide</returns>
    private bool Match(string fname, string s)
    {
      return fname.StartsWith(s);
    }

    /// <summary>
    /// Obtener nomenclator o null si no ha encontrado ninguno asociado 
    /// al tipo de sip
    /// </summary>
    /// <returns>nomenclator</returns>
    public string GetIdentifiedNomenclator() 
    {
      return identifiedNomenclator;
    }

    /// <summary>
    /// Obtiene un nomenclator a partir del nombre del fichero (eliminando
    /// extensión, etc.)
    /// </summary>
    /// <param name="filename">fichero</param>
    /// <returns>nomenclator</returns>
    public string FileNameToNomenclator(string filename)
    {
      int ix = filename.LastIndexOf("\\");
      if (ix != -1)
        filename = filename.Substring(ix + 1);
      ix = filename.LastIndexOf(".");
      if (ix != -1)
        filename = filename.Substring(0, ix);
      return filename.ToLower();
    }

    /// <summary>
    /// Obtiene la parte nomenclator que actúa como master
    /// </summary>
    /// <param name="nomenclator">nomenclator</param>
    /// <returns>parte nomenclator que actúa como master</returns>
    public static string GetMaster(string nomenclator) 
    {
      StringTokenizer st = new StringTokenizer(nomenclator, NOMENCLATOR_SEPARATOR);
      return st.NextToken();
    }

    /// <summary>
    /// Obtiene la parte nomenclator que actúa como "slave"
    /// </summary>
    /// <param name="nomenclator">nomenclator</param>
    /// <returns>parte nomenclator que actúa como slave</returns>
    public static string GetSlave(string nomenclator)
    {
        StringTokenizer st = new StringTokenizer(nomenclator, NOMENCLATOR_SEPARATOR);
        st.NextToken();
        if(st.HasMoreTokens())
            return st.NextToken();
        else
            return "";
    }

    /// <summary>
    /// Obtener nomenclator principal o null si no ha encontrado ninguno asociado 
    /// al tipo de sip
    /// </summary>
    /// <returns>nomenclator</returns>
    public string GetMainNomenclator()
    {
      if (list.Count == 0)
        throw new IOException("Nomenclator no encontrado para SIP >> " + siptype);
      return (string)list[0];
    }

    /// <summary>
    /// Obtener lista de nomenclators (del fichero de configuración)
    /// </summary>
    /// <returns>lista de nomenclators</returns>
    public static string[] ListOfNomenclators()
    {
      string nomenclator = IniManager.INIGetKeyValue(IniManager.CFG_NOMENCLATORS, null, IniManager.GetIniFile());
      return nomenclator.Split('\0');
    }

    /// <summary>
    /// Comprueba si un nombre de fichero corresponde a un nomenclator "maestro",
    /// es decir, el principal en aquellos que tienen más de un nivel.
    /// </summary>
    /// <param name="n">nomenclator</param>
    /// <param name="filename">fichero</param>
    /// <returns>true si es maestro</returns>
    public bool IsMasterFile(string n, string filename)
    {
        string master = GetMaster(n);
        string fname = FileNameToNomenclator(filename);
        bool ok = false;
        if (list != null)
        {
            for (int i = 0; i < list.Count; i++)
            {
                string nomenclator = (string)list[i];
                if (nomenclator.IndexOf(NOMENCLATOR_SEPARATOR) != -1)
                {
                    StringTokenizer st = new StringTokenizer(nomenclator, NOMENCLATOR_SEPARATOR);
                    string s = st.NextToken();
                    if (Match(fname, s))
                    {
                        ok = true;
                        break;
                    }
                }
                else
                {
                    if (Match(fname, nomenclator))
                    {
                        ok = true;
                        break;
                    }
                }
            }
        }
        return ok;
    }

    /// <summary>
    /// Obtener el identificador de Sip. Nótese que en fichero de configuración,
    /// los sips pueden tener alias.
    /// </summary>
    /// <returns>identificador de Sip</returns>
    public string GetSip()
    {
      string tmp = "";
      foreach (char c in siptype)
      {
        if (Char.IsNumber(c))
          break;
        tmp += c;
      }
      return tmp;
    }
  }
}
