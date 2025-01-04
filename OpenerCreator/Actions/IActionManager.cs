using Dalamud.Interface.Textures;

namespace OpenerCreator.Actions;

public interface IActionManager
{
    static uint CatchAllActionId => 0;
    static string CatchAllActionName => "Catch-All Action";
    static string OldActionName => "Old Action";

    static ISharedImmediateTexture GetUnknownActionTexture =>
        Plugin.TextureProvider.GetFromGame("ui/icon/000000/000786_hr1.tex");

    static ushort GetCatchAllIcon => 405;

    string GetActionName(int action);
    bool SameActionsByName(string action1, int action2);
}
