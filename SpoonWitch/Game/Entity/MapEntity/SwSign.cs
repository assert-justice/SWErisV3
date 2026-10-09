using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Rendering;

namespace SpoonWitch.Game.Entity.MapEntity;

public class SwSign: SwMapEntity
{
    private ErTexture? Texture;
    private ErTexture? UsePromptTexture;
    private SwSprite? UsePromptSprite;
    private readonly ErVec2 PromptOffset = new(0,-12);
    public bool IsUsable{get; private set;} = false;
    public override void Init()
    {
        base.Init();
        uint mask = (uint)SwCollisionMask.Solid;
        foreach (var tileCoord in RectTiles.GetInnerCoords())
        {
            Game.PhysicsWorld.SetTileMask(tileCoord,mask);
        }
        ErTexture.TryFromPath("game_data/map/props/sign.png", out Texture);
        if(ErTexture.TryFromPath("game_data/hud/use_prompt.png", out UsePromptTexture))
        {
            var frames = SwFrame.GetAllFrames(new(UsePromptTexture),new(10,10));
            SwAnimationState animationState = default;
            SwAnimationState.Set(ref animationState, fps:2, isLooping:true);
            SwAnimation animation = new("default",[..frames],new(10,10),animationState);
            UsePromptSprite = new("use_prompt");
            UsePromptSprite.AddAnimation(animation);
            UsePromptSprite.Offset = PromptOffset;
            RegisterComponent(new SwSpriteComponent(this, UsePromptSprite));
            UsePromptSprite.Play();
            UsePromptSprite.Visible = false;
        }
        RegisterComponent(new SwAreaComponent(this, "area", (uint)SwCollisionMask.PlayerTeam, new(48,48), enabled:true, onBodyEnter:OnEnter,onBodyExit:OnExit));
        AddGlobalHandler("player_use", OnUse);
    }
    protected override void Draw()
    {
        base.Draw();
        Texture?.Draw(Position - Texture.Size * 0.5);
    }
    private void OnEnter(SwEntity _)
    {
        IsUsable = true;
        UsePromptSprite?.Visible = true;
    }
    private void OnExit(SwEntity _)
    {
        IsUsable = false;
        UsePromptSprite?.Visible = false;
    }
    private void OnUse(PriNode _)
    {
        if(!IsUsable) return;
//         {
//   "verb": "show_text",
//   "title": "target broken!",
//   "text": "you smashed that sucker",
//   "duration": 1
// }
        PriDict command = [];
        command.TrySet("verb", "show_text");
        command.Add("title", Props.Get("fields/title"));
        command.Add("text", Props.Get("fields/text"));
        SwApp.CommandQueue.AddCommand(command);
    }
}
