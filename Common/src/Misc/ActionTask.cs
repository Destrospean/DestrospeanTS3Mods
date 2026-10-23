using Sims3.SimIFace;
using Destrospean.Delegates;

namespace Destrospean.Misc
{
    public class ActionTask : Task
    {
        Action[] mActions = null;

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
                    foreach (Action action in mActions)
                    {
                        action();
                    }
                }
            }
            catch
            {
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
