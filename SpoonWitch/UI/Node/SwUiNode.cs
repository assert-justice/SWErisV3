using Eris;
using ErisMath;

namespace SpoonWitch.UI.Node;

public abstract class SwUiNode
{
    private readonly List<SwUiNode> Children = [];
    public SwUiNode? Parent{get; private set;}
    public virtual ErVec2 MinSize => ErVec2.Zero;
    public ErVec2 Position{get; private set;}
    public bool CanFocus => Visible && GetCanFocus();
    private bool _Visible;
    public bool IsDirty{get; private set;}
    public bool Visible
    {
        get => _Visible;
        set
        {
            if(_Visible != value) SetVisible(value);
        }
    }
    protected virtual void SetVisible(bool isVisible)
    {
        _Visible = isVisible;
    }
    protected virtual bool GetCanFocus()
    {
        return false;
    }
    protected virtual void Clean()
    {
        IsDirty = false;
        foreach (var item in Children)
        {
            item.Clean();
        }
    }
    public virtual void Draw()
    {
        foreach (var item in Children)
        {
            item.Draw();
        }
    }
    public virtual void Update(){}
    public SwUiNode[] GetChildren()
    {
        return [..Children];
    }
    public IEnumerable<T> GetChildren<T>() where T: SwUiNode
    {
        foreach (var item in Children)
        {
            if(item is T t) yield return t;
        }
    }
    public bool TryGetChild(int idx, out SwUiNode child)
    {
        child = default!;
        if(idx < 0 || idx >= Children.Count) return false;
        child = Children[idx];
        return true;
    }
    public void AddChild(SwUiNode child)
    {
        child.Parent?.RemoveChild(child);
        child.Parent = this;
        Children.Add(child);
    }
    public void SetParent(SwUiNode parent)
    {
        parent.AddChild(this);
    }
    public bool RemoveChild(SwUiNode child)
    {
        bool res = Children.Remove(child);
        if(!res) return false;
        child.Parent = null;
        return true;
    }
    public void ClearChildren()
    {
        Children.Clear();
    }
}