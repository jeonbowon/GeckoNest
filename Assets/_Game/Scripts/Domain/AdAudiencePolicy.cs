using System.Globalization;

/// <summary>Conservative global policy: all under-18 and unknown users receive child treatment.</summary>
public static class AdAudiencePolicy
{
    public const int Version = 1;

    public static bool HasChoice(SettingsData settings) => settings != null
        && settings.adAudienceVersion == Version
        && (settings.adAudience == AdAudience.Unknown || settings.adAudience == AdAudience.Minor
            || settings.adAudience == AdAudience.Adult);

    public static bool RequiresProtection(SettingsData settings) =>
        !HasChoice(settings) || settings.adAudience != AdAudience.Adult;

    public static bool TryClassify(string text, out AdAudience audience)
    {
        audience = AdAudience.Unknown;
        if (string.IsNullOrEmpty(text) || text.Length > 3) return false;
        foreach (char c in text) if (c < '0' || c > '9') return false;
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int age)
            || age > 120) return false;
        audience = age < 18 ? AdAudience.Minor : AdAudience.Adult;
        return true;
    }
}
