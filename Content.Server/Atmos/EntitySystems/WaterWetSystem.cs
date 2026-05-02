// Content.Server/Atmos/EntitySystems/WaterWetSystem.cs
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.GameObjects;

namespace Content.Server.Atmos.EntitySystems;

public sealed class WaterWetSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<WaterWetComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<WaterWetComponent, AtmosExposedUpdateEvent>(OnAtmosExposed);
    }

    private void OnInit(Entity<WaterWetComponent> ent, ref ComponentInit args)
    {
        UpdateWetness(ent);
    }

    private void OnAtmosExposed(Entity<WaterWetComponent> ent, ref AtmosExposedUpdateEvent args)
    {
        UpdateWetness(ent, args.GasMixture);
    }

    private void UpdateWetness(Entity<WaterWetComponent> ent, GasMixture? mixture = null)
    {
        // ИСПРАВЛЕНО: Получаем TransformComponent для передачи в GetContainingMixture
        if (!TryComp<TransformComponent>(ent, out var xform))
            return;

        mixture ??= _atmosphere.GetContainingMixture((ent, xform));

        var waterMoles = mixture?.GetMoles(Gas.Water) ?? 0;
        var oldWetness = ent.Comp.Wetness;

        if (waterMoles > 0.1f)
        {
            ent.Comp.Wetness = Math.Min(1f, ent.Comp.Wetness + 0.1f);
        }
        else
        {
            ent.Comp.Wetness = Math.Max(0f, ent.Comp.Wetness - ent.Comp.DryingRate);
        }

        if (Math.Abs(oldWetness - ent.Comp.Wetness) > 0.01f)
        {
            Dirty(ent);
            UpdateMovementSpeed(ent);
        }
    }

    private void UpdateMovementSpeed(Entity<WaterWetComponent> ent)
    {
        var modifier = 1f - (ent.Comp.Wetness * (1f - ent.Comp.SpeedModifier));

        // ИСПРАВЛЕНО: Просто обновляем модификаторы скорости через событие
        var ev = new RefreshMovementSpeedModifiersEvent();
        ev.ModifySpeed(modifier);
        RaiseLocalEvent(ent, ev);

        // Также обновляем компонент скорости (если есть)
        if (TryComp<MovementSpeedModifierComponent>(ent, out var moveComp))
        {
            _movementSpeed.RefreshMovementSpeedModifiers(ent, moveComp);
        }
    }
}
