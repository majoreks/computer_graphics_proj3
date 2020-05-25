using JsonSubTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace cg_proj2
{
    [JsonConverter(typeof(JsonSubtypes))]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Circle), "R")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Circle), "Centre")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Circle), "ColorCircle")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Circle), "ThicknessCircle")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Brush), "Thickness")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Brush), "BrushCentre")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Brush), "ColorBrush")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Line), "P0")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Line), "P1")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Line), "ColorLine")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Line), "LineThickness")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Polygon), "Vertices")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Polygon), "InitialPoint")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Polygon), "ColorPoly")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Polygon), "IsFilled")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Polygon), "IsClipping")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Polygon), "FillColor")]

    public interface IShape
    {
        void DeleteShape();
        void DrawShape();
        bool WasClicked(int x, int y);
        void ReColour(Color color);
        void Resize(int _thickness);
    }
}
