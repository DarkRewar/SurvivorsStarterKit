using Godot;

public enum PowerupType
{
    // Weapons
    ShootingDamages,
    ShootingAttackSpeed,
    ShootingBulletSpeed,
    FloatingSphereCount,
    FloatingSphereDamages,
    LifestealDamages,
    LifestealCooldown,
    SpiritWaterDamages,
    SpiritWaterDuration,
    SpiritWaterCooldown,

    // Stats
    Lifepoints,
    Strength,
    Armor,
    Speed,
    AttackSpeed,
}

[GlobalClass]
public partial class Powerup : Resource
{
    [Export]
    public PowerupType Type;

    [Export]
    public string Name;

    [Export(PropertyHint.MultilineText)]
    public string Description;

    [Export]
    public bool IsCumulable = true;

    [Export]
    public int MaxCumul = 1;

    public Powerup() { }
}
