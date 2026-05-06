// Content.Server/Atmos/EntitySystems/AtmosphereSystem.WaterMaintenance.cs

using Content.Server.GameTicking.Events;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    private float _waterMaintenanceTimer = 0f;
    private const float WaterMaintenanceInterval = 5f; // Проверяем раз в 5 секунд

    // Целевые параметры для космической воды
    private const float TargetWaterPressure = 1000f; // kPa
    private const float TargetWaterTemperature = Atmospherics.T0C; // 273.15 K (0°C)

    private void InitializeWaterMaintenance()
    {
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStartingForWater);
    }

    private void OnRoundStartingForWater(RoundStartingEvent ev)
    {
        // При старте раунда обновляем всю воду на всех гридах
        var query = EntityQueryEnumerator<GridAtmosphereComponent>();
        while (query.MoveNext(out var uid, out var gridAtmos))
        {
            MaintainWaterOnGrid((uid, gridAtmos));
        }
    }

    public void MaintainWaterOnTile(GridAtmosphereComponent gridAtmos, TileAtmosphere tile)
    {
        // Работаем только с иммутабельной (космической) водой
        if (tile?.Air == null || !tile.Air.Immutable)
            return;

        var waterMoles = tile.Air.GetMoles(Gas.Water);
        if (waterMoles <= 0)
            return;

        var changed = false;

        // 1. Фиксируем температуру на 0°C
        if (Math.Abs(tile.Air.Temperature - TargetWaterTemperature) > 0.1f)
        {
            tile.Air.Temperature = TargetWaterTemperature;
            changed = true;
        }

        // 2. Рассчитываем нужное количество молей для давления 1000 kPa
        // n = (P * V) / (R * T)
        var targetMoles = (TargetWaterPressure * tile.Air.Volume) /
                          (Atmospherics.R * TargetWaterTemperature);

        // 3. Корректируем количество молей, если нужно
        if (Math.Abs(waterMoles - targetMoles) > 0.1f)
        {
            tile.Air.SetMoles(Gas.Water, targetMoles);
            changed = true;
        }

        // 4. Убеждаемся, что смесь остаётся иммутабельной
        if (!tile.Air.Immutable)
        {
            tile.Air.MarkImmutable();
            changed = true;
        }

        // Если что-то изменилось, обновляем визуализацию
        if (changed && TryComp(tile.GridIndex, out GasTileOverlayComponent? overlay))
        {
            InvalidateVisuals((tile.GridIndex, overlay), tile.GridIndices);
        }
    }

    public void MaintainWaterOnGrid(Entity<GridAtmosphereComponent> ent)
    {
        foreach (var (indices, tile) in ent.Comp.Tiles)
        {
            if (tile.Air?.GetMoles(Gas.Water) > 0)
            {
                MaintainWaterOnTile(ent.Comp, tile);
            }
        }
    }

    private void UpdateWaterMaintenance(float frameTime)
    {
        _waterMaintenanceTimer += frameTime;
        if (_waterMaintenanceTimer < WaterMaintenanceInterval)
            return;

        _waterMaintenanceTimer = 0f;

        var query = EntityQueryEnumerator<GridAtmosphereComponent>();
        while (query.MoveNext(out var uid, out var gridAtmos))
        {
            MaintainWaterOnGrid((uid, gridAtmos));
        }
    }
}
