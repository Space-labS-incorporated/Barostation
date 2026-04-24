using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.StylesheetHelpers;

namespace Content.Client._BaroStation.Achievements;

public static class AchievementsStylesheet
{
    public static Stylesheet Create(IResourceCache res)
    {
        var notoSansFont = res.GetResource<FontResource>("/EngineFonts/NotoSans/NotoSans-Regular.ttf");
        var notoSansFont12 = new VectorFont(notoSansFont, 12);

        return new Stylesheet(new StyleRule[]
        {
            Element().Class("BaroToastNotification")
                .Prop(PanelContainer.StylePropertyPanel, new StyleBoxFlat
                {
                    BackgroundColor = Color.FromHex("#25252ADD"),
                    BorderThickness = new Thickness(1),
                    BorderColor = Color.FromHex("#447044"),
                    ContentMarginLeftOverride = 4,
                    ContentMarginRightOverride = 4,
                    ContentMarginTopOverride = 4,
                    ContentMarginBottomOverride = 4,
                }),

            Element<Label>().Class("BaroToastTitle")
                .Prop("font", notoSansFont12)
                .Prop("font-color", Color.Gold),

            Element<Label>().Class("BaroToastAchievementName")
                .Prop("font", notoSansFont12)
                .Prop("font-color", Color.White),
        });
    }
}
