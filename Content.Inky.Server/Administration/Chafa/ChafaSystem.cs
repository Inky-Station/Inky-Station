using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Inky.Shared.Administration.Chafa;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Inky.Server.Administration.Chafa;

public sealed partial class ChafaSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EuiManager _eui = default!;
    [Dependency] private IChatManager _chat = default!;

    private TimeSpan _nextUpdate = TimeSpan.Zero;
    private readonly List<ChafaRequest> _requests = [];
    private readonly TimeSpan _requestUpdateInterval = TimeSpan.FromSeconds(1);
    private readonly TimeSpan _requestTimeoutTime = TimeSpan.FromSeconds(20);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ChafaResponseEvent>(ChafaResponse);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_requests.Count == 0)
            return;

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + _requestUpdateInterval;

        if (!_requests.Any(req => req.TimeoutTime < _timing.CurTime))
            return;

        foreach (var request in _requests)
        {
            if (request.TimeoutTime < _timing.CurTime)
                _chat.SendAdminAnnouncementMessage(request.RequestBy, "chafa timeout");
        }

        _requests.RemoveAll(req => req.TimeoutTime < _timing.CurTime);
    }

    public void ChafaRequest(ICommonSession session, ICommonSession requestBy)
    {
        if (_requests.Any(req => req.RequestBy == requestBy))
        {
            _chat.SendAdminAnnouncementMessage(requestBy, "chafa request already sent, wait");
            return;
        }

        var requestHolder = new ChafaRequest
        {
            Player = session,
            RequestBy = requestBy,
            TimeoutTime = _timing.CurTime + _requestTimeoutTime
        };

        _requests.Add(requestHolder);
        RaiseNetworkEvent(new ChafaRequestEvent(), session);
    }

    private void ChafaResponse(ChafaResponseEvent ev, EntitySessionEventArgs args)
    {
        if (ev.Image is null)
            return;

        if (!_requests.Any(req => req.Player == args.SenderSession))
            return;

        foreach (var request in _requests)
        {
            if (request.Player == args.SenderSession)
            {
                var eui = new ChafaEui(ev.Image);

                _eui.OpenEui(eui, request.RequestBy);
                eui.DoStateUpdate();
            }
        }

        _requests.RemoveAll(req => req.Player == args.SenderSession);
    }
}

public struct ChafaRequest
{
    public ICommonSession RequestBy;
    public ICommonSession Player;
    public TimeSpan TimeoutTime;
}
