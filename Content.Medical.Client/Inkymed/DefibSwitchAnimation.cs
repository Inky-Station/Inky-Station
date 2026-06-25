namespace Content.Medical.Client.Inkymed;


public sealed class DefibSwitchAnimation(AnimatedTextureRect texture, bool enabled) // kill me please
{
    private const float SwitchDuration = 0.165f;

    public AnimatedTextureRect Texture { get; } = texture;
    public bool Enabled { get; } = enabled;
    public float TimeLeft { get; set; } = SwitchDuration;
    public bool IsFinished => TimeLeft <= 0f;
}
