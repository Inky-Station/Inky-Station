using System.Linq;
using System.Numerics;
using Content.Inky.Shared.Werewolf.Components;
using Content.Shared.Localizations;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;

namespace Content.Inky.Shared.Werewolf.Systems;

public sealed partial class SharedWerewolfBasicAbilitiesSystem
{
    private const float MarkNotificationInterval = 15f; // in seconds todo werewolf unhardcode?
    public void InitializeWhite()
    {
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, TransfurmWhiteEvent>(TryTransfurmWhite);
        SubscribeLocalEvent<WerewolfBasicAbilitiesComponent, WerewolfPositionQueryEvent>(OnPosQuery);
    }

    private void TryTransfurmWhite(EntityUid uid, WerewolfBasicAbilitiesComponent comp, TransfurmWhiteEvent args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out _)
            || !TryComp<WerewolfMindComponent>(mindId, out var mindComp))
            return;

        if (mindComp.Accumulator < mindComp.TransfurmOnCommandDelay)
        {
            args.Handled = true;
            return;
        }

        Calc(uid, comp, args);

        RaiseLocalEvent(uid, new TransfurmEvent());

        if (mindComp.CurrentMarkedVictim != null)
        {
            // to not have 2 or more marked guys be hunted by the same guy
            RemComp<WerewolfMarkedComponent>(mindComp.CurrentMarkedVictim.Value);
            mindComp.CurrentMarkedVictim = null;
        }

        args.Handled = true;
    }

    private void OnPosQuery(EntityUid uid, WerewolfBasicAbilitiesComponent comp, WerewolfPositionQueryEvent args)
    {
        var pos = Transform(uid).MapPosition;
        args.Positions[uid] = pos.Position;
    }

    /// <summary>
    /// Calculates the closest werewolf to the hunter wolf
    /// </summary>
    private void Calc(EntityUid uid, WerewolfBasicAbilitiesComponent comp, TransfurmWhiteEvent args)
    {
        var entMapCoords = _transform.GetMapCoordinates(uid);
        EntityUid? closestUid = null;
        var minDistanceSq = args.Radius * args.Radius;

        if (_mind.TryGetMind(uid, out var initMind, out _) && TryComp<WerewolfMindComponent>(initMind, out var initMindComp))
            initMindComp.MarkImmune = true; // :trol:

        var eqe = EntityQueryEnumerator<MindContainerComponent>();
        while (eqe.MoveNext(out var otherUid, out var mindContainer))
        {
            if (mindContainer.Mind is not { } mind
                || !TryComp<WerewolfMindComponent>(mind, out var otherMind))
                continue;

            if (otherUid == uid || otherMind.MarkImmune)
                continue;

            var otherMapCoords = _transform.GetMapCoordinates(otherUid);

            if (otherMapCoords.MapId != entMapCoords.MapId)
                continue;

            var distSq = Vector2.DistanceSquared(entMapCoords.Position, otherMapCoords.Position);
            if (distSq < minDistanceSq)
            {
                minDistanceSq = distSq;
                closestUid = otherUid;
            }
        }

        if (closestUid == null)
        {
            args.Handled = true;
            return;
        }

        var mark = EnsureComp<WerewolfMarkedComponent>(closestUid.Value);
        mark.MarkedBy = uid;

        _popup.PopupEntity(Loc.GetString("werewolf-marked-popup"),
            closestUid.Value,
            closestUid.Value,
            PopupType.LargeCaution);
    }

    public void UpdateMark(float frameTime) // todo werewolf doesnt workkkkkk
    {
        base.Update(frameTime);
        var eqe = EntityQueryEnumerator<WerewolfBasicAbilitiesComponent>();
        while (eqe.MoveNext(out var uid, out var comp))
        {
            if (!_mind.TryGetMind(uid, out var mindId, out _)
                || !TryComp<WerewolfMindComponent>(mindId, out var mindComp))
                continue;
            // partially copied from heretic living heart todo werewolf replace with the vampire thingy when thats around bcuz this right here is a horrible piece of crap
            if (mindComp.CurrentMarkedVictim == null)
                continue;

            var victim = mindComp.CurrentMarkedVictim.Value;
            if (TryComp<MobStateComponent>(uid, out var hunterState) && hunterState.CurrentState == MobState.Dead)
            {
                RemComp<WerewolfMarkedComponent>(victim);
                mindComp.CurrentMarkedVictim = null;
                continue;
            }
            if (TryComp<MobStateComponent>(victim, out var victimState) && victimState.CurrentState == MobState.Dead)
            {
                RemComp<WerewolfMarkedComponent>(victim);
                mindComp.CurrentMarkedVictim = null;
                continue;
            }

            mindComp.AccumulatorPopup -= frameTime;
            if (victimState == null)
                return;
            if (mindComp.AccumulatorPopup < 0)
            {
                mindComp.AccumulatorPopup = MarkNotificationInterval;
                string loc;

                var state = victimState.CurrentState;
                var locstate = state.ToString().ToLower();

                var ourMapCoords = _transform.GetMapCoordinates(uid);
                var targetMapCoords = _transform.GetMapCoordinates(victim);

                if (_map.IsPaused(targetMapCoords.MapId))
                    loc = Loc.GetString("heretic-livingheart-unknown"); // todo werewolf
                else if (targetMapCoords.MapId != ourMapCoords.MapId)
                    loc = Loc.GetString("heretic-livingheart-faraway", ("state", locstate));
                else
                {
                    var targetStation = _station.GetOwningStation(victim);
                    var ownStation = _station.GetOwningStation(uid);

                    var isOnStation = targetStation != null && targetStation == ownStation;

                    var ang = Angle.Zero;
                    if (_mapMan.TryFindGridAt(_transform.GetMapCoordinates(Transform(uid)), out var grid, out var _))
                        ang = Transform(grid).LocalRotation;

                    var vector = targetMapCoords.Position - ourMapCoords.Position;
                    var direction = (vector.ToWorldAngle() - ang).GetDir();

                    var locdir = ContentLocalizationManager.FormatDirection(direction).ToLower();

                    loc = Loc.GetString(isOnStation ? "heretic-livingheart-onstation" : "heretic-livingheart-offstation",
                        ("state", locstate),
                        ("direction", locdir));
                }

                _popup.PopupEntity(loc, uid, uid, PopupType.Medium);
            }
        }
    }

}
