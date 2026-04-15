using Robust.Shared.GameStates;

namespace Content.Shared._BaroStation.NuclearReactor;

[NetworkedComponent, RegisterComponent]
public sealed partial class UraniumRodComponent : Component
{
    /// <summary>
    /// Оставшееся топливо (в секундах работы на оптимальной температуре).
    /// </summary>
    [DataField("fuel"), ViewVariables(VVAccess.ReadWrite)]
    public float Fuel = 600f; // 10 минут

    /// <summary>
    /// Начальный запас топлива.
    /// </summary>
    [DataField("maxFuel")]
    public float MaxFuel = 600f;

    /// <summary>
    /// Множитель тепловыделения. Уран греет сильно.
    /// </summary>
    [DataField("heatMultiplier")]
    public float HeatMultiplier = 1.5f;
}
