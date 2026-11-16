using Destrospean.Delegates;
using Sims3.Gameplay.Abstracts;
using Sims3.SimIFace;

namespace Destrospean.Misc
{
    [Persistable(false)]
    public class ActionScriptTask : ScriptTask
    {
        private Action[] mActions;

        public ActionScriptTask(params Action[] actions)
        {
            mActions = actions;
        }
        
        public override void Simulate()
        {
            try
            {
                if (mActions != null)
                {
                    foreach (var action in mActions)
                    {
                        action();
                    }
                }
            }
            catch (ResetException)
            {
            }
        }
        
        public void Start()
        {
            Simulator.AddObject(this);
        }

        public static void Start(params Action[] actions)
        {
            Simulator.AddObject(new ActionScriptTask(actions));
        }
    }
}