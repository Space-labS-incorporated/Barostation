using Content.Shared.Examine;

namespace Content.Shared._BaroStation.NuclearReactor;

public sealed class UraniumRodSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UraniumRodComponent, ExaminedEvent>(OnFuelExamined);
    }

    private void OnFuelExamined(EntityUid uid, UraniumRodComponent comp, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (comp.Depleted)
        {
            args.PushMarkup(Loc.GetString("uranium-rod-component-on-examine-depleted"));
            return;
        }

        var low = comp.Fuel * 4 < comp.MaxFuel;
        args.PushMarkup(Loc.GetString("uranium-rod-component-on-examine-detailed-message",
            ("colorName", low ? "darkorange" : "limegreen"),
            ("amount", (int)comp.Fuel),
            ("capacity", (int)comp.MaxFuel)));
    }
}
