using Content.Server.Database; // ДОБАВЛЕНО
using Content.Server.GameTicking;
using Content.Shared._BaroStation.Achievements;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Linq;
using System.Threading.Tasks;

namespace Content.Server._BaroStation.Achievements;

public sealed class AchievementsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConsoleHost _consoleHost = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IServerDbManager _dbManager = default!; // ДОБАВЛЕНО

    private ISawmill _sawmill = default!;
    private readonly Dictionary<string, HashSet<string>> _playerAchievements = new();

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("achievements");

        SubscribeNetworkEvent<RequestAchievementsMessage>(OnRequestAchievements);
        SubscribeNetworkEvent<ResetAchievementsMessage>(OnResetAchievements);
        SubscribeLocalEvent<InventoryComponent, DidEquipEvent>(OnDidEquip);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        _consoleHost.RegisterCommand("reset_achievements", "Resets all achievements for a player", "reset_achievements <username>", ResetAchievementsCommand);
        _consoleHost.RegisterCommand("list_achievements", "Lists all achievements", "list_achievements", ListAchievementsCommand);
        _consoleHost.RegisterCommand("give_achievement", "Gives an achievement to a player", "give_achievement <username> <achievementId>", GiveAchievementCommand);
        _consoleHost.RegisterCommand("my_achievements", "Shows your achievements", "my_achievements", MyAchievementsCommand);
    }

    private void ResetAchievementsCommand(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteLine("Usage: reset_achievements <username>");
            return;
        }

        var username = args[0];
        if (!TryGetPlayerByUsername(username, out var session, out var playerEntity))
        {
            shell.WriteLine($"Player {username} not found");
            return;
        }

        var userId = session.UserId;

        // Удаляем из БД асинхронно
        _ = _dbManager.RemoveAllPlayerAchievementsAsync(userId);

        if (_playerAchievements.ContainsKey(userId.ToString()))
        {
            _playerAchievements[userId.ToString()].Clear();
        }
        else
        {
            _playerAchievements[userId.ToString()] = new HashSet<string>();
        }

        if (TryComp<PlayerAchievementsComponent>(playerEntity, out var comp))
        {
            comp.EarnedAchievements.Clear();
            Dirty(playerEntity, comp);
        }

        shell.WriteLine($"Reset all achievements for {username}");
        _sawmill.Info($"Reset achievements for {username}");

        var stateMsg = new AchievementsStateMessage { EarnedIds = new List<string>() };
        RaiseNetworkEvent(stateMsg, session);
    }

    private void ListAchievementsCommand(IConsoleShell shell, string argStr, string[] args)
    {
        var achievements = _prototypeManager.EnumeratePrototypes<AchievementPrototype>().ToList();
        shell.WriteLine($"Found {achievements.Count} achievements:");
        foreach (var achievement in achievements)
        {
            shell.WriteLine($"  - {achievement.ID}: {achievement.Name}");
        }
    }

    private void GiveAchievementCommand(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteLine("Usage: give_achievement <username> <achievementId>");
            return;
        }

        var username = args[0];
        var achievementId = args[1];

        if (!TryGetPlayerByUsername(username, out var session, out var playerEntity))
        {
            shell.WriteLine($"Player {username} not found");
            return;
        }

        if (!TryComp<PlayerAchievementsComponent>(playerEntity, out var comp))
        {
            comp = AddComp<PlayerAchievementsComponent>(playerEntity);
        }

        GrantAchievement(playerEntity, comp, achievementId);
        shell.WriteLine($"Granted achievement {achievementId} to {username}");
    }

    private void MyAchievementsCommand(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteLine("This command can only be used by a player");
            return;
        }

        var playerEntity = player.AttachedEntity;
        if (playerEntity == null)
        {
            shell.WriteLine("You have no attached entity");
            return;
        }

        if (TryComp<PlayerAchievementsComponent>(playerEntity, out var comp))
        {
            if (comp.EarnedAchievements.Count == 0)
            {
                shell.WriteLine("You have no achievements yet!");
            }
            else
            {
                shell.WriteLine($"You have {comp.EarnedAchievements.Count} achievements:");
                foreach (var achievement in comp.EarnedAchievements)
                {
                    shell.WriteLine($"  - {achievement}");
                }
            }
        }
        else
        {
            shell.WriteLine("You have no achievements component");
        }
    }

    private bool TryGetPlayerByUsername(string username, out ICommonSession session, out EntityUid playerEntity)
    {
        session = null!;
        playerEntity = EntityUid.Invalid;

        foreach (var player in _playerManager.Sessions)
        {
            if (player.Name.Equals(username, StringComparison.InvariantCultureIgnoreCase))
            {
                session = player;
                if (player.AttachedEntity is { } entity)
                {
                    playerEntity = entity;
                }
                return true;
            }
        }
        return false;
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        var userId = ev.Player.UserId;
        // Загружаем из БД асинхронно
        _ = LoadAndApplyAchievementsAsync(ev.Player, ev.Mob, userId);
    }

    private async Task LoadAndApplyAchievementsAsync(ICommonSession session, EntityUid mob, NetUserId userId)
    {
        try
        {
            var earnedIds = await _dbManager.GetPlayerAchievementsAsync(userId);
            var earnedSet = new HashSet<string>(earnedIds);

            _playerAchievements[userId.ToString()] = earnedSet;

            var achievementComp = EnsureComp<PlayerAchievementsComponent>(mob);
            achievementComp.EarnedAchievements = earnedSet;
            Dirty(mob, achievementComp);

            _sawmill.Info($"Loaded {earnedSet.Count} achievements for player {session.Name} from DB");

            var stateMsg = new AchievementsStateMessage { EarnedIds = earnedIds };
            RaiseNetworkEvent(stateMsg, session);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to load achievements for {session.Name}: {ex}");
        }
    }

    private void OnDidEquip(EntityUid uid, InventoryComponent component, DidEquipEvent args)
    {
        if (!TryComp<PlayerAchievementsComponent>(uid, out var achievementComp))
            return;

        var prototypeId = MetaData(args.Equipment).EntityPrototype?.ID;

        if (string.IsNullOrEmpty(prototypeId))
            return;

        _sawmill.Info($"Player equipped item: {prototypeId}");

        if (prototypeId == "ClothingMaskClown")
        {
            _sawmill.Info($"Clown mask equipped! Granting achievement to {ToPrettyString(uid)}");
            GrantAchievement(uid, achievementComp, "ClownMaskAchievement");
        }
    }

    private void GrantAchievement(EntityUid player, PlayerAchievementsComponent achievementComp, string achievementId)
    {
        if (achievementComp.EarnedAchievements.Contains(achievementId))
            return;

        if (!_prototypeManager.HasIndex<AchievementPrototype>(achievementId))
        {
            _sawmill.Error($"Unknown achievement ID: {achievementId}");
            return;
        }

        achievementComp.EarnedAchievements.Add(achievementId);
        Dirty(player, achievementComp);

        if (TryComp<ActorComponent>(player, out var actor))
        {
            var userId = actor.PlayerSession.UserId;

            // Сохраняем в БД асинхронно
            _ = _dbManager.AddPlayerAchievementAsync(userId, achievementId);

            if (!_playerAchievements.ContainsKey(userId.ToString()))
                _playerAchievements[userId.ToString()] = new HashSet<string>();

            _playerAchievements[userId.ToString()].Add(achievementId);

            _sawmill.Info($"Granted achievement {achievementId} to player {actor.PlayerSession.Name}");

            var msg = new AchievementEarnedMessage { AchievementId = achievementId };
            RaiseNetworkEvent(msg, actor.PlayerSession);

            var stateMsg = new AchievementsStateMessage { EarnedIds = achievementComp.EarnedAchievements.ToList() };
            RaiseNetworkEvent(stateMsg, actor.PlayerSession);
        }
    }

    private void OnRequestAchievements(RequestAchievementsMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession;
        _sawmill.Info($"Received RequestAchievementsMessage from {player.Name}");

        if (player.AttachedEntity == null)
        {
            _sawmill.Info($"Player {player.Name} has no attached entity, waiting...");
            Timer.Spawn(TimeSpan.FromMilliseconds(100), () => OnRequestAchievements(msg, args));
            return;
        }

        // Загружаем из БД асинхронно
        _ = LoadAndSendAchievementsAsync(player, player.AttachedEntity.Value);
    }

    private async Task LoadAndSendAchievementsAsync(ICommonSession session, EntityUid playerEntity)
    {
        try
        {
            var earnedIds = await _dbManager.GetPlayerAchievementsAsync(session.UserId);
            var earnedSet = new HashSet<string>(earnedIds);

            _playerAchievements[session.UserId.ToString()] = earnedSet;

            if (!TryComp<PlayerAchievementsComponent>(playerEntity, out var achievementComp))
            {
                achievementComp = AddComp<PlayerAchievementsComponent>(playerEntity);
            }

            achievementComp.EarnedAchievements = earnedSet;
            Dirty(playerEntity, achievementComp);

            var stateMsg = new AchievementsStateMessage { EarnedIds = earnedIds };
            RaiseNetworkEvent(stateMsg, session);

            _sawmill.Info($"Sent {earnedIds.Count} achievements to {session.Name} from DB");
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Failed to send achievements to {session.Name}: {ex}");
        }
    }

    private void OnResetAchievements(ResetAchievementsMessage msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession;
        _sawmill.Info($"Received ResetAchievementsMessage from {player.Name}");

        if (player.AttachedEntity is not { } playerEntity)
        {
            _sawmill.Info($"Player {player.Name} has no attached entity, waiting...");
            Timer.Spawn(TimeSpan.FromMilliseconds(100), () => OnResetAchievements(msg, args));
            return;
        }

        var userId = player.UserId;

        // Удаляем из БД асинхронно
        _ = _dbManager.RemoveAllPlayerAchievementsAsync(userId);

        if (!_playerAchievements.ContainsKey(userId.ToString()))
        {
            _playerAchievements[userId.ToString()] = new HashSet<string>();
        }
        _playerAchievements[userId.ToString()].Clear();

        if (TryComp<PlayerAchievementsComponent>(playerEntity, out var achievementComp))
        {
            achievementComp.EarnedAchievements.Clear();
            Dirty(playerEntity, achievementComp);
        }

        _sawmill.Info($"Player {player.Name} reset all achievements");

        var stateMsg = new AchievementsStateMessage { EarnedIds = new List<string>() };
        RaiseNetworkEvent(stateMsg, player);
    }
}
