using Content.Shared._BaroStation.NuclearReactor;
using JetBrains.Annotations;
using Robust.Client.GameObjects;

namespace Content.Client._BaroStation.NuclearReactor;

[UsedImplicitly]
public sealed class NuclearReactorConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private NuclearReactorConsoleWindow? _window;

    public NuclearReactorConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _window = new NuclearReactorConsoleWindow();
        _window.OnClose += Close;
        _window.ToggleReactor += enabled => SendMessage(new NuclearReactorToggleMessage());
        _window.SetTemperature += temp => SendMessage(new NuclearReactorSetTemperatureMessage(temp));
        _window.EjectRod += slot => SendMessage(new NuclearReactorEjectMessage(slot));
        _window.SetCoolingLevel += level => SendMessage(new NuclearReactorSetCoolingMessage(level));
        _window.ClearLink += () => SendMessage(new NuclearReactorConsoleClearLinkMessage());
        _window.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is NuclearReactorConsoleUiState cast)
            _window?.UpdateState(cast);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }
}
