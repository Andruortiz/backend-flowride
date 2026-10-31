namespace FlowRide.Nucleo.Helpers;

public static class UuidHelper
{
    public static Guid GetDefault() => Guid.Empty;

    public static Guid GetDefault(Guid? value) => value ?? GetDefault();

    public static bool IsDefault(Guid? value) => GetDefault(value) == GetDefault();

    public static Guid FromString(string? text) =>
        TextHelper.IsEmptyWithTrim(text) ? GetDefault() : Guid.Parse(text);

    public static Guid Generate() => Guid.CreateVersion7();

    public static Guid GetOrGenerate(Guid? value) => IsDefault(value) ? Generate() : value!.Value;
}