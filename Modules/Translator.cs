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
        var localizedString = _stringLocalizerImplementation[key, arguments];
        
        if (localizedString == null || localizedString.ResourceNotFound)
        {
            return key;
        }

        var translation = localizedString.Value;
        
        // Handle translation colours
        return translation
            .Replace("[GREEN]", ChatColors.Green.ToString())
            .Replace("[RED]", ChatColors.Red.ToString())
            .Replace("[YELLOW]", ChatColors.Yellow.ToString())
            .Replace("[BLUE]", ChatColors.Blue.ToString())
            .Replace("[PURPLE]", ChatColors.Purple.ToString())
            .Replace("[ORANGE]", ChatColors.Orange.ToString())
            .Replace("[WHITE]", ChatColors.White.ToString())
            .Replace("[NORMAL]", ChatColors.White.ToString())
            .Replace("[GREY]", ChatColors.Grey.ToString())
            .Replace("[LIGHT_RED]", ChatColors.LightRed.ToString())
            .Replace("[LIGHT_BLUE]", ChatColors.LightBlue.ToString())
            .Replace("[LIGHT_PURPLE]", ChatColors.LightPurple.ToString())
            .Replace("[LIGHT_YELLOW]", ChatColors.LightYellow.ToString())
            .Replace("[DARK_RED]", ChatColors.Darkred.ToString())
            .Replace("[DARK_BLUE]", ChatColors.DarkBlue.ToString())
            .Replace("[BLUE_GREY]", ChatColors.BlueGrey.ToString())
            .Replace("[OLIVE]", ChatColors.Olive.ToString())
            .Replace("[LIME]", ChatColors.Lime.ToString())
            .Replace("[GOLD]", ChatColors.Gold.ToString())
            .Replace("[SILVER]", ChatColors.Silver.ToString())
            .Replace("[MAGENTA]", ChatColors.Magenta.ToString());
    }
}