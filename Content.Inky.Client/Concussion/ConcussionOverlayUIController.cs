using Content.Inky.Shared.Concussion;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Inky.Client.Concussion;

[UsedImplicitly]
public sealed class ConcussionOverlayUiController : UIController
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPlayerManager _gamerMan = default!;

    private ConcussionOverlay _overlay = default!;

    public override void Initialize()
    {
        _overlay = new ConcussionOverlay();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttach);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<ConcussionStateChangedEvent>(OnConcussionStateChanged);
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        if (_gamerMan.LocalEntity is not { } player)
            return;

        if (!EntityManager.TryGetComponent<ConcussionThresholdComponent>(player, out var comp))
            return;

        var level = ShitPoo(comp);
        _overlay.ConcussionLevel = level;
    }

    private void OnPlayerAttach(LocalPlayerAttachedEvent args)
    {
        _overlayManager.AddOverlay(_overlay);
        ClearAll();
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
        ClearAll();
    }

    private void OnConcussionStateChanged(ref ConcussionStateChangedEvent args)
    {
        if (args.Target != _gamerMan.LocalEntity)
            return;

        if (args.NewState == ConcussionState.Sane)
            ClearAll();
    }

    private void ClearAll()
        => _overlay.ConcussionLevel = 0f;

    private static float ShitPoo(ConcussionThresholdComponent comp)
    {
        if (comp.CurrentState == ConcussionState.Sane
            || comp.StoredDamage <= 0)
            return 0f;

        FixedPoint2 minorThreshold = 0;
        FixedPoint2 overwhelmThreshold = 0;

        foreach (var (threshold, state) in comp.Thresholds)
        {
            if (state == ConcussionState.Minor)
                minorThreshold = threshold;
            else if (state == ConcussionState.Overwhelmed)
                overwhelmThreshold = threshold;
        }

        var range = (float) overwhelmThreshold - (float) minorThreshold;
        if (range <= 0f)
            return 1f;

        return Math.Clamp(((float) comp.StoredDamage - (float) minorThreshold) / range, 0f, 1f);
    }
}
