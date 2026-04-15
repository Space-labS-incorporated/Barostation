using Content.Shared._BaroStation.NuclearReactor;

namespace Content.Shared._BaroStation.NuclearReactor;

public abstract class SharedNuclearReactorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NuclearReactorComponent, NuclearReactorToggleMessage>(OnToggleMessageWrap);
        SubscribeLocalEvent<NuclearReactorComponent, NuclearReactorSetTemperatureMessage>(OnSetTemperatureMessageWrap);
        SubscribeLocalEvent<NuclearReactorComponent, NuclearReactorEjectMessage>(OnEjectMessageWrap);
        SubscribeLocalEvent<NuclearReactorComponent, NuclearReactorSetCoolingMessage>(OnSetCoolingMessageWrap); // ДОБАВИТЬ
        SubscribeLocalEvent<NuclearReactorComponent, BoundUIOpenedEvent>(OnUIOpen);
    }
    private void OnSetCoolingMessageWrap(EntityUid uid, NuclearReactorComponent comp, NuclearReactorSetCoolingMessage args)
    => OnSetCoolingMessage(uid, comp, args);

    protected virtual void OnSetCoolingMessage(EntityUid uid, NuclearReactorComponent comp, NuclearReactorSetCoolingMessage args) { }
    private void OnToggleMessageWrap(EntityUid uid, NuclearReactorComponent comp, NuclearReactorToggleMessage args)
        => OnToggleMessage(uid, comp, args);

    private void OnSetTemperatureMessageWrap(EntityUid uid, NuclearReactorComponent comp, NuclearReactorSetTemperatureMessage args)
        => OnSetTemperatureMessage(uid, comp, args);

    private void OnEjectMessageWrap(EntityUid uid, NuclearReactorComponent comp, NuclearReactorEjectMessage args)
        => OnEjectMessage(uid, comp, args);

    private void OnUIOpen(EntityUid uid, NuclearReactorComponent comp, BoundUIOpenedEvent args)
        => UpdateUI(uid, comp);

    protected virtual void OnToggleMessage(EntityUid uid, NuclearReactorComponent comp, NuclearReactorToggleMessage args) { }
    protected virtual void OnSetTemperatureMessage(EntityUid uid, NuclearReactorComponent comp, NuclearReactorSetTemperatureMessage args) { }
    protected virtual void OnEjectMessage(EntityUid uid, NuclearReactorComponent comp, NuclearReactorEjectMessage args) { }
    protected virtual void UpdateUI(EntityUid uid, NuclearReactorComponent comp) { }
}
