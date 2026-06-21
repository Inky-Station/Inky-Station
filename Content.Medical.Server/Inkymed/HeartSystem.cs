using Content.Medical.Shared.Body;
using Content.Shared.Alert;
using Content.Shared.Body;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Timing;

namespace Content.Medical.Server.Inkymed;

public sealed partial class HeartSystem : EntitySystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();
        _nextUpdate = _timing.CurTime + UpdateInterval;

        SubscribeLocalEvent<HeartComponent, ComponentInit>(OnHeartInit);
    }

    private void OnHeartInit(EntityUid uid, HeartComponent heart, ComponentInit args)
    {
        heart.CurrentHeartRate = heart.StartingHeartRate;
        Dirty(uid, heart);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var eqe = EntityQueryEnumerator<HeartComponent, OrganComponent>();
        while (eqe.MoveNext(out var uid, out var heart, out var organ))
        {
            UpdateHeart(uid, heart, organ);
        }
    }

    private void UpdateHeart(EntityUid uid, HeartComponent heart, OrganComponent organ)
    {
        var body = organ.Body;

        var isAlive = body != null
            && TryComp<MobStateComponent>(body, out var mobState)
            && mobState.CurrentState != MobState.Dead;

        if (heart.CurrentlyActive != isAlive)
        {
            heart.CurrentlyActive = isAlive;
            Dirty(uid, heart);
        }

        // if youre dead then bpm falls
        if (!heart.CurrentlyActive)
        {
            if (heart.CurrentHeartRate <= heart.MinHeartRate)
                return;

            var reductionidk = Math.Max(heart.CurrentHeartRate - (int)Math.Round(heart.StabilisationRate), heart.MinHeartRate);
            if (reductionidk == heart.CurrentHeartRate)
                return;

            heart.CurrentHeartRate = reductionidk;
            if (body is { } deadBody)
                UpdateAlerts(deadBody, heart);
            Dirty(uid, heart);
            return;
        }

        var shit = Math.Abs(heart.CurrentHeartRate - heart.StartingHeartRate);
        var shouldFibrillate = shit >= heart.FibrillationCap;

        if (heart.CurrentlyFibrillating != shouldFibrillate)
        {
            heart.CurrentlyFibrillating = shouldFibrillate;
            if (body is { } b)
                UpdateAlerts(b, heart);
            Dirty(uid, heart);
        }


        if (heart.CurrentHeartRate == heart.StartingHeartRate)
            return;

        /*
         * Being in Fibrillations drifts AWAY from the startingheartrate
         * outward toward min/max
         * Being stable drifts TOWARDS startingheartrate
         */
        float delta = heart.CurrentHeartRate < heart.StartingHeartRate ^ heart.CurrentlyFibrillating // today i have learned that ^ is xor, creds to @pheenty
            ? heart.StabilisationRate
            : -heart.StabilisationRate;

        var newRate = (int)Math.Round(heart.CurrentHeartRate + delta); // bro thinks its double and it breaks

        // snap to target only when stabilising
        if (!heart.CurrentlyFibrillating)
        {
            if (delta > 0 && newRate > heart.StartingHeartRate
                || delta < 0 && newRate < heart.StartingHeartRate)
                newRate = heart.StartingHeartRate;
        }

        newRate = Math.Clamp(newRate, heart.MinHeartRate, heart.MaxHeartRate);

        // flatline or going to the 300BPMs flips the BPM to 0 and shuts it down
        if (newRate <= heart.MinHeartRate
            || newRate >= heart.MaxHeartRate)
        {
            heart.CurrentHeartRate = heart.MinHeartRate;
            heart.CurrentlyActive = false;
            heart.CurrentlyFibrillating = false;
            if (body is { } b)
                UpdateAlerts(b, heart);
            Dirty(uid, heart);
            return;
        }

        SetHeartRate(uid, heart, newRate);
    }

    #region api

    public void SetHeartRate(EntityUid uid,
        HeartComponent heart,
        int rate,
        bool forced = true)
    {
        if (!heart.CurrentlyActive && !forced)
            return;

        var clamped = Math.Clamp(rate, heart.MinHeartRate, heart.MaxHeartRate);
        if (heart.CurrentHeartRate == clamped)
            return;

        heart.CurrentHeartRate = clamped;
        Dirty(uid, heart);
    }

    #endregion

    private void UpdateAlerts(EntityUid body, HeartComponent heart)
    {
        if (heart.FibrillationAlert is { } fibAlert)
        {
            if (heart.CurrentlyFibrillating)
                _alerts.ShowAlert(body, fibAlert);
            else
                _alerts.ClearAlert(body, fibAlert);
        }

        if (heart.HeartStopAlert is { } stopAlert)
        {
            if (heart.CurrentHeartRate <= heart.MinHeartRate)
                _alerts.ShowAlert(body, stopAlert);
            else
                _alerts.ClearAlert(body, stopAlert);
        }
    }
}
