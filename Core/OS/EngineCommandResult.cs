using System;
using System.Collections.Generic;

namespace VS.Helper.Core.OS;

public sealed class EngineCommandResult
{
    public EngineCommandResult(string name, bool success, string message)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "unknown" : name;
        Success = success;
        Message = message ?? string.Empty;
        FinishedUtc = DateTime.UtcNow;
    }

    public string Name { get; }
    public bool Success { get; }
    public string Message { get; }
    public DateTime FinishedUtc { get; }
    public Dictionary<string, string> Data { get; } = new(StringComparer.OrdinalIgnoreCase);

    public static EngineCommandResult Ok(string name, string message)
    {
        return new EngineCommandResult(name, true, message);
    }

    public static EngineCommandResult Fail(string name, string message)
    {
        return new EngineCommandResult(name, false, message);
    }
}
