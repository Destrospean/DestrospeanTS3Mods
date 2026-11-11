using System.Collections.Generic;
using Destrospean.Utils;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Objects.Decorations;
using Sims3.SimIFace;
using Queries = Sims3.Gameplay.Queries;

namespace Destrospean.HairTrouble
{
    public class Main
    {
        [Tunable]
        protected static bool kInstantiator;

        static Main()
        {
            CommonUtils.ReplaceMethod<Genetics, Replacements>(nameof(Genetics.InheritHairColor));
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
                    _ = SimHairGrowth.HairGrowthStateMap;
                    foreach (var mirror in Queries.GetObjects<Mirror>())
                    {
                        AddInteractions(mirror);
                    }
                    foreach (var sim in Queries.GetObjects<Sim>())
                    {
                        AddInteractions(sim);
                    }
                    if (SimHairData.OriginalHairColors.Count == 0 && Household.ActiveHousehold != null)
                    {
                        foreach (var sim in Queries.GetObjects<Sim>())
                        {
                            sim.SimDescription.InitOriginalOverallHairColors();
                        }
                    }
                    listeners.Add(EventTracker.AddListener(EventTypeId.kSimDescriptionDisposed, evt => DebugUtils.TryDisplayScriptError(() =>
                        {
                            if (evt is SimDescriptionEvent simDescriptionEvent)
                            {
                                simDescriptionEvent.SimDescription.ClearOriginalOverallHairColors();
                            }
                            return ListenerAction.Keep;
                        }, out var retVal) ? ListenerAction.Keep : retVal));
                    listeners.Add(EventTracker.AddListener(EventTypeId.kSimInstantiated, evt => DebugUtils.TryDisplayScriptError(() =>
                        {
                            if (evt.TargetObject is Sim sim)
                            {
                                AddInteractions(sim);
                                sim.SimDescription.InitOriginalOverallHairColors();
                            }
                            return ListenerAction.Keep;
                        }, out var retVal) ? ListenerAction.Keep : retVal));
                    listeners.Add(EventTracker.AddListener(EventTypeId.kEventSimSelected, evt => DebugUtils.TryDisplayScriptError(() =>
                        {
                            if (SimHairData.OriginalHairColors.Count == 0)
                            {
                                foreach (var sim in Queries.GetObjects<Sim>())
                                {
                                    sim.SimDescription.InitOriginalOverallHairColors();
                                }
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
    }
}
