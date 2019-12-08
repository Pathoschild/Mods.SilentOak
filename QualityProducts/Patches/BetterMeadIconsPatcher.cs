using System;
using Harmony;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using SilentOak.QualityProducts.Utils;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace SilentOak.QualityProducts.Patches
{
    /// <summary>Handles patches to implement better mead icons.</summary>
    /// <remarks>Inspired by https://github.com/danvolchek/StardewMods/blob/32046f848ea1a1aade495b9adad612f8b94928d1/BetterArtisanGoodIcons/Patches .</remarks>
    internal static class BetterMeadIconsPatcher
    {
        /*********
        ** Fields
        *********/
        /// <summary>The mead icon texture.</summary>
        private static Texture2D MeadIcon;


        /*********
        ** Public methods
        *********/
        /// <summary>Initialise the patcher.</summary>
        /// <param name="harmony">The mod's Harmony instance.</param>
        /// <param name="meadIcon">The mead icon texture.</param>
        public static void Init(HarmonyInstance harmony, Texture2D meadIcon)
        {
            BetterMeadIconsPatcher.MeadIcon = meadIcon;

            // furniture
            harmony.Patch(
                original: AccessTools.Method(typeof(Furniture), nameof(Furniture.draw), parameters: new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
                prefix: new HarmonyMethod(typeof(BetterMeadIconsPatcher), nameof(BetterMeadIconsPatcher.Furniture_Draw))
            );

            // object
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), nameof(SObject.draw), parameters: new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float) }),
                prefix: new HarmonyMethod(typeof(BetterMeadIconsPatcher), nameof(BetterMeadIconsPatcher.Object_Draw))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), nameof(SObject.draw), parameters: new[] { typeof(SpriteBatch), typeof(int), typeof(int), typeof(float), typeof(float) }),
                prefix: new HarmonyMethod(typeof(BetterMeadIconsPatcher), nameof(BetterMeadIconsPatcher.Object_Draw2))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), nameof(SObject.drawInMenu), parameters: new[] { typeof(SpriteBatch), typeof(Vector2), typeof(float), typeof(float), typeof(float), typeof(StackDrawType), typeof(Color), typeof(bool) }),
                prefix: new HarmonyMethod(typeof(BetterMeadIconsPatcher), nameof(BetterMeadIconsPatcher.Object_DrawInMenu))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(SObject), nameof(SObject.drawWhenHeld)),
                prefix: new HarmonyMethod(typeof(BetterMeadIconsPatcher), nameof(BetterMeadIconsPatcher.Object_DrawWhenHeld))
            );
        }

        /****
        ** Furniture
        ****/
        /// <summary>Patch for drawing a custom sprite (if available) for the furniture's held object.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="x">The x tile coordinate.</param>
        /// <param name="y">The y tile coordinate.</param>
        /// <param name="alpha">The draw transparency.</param>
        /// <returns>Returns true if the original method should be invoked, else false.</returns>
        public static bool Furniture_Draw(Furniture __instance, SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
        {
            if (!BetterMeadIconsPatcher.TryLoadSprite(__instance.heldObject.Value, out Texture2D texture, out Rectangle sourceRect))
                return true;

            Vector2 drawPosition = Util.Helper.Reflection.GetField<NetVector2>(__instance, "drawPosition").GetValue().Value;

            Rectangle rectangle;
            if (x == -1)
            {
                float layerDepth;

                if (__instance.furniture_type.Value != Furniture.rug)
                {
                    rectangle = __instance.boundingBox.Value;
                    layerDepth = (rectangle.Bottom - 8) / 10000f;
                }
                else
                    layerDepth = 0f;

                spriteBatch.Draw(
                    texture: Furniture.furnitureTexture,
                    position: Game1.GlobalToLocal(Game1.viewport, drawPosition),
                    sourceRectangle: __instance.sourceRect.Value,
                    color: Color.White * alpha,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale: 4f,
                    effects: __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    layerDepth: layerDepth
                );
            }
            else
            {
                float layerDepth2;

                if (__instance.furniture_type.Value != Furniture.rug)
                {
                    rectangle = __instance.boundingBox.Value;
                    layerDepth2 = (rectangle.Bottom - 8) / 10000f;
                }
                else
                    layerDepth2 = 0f;

                spriteBatch.Draw(
                    texture: Furniture.furnitureTexture,
                    position: Game1.GlobalToLocal(viewport: Game1.viewport, globalPosition: new Vector2(x * 64, y * 64 - (__instance.sourceRect.Height * 4 - __instance.boundingBox.Height))),
                    sourceRectangle: __instance.sourceRect.Value,
                    color: Color.White * alpha,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale: 4f,
                    effects: __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    layerDepth: layerDepth2
                );
            }

            Rectangle bounds = Game1.shadowTexture.Bounds;
            spriteBatch.Draw(
                texture: Game1.shadowTexture,
                position:
                    Game1.GlobalToLocal(viewport: Game1.viewport, globalPosition: new Vector2(__instance.boundingBox.Center.X - 32, __instance.boundingBox.Center.Y - (__instance.drawHeldObjectLow.Value ? 32 : 85)))
                    + new Vector2(32f, 53f),
                sourceRectangle: bounds,
                color: Color.White * alpha,
                rotation: 0f,
                origin: new Vector2(bounds.Center.X, bounds.Center.Y),
                scale: 4f,
                effects: SpriteEffects.None,
                layerDepth: __instance.boundingBox.Bottom / 10000f
            );

            spriteBatch.Draw(
                texture: texture,
                position: Game1.GlobalToLocal(
                    viewport: Game1.viewport,
                    globalPosition: new Vector2(__instance.boundingBox.Center.X - 32, __instance.boundingBox.Center.Y - (__instance.drawHeldObjectLow.Value ? 32 : 85))
                ),
                sourceRectangle: sourceRect,
                color: Color.White * alpha,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 4f,
                effects: SpriteEffects.None,
                layerDepth: (__instance.boundingBox.Bottom + 1) / 10000f
            );

            if (__instance.IsOn && __instance.furniture_type.Value == Furniture.fireplace)
            {
                Vector2 position4 = Game1.GlobalToLocal(
                    viewport: Game1.viewport,
                    globalPosition: new Vector2(__instance.boundingBox.Center.X - 12, __instance.boundingBox.Center.Y - 64)
                );

                TimeSpan totalGameTime = Game1.currentGameTime.TotalGameTime;

                rectangle = __instance.getBoundingBox(new Vector2(x, y));

                spriteBatch.Draw(
                    texture: Game1.mouseCursors,
                    position: position4,
                    sourceRectangle: new Rectangle(276 + (int)((totalGameTime.TotalMilliseconds + x * 3047 + y * 88) % 400.0 / 100.0) * 12, 1985, 12, 11),
                    color: Color.White,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale: 4f,
                    effects: SpriteEffects.None,
                    layerDepth: (rectangle.Bottom - 2) / 10000f
                );

                Vector2 position5 = Game1.GlobalToLocal(
                    viewport: Game1.viewport,
                    globalPosition: new Vector2(__instance.boundingBox.Center.X - 32 - 4, __instance.boundingBox.Center.Y - 64)
                );

                totalGameTime = Game1.currentGameTime.TotalGameTime;
                rectangle = __instance.getBoundingBox(new Vector2(x, y));
                spriteBatch.Draw(
                    texture: Game1.mouseCursors,
                    position: position5,
                    sourceRectangle: new Rectangle(276 + (int)((totalGameTime.TotalMilliseconds + x * 2047 + y * 98) % 400.0 / 100.0) * 12, 1985, 12, 11),
                    color: Color.White,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale: 4f,
                    effects: SpriteEffects.None,
                    layerDepth: (rectangle.Bottom - 1) / 10000f
                );
            }

            return false;
        }

        /****
        ** Object
        ****/
        /// <summary>Patch for drawing a custom sprite (if available) for the object held by the instance.</summary>
        /// <param name="__instance">The object instance.</param>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="x">The x tile coordinate.</param>
        /// <param name="y">The y tile coordinate.</param>
        /// <param name="alpha">The draw transparency.</param>
        /// <returns>Returns true if the original method should be invoked, else false.</returns>
        public static bool Object_Draw(SObject __instance, SpriteBatch spriteBatch, int x, int y, float alpha = 1f)
        {
            if (
                !__instance.bigCraftable.Value
                || !__instance.readyForHarvest.Value
                || __instance.heldObject.Value == null
                || !BetterMeadIconsPatcher.TryLoadSprite(__instance.heldObject.Value, out Texture2D texture, out Rectangle sourceRect)
                )
            {
                return true;
            }

            Vector2 value = 4 * __instance.getScale();

            Vector2 vector = Game1.GlobalToLocal(viewport: Game1.viewport, globalPosition: new Vector2(x * 64, y * 64 - 64));

            Rectangle destinationRectangle = new Rectangle(
                (int)(vector.X - value.X / 2f) + ((__instance.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0),
                (int)(vector.Y - value.Y / 2f) + ((__instance.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0),
                (int)(64f + value.X),
                (int)(128f + value.Y / 2f)
            );

            spriteBatch.Draw(
                texture: Game1.bigCraftableSpriteSheet,
                destinationRectangle: destinationRectangle,
                sourceRectangle: SObject.getSourceRectForBigCraftable(__instance.showNextIndex.Value ? (__instance.ParentSheetIndex + 1) : __instance.ParentSheetIndex),
                color: Color.White * alpha,
                rotation: 0f,
                origin: Vector2.Zero,
                effects: SpriteEffects.None,
                layerDepth: Math.Max(0f, ((y + 1) * 64 - 24) / 10000f) + (__instance.ParentSheetIndex == 105 ? 0.0035f : 0f) + x * 1E-05f
                );

            if (__instance.Name.Equals("Loom") && __instance.MinutesUntilReady > 0)
            {
                spriteBatch.Draw(
                    texture: Game1.objectSpriteSheet,
                    position: __instance.getLocalPosition(Game1.viewport) + new Vector2(32f, 0f),
                    sourceRectangle: Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 435, 16, 16),
                    color: Color.White * alpha,
                    rotation: __instance.Scale.X,
                    origin: new Vector2(8f, 8f),
                    scale: 4f,
                    effects: SpriteEffects.None,
                    layerDepth: Math.Max(0f, (y + 1) * 64 / 10000f + 0.0001f + x * 1E-05f)
                );
            }

            if (__instance.isLamp.Value && Game1.isDarkOut())
            {
                spriteBatch.Draw(
                    texture: Game1.mouseCursors,
                    position: vector + new Vector2(-32f, -32f),
                    sourceRectangle: new Rectangle(88, 1779, 32, 32),
                    color: Color.White * 0.75f,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale: 4f,
                    effects: SpriteEffects.None,
                    layerDepth: Math.Max(0f, ((y + 1) * 64 - 20) / 10000f)
                );
            }

            if (__instance.ParentSheetIndex == 126 && __instance.Quality != 0)
            {
                spriteBatch.Draw(
                    texture: FarmerRenderer.hatsTexture,
                    position: vector + new Vector2(-3f, -6f) * 4f,
                    sourceRectangle: new Rectangle((__instance.Quality - 1) * 20 % FarmerRenderer.hatsTexture.Width, (__instance.Quality - 1) * 20 / FarmerRenderer.hatsTexture.Width * 20 * 4, 20, 20),
                    color: Color.White * alpha,
                    rotation: 0f,
                    origin: Vector2.Zero,
                    scale: 4f,
                    effects: SpriteEffects.None,
                    layerDepth: Math.Max(0f, ((y + 1) * 64 - 20) / 10000f) + x * 1E-05f
                );
            }

            TimeSpan timeSpan = DateTime.UtcNow.TimeOfDay;
            float num6 = 4f * (float)Math.Round(Math.Sin(timeSpan.TotalMilliseconds / 250.0), 2);
            spriteBatch.Draw(
                texture: Game1.mouseCursors,
                position: Game1.GlobalToLocal(viewport: Game1.viewport, globalPosition: new Vector2(x * 64 - 8, y * 64 - 96 - 16 + num6)),
                sourceRectangle: new Rectangle(141, 465, 20, 24),
                color: Color.White * 0.75f,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 4f,
                effects: SpriteEffects.None,
                layerDepth: (y + 1) * 64 / 10000f + 1E-06f + __instance.TileLocation.X / 10000f + ((__instance.ParentSheetIndex == 105) ? 0.0015f : 0f)
            );

            spriteBatch.Draw(
                texture: texture,
                position: Game1.GlobalToLocal(viewport: Game1.viewport, globalPosition: new Vector2(x * 64 + 32, y * 64 - 64 - 8 + num6)),
                sourceRectangle: sourceRect,
                color: Color.White * 0.75f,
                rotation: 0f,
                origin: new Vector2(8f, 8f),
                scale: 4f,
                effects: SpriteEffects.None,
                layerDepth: (y + 1) * 64 / 10000f + 1E-05f + __instance.TileLocation.X / 10000f + ((__instance.ParentSheetIndex == 105) ? 0.0015f : 0f)
            );

            return false;
        }

        /// <summary>Patch for drawing a custom sprite (if available) for the object held by the instance.</summary>
        /// <param name="__instance">The object instance.</param>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="xNonTile">The x non-tile coordinate.</param>
        /// <param name="yNonTile">The y non-tile coordinate.</param>
        /// <param name="layerDepth">The layer depth.</param>
        /// <param name="alpha">The draw transparency.</param>
        /// <returns>Returns true if the original method should be invoked, else false.</returns>
        public static bool Object_Draw2(SObject __instance, SpriteBatch spriteBatch, int xNonTile, int yNonTile, float layerDepth, float alpha)
        {
            if (!BetterMeadIconsPatcher.TryLoadSprite(__instance, out Texture2D texture, out Rectangle sourceRect))
                return true;

            if (Game1.eventUp && Game1.CurrentEvent.isTileWalkedOn(xNonTile / 64, yNonTile / 64))
                return false;

            if (__instance.Fragility != 2)
            {
                Texture2D shadowTexture = Game1.shadowTexture;
                spriteBatch.Draw(
                    texture: shadowTexture,
                    position: Game1.GlobalToLocal(viewport: Game1.viewport, globalPosition: new Vector2(xNonTile + 32, yNonTile + 51 + 4)),
                    sourceRectangle: shadowTexture.Bounds,
                    color: Color.White * alpha,
                    rotation: 0f,
                    origin: new Vector2(shadowTexture.Bounds.Center.X, shadowTexture.Bounds.Center.Y),
                    scale: 4f,
                    effects: SpriteEffects.None,
                    layerDepth: layerDepth - 1E-06f
                );
            }

            spriteBatch.Draw(
                texture: texture,
                position: Game1.GlobalToLocal(
                    viewport: Game1.viewport,
                    globalPosition: new Vector2(xNonTile + 32 + ((__instance.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0), yNonTile + 32 + ((__instance.shakeTimer > 0) ? Game1.random.Next(-1, 2) : 0))
                ),
                sourceRectangle: sourceRect,
                color: Color.White * alpha,
                rotation: 0f,
                origin: new Vector2(8f, 8f),
                scale: (__instance.Scale.Y > 1f) ? __instance.getScale().Y : 4f,
                effects: __instance.Flipped ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                layerDepth: layerDepth
            );

            return false;
        }

        /// <summary>Patch for drawing a custom sprite (if available) for the object in the menu.</summary>
        /// <param name="__instance">The object instance.</param>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="location">Where to draw on the sprite batch.</param>
        /// <param name="scaleSize">The scale at which to draw the asset.</param>
        /// <param name="transparency">The draw transparency.</param>
        /// <param name="layerDepth">The layer depth.</param>
        /// <param name="drawStackNumber">Whether to draw the item stack number.</param>
        /// <param name="color">The tint color.</param>
        /// <param name="drawShadow">Whether to draw a shadow under the item.</param>
        /// <returns>Returns true if the original method should be invoked, else false.</returns>
        public static bool Object_DrawInMenu(SObject __instance, SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, bool drawStackNumber, Color color, bool drawShadow)
        {
            if (!BetterMeadIconsPatcher.TryLoadSprite(__instance, out Texture2D texture, out Rectangle sourceRect))
                return true;

            if (drawShadow)
            {
                Texture2D shadowTexture = Game1.shadowTexture;
                spriteBatch.Draw(
                    texture: shadowTexture,
                    position: location + new Vector2(32f, 48f),
                    sourceRectangle: shadowTexture.Bounds,
                    color: color * 0.5f,
                    rotation: 0f,
                    origin: new Vector2(shadowTexture.Bounds.Center.X, shadowTexture.Bounds.Center.Y),
                    scale: 3f,
                    effects: SpriteEffects.None,
                    layerDepth: layerDepth - 0.0001f
                );
            }

            spriteBatch.Draw(
                texture: texture,
                position: location + new Vector2((int)(32f * scaleSize), (int)(32f * scaleSize)),
                sourceRectangle: sourceRect,
                color: color * transparency,
                rotation: 0f,
                origin: new Vector2(8f, 8f) * scaleSize,
                scale: 4f * scaleSize,
                effects: SpriteEffects.None,
                layerDepth: layerDepth
            );

            if (drawStackNumber && __instance.maximumStackSize() > 1 && scaleSize > 0.3 && __instance.Stack != 2147483647 && __instance.Stack > 1)
            {
                StardewValley.Utility.drawTinyDigits(
                    toDraw: __instance.Stack,
                    spriteBatch,
                    position: location + new Vector2(64 - StardewValley.Utility.getWidthOfTinyDigitString(__instance.Stack, 3f * scaleSize) + 3f * scaleSize, 64f - 18f * scaleSize + 2f),
                    scale: 3f * scaleSize,
                    layerDepth: 1f,
                    color
                );
            }

            if (drawStackNumber && __instance.Quality > 0)
            {
                float num = (__instance.Quality < 4) ? 0f : (((float)Math.Cos(Game1.currentGameTime.TotalGameTime.Milliseconds * Math.PI / 512.0) + 1f) * 0.05f);
                spriteBatch.Draw(
                    texture: Game1.mouseCursors,
                    position: location + new Vector2(12f, 52f + num),
                    sourceRectangle: (__instance.Quality < 4) ? new Rectangle(338 + (__instance.Quality - 1) * 8, 400, 8, 8) : new Rectangle(346, 392, 8, 8),
                    color: color * transparency,
                    rotation: 0f,
                    origin: new Vector2(4f, 4f),
                    scale: 3f * scaleSize * (1f + num),
                    effects: SpriteEffects.None,
                    layerDepth: layerDepth
                );
            }

            return false;
        }

        /// <summary>Patch for drawing a custom sprite (if available) for the given farmer's active object.</summary> 
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        /// <param name="objectPosition">Where to draw on the sprite batch.</param>
        /// <param name="f">The player holding the object.</param>
        /// <returns>Returns true if the original method should be invoked, else false.</returns>
        public static bool Object_DrawWhenHeld(SpriteBatch spriteBatch, Vector2 objectPosition, Farmer f)
        {
            if (!BetterMeadIconsPatcher.TryLoadSprite(f.ActiveObject, out Texture2D texture, out Rectangle sourceRect))
                return true;

            spriteBatch.Draw(
                texture: texture,
                position: objectPosition,
                sourceRectangle: sourceRect,
                color: Color.White,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: 4f,
                effects: SpriteEffects.None,
                layerDepth: Math.Max(0f, (f.getStandingY() + 2) / 10000f)
            );

            if (f.ActiveObject != null && f.ActiveObject.Name.Contains("="))
            {
                spriteBatch.Draw(
                    texture: texture,
                    position: objectPosition + new Vector2(32f, 32f),
                    sourceRectangle: sourceRect,
                    color: Color.White,
                    rotation: 0f,
                    origin: new Vector2(32f, 32f),
                    scale: 4f + Math.Abs(Game1.starCropShimmerPause) / 8f,
                    effects: SpriteEffects.None,
                    layerDepth: Math.Max(0f, (f.getStandingY() + 2) / 10000f)
                );
                if (Math.Abs(Game1.starCropShimmerPause) <= 0.05f && Game1.random.NextDouble() < 0.97)
                    return false;

                Game1.starCropShimmerPause += 0.04f;
                if (Game1.starCropShimmerPause >= 0.8f)
                    Game1.starCropShimmerPause = -0.8f;
            }

            return false;
        }


        /*********
        ** Private methods
        *********/
        public static bool TryLoadSprite(SObject obj, out Texture2D texture, out Rectangle sourceRect)
        {
            if (
                obj == null
                || obj.bigCraftable.Value
                || obj.ParentSheetIndex != 459
                || obj.honeyType.Value == null
                || obj.honeyType.Value.Value == SObject.HoneyType.Wild
                )
            {
                texture = default;
                sourceRect = default;
                return false;
            }

            texture = MeadIcon;
            switch (obj.honeyType.Value.Value)
            {
                case SObject.HoneyType.Tulip:
                    sourceRect = new Rectangle(0, 0, 16, 16);
                    break;

                case SObject.HoneyType.BlueJazz:
                    sourceRect = new Rectangle(16, 0, 16, 16);
                    break;

                case SObject.HoneyType.SummerSpangle:
                    sourceRect = new Rectangle(32, 0, 16, 16);
                    break;

                case SObject.HoneyType.Poppy:
                    sourceRect = new Rectangle(48, 0, 16, 16);
                    break;

                case SObject.HoneyType.FairyRose:
                    sourceRect = new Rectangle(64, 0, 16, 16);
                    break;

                default:
                    sourceRect = default;
                    break;
            }

            return true;
        }
    }
}
