using System;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace OpenerCreator.Windows.Config;

public partial class ConfigWindow
{
    private void Settings()
    {
        using var tabItem = ImRaii.TabItem("Settings");
        if (!tabItem.Success)
            return;

        var changed = false;

        if (ImGui.CollapsingHeader("Countdown", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ImGui.Checkbox("Enable countdown", ref Plugin.Config.IsCountdownEnabled);
            if (ImGui.InputInt("Countdown timer", ref Plugin.Config.CountdownTime))
            {
                Plugin.Config.CountdownTime = Math.Clamp(Plugin.Config.CountdownTime, 0, 30);
                changed = true;
            }
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Action Recording", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ImGui.Checkbox("Stop recording at first mistake", ref Plugin.Config.StopAtFirstMistake);
            changed |= ImGui.Checkbox("Ignore True North if it isn't present on the opener.", ref Plugin.Config.IgnoreTrueNorth);
            changed |= ImGui.Checkbox("Use ability ants for next opener action.", ref Plugin.Config.AbilityAnts);
        }

        if (changed)
            Plugin.Config.Save();
    }
}
