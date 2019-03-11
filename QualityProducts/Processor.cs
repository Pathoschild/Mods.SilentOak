﻿using System;
using Microsoft.Xna.Framework;
using QualityProducts.Processors;
using QualityProducts.Util;
using StardewValley;
using SObject = StardewValley.Object;

namespace QualityProducts
{
    /// <summary>
    /// An entity that is capable of processing items into products.
    /// </summary>
    public abstract class Processor : SObject
    {
        /****************
         * Public methods
         ****************/

        public static readonly string ProcessorNameSuffix = " [Processor]";

        public enum ProcessorType
        {
            KEG = 12,
            PRESERVE_JAR = 15,
            CHEESE_PRESS = 16,
            LOOM = 17,
            OIL_MAKER = 19,
            MAYONNAISE_MACHINE = 24
        }

        /// <summary>
        /// Gets the type of the processor for the corresponding index.
        /// </summary>
        /// <returns>The processor type.</returns>
        /// <param name="parentSheetIndex">Parent sheet index.</param>
        public static ProcessorType? GetProcessorType(int parentSheetIndex)
        {
            if (Enum.IsDefined(typeof(ProcessorType), parentSheetIndex))
            {
                return (ProcessorType)Enum.ToObject(typeof(ProcessorType), parentSheetIndex);
            }

            return null;
        }

        /// <summary>
        /// Creates a new instance of the specified processor type.
        /// </summary>
        /// <returns>The new processor instance.</returns>
        /// <param name="processorType">Processor type to be instantiated.</param>
        public static Processor Create(ProcessorType processorType)
        {
            switch (processorType)
            {
                case ProcessorType.KEG:
                    return new Keg();
                case ProcessorType.PRESERVE_JAR:
                    return new PreserveJar();
                case ProcessorType.CHEESE_PRESS:
                    return new CheesePress();
                case ProcessorType.LOOM:
                    return new Loom();
                case ProcessorType.OIL_MAKER:
                    return new OilMaker();
                case ProcessorType.MAYONNAISE_MACHINE:
                    return new MayonnaiseMachine();
                default:
                    throw new UnimplementedCaseException($"Enum value {Enum.GetName(typeof(ProcessorType), processorType)} of Processor.ValidType has no corresponding case");
            }
        }

        /// <summary>
        /// Creates a new instance of the specified processor type, initializing it with the specified initializer.
        /// </summary>
        /// <returns>The new processor instance.</returns>
        /// <param name="processorType">Processor type to be instantiated.</param>
        /// <param name="initializer">Initializer.</param>
        public static Processor Create(ProcessorType processorType, Action<Processor> initializer)
        {
            Processor newObj;
            switch (processorType)
            {
                case ProcessorType.KEG:
                    newObj = new Keg();
                    break;
                case ProcessorType.PRESERVE_JAR:
                    newObj = new PreserveJar();
                    break;
                case ProcessorType.CHEESE_PRESS:
                    newObj = new CheesePress();
                    break;
                case ProcessorType.LOOM:
                    newObj = new Loom();
                    break;
                case ProcessorType.OIL_MAKER:
                    newObj = new OilMaker();
                    break;
                case ProcessorType.MAYONNAISE_MACHINE:
                    newObj = new MayonnaiseMachine();
                    break;
                default:
                    throw new UnimplementedCaseException($"Enum value {Enum.GetName(typeof(ProcessorType), processorType)} of Processor.ValidType has no corresponding case");
            }
            initializer(newObj);
            return newObj;
        }

        /// <summary>
        /// Creates a processor instance based on the specified Stardew Valley object.
        /// </summary>
        /// <returns>The new processor instance.</returns>
        /// <param name="object">Reference object.</param>
        public static Processor FromObject(SObject @object)
        {
            if (!@object.bigCraftable.Value)
            {
                return null;
            }

            ProcessorType? processorType = GetProcessorType(@object.ParentSheetIndex);
            if (processorType != null) {
                Processor processor = Create(processorType.Value,
                p => 
                {
                    p.TileLocation = @object.TileLocation;
                    p.IsRecipe = (bool)@object.isRecipe;
                    p.DisplayName = @object.DisplayName;
                    p.Scale = @object.Scale;
                    p.MinutesUntilReady = @object.MinutesUntilReady;
                });

                processor.owner.Value = @object.owner.Value;
                processor.heldObject.Value = @object.heldObject.Value;
                processor.readyForHarvest.Value = @object.readyForHarvest.Value;

                return processor;
            }
            return null;
        }

        /// <summary>
        /// Creates a new regular Stardew Valley object with the same attributes as this one.  
        /// </summary>
        /// <returns>The new object.</returns>
        public SObject ToObject()
        {
            SObject @object = new SObject(tileLocation, parentSheetIndex, false)
            {
                IsRecipe = (bool)isRecipe,
                Name = baseName,
                DisplayName = base.DisplayName,
                Scale = Scale,
                MinutesUntilReady = MinutesUntilReady
            };

            @object.owner.Value = owner.Value;
            @object.heldObject.Value = heldObject.Value;
            @object.readyForHarvest.Value = readyForHarvest.Value;

            return @object;
        }

        /***
         * Modified from StardewValley.Object.performObjectDropInAction
         **/
        /// <summary>
        /// Performs the object drop in action.
        /// </summary>
        /// <returns><c>true</c>, if object drop in action was performed, <c>false</c> otherwise.</returns>
        /// <param name="dropInItem">Drop in item.</param>
        /// <param name="probe">If set to <c>true</c> probe.</param>
        /// <param name="who">Who.</param>
        public override bool performObjectDropInAction(Item dropInItem, bool probe, Farmer who)
        {
            if (dropInItem is SObject)
            {
                SObject @object = dropInItem as SObject;
                if (heldObject.Value != null)
                {
                    return false;
                }
                if (@object != null && (bool)@object.bigCraftable)
                {
                    return false;
                }
                if (!probe && @object != null && heldObject.Value == null)
                {
                    scale.X = 5f;
                }

                if (PerformProcessing(@object, probe, who))
                {
                    heldObject.Value.Quality = @object.Quality;
                    if (!probe)
                    {
                        QualityProducts.Instance.Monitor.VerboseLog($"Inserted {@object.DisplayName} (quality {@object.Quality}) into {Name} @({TileLocation.X},{TileLocation.Y})");
                        QualityProducts.Instance.Monitor.VerboseLog($"{Name} @({TileLocation.X},{TileLocation.Y}) is producing {heldObject.Value.DisplayName} (quality {heldObject.Value.Quality})");
                    }
                    return true;
                }
            }
            return false;
        }


        /*******************
         * Protected methods
         *******************/

        protected Processor(ProcessorType processorType) : base(Vector2.Zero, (int)processorType, false)
        {
            baseName = base.Name;
            Name = ToProcessorName(baseName);
        }

        /// <summary>
        /// Performs item processing.
        /// </summary>
        /// <returns><c>true</c> if started processing, <c>false</c> otherwise.</returns>
        /// <param name="object">Object to be processed.</param>
        /// <param name="probe">If set to <c>true</c> probe.</param>
        /// <param name="who">Farmer that initiated processing.</param>
        protected abstract bool PerformProcessing(SObject @object, bool probe, Farmer who);


        /******************
         * Private methods
         ******************/
        
        /// <summary>
        /// The base name.
        /// </summary>
        private string baseName;

        /// <summary>
        /// Converts name of the object to name of corresponding processor.
        /// </summary>
        /// <returns>The processor name.</returns>
        /// <param name="name">Object name.</param>
        private static string ToProcessorName(string name)
        {
            return name + ProcessorNameSuffix;
        }
    }
}
