using System;
using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;

namespace OpenerCreator.Windows;

public static class Helper
{
    internal static void CollapsingHeader(string label, Action action)
    {
        if (ImGui.CollapsingHeader(label, ImGuiTreeNodeFlags.DefaultOpen)) action();
    }

    internal static void Tooltip(string tooltip)
    {
        using (ImRaii.Tooltip())
        using (ImRaii.TextWrapPos(ImGui.GetFontSize() * 35.0f))
        {
            ImGui.TextUnformatted(tooltip);
        }
    }

    public struct EndUnconditionally(Action endAction, bool success) : ImRaii.IEndObject
    {
        public bool Success { get; } = success;

        private bool Disposed { get; set; } = false;
        private Action EndAction { get; } = endAction;

        public void Dispose()
        {
            if (!Disposed)
            {
                EndAction();
                Disposed = true;
            }
        }
    }

    public static ImRaii.IEndObject ChildFrame(uint id, Vector2 size, ImGuiWindowFlags flags)
    {
        var success = ImGui.BeginChildFrame(id, size, flags);
        return new EndUnconditionally(ImGui.EndChildFrame, success);
    }
}
