using System;
using System.Linq;
using Harmony;
using Pathoschild.Stardew.Automate;
using SilentOak.AutoQualityPatch.Utils;
using SilentOak.QualityProducts;
using StardewModdingAPI;
using StardewValley;
using SObject = StardewValley.Object;

namespace SilentOak.AutoQualityPatch.Patches
{
    /// <summary>Handles patches for compatibility with the Automate mod.</summary>
    internal static class AutomatePatcher
    {
        /*********
        ** Fields
        *********/
        /// <summary>Writes messages to the console and log file.</summary>
        private static IMonitor Monitor;

        /// <summary>The Automate machine full type names to patch.</summary>
        private static readonly string[] MachineTypeNames =
        {
            "Pathoschild.Stardew.Automate.Framework.Machines.Objects.KegMachine, Automate",
            "Pathoschild.Stardew.Automate.Framework.Machines.Objects.LoomMachine, Automate",
            "Pathoschild.Stardew.Automate.Framework.Machines.Objects.CheesePressMachine, Automate",
            "Pathoschild.Stardew.Automate.Framework.Machines.Objects.MayonnaiseMachine, Automate",
            "Pathoschild.Stardew.Automate.Framework.Machines.Objects.PreservesJarMachine, Automate",
            "Pathoschild.Stardew.Automate.Framework.Machines.Objects.OilMakerMachine, Automate"
        };


        /*********
        ** Public methods
        *********/
        /// <summary>Initialise the patcher.</summary>
        /// <param name="harmony">The mod's Harmony instance.</param>
        /// <param name="monitor">Writes messages to the console and log file.</param>
        public static void Init(HarmonyInstance harmony, IMonitor monitor)
        {
            Monitor = monitor;

            foreach (string typeName in AutomatePatcher.MachineTypeNames)
            {
                Type type = Type.GetType(typeName);
                if (type == null)
                    throw new InvalidOperationException($"Can't find type '{typeName}' to patch.");

                harmony.Patch(
                    original: AccessTools.Method(type, "SetInput"),
                    prefix: new HarmonyMethod(typeof(AutomatePatcher), nameof(AutomatePatcher.SetInput))
                );
            }
        }

        /// <summary>Replaces the recipes with the ones defined by the processor.</summary>
        public static bool SetInput(IMachine __instance, ref bool __result, IStorage input)
        {
            try
            {
                SObject machine = Util.Helper.Reflection.GetProperty<SObject>(__instance, "Machine").GetValue();
                if (QualityProducts.ModEntry.Factory.TryGetFor(machine, out Processor processor))
                {
                    IRecipe[] automateRecipes = Util.Helper.Reflection.GetField<IRecipe[]>(__instance, "Recipes").GetValue();
                    IRecipe[] recipes = RecipeManager.GetRecipeAdaptorsFor(processor, automateRecipes);

                    IConsumable consumable = null;
                    IRecipe recipe = null;

                    foreach (ITrackedStack item in input.GetItems())
                    {
                        IRecipe possibleRecipe = recipes.FirstOrDefault(rec => rec.AcceptsInput(item));
                        if (possibleRecipe != null && input.TryGetIngredient(item.Sample.ParentSheetIndex, possibleRecipe.InputCount, out consumable))
                        {
                            recipe = possibleRecipe;
                            break;
                        }
                    }

                    if (recipe != null && consumable != null)
                    {
                        Item inputStack = consumable.Take();
                        machine.heldObject.Value = recipe.Output(inputStack);
                        machine.MinutesUntilReady = recipe.Output(inputStack).MinutesUntilReady;//recipe.Minutes;
                        __result = true;
                        return false;
                    }

                    __result = false;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed overriding Automate.\n{ex}", LogLevel.Error);
                return true; // run original code instead
            }
        }
    }
}
