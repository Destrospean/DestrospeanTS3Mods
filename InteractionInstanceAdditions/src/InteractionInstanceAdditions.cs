using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.ChildAndTeenUpdates;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI.Resort;

namespace Destrospean
{
    public class InteractionInstanceAdditions
    {
        [Tunable]
        protected static bool kInstantiator;

        public delegate void InteractionInstanceAction(InteractionInstance interactionInstance);

        public delegate void WaitForSynchronizationLevelAction(InteractionInstance interactionInstance, Sim.SyncLevel syncLevel);

        public static InteractionInstanceAction OnInteractedStarted = interactionInstance =>
            {
            };

        public static InteractionInstanceAction OnInteractionEnded = interactionInstance =>
            {
            };

        public static InteractionInstanceAction StandardEntryPreCallCallback = interactionInstance =>
            {
            };

        public static InteractionInstanceAction StandardExitPostCallCallback = interactionInstance =>
            {
            };

        public static WaitForSynchronizationLevelAction OnWaitForSynchronizationLevel = (interactionInstance, syncLevel) =>
            {
            };
        
        static InteractionInstanceAdditions()
        {
            ReplaceMethod<InteractionInstance, InteractionInstanceAdditions>(nameof(RunInteractionWithoutCleanup));
            ReplaceMethod<InteractionInstance, InteractionInstanceAdditions>(nameof(StandardEntry));
            ReplaceMethod<InteractionInstance, InteractionInstanceAdditions>(nameof(StandardExit));
            ReplaceMethod<Sim, InteractionInstanceAdditions>(nameof(WaitForSynchronizationLevelWithSim));
        }

        /// <summary>
        /// Replaces a method (and its overloads) with another method.
        /// Note: only the overloads defined in the new type will replace the corresponding overloads of the old type.
        /// </summary>
        /// <typeparam name="OldType">The class that holds the method to be replaced.</typeparam>
        /// <typeparam name="NewType">The class that holds the new method. Note: only the overloads defined in the new type will replace the corresponding overloads of the old type.</typeparam>
        public static void ReplaceMethod<OldType, NewType>(string methodName)
        {
            ReplaceMethod<OldType, NewType>(methodName, methodName);
        }

        /// <summary>
        /// This method was borrowed from Lazy Duchess' Mono Patcher.
        /// </summary>
        public static void ReplaceMethod(MethodInfo oldMethod, MethodInfo newMethod)
        {
            var replacementByteArray = new byte[40];
            Marshal.Copy(newMethod.MethodHandle.Value, replacementByteArray, 0, 40);
            Marshal.Copy(replacementByteArray, 0, oldMethod.MethodHandle.Value, 24);
            Marshal.Copy(replacementByteArray, 28, new IntPtr(oldMethod.MethodHandle.Value.ToInt32() + 28), 12);
        }

        /// <summary>
        /// Replaces a method (and its overloads) with another method.
        /// Note: only the overloads defined in the new type will replace the corresponding overloads of the old type.
        /// </summary>
        /// <param name="oldMethodName">The name of the method to be replaced.</param>
        /// <param name="newMethodName">The name of the new method.</param>
        /// <typeparam name="OldType">The class that holds the method to be replaced.</typeparam>
        /// <typeparam name="NewType">The class that holds the new method. Note: only the overloads defined in the new type will replace the corresponding overloads of the old type.</typeparam>
        public static void ReplaceMethod<OldType, NewType>(string oldMethodName, string newMethodName)
        {
            foreach (var method in typeof(NewType).GetMethods((BindingFlags)0x3C))
            {
                if (method.Name == newMethodName)
                {
                    var oldMethod = typeof(OldType).GetMethod(oldMethodName, (BindingFlags)0x3C, null, Array.ConvertAll(method.GetParameters(), x => x.ParameterType), null);
                    if (oldMethod != null)
                    {
                        ReplaceMethod(oldMethod, method);
                    }
                }
            }
        }

        public bool RunInteractionWithoutCleanup()
        {
            var interactionInstance = (InteractionInstance)(object)this;
            if (interactionInstance.StandardEntryCalled)
            {
                return false;
            }
            OnInteractedStarted(interactionInstance);
            interactionInstance.StartInteraction();
            var actorless = interactionInstance.InteractionDefinition is IActorlessDefinition;
            var notIntermediate = false;
            bool succeeded;
            try
            {
                if (interactionInstance.Target.GetOwnerLot() == null && interactionInstance.InstanceActor != null && interactionInstance.Target.LotCurrent == interactionInstance.InstanceActor.LotHome)
                {
                    interactionInstance.Target.SetOwnerLot(interactionInstance.InstanceActor.LotHome);
                }
                interactionInstance.MustRun = false;
                if (!(interactionInstance is IImmediateInteraction))
                {
                    interactionInstance.InstanceActor?.InteractionQueue.PushRunningInteraction(interactionInstance);
                    notIntermediate = true;
                }
                if (!actorless)
                {
                    Punishment.PunishmentPreRun(interactionInstance.InstanceActor, interactionInstance);
                    OccultImaginaryFriend.StartReactionsIfNecessary(interactionInstance.InstanceActor, interactionInstance);
                }
                if (interactionInstance.InstanceActor?.Inventory != null && interactionInstance.InstanceActor.Inventory.Contains(interactionInstance.Target))
                {
                    if (!(interactionInstance is IRouteFromInventoryOrSelfWithoutCarrying) && !(interactionInstance is IImmediateInteraction))
                    {
                        if (!(interactionInstance.Target is Cane))
                        {
                            interactionInstance.InstanceActor.PopCanePostureIfNecessary();
                        }
                        if (!(interactionInstance.Target is Umbrella))
                        {
                            Umbrella.PopUmbrellaPostureIfNecessary(interactionInstance.InstanceActor, false);
                        }
                        if (!(interactionInstance.Target is Backpack))
                        {
                            interactionInstance.InstanceActor.PopBackpackPostureIfNecessary();
                        }
                        if (!(interactionInstance is IJetpackInteraction))
                        {
                            interactionInstance.InstanceActor.PopJetpackPostureIfNecessary();
                        }
                    }
                    succeeded = interactionInstance.RunFromInventory();
                }
                else
                {
                    succeeded = interactionInstance.Run();
                }
            }
            catch (Exception ex)
            {
                if (interactionInstance.InstanceActor != null && !ex.Data.Contains("Actor"))
                {
                    ex.Data.Add("Actor", interactionInstance.InstanceActor.ObjectId.Value);
                }
                if (interactionInstance.Target != null && !ex.Data.Contains("Target"))
                {
                    ex.Data.Add("Target", interactionInstance.Target.ObjectId.Value);
                }
                if (!ex.Data.Contains("Interaction"))
                {
                    ex.Data.Add("Interaction", GetType().FullName);
                }
                if (interactionInstance.Target != null && interactionInstance.InstanceActor?.InteractionQueue != null && interactionInstance.InstanceActor.InteractionQueue.TransitionInteraction == interactionInstance && interactionInstance.Target.IsActorUsingMe(interactionInstance.InstanceActor))
                {
                    interactionInstance.Target.SetObjectToReset();
                }
                throw;
            }
            finally
            {
                if (notIntermediate)
                {
                    interactionInstance.InstanceActor?.InteractionQueue.PopRunningInteraction(interactionInstance);
                }
                if (!actorless)
                {
                    Punishment.PunishmentPostRun(interactionInstance.InstanceActor);
                    OccultImaginaryFriend.StopReactionsIfNecessary(interactionInstance.InstanceActor);
                }
                if (interactionInstance.InstanceActor != null && interactionInstance.InstanceActor.SimDescription.IsBonehilda)
                {
                    Relationship.RemoveSimDescriptionRelationships(interactionInstance.InstanceActor.SimDescription);
                }
                if (interactionInstance.Target is Sim sim && sim.SimDescription.IsBonehilda)
                {
                    Relationship.RemoveSimDescriptionRelationships(sim.SimDescription);
                }
            }
            if (interactionInstance.InteractionDefinition is IResortManagementInteraction || interactionInstance.InteractionDefinition is IResortManagementStaffingInteraction || interactionInstance.InteractionDefinition is IResortManagementCrewInteraction)
            {
                ResortExpenseDialog.RefreshGrid();
            }
            if (interactionInstance.InstanceActor != null && (interactionInstance.InstanceActor.ExitReason & ExitReason.RouteFailed) != 0)
            {
                EventTracker.SendEvent(EventTypeId.kUsedObect, interactionInstance.InstanceActor, interactionInstance.Target);
            }
            try
            {
                interactionInstance.ValidatePostconditions(succeeded);
                OnInteractionEnded(interactionInstance);
                return succeeded;
            }
            catch (Exception ex)
            {
                if (interactionInstance.InstanceActor != null && !ex.Data.Contains("Actor"))
                {
                    ex.Data.Add("Actor", interactionInstance.InstanceActor.ObjectId.Value);
                }
                if (interactionInstance.Target != null && !ex.Data.Contains("Target"))
                {
                    ex.Data.Add("Target", interactionInstance.Target.ObjectId.Value);
                }
                if (!ex.Data.Contains("Interaction"))
                {
                    ex.Data.Add("Interaction", GetType().FullName);
                }
                throw;
            }
        }

        public void StandardEntry(bool addToUseList)
        {
            var interactionInstance = (InteractionInstance)(object)this;
            StandardEntryPreCallCallback(interactionInstance);
            interactionInstance.mInteractionState = InteractionInstance.InteractionState.StandardEntry;
            if (addToUseList)
            {
                interactionInstance.Target.AddToUseList(interactionInstance.InstanceActor);
            }
            if (interactionInstance.Target.LookAtTuning != null && interactionInstance.Target.LookAtTuning.DefaultInteractionLookAtThreshold > -1)
            {
                interactionInstance.InstanceActor.LookAtManager.SetInteractionLookAtThreshold(interactionInstance.Target.LookAtTuning.DefaultInteractionLookAtThreshold);
            }
        }

        public void StandardExit(bool removeFromUseList, bool validateUseList)
        {
            var interactionInstance = (InteractionInstance)(object)this;
            interactionInstance.mInteractionState = InteractionInstance.InteractionState.StandardExit;
            interactionInstance.DeactivateTone();
            if (removeFromUseList && interactionInstance.Target != null)
            {
                interactionInstance.Target.RemoveFromUseList(interactionInstance.InstanceActor);
            }
            if (!validateUseList)
            {
                interactionInstance.mInteractionState = InteractionInstance.InteractionState.StandardExitDoNotValidateUseList;
            }
            var lookAtTuning = interactionInstance.Target?.LookAtTuning;
            if (lookAtTuning != null && lookAtTuning.DefaultInteractionLookAtThreshold > -1)
            {
                interactionInstance.InstanceActor.LookAtManager.ClearInteractionLookAt();
            }
            StandardExitPostCallCallback(interactionInstance);
        }

        public bool WaitForSynchronizationLevelWithSim(Sim targetSim, Sim.SyncLevel desiredSynchLevel, ExitReason exitReasonInterrupt, float giveupTime, SyncLoopCallbackFunction loopCallback, float notifySimMinutes, bool performSocializeWithTest)
        {
            var sim = (Sim)(object)this;
            var dateAndTime = SimClock.CurrentTime();
            var previousDateAndTime = dateAndTime;
            GreyedOutTooltipCallback greyedOutTooltipCallback = null;
            sim.SynchronizationSleeping = sim.SynchronizationLevel >= desiredSynchLevel;
            while (!sim.IsAtSynchronizationLevelWith(targetSim, desiredSynchLevel))
            {
                if (SimClock.ElapsedTime(TimeUnit.Minutes, dateAndTime) >= giveupTime || sim.HasExitReason(exitReasonInterrupt))
                {
                    sim.OnWaitForSynchronizationLevelWithSimFailed(targetSim);
                    return false;
                }
                if (performSocializeWithTest)
                {
                    var result = SocialInteractionA.Definition.CanSocializeWithSyncCheck(null, sim, targetSim, sim.IsInAutonomousInteraction(), ref greyedOutTooltipCallback, true, true);
                    if (!IUtil.IsPass(result))
                    {
                        sim.OnWaitForSynchronizationLevelWithSimFailed(targetSim);
                        return false;
                    }
                }
                if (loopCallback != null && SimClock.ElapsedTime(TimeUnit.Minutes, previousDateAndTime) >= notifySimMinutes)
                {
                    if (!loopCallback())
                    {
                        sim.OnWaitForSynchronizationLevelWithSimFailed(targetSim);
                        return false;
                    }
                    previousDateAndTime = SimClock.CurrentTime();
                }
                Simulator.Sleep(0);
            }
            sim.SynchronizationSleeping = false;
            var exitReason = exitReasonInterrupt;
            if (sim.SynchronizationRole == Sim.SyncRole.Receiver)
            {
                exitReason &= ExitReason.SynchronizationFailed;
            }
            if (sim.HasExitReason(exitReason) || !sim.IsAtSynchronizationLevelWith(targetSim, desiredSynchLevel))
            {
                sim.OnWaitForSynchronizationLevelWithSimFailed(targetSim);
                return false;
            }
            OnWaitForSynchronizationLevel(sim.CurrentInteraction, desiredSynchLevel);
            return true;
        }
    }
}
