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
        if (isVisible)
        {
            // focus first element
        }
    }
}