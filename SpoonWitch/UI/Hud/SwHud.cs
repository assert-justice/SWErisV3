using Eris.Renderer;
using ErisMath;

namespace SpoonWitch.UI.Hud;

public class SwHud
{
    private readonly ErTexture Texture = ErTexture.GetColoredTexture(SwApp.INTERNAL_WIDTH,SwApp.HUD_HEIGHT, ErColor.White);
    public void Draw()
    {
        Texture.Draw(ErVec2.Zero);
    }
}