using System.Reflection;

namespace SupabaseAuth;

public static class Util
{
    public static string GetAssemblyVersion()
    {
        var assembly = typeof(AuthClient).Assembly;
        var informationVersion =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var name = assembly.GetName().Name;

        return $"{name?.ToLower()}-csharp/{informationVersion}";
    }
}
