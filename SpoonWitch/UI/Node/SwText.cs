using Eris;
using Eris.Renderer;
using Prion.Node;

namespace SpoonWitch.UI.Node;

public class SwText: SwUiNode
{
    private string _Text = string.Empty;
    public string Text{get => _Text; set{_Text = value;}}
    public ErColor FontColor = ErColor.Black;
    public float _FontSize = 16;
    public double FontSize
    {
        get => _FontSize;
        set
        {
            _FontSize = (float)value;
            _Font = null;
        }
    }
    private ErFont? _Font;

    public SwText(PriNode node) : base(node)
    {
    }

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