using Robust.Shared.GameStates;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared._BaroStation.Achievements;

/// <summary>
/// Компонент для хранения достижений игрока на его игровой сущности
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PlayerAchievementsComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<string> EarnedAchievements = new();
}
