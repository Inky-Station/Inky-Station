using Content.Inky.Shared.Administration.Chafa;
using Robust.Client.Graphics;
using SixLabors.ImageSharp;
using System.IO;

namespace Content.Inky.Client.Administration.Chafa;

public sealed partial class ChafaSystem : EntitySystem
{
    [Dependency] private IClyde _clyde = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ChafaRequestEvent>(OnChafaRequest);
    }

    private async void OnChafaRequest(ChafaRequestEvent ev)
    {
        var capture = await _clyde.ScreenshotAsync(ScreenshotType.Final);
        using var ms = new MemoryStream();
        await capture.SaveAsPngAsync(ms);
        RaiseNetworkEvent(new ChafaResponseEvent(ms.ToArray()));
    }
}
