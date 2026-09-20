namespace SpoonWitch.Game.Effect.Spell;

public class SwSpell
{
    public virtual double ManaCost => 10;
    private bool _IsActive = false;
    public bool IsActive => _IsActive;
    public virtual void Begin()
    {
        _IsActive = true;
    }
    public virtual void End()
    {
        _IsActive = false;
    }
    public virtual void Update(){}
    public virtual void Draw(){}
}
