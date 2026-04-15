using Content.Shared._BaroStation.NuclearReactor;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client._BaroStation.NuclearReactor;

[UsedImplicitly]
public sealed class NuclearReactorBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private NuclearReactorWindow? _window;

    public NuclearReactorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }
    public event Action<int>? SetCoolingLevel;

    protected override void Open()
    {
        base.Open();
        _window = new NuclearReactorWindow();
        _window.OnClose += Close;
        _window.ToggleReactor += enabled => SendMessage(new NuclearReactorToggleMessage());
        _window.SetTemperature += temp => SendMessage(new NuclearReactorSetTemperatureMessage(temp));
        _window.EjectRod += slot => SendMessage(new NuclearReactorEjectMessage(slot));
        _window.SetCoolingLevel += level => SendMessage(new NuclearReactorSetCoolingMessage(level)); // ДОБАВИТЬ
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is NuclearReactorUiState cast)
            _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }
}
