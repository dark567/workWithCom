using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeDataCom
{
    public static class Extensions
    {
        public static List<string> listGoods = new List<string> { "WBC", /*"LYM", "NEU", "MON", "EOS", "BAS", */"LY%", "NE%", "MO%", "EO%", "BA%", "RBC", "HGB", "HCT","MCV", "MCH", "MCHC", "MPV", "PLT", "RDWc", "PDWc", "PCT" };
        public static bool InCont<T>(this T item, params T[] items)
        {
            if (items == null)
                throw new ArgumentNullException("items");

            return items.Contains(item);
        }

        public static bool InContVerTwo(string x)
        {
            if (listGoods.Contains(x)) return true;
            return false;

            //if (new[] { 1, 2, 3, 99 }.Contains(x))
            //{
            //    // do something
            //}
        }

    }
}
