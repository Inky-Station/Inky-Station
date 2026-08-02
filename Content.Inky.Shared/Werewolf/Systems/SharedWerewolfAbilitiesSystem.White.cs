using System.Numerics;
using Content.Inky.Shared.Werewolf.Components;
using Content.Shared.Localizations;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Trauma.Common.CollectiveMind;

namespace Content.Inky.Shared.Werewolf.Systems;

public sealed partial class SharedWerewolfAbilitiesSystem
{
    private readonly TimeSpan _markNotificationInterval = TimeSpan.FromSeconds(15); // in seconds todo werewolf unhardcode?
    public void InitializeWhite()
    {
        SubscribeLocalEvent<WerewolfAbilitiesComponent, TransfurmWhiteEvent>(TryTransfurmWhite);
        SubscribeLocalEvent<WerewolfAbilitiesComponent, WerewolfPositionQueryEvent>(OnPosQuery);
        SubscribeLocalEvent<WerewolfAbilitiesComponent, WerewolfAddCollectivemind>(OnCollectiveMindBuy);
        SubscribeLocalEvent<WerewolfAbilitiesComponent, WerewolfRevelationEvent>(OnRevelation);
    }

    private void TryTransfurmWhite(EntityUid uid, WerewolfAbilitiesComponent comp, TransfurmWhiteEvent args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out _)
            || !TryComp<WerewolfMindComponent>(mindId, out var mindComp))
            return;

        if (mindComp.Accumulator < mindComp.TransfurmOnCommandDelay)
        {
            args.Handled = true;
            return;
        }

        var victimMindUid = FindFurryErpPartner(uid, comp, args);

        RaiseLocalEvent(uid, new TransfurmEvent());

        if (mindComp.CurrentMarkedVictim != null)
        {
            var oldVictimEntity = GetMindContainer(mindComp.CurrentMarkedVictim.Value);
            if (oldVictimEntity != null)
                RemComp<WerewolfMarkedComponent>(oldVictimEntity.Value);
            mindComp.CurrentMarkedVictim = null;
        }

        if (victimMindUid != null)
            mindComp.CurrentMarkedVictim = victimMindUid;

        args.Handled = true;
    }

    private void OnPosQuery(EntityUid uid, WerewolfAbilitiesComponent comp, WerewolfPositionQueryEvent args)
    {
        var pos = Transform(uid).MapPosition;
        args.Positions[uid] = pos.Position;
    }

    /// <summary>
    /// Calculates the closest werewolf to the hunter wolf (the mind)
    /// </summary>
    private EntityUid? FindFurryErpPartner(EntityUid uid, WerewolfAbilitiesComponent comp, TransfurmWhiteEvent args) // FUCKING KILL YOURSELF
    {
        var entMapCoords = _transform.GetMapCoordinates(uid);
        EntityUid? closestUid = null;
        EntityUid? closestMindId = null;
        var minDistanceSq = args.Radius * args.Radius;

        if (_mind.TryGetMind(uid, out var initMind, out _) && TryComp<WerewolfMindComponent>(initMind, out var initMindComp))
            initMindComp.MarkImmune = true; // :trol:

        var eqe = EntityQueryEnumerator<MindContainerComponent>();
        while (eqe.MoveNext(out var otherUid, out var mindContainer))
        {
            if (mindContainer.Mind is not { } mind
                || !TryComp<WerewolfMindComponent>(mind, out var otherMind)
                || otherUid == uid
                || otherMind.MarkImmune)
                continue;

            var otherMapCoords = _transform.GetMapCoordinates(otherUid);

            if (otherMapCoords.MapId != entMapCoords.MapId)
                continue;

            var distSq = Vector2.DistanceSquared(entMapCoords.Position, otherMapCoords.Position);
            if (distSq < minDistanceSq)
            {
                minDistanceSq = distSq;
                closestUid = otherUid;
                closestMindId = mind; // fuck!
            }
        }

        if (closestUid == null)
            return null;

        var mark = EnsureComp<WerewolfMarkedComponent>(closestUid.Value);
        mark.MarkedBy = uid;

        _popup.PopupEntity(Loc.GetString("werewolf-marked-popup"),
            closestUid.Value,
            closestUid.Value,
            PopupType.LargeCaution);

        return closestMindId;
    }

    public void UpdateMark(float frameTime) // its not frameTime but who cares lmao
    {
        var eqe = EntityQueryEnumerator<WerewolfAbilitiesComponent>();
        while (eqe.MoveNext(out var ent))
        {
            var uid = ent.Owner;
            if (!_mind.TryGetMind(uid, out var mindId, out _)
                || !TryComp<WerewolfMindComponent>(mindId, out var mindComp))
                continue;
            // partially copied from heretic living heart todo werewolf replace with the vampire thingy when thats around bcuz this right here is a horrible piece of crap
            if (mindComp.CurrentMarkedVictim == null)
                continue;

            var victimEnt = GetMindContainer(mindComp.CurrentMarkedVictim.Value);
            if (victimEnt == null)
            {
                mindComp.CurrentMarkedVictim = null;
                continue;
            }

            var victim = victimEnt.Value;

            if (TryComp<MobStateComponent>(uid, out var hunterState) && hunterState.CurrentState == MobState.Dead)
            {
                if (TryComp<MobStateComponent>(victim, out _))
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

            mindComp.AccumulatorPopup -= TimeSpan.FromSeconds(frameTime);
            if (mindComp.AccumulatorPopup.Ticks > 0)
                continue;

            if (victimState == null)
                return;

            mindComp.AccumulatorPopup = _markNotificationInterval;

            var ourMapCoords = _transform.GetMapCoordinates(uid);
            var targetMapCoords = _transform.GetMapCoordinates(victim);

            string loc;
            var state = victimState.CurrentState;
            var locstate = state.ToString().ToLower();
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
                if (_map.TryFindGridAt(_transform.GetMapCoordinates(Transform(uid)), out var grid, out var _))
                    ang = Transform(grid).LocalRotation;

                var vector = targetMapCoords.Position - ourMapCoords.Position;
                var direction = (vector.ToWorldAngle() - ang).GetDir();

                var locdir = ContentLocalizationManager.FormatDirection(direction).ToLower();

                loc = Loc.GetString(isOnStation ? "heretic-livingheart-onstation" : "heretic-livingheart-offstation", // GOIDA!!!
                    ("state", locstate),
                    ("direction", locdir));
            }

            _popup.PopupEntity(loc, uid, uid, PopupType.Medium);
        }
    }

    private void OnCollectiveMindBuy(EntityUid uid,
        WerewolfAbilitiesComponent comp,
        WerewolfAddCollectivemind args)
    {
        EnsureComp<CollectiveMindComponent>(uid, out var m);
        m.Channels.Add(comp.CollectiveMindChannel);
        if (args.Popup != null)
            _popup.PopupEntity(Loc.GetString(args.Popup), uid, uid, PopupType.Medium);
    }

    private void OnRevelation(EntityUid uid,
        WerewolfAbilitiesComponent comp,
        WerewolfRevelationEvent args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out _)
            || !TryComp<WerewolfMindComponent>(mindId, out var mindComp))
            return;

        RaiseLocalEvent(uid, new TransfurmWhiteEvent());
        mindComp.BlockTransfurm = true;
    }


    private EntityUid? GetMindContainer(EntityUid targetMind)
    {
        var eqe = EntityQueryEnumerator<MindContainerComponent>();
        while (eqe.MoveNext(out var entityUid, out var mindContainer))
        {
            if (mindContainer.Mind == targetMind)
                return entityUid;
        }
        return null;
    }
}
