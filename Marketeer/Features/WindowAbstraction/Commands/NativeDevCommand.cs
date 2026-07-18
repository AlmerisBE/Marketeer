using Dalamud.Plugin.Services;
using Marketeer.Features.Command.Contracts;
using Marketeer.Features.Logging.Contracts;
using Marketeer.Features.WindowAbstraction.Contracts;
using System;
using System.Linq;

namespace Marketeer.Features.WindowAbstraction.Commands;

public class NativeDevCommand : ICommand {
    private INativeWindowService nativeWindowService;
    private IChatGui chatGui;
    private ILoggerService logger;

    public string CommandTrigger => "native";
    public string Description => "Native UI developer tools. Usage: /marketeer native <close [ID] | dump | elements>";

    public NativeDevCommand(INativeWindowService nativeWindowService, IChatGui chatGui, ILoggerService logger) {
        this.nativeWindowService = nativeWindowService;
        this.chatGui = chatGui;
        this.logger = logger;
    }

    public void Execute(string arguments) {
        var args = arguments.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var action = args.Length > 0 ? args[0].ToLowerInvariant() : string.Empty;

        if (action == "close" && args.Length == 2) {
            this.HandleClose(args[1]);
        }
        else if (action == "dump") {
            this.HandleDump();
        }
        else if (action == "elements") {
            this.HandleElements();
        }
        else {
            this.chatGui.Print("Usage: /marketeer native <close [WindowID] | dump | elements>");
        }
    }

    private void HandleClose(string windowId) {
        var window = this.nativeWindowService.GetWindow(windowId);

        if (window == null || !window.IsVisible) {
            this.chatGui.PrintError($"[Marketeer] Native window '{windowId}' is not open or cannot be found.");
            return;
        }

        window.Close();
        this.logger.Info($"Native window '{windowId}' closed via dev command.");
        this.chatGui.Print($"[Marketeer] Closed native window: {windowId}");
    }

    private void HandleDump() {
        var openWindows = this.nativeWindowService.GetOpenWindows().ToList();

        this.logger.Info("--- Active Native Windows Dump ---");
        foreach (var win in openWindows) {
            this.logger.Info($"- {win.Name} (Type: {win.Type})");
        }
        this.logger.Info("----------------------------------");

        this.chatGui.Print($"[Marketeer] Dumped {openWindows.Count} visible windows to the Dalamud log.");
    }

    private void HandleElements() {
        var focusedWindow = this.nativeWindowService.GetFocusedWindow();

        if (focusedWindow == null) {
            this.chatGui.PrintError("[Marketeer] No focused native window detected.");
            this.logger.Warning("Attempted to dump elements, but no focused window was found.");
            return;
        }

        var elements = focusedWindow.GetElements().ToList();

        this.logger.Info($"--- UI Elements for '{focusedWindow.Name}' ---");
        foreach (var element in elements) {
            this.logger.Info($"[{element.Type}] NodeID: {element.NodeId} | Text: \"{element.Text}\"");
        }
        this.logger.Info("---------------------------------------------");

        this.chatGui.Print($"[Marketeer] Dumped {elements.Count} elements from '{focusedWindow.Name}' to /xllog.");
    }
}