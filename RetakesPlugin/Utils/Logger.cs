using CounterStrikeSharp.API;

namespace RetakesPlugin.Utils;

public static class Logger
{
    private static readonly string LogPrefix = $"Retakes {RetakesPlugin.Version}";
    private static bool _isDebugEnabled;

    public static void Initialize(bool isDebugEnabled)
    {
        _isDebugEnabled = isDebugEnabled;
    }

    public static void LogServer(string message)
    {
        Server.PrintToChatAll($"[{LogPrefix}] {message}");
    }

    public static void LogInfo(string category, string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[{LogPrefix} - {category}] {message}");
        Console.ResetColor();
    }

    public static void LogWarning(string category, string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[{LogPrefix} - {category}] WARNING: {message}");
        Console.ResetColor();
    }

    public static void LogError(string category, string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[{LogPrefix} - {category}] ERROR: {message}");
        Console.ResetColor();
    }

    public static void LogDebug(string category, string message)
    {
        if (!_isDebugEnabled) return;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[{LogPrefix} - {category}] DEBUG: {message}");
        Console.ResetColor();
    }

    public static void LogException(string category, Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[{LogPrefix} - {category}] EXCEPTION: {ex.Message}");
        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        Console.ResetColor();
    }
}