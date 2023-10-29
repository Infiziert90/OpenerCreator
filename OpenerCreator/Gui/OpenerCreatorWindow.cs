using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Internal;
using Dalamud.Utility;
using ImGuiNET;
using OpenerCreator.Helpers;
using OpenerCreator.Managers;

namespace OpenerCreator.Gui;

public class OpenerCreatorWindow : IDisposable
{
    public bool Enabled;
    private List<uint> actions;

    private Dictionary<uint, IDalamudTextureWrap> iconCache;
    private string search;
    private string name;
    private List<uint> filteredActions;

    private const int IconSize = 32;

    public OpenerCreatorWindow()
    {
        Enabled = false;
        actions = new();
        iconCache = new();
        search = "";
        name = "";
        filteredActions = ActionDictionary.Instance.NonRepeatedIdList();
    }

    public void Dispose()
    {
        foreach (var v in iconCache)
            v.Value.Dispose();
        GC.SuppressFinalize(this);

    }

    public void Draw()
    {
        if (!Enabled)
            return;

        ImGui.SetNextWindowSizeConstraints(new Vector2(100, 100), new Vector2(4000, 2000));
        ImGui.Begin("Opener Creator", ref Enabled);

        DrawActionsGui();

        ImGui.BeginTabBar("OpenerCreatorMainTabBar");
        DrawOpenerLoader();
        DrawAbilityFilter();
        ImGui.EndTabBar();
        ImGui.Spacing();
        ImGui.End();
    }

    public void DrawActionsGui()
    {
        var spacing = ImGui.GetStyle().ItemSpacing;
        var padding = ImGui.GetStyle().FramePadding;
        var icons_per_line = (int)Math.Floor((ImGui.GetContentRegionAvail().X - padding.X * 2.0 + spacing.X) / (IconSize + spacing.X));
        var lines = (float)Math.Max(Math.Ceiling(actions.Count / (float)icons_per_line), 1);
        ImGui.BeginChildFrame(2426787, new Vector2(ImGui.GetContentRegionAvail().X, lines * (IconSize + spacing.Y) - spacing.Y + padding.Y * 2), ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        int? delete = null;
        for (var i = 0; i < actions.Count; i++)
        {
            if (i > 0)
            {
                ImGui.SameLine();
                if (ImGui.GetContentRegionAvail().X < IconSize)
                    ImGui.NewLine();
            }

            ImGui.Image(GetIcon(actions[i]), new Vector2(IconSize, IconSize));
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(ActionDictionary.Instance.GetActionName(actions[i]));
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                delete = i;
        }

        if (delete != null)
            actions.RemoveAt(delete.Value);

        ImGui.Dummy(Vector2.Zero);
        ImGui.EndChildFrame();
    }

    public void DrawOpenerLoader()
    {
        if (!ImGui.BeginTabItem("Loader"))
            return;

        ImGui.BeginChild("loadopener");
        if (ImGui.Button("Clear"))
        {
            actions.Clear();
        }
        var defaultOpeners = OpenerManager.Instance.GetDefaultNames();
        foreach (var opener in defaultOpeners)
        {
            ImGui.Text(opener);
            ImGui.SameLine();
            if (ImGui.Button($"Load##{opener}"))
            {
                actions = OpenerManager.Instance.GetDefaultOpener(opener);
                OpenerManager.Instance.Loaded = actions;
                ChatMessages.OpenerLoaded();
            }
        }
        ImGui.EndChild();
        ImGui.EndTabItem();
    }

    private void DrawAbilityFilter()
    {
        if (!ImGui.BeginTabItem("Creator"))
            return;

        ImGui.BeginChild("allactions");
        if (ImGui.InputText("Search", ref search, 64))
        {
            if (search.Length > 0)
                filteredActions = ActionDictionary.Instance.GetNonRepeatedActionsByName(search);
            else
                filteredActions = ActionDictionary.Instance.NonRepeatedIdList();
        }

        ImGui.Text($"{filteredActions.Count} Results");
        ImGui.SameLine();
        if (ImGui.Button("Lock opener"))
        {
            OpenerManager.Instance.Loaded = actions;
            ChatMessages.OpenerLoaded();
        }
        ImGui.SameLine();
        if (ImGui.Button("Save") && !name.IsNullOrEmpty())
        {
            OpenerManager.Instance.AddOpener(name, actions);
            OpenerManager.Instance.SaveOpeners();
            ChatMessages.OpenerSaved();
        }
        ImGui.SameLine();
        ImGui.InputText("Opener name", ref name, 32);


        for (var i = 0; i < Math.Min(20, filteredActions.Count); i++) // at max 5
        {
            var action = ActionDictionary.Instance.GetAction(filteredActions[i]);
            ImGui.Image(GetIcon(filteredActions[i]), new Vector2(IconSize, IconSize));
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                actions.Add(filteredActions[i]);

            ImGui.SameLine();
            ImGui.Text(action.Name.ToString());
        }

        ImGui.EndChild();
        ImGui.EndTabItem();
    }

    private nint GetIcon(uint id)
    {
        if (!iconCache.ContainsKey(id))
            iconCache[id] = ActionDictionary.Instance.GetIconTexture(id);
        return iconCache[id].ImGuiHandle;
    }
}
