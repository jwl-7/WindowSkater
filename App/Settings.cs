using System;
using Microsoft.Win32;

namespace WindowSkater;

/// <summary>User choices persisted in the registry. Failures are ignored: settings are a convenience.</summary>
internal static class Settings
{
    public const string RandomCharacter = "Random";

    private const string AppKey = @"Software\WindowSkater";
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string CharacterValue = "Character", RunValue = "WindowSkater";

    public static string Character
    {
        get => ReadString(AppKey, CharacterValue) ?? RandomCharacter;
        set => WriteString(AppKey, CharacterValue, value);
    }

    public static bool RunAtStartup
    {
        get => ReadString(RunKey, RunValue) is not null;
        set
        {
            if (value && Environment.ProcessPath is { } path) WriteString(RunKey, RunValue, $"\"{path}\"");
            else DeleteValue(RunKey, RunValue);
        }
    }

    private static string? ReadString(string subKey, string name)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(subKey);
            return key?.GetValue(name) as string;
        }
        catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException) { return null; }
    }

    private static void WriteString(string subKey, string name, string value)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(subKey);
            key.SetValue(name, value);
        }
        catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException) { }
    }

    private static void DeleteValue(string subKey, string name)
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(subKey, writable: true);
            key?.DeleteValue(name, throwOnMissingValue: false);
        }
        catch (Exception e) when (e is System.Security.SecurityException or UnauthorizedAccessException) { }
    }
}
