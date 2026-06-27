using System;
using System.Diagnostics;

namespace VS.Helper.Core.OS.V5;

public class EventBus
{
    public event Action<string, string>? OnEvent;

    public void Emit(string type, string data)
    {
        OnEvent?.Invoke(type, data);
        Debug.WriteLine($"[VS.Helper V5] {type}: {data}");
    }
}