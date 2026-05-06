using Content.Server.Database;
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
    [Dependency] private readonly IServerDbManager _dbManager = default!;

    private ISawmill _sawmill = default!;
    private readonly Dictionary<string, HashSet<string>> _playerAchievements = new();

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("achievements");

        SubscribeNetworkEvent<RequestAchievementsMessage>(OnRequestAchievements);
        SubscribeLocalEvent<InventoryComponent, DidEquipEvent>(OnDidEquip);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
    }

    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        var userId = ev.Player.UserId;
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
}
