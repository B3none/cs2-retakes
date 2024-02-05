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

    private string Translate(string key, params object[] arguments)
    {
        var isHtml = key.StartsWith("html.");
        key = key.Replace("html.", "");
        
        var localizedString = _stringLocalizerImplementation[key, arguments];

        if (localizedString == null || localizedString.ResourceNotFound)
        {
            return key;
        }

        var translation = localizedString.Value;
        
        // Handle translation colours
        return translation
            .Replace("[GREEN]", isHtml ? "<font color='green'>" : ChatColors.Green.ToString())
            .Replace("[RED]", isHtml ? "<font color='red'>" : ChatColors.Red.ToString())
            .Replace("[YELLOW]", isHtml ? "<font color='yellow'>" : ChatColors.Yellow.ToString())
            .Replace("[BLUE]", isHtml ? "<font color='blue'>" : ChatColors.Blue.ToString())
            .Replace("[PURPLE]", isHtml ? "<font color='purple'>" : ChatColors.Purple.ToString())
            .Replace("[ORANGE]", isHtml ? "<font color='orange'>" : ChatColors.Orange.ToString())
            .Replace("[WHITE]", isHtml ? "<font color='white'>" : ChatColors.White.ToString())
            .Replace("[NORMAL]", isHtml ? "<font color='white'>" : ChatColors.White.ToString())
            .Replace("[GREY]", isHtml ? "<font color='grey'>" : ChatColors.Grey.ToString())
            .Replace("[LIGHT_RED]", isHtml ? "<font color='lightred'>" : ChatColors.LightRed.ToString())
            .Replace("[LIGHT_BLUE]", isHtml ? "<font color='lightblue'>" : ChatColors.LightBlue.ToString())
            .Replace("[LIGHT_PURPLE]", isHtml ? "<font color='mediumpurple'>" : ChatColors.LightPurple.ToString())
            .Replace("[LIGHT_YELLOW]", isHtml ? "<font color='lightyellow'>" : ChatColors.LightYellow.ToString())
            .Replace("[DARK_RED]", isHtml ? "<font color='darkred'>" : ChatColors.DarkRed.ToString())
            .Replace("[DARK_BLUE]", isHtml ? "<font color='darkblue'>" : ChatColors.DarkBlue.ToString())
            .Replace("[BLUE_GREY]", isHtml ? "<font color='grey'>" : ChatColors.BlueGrey.ToString())
            .Replace("[OLIVE]", isHtml ? "<font color='olive'>" : ChatColors.Olive.ToString())
            .Replace("[LIME]", isHtml ? "<font color='lime'>" : ChatColors.Lime.ToString())
            .Replace("[GOLD]", isHtml ? "<font color='gold'>" : ChatColors.Gold.ToString())
            .Replace("[SILVER]", isHtml ? "<font color='silver'>" : ChatColors.Silver.ToString())
            .Replace("[MAGENTA]", isHtml ? "<font color='magenta'>" : ChatColors.Magenta.ToString());
    }
}
