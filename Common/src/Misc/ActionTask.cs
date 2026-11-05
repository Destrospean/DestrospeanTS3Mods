using Destrospean.Delegates;
using Sims3.SimIFace;

namespace Destrospean.Misc
{
    public class ActionTask : Task
    {
        private Action[] mActions;

        public ActionTask(params Action[] actions)
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
            finally 
            {
                Simulator.DestroyObject(ObjectId);
            }
        }

        public void Start()
        {
            Simulator.AddObject(this);
        }

        public static void Start(params Action[] actions)
        {
            Simulator.AddObject(new ActionTask(actions));
        }
    }
}
