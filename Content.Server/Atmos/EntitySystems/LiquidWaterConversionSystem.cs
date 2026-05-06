// Content.Server/Atmos/EntitySystems/LiquidWaterConversionSystem.cs

using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Maps;

namespace Content.Server.Atmos.EntitySystems;

/// <summary>
/// Конвертирует невидимую воду (Water) в видимую жидкую воду (LiquidWater)
/// на тайлах, которые принадлежат гридам (станциям).
/// </summary>
public sealed class LiquidWaterConversionSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    private float _updateCounter = 0f;
    private const float UpdateInterval = 0.5f;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateCounter += frameTime;
        if (_updateCounter < UpdateInterval)
            return;

        _updateCounter = 0f;

        var query = EntityQueryEnumerator<GridAtmosphereComponent>();
        while (query.MoveNext(out var uid, out var gridAtmos))
        {
            ConvertWaterToLiquidOnGrid((uid, gridAtmos));
        }
    }

    private void ConvertWaterToLiquidOnGrid(Entity<GridAtmosphereComponent> ent)
    {
        foreach (var (indices, tile) in ent.Comp.Tiles)
        {
            if (tile.Air == null || tile.MapAtmosphere)
                continue;

            var waterMoles = tile.Air.GetMoles(Gas.Water);
            var liquidWaterMoles = tile.Air.GetMoles(Gas.LiquidWater);

            if (waterMoles > 0.01f)
            {
                // Конвертируем Water в LiquidWater
                tile.Air.AdjustMoles(Gas.Water, -waterMoles);
                tile.Air.AdjustMoles(Gas.LiquidWater, waterMoles);

                // Используем публичный API для инвалидации тайла
                _atmosphere.InvalidateTile(ent.Owner, indices);
            }
        }
    }
}
