using Content.Server.DeviceLinking.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Radiation.Components;
using Content.Server.Radiation.Systems;
using Content.Shared._BaroStation.NuclearReactor;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Radiation.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.UserInterface;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._BaroStation.NuclearReactor;

public sealed class NuclearReactorSystem : SharedNuclearReactorSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NuclearReactorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NuclearReactorComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<NuclearReactorComponent, EntInsertedIntoContainerMessage>(OnRodInserted);
        SubscribeLocalEvent<NuclearReactorComponent, EntRemovedFromContainerMessage>(OnRodRemoved);
        SubscribeLocalEvent<NuclearReactorComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<NuclearReactorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NuclearReactorComponent, AfterInteractUsingEvent>(OnAfterInteractUsing); // ЭТА СТРОКА ДОЛЖНА БЫТЬ
    }

    private void OnMapInit(EntityUid uid, NuclearReactorComponent comp, MapInitEvent args)
    {
        // Регистрируем порт источника для линковки с консолью
        _deviceLink.EnsureSourcePorts(uid, comp.LinkSourcePort);
    }

    private void OnAfterInteractUsing(EntityUid uid, NuclearReactorComponent comp, AfterInteractUsingEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        var toolSystem = EntityManager.System<SharedToolSystem>();
        if (!toolSystem.HasQuality(args.Used, "Pulsing"))
            return;

        // Проверяем, что цель - консоль управления реактором
        if (!TryComp<NuclearReactorConsoleComponent>(args.Target, out var consoleComp))
            return;

        // Отправляем сигнал линковки через DeviceLink
        _deviceLink.InvokePort(uid, comp.LinkSourcePort);

        _popup.PopupEntity(Loc.GetString("nuclear-reactor-console-link-success"), uid, args.User);
        args.Handled = true;
    }

    private void OnInit(EntityUid uid, NuclearReactorComponent comp, ComponentInit args)
    {
        var whitelist = new EntityWhitelist();
        whitelist.Components = new[] { "UraniumRod" };

        comp.RodSlot1.Whitelist = whitelist;
        comp.RodSlot2.Whitelist = whitelist;
        comp.RodSlot3.Whitelist = whitelist;
        comp.RodSlot4.Whitelist = whitelist;

        _itemSlots.AddItemSlot(uid, NuclearReactorComponent.RodSlot1Id, comp.RodSlot1);
        _itemSlots.AddItemSlot(uid, NuclearReactorComponent.RodSlot2Id, comp.RodSlot2);
        _itemSlots.AddItemSlot(uid, NuclearReactorComponent.RodSlot3Id, comp.RodSlot3);
        _itemSlots.AddItemSlot(uid, NuclearReactorComponent.RodSlot4Id, comp.RodSlot4);
        UpdateUI(uid, comp);
    }

    private void OnRemove(EntityUid uid, NuclearReactorComponent comp, ComponentRemove args)
    {
        _itemSlots.RemoveItemSlot(uid, comp.RodSlot1);
        _itemSlots.RemoveItemSlot(uid, comp.RodSlot2);
        _itemSlots.RemoveItemSlot(uid, comp.RodSlot3);
        _itemSlots.RemoveItemSlot(uid, comp.RodSlot4);
    }

    private void OnInteractUsing(EntityUid uid, NuclearReactorComponent comp, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var toolSystem = EntityManager.System<SharedToolSystem>();
        if (toolSystem.HasQuality(args.Used, "Pulsing"))
            return;

        if (!TryComp<UraniumRodComponent>(args.Used, out _))
            return;

        args.Handled = true;

        if (!comp.RodSlot1.HasItem)
            _itemSlots.TryInsert(uid, comp.RodSlot1, args.Used, args.User);
        else if (!comp.RodSlot2.HasItem)
            _itemSlots.TryInsert(uid, comp.RodSlot2, args.Used, args.User);
        else if (!comp.RodSlot3.HasItem)
            _itemSlots.TryInsert(uid, comp.RodSlot3, args.Used, args.User);
        else if (!comp.RodSlot4.HasItem)
            _itemSlots.TryInsert(uid, comp.RodSlot4, args.Used, args.User);
        else
            _popup.PopupEntity(Loc.GetString("nuclear-reactor-full"), uid, args.User);
    }

    private void OnRodInserted(EntityUid uid, NuclearReactorComponent comp, ContainerModifiedMessage args)
    {
        if (args.Container.ID == comp.RodSlot1.ID ||
            args.Container.ID == comp.RodSlot2.ID ||
            args.Container.ID == comp.RodSlot3.ID ||
            args.Container.ID == comp.RodSlot4.ID)
        {
            UpdateUI(uid, comp);
        }
    }

    private void OnRodRemoved(EntityUid uid, NuclearReactorComponent comp, ContainerModifiedMessage args)
    {
        if (args.Container.ID == comp.RodSlot1.ID ||
            args.Container.ID == comp.RodSlot2.ID ||
            args.Container.ID == comp.RodSlot3.ID ||
            args.Container.ID == comp.RodSlot4.ID)
        {
            UpdateUI(uid, comp);
        }
    }

    protected override void OnSetCoolingMessage(EntityUid uid, NuclearReactorComponent comp, NuclearReactorSetCoolingMessage args)
    {
        if (comp.Enabled)
            return;

        comp.CoolingLevel = Math.Clamp(args.CoolingLevel, 1, 8);
        UpdateUI(uid, comp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<NuclearReactorComponent, PowerSupplierComponent>();
        while (query.MoveNext(out var uid, out var comp, out var supplier))
        {
            if (!comp.Enabled)
            {
                supplier.MaxSupply = 0;
                continue;
            }

            if (_timing.CurTime < comp.NextUpdate)
                continue;

            comp.NextUpdate = _timing.CurTime + TimeSpan.FromSeconds(comp.UpdateInterval);
            ProcessReactor(uid, comp, supplier);
        }
    }

    private void ProcessReactor(EntityUid uid, NuclearReactorComponent comp, PowerSupplierComponent supplier)
    {
        float totalHeatGen = 0f;
        int rodCount = 0;

        CheckRod(comp.RodSlot1.Item, ref rodCount, ref totalHeatGen, comp);
        CheckRod(comp.RodSlot2.Item, ref rodCount, ref totalHeatGen, comp);
        CheckRod(comp.RodSlot3.Item, ref rodCount, ref totalHeatGen, comp);
        CheckRod(comp.RodSlot4.Item, ref rodCount, ref totalHeatGen, comp);

        if (rodCount == 0)
        {
            comp.CurrentTemperature = MathF.Max(293.15f, comp.CurrentTemperature - 50f * comp.UpdateInterval);
            supplier.MaxSupply = 0;
            UpdateUI(uid, comp);
            return;
        }

        comp.OptimalTemperature = rodCount * 1000f;
        float optimalMin = comp.OptimalTemperature - 300f;
        float optimalMax = comp.OptimalTemperature + 300f;

        float heating = totalHeatGen * comp.UpdateInterval * 0.05f;
        float coolingPower = 0.01f + (comp.CoolingLevel * 0.0075f);
        float cooling = (comp.CurrentTemperature - 293.15f) * coolingPower * comp.UpdateInterval;
        float targetInfluence = (comp.TargetTemperature - comp.CurrentTemperature) * 0.01f * comp.UpdateInterval;

        comp.CurrentTemperature += heating - cooling + targetInfluence;
        comp.CurrentTemperature = MathF.Max(293.15f, comp.CurrentTemperature);
        comp.CurrentTemperature = MathF.Min(comp.CurrentTemperature, comp.MaxTemperature);

        float efficiency;
        if (comp.CurrentTemperature < optimalMin)
        {
            efficiency = comp.CurrentTemperature / optimalMin * 0.7f;
        }
        else if (comp.CurrentTemperature > optimalMax)
        {
            float overheatFactor = (comp.CurrentTemperature - optimalMax) / comp.OptimalTemperature;
            efficiency = 1.0f + overheatFactor * 0.3f;

            float damage = overheatFactor * 0.2f * comp.UpdateInterval;
            comp.Integrity -= damage;

            if (_random.Prob(0.05f))
                _popup.PopupEntity(Loc.GetString("nuclear-reactor-overheat-warning"), uid, PopupType.LargeCaution);
        }
        else
        {
            efficiency = 1.0f;
            if (comp.Integrity < 100f)
                comp.Integrity = MathF.Min(100f, comp.Integrity + 0.05f * comp.UpdateInterval);
        }

        var generatedPower = comp.MaxPowerOutput * efficiency * (rodCount / 4f);
        supplier.MaxSupply = generatedPower;

        if (comp.Integrity <= 0)
        {
            Meltdown(uid, comp);
            return;
        }

        UpdateUI(uid, comp);
    }

    private void CheckRod(EntityUid? rodEntity, ref int rodCount, ref float totalHeatGen, NuclearReactorComponent comp)
    {
        if (rodEntity == null || !TryComp<UraniumRodComponent>(rodEntity, out var rod))
            return;

        if (rod.Depleted)
            return;

        rodCount++;

        var consumptionRate = 0.05f * (comp.CurrentTemperature / 800f);
        rod.Fuel = MathF.Max(0, rod.Fuel - consumptionRate * comp.UpdateInterval);

        if (rod.Fuel <= 0)
        {
            rod.Fuel = 0;
            rod.Depleted = true;
        }

        Dirty(rodEntity.Value, rod);

        if (rod.Fuel > 0)
        {
            totalHeatGen += rod.HeatMultiplier * (rod.Fuel / rod.MaxFuel) * 80f;
        }
    }

    private void Meltdown(EntityUid uid, NuclearReactorComponent comp)
    {
        _explosion.QueueExplosion(uid, "Default", 200, 5, 10, canCreateVacuum: true);
        EntityManager.QueueDeleteEntity(uid);
    }

    protected override void UpdateUI(EntityUid uid, NuclearReactorComponent comp)
    {
        var slots = new ContainerInfo[4];
        slots[0] = GetSlotInfo(comp.RodSlot1.Item);
        slots[1] = GetSlotInfo(comp.RodSlot2.Item);
        slots[2] = GetSlotInfo(comp.RodSlot3.Item);
        slots[3] = GetSlotInfo(comp.RodSlot4.Item);

        var power = 0f;
        if (TryComp<PowerSupplierComponent>(uid, out var supplier))
            power = supplier.CurrentSupply;

        var state = new NuclearReactorUiState(
            comp.Enabled,
            comp.CurrentTemperature,
            comp.TargetTemperature,
            power,
            comp.Integrity,
            slots,
            comp.OptimalTemperature,
            comp.OptimalTemperature * 2.5f,
            comp.CoolingLevel,
            GetAnyDepleted(slots)
        );

        // Всегда отправляем обновление на консоли, даже если UI реактора не открыт
        var consoleQuery = EntityQueryEnumerator<NuclearReactorConsoleComponent>();
        while (consoleQuery.MoveNext(out var consoleUid, out var consoleComp))
        {
            if (consoleComp.LinkedReactor == uid)
            {
                var consoleSys = EntityManager.System<SharedNuclearReactorConsoleSystem>();
                consoleSys.UpdateFromReactor(consoleUid, consoleComp, state);
            }
        }

        // Отправляем обновление на UI реактора, только если он открыт
        var uiSystem = EntityManager.System<SharedUserInterfaceSystem>();
        if (uiSystem.IsUiOpen(uid, NuclearReactorUiKey.Key))
        {
            uiSystem.SetUiState(uid, NuclearReactorUiKey.Key, state);
        }
    }
    private bool GetAnyDepleted(ContainerInfo[] slots)
    {
        foreach (var slot in slots)
        {
            if (slot.Depleted)
                return true;
        }
        return false;
    }

    private ContainerInfo GetSlotInfo(EntityUid? item)
    {
        if (item == null)
            return new ContainerInfo(false, null, null, false);

        var name = MetaData(item.Value).EntityName;

        if (TryComp<UraniumRodComponent>(item, out var rod))
        {
            var fuel = rod.Depleted ? 0 : rod.Fuel / rod.MaxFuel;
            return new ContainerInfo(true, name, fuel, rod.Depleted);
        }

        return new ContainerInfo(true, name, null, false);
    }

    protected override void OnToggleMessage(EntityUid uid, NuclearReactorComponent comp, NuclearReactorToggleMessage args)
    {
        comp.Enabled = !comp.Enabled;
        _itemSlots.SetLock(uid, comp.RodSlot1, comp.Enabled);
        _itemSlots.SetLock(uid, comp.RodSlot2, comp.Enabled);
        _itemSlots.SetLock(uid, comp.RodSlot3, comp.Enabled);
        _itemSlots.SetLock(uid, comp.RodSlot4, comp.Enabled);

        // Управление радиацией через RadiationSystem
        var radiationSystem = EntityManager.System<RadiationSystem>();
        radiationSystem.SetSourceEnabled(uid, comp.Enabled);

        UpdateUI(uid, comp);
    }

    protected override void OnSetTemperatureMessage(EntityUid uid, NuclearReactorComponent comp, NuclearReactorSetTemperatureMessage args)
    {
        comp.TargetTemperature = Math.Clamp(args.Temperature, 300f, comp.MaxTemperature);
        UpdateUI(uid, comp);
    }

    protected override void OnEjectMessage(EntityUid uid, NuclearReactorComponent comp, NuclearReactorEjectMessage args)
    {
        ItemSlot? slot = args.Slot switch
        {
            0 => comp.RodSlot1,
            1 => comp.RodSlot2,
            2 => comp.RodSlot3,
            3 => comp.RodSlot4,
            _ => null
        };

        if (slot == null || !slot.HasItem)
            return;

        _itemSlots.TryEjectToHands(uid, slot, null);
        UpdateUI(uid, comp);
    }
}
