using Eris;
using Eris.Renderer;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;

namespace SpoonWitch.Game.Entity.MapEntity;

public class SwSign: SwMapEntity
{
    private ErTexture? Texture;
    private ErTexture? UsePromptTexture;
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
        ErTexture.TryFromPath("game_data/hud/use_prompt.png", out UsePromptTexture);
        RegisterComponent(new SwAreaComponent(this, "area", (uint)SwCollisionMask.PlayerTeam, new(48,48), enabled:true, onBodyEnter:OnEnter,onBodyExit:OnExit));
        AddGlobalHandler("player_use", OnUse);
    }
    protected override void Draw()
    {
        base.Draw();
        Texture?.Draw(Position - Texture.Size * 0.5);
        if(IsUsable) UsePromptTexture?.Draw(Position - UsePromptTexture.Size * 0.5 + PromptOffset);
    }
    private void OnEnter(SwEntity _)
    {
        IsUsable = true;
    }
    private void OnExit(SwEntity _)
    {
        IsUsable = false;
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
