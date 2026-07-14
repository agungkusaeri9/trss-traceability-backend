using System;
using System.Diagnostics;
using System.Text.Json;

namespace TraceabilitySystem.Shared.Helpers;

public static class DebugHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    [Conditional("DEBUG")]
    public static void Log(string message)
    {
        Write(message);
    }

    [Conditional("DEBUG")]
    public static void Log(string title, object? value)
    {
        Write($"{title}\n{JsonSerializer.Serialize(value, JsonOptions)}");
    }

    [Conditional("DEBUG")]
    public static void Separator()
    {
        Write(new string('=', 80));
    }

    private static void Write(string message)
    {
        var current = Console.ForegroundColor;

        Console.ForegroundColor = ConsoleColor.Green;

        Console.WriteLine(
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");

        Console.ForegroundColor = current;
    }
}