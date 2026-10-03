using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Data;

namespace SpoonWitch.UI.Node;

public class SwText: SwUiNode
{
    private string _Text = string.Empty;
    public string Text
    {
        get => _Text;
        set
        {
            _Text = value;
            _MinSize = null;
        }
    }
    private string FontPath = string.Empty;
    private string _FontName = "default_font";
    public string FontName
    {
        get => _FontName;
        set
        {
            if(!SwData.UiConfig.TryGet($"fonts/{value}", out string fontPath))
            {
                ErEngine.LogWarning("no font of name ", value, " found");
                return;
            }
            FontPath = fontPath;
            _FontName = value;
        }
    }
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
    public ErFont? Font
    {
        get
        {
            if(_Font is null)
            {
                if(!ErFont.TryLoad(FontPath, _FontSize, out _Font)) ErEngine.LogError("failed to load font from path ", FontPath);
            }
            return _Font;
        }
    }
    public SwText(PriNode node) : base(node)
    {
        // init font path
        FontName = _FontName;
        if(node.TryGet("text", out string s)) Text = s;
        if(node.TryGet("font_size", out double font_size)) FontSize = font_size;
        if(node.TryGet("font_color", out string font_color))
        {
            if(!ErColor.TryParse(font_color, out var color)) ErEngine.LogWarning("bad color string '", font_color, "'");
            else FontColor = color;
        }
    }
    public override void Draw()
    {
        base.Draw();
        Font?.DrawString(Text, FontColor, GlobalPosition);
    }
}