using Eris.Renderer;
using SpoonWitch.UI.Node;

namespace SpoonWitch.UI.Menu;

public class SwMainMenu : SwMenu
{
    //
    public SwMainMenu() : base("main_menu")
    {
        SwText text = new()
        {
            Text = "Spoon Witch",
            FontColor = ErColor.White,
        };
        AddChild(text);
    }
}