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

    private DefibrillatorChargeSetting _activeSetting;
    private DefibrillatorChargeSetting _serverSetting;
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

        IO.OnPressed += _ => ToggleFlip(DefibrillatorChargeSetting.PowerFlip);
        LOW.OnPressed += _ => ToggleFlip(DefibrillatorChargeSetting.FirstFlip);
        STN.OnPressed += _ => ToggleFlip(DefibrillatorChargeSetting.SecondFlip);
        HGH.OnPressed += _ => ToggleFlip(DefibrillatorChargeSetting.ThirdFlip);
        MAX.OnPressed += _ => ToggleFlip(DefibrillatorChargeSetting.FourthFlip); // todo inkymed compress this shit idk

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
        if (_sendTimeLeft != null)
            return;

        var serverSended = _serverSend;
        if (serverSended && setting != _activeSetting)
            return;

        _serverSend = false; // weve found out stuff that server sended so we are supposed to be clean
        _serverSetting = setting;

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
        var changed = _activeSetting ^ setting; // ty pheenty
        _activeSetting = setting;

        IOFlip(IOTexture, DefibrillatorChargeSetting.PowerFlip, setting, changed);
        Flip(LOWTexture, DefibrillatorChargeSetting.FirstFlip, setting, changed);
        Flip(STNTexture, DefibrillatorChargeSetting.SecondFlip, setting, changed);
        Flip(HGHTexture, DefibrillatorChargeSetting.ThirdFlip, setting, changed);
        Flip(MAXTexture, DefibrillatorChargeSetting.FourthFlip, setting, changed);
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

    private void ToggleFlip(DefibrillatorChargeSetting flip)
    {
        var setting = _activeSetting ^ flip;
        ApplySetting(setting);
        _sendTimeLeft = Delay;
    }

    private void SendSetting()
    {
        _sendTimeLeft = null;

        if (_activeSetting == _serverSetting)
            return;

        _serverSend = true;
        OnChargeSettingSelected?.Invoke(_activeSetting);
    }

    private void Flip(
        AnimatedTextureRect texture,
        DefibrillatorChargeSetting flip,
        DefibrillatorChargeSetting setting,
        DefibrillatorChargeSetting changed)
    {
        if (!changed.HasFlag(flip))
            return;

        SetFlipState(texture, setting.HasFlag(flip));
    }

    private static void IOFlip(
        AnimatedTextureRect texture,
        DefibrillatorChargeSetting flip,
        DefibrillatorChargeSetting setting,
        DefibrillatorChargeSetting changed)
    {
        if (!changed.HasFlag(flip))
            return;

        SetIOSprite(texture, setting.HasFlag(flip));
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
