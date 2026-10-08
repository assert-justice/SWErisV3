using Eris;
using ErisMath;
using Prion.Node;
using SpoonWitch.Game.Entity.Component;
using SpoonWitch.Utils;

namespace SpoonWitch.Game.Entity.MapEntity;

public class SwFwoosher: SwMapEntity
{
    // private SwAreaComponent Area = null!;
    private SwHurtboxComponent Hurtbox = null!;
    private SwSpriteComponent Sprite = null!;
    public SwCollisionMask Mask;
    private readonly SwClock Clock;
    private const int NumPhases = 3;
    private int Phase = 0;
    private double Delay;
    private double TimeEnabled;
    private double Cooldown;
    private bool StartActive;
    public bool IsActive{get; private set;} = false;
    public SwFwoosher()
    {
        Clock = AddClock();
    }
    public override void SetProps(PriNode props)
    {
        base.SetProps(props);
        if(Props.TryGet("mask", out uint mask)) Mask = (SwCollisionMask)mask;
        var cycle = Props.Get("cycle");
        if(cycle.TryGet("delay", out double d)) Delay = d;
        if(cycle.TryGet("time_enabled", out d)) TimeEnabled = d;
        if(cycle.TryGet("cooldown", out d)) Cooldown = d;
        if(cycle.TryGet("start_active", out bool b)) StartActive = b;
    }
    public override void Init()
    {
        base.Init();
        LoadSprites("sprites");
        if(!SwHurtboxComponent.TryFromData(out Hurtbox, this, Props.Get("hurtbox"))) ErEngine.LogWarning("bad hurtbox");
        else
        {
            Hurtbox.Size = Size;
            RegisterComponent(Hurtbox);
        }
        Sprite = GetComponent<SwSpriteComponent>("sprite")!;
        Sprite.Sprite.Visible = false;
        Sprite.Sprite.Centered = false;
        SetActive(StartActive);
    }
    protected override void Update(double dt)
    {
        base.Update(dt);
        if(!IsActive) return;
        if(Clock.IsRunning) return;
        switch (Phase)
        {
            case 0:
                Clock.Start(Delay);
                break;
            case 1:
                Clock.Start(TimeEnabled);
                // Enable
                SetEnabled(true);
                break;
            case 2:
                Clock.Start(Cooldown);
                // Disable
                SetEnabled(false);
                break;
        }
        Phase = (Phase + 1) % NumPhases;
    }
    protected override void Draw()
    {
        base.Draw();
        Sprite.Sprite.Visible = true;
        foreach (var item in RectTiles.GetInnerCoords())
        {
            var p = (ErVec2)(item * Game.Map.TileSize);
            Sprite.Sprite.Draw(p);
        }
        Sprite.Sprite.Visible = false;
    }
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        Hurtbox.Enabled = false;
        Phase = 0;
    }
    private void SetEnabled(bool isEnabled)
    {
        Hurtbox.Enabled = isEnabled;
        Sprite.Sprite.Play(isEnabled ? "fwoosh" : "default");
    }
}
