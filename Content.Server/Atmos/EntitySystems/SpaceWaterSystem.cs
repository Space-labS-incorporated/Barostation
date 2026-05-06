// Content.Server/Atmos/EntitySystems/SpaceWaterSystem.cs

using Content.Server.GameTicking.Events;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Robust.Shared.Map.Components;

namespace Content.Server.Atmos.EntitySystems;

public sealed class SpaceWaterSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        var query = EntityQueryEnumerator<GridAtmosphereComponent, MapGridComponent>();
        while (query.MoveNext(out var uid, out var gridAtmos, out var grid))
        {
            FillGridWithSpaceWater(uid, gridAtmos, grid);
        }
    }

    private void FillGridWithSpaceWater(EntityUid gridUid, GridAtmosphereComponent gridAtmos, MapGridComponent grid)
    {
        var enumerator = _mapSystem.GetAllTilesEnumerator(gridUid, grid);

        while (enumerator.MoveNext(out var tileRef))
        {
            if (!tileRef.HasValue)
                continue;

            var indices = tileRef.Value.GridIndices;

            // Проверяем, является ли тайл космосом
            if (!_atmosphere.IsTileSpace(gridUid, null, indices))
                continue;

            // Создаём космическую воду с правильными параметрами
            var spaceWater = CreateSpaceWaterMixture();

            // Устанавливаем смесь на тайл через API
            _atmosphere.SetTileMixtureInternal(gridUid, indices, spaceWater);

            // Инвалидируем тайл для обновления визуализации
            _atmosphere.InvalidateTile(gridUid, indices);
        }
    }

    private GasMixture CreateSpaceWaterMixture()
    {
        const float targetPressure = 1000f;
        const float targetTemp = Atmospherics.T0C; // 273.15K (0°C)
        var volume = Atmospherics.CellVolume; // 2500L

        // n = (P * V) / (R * T)
        var requiredMoles = (targetPressure * volume) / (Atmospherics.R * targetTemp);

        var mixture = new GasMixture(volume)
        {
            Temperature = targetTemp
        };

        mixture.SetMoles(Gas.Water, requiredMoles);

        // Убеждаемся, что другие газы отсутствуют
        mixture.SetMoles(Gas.Oxygen, 0);
        mixture.SetMoles(Gas.Nitrogen, 0);
        mixture.SetMoles(Gas.CarbonDioxide, 0);
        mixture.SetMoles(Gas.Plasma, 0);
        mixture.SetMoles(Gas.Tritium, 0);
        mixture.SetMoles(Gas.WaterVapor, 0);
        mixture.SetMoles(Gas.Ammonia, 0);
        mixture.SetMoles(Gas.NitrousOxide, 0);
        mixture.SetMoles(Gas.Frezon, 0);
        mixture.SetMoles(Gas.LiquidWater, 0);

        mixture.MarkImmutable();

        return mixture;
    }
}
