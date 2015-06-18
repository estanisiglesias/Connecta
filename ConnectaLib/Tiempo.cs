using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data.OleDb;

namespace ConnectaLib
{
  public class Tiempo
  {
    private string anyo = "";
    private string mes = "";
    private string semana = "";
    private string fecha = "";

    //Getters/setters
      public string Anyo { get { return anyo; } set { anyo = value; } }
      public string Mes { get { return mes; } set { mes = value; } }
      public string Semana { get { return semana; } set { semana = value; } }
      public string Fecha { get { return fecha; } set { fecha = value; } }

      /// <summary>
      /// Tratar los campos de tiempo
      ///(O bien tenemos la fecha, o bien a�o y mes, o bien a�o y semana)
      ///(Si uno de los campos a�o, mes, semana y fecha est�n vac�os se deducen del resto, tal que:
      ///    a�o se puede deducir de la fecha.
      ///    mes se puede deducir de la fecha o del a�o y la semana.
      ///    semana se puede deducir de la fecha o si no se deja en blanco
      ///    fecha se puede deducir del a�o y el mes (se fija �ltimo dia del mes) o del a�o y semana (se fija �ltimo d�a de la semana)
      /// </summary>
      /// <param name="pAnyo">A�o</param>
      /// <param name="pMes">Mes</param>
      /// <param name="pSemana">Semana</param>
      /// <param name="pFecha">Fecha</param>
      /// <returns>true si es correcto</returns>
      public bool TratarTiempo(Database db, string pAnyo, string pMes, string pSemana, string pFecha)
      {
          bool bResult = true;

          if (Utils.IsBlankField(pFecha))
          {
              if (!Utils.IsBlankField(pAnyo) && !Utils.IsBlankField(pSemana))
              {
                  int iWrk = 0;
                  if (int.TryParse(pAnyo, out iWrk) && int.TryParse(pSemana, out iWrk) && int.Parse(pAnyo) > 0 && int.Parse(pAnyo) < 9999 && int.Parse(pSemana) > 0 &&int.Parse(pSemana) <= 63)
                  {
                      anyo = pAnyo;
                      mes = pMes;
                      semana = pSemana;
                      DateTime dt = ObtenerUltimoDiaSemana(pAnyo, pMes, pSemana, true);
                      fecha = dt.ToShortDateString();
                      if (Utils.IsBlankField(pMes))
                      {
                          //Obtenemos el jueves mas cercano a la fecha calculada ya que era el dia que nos marcar� el a�o y el mes asociado a esa fecha
                          //ATENCI�N: en este punto puede quedar cierta inconsistencia porque podria pasar por ejemplo que que sea la primera semana
                          //del a�o y el dia del medio (jueves) caiga en el a�o anterior y por lo tanto el mes seria 12 pero del a�o de la primera semana
                          //y no podemos ajustar el a�o en este caso porque es un dato que nos ha llegado y debemos respetar
                          //Por lo tanto quiz�s quedar�a hacer un tratamiento especial para las semanas que son extremos del a�o...
                          mes = ObtenerDiaMediaSemana(dt).Month.ToString().PadLeft(2, '0');
                      }
                  }
                  else
                  {
                      bResult = false;
                  }
              }
              else if (!Utils.IsBlankField(pAnyo) && !Utils.IsBlankField(pMes))
              {
                  int iWrk = 0;
                  if (int.TryParse(pAnyo, out iWrk) && int.TryParse(pMes, out iWrk) && int.Parse(pAnyo) > 0 && int.Parse(pAnyo) < 9999 && int.Parse(pMes) > 0 && int.Parse(pMes) <= 12)
                  {
                      anyo = pAnyo;
                      mes = pMes;
                      fecha = DateTime.Parse(DateTime.DaysInMonth(int.Parse(pAnyo), int.Parse(pMes)).ToString() + "/" + pMes + "/" + pAnyo).ToShortDateString();
                      //Obtenemos el primer dia de la primera semana del a�o que estamos tratando, para poder compararlo 
                      //con la fecha que hemos calculado y as� conseguir el n�mero de semanas que hay entre ellas.
                      DateTime dt = ObtenerPrimerDiaSemana(pAnyo, "1");
                      TimeSpan ts = DateTime.Parse(fecha) - dt;                      
                      semana = Math.Truncate((decimal)(ts.Days / 7) + 1).ToString().PadLeft(2, '0');
                  }
                  else
                  {
                      bResult = false;
                  }
              }
              else
              {
                  bResult = false;
              }
          }
          else
          {
              fecha = pFecha;
              object x = db.GetDate(fecha);
              if (x != null)
              {                                    
                  DateTime dt = (DateTime)x;
                  //Obtenemos el jueves mas cercano a la fecha recibida ya que era el dia que nos marcar� el a�o y el mes asociado a esa fecha
                  dt = ObtenerDiaMediaSemana(dt);
                  if (Utils.IsBlankField(pAnyo)) 
                      anyo = dt.Year.ToString();
                  else 
                      anyo = pAnyo;
                  if (Utils.IsBlankField(pMes))
                      mes = dt.Month.ToString().PadLeft(2, '0');
                  else
                      mes = pMes;
                  if (Utils.IsBlankField(pSemana))
                  {
                      //Obtenemos el primer dia de la primera semana del a�o que estamos tratando, para poder compararlo 
                      //con la fecha que hemos recibido y as� conseguir el n�mero de semanas que hay entre ellas.
                      DateTime dt1 = ObtenerPrimerDiaSemana(dt.Year.ToString(), "1");
                      dt = (DateTime)x;
                      TimeSpan ts = dt - dt1;
                      semana = Math.Truncate((decimal)(ts.Days / 7) + 1).ToString().PadLeft(2, '0');
                  }
                  else
                      semana = pSemana;
              }
              else
              {
                  bResult = false;
              }
          }
          return bResult;
      }

      /// <summary>Obtenemos el �ltimo dia de la semana y del a�o indicado</summary>
      /// <param name="anyo">A�o</param>      
      /// <param name="semana">Semana</param>      
      /// <returns>fecha</returns>
      private DateTime ObtenerUltimoDiaSemana(string pAnyo, string pMes, string pSemana, bool pAjustar)
      {
          DateTime dt = new DateTime(int.Parse(pAnyo), 1, 1);
          if (dt.DayOfWeek == DayOfWeek.Monday || dt.DayOfWeek == DayOfWeek.Tuesday ||
              dt.DayOfWeek == DayOfWeek.Wednesday || dt.DayOfWeek == DayOfWeek.Thursday)
          {
              //Si el a�o empieza en lunes, martes, mi�rcoles o jueves contamos 
              //la semana actual c�mo la primera semana del a�o (lunes-domingo)
              if (dt.DayOfWeek == DayOfWeek.Monday) dt = dt.AddDays(-1);
              else if (dt.DayOfWeek == DayOfWeek.Tuesday) dt = dt.AddDays(-2);
              else if (dt.DayOfWeek == DayOfWeek.Wednesday) dt = dt.AddDays(-3);
              else if (dt.DayOfWeek == DayOfWeek.Thursday) dt = dt.AddDays(-4);
          }
          else
          {
              //Si el a�o empieza en viernes, s�bado o domingo contamos la semana 
              //siguiente a la actual c�mo la primera semana del a�o (lunes-domingo)
              if (dt.DayOfWeek == DayOfWeek.Friday) dt = dt.AddDays(2);
              else if (dt.DayOfWeek == DayOfWeek.Saturday) dt = dt.AddDays(1);
          }
          dt = dt.AddDays(7 * int.Parse(pSemana));
          if (pAjustar)
          {
              //ajustar significa que si el dia obtenido sobrepassa el a�o recibido por parametro, 
              //entonces tenemos que ajustar ese d�a y devolver el �ltimo d�a del a�o que hemos recibido por parametro
              if (int.Parse(pAnyo) < dt.Year)
              {
                  dt = DateTime.Parse(DateTime.DaysInMonth(int.Parse(pAnyo), 12).ToString() + "/12/" + pAnyo);
              }
              else
              {
                  //ajustar tambi�n significa que si hemos recibido el mes, y el d�a sobrepasa el mes recibido por parametro
                  //entonces tenemos que ajustar ese d�a y devolver el �ltimo d�a del mes recibido por parametro
                  if (!Utils.IsBlankField(pMes))
                  {
                      if (int.Parse(pMes) < dt.Month)
                      {
                          dt = DateTime.Parse(DateTime.DaysInMonth(int.Parse(pAnyo), int.Parse(pMes)).ToString() + "/" +  pMes + "/" + pAnyo);
                      }
                  }
              }
          }
          return dt;
      }

      /// <summary>Obtenemos el primer dia de la semana y del a�o indicado</summary>
      /// <param name="anyo">A�o</param>      
      /// <param name="semana">Semana</param>      
      /// <returns>fecha</returns>
      private DateTime ObtenerPrimerDiaSemana(string anyo, string semana)
      {
          DateTime dt = ObtenerUltimoDiaSemana(anyo, "", semana, false);
          dt = dt.AddDays(-6);
          return dt;
      }

      /// <summary>Obtenemos el dia del medio de la semana m�s cercano a la fecha recibida</summary>
      /// <param name="dt">fecha</param>            
      /// <returns>fecha</returns>
      private DateTime ObtenerDiaMediaSemana(DateTime dt)
      {
          DateTime dtResult = dt;
          if (dt.DayOfWeek == DayOfWeek.Monday) dtResult = dt.AddDays(3);
          else if (dt.DayOfWeek == DayOfWeek.Tuesday) dtResult = dt.AddDays(2);
          else if (dt.DayOfWeek == DayOfWeek.Wednesday) dtResult = dt.AddDays(1);
          else if (dt.DayOfWeek == DayOfWeek.Friday) dtResult = dt.AddDays(-1);
          else if (dt.DayOfWeek == DayOfWeek.Saturday) dtResult = dt.AddDays(-2);
          else if (dt.DayOfWeek == DayOfWeek.Sunday) dtResult = dt.AddDays(-3);
          return dtResult;
      }
  }
}
