using System;
using Destrospean.OutfitAssignment.Interactions;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems.Children;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Objects.ShelvesStorage;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Queries = Sims3.Gameplay.Queries;
using Tuning = Sims3.Gameplay.Destrospean.OutfitAssignment;

namespace Destrospean.OutfitAssignment
{
    public class Main
    {
        [Tunable]
        protected static bool kInstantiator;

        static Main()
        {
            InteractionInstanceTypeUtils.InitInteractionInstanceTypes();
            InteractionInstanceAdditions.ReplaceMethod<Sim.SwitchOutfitHelper, Replacements>(nameof(Replacements.Init));
            InteractionInstanceAdditions.ReplaceMethod<ChangeToddlerClothes, Replacements>(nameof(Replacements.StartBuildingOutfit));
            InteractionInstanceAdditions.ReplaceMethod<Sim, Replacements>(nameof(Replacements.SwitchToOutfitDelayed));
            InteractionInstanceAdditions.ReplaceMethod<Sim, Replacements>(nameof(Replacements.SwitchToOutfitWithoutSpin));
            InteractionInstanceAdditions.ReplaceMethod<Sim, Replacements>(nameof(Replacements.SwitchToOutfitWithSpin));
            if (Array.Exists(AppDomain.CurrentDomain.GetAssemblies(), x => x.GetName().Name == "NRaasMasterController"))
            {
                NRaasMasterControllerIntegration.Init();
            }
            EventListener simAgeTransitionListener = null;
            EventListener simDescriptionDisposedListener = null;
            EventListener simInstantiatedListener = null;
            World.sOnObjectPlacedInLotEventHandler += (sender, e) =>
                {
                    if (e is World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs)
                    {
                        AddInteractions(GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId) as Dresser);
                    }
                };
            World.sOnWorldLoadFinishedEventHandler += (sender, e) =>
                {
                    OutfitAssignmentUtils.OutfitAssignments.RemoveAll(x => x.SimDescription == null && x.SpecialOutfitKey.StartsWith(OutfitAssignmentUtils.OutfitAssignmentCategoryPrefix));
                    OutfitAssignmentUtils.PreviousOutfits.RemoveAll(x => !OutfitAssignmentUtils.OutfitAssignments.Exists(y => y.SimDescription == x.SimDescription));
                    foreach (var sim in Queries.GetObjects<Sim>())
                    {
                        AddInteractions(sim);
                    }
                    foreach (var dresser in Queries.GetObjects<Dresser>())
                    {
                        AddInteractions(dresser);
                    }
                    simAgeTransitionListener = EventTracker.AddListener(EventTypeId.kSimAgeTransition, evt =>
                        {
                            try
                            {
                                if (evt.TargetObject is Sim sim)
                                {
                                    sim.SimDescription.RemoveAllOutfitAssignments(true);
                                    OutfitAssignmentUtils.PreviousOutfits.RemoveAll(x => x.SimDescription == sim.SimDescription);
                                }
                            }
                            catch (Exception ex)
                            {
                                ((IScriptErrorWindow)AppDomain.CurrentDomain.GetData("ScriptErrorWindow")).DisplayScriptError(null, ex);
                            }
                            return ListenerAction.Keep;
                        });
                    simDescriptionDisposedListener = EventTracker.AddListener(EventTypeId.kSimDescriptionDisposed, evt =>
                        {
                            try
                            {
                                if (evt.TargetObject is Sim sim)
                                {
                                    sim.SimDescription.RemoveAllOutfitAssignments();
                                    OutfitAssignmentUtils.PreviousOutfits.RemoveAll(x => x.SimDescription == sim.SimDescription);
                                }
                            }
                            catch (Exception ex)
                            {
                                ((IScriptErrorWindow)AppDomain.CurrentDomain.GetData("ScriptErrorWindow")).DisplayScriptError(null, ex);
                            }
                            return ListenerAction.Keep;
                        });
                    simInstantiatedListener = EventTracker.AddListener(EventTypeId.kSimInstantiated, evt =>
                        {
                            try
                            {
                                if (evt.TargetObject is Sim sim)
                                {
                                    AddInteractions(sim);
                                }
                            }
                            catch (Exception ex)
                            {
                                ((IScriptErrorWindow)AppDomain.CurrentDomain.GetData("ScriptErrorWindow")).DisplayScriptError(null, ex);
                            }
                            return ListenerAction.Keep;
                        });
                };
            World.sOnWorldQuitEventHandler += (sender, e) =>
                {
                    EventTracker.RemoveListener(simAgeTransitionListener);
                    EventTracker.RemoveListener(simDescriptionDisposedListener);
                    EventTracker.RemoveListener(simInstantiatedListener);
                    simAgeTransitionListener = null;
                    simDescriptionDisposedListener = null;
                    simInstantiatedListener = null;
                };
            InteractionInstanceAdditions.OnInteractedStarted += interactionInstance =>
                {
                    try
                    {
                        if (interactionInstance?.InstanceActor?.SimDescription != null && (interactionInstance.InstanceActor.SimDescription.TryGetOutfitAssignment(interactionInstance, out var outfitAssignment) || interactionInstance.InstanceActor.SimDescription.TryGetGlobalOutfitAssignment(interactionInstance, out outfitAssignment)))
                        {
                            if (interactionInstance.InstanceActor.CurrentOutfitCategory != OutfitCategories.Special || interactionInstance.InstanceActor.CurrentOutfitIndex != interactionInstance.InstanceActor.SimDescription.GetSpecialOutfitIndexFromKey(ResourceUtils.HashString32(outfitAssignment.SpecialOutfitKey)))
                            {
                                OutfitAssignmentUtils.PreviousOutfits.RemoveAll(x => x.SimDescription == interactionInstance.InstanceActor.SimDescription);
                                OutfitAssignmentUtils.PreviousOutfits.Add(new OutfitAssignmentUtils.Outfit
                                    {
                                        Category = interactionInstance.InstanceActor.CurrentOutfitCategory,
                                        Index = interactionInstance.InstanceActor.CurrentOutfitIndex,
                                        SimDescription = interactionInstance.InstanceActor.SimDescription
                                    });
                            }
                            if (outfitAssignment.EntryCallbackType == InteractionInstanceTypeUtils.CallbackTypes.InteractionStarted)
                            {
                                interactionInstance.InstanceActor.SwitchToAssignedOutfit(outfitAssignment);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ((IScriptErrorWindow)AppDomain.CurrentDomain.GetData("ScriptErrorWindow")).DisplayScriptError(null, ex);
                    }
                };
            InteractionInstanceAdditions.OnInteractionEnded += interactionInstance =>
                {
                    try
                    {
                        if (interactionInstance?.InstanceActor?.SimDescription != null && (interactionInstance.InstanceActor.SimDescription.TryGetOutfitAssignment(interactionInstance, out var outfitAssignment) || interactionInstance.InstanceActor.SimDescription.TryGetGlobalOutfitAssignment(interactionInstance, out outfitAssignment)) && outfitAssignment.ExitCallbackType == InteractionInstanceTypeUtils.CallbackTypes.InteractionEnded)
                        {
                            interactionInstance.InstanceActor.SwitchToPreviousOutfit();
                        }
                    }
                    catch (Exception ex)
                    {
                        ((IScriptErrorWindow)AppDomain.CurrentDomain.GetData("ScriptErrorWindow")).DisplayScriptError(null, ex);
                    }
                };
            InteractionInstanceAdditions.OnWaitForSynchronizationLevel += (interactionInstance, syncLevel) =>
                {
                    try
                    {
                        if (interactionInstance?.InstanceActor?.SimDescription != null && (interactionInstance.InstanceActor.SimDescription.TryGetOutfitAssignment(interactionInstance, out var outfitAssignment) || interactionInstance.InstanceActor.SimDescription.TryGetGlobalOutfitAssignment(interactionInstance, out outfitAssignment)))
                        {
                            switch (syncLevel)
                            {
                                case Sim.SyncLevel.Committed:
                                    if (outfitAssignment.EntryCallbackType == InteractionInstanceTypeUtils.CallbackTypes.SyncLevelCommitted)
                                    {
                                        interactionInstance.InstanceActor.SwitchToAssignedOutfit(outfitAssignment);
                                    }
                                    break;
                                case Sim.SyncLevel.Completed:
                                    if (outfitAssignment.ExitCallbackType == InteractionInstanceTypeUtils.CallbackTypes.SyncLevelCompleted)
                                    {
                                        interactionInstance.InstanceActor.SwitchToPreviousOutfit();
                                    }
                                    break;
                                case Sim.SyncLevel.Routed:
                                    if (outfitAssignment.EntryCallbackType == InteractionInstanceTypeUtils.CallbackTypes.SyncLevelRouted)
                                    {
                                        interactionInstance.InstanceActor.SwitchToAssignedOutfit(outfitAssignment);
                                    }
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ((IScriptErrorWindow)AppDomain.CurrentDomain.GetData("ScriptErrorWindow")).DisplayScriptError(null, ex);
                    }
                };
            InteractionInstanceAdditions.StandardEntryPreCallCallback += interactionInstance =>
                {
                    try
                    {
                        if (interactionInstance?.InstanceActor?.SimDescription != null && (interactionInstance.InstanceActor.SimDescription.TryGetOutfitAssignment(interactionInstance, out var outfitAssignment) || interactionInstance.InstanceActor.SimDescription.TryGetGlobalOutfitAssignment(interactionInstance, out outfitAssignment)) && (outfitAssignment.EntryCallbackType == InteractionInstanceTypeUtils.CallbackTypes.OutfitChanged || outfitAssignment.EntryCallbackType == InteractionInstanceTypeUtils.CallbackTypes.StandardEntry))
                        {
                            interactionInstance.InstanceActor.SwitchToAssignedOutfit(outfitAssignment);
                        }
                    }
                    catch (Exception ex)
                    {
                        ((IScriptErrorWindow)AppDomain.CurrentDomain.GetData("ScriptErrorWindow")).DisplayScriptError(null, ex);
                    }
                };
            InteractionInstanceAdditions.StandardExitPostCallCallback += interactionInstance =>
                {
                    try
                    {
                        if (interactionInstance?.InstanceActor?.SimDescription != null && (interactionInstance.InstanceActor.SimDescription.TryGetOutfitAssignment(interactionInstance, out var outfitAssignment) || interactionInstance.InstanceActor.SimDescription.TryGetGlobalOutfitAssignment(interactionInstance, out outfitAssignment)) && outfitAssignment.ExitCallbackType == InteractionInstanceTypeUtils.CallbackTypes.StandardExit)
                        {
                            interactionInstance.InstanceActor.SwitchToPreviousOutfit();
                        }
                    }
                    catch (Exception ex)
                    {
                        ((IScriptErrorWindow)AppDomain.CurrentDomain.GetData("ScriptErrorWindow")).DisplayScriptError(null, ex);
                    }
                };
        }

        private static void AddInteractions(GameObject gameObject)
        {
            if (gameObject is Sim)
            {
                if (!Tuning.kAddInteractionsToSims)
                {
                    return;
                }
                gameObject.AddInteraction(AssignOutfitToInteraction.SimSingleton, true);
                gameObject.AddInteraction(AssignOutfitToInteraction.PartialOutfitSimSingleton, true);
                gameObject.AddInteraction(AssignOutfitCategoryToInteraction.SimSingleton, true);
                gameObject.AddInteraction(EditAssignedOutfit.SimSingleton, true);
                gameObject.AddInteraction(ConfigureOutfitAssignment.SimSingleton, true);
                gameObject.AddInteraction(CopyAssignedOutfitToInteraction.SimSingleton, true);
                gameObject.AddInteraction(CopyOutfitAssignmentToSim.SimSingleton, true);
                gameObject.AddInteraction(ExtendAssignedOutfitToInteraction.SimSingleton, true);
                gameObject.AddInteraction(UnassignOutfitToInteraction.SimSingleton, true);
            }
            else if (gameObject != null)
            {
                gameObject.AddInteraction(AssignOutfitToInteraction.GlobalOutfitSingleton, true);
                gameObject.AddInteraction(AssignOutfitToInteraction.Singleton, true);
                gameObject.AddInteraction(AssignOutfitToInteraction.PartialOutfitSingleton, true);
                gameObject.AddInteraction(AssignOutfitCategoryToInteraction.GlobalOutfitSingleton, true);
                gameObject.AddInteraction(AssignOutfitCategoryToInteraction.Singleton, true);
                gameObject.AddInteraction(EditAssignedOutfit.GlobalOutfitSingleton, true);
                gameObject.AddInteraction(EditAssignedOutfit.Singleton, true);
                gameObject.AddInteraction(ConfigureOutfitAssignment.GlobalOutfitSingleton, true);
                gameObject.AddInteraction(ConfigureOutfitAssignment.Singleton, true);
                gameObject.AddInteraction(CopyAssignedOutfitToInteraction.GlobalOutfitSingleton, true);
                gameObject.AddInteraction(CopyAssignedOutfitToInteraction.Singleton, true);
                gameObject.AddInteraction(CopyOutfitAssignmentToSim.Singleton, true);
                gameObject.AddInteraction(ExtendAssignedOutfitToInteraction.GlobalOutfitSingleton, true);
                gameObject.AddInteraction(ExtendAssignedOutfitToInteraction.Singleton, true);
                gameObject.AddInteraction(UnassignOutfitToInteraction.GlobalOutfitSingleton, true);
                gameObject.AddInteraction(UnassignOutfitToInteraction.Singleton, true);
            }
        }
    }
}
