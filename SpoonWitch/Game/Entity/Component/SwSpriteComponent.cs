using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.ByteStream;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.Component;

public class SwSpriteComponent(SwEntity parent, SwSprite sprite) : SwComponent(parent, sprite.Name)
{
    public readonly SwSprite Sprite = sprite;
    public override void Update()
    {
        base.Update();
        Sprite.Update();
    }
    public override void Draw(SwComponent nextState)
    {
        base.Draw(nextState);
        var pos = ErMath.Lerp(Parent.Position, nextState.Parent.Position, SwGame.FrameWeight);
        Sprite.Draw(pos);
    }
    public override void Read(SwByteStream byteStream)
    {
        base.Read(byteStream);
    }
    public override void Write(SwByteStream byteStream)
    {
        base.Write(byteStream);
    }
}
