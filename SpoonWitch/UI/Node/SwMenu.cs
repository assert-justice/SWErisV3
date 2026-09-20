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
    protected override void SetVisible(bool isVisible)
    {
        base.SetVisible(isVisible);
        // Note, SetVisible is idempotent, so this is fine
        FocusNode?.FocusEnd();
        FocusNode = null;
        if (!isVisible) return;
        // position children
        FocusableNodes.Clear();
        var pos = LocalPosition;
        foreach (var item in Children)
        {
            if(!item.Visible) continue;
            item.LocalPosition = pos;
            pos += new ErVec2(0, item.MinSize.Y);
            if(item.CanFocus) FocusableNodes.Add(item);
        }
        // focus first element
        foreach (var item in FocusableNodes)
        {
            if(!item.Visible) continue;
            SetFocus(item);
            break;
        }
        if(FocusNode is null) ErEngine.LogWarning("menu has no focus");
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