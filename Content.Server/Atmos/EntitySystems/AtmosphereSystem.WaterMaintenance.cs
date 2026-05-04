// Content.Server/Atmos/EntitySystems/AtmosphereSystem.WaterMaintenance.cs

using Content.Server.GameTicking.Events;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;

namespace Content.Server.Atmos.EntitySystems;

public sealed partial class AtmosphereSystem
{
    private float _waterMaintenanceTimer = 0f;
    private const float WaterMaintenanceInterval = 2f; // Проверяем каждые 2 секунды

    // Целевые параметры для Water
    private const float TargetWaterPressure = 500f; // kPa
    private const float MinWaterTemperature = 247.15f; // -26°C
    private const float MaxWaterTemperature = 273.15f; // 0°C

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
        if (tile?.Air == null || !tile.Air.Immutable)
            return;

        var waterMoles = tile.Air.GetMoles(Gas.Water);
        if (waterMoles <= 0)
            return;

        // Рассчитываем нужное количество молей для целевого давления
        // P*V = n*R*T => n = (P*V)/(R*T)
        var targetMoles = (TargetWaterPressure * tile.Air.Volume) / (Atmospherics.R * tile.Air.Temperature);

        // Корректируем моли, если нужно
        if (Math.Abs(waterMoles - targetMoles) > Atmospherics.GasMinMoles)
        {
            tile.Air.SetMoles(Gas.Water, targetMoles);
        }

        // Корректируем температуру, если вышла за пределы
        if (tile.Air.Temperature < MinWaterTemperature || tile.Air.Temperature > MaxWaterTemperature)
        {
            // Случайная температура в допустимом диапазоне
            var newTemp = _random.NextFloat(MinWaterTemperature, MaxWaterTemperature);
            tile.Air.Temperature = newTemp;
        }

        // Убеждаемся, что смесь остается неизменяемой
        if (!tile.Air.Immutable)
            tile.Air.MarkImmutable();
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

    // Обновляем воду периодически (вызывается из Update)
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
