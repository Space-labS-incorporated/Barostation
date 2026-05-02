// Content.Server/Atmos/EntitySystems/SpaceWaterSystem.cs
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.GameTicking;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.EntitySystems;

public sealed class SpaceWaterSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    private const float SpaceWaterAmount = 10000f;

    private float _updateCounter = 0f;
    private const float UpdateInterval = 2f; // Обновляем раз в 2 секунды

    public override void Initialize()
    {
        base.Initialize();
        // Подписываемся только на начало раунда
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        // Заполняем все существующие сетки водой при старте раунда
        var query = EntityQueryEnumerator<GridAtmosphereComponent>();
        while (query.MoveNext(out var uid, out var gridAtmos))
        {
            FillGridWithWater(uid, gridAtmos);
        }
    }

    private void FillGridWithWater(EntityUid gridUid, GridAtmosphereComponent gridAtmos)
    {
        foreach (var (indices, tile) in gridAtmos.Tiles)
        {
            if (tile.Space && !tile.MapAtmosphere)
            {
                var tileAir = _atmosphere.GetTileMixture(gridUid, null, indices);
                if (tileAir != null && !tileAir.Immutable)
                {
                    var currentWater = tileAir.GetMoles(Gas.Water);
                    if (currentWater < SpaceWaterAmount)
                    {
                        tileAir.AdjustMoles(Gas.Water, SpaceWaterAmount - currentWater);
                    }
                }
            }
        }
    }

    // Периодическая проверка во время игры
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
                if (tile.Space && !tile.MapAtmosphere)
                {
                    var tileAir = _atmosphere.GetTileMixture(uid, null, indices);
                    if (tileAir != null && !tileAir.Immutable)
                    {
                        var currentWater = tileAir.GetMoles(Gas.Water);
                        if (currentWater < SpaceWaterAmount * 0.9f)
                        {
                            tileAir.AdjustMoles(Gas.Water, SpaceWaterAmount - currentWater);
                        }
                    }
                }
            }
        }
    }
}
