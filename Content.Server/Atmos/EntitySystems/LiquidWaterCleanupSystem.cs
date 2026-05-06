// Content.Server/Atmos/EntitySystems/LiquidWaterCleanupSystem.cs

using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;

namespace Content.Server.Atmos.EntitySystems;

/// <summary>
/// Удаляет жидкую воду (LiquidWater) с тайлов, которые становятся космосом
/// или выходят за пределы станции.
/// </summary>
public sealed class LiquidWaterCleanupSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    private float _updateCounter = 0f;
    private const float UpdateInterval = 0.5f;

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
            foreach (var (indices, tile) in gridAtmos.Tiles)
            {
                if (tile.Air == null)
                    continue;

                var liquidWater = tile.Air.GetMoles(Gas.LiquidWater);
                if (liquidWater <= 0)
                    continue;

                // Если тайл стал космосом или не принадлежит станции
                if (tile.Space || tile.MapAtmosphere)
                {
                    // Превращаем жидкую воду обратно в обычную (невидимую) воду
                    tile.Air.AdjustMoles(Gas.LiquidWater, -liquidWater);
                    tile.Air.AdjustMoles(Gas.Water, liquidWater);

                    // Используем публичный API для инвалидации тайла
                    _atmosphere.InvalidateTile(uid, indices);
                }
            }
        }
    }
}
