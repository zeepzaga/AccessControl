namespace AccessControl.Infrastructure.Options;

public class BiometricOptions
{
    public const string SectionName = "Biometric";

    public BiometricLocalOptions Local { get; set; } = new();
}

public class BiometricLocalOptions
{
    public float MatchThreshold { get; set; } = 0.42f;
}
