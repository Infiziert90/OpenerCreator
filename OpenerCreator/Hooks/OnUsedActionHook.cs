using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Lumina.Excel;
using OpenerCreator.Actions;
using OpenerCreator.Helpers;
using OpenerCreator.Managers;
using Action = Lumina.Excel.Sheets.Action;


namespace OpenerCreator.Hooks;

public class UsedActionHook : IDisposable
{
    private const int MaxItemCount = 50;

    private readonly ExcelSheet<Action>? sheet = Plugin.DataManager.GetExcelSheet<Action>();
    private readonly List<int> used = new(MaxItemCount);
    private readonly Hook<ActionEffectHandler.Delegates.Receive>? usedActionHook;
    private Action<int> currentIndex = _ => { };
    private bool ignoreTrueNorth;

    private int nActions;
    private Action<Feedback> provideFeedback = _ => { };
    private Action<int> updateAbilityAnts = _ => { };
    private Action<int> wrongAction = _ => { };


    public unsafe UsedActionHook()
    {
        usedActionHook = Plugin.GameInteropProvider.HookFromAddress<ActionEffectHandler.Delegates.Receive>(ActionEffectHandler.MemberFunctionPointers.Receive, DetourUsedAction);
    }

    public void Dispose()
    {
        usedActionHook?.Disable();
        usedActionHook?.Dispose();
        GC.SuppressFinalize(this);
    }

    public void StartRecording(
        int cd, Action<Feedback> provideFeedbackA, Action<int> wrongActionA, Action<int> currentIndexA, bool ignoreTn,
        Action<int> updateAbilityAntsA)
    {
        if (usedActionHook?.IsEnabled ?? true)
            return;

        provideFeedback = provideFeedbackA;
        wrongAction = wrongActionA;
        currentIndex = currentIndexA;
        usedActionHook?.Enable();
        nActions = OpenerManager.Instance.Loaded.Count;
        ignoreTrueNorth = ignoreTn;
        updateAbilityAnts = updateAbilityAntsA;
    }

    public void StopRecording()
    {
        if (!(usedActionHook?.IsEnabled ?? false))
            return;

        usedActionHook?.Disable();
        nActions = 0;
        used.Clear();
    }

    private void Compare()
    {
        if (!(usedActionHook?.IsEnabled ?? false))
            return;

        usedActionHook?.Disable();
        nActions = 0;
        OpenerManager.Instance.Compare(used, provideFeedback, wrongAction);
        used.Clear();
    }

    private unsafe void DetourUsedAction(uint casterEntityId, Character* casterPtr, Vector3* targetPos, ActionEffectHandler.Header* header, ActionEffectHandler.TargetEffects* effects, GameObjectId* targetEntityIds)
    {
        usedActionHook?.Original(casterEntityId, casterPtr, targetPos, header, effects, targetEntityIds);

        var player = Plugin.ClientState.LocalPlayer;
        if (player == null || casterEntityId != player.EntityId) return;

        var actionId = header->ActionId;
        var ok = sheet!.TryGetRow(actionId, out var actionRow);
        var isActionTrueNorth = actionId == PvEActions.TrueNorthId;
        var analyseWhenTrueNorth = !(ignoreTrueNorth && isActionTrueNorth); //nand
        if (ok && PvEActions.IsPvEAction(actionRow) && analyseWhenTrueNorth)
        {
            if (nActions == 0) // Opener not defined or fully processed
            {
                StopRecording();
                return;
            }

            // Leave early
            var loadedLength = OpenerManager.Instance.Loaded.Count;
            var index = loadedLength - nActions;
            var intendedAction = OpenerManager.Instance.Loaded[index];
            if (index + 1 < OpenerManager.Instance.Loaded.Count && Plugin.Config.AbilityAnts)
                updateAbilityAnts(OpenerManager.Instance.Loaded[index + 1]);
            var intendedName = PvEActions.Instance.GetActionName(intendedAction);

            currentIndex(index);

            if (Plugin.Config.StopAtFirstMistake &&
                !OpenerManager.Instance.AreActionsEqual(intendedAction, intendedName, actionId)
               )
            {
                wrongAction(index);
                var f = new Feedback();
                f.AddMessage(
                    Feedback.MessageType.Error,
                    $"Difference in action {index + 1}: Substituted {intendedName} for {PvEActions.Instance.GetActionName((int)actionId)}"
                );
                provideFeedback(f);
                StopRecording();
                return;
            }

            // Process the opener
            used.Add((int)actionId);
            nActions--;
            if (nActions <= 0) Compare();
        }
    }
}
