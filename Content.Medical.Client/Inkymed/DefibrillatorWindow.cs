using Content.Medical.Shared.Inkymed;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Timing;

namespace Content.Medical.Client.Inkymed;

[GenerateTypedNameReferences]
public sealed partial class DefibrillatorWindow : DefaultWindow
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IResourceCache _resourceCache = default!;

    private static ResPath SwitchRsi = new("_Inky/Inkymed/UserInterface/switches.rsi");
    private static ResPath OnOffRsi = new("_Inky/Inkymed/UserInterface/onoff.rsi");
    private static ResPath BpmFont = new("/Fonts/_Trauma/Pixellari.ttf");

    // after not doing anything for .Delay time, sends a message to the server with the flips
    // is here because of fucking misspredicts on flips (picture id computer delay)
    // even if its 0, if you remove it completely if fucks up the predictions MASSIVELY
    private const float Delay = 0.0f;

    private DefibrillatorChargeSetting _activeSetting = new();
    private DefibrillatorChargeSetting _serverSetting = new();
    private ProtoId<PulseStatePrototype>? _pulseState;
    private bool _serverSend;
    private float? _sendTimeLeft;
    private List<DefibSwitchAnimation> _animations = new();

    public event Action<DefibrillatorChargeSetting>? OnChargeSettingSelected;

    public DefibrillatorWindow()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);

        WindowHeader.Visible = false;
        ContentsContainer.Margin = new Thickness(0f);
        BpmLabel.FontOverride = new VectorFont(_resourceCache.GetResource<FontResource>(BpmFont), 16);

        IOInit(IOTexture);
        PulseInit(PulseTexture);
        SwitchInit(LOWTexture);
        SwitchInit(STNTexture);
        SwitchInit(HGHTexture);
        SwitchInit(MAXTexture);
        SetSwitchesDisabled(true);

        IO.OnPressed += _ => TogglePower();
        LOW.OnPressed += _ => ToggleFlip(0);
        STN.OnPressed += _ => ToggleFlip(1);
        HGH.OnPressed += _ => ToggleFlip(2);
        MAX.OnPressed += _ => ToggleFlip(3); // goida bro

        var transparent = new StyleBoxFlat // todo find something better
        {
            BackgroundColor = Color.Transparent,
        };
        IO.StyleBoxOverride = transparent;
        LOW.StyleBoxOverride = transparent;
        STN.StyleBoxOverride = transparent;
        HGH.StyleBoxOverride = transparent;
        MAX.StyleBoxOverride = transparent;
    }

    public void SetActiveSetting(DefibrillatorChargeSetting setting)
    {
        if (setting.Flips.Length != DefibrillatorChargeSetting.FlipAmount)
            return;

        if (_sendTimeLeft != null)
            return;

        var serverSended = _serverSend;
        if (serverSended && !SettingsEqual(setting, _activeSetting)) // KILL YOURSELF
            return;

        _serverSend = false; // weve found out stuff that server sended so we are supposed to be clean
        _serverSetting = setting.Clone();

        if (!serverSended)
            ApplySetting(setting);

        SetSwitchesDisabled(false);
    }

    public void FlushPendingSetting()
    {
        if (_sendTimeLeft == null)
            return;

        SendSetting();
    }

    public void SetPulseState(ProtoId<PulseStatePrototype> pulseState)
    {
        if (_pulseState == pulseState)
            return;

        _pulseState = pulseState;
        var proto = _prototypeManager.Index(pulseState);
        PulseTexture.SetFromSpriteSpecifier(proto.Sprite);
    }

    public void SetBpm(float? bpm)
    {
        BpmLabel.Text = bpm == null
            ? "---"
            : $"{MathF.Round(bpm.Value)}";
    }

    private void ApplySetting(DefibrillatorChargeSetting setting)
    {
        if (_activeSetting.Power != setting.Power)
            SetIOSprite(IOTexture, setting.Power);

        Flip(LOWTexture, _activeSetting.Flips[0], setting.Flips[0]); // GOIDA
        Flip(STNTexture, _activeSetting.Flips[1], setting.Flips[1]);
        Flip(HGHTexture, _activeSetting.Flips[2], setting.Flips[2]);
        Flip(MAXTexture, _activeSetting.Flips[3], setting.Flips[3]);
        _activeSetting = setting.Clone();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        UpdatePendingSend(args.DeltaSeconds);
        UpdateSwitchAnimations(args.DeltaSeconds);
    }

    private void UpdatePendingSend(float deltaSeconds)
    {
        if (_sendTimeLeft == null)
            return;

        _sendTimeLeft -= deltaSeconds;
        if (_sendTimeLeft <= 0f)
            SendSetting();
    }

    private void UpdateSwitchAnimations(float deltaSeconds)
    {
        foreach (var animation in _animations)
        {
            animation.TimeLeft -= deltaSeconds;
            if (!animation.IsFinished)
                continue;

            SetSwitchSprite(animation.Texture, GetSwitchState(animation.Enabled));
        }

        _animations.RemoveAll(animation => animation.IsFinished);
    }

    private static string GetSwitchState(bool enabled)
    {
        return enabled
            ? "defib_switch_on"
            : "defib_switch_off";
    }

    protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
    {
        return DragMode.Move;
    }

    private void TogglePower()
    {
        var setting = _activeSetting.Clone();
        setting.Power = !setting.Power;
        ApplySetting(setting);
        _sendTimeLeft = Delay;
    }

    private void ToggleFlip(int thing)
    {
        var setting = _activeSetting.Clone();
        setting.Flips[thing] = !setting.Flips[thing];
        ApplySetting(setting);
        _sendTimeLeft = Delay;
    }

    private void SendSetting()
    {
        _sendTimeLeft = null;

        if (SettingsEqual(_activeSetting, _serverSetting))
            return;

        _serverSend = true;
        OnChargeSettingSelected?.Invoke(_activeSetting);
    }

    private void Flip(AnimatedTextureRect texture, bool oldValue, bool newValue)
    {
        if (oldValue == newValue)
            return;

        SetFlipState(texture, newValue);
    }

    private static bool SettingsEqual(DefibrillatorChargeSetting first, DefibrillatorChargeSetting second)
    {
        return first.Power == second.Power && first.Flips.SequenceEqual(second.Flips);
    }

    private void SetFlipState(AnimatedTextureRect texture, bool enabled)
    {
        SetSwitchSprite(texture, enabled
            ? "defib_switch_turning_on"
            : "defib_switch_turning_off");
        StopAnimation(texture);
        _animations.Add(new DefibSwitchAnimation(texture, enabled));
    }

    private static void SwitchInit(AnimatedTextureRect texture)
    {
        texture.DisplayRect.TextureScale = new Vector2(3f);
        SetSwitchSprite(texture, "defib_switch_off");
    }

    private static void IOInit(AnimatedTextureRect texture)
    {
        texture.DisplayRect.TextureScale = new Vector2(3f);
        SetIOSprite(texture, false);
    }

    private static void PulseInit(AnimatedTextureRect texture)
    {
        texture.DisplayRect.TextureScale = new Vector2(3f);
    }

    private static void SetSwitchSprite(AnimatedTextureRect texture, string state)
    {
        texture.SetFromSpriteSpecifier(new SpriteSpecifier.Rsi(SwitchRsi, state));
    }

    private static void SetIOSprite(AnimatedTextureRect texture, bool enabled)
    {
        texture.SetFromSpriteSpecifier(new SpriteSpecifier.Rsi(OnOffRsi, enabled
            ? "i"
            : "o"));
    }

    private void StopAnimation(AnimatedTextureRect texture)
    {
        _animations.RemoveAll(animation => animation.Texture == texture);
    }

    private void SetSwitchesDisabled(bool disabled)
    {
        IO.Disabled = disabled;
        LOW.Disabled = disabled;
        STN.Disabled = disabled;
        HGH.Disabled = disabled;
        MAX.Disabled = disabled;
    }
}
