using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using OpenerCreator.Actions;
using OpenerCreator.Helpers;
using OpenerCreator.Managers;


namespace OpenerCreator.Hooks;

public class UsedActionHook : IDisposable
{
    private const int MaxItemCount = 50;

    private readonly List<int> Used = new(MaxItemCount);
    private readonly Hook<ActionEffectHandler.Delegates.Receive>? UsedActionHooked;
    private Action<int> CurrentIndex = _ => { };
    private bool IgnoreTrueNorth;

    private int NActions;
    private Action<Feedback> ProvideFeedback = _ => { };
    private Action<int> UpdateAbilityAnts = _ => { };
    private Action<int> WrongAction = _ => { };


    public unsafe UsedActionHook()
    {
        UsedActionHooked = Plugin.GameInteropProvider.HookFromAddress<ActionEffectHandler.Delegates.Receive>(ActionEffectHandler.MemberFunctionPointers.Receive, DetourUsedAction);
    }

    public void Dispose()
    {
        UsedActionHooked?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void StartRecording(
        int cd, Action<Feedback> provideFeedbackA, Action<int> wrongActionA, Action<int> currentIndexA, bool ignoreTn,
        Action<int> updateAbilityAntsA)
    {
        if (UsedActionHooked?.IsEnabled ?? true)
            return;

        ProvideFeedback = provideFeedbackA;
        WrongAction = wrongActionA;
        CurrentIndex = currentIndexA;
        UsedActionHooked?.Enable();
        NActions = OpenerManager.Instance.Loaded.Count;
        IgnoreTrueNorth = ignoreTn;
        UpdateAbilityAnts = updateAbilityAntsA;
    }

    public void StopRecording()
    {
        if (!(UsedActionHooked?.IsEnabled ?? false))
            return;

        UsedActionHooked?.Disable();
        NActions = 0;
        Used.Clear();
    }

    private void Compare()
    {
        if (!(UsedActionHooked?.IsEnabled ?? false))
            return;

        UsedActionHooked?.Disable();
        NActions = 0;
        OpenerManager.Instance.Compare(Used, ProvideFeedback, WrongAction);
        Used.Clear();
    }

    private unsafe void DetourUsedAction(uint casterEntityId, Character* casterPtr, Vector3* targetPos, ActionEffectHandler.Header* header, ActionEffectHandler.TargetEffects* effects, GameObjectId* targetEntityIds)
    {
        UsedActionHooked?.Original(casterEntityId, casterPtr, targetPos, header, effects, targetEntityIds);

        var player = Plugin.ClientState.LocalPlayer;
        if (player == null || casterEntityId != player.EntityId) return;

        var actionId = header->ActionId;
        if (!Sheets.ActionSheet.TryGetRow(actionId, out var actionRow))
            return;

        var isActionTrueNorth = actionId == PvEActions.TrueNorthId;
        var analyseWhenTrueNorth = !(IgnoreTrueNorth && isActionTrueNorth); //nand
        if (PvEActions.IsPvEAction(actionRow) && analyseWhenTrueNorth)
        {
            if (NActions == 0) // Opener not defined or fully processed
            {
                StopRecording();
                return;
            }

            // Leave early
            var loadedLength = OpenerManager.Instance.Loaded.Count;
            var index = loadedLength - NActions;
            var intendedAction = OpenerManager.Instance.Loaded[index];
            if (index + 1 < OpenerManager.Instance.Loaded.Count && Plugin.Config.AbilityAnts)
                UpdateAbilityAnts(OpenerManager.Instance.Loaded[index + 1]);

            var intendedName = PvEActions.Instance.GetActionName(intendedAction);
            CurrentIndex(index);

            if (Plugin.Config.StopAtFirstMistake && !OpenerManager.Instance.AreActionsEqual(intendedAction, intendedName, actionId))
            {
                WrongAction(index);
                var f = new Feedback();
                f.AddMessage(
                    Feedback.MessageType.Error,
                    $"Difference in action {index + 1}: Substituted {intendedName} for {PvEActions.Instance.GetActionName((int)actionId)}"
                );
                ProvideFeedback(f);
                StopRecording();
                return;
            }

            // Process the opener
            Used.Add((int)actionId);
            NActions--;
            if (NActions <= 0)
                Compare();
        }
    }
}
