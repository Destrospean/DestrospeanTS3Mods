using System;
using System.Collections.Generic;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Utilities;

namespace Destrospean.Utils
{
    public static class InteractionTuningUtils
    {
        /// <summary>
        /// Adds the commodity kind as a commodity change output to an interaction tuning.
        /// </summary>
        /// <param name="commodityKind">Commodity kind.</param>
        /// <param name="constantChange">Advertised value.</param>
        /// <param name="locked">If set to <c>true</c>, locked.</param>
        /// <param name="actualValue">Actual value.</param>
        /// <param name="updateType">Update type.</param>
        /// <param name="timeDependsOnCommodityFilling">If set to <c>true</c> time depends on commodity filling.</param>
        /// <param name="updateEvenOnFailure">If set to <c>true</c> update even on failure.</param>
        /// <param name="updateAboveAndBelowZero">Update above and below zero.</param>
        /// <typeparam name="InteractionDefinition">Interaction definition type.</typeparam>
        /// <typeparam name="Target">Target type.</typeparam>
        public static void AddAsOutput<InteractionDefinition, Target>(this CommodityKind commodityKind, float constantChange, bool locked, float actualValue, OutputUpdateType updateType, bool timeDependsOnCommodityFilling = false, bool updateEvenOnFailure = false, UpdateAboveAndBelowZeroType updateAboveAndBelowZero = UpdateAboveAndBelowZeroType.Either) where InteractionDefinition : Sims3.Gameplay.Interactions.InteractionDefinition where Target : IGameObject
        {
            commodityKind.AddAsOutput(typeof(InteractionDefinition), typeof(Target), constantChange, locked, actualValue, updateType, timeDependsOnCommodityFilling, updateEvenOnFailure, updateAboveAndBelowZero);
        }

        /// <summary>
        /// Adds the commodity kind as a commodity change output to an interaction tuning.
        /// </summary>
        /// <param name="commodityKind">Commodity kind.</param>
        /// <param name="interactionDefinitionType">Interaction definition type.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="constantChange">Advertised value.</param>
        /// <param name="locked">If set to <c>true</c>, locked.</param>
        /// <param name="actualValue">Actual value.</param>
        /// <param name="updateType">Update type.</param>
        /// <param name="timeDependsOnCommodityFilling">If set to <c>true</c> time depends on commodity filling.</param>
        /// <param name="updateEvenOnFailure">If set to <c>true</c> update even on failure.</param>
        /// <param name="updateAboveAndBelowZero">Update above and below zero.</param>
        public static void AddAsOutput(this CommodityKind commodityKind, Type interactionDefinitionType, Type targetType, float constantChange, bool locked, float actualValue, OutputUpdateType updateType, bool timeDependsOnCommodityFilling = false, bool updateEvenOnFailure = false, UpdateAboveAndBelowZeroType updateAboveAndBelowZero = UpdateAboveAndBelowZeroType.Either)
        {
            var outputs = AutonomyTuning.GetTuning(interactionDefinitionType.FullName, targetType)?.mTradeoff?.mOutputs;
            if (outputs == null)
            {
                return;
            }
            outputs.RemoveAll(x => x.Commodity == commodityKind && x.ConstantChange == constantChange && x.mLocked == locked && x.mActualValue == actualValue && x.mTimeDependsOnCommodityFilling == timeDependsOnCommodityFilling && x.mUpdateEvenOnFailure == updateEvenOnFailure && x.mUpdateAboveAndBelowZero == updateAboveAndBelowZero);
            outputs.Add(new CommodityChange(commodityKind, constantChange, locked, actualValue, updateType, timeDependsOnCommodityFilling, updateEvenOnFailure, updateAboveAndBelowZero));
            RefreshInteractionObjectPairs(interactionDefinitionType, targetType);
        }

        /// <summary>
        /// Adds the commodity kind as a commodity change output to an interaction tuning.
        /// </summary>
        /// <param name="commodityKind">Commodity kind.</param>
        /// <param name="interactionDefinitionType">Interaction definition type full name.</param>
        /// <param name="targetType">Target type full name.</param>
        /// <param name="constantChange">Advertised value.</param>
        /// <param name="locked">If set to <c>true</c>, locked.</param>
        /// <param name="actualValue">Actual value.</param>
        /// <param name="updateType">Update type.</param>
        /// <param name="timeDependsOnCommodityFilling">If set to <c>true</c> time depends on commodity filling.</param>
        /// <param name="updateEvenOnFailure">If set to <c>true</c> update even on failure.</param>
        /// <param name="updateAboveAndBelowZero">Update above and below zero.</param>
        public static void AddAsOutput(this CommodityKind commodityKind, string interactionDefinitionType, string targetType, float constantChange, bool locked, float actualValue, OutputUpdateType updateType, bool timeDependsOnCommodityFilling = false, bool updateEvenOnFailure = false, UpdateAboveAndBelowZeroType updateAboveAndBelowZero = UpdateAboveAndBelowZeroType.Either)
        {
            commodityKind.AddAsOutput(InteractionObjectTypeUtils.InteractionDefinitionTypes[interactionDefinitionType], InteractionObjectTypeUtils.GameObjectTypes[targetType], constantChange, locked, actualValue, updateType, timeDependsOnCommodityFilling, updateEvenOnFailure, updateAboveAndBelowZero);
        }
        
        public static void CopyTuning(Type baseType, Type oldType, Type newType)
        {
            if (AutonomyTuning.GetTuning(newType.FullName, baseType.FullName) == null)
            {
                var tuning = AutonomyTuning.GetTuning(oldType, oldType.FullName, baseType);
                if (tuning != null)
                {
                    AutonomyTuning.AddTuning(newType.FullName, baseType.FullName, tuning);
                }
            }
            InteractionObjectPair.sTuningCache.Remove(new Pair<Type, Type>(newType, baseType));
        }
        
        /// <summary>
        /// Refreshes the interaction object pairs of a specified interaction definition for all existing objects of a specified type in the world.
        /// </summary>
        /// <typeparam name="InteractionDefinition">Interaction definition type.</typeparam>
        /// <typeparam name="Target">Target type.</typeparam>
        public static void RefreshInteractionObjectPairs<InteractionDefinition, Target>() where InteractionDefinition : Sims3.Gameplay.Interactions.InteractionDefinition where Target : IGameObject
        {
            RefreshInteractionObjectPairs(typeof(InteractionDefinition), typeof(Target));
        }

        /// <summary>
        /// Refreshes the interaction object pairs of a specified interaction definition for all existing objects of a specified type in the world.
        /// </summary>
        /// <param name="interactionDefinitionType">Interaction definition type.</param>
        /// <param name="targetType">Target type.</param>
        public static void RefreshInteractionObjectPairs(Type interactionDefinitionType, Type targetType)
        {
            foreach (var gameObject in Queries.GetObjects<GameObject>())
            {
                if (targetType.IsAssignableFrom(gameObject.GetType()))
                {
                    foreach (var interaction in new List<InteractionObjectPair>(gameObject.Interactions))
                    {
                        if (interactionDefinitionType.IsAssignableFrom(interaction.mInteraction.GetType()) && targetType.IsAssignableFrom(interaction.mTargetType))
                        {
                            gameObject.RemoveInteraction(interaction);
                            gameObject.AddInteraction(new InteractionObjectPair(interaction.InteractionDefinition, interaction.Target, AutonomyTuning.GetTuning(interactionDefinitionType.FullName, targetType)));
                            if (gameObject.ItemComp != null && gameObject.ItemComp.InteractionsInventory.Contains(interaction))
                            {
                                gameObject.ItemComp.InteractionsInventory.Remove(interaction);
                                gameObject.AddInventoryInteraction(interaction.InteractionDefinition);
                            }
                        }
                    }
                }
            }
            foreach (var sim in Queries.GetObjects<Sim>())
            {
                sim.UpdateCommodityInteractionMap();
            }
        }

        /// <summary>
        /// Refreshes the interaction object pairs of a specified interaction definition for all existing objects of a specified type in the world.
        /// </summary>
        /// <param name="interactionDefinitionType">Interaction definition type full name.</param>
        /// <param name="targetType">Target type full name.</param>
        public static void RefreshInteractionObjectPairs(string interactionDefinitionType, string targetType)
        {
            RefreshInteractionObjectPairs(InteractionObjectTypeUtils.InteractionDefinitionTypes[interactionDefinitionType], InteractionObjectTypeUtils.GameObjectTypes[targetType]);
        }
        
        /// <summary>
        /// Removes the commodity kind as a commodity change output from an interaction tuning.
        /// </summary>
        /// <param name="commodityKind">Commodity kind.</param>
        /// <param name="constantChange">Advertised value.</param>
        /// <param name="locked">If set to <c>true</c>, locked.</param>
        /// <param name="actualValue">Actual value.</param>
        /// <param name="updateType">Update type.</param>
        /// <param name="timeDependsOnCommodityFilling">If set to <c>true</c>, time depends on commodity filling.</param>
        /// <param name="updateEvenOnFailure">If set to <c>true</c>, update even on failure.</param>
        /// <param name="updateAboveAndBelowZero">Update above and below zero.</param>
        /// <typeparam name="InteractionDefinition">Interaction definition type.</typeparam>
        /// <typeparam name="Target">Target type.</typeparam>
        public static void RemoveAsOutput<InteractionDefinition, Target>(this CommodityKind commodityKind, float constantChange, bool locked, float actualValue, OutputUpdateType updateType, bool timeDependsOnCommodityFilling = false, bool updateEvenOnFailure = false, UpdateAboveAndBelowZeroType updateAboveAndBelowZero = UpdateAboveAndBelowZeroType.Either) where InteractionDefinition : Sims3.Gameplay.Interactions.InteractionDefinition where Target : IGameObject
        {
            commodityKind.RemoveAsOutput(typeof(InteractionDefinition), typeof(Target), constantChange, locked, actualValue, updateType, timeDependsOnCommodityFilling, updateEvenOnFailure, updateAboveAndBelowZero);
        }

        /// <summary>
        /// Removes the commodity kind as a commodity change output from an interaction tuning.
        /// </summary>
        /// <param name="commodityKind">Commodity kind.</param>
        /// <param name="interactionDefinitionType">Interaction definition type.</param>
        /// <param name="targetType">Target type.</param>
        /// <param name="constantChange">Advertised value.</param>
        /// <param name="locked">If set to <c>true</c>, locked.</param>
        /// <param name="actualValue">Actual value.</param>
        /// <param name="updateType">Update type.</param>
        /// <param name="timeDependsOnCommodityFilling">If set to <c>true</c>, time depends on commodity filling.</param>
        /// <param name="updateEvenOnFailure">If set to <c>true</c>, update even on failure.</param>
        /// <param name="updateAboveAndBelowZero">Update above and below zero.</param>
        public static void RemoveAsOutput(this CommodityKind commodityKind, Type interactionDefinitionType, Type targetType, float constantChange, bool locked, float actualValue, OutputUpdateType updateType, bool timeDependsOnCommodityFilling = false, bool updateEvenOnFailure = false, UpdateAboveAndBelowZeroType updateAboveAndBelowZero = UpdateAboveAndBelowZeroType.Either)
        {
            var outputs = AutonomyTuning.GetTuning(interactionDefinitionType.FullName, targetType)?.mTradeoff?.mOutputs ?? new List<CommodityChange>();
            outputs.RemoveAll(x => x.Commodity == commodityKind && x.ConstantChange == constantChange && x.mLocked == locked && x.mActualValue == actualValue && x.mTimeDependsOnCommodityFilling == timeDependsOnCommodityFilling && x.mUpdateEvenOnFailure == updateEvenOnFailure && x.mUpdateAboveAndBelowZero == updateAboveAndBelowZero);
            RefreshInteractionObjectPairs(interactionDefinitionType, targetType);
        }

        /// <summary>
        /// Removes the commodity kind as a commodity change output from an interaction tuning.
        /// </summary>
        /// <param name="commodityKind">Commodity kind.</param>
        /// <param name="interactionDefinitionType">Interaction definition type full name.</param>
        /// <param name="targetType">Target type full name.</param>
        /// <param name="constantChange">Advertised value.</param>
        /// <param name="locked">If set to <c>true</c>, locked.</param>
        /// <param name="actualValue">Actual value.</param>
        /// <param name="updateType">Update type.</param>
        /// <param name="timeDependsOnCommodityFilling">If set to <c>true</c>, time depends on commodity filling.</param>
        /// <param name="updateEvenOnFailure">If set to <c>true</c>, update even on failure.</param>
        /// <param name="updateAboveAndBelowZero">Update above and below zero.</param>
        public static void RemoveAsOutput(this CommodityKind commodityKind, string interactionDefinitionType, string targetType, float constantChange, bool locked, float actualValue, OutputUpdateType updateType, bool timeDependsOnCommodityFilling = false, bool updateEvenOnFailure = false, UpdateAboveAndBelowZeroType updateAboveAndBelowZero = UpdateAboveAndBelowZeroType.Either)
        {
            commodityKind.RemoveAsOutput(InteractionObjectTypeUtils.InteractionDefinitionTypes[interactionDefinitionType], InteractionObjectTypeUtils.GameObjectTypes[targetType], constantChange, locked, actualValue, updateType, timeDependsOnCommodityFilling, updateEvenOnFailure, updateAboveAndBelowZero);
        }
    }
}
