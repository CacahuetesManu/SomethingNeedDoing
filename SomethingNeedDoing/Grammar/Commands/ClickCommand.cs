using ECommons;
using ECommons.Automation.UIInput;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar.Modifiers;
using SomethingNeedDoing.Misc;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SomethingNeedDoing.Grammar.Commands;

internal class ClickCommand : MacroCommand
{
    public static string[] Commands => ["click"];
    public static string Description => "Click a pre-defined button in an addon or window.";
    public static string[] Examples => ["/click RecipeNote_Synthesize"];

    private static readonly Regex Regex = new($@"^/{string.Join("|", Commands)}\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly string clickName;

    private ClickCommand(string text, string clickName, WaitModifier wait) : base(text, wait) => this.clickName = clickName;

    public static ClickCommand Parse(string text)
    {
        _ = WaitModifier.TryParse(ref text, out var waitModifier);

        var match = Regex.Match(text);
        if (!match.Success)
            throw new MacroSyntaxError(text);

        var nameValue = ExtractAndUnquote(match, "name");

        return new ClickCommand(text, nameValue, waitModifier);
    }

    public override async Task Execute(ActiveMacro macro, CancellationToken token)
    {
        Svc.Log.Debug($"Executing: {Text}");

        try
        {
            unsafe
            {
                var addonName = clickName[..clickName.IndexOf('_')];
                if (GenericHelpers.TryGetAddonByName<AtkUnitBase>(addonName, out var addon))
                    ClickHelper.SendClick(clickName, (nint)addon);
                else
                    throw new MacroCommandError($"Addon {addonName} not found.");
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error(ex, "Unexpected click error");
            throw new MacroCommandError("Unexpected click error", ex);
        }

        await PerformWait(token);
    }
}
