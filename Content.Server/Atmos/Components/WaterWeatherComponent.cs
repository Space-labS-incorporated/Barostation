// Content.Server/Atmos/Components/WaterWeatherComponent.cs

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Server.Atmos.Components;

/// <summary>
/// Автоматически добавляет погоду-воду на карту при инициализации.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WaterWeatherComponent : Component
{
    /// <summary>
    /// Прототип погоды для воды.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId WeatherPrototype = "WeatherWater";
}
