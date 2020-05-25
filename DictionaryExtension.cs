using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace cg_proj2
{
    public static class DictionaryExtension
    {
        public static Polygon FindPolyKey(this Dictionary<Polygon, List<Polygon>> dict, Polygon val)
        {
            return dict.FirstOrDefault(x => x.Value.Contains(val)).Key;
        }

        public static Polygon DeleteFromList(this Dictionary<Polygon, List<Polygon>> dict, Polygon val)
        {
            return dict.FirstOrDefault(x =>
            {
                if (x.Value.Contains(val))
                {
                    x.Value.Remove(val);
                    return true;
                }
                return false;
            }).Key;
        }

        public static bool ContainsClippingPoly(this Dictionary<Polygon, List<Polygon>> dict, Polygon val)
        {
            foreach(List<Polygon> polygons in dict.Values)
            {
                if(polygons.Contains(val))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
