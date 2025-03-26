using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OpenerCreator.Actions;
using OpenerCreator.Helpers;

namespace OpenerCreator.Managers;

public class OpenerManager(IActionManager actions)
{
    private static OpenerManager? SingletonInstance;
    private static readonly object LockObject = new();
    private readonly Dictionary<Jobs, Dictionary<string, List<int>>> DefaultOpeners = new();
    private readonly Dictionary<Jobs, Dictionary<string, List<int>>> Openers = new();

    private OpenerManager(IActionManager actions, ValueTuple _) : this(actions)
    {
        OpenersFile = Path.Combine(Plugin.PluginInterface.ConfigDirectory.FullName, "openers.json");
        Openers = LoadOpeners(OpenersFile);
        DefaultOpeners = LoadOpeners(Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory!.FullName,
                                                  "openers.json"));
    }

    public List<int> Loaded { get; set; } = [];
    private string OpenersFile { get; init; } = "empty";

    public static OpenerManager Instance
    {
        get
        {
            if (SingletonInstance == null)
            {
                lock (LockObject)
                {
                    SingletonInstance ??= new OpenerManager(PvEActions.Instance, new ValueTuple());
                }
            }

            return SingletonInstance;
        }
    }

    public void AddOpener(string name, Jobs job, IEnumerable<int> opener)
    {
        if (!Openers.TryGetValue(job, out var value))
        {
            value = new Dictionary<string, List<int>>();
            Openers[job] = value;
        }

        value[name] = [..opener];
    }

    public List<Tuple<Jobs, List<string>>> GetDefaultNames()
    {
        return DefaultOpeners.Select(x => Tuple.Create(x.Key, x.Value.Keys.ToList())).ToList();
    }

    public List<int> GetDefaultOpener(string name, Jobs job)
    {
        return [..DefaultOpeners[job][name]];
    }

    public List<int> GetOpener(string name, Jobs job)
    {
        return [..Openers[job][name]];
    }

    public List<Tuple<Jobs, List<string>>> GetNames()
    {
        return Openers.Select(x => Tuple.Create(x.Key, x.Value.Keys.ToList())).ToList();
    }

    public void DeleteOpener(string name, Jobs job)
    {
        if (Openers.TryGetValue(job, out var value))
        {
            value.Remove(name);
            if (value.Count == 0) Openers.Remove(job);
        }
    }

    public void Compare(List<int> used, Action<Feedback> provideFeedback, Action<int> wrongAction)
    {
        var feedback = new Feedback();
        used = used.Take(Loaded.Count).ToList();

        if (Loaded.SequenceEqual(used))
        {
            feedback.AddMessage(Feedback.MessageType.Success, "Great job! Opener executed perfectly.");
            provideFeedback(feedback);
            return;
        }

        var error = false;
        var size = Math.Min(Loaded.Count, used.Count);
        var shift = 0;

        for (var i = 0; i + shift < size; i++)
        {
            var openerIndex = i + shift;

            if (!AreActionsEqual(used, openerIndex, i, out var intended, out var actual))
            {
                error = true;
                feedback.AddMessage(Feedback.MessageType.Error,
                                    $"Difference in action {i + 1}: Substituted {intended} for {actions.GetActionName(actual)}");
                wrongAction(openerIndex);

                if (ShouldShift(openerIndex, size, used[i])) shift++;
            }
        }

        if (!error && shift == 0)
            feedback.AddMessage(Feedback.MessageType.Success, "Great job! Opener executed perfectly.");

        if (shift != 0)
        {
            feedback.AddMessage(Feedback.MessageType.Info,
                                $"You shifted your opener by {shift} {(shift == 1 ? "action" : "actions")}.");
        }

        provideFeedback(feedback);
    }

    private bool AreActionsEqual(
        IReadOnlyList<int> used, int openerIndex, int usedIndex, out string intended, out int actualId)
    {
        var intendedId = Loaded[openerIndex];
        intended = actions.GetActionName(intendedId);
        actualId = used[usedIndex];

        return AreActionsEqual(intendedId, intended, (uint)actualId);
    }

    public bool AreActionsEqual(int intendedId, string intendedName, uint actualId)
    {
        if (intendedId < 0)
            return GroupOfActions.DefaultGroups.First(g => g.HasId(intendedId)).IsMember(actualId);
        return intendedId == actualId ||
               intendedId == IActionManager.CatchAllActionId ||
               actions.SameActionsByName(intendedName, (int) actualId);
    }

    private bool ShouldShift(int openerIndex, int size, int usedValue)
    {
        var nextIntended = actions.GetActionName(Loaded[openerIndex]);
        return openerIndex + 1 < size &&
               (Loaded[openerIndex + 1] == usedValue || actions.SameActionsByName(nextIntended, usedValue));
    }

    private static Dictionary<Jobs, Dictionary<string, List<int>>> LoadOpeners(string path)
    {
        try
        {
            if (!File.Exists(path))
                return new Dictionary<Jobs, Dictionary<string, List<int>>>();

            var jsonData = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<Jobs, Dictionary<string, List<int>>>>(jsonData)!;
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Failed to load Openers");
            return new Dictionary<Jobs, Dictionary<string, List<int>>>();
        }
    }

    public void SaveOpeners()
    {
        try
        {
            var jsonData = JsonSerializer.Serialize(Openers);
            File.WriteAllText(OpenersFile, jsonData);
        }
        catch (Exception ex)
        {
            Plugin.PluginLog.Error(ex, "Failed to save Openers");
        }
    }
}
