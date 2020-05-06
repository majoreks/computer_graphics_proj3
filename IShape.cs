using JsonSubTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cg_proj2
{
    [JsonConverter(typeof(JsonSubtypes))]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Circle), "R")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Circle), "Centre")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Brush), "Thickness")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Brush), "BrushCentre")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Line), "P0")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Line), "P1")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Polygon), "Vertices")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Polygon), "InitialPoint")]
    public interface IShape
    {
        void DeleteShape();
        void DrawShape();
        bool WasClicked(int x, int y);
    }
}
