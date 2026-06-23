using Serilog.Core;
using Serilog.Events;

namespace Web.Setup;

public class SourceContextFilter(string match, bool include) : ILogEventFilter
{
    public bool IsEnabled(LogEvent logEvent)
    {
        if (!logEvent.Properties.TryGetValue("SourceContext", out var value))
            return !include;

        var context = value.ToString().Trim('"');
        var contains = context.Contains(match, StringComparison.OrdinalIgnoreCase);

        return include ? contains : !contains;
    }

    public static SourceContextFilter Include(string match) => new(match, true);
    public static SourceContextFilter Exclude(string match) => new(match, false);
}
