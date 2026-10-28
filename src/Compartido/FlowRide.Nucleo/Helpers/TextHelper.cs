using System.Diagnostics.CodeAnalysis;

namespace FlowRide.Nucleo.Helpers;

public static class TextHelper
{
    public static string GetDefault() => string.Empty;

    public static string GetDefault(string? value) => value ?? GetDefault();

    public static string GetDefaultWithTrim(string? value) => GetDefault(value).Trim();

    public static bool IsEmpty([NotNullWhen(false)] string? value) => GetDefault(value).Length == 0;

    public static bool IsEmptyWithTrim([NotNullWhen(false)] string? value) => GetDefaultWithTrim(value).Length == 0;
}