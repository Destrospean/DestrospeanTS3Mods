extern alias NRaasMasterController;
using System;
using System.Collections.Generic;
using Destrospean.Utils;
using Destrospean.Utils.HairTrouble;
using NRaasMasterController::NRaas.MasterControllerSpace.CAS;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Objects.Decorations;
using Sims3.SimIFace;
using Sims3.UI.CAS;
using Queries = Sims3.Gameplay.Queries;

namespace Destrospean.HairTrouble
{
    public class Main
    {
        [Tunable]
        protected static bool kInstantiator;

        static Main()
        {
            CommonUtils.ReplaceMethod<Genetics, Replacements>(nameof(Replacements.InheritHairColor));
            CommonUtils.ReplaceMethod<CASHair, Replacements>(nameof(Replacements.PopulateTypesGrid));
            if (Array.Exists(AppDomain.CurrentDomain.GetAssemblies(), x => x.GetName().Name == "NRaasMasterController"))
            {
                IntegrateNRaasMasterController();
            }
            var listeners = new List<EventListener>();
            World.OnObjectPlacedInLotEventHandler += (sender, e) => DebugUtils.TryDisplayScriptError(() =>
                {
                    if (e is World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs)
                    {
                        AddInteractions(GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId) as Mirror);
                    }
                });
            World.sOnWorldLoadFinishedEventHandler += (sender, e) => DebugUtils.TryDisplayScriptError(() =>
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (assembly.GetType("Destrospean.HairTrouble.Data") != null)
                        {
                            FileUtils.Singleton.ImportFromTuning(assembly.GetName().Name);
                        }
                    }
                    foreach (var mirror in Queries.GetObjects<Mirror>())
                    {
                        AddInteractions(mirror);
                    }
                    foreach (var sim in Queries.GetObjects<Sim>())
                    {
                        AddInteractions(sim);
                    }
                    foreach (var sim in Queries.GetObjects<Sim>())
                    {
                        sim.SimDescription?.InitOriginalOverallHairColors();
                        sim.SimDescription?.GetHairGrowthState();
                    }
                    listeners.Add(EventTracker.AddListener(EventTypeId.kSimDescriptionDisposed, evt => DebugUtils.TryDisplayScriptError(() =>
                        {
                            if (evt is SimDescriptionEvent simDescriptionEvent)
                            {
                                simDescriptionEvent.SimDescription.ClearOriginalOverallHairColors();
                                SimHairData.GrowthStates.Remove(simDescriptionEvent.SimDescription.SimDescriptionId);
                            }
                            return ListenerAction.Keep;
                        }, out var retVal) ? ListenerAction.Keep : retVal));
                    listeners.Add(EventTracker.AddListener(EventTypeId.kSimInstantiated, evt => DebugUtils.TryDisplayScriptError(() =>
                        {
                            if (evt.TargetObject is Sim sim)
                            {
                                AddInteractions(sim);
                                sim.SimDescription.InitOriginalOverallHairColors();
                                sim.SimDescription.GetHairGrowthState();
                            }
                            return ListenerAction.Keep;
                        }, out var retVal) ? ListenerAction.Keep : retVal));
                });
            World.sOnWorldQuitEventHandler += (sender, e) =>
                {
                    foreach (var listener in listeners)
                    {
                        EventTracker.RemoveListener(listener);
                    }
                    listeners.Clear();
                };
        }
        
        public static void AddInteractions(Mirror mirror)
        {
            if (mirror == null)
            {
                return;
            }
            mirror.AddInteraction(Interactions.RemoveHairDye.Singleton, true);
            mirror.AddInteraction(Interactions.ClassifyHairstyles.Singleton, true);
            mirror.AddInteraction(Interactions.ImportHairClassificationCollection.Singleton, true);
            mirror.AddInteraction(Interactions.ExportHairClassificationCollection.Singleton, true);
            mirror.AddInteraction(Interactions.DeleteHairClassificationCollection.Singleton, true);
        }

        public static void AddInteractions(Sim sim)
        {
            if (sim == null)
            {
                return;
            }
            sim.AddInteraction(Interactions.ResetOriginalHair.Singleton, true);
            sim.AddInteraction(Interactions.IncrementHairGrowthState.Singleton, true);
            sim.AddInteraction(Interactions.DecrementHairGrowthState.Singleton, true);
        }

        public static void IntegrateNRaasMasterController()
        {
            CommonUtils.ReplaceMethod<CASHairEx, Replacements>(nameof(Replacements.PopulateTypesGrid));
        }
    }
}
