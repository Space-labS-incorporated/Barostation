// Content.Shared/Atmos/Components/MapAtmosphereComponent.cs

using Content.Shared.Atmos.EntitySystems;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// Component that defines the default GasMixture for a map.
/// </summary>
[RegisterComponent, Access(typeof(SharedAtmosphereSystem))]
public sealed partial class MapAtmosphereComponent : SharedMapAtmosphereComponent
{
    private static GasMixture CreateSpaceWaterMixture()
    {
        // Правильные параметры: 1000 kPa, 0°C (273.15K)
        const float targetPressure = 1000f;
        const float targetTemp = Atmospherics.T0C; // 273.15K
        var volume = Atmospherics.CellVolume; // 2500L

        // n = (P * V) / (R * T)
        var requiredMoles = (targetPressure * volume) / (Atmospherics.R * targetTemp);

        var mixture = new GasMixture(volume)
        {
            Temperature = targetTemp
        };
        mixture.SetMoles(Gas.Water, requiredMoles);
        mixture.MarkImmutable(); // Используем метод вместо прямого присваивания

        return mixture;
    }

    /// <summary>
    /// The default GasMixture a map will have. Space mixture by default.
    /// </summary>
    [DataField]
    public GasMixture Mixture = CreateSpaceWaterMixture();  // #BaroStation - теперь 1000 kPa, 0°C

    /// <summary>
    /// Whether empty tiles will be considered space or not.
    /// </summary>
    [DataField]
    public bool Space = true;

    public SharedGasTileOverlaySystem.GasOverlayData Overlay;
}
