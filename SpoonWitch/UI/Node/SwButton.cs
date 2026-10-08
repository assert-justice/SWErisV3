using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;

namespace SpoonWitch.UI.Node;

public class SwButton : SwUiNode
{
    private readonly SwText TextNode;
    private string _Text = string.Empty;
    private const char Sep = '-';
    public string Text
    {
        get => _Text;
        set
        {
            _Text = value;
            if (HasFocus)
            {
                TextNode.Text = Sep + value + Sep;
            }
            else
            {
                TextNode.Text = value;
            }
        }
    }
    public override ErVec2 MinSize => TextNode.MinSize;
    private ErColor DefaultColor = ErColor.Red;
    private ErColor FocusColor = ErColor.White;
    protected override bool CanFocusPro => true;
    private readonly PriNode Command = PriNull.Null;
    public SwButton(PriNode node) : base(node)
    {
        TextNode = new(node);
        AddChild(TextNode);
        DefaultColor = TextNode.FontColor;
        if(node.TryGet("font_color_focus", out string font_color_focus))
        {
            if(!ErColor.TryParse(font_color_focus, out var color)) ErEngine.LogWarning("bad color string '", font_color_focus, "'");
            else FocusColor = color;
        }
        Text = TextNode.Text;
        Command = node.Get("on_click_command");
    }
    public override void FocusBegin()
    {
        base.FocusBegin();
        TextNode.FontColor = FocusColor;
        Text = _Text;
    }
    public override void FocusEnd()
    {
        base.FocusEnd();
        TextNode.FontColor = DefaultColor;
        Text = _Text;
    }
    public override void Confirm()
    {
        base.Confirm();
        if(Command != PriNull.Null) SwApp.CommandQueue.AddCommand(Command);
    }
}