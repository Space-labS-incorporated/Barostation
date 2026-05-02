// Content.Shared/Atmos/Components/WaterWetComponent.cs
using Robust.Shared.GameStates;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Компонент для существ, находящихся в воде
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]  // Добавлен AutoGenerateComponentState
public sealed partial class WaterWetComponent : Component
{
    /// <summary>
    /// Насколько существо мокрое (0-1)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Wetness = 0f;

    /// <summary>
    /// Скорость высыхания
    /// </summary>
    [DataField]
    public float DryingRate = 0.1f;

    /// <summary>
    /// Замедление движения в воде
    /// </summary>
    [DataField]
    public float SpeedModifier = 0.7f;
}
