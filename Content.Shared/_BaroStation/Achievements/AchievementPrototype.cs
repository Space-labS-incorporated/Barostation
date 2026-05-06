using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._BaroStation.Achievements;

[Prototype("achievement")]
public sealed partial class AchievementPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = string.Empty;
    [DataField(required: true)]
    public LocId Name { get; private set; }

    [DataField(required: true)]
    public LocId Description { get; private set; }

    [DataField]
    public ResPath Icon { get; private set; } = new("/Textures/_BaroStation/Achievements/default.png");
}
