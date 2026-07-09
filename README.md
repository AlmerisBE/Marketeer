# BasePlugin - Dalamud Plugin Template

[![Build and Release Plugin](https://github.com/AlmerisBE/BasePlugin/actions/workflows/release.yml/badge.svg)](https://github.com/AlmerisBE/BasePlugin/actions/workflows/release.yml)

A robust, enterprise-grade template for building [Dalamud](https://github.com/goatcorp/Dalamud) plugins for Final Fantasy XIV. 

This template is built upon the **Vertical Slice Architecture** pattern, utilizing **SOLID principles**, robust **Dependency Injection** (via `Microsoft.Extensions.DependencyInjection`), and is fully set up for **Test-Driven Development (TDD)** using xUnit and NSubstitute.

---

## 🌟 Core Features Included

This base plugin provides a solid foundation so you can immediately focus on your business logic instead of boilerplate code:

*   **🧩 Feature Module Architecture**: Automatically discovers and registers services, commands, and UI windows using reflection (`IFeatureModule`). No more bloated `Plugin.cs`.
*   **💉 Dependency Injection**: Native support for constructor injection for all your services and Dalamud APIs.
*   **💬 Command Dispatcher**: A routing system that maps chat commands (e.g., `/myplugin config`) to specific `ICommand` classes.
*   **🌐 Modular Localization**: A JSON-based embedded localization system (`en`, `fr`, `de`, `ja`) with automatic fallback mechanisms.
*   **⚙️ Configuration & UI**: Pre-configured ImGui window (`WindowSystem` via `Dalamud.Bindings.ImGui`) linked to the Dalamud `IPluginConfiguration` save states.
*   **📜 Unified Logging**: An abstraction over `IPluginLog` (`ILoggerService`) to ensure consistent logging across your application and easy mocking in unit tests.
*   **🧪 TDD Ready**: A pre-configured `xUnit` test project demonstrating how to mock Dalamud interfaces and test your services.

---

## 🚀 Getting Started

To create a new plugin from this template:

### 1. Create your repository
Click the green **Use this template** button at the top of this repository on GitHub to create your own copy.

### 2. Rename the project
Once cloned to your local machine, you need to replace the `BasePlugin` placeholder with your actual plugin name.

1. Rename the folders: `BasePlugin` and `BasePlugin.Tests`.
2. Rename the `.sln` and `.csproj` files.
3. Open the solution in Visual Studio and perform a global **Find and Replace** (Ctrl+Shift+F):
   * Find: `BasePlugin`
   * Replace: `YourPluginName`
4. Update the `BasePlugin.json` file with your plugin's metadata (Author, Description, etc.).

### 3. Build & Run
Build the solution in `Debug | x64`. 
Load the resulting folder into Dalamud via the `/xlsettings` > **Experimental** > **Dev Plugin Locations** menu.

---

## 🏗️ Architecture Guide: How to add a new Feature

Instead of organizing files by technical type (e.g., all interfaces in one folder, all models in another), this template groups code by **Feature** (Vertical Slicing).

To add a new feature (e.g., `AutoLoot`):

### 1. Create the Feature Folder
Create a new directory: `Features/AutoLoot/`. Inside, you can have subfolders like `Services/`, `Commands/`, `UI/`, and `Contracts/`.

### 2. Define the Feature Module
Create a registration class that implements `IFeatureModule`. The core system will automatically detect this and register your services on startup.

```csharp
using Microsoft.Extensions.DependencyInjection;
using BasePlugin.Core;
using BasePlugin.Features.AutoLoot.Contracts;
using BasePlugin.Features.AutoLoot.Services;
using BasePlugin.Features.AutoLoot.Commands;
using BasePlugin.Features.Command.Contracts;

namespace BasePlugin.Features.AutoLoot;

public class AutoLootFeature : IFeatureModule {
    public void RegisterServices(IServiceCollection services) {
        services.AddSingleton<IAutoLootService, AutoLootService>();
        services.AddSingleton<ICommand, ToggleAutoLootCommand>();
    }
}
```

### 3. Add a Command
Implement the `ICommand` interface. The `CommandDispatcher` will route user input directly to your `Execute` method.

```csharp
using BasePlugin.Features.Command.Contracts;

namespace BasePlugin.Features.AutoLoot.Commands;

public class ToggleAutoLootCommand : ICommand {
    public string CommandTrigger => "autoloot";
    public string Description => "Toggles the auto-loot feature.";

    public void Execute(string arguments) {
        // Your command logic here
    }
}
```

### 4. Add Localized Text
Create a `Resources/` folder in your feature directory and add an `en.json` file. Ensure it is marked as an **Embedded Resource** by MSBuild (already configured in the `.csproj`).

```json
{
  "AutoLoot_Enabled": "Auto-Loot is now enabled!"
}
```

Create a provider to feed this JSON into the global translation engine:

```csharp
using BasePlugin.Features.Localization.Providers;

namespace BasePlugin.Features.AutoLoot.Providers;

public class AutoLootLocalizationProvider : JsonLocalizationProvider {
    protected override string ResourceBasePath => "BasePlugin.Features.AutoLoot.Resources";
}
```

*(Don't forget to register this provider in your `AutoLootFeature`!)*

---

## 🤝 Contributing & License

This template is provided as-is to help the Final Fantasy XIV community build better, more maintainable plugins. Feel free to fork, improve, and submit pull requests.