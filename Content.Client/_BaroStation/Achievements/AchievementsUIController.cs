using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using Content.Client.UserInterface.Controls;
using Content.Shared._BaroStation.Achievements;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Client._BaroStation.Achievements;

public sealed class AchievementsUIController : UIController,
    IOnStateEntered<LobbyState>,
    IOnStateExited<LobbyState>
{
    [Dependency] private readonly IClientNetManager _netManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private AchievementsWindow? _window;
    private readonly List<AchievementPrototype> _allAchievements = new();
    private HashSet<string> _earnedAchievements = new();
    private ISawmill _sawmill = default!;
    private bool _hasCachedData;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("achievements.ui");

        foreach (var proto in _prototypeManager.EnumeratePrototypes<AchievementPrototype>())
        {
            _allAchievements.Add(proto);
            _sawmill.Info($"Loaded achievement prototype: {proto.ID}");
        }

        _allAchievements.Sort((a, b) => string.Compare(a.ID, b.ID, StringComparison.Ordinal));

        SubscribeNetworkEvent<AchievementEarnedMessage>(OnAchievementEarned);
        SubscribeNetworkEvent<AchievementsStateMessage>(OnAchievementsState);

        _netManager.ClientConnectStateChanged += OnClientConnectStateChanged;
    }

    private void OnClientConnectStateChanged(ClientConnectionState obj)
    {
        _sawmill.Info($"Client connection state changed: {obj}");
        if (obj == ClientConnectionState.Connected)
        {
            RequestAchievements();
        }
    }

    public void OnStateEntered(LobbyState state)
    {
        EnsureWindow();
        RequestAchievements();
    }

    public void OnStateExited(LobbyState state)
    {
        _window?.Close();
    }

    private void EnsureWindow()
    {
        if (_window != null && !_window.Disposed)
            return;

        _window = UIManager.CreateWindow<AchievementsWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.Center);

        _window.ResetButton.OnPressed += OnResetPressed;
        _window.OnOpen += OnWindowOpened;

        // Если есть кэшированные данные, показываем их при создании окна
        if (_hasCachedData)
        {
            _window.CacheAchievements(_allAchievements, _earnedAchievements);
        }
    }

    private void OnWindowOpened()
    {
        _sawmill.Info("Window opened, requesting achievements");
        RequestAchievements();
    }

    public void ToggleWindow()
    {
        if (_window == null || _window.Disposed)
            return;

        if (_window.IsOpen)
        {
            _window.Close();
        }
        else
        {
            _window.Open();
        }
    }

    private void RequestAchievements()
    {
        if (_netManager.ClientConnectState != ClientConnectionState.Connected)
        {
            _sawmill.Warning("Cannot request achievements: not connected to server");
            return;
        }

        _sawmill.Info("Requesting achievements from server...");
        _entityManager.EventBus.RaiseEvent(EventSource.Network, new RequestAchievementsMessage());
    }

    private void OnAchievementsState(AchievementsStateMessage msg, EntitySessionEventArgs args)
    {
        _sawmill.Info($"Received AchievementsStateMessage with {msg.EarnedIds.Count} earned IDs");

        _earnedAchievements = new HashSet<string>(msg.EarnedIds);
        _hasCachedData = true;

        foreach (var id in msg.EarnedIds)
        {
            _sawmill.Info($"  - Earned: {id}");
        }

        // Всегда обновляем окно, если оно существует (даже если закрыто - кэшируем)
        if (_window != null && !_window.Disposed)
        {
            _window.CacheAchievements(_allAchievements, _earnedAchievements);

            if (_window.IsOpen)
            {
                _window.UpdateAchievements(_allAchievements, _earnedAchievements);
            }
        }
    }

    private void OnAchievementEarned(AchievementEarnedMessage msg, EntitySessionEventArgs args)
    {
        _sawmill.Info($"Achievement earned: {msg.AchievementId}");

        if (!_allAchievements.Any(a => a.ID == msg.AchievementId))
        {
            _sawmill.Warning($"Unknown achievement ID: {msg.AchievementId}");
            return;
        }

        _earnedAchievements.Add(msg.AchievementId);
        _hasCachedData = true;

        if (_window != null && !_window.Disposed)
        {
            _window.CacheAchievements(_allAchievements, _earnedAchievements);

            if (_window.IsOpen)
            {
                _window.UpdateAchievements(_allAchievements, _earnedAchievements);
            }
        }

        var proto = _allAchievements.FirstOrDefault(a => a.ID == msg.AchievementId);
        if (proto != null)
        {
            ShowAchievementToast(proto);
        }
    }

    private void ShowAchievementToast(AchievementPrototype proto)
    {
        var toast = new ToastNotification(proto);
        toast.OnClosed += () => toast.Dispose();
        toast.Show();
    }

    private void OnResetPressed(BaseButton.ButtonEventArgs args)
    {
        if (_netManager.ClientConnectState != ClientConnectionState.Connected)
        {
            _sawmill.Warning("Cannot reset achievements: not connected to server");
            return;
        }

        _sawmill.Info("Reset achievements request sent");
        _entityManager.EventBus.RaiseEvent(EventSource.Network, new ResetAchievementsMessage());

        // Очищаем локально для мгновенного UI отклика
        _earnedAchievements.Clear();
        _hasCachedData = true;

        if (_window != null && !_window.Disposed)
        {
            _window.CacheAchievements(_allAchievements, _earnedAchievements);

            if (_window.IsOpen)
            {
                _window.UpdateAchievements(_allAchievements, _earnedAchievements);
            }
        }
    }
}
