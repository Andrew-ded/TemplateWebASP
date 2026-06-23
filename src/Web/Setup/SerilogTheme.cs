using Serilog.Sinks.SystemConsole.Themes;

namespace Web.Setup;

public static class SerilogTheme
{
    public static AnsiConsoleTheme Colored { get; } = new(new Dictionary<ConsoleThemeStyle, string>
    {
        [ConsoleThemeStyle.Text] = "\x1b[0m",
        [ConsoleThemeStyle.SecondaryText] = "\x1b[90m",
        [ConsoleThemeStyle.TertiaryText] = "\x1b[90m",
        [ConsoleThemeStyle.Invalid] = "\x1b[31m",
        [ConsoleThemeStyle.Null] = "\x1b[95m",
        [ConsoleThemeStyle.Name] = "\x1b[93m",
        [ConsoleThemeStyle.String] = "\x1b[96m",
        [ConsoleThemeStyle.Number] = "\x1b[95m",
        [ConsoleThemeStyle.Boolean] = "\x1b[95m",
        [ConsoleThemeStyle.Scalar] = "\x1b[95m",
        [ConsoleThemeStyle.LevelVerbose] = "\x1b[37m",
        [ConsoleThemeStyle.LevelDebug] = "\x1b[36;1m",
        [ConsoleThemeStyle.LevelInformation] = "\x1b[32;1m",
        [ConsoleThemeStyle.LevelWarning] = "\x1b[33;1m",
        [ConsoleThemeStyle.LevelError] = "\x1b[31;1m",
        [ConsoleThemeStyle.LevelFatal] = "\x1b[97;1m\x1b[41m",
    });
}
