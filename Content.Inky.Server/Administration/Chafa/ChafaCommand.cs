using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Inky.Server.Administration.Chafa;

[AdminCommand(AdminFlags.Moderator)]
public sealed partial class CaptureCommand : IConsoleCommand
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IEntityManager _manager = default!;

    public string Command => "chafa";
    public string Description => "chafas the display of an entity";
    public string Help => "chafa <id>";

    public async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is null)
        {
            shell.WriteLine(Loc.GetString("shell-only-players-can-run-this-command"));
            return;
        }

        if (args[0] is null)
        {
            shell.WriteLine(Loc.GetString("shell-target-player-does-not-exist"));
            return;
        }

        if (!_player.TryGetSessionByUsername(args[0], out var session))
        {
            shell.WriteLine(Loc.GetString("shell-target-player-does-not-exist"));
            return;
        }

        _manager.System<ChafaSystem>().ChafaRequest(session, shell.Player);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Loc.GetString("shell-argument-username-hint"));
        }

        return CompletionResult.Empty;
    }
}
