// Content.Server/Atmos/EntitySystems/SpaceWaterSystem.cs

using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.EntitySystems;

public sealed class SpaceWaterSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    private const float SpaceWaterAmount = 10000f;

    private float _updateCounter = 0f;
    private const float UpdateInterval = 5f;

    public override void Initialize()
    {
        base.Initialize();

        // Подписываемся только на событие старта раунда
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);

        // НЕ подписываемся на события GridAtmosphereComponent, так как они уже заняты
        // Новые гриды будут обработаны в Update() при следующем цикле
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        var query = EntityQueryEnumerator<GridAtmosphereComponent, MapGridComponent>();
        while (query.MoveNext(out var uid, out var gridAtmos, out var grid))
        {
            FillGridWithWater(uid, gridAtmos, grid);
        }
    }

    private void FillGridWithWater(EntityUid gridUid, GridAtmosphereComponent gridAtmos, MapGridComponent grid)
    {
        var enumerator = _mapSystem.GetAllTilesEnumerator(gridUid, grid);

        while (enumerator.MoveNext(out var tileRef))
        {
            if (!tileRef.HasValue)
                continue;

            var indices = tileRef.Value.GridIndices;

            var tileAir = _atmosphere.GetTileMixture(gridUid, null, indices);
            if (tileAir == null || tileAir.Immutable)
                continue;

            var isSpace = _atmosphere.IsTileSpace(gridUid, null, indices);

            if (isSpace)
            {
                var currentWater = tileAir.GetMoles(Gas.Water);
                var currentLiquidWater = tileAir.GetMoles(Gas.LiquidWater);

                if (currentWater + currentLiquidWater < SpaceWaterAmount)
                {
                    tileAir.SetMoles(Gas.Water, SpaceWaterAmount);
                    tileAir.SetMoles(Gas.LiquidWater, 0);
                    tileAir.Temperature = Atmospherics.TCMB;
                    tileAir.MarkImmutable();

                    _atmosphere.InvalidateTile(gridUid, indices);
                }
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _updateCounter += frameTime;
        if (_updateCounter < UpdateInterval)
            return;

        _updateCounter = 0f;

        var query = EntityQueryEnumerator<GridAtmosphereComponent, MapGridComponent>();
        while (query.MoveNext(out var uid, out var gridAtmos, out var grid))
        {
            var enumerator = _mapSystem.GetAllTilesEnumerator(uid, grid);

            while (enumerator.MoveNext(out var tileRef))
            {
                if (!tileRef.HasValue)
                    continue;

                var indices = tileRef.Value.GridIndices;

                var tileAir = _atmosphere.GetTileMixture(uid, null, indices);
                if (tileAir == null)
                    continue;

                var isSpace = _atmosphere.IsTileSpace(uid, null, indices);

                // Восстанавливаем воду, если она была удалена и тайл стал mutable
                if (isSpace && !tileAir.Immutable)
                {
                    var currentWater = tileAir.GetMoles(Gas.Water);
                    var currentLiquidWater = tileAir.GetMoles(Gas.LiquidWater);

                    if (currentWater + currentLiquidWater < SpaceWaterAmount * 0.9f)
                    {
                        tileAir.SetMoles(Gas.Water, SpaceWaterAmount);
                        tileAir.SetMoles(Gas.LiquidWater, 0);
                        tileAir.Temperature = Atmospherics.TCMB;
                        tileAir.MarkImmutable();

                        _atmosphere.InvalidateTile(uid, indices);
                    }
                }
            }
        }
    }
}
