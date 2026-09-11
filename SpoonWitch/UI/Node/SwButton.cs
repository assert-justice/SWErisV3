using Eris.Renderer;
using ErisMath;
using Prion.Node;

namespace SpoonWitch.UI.Node;

public class SwButton : SwUiNode
{
    private readonly SwText Text;
    public override ErVec2 MinSize => Text.MinSize;
    private ErColor DefaultColor = ErColor.Red;
    private ErColor FocusColor = ErColor.White;
    protected override bool CanFocusPro => true;
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
    public override void FocusBegin()
    {
        base.FocusBegin();
        Text.FontColor = FocusColor;
    }
    public override void FocusEnd()
    {
        base.FocusEnd();
        Text.FontColor = DefaultColor;
    }
}