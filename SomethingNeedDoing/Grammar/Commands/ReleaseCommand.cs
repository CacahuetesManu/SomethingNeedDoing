using Dalamud.Game.ClientState.Keys;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar.Modifiers;
using SomethingNeedDoing.Misc;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SomethingNeedDoing.Grammar.Commands;

internal class ReleaseCommand : MacroCommand
{
    public static string[] Commands => ["release"];
    public static string Description => "Releases arbitrary keystrokes with optional modifiers that have been set to hold. Keys are pressed in the same order as the command.";
    public static string[] Examples => ["/release MULTIPLY", "/release NUMPAD0", "/release CONTROL+MENU+SHIFT+NUMPAD0"];

    private static readonly Regex Regex = new($@"^/{string.Join("|", Commands)}\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly VirtualKey[] vkCodes;

    private ReleaseCommand(string text, VirtualKey[] vkCodes, WaitModifier wait) : base(text, wait) => this.vkCodes = vkCodes;

    public static ReleaseCommand Parse(string text)
    {
        _ = WaitModifier.TryParse(ref text, out var waitModifier);

        var match = Regex.Match(text);
        if (!match.Success)
            throw new MacroSyntaxError(text);

        var nameValue = ExtractAndUnquote(match, "name");
        var vkCodes = nameValue.Split("+")
            .Select(name =>
            {
                return !Enum.TryParse<VirtualKey>(name, true, out var vkCode) ? throw new MacroCommandError("Invalid virtual key") : vkCode;
            })
            .ToArray();

        return new ReleaseCommand(text, vkCodes, waitModifier);
    }

    public override async Task Execute(ActiveMacro macro, CancellationToken token)
    {
        Svc.Log.Debug($"Executing: {Text}");

        if (vkCodes.Length == 1)
            Keyboard.Release(vkCodes[0]);
        else
        {
            var key = vkCodes.Last();
            var mods = vkCodes.SkipLast(1);
            Keyboard.Release(key, mods);
        }

        await PerformWait(token);
    }
}
