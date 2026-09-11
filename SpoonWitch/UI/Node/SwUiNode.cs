using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.UI.Menu;

namespace SpoonWitch.UI.Node;

public abstract class SwUiNode
{
    public readonly List<SwUiNode> Children = [];
    public SwUiNode? Parent{get; private set;}
    public virtual ErVec2 MinSize => ErVec2.Zero;
    public ErVec2 Position{get; private set;}
    public bool CanFocus => Visible && CanFocusPro;
    protected virtual bool CanFocusPro => false;
    private bool _Visible = true;
    public bool IsDirty{get; private set;}
    public bool Visible
    {
        get => _Visible;
        set
        {
            if(_Visible != value) SetVisible(value);
        }
    }
    protected SwUiNode(PriNode node)
    {
        if(node.TryGet("visible", out bool b)) _Visible = b;
        foreach (var item in node.Get("children").Values)
        {
            if(!TryFromPrion(item, out var uiNode)) continue;
            AddChild(uiNode);
        }
    }
    protected virtual void SetVisible(bool isVisible)
    {
        _Visible = isVisible;
    }
    public virtual void SetPosition(ErVec2 position)
    {
        Position = position;
    }
    public virtual void FocusBegin(){}
    public virtual void FocusEnd(){}
    public virtual void Up(){}
    public virtual void Down(){}
    public virtual void Left(){}
    public virtual void Right(){}
    public virtual void Confirm(){}
    public virtual void Cancel(){}
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
            if(item.Visible) item.Draw();
        }
    }
    public virtual void Update(){}
    // public SwUiNode[] GetChildren()
    // {
    //     return [..Children];
    // }
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
    public static bool TryFromPrion<T>(PriNode priNode, out T uiNode) where T: SwUiNode
    {
        uiNode = default!;
        if(!TryFromPrion(priNode, out var node)) return false;
        if(node is not T val) return false;
        uiNode = val;
        return true;
    }
    public static bool TryFromPrion(PriNode priNode, out SwUiNode uiNode)
    {
        uiNode = null!;
        if(!priNode.TryGet("type", out string type)) return ErEngine.LogWarning("no type provided");
        try
        {
            switch (type)
            {
                case "menu_holder":
                    uiNode = new SwMenuHolder(priNode);
                break;
                case "menu":
                    uiNode = new SwMenu(priNode);
                break;
                case "text":
                    uiNode = new SwText(priNode);
                break;
                case "button":
                    uiNode = new SwButton(priNode);
                break;
                case "slider":
                break;
                case "toggle":
                break;
                default:
                    ErEngine.LogWarning("unexpected ui node type '", type, "'");
                break;
            }
        }
        catch(Exception e)
        {
            return ErEngine.LogWarning(e);
        }
        return uiNode is not null;
    }
}