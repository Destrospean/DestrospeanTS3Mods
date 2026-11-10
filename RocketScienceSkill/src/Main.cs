using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.SimIFace;
using Queries = Sims3.Gameplay.Queries;

namespace Destrospean.RocketScienceSkill
{
    public class Main
    {
        [Tunable]
        protected static bool kInstantiator;

        static Main()
        {
            //var listeners = new List<EventListener>();
            LoadSaveManager.ObjectGroupsPreLoad += () =>
                {
                };
            World.OnObjectPlacedInLotEventHandler += (sender, e) => DebugUtils.TryDisplayScriptError(() =>
                {
                    /*
                    if (e is World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs)
                    {
                        var gameObject = GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId);
                        gameObject.AddInteraction(ListInteractions.Singleton, true);
                    }
                    */
                });
            World.sOnWorldLoadFinishedEventHandler += (sender, e) => DebugUtils.TryDisplayScriptError(() =>
                {
                    foreach (var gameObject in Queries.GetObjects<GameObject>())
                    {
                        //gameObject.AddInteraction(ListInteractions.Singleton, true);
                    }
                });
            World.sOnWorldQuitEventHandler += (sender, e) =>
                {
                    /*
                    foreach (var listener in listeners)
                    {
                        EventTracker.RemoveListener(listener);
                    }
                    listeners.Clear();
                    */
                };
        }
    }
}
