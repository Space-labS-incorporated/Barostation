using Content.Server.DeviceLinking.Systems;
using Content.Server.Power.Components;
using Content.Server.UserInterface;
using Content.Shared._BaroStation.NuclearReactor;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._BaroStation.NuclearReactor;

public sealed class NuclearReactorConsoleSystem : SharedNuclearReactorConsoleSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NuclearReactorConsoleComponent, BoundUIOpenedEvent>(OnUIOpen);
        SubscribeLocalEvent<NuclearReactorConsoleComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<NuclearReactorConsoleComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<NuclearReactorConsoleComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<NuclearReactorConsoleComponent, NewLinkEvent>(OnNewLink);
        SubscribeLocalEvent<NuclearReactorConsoleComponent, EntityTerminatingEvent>(OnReactorDeleted);
        SubscribeLocalEvent<NuclearReactorConsoleComponent, NuclearReactorToggleMessage>(OnToggleMessageFromConsole);
        SubscribeLocalEvent<NuclearReactorConsoleComponent, NuclearReactorSetTemperatureMessage>(OnSetTempMessageFromConsole);
        SubscribeLocalEvent<NuclearReactorConsoleComponent, NuclearReactorEjectMessage>(OnEjectMessageFromConsole);
        SubscribeLocalEvent<NuclearReactorConsoleComponent, NuclearReactorSetCoolingMessage>(OnSetCoolingMessageFromConsole);
    }

    private void OnReactorDeleted(EntityUid uid, NuclearReactorConsoleComponent comp, ref EntityTerminatingEvent args)
    {
        if (comp.LinkedReactor == args.Entity.Owner)
        {
            comp.LinkedReactor = null;
            Dirty(uid, comp);
            UpdateConsoleUi(uid, comp);
        }
    }

    private void OnToggleMessageFromConsole(EntityUid uid, NuclearReactorConsoleComponent comp, NuclearReactorToggleMessage args)
    {
        if (comp.LinkedReactor is { Valid: true } reactor)
        {
            var toggleMsg = new NuclearReactorToggleMessage();
            RaiseLocalEvent(reactor, toggleMsg);
            UpdateConsoleUi(uid, comp);
        }
    }

    private void OnSetTempMessageFromConsole(EntityUid uid, NuclearReactorConsoleComponent comp, NuclearReactorSetTemperatureMessage args)
    {
        if (comp.LinkedReactor is { Valid: true } reactor)
        {
            var tempMsg = new NuclearReactorSetTemperatureMessage(args.Temperature);
            RaiseLocalEvent(reactor, tempMsg);
            UpdateConsoleUi(uid, comp);
        }
    }

    private void OnEjectMessageFromConsole(EntityUid uid, NuclearReactorConsoleComponent comp, NuclearReactorEjectMessage args)
    {
        if (comp.LinkedReactor is { Valid: true } reactor)
        {
            var ejectMsg = new NuclearReactorEjectMessage(args.Slot);
            RaiseLocalEvent(reactor, ejectMsg);
            UpdateConsoleUi(uid, comp);
        }
    }

    private void OnSetCoolingMessageFromConsole(EntityUid uid, NuclearReactorConsoleComponent comp, NuclearReactorSetCoolingMessage args)
    {
        if (comp.LinkedReactor is { Valid: true } reactor)
        {
            var coolingMsg = new NuclearReactorSetCoolingMessage(args.CoolingLevel);
            RaiseLocalEvent(reactor, coolingMsg);
            UpdateConsoleUi(uid, comp);
        }
    }

    private void OnMapInit(EntityUid uid, NuclearReactorConsoleComponent comp, MapInitEvent args)
    {
        _deviceLink.EnsureSinkPorts(uid, comp.LinkPort);
    }

    private void OnNewLink(EntityUid uid, NuclearReactorConsoleComponent comp, NewLinkEvent args)
    {

        if (args.Sink != uid)
            return;

        var reactor = args.Source;
        if (!TryComp<NuclearReactorComponent>(reactor, out _))
            return;

        comp.LinkedReactor = reactor;
        Dirty(uid, comp);

        if (args.User != null)
            _popup.PopupEntity(Loc.GetString("nuclear-reactor-console-link-success"), uid, args.User.Value, PopupType.Medium);

        UpdateConsoleUi(uid, comp);
    }

    private void OnSignalReceived(EntityUid uid, NuclearReactorConsoleComponent comp, ref SignalReceivedEvent args)
    {
        if (args.Port != comp.LinkPort)
            return;

        if (args.Trigger == null)
            return;

        if (!TryComp<NuclearReactorComponent>(args.Trigger, out _))
            return;

        comp.LinkedReactor = args.Trigger;
        Dirty(uid, comp);

        _popup.PopupEntity(Loc.GetString("nuclear-reactor-console-link-success"), uid, args.Trigger.Value, PopupType.Medium);

        UpdateConsoleUi(uid, comp);
    }

    private void OnStartup(EntityUid uid, NuclearReactorConsoleComponent comp, ComponentStartup args)
    {
        UpdateConsoleUi(uid, comp);
    }

    private void OnUIOpen(EntityUid uid, NuclearReactorConsoleComponent comp, BoundUIOpenedEvent args)
    {
        UpdateConsoleUi(uid, comp);
    }

    protected override void UpdateConsoleUi(EntityUid uid, NuclearReactorConsoleComponent comp)
    {
        if (!_ui.IsUiOpen(uid, NuclearReactorConsoleUiKey.Key))
            return;

        if (comp.LinkedReactor != null && !EntityManager.EntityExists(comp.LinkedReactor.Value))
        {
            comp.LinkedReactor = null;
            Dirty(uid, comp);
        }

        if (comp.LinkedReactor == null || !TryComp<NuclearReactorComponent>(comp.LinkedReactor, out var reactor))
        {
            var emptyState = new NuclearReactorConsoleUiState(
                hasReactor: false,
                reactorEnabled: false,
                curTemp: 293.15f,
                tarTemp: 500f,
                power: 0f,
                integrity: 100f,
                slots: new ContainerInfo[4],
                optTemp: 1000f,
                critTemp: 2500f,
                coolingLevel: 1,
                hasDepletedRod: false,
                hasPower: HasPower(uid)
            );
            _ui.SetUiState(uid, NuclearReactorConsoleUiKey.Key, emptyState);
            return;
        }

        var power = TryComp<PowerSupplierComponent>(comp.LinkedReactor, out var supplier) ? supplier.CurrentSupply : 0f;
        var slots = new ContainerInfo[4];
        slots[0] = GetSlotInfo(reactor.RodSlot1.Item);
        slots[1] = GetSlotInfo(reactor.RodSlot2.Item);
        slots[2] = GetSlotInfo(reactor.RodSlot3.Item);
        slots[3] = GetSlotInfo(reactor.RodSlot4.Item);

        var state = new NuclearReactorConsoleUiState(
            hasReactor: true,
            reactorEnabled: reactor.Enabled,
            curTemp: reactor.CurrentTemperature,
            tarTemp: reactor.TargetTemperature,
            power: power,
            integrity: reactor.Integrity,
            slots: slots,
            optTemp: reactor.OptimalTemperature,
            critTemp: reactor.OptimalTemperature * 2.5f,
            coolingLevel: reactor.CoolingLevel,
            hasDepletedRod: GetAnyDepleted(slots),
            hasPower: HasPower(uid)
        );

        _ui.SetUiState(uid, NuclearReactorConsoleUiKey.Key, state);
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

    private bool GetAnyDepleted(ContainerInfo[] slots)
    {
        foreach (var slot in slots)
            if (slot.Depleted) return true;
        return false;
    }

    private bool HasPower(EntityUid uid)
    {
        if (TryComp<ApcPowerReceiverComponent>(uid, out var power))
            return power.Powered;
        return true;
    }
}
