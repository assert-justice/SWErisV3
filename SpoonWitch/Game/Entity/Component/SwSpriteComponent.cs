using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.ByteStream;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.Component;

public class SwSpriteComponent : SwComponent
{
    public readonly SwSprite Sprite;
    public SwSpriteComponent(SwEntity parent, SwSprite sprite) : base(parent, sprite.Name)
    {
        Sprite = sprite;
    }
    public override void Update(double dt)
    {
        base.Update(dt);
        Sprite.Update(dt);
    }
    public override void Draw()
    {
        base.Draw();
        Sprite.Draw(Parent.Position);
    }
}
