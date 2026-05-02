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
        var mixture = new GasMixture(Atmospherics.CellVolume)
        {
            Temperature = Atmospherics.TCMB
        };
        mixture.AdjustMoles(Gas.Water, 1000f);
        mixture.MarkImmutable();
        return mixture;
    }

    /// <summary>
    /// The default GasMixture a map will have. Space mixture by default.
    /// </summary>
    [DataField]
    public GasMixture Mixture = CreateSpaceWaterMixture();  // #BaroStation

    /// <summary>
    /// Whether empty tiles will be considered space or not.
    /// </summary>
    [DataField]
    public bool Space = true;

    public SharedGasTileOverlaySystem.GasOverlayData Overlay;
}
