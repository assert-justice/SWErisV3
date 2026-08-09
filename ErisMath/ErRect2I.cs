namespace ErisMath;

public readonly struct ErRect2I
{
    public ErVec2I Position{get; init;}
    public ErVec2I Size{get; init;}
    public int Left{get => Size.X < 0 ? Position.X + Size.X : Position.X;}
    public int Right{get => Size.X < 0 ? Position.X : Position.X + Size.X;}
    public int Top{get => Size.Y < 0 ? Position.Y - Size.Y : Position.Y;}
    public int Bottom{get => Size.Y < 0 ? Position.Y : Position.Y + Size.Y;}
    public ErRect2I(int x, int y, int w, int h)
    {
        Position = new(x,y);
        Size = new(w,h);
    }
    public ErRect2I(ErVec2I position, ErVec2I size)
    {
        Position = position;
        Size = size;
    }
    public ErRect2I(int x, int y, ErVec2I size)
    {
        Position = new(x,y);
        Size = size;
    }
    public static ErRect2I operator +(ErRect2I left, ErRect2I right)=>new(left.Position + right.Position, left.Size + right.Size);
    public static ErRect2I operator -(ErRect2I left, ErRect2I right)=>new(left.Position - right.Position, left.Size - right.Size);
    public static ErRect2I operator *(ErRect2I left, ErRect2I right)=>new(left.Position * right.Position, left.Size * right.Size);
    public static ErRect2I operator /(ErRect2I left, ErRect2I right)=>new(left.Position / right.Position, left.Size / right.Size);
    public static ErRect2I operator *(ErRect2I left, int right)=>new(left.Position * right, left.Size * right);
    public static ErRect2I operator /(ErRect2I left, int right)=>new(left.Position / right, left.Size / right);
    public static ErRect2I operator *(ErRect2I left, ErVec2I right)=>new(left.Position * right, left.Size * right);
    public static ErRect2I operator /(ErRect2I left, ErVec2I right)=>new(left.Position / right, left.Size / right);
    public static explicit operator ErRect2(ErRect2I value) => new((ErVec2)value.Position, (ErVec2)value.Size);
    // public ErisRect2 Scale(double scale)
    // {
    //     return new((ErisVec2)Position*scale, (ErisVec2)Size*scale);
    // }
    public bool Contains(ErVec2I point)
    {
        if(point.X < Left || point.X > Right) return false;
        if(point.Y < Top || point.Y > Bottom) return false;
        return true;
    }
    public bool Contains(ErRect2I rect)
    {
        return Contains(rect.Position) && Contains(rect.Position + rect.Size);
    }
    public bool Overlaps(ErRect2I rect)
    {
        if(rect.Left > Right) return false;
        if(rect.Right < Left) return false;
        if(rect.Top > Bottom) return false;
        if(rect.Bottom < Top) return false;
        return true;
    }
    public ErRect2I Translate(ErVec2I vector)
    {
        return new(Position + vector, Size);
    }
    public ErRect2I TranslateAndScale(ErRect2I rect)
    {
        return new(Position + rect.Position, Size * rect.Size);
    }
    public ErRect2I TranslateAndScale(ErVec2I position, ErVec2I scale)
    {
        return new(Position + position, Size * scale);
    }
    public static ErRect2I Centered(ErVec2I center, ErVec2I size)
    {
        return new(center - size / 2, size);
    }
    public ErRect2I Centered(ErVec2I center)
    {
        return Centered(center, Size);
    }
    public static ErRect2I FromEdges(int left, int right, int top, int bottom)
    {
        int width = right - left;
        int height = bottom - top;
        return new(left, top, width, height);
    }
    public override string ToString()
    {
        return $"({Position},{Size})";
    }
}