// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Server;

namespace Content.Inky.Server;

internal static class Program
{
    public static void Main(string[] args)
    {
        ContentStart.Start(args);
    }
}
