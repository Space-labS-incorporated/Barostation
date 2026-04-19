using Content.Shared.Interaction;
using Robust.Shared.Network;

namespace Content.Shared._BaroStation.NuclearReactor;

public abstract class SharedNuclearReactorConsoleSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NuclearReactorConsoleComponent, NuclearReactorConsoleLinkMessage>(OnLinkMessage);
        SubscribeLocalEvent<NuclearReactorConsoleComponent, NuclearReactorConsoleClearLinkMessage>(OnClearLinkMessage);
        // SubscribeLocalEvent<NuclearReactorConsoleComponent, NuclearReactorToggleMessage>(OnToggleMessage);
        //  SubscribeLocalEvent<NuclearReactorConsoleComponent, NuclearReactorSetTemperatureMessage>(OnSetTempMessage);
        //  SubscribeLocalEvent<NuclearReactorConsoleComponent, NuclearReactorEjectMessage>(OnEjectMessage);
        //  SubscribeLocalEvent<NuclearReactorConsoleComponent, NuclearReactorSetCoolingMessage>(OnSetCoolingMessage);

        //  SubscribeLocalEvent<NuclearReactorConsoleComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
    }

    private void OnAfterInteractUsing(EntityUid uid, NuclearReactorConsoleComponent comp, AfterInteractUsingEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        if (!TryComp<NuclearReactorComponent>(args.Target, out _))
        {
            if (_net.IsServer)
                PopupLinkFail(uid, args.User);
            return;
        }

        args.Handled = true;
        comp.LinkedReactor = args.Target;
        Dirty(uid, comp);

        if (_net.IsServer)
            PopupLinkSuccess(uid, args.User);

        UpdateConsoleUi(uid, comp);
    }

    protected virtual void PopupLinkFail(EntityUid uid, EntityUid user) { }
    protected virtual void PopupLinkSuccess(EntityUid uid, EntityUid user) { }

    private void OnLinkMessage(EntityUid uid, NuclearReactorConsoleComponent comp, NuclearReactorConsoleLinkMessage args)
    {
        var target = GetEntity(args.Target);
        if (TryComp<NuclearReactorComponent>(target, out _))
        {
            comp.LinkedReactor = target;
            Dirty(uid, comp);
            UpdateConsoleUi(uid, comp);
        }
    }

    private void OnClearLinkMessage(EntityUid uid, NuclearReactorConsoleComponent comp, NuclearReactorConsoleClearLinkMessage args)
    {
        comp.LinkedReactor = null;
        Dirty(uid, comp);
        UpdateConsoleUi(uid, comp);
    }

    private void OnToggleMessage(EntityUid uid, NuclearReactorConsoleComponent comp, NuclearReactorToggleMessage args)
    {
        if (comp.LinkedReactor is { Valid: true } reactor)
            SendReactorMessage(reactor, args);
    }

    private void OnSetTempMessage(EntityUid uid, NuclearReactorConsoleComponent comp, NuclearReactorSetTemperatureMessage args)
    {
        if (comp.LinkedReactor is { Valid: true } reactor)
            SendReactorMessage(reactor, args);
    }

    private void OnEjectMessage(EntityUid uid, NuclearReactorConsoleComponent comp, NuclearReactorEjectMessage args)
    {
        if (comp.LinkedReactor is { Valid: true } reactor)
            SendReactorMessage(reactor, args);
    }

    private void OnSetCoolingMessage(EntityUid uid, NuclearReactorConsoleComponent comp, NuclearReactorSetCoolingMessage args)
    {
        if (comp.LinkedReactor is { Valid: true } reactor)
            SendReactorMessage(reactor, args);
    }

    private void SendReactorMessage(EntityUid reactor, BoundUserInterfaceMessage message)
    {
        RaiseLocalEvent(reactor, message);
    }

    protected virtual void UpdateConsoleUi(EntityUid uid, NuclearReactorConsoleComponent comp) { }

    public void UpdateFromReactor(EntityUid consoleUid, NuclearReactorConsoleComponent comp, NuclearReactorUiState state)
    {
        comp.LastReactorState = state;
        Dirty(consoleUid, comp);
        UpdateConsoleUi(consoleUid, comp);
    }
}
