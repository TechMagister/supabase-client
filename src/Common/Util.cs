using System.Reflection;

namespace Common;

public static class Util
{
    public static string GetAssemblyVersion(Assembly assembly)
    {
        var informationVersion =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var name = assembly.GetName().Name;

        return $"{name?.ToLower()}-csharp/{informationVersion}";
    }
}
