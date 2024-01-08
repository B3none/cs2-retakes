using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;

namespace RetakesPlugin.Modules;

public class Translator : IStringLocalizer
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

    public LocalizedString this[string name] => _stringLocalizerImplementation[name];

    public LocalizedString this[string name, params object[] arguments] => _stringLocalizerImplementation[name, arguments];
    
    public string Translate(string key, params object[] arguments)
    {
        var localizedString = this[key, arguments];
        
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
            .Replace("[LIGHTRED]", ChatColors.LightRed.ToString())
            .Replace("[LIGHTBLUE]", ChatColors.LightBlue.ToString())
            .Replace("[LIGHTPURPLE]", ChatColors.LightPurple.ToString())
            .Replace("[LIGHTYELLOW]", ChatColors.LightYellow.ToString())
            .Replace("[DARKRED]", ChatColors.Darkred.ToString())
            .Replace("[DARKBLUE]", ChatColors.DarkBlue.ToString())
            .Replace("[BLUEGREY]", ChatColors.BlueGrey.ToString())
            .Replace("[OLIVE]", ChatColors.Olive.ToString())
            .Replace("[LIME]", ChatColors.Lime.ToString())
            .Replace("[GOLD]", ChatColors.Gold.ToString())
            .Replace("[SILVER]", ChatColors.Silver.ToString())
            .Replace("[MAGENTA]", ChatColors.Magenta.ToString());
    }
}