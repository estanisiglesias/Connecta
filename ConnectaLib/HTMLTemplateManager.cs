using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;

namespace ConnectaLib
{
  public class HTMLTemplateManager
  {
    Hashtable m_stringArray = new Hashtable(10);
    string m_strIdStartSection = "<!-- Section=";
    string m_strIdEndSection = " -->";

    public void LoadTemplate(string pszTemplate)
    {
      string str, strTemplate;
      int iFindPos, iFindPos2;
      string strName;

      strTemplate = "";
      strName = "";

      StreamReader reader = new StreamReader(pszTemplate);

      while ((str = reader.ReadLine()) != null)
      {
        // Buscar sección.

        iFindPos = str.IndexOf(m_strIdStartSection);
        iFindPos2 = str.IndexOf(m_strIdEndSection);

        if ((iFindPos != -1) && (iFindPos2 > iFindPos))
        {
          // Fin de una sección, inicio de otra.

          if (iFindPos > 0)
            strTemplate += str.Substring(0, iFindPos);

          //strName = fileInfoArray[i] + "." + strName;
          strName = pszTemplate + "." + strName;

          m_stringArray.Add(strName, strTemplate);

          iFindPos += m_strIdStartSection.Length;
          strName = str.Substring(iFindPos, iFindPos2 - iFindPos);

          iFindPos2 += m_strIdEndSection.Length;
          if (iFindPos2 <= str.Length)
          {
            strTemplate = str.Substring(iFindPos2);
            strTemplate += "\n";
          }
        }
        else
        {
          strTemplate += str;
          strTemplate += "\n";
        }
      }

      reader.Close();

      strName = pszTemplate + "." + strName; 

      m_stringArray.Add(strName, strTemplate);
    }

    public bool LoadTemplates(string pszTemplates)
    {
      bool bSuccess;

      bSuccess = true;

      try
      {
        DirectoryInfo directoryInfo = new DirectoryInfo(pszTemplates);
        FileInfo[] fileInfoArray = directoryInfo.GetFiles("*.template");

        if (fileInfoArray.Length > 0)
        {
          for (int i = 0; i < fileInfoArray.Length; i++)
          {
            LoadTemplate(pszTemplates + "\\" + fileInfoArray[i]);
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine(e.StackTrace);
        bSuccess = false;
      }
      return bSuccess;
    }

    public string GetTemplate(string pszTemplate)
    {
      return (string)m_stringArray[pszTemplate];
    }

    public string Replace(string pszTemplate, string pszId, string pszValue)
    {
      string str, strTemplate;
      int i, j, iLen;

      i = 0;

      str = "";
      strTemplate = pszTemplate;
      iLen = strTemplate.Length;

      while (i < iLen)
      {
        j = strTemplate.IndexOf(pszId, i);
        if (j < 0)
        {
          str += strTemplate.Substring(i);
          i = strTemplate.Length;
        }
        else
        {
          if (j > i)
            str += strTemplate.Substring(i, j - i);

          str += pszValue;
          i = j + pszId.Length;
        }
      }

      return str;
    }
  }
}
