namespace cg_proj2.enums
{
    public enum Modes
    {
        DrawElipse,
        DrawLines,
        DrawCircles,
        Moving,
        DrawPolygons,
        DrawBrush,
        DrawRectangles,
        ClippingByPolygon =DrawPolygons,
        ClippingByRectangle=DrawRectangles
        
    }

    public enum PolyMoveModes
    {
        ByVertex,
        WholePoly
    }

    public enum RightClickModes
    {
        Move,
        Colour,
        Resize,
        PickPolyToClip
    }

    public enum PolyLineResize
    {
        NextEdge,
        PrevEdge,
        Whole
    }
}