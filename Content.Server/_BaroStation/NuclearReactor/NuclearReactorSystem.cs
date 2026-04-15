using Content.Server.Explosion.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._BaroStation.NuclearReactor;
using Content.Shared.Audio;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
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
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NuclearReactorComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NuclearReactorComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<NuclearReactorComponent, EntInsertedIntoContainerMessage>(OnRodInserted);
        SubscribeLocalEvent<NuclearReactorComponent, EntRemovedFromContainerMessage>(OnRodRemoved);
        SubscribeLocalEvent<NuclearReactorComponent, InteractUsingEvent>(OnInteractUsing);
        // Подписка на NuclearReactorSetCoolingMessage уже есть в SharedNuclearReactorSystem
    }
    private void OnInit(EntityUid uid, NuclearReactorComponent comp, ComponentInit args)
    {
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
            UpdateCoolingLevel(uid, comp);
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
            UpdateCoolingLevel(uid, comp);
            UpdateUI(uid, comp);
        }
    }
    protected override void OnSetCoolingMessage(EntityUid uid, NuclearReactorComponent comp, NuclearReactorSetCoolingMessage args)
    {
        if (comp.Enabled)
            return; // Нельзя менять при работе

        comp.CoolingLevel = Math.Clamp(args.CoolingLevel, 1, 8);
        UpdateUI(uid, comp);
    }
    private void UpdateCoolingLevel(EntityUid uid, NuclearReactorComponent comp)
    {
        int rodCount = 0;
        if (comp.RodSlot1.HasItem) rodCount++;
        if (comp.RodSlot2.HasItem) rodCount++;
        if (comp.RodSlot3.HasItem) rodCount++;
        if (comp.RodSlot4.HasItem) rodCount++;

        comp.CoolingLevel = rodCount;
        comp.OptimalTemperature = rodCount * 1000f;
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

        // === НАГРЕВ ===
        float heating = totalHeatGen * comp.UpdateInterval * 0.5f;

        // === ОХЛАЖДЕНИЕ (8 уровней) ===
        float coolingPower = 0.1f + (comp.CoolingLevel * 0.075f); // 0.175 (ур.1) до 0.7 (ур.8)
        float cooling = (comp.CurrentTemperature - 293.15f) * coolingPower * comp.UpdateInterval;

        // Влияние целевой температуры
        float targetInfluence = (comp.TargetTemperature - comp.CurrentTemperature) * 0.1f * comp.UpdateInterval;

        comp.CurrentTemperature += heating - cooling + targetInfluence;
        comp.CurrentTemperature = MathF.Max(293.15f, comp.CurrentTemperature);

        // Расчёт эффективности
        float efficiency;
        if (comp.CurrentTemperature < optimalMin)
        {
            efficiency = comp.CurrentTemperature / optimalMin * 0.7f;
        }
        else if (comp.CurrentTemperature > optimalMax)
        {
            float overheatFactor = (comp.CurrentTemperature - optimalMax) / comp.OptimalTemperature;
            efficiency = 1.0f + overheatFactor * 0.3f;

            float damage = overheatFactor * 2f * comp.UpdateInterval;
            comp.Integrity -= damage;

            if (_random.Prob(0.05f))
                _popup.PopupEntity(Loc.GetString("nuclear-reactor-overheat-warning"), uid, PopupType.LargeCaution);
        }
        else
        {
            efficiency = 1.0f;
            if (comp.Integrity < 100f)
                comp.Integrity = MathF.Min(100f, comp.Integrity + 0.5f * comp.UpdateInterval);
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

        rodCount++;

        var consumptionRate = 0.03f * (comp.CurrentTemperature / 800f);
        rod.Fuel = MathF.Max(0, rod.Fuel - consumptionRate * comp.UpdateInterval);
        Dirty(rodEntity.Value, rod);

        if (rod.Fuel > 0)
        {
            totalHeatGen += rod.HeatMultiplier * (rod.Fuel / rod.MaxFuel) * 600f;
        }
    }

    private void Meltdown(EntityUid uid, NuclearReactorComponent comp)
    {
        _explosion.QueueExplosion(uid, "Default", 200, 5, 10, canCreateVacuum: true);
        EntityManager.QueueDeleteEntity(uid);
    }

    protected override void UpdateUI(EntityUid uid, NuclearReactorComponent comp)
    {
        var uiSystem = EntityManager.System<SharedUserInterfaceSystem>();
        if (!uiSystem.IsUiOpen(uid, NuclearReactorUiKey.Key))
            return;

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
            comp.CoolingLevel
        );

        uiSystem.SetUiState(uid, NuclearReactorUiKey.Key, state);
    }

    private ContainerInfo GetSlotInfo(EntityUid? item)
    {
        if (item == null)
            return new ContainerInfo(false, null, null);

        var name = MetaData(item.Value).EntityName;
        var fuel = TryComp<UraniumRodComponent>(item, out var rod) ? rod.Fuel / rod.MaxFuel : (float?)null;
        return new ContainerInfo(true, name, fuel);
    }

    protected override void OnToggleMessage(EntityUid uid, NuclearReactorComponent comp, NuclearReactorToggleMessage args)
    {
        comp.Enabled = !comp.Enabled;
        _itemSlots.SetLock(uid, comp.RodSlot1, comp.Enabled);
        _itemSlots.SetLock(uid, comp.RodSlot2, comp.Enabled);
        _itemSlots.SetLock(uid, comp.RodSlot3, comp.Enabled);
        _itemSlots.SetLock(uid, comp.RodSlot4, comp.Enabled);
        UpdateUI(uid, comp);
    }

    protected override void OnSetTemperatureMessage(EntityUid uid, NuclearReactorComponent comp, NuclearReactorSetTemperatureMessage args)
    {
        comp.TargetTemperature = Math.Clamp(args.Temperature, 300f, 5000f);
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
