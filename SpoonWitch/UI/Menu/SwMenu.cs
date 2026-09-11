using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.UI.Node;

namespace SpoonWitch.UI.Menu;

public class SwMenu: SwUiNode
{
    public readonly string Id;
    public SwMenu(PriNode node) : base(node)
    {
        if(!node.TryGet("id", out Id)) throw new("no id");
    }
    protected override void SetVisible(bool isVisible)
    {
        base.SetVisible(isVisible);
        if (!isVisible) return;
        // position children
        var pos = Position;
        foreach (var item in Children)
        {
            item.SetPosition(pos);
            pos += new ErVec2(0, item.MinSize.Y);
        }
        // focus first element
    }
}