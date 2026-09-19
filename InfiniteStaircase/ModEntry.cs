using System;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.GameData.Tools;
using StardewValley.Locations;
using SObject = StardewValley.Object;

namespace InfiniteStaircase
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        /***********
        ** Fields
        ***********/
        /// <summary>The unqualified item ID for the crafted placeholder object. This exists only because a
        /// crafting recipe can't directly output a tool, so the recipe produces this object and
        /// <see cref="OnInventoryChanged"/> immediately swaps it for the real tool.</summary>
        private const string PlaceholderId = "Ryman.InfiniteStaircase";

        /// <summary>The qualified item ID for <see cref="PlaceholderId"/>.</summary>
        private const string PlaceholderQualifiedId = "(O)" + PlaceholderId;

        /// <summary>The unqualified item ID for the real Infinite Staircase tool.</summary>
        private const string ToolId = "Ryman.InfiniteStaircaseTool";

        /// <summary>The qualified item ID for <see cref="ToolId"/>.</summary>
        private const string ToolQualifiedId = "(T)" + ToolId;

        /// <summary>The vanilla Staircase's qualified item ID (a big craftable), used as the crafting ingredient.</summary>
        private const string VanillaStaircaseQualifiedId = "(BC)71";

        /***********
        ** Public methods
        ***********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            helper.Events.Player.InventoryChanged += this.OnInventoryChanged;
            helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
        }

        /***********
        ** Private methods
        ***********/
        /// <summary>Raised when the game is about to load an asset, letting us add or edit game data.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // add the placeholder object shown/produced by the crafting recipe (see PlaceholderId)
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;
                    data[PlaceholderId] = new ObjectData
                    {
                        Name = PlaceholderId,
                        DisplayName = "Infinite Staircase",
                        Description = "Instantly creates a ladder down in the mines. Never runs out.",
                        Type = "Crafting",
                        Category = SObject.CraftingCategory,
                        Price = 500,
                        Texture = this.Helper.ModContent.GetInternalAssetName("assets/infinitestaircase.png").BaseName,
                        SpriteIndex = 0
                    };
                });
            }

            // add the real tool, using the same sprite as the placeholder
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Tools"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ToolData>().Data;
                    data[ToolId] = new ToolData
                    {
                        ClassName = "GenericTool",
                        Name = ToolId,
                        DisplayName = "Infinite Staircase",
                        Description = "Instantly creates a ladder down in the mines. Never runs out.",
                        Texture = this.Helper.ModContent.GetInternalAssetName("assets/infinitestaircase.png").BaseName,
                        SpriteIndex = 0,
                        MenuSpriteIndex = 0,
                        CanBeLostOnDeath = false
                    };
                });
            }

            // add the crafting recipe: 10 Staircases + Mining level 5 (produces the placeholder object)
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, string>().Data;
                    data[PlaceholderId] = $"{VanillaStaircaseQualifiedId} 10/Home/{PlaceholderId} 1/false/Mining 5/";
                });
            }
        }

        /// <summary>Raised after items are added to or removed from a player's inventory. Used to swap the
        /// crafted placeholder object for the real tool, since a crafting recipe can't output a tool directly.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
        {
            foreach (Item added in e.Added)
            {
                if (added.QualifiedItemId == PlaceholderQualifiedId)
                {
                    e.Player.Items.Remove(added);
                    e.Player.addItemToInventoryBool(ItemRegistry.Create(ToolQualifiedId));
                }
            }
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet, or isn't free to act
            if (!Context.IsWorldReady || !Context.IsPlayerFree)
                return;

            // this is a tool, so only the "use tool" button does anything - it never responds to the action
            // button, exactly like any other tool
            if (!e.Button.IsUseToolButton())
                return;

            // only act if the player is holding the Infinite Staircase tool
            if (Game1.player.CurrentTool?.QualifiedItemId != ToolQualifiedId)
                return;

            // suppress the button so the game's own generic tool-swing animation never plays - since GenericTool
            // has no swing sprites of its own, that animation just shows a broken/empty texture. We handle
            // everything ourselves below, so the default tool-use pipeline is never needed.
            this.Helper.Input.Suppress(e.Button);

            // tools have no default effect outside their intended context, so simply do nothing if we're not
            // in the mines - matching how e.g. a Pickaxe harmlessly does nothing useful outside its own context
            if (Game1.currentLocation is not MineShaft mine)
                return;

            // resolve the targeted tile - see GetTargetTile() for why, and this must match OnRenderedWorld
            // exactly so the preview never disagrees with where the ladder actually gets placed
            Vector2 tile = GetTargetTile();

            if (mine.shouldCreateLadderOnThisLevel() && IsValidLadderTile(mine, tile))
            {
                mine.createLadderAt(tile, "stairsdown");
            }
            else
            {
                Game1.showRedMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:Object.cs.13053"));
            }
        }

        /// <summary>Get whether a ladder can be dug at the given tile - it must be clear, walkable ground with
        /// nothing already there, and the floor must be solid stone.</summary>
        /// <param name="mine">The mine to check.</param>
        /// <param name="tile">The tile to check.</param>
        private static bool IsValidLadderTile(MineShaft mine, Vector2 tile)
        {
            if (mine.IsTileOccupiedBy(tile) || !mine.isTileOnClearAndSolidGround(tile))
                return false;

            // a ladder can only be dug through solid stone (e.g. dirt patches or wood bridge tiles are clear
            // ground, but not diggable)
            string? backTileType = mine.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Type", "Back");
            return "Stone".Equals(backTileType);
        }

        /// <summary>Get the tile the player is currently targeting for placement. Mirrors the same device
        /// detection the game itself uses for placing things (e.g. chests, fences, seeds) - <see
        /// cref="Game1.IsPerformingMousePlacement"/> - but, unlike the game's own <see
        /// cref="Game1.GetPlacementGrabTile"/>, never falls back to <see cref="Character.GetGrabTile"/> for
        /// controller/keyboard input: that method is a loose "interact range" hit-test which, depending on the
        /// player's exact sub-tile pixel position, can resolve to the player's own tile instead of the tile
        /// they're facing. We need the latter guaranteed, so we compute it ourselves as the player's tile plus
        /// a one-tile offset in their facing direction.</summary>
        private static Vector2 GetTargetTile()
        {
            if (Game1.IsPerformingMousePlacement())
                return Game1.GetPlacementGrabTile();

            return Game1.player.Tile + GetFacingDirectionOffset();
        }

        /// <summary>Get a one-tile offset in the direction the player is currently facing.</summary>
        private static Vector2 GetFacingDirectionOffset()
        {
            return Game1.player.FacingDirection switch
            {
                0 => new Vector2(0, -1), // up
                1 => new Vector2(1, 0),  // right
                2 => new Vector2(0, 1),  // down
                3 => new Vector2(-1, 0), // left
                _ => Vector2.Zero
            };
        }

        /// <summary>Raised after the game draws to the world, letting us draw over it. Used to show a green/red
        /// tile preview - like the vanilla placement preview - for where the ladder would be created.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (Game1.player.CurrentTool?.QualifiedItemId != ToolQualifiedId)
                return;

            if (Game1.currentLocation is not MineShaft mine)
                return;

            // resolve the targeted tile - see GetTargetTile() for why, and this must match OnButtonPressed
            // exactly so the preview never disagrees with where the ladder actually gets placed
            Vector2 tile = GetTargetTile();

            bool valid = mine.shouldCreateLadderOnThisLevel() && IsValidLadderTile(mine, tile);
            Color color = (valid ? Color.Green : Color.Red) * 0.5f;

            Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, tile * 64f);
            e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle((int)screenPos.X, (int)screenPos.Y, 64, 64), color);
        }
    }
}
