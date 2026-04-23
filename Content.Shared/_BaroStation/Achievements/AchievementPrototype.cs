using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._BaroStation.Achievements;

/// <summary>
/// Прототип достижения. Определяется в YAML файлах.
/// </summary>
[Prototype("achievement")]
public sealed partial class AchievementPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;

    /// <summary>
    /// Локализуемое название достижения.
    /// </summary>
    [DataField(required: true)]
    public LocId Name { get; private set; }

    /// <summary>
    /// Локализуемое описание достижения.
    /// </summary>
    [DataField(required: true)]
    public LocId Description { get; private set; }

    /// <summary>
    /// Путь к иконке достижения.
    /// </summary>
    [DataField]
    public ResPath Icon { get; private set; } = new("/Textures/_BaroStation/Achievements/default.png");
}
