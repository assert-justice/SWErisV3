using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;

namespace SpoonWitch.UI.Node;

public class SwText: SwUiNode
{
    private string _Text = string.Empty;
    public string Text{get => _Text; set{_Text = value;}}
    public ErColor FontColor = ErColor.Red;
    public float _FontSize = 16;
    private ErVec2? _MinSize;
    public override ErVec2 MinSize => _MinSize ??= Font?.GetStringSize(Text) ?? base.MinSize;
    public double FontSize
    {
        get => _FontSize;
        set
        {
            _FontSize = (float)value;
            _Font = null;
            _MinSize = null;
        }
    }
    private ErFont? _Font;
    public SwText(PriNode node) : base(node)
    {
        if(node.TryGet("text", out string s)) Text = s;
    }
    // private ErVec2 GetMinSize()
    // {
    //     if(_MinSize is not null) return _MinSize.Value;
    //     if(Font is null)
    //     {
    //         ErEngine.Log("dumb");
    //         return base.MinSize;
    //     }
    //     ErEngine.Log("text size", Font.GetStringSize(Text));
    //     return _MinSize ??= Font?.GetStringSize(Text) ?? base.MinSize;
    // }
    private ErFont? Font
    {
        get
        {
            if(_Font is null)
            {
                if(!SwApp.TryGetFont(_FontSize, out _Font)) ErEngine.LogWarning("failed to get font");
            }
            return _Font;
        }
    }
    public override void Draw()
    {
        base.Draw();
        Font?.DrawString(Text, FontColor, Position);
    }
}