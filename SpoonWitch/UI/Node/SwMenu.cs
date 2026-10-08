using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.UI.Node;

namespace SpoonWitch.UI.Node;

public class SwMenu: SwUiNode
{
    public readonly string Id;
    private readonly List<SwUiNode> FocusableNodes = [];
    private SwUiNode? FocusNode;
    public SwMenu(PriNode node) : base(node)
    {
        if(!node.TryGet("id", out Id)) throw new("no id");
    }
    private ErVec2 GetMinSize()
    {
        double minX = 0;
        double minY = 0;
        foreach (var item in Children)
        {
            if(!item.Visible) continue;
            if(item.MinSize.X > minX) minX = item.MinSize.X;
            minY += item.MinSize.Y;
        }
        return new(minX, minY);
    }
    protected override void Clean()
    {
        base.Clean();
        UpdateLayout();
    }
    private void UpdateLayout()
    {
        ErVec2 minSize = GetMinSize();
        ErVec2 ul = new((SwApp.CameraSize.X - minSize.X)/2,(SwApp.CameraSize.Y - minSize.Y)/2);
        double posY = ul.Y;
        foreach (var item in Children)
        {
            if(!item.Visible) continue;
            double posX = SwApp.CameraSize.X / 2 - item.MinSize.X / 2;
            item.LocalPosition = new(posX,posY);
            posY += item.MinSize.Y;
        }
    }
    public override void Draw()
    {
        UpdateLayout();
        base.Draw();
    }
    protected override void SetVisible(bool isVisible)
    {
        base.SetVisible(isVisible);
        // Note, SetVisible is idempotent, so this is fine
        FocusNode?.FocusEnd();
        FocusNode = null;
        if (!isVisible) return;
        // position children
        FocusableNodes.Clear();
        UpdateLayout();
        foreach (var item in Children)
        {
            if(!item.Visible) continue;
            if(item.CanFocus) FocusableNodes.Add(item);
        }
        // focus first element
        foreach (var item in FocusableNodes)
        {
            if(!item.Visible) continue;
            SetFocus(item);
            break;
        }
        // if(FocusNode is null) ErEngine.LogWarning("menu has no focus");
    }
    public override void Up()
    {
        base.Up();
        FocusNext(-1);
    }
    public override void Down()
    {
        base.Down();
        FocusNext();
    }
    public override void Left()
    {
        base.Left();
        FocusNode?.Left();
    }
    public override void Right()
    {
        base.Right();
        FocusNode?.Right();
    }
    public override void Confirm()
    {
        base.Confirm();
        FocusNode?.Confirm();
    }
    private void SetFocus(SwUiNode node)
    {
        FocusNode?.FocusEnd();
        node.FocusBegin();
        FocusNode = node;
    }
    private void FocusNext(int direction = 1)
    {
        if(FocusNode is null) return;
        int startIdx = FocusableNodes.IndexOf(FocusNode!);
        int idx = startIdx;
        if(idx == -1)
        {
            ErEngine.LogWarning("bad menu focus");
            return;
        }
        while (true)
        {
            idx = ErMath.Mod(idx + direction, FocusableNodes.Count);
            // we have looped around and failed to find another focusable element
            if(idx == startIdx) return;
            if(!FocusableNodes[idx].Visible) continue;
            SetFocus(FocusableNodes[idx]);
            break;
        }
    }
}