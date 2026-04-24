using Robust.Shared.Serialization;

namespace Content.Shared._BaroStation.Achievements;

[Serializable, NetSerializable]
public sealed class AchievementEarnedMessage : EntityEventArgs
{
    public string AchievementId { get; set; } = string.Empty;
}

[Serializable, NetSerializable]
public sealed class RequestAchievementsMessage : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class AchievementsStateMessage : EntityEventArgs
{
    public List<string> EarnedIds { get; set; } = new();
}
