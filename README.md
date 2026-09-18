# Infinite Staircase

A [SMAPI](https://smapi.io/) mod for Stardew Valley that adds a craftable **Infinite Staircase** tool — equip it and use it to instantly create a ladder down in the mines, just like a regular Staircase, except it's never consumed. Craft one and you're set for good.

## Features

- Adds a new tool, the **Infinite Staircase** - equip it from your inventory just like a Pickaxe or Axe.
- Crafting recipe unlocks at **Mining level 5**.
- Costs **10 regular Staircases** to craft.
- Use it (left-click, or the equivalent controller button) while in the mines to create a ladder down to the next level on the tile in front of you - but it's never consumed, so one is all you'll ever need.
- Behaves exactly like any other tool everywhere else: it does nothing outside the mines, and never interferes with other interactions like mailboxes, doors, NPCs, or existing ladders.

## Installation (players)

1. Install [SMAPI](https://smapi.io/) (4.0.0 or later).
2. Download the latest release and extract the `InfiniteStaircase` folder into your `Stardew Valley/Mods` folder.
3. Launch the game through SMAPI.

## Requirements

- SMAPI 4.0.0+
- Stardew Valley 1.6+
- No other mods required

## Building from source

This is a standard SMAPI mod project using [Pathoschild's mod build package](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig), which auto-detects your game install and deploys the built mod straight into your `Mods` folder.

```
cd InfiniteStaircase
dotnet build
```

Requires the .NET SDK compatible with `net6.0` and a local Stardew Valley install. If the build can't find your game, see the [mod build package docs](https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/mod-package.md) for how to set the `GamePath` property manually.

## Project structure

```
InfiniteStaircase/
  ModEntry.cs         # mod entry point - tool/recipe data and use-tool logic
  manifest.json        # mod metadata
  assets/               # sprite(s) used by the mod
```

## Credits

- Built with [SMAPI](https://smapi.io/) by Pathoschild.
- Reference material from the [Stardew Valley Wiki modding docs](https://stardewvalleywiki.com/Modding:Index).

## License

No license has been chosen yet for this project.
