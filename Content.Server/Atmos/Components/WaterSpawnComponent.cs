// Content.Server/Atmos/Components/WaterSpawnComponent.cs - ИСПРАВЛЕНАЯ ВЕРСИЯ
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
    /// Рассчитано для давления 1000 kPa при 0°C
    /// n = (1000 * 2500) / (8.314 * 273.15) ≈ 1100.5 моль
    /// </summary>
    [DataField("waterAmount")]
    public float WaterAmount = 1100.5f; // ИСПРАВЛЕНО

    /// <summary>
    /// Температура воды в космосе - теперь 0°C
    /// </summary>
    [DataField("waterTemperature")]
    public float WaterTemperature = Atmospherics.T0C; // 273.15K - ИСПРАВЛЕНО
}
