using Robust.Shared.GameStates;

namespace Content.Shared._BaroStation.NuclearReactor;

[NetworkedComponent, RegisterComponent, AutoGenerateComponentState]
public sealed partial class UraniumRodComponent : Component
{
    /// <summary>
    /// Оставшееся топливо (в секундах работы на оптимальной температуре).
    /// </summary>
    [DataField("fuel"), AutoNetworkedField]
    public float Fuel = 600f;

    /// <summary>
    /// Начальный запас топлива.
    /// </summary>
    [DataField("maxFuel"), AutoNetworkedField]
    public float MaxFuel = 600f;

    /// <summary>
    /// Множитель тепловыделения. Уран греет сильно.
    /// </summary>
    [DataField("heatMultiplier")]
    public float HeatMultiplier = 1.5f;
}
