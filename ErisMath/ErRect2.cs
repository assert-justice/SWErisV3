using SDL3;

namespace ErisMath;

public readonly struct ErRect2
{
    public ErVec2 Position{get; init;}
    public ErVec2 Size{get; init;}
    public double Left{get => Size.X < 0 ? Position.X + Size.X : Position.X;}
    public double Right{get => Size.X < 0 ? Position.X : Position.X + Size.X;}
    public double Top{get => Size.Y < 0 ? Position.Y - Size.Y : Position.Y;}
    public double Bottom{get => Size.Y < 0 ? Position.Y : Position.Y + Size.Y;}
    public ErVec2 Center{get => Position + Size * 0.5;}
    public ErRect2(double x, double y, double w, double h)
    {
        Position = new(x,y);
        Size = new(w,h);
    }
    public ErRect2(ErVec2 position, ErVec2 size)
    {
        Position = position;
        Size = size;
    }
    public ErRect2(double x, double y, ErVec2 size)
    {
        Position = new(x,y);
        Size = size;
    }
    public static explicit operator ErRect2I(ErRect2 value) => new((ErVec2I)value.Position, (ErVec2I)value.Size);
    public bool Contains(ErVec2 point)
    {
        if(point.X < Left || point.X > Right) return false;
        if(point.Y < Top || point.Y > Bottom) return false;
        return true;
    }
    public bool Contains(ErRect2 rect)
    {
        return Contains(rect.Position) && Contains(rect.Position + rect.Size);
    }
    public bool Overlaps(ErRect2 rect)
    {
        if(rect.Left > Right) return false;
        if(rect.Right < Left) return false;
        if(rect.Top > Bottom) return false;
        if(rect.Bottom < Top) return false;
        return true;
    }
    // Todo: implement this
    // public bool OverlapsCircle(ErisVec2 point, double radius)
    // {
    //     // circle equation: x*x + y*y = r*r
    //     // x =  +/-sqrt(r*r - y*y)
    //     // if r*r - y*y < 0 there are no solutions
    //     var rect = Translate(point * -1);
    //     double rsq = radius * radius;
    //     if(rsq - Top * Top >= 0)
    //     {
    //         if()
    //     }
    // }
    public ErRect2 Translate(ErVec2 vector)
    {
        return new(Position + vector, Size);
    }
    // public ErRect2 TranslateAndScale(ErRect2 rect)
    // {
    //     return new(Position + rect.Position, Size * rect.Size);
    // }
    // public ErRect2 Scale(ErRect2 rect){}
    // public ErRect2 TranslateAndScale(ErVec2 position, ErVec2 scale)
    // {
    //     return new(Position + position, Size * scale);
    // }
    public ErRect2 Centered(ErVec2 center)
    {
        return Centered(center, Size);
        // return new(center - rect.Size * 0.5, rect.Size);
    }
    public ErVec2 Clamp(ErVec2 vec)
    {
        double x = Math.Clamp(vec.X,Left,Right);
        double y = Math.Clamp(vec.Y,Top,Bottom);
        return new(x,y);
    }
    public SDL.FRect ToSdlRect()
    {
        return new()
        {
            X = (float)Position.X,
            Y = (float)Position.Y,
            W = (float)Size.X,
            H = (float)Size.Y,
        };
    }
    public static ErRect2 Centered(ErVec2 center, ErVec2 size)
    {
        return new(center - size * 0.5, size);
    }
    public static ErRect2 FromEdges(double left, double right, double top, double bottom)
    {
        double width = right - left;
        double height = bottom - top;
        return new(left, top, width, height);
    }
    // public ErisRe
    public override string ToString()
    {
        return $"({Position},{Size})";
    }
}