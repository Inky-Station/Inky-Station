using System.Numerics;
using Content.Client.UserInterface.Systems.DamageOverlays.Overlays;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Inky.Client.Concussion;

// damageoverlay copypaste hell yeah (kill me)
public sealed class ConcussionOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> CircleMaskShader = "GradientCircleMask";

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IPlayerManager _gamerMan = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    private readonly ShaderInstance _concussionShader;

    public float ConcussionLevel = 0f; // 0–1 intensity fed from the ui controller EACH FRAME
    private float _smoothedLevel = 0f;

    public ConcussionOverlay()
    {
        IoCManager.InjectDependencies(this);
        _concussionShader = _prototypeManager.Index(CircleMaskShader).InstanceUnique();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_entMan.TryGetComponent(_gamerMan.LocalEntity, out EyeComponent? eyeComp))
            return;

        if (args.Viewport.Eye != eyeComp.Eye)
            return;

        var viewport = args.WorldAABB;
        var handle = args.WorldHandle;
        var distance = args.ViewportBounds.Width;

        var lastFrameTime = (float) _timing.FrameTime.TotalSeconds;

        if (!MathHelper.CloseTo(_smoothedLevel, ConcussionLevel, 0.001f))
        {
            var diff = ConcussionLevel - _smoothedLevel;
            _smoothedLevel += GetDiff(diff, lastFrameTime);
        }
        else
            _smoothedLevel = ConcussionLevel;

        if (_smoothedLevel <= 0f)
        {
            handle.UseShader(null);
            return;
        }
        var outerMaxLevel = 2.0f * distance;
        var outerMinLevel = 0.7f * distance;
        var innerMaxLevel = 0.5f * distance;
        var innerMinLevel = 0.05f * distance;

        var outerRadius = outerMaxLevel - _smoothedLevel * (outerMaxLevel - outerMinLevel);

        // fade starts at 0.6, fully gone by 0.8
        var innerFade = 1f - Math.Clamp((_smoothedLevel - 0.5f) / 0.2f, 0f, 1f); // I HATE MATH

        var innerRadius = (innerMaxLevel - _smoothedLevel * (innerMaxLevel - innerMinLevel)) * innerFade;
        var innerMaxRadius = innerRadius + 0.002f * distance * (1f - _smoothedLevel) * innerFade;

        // _concussionShader.SetParameter("time", pulse);
        _concussionShader.SetParameter("color", new Vector3(1f, 1f, 1.0f));
        _concussionShader.SetParameter("darknessAlphaOuter", 2.5f);
        _concussionShader.SetParameter("outerCircleRadius", outerRadius);
        _concussionShader.SetParameter("outerCircleMaxRadius", outerRadius + 0.2f * distance);
        _concussionShader.SetParameter("innerCircleRadius", innerRadius);
        _concussionShader.SetParameter("innerCircleMaxRadius", innerMaxRadius);

        handle.UseShader(_concussionShader);
        handle.DrawRect(viewport, Color.White);
        handle.UseShader(null);
    }

    private static float GetDiff(float value, float lastFrameTime)
    {
        var adjustment = value * 5f * lastFrameTime;

        if (value < 0f)
            adjustment = Math.Clamp(adjustment, value, -value);
        else
            adjustment = Math.Clamp(adjustment, -value, value);

        return adjustment;
    }
}
