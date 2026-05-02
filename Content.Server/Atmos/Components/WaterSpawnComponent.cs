// Content.Server/Atmos/Components/WaterSpawnComponent.cs
using Content.Shared.Atmos;

namespace Content.Server.Atmos.Components;

/// <summary>
/// Компонент для управления "космической водой"
/// </summary>
[RegisterComponent]
public sealed partial class WaterSpawnComponent : Component
{
    /// <summary>
    /// Количество воды, которое будет добавлено при инициализации карты
    /// </summary>
    [DataField("waterAmount")]
    public float WaterAmount = 1000f;

    /// <summary>
    /// Температура воды в космосе
    /// </summary>
    [DataField("waterTemperature")]
    public float WaterTemperature = Atmospherics.TCMB;
}
