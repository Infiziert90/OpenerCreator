using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using OpenerCreator.Hooks;
using OpenerCreator.Windows;
using OpenerCreator.Windows.Config;

namespace OpenerCreator;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static IPluginLog PluginLog { get; private set; } = null!;

    private const string Command = "/ocrt";

    public readonly WindowSystem WindowSystem = new("OpenerCreator");

    private ConfigWindow ConfigWindow { get; init; }
    private OpenerCreatorWindow OpenerCreatorWindow { get; init; }
    private UsedActionHook UsedActionHook { get; init; } = new();
    private AbilityAntsHook AbilityAntsHook { get; init; } = new();
    public static Configuration Config { get; set; } = null!;

    public Plugin()
    {
        Config = Configuration.Load();

        ConfigWindow = new ConfigWindow();
        OpenerCreatorWindow = new OpenerCreatorWindow(this, UsedActionHook.StartRecording, UsedActionHook.StopRecording,
                                                      AbilityAntsHook.Enable, AbilityAntsHook.Disable,
                                                      a => AbilityAntsHook.CurrentAction = a);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(OpenerCreatorWindow);

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi += OpenMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;

        CommandManager.AddHandler(Command, new CommandInfo(OnCommand)
        {
            HelpMessage = "Create, save, and practice your openers."
        });
    }
    public void Dispose()
    {
        CommandManager.RemoveHandler(Command);
        UsedActionHook.Dispose();
        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
        OpenerCreatorWindow.Dispose();
        AbilityAntsHook.Dispose();

        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenMainUi -= OpenMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
    }

    private void OnCommand(string command, string args)
    {
        if (args == "config")
            ConfigWindow.Toggle();
        else
            OpenerCreatorWindow.Toggle();
    }

    public void OpenConfigUi() => ConfigWindow.Toggle();
    public void OpenMainUi() => OpenerCreatorWindow.Toggle();
}
