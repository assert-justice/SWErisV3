using ErisMath;
using Prion.Node;

namespace SpoonWitch.UI.Node;

public class SwButton : SwUiNode
{
    private readonly SwText Text;
    public override ErVec2 MinSize => Text.MinSize;
    public SwButton(PriNode node) : base(node)
    {
        Text = new(node);
        AddChild(Text);
    }
    public override void SetPosition(ErVec2 position)
    {
        base.SetPosition(position);
        Text.SetPosition(position);
    }
}