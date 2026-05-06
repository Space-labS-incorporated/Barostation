using Robust.Shared.GameStates;

namespace Content.Shared._BaroStation.NuclearReactor;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState]
public sealed partial class UraniumRodComponent : Component
{
    [DataField("fuel"), AutoNetworkedField]
    public float Fuel = 3000f;

    [DataField("maxFuel"), AutoNetworkedField]
    public float MaxFuel = 3000f;

    [DataField("heatMultiplier")]
    public float HeatMultiplier = 1.5f;

    [DataField("depleted"), AutoNetworkedField]
    public bool Depleted = false;
}
