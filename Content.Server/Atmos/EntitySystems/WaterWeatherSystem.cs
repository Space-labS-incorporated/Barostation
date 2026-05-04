// Content.Server/Atmos/EntitySystems/WaterWeatherSystem.cs

using Content.Server.GameTicking;
using Content.Server.Weather;
using Content.Shared.Atmos.Components;
using Content.Shared.Weather;
using Content.Server.Atmos.Components;
using Content.Shared.GameTicking;

namespace Content.Server.Atmos.EntitySystems;

public sealed class WaterWeatherSystem : EntitySystem
{
    [Dependency] private readonly WeatherSystem _weather = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WaterWeatherComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<WaterWeatherComponent, ComponentRemove>(OnComponentRemove);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnMapInit(Entity<WaterWeatherComponent> ent, ref MapInitEvent args)
    {
        // Добавляем погоду-воду на карту
        _weather.TryAddWeather(ent, ent.Comp.WeatherPrototype, out _, null);
    }

    private void OnComponentRemove(Entity<WaterWeatherComponent> ent, ref ComponentRemove args)
    {
        // Удаляем погоду-воду при удалении компонента
        _weather.TryRemoveWeather(ent, ent.Comp.WeatherPrototype);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        // При рестарте раунда нужно заново добавить воду на все карты
        // Это делается через MapInitEvent при инициализации карт
    }
}
