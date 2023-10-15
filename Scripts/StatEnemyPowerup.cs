using Godot;


[GlobalClass]
public partial class StatEnemyPowerup : EnemyPowerup
{
    [Export]
    public float Value { get; private set; }
}
