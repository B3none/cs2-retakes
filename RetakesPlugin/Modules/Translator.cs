using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;

namespace RetakesPlugin.Modules;

public class Translator
{
    private IStringLocalizer _stringLocalizerImplementation;

    public Translator(IStringLocalizer localizer)
    {
        _stringLocalizerImplementation = localizer;
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        return _stringLocalizerImplementation.GetAllStrings(includeParentCultures);
    }

    public string this[string name] => Translate(name);

    public string this[string name, params object[] arguments] => Translate(name, arguments);

    private const string CenterModifier = "center.";
    private const string HtmlModifier = "html.";

    private string Translate(string key, params object[] arguments)
    {
        var isCenter = key.StartsWith(CenterModifier);
        var isHtml = key.StartsWith(HtmlModifier);

        if (isCenter)
        {
            key = key.Substring(CenterModifier.Length);
        }
        else if (isHtml)
        {
            key = key.Substring(HtmlModifier.Length);
        }

        var localizedString = _stringLocalizerImplementation[key, arguments];

        if (localizedString == null || localizedString.ResourceNotFound)
        {
            return key;
        }

        var translation = localizedString.Value;

        // Handle translation colours
        return translation
            .Replace("[GREEN]", isCenter ? "" : isHtml ? "<font color='green'>" : ChatColors.Green.ToString())
            .Replace("[RED]", isCenter ? "" : isHtml ? "<font color='red'>" : ChatColors.Red.ToString())
            .Replace("[YELLOW]", isCenter ? "" : isHtml ? "<font color='yellow'>" : ChatColors.Yellow.ToString())
            .Replace("[BLUE]", isCenter ? "" : isHtml ? "<font color='blue'>" : ChatColors.Blue.ToString())
            .Replace("[PURPLE]", isCenter ? "" : isHtml ? "<font color='purple'>" : ChatColors.Purple.ToString())
            .Replace("[ORANGE]", isCenter ? "" : isHtml ? "<font color='orange'>" : ChatColors.Orange.ToString())
            .Replace("[WHITE]", isCenter ? "" : isHtml ? "<font color='white'>" : ChatColors.White.ToString())
            .Replace("[NORMAL]", isCenter ? "" : isHtml ? "<font color='white'>" : ChatColors.White.ToString())
            .Replace("[GREY]", isCenter ? "" : isHtml ? "<font color='grey'>" : ChatColors.Grey.ToString())
            .Replace("[LIGHT_RED]", isCenter ? "" : isHtml ? "<font color='lightred'>" : ChatColors.LightRed.ToString())
            .Replace("[LIGHT_BLUE]", isCenter ? "" : isHtml ? "<font color='lightblue'>" : ChatColors.LightBlue.ToString())
            .Replace("[LIGHT_PURPLE]", isCenter ? "" : isHtml ? "<font color='mediumpurple'>" : ChatColors.LightPurple.ToString())
            .Replace("[LIGHT_YELLOW]", isCenter ? "" : isHtml ? "<font color='lightyellow'>" : ChatColors.LightYellow.ToString())
            .Replace("[DARK_RED]", isCenter ? "" : isHtml ? "<font color='darkred'>" : ChatColors.DarkRed.ToString())
            .Replace("[DARK_BLUE]", isCenter ? "" : isHtml ? "<font color='darkblue'>" : ChatColors.DarkBlue.ToString())
            .Replace("[BLUE_GREY]", isCenter ? "" : isHtml ? "<font color='grey'>" : ChatColors.BlueGrey.ToString())
            .Replace("[OLIVE]", isCenter ? "" : isHtml ? "<font color='olive'>" : ChatColors.Olive.ToString())
            .Replace("[LIME]", isCenter ? "" : isHtml ? "<font color='lime'>" : ChatColors.Lime.ToString())
            .Replace("[GOLD]", isCenter ? "" : isHtml ? "<font color='gold'>" : ChatColors.Gold.ToString())
            .Replace("[SILVER]", isCenter ? "" : isHtml ? "<font color='silver'>" : ChatColors.Silver.ToString())
            .Replace("[MAGENTA]", isCenter ? "" : isHtml ? "<font color='magenta'>" : ChatColors.Magenta.ToString());
    }
}
