using Sims3.Gameplay;
using Sims3.Gameplay.Interfaces;
using Sims3.SimIFace;

namespace Destrospean.Misc
{
    [Persistable]
    public class InventoryObjectCreationParameters
    {
        public int Count = 0;

        public uint GroupId = 0x00000000;

        public ulong InstanceId = 0x0000000000000000;

        public string InstanceName;

        public string Preset;

        public InventoryObjectCreationParameters()
        {
        }

        public InventoryObjectCreationParameters(string instanceName, ulong instanceId, uint groupId, int count, string preset = null)
        {
            Count = count;
            GroupId = groupId;
            InstanceId = instanceId;
            InstanceName = instanceName;
            Preset = preset;
        }

        public IGameObject Instantiate()
        {
            return InstantiateMany(1)[0];
        }

        public IGameObject[] InstantiateMany()
        {
            return InstantiateMany(Count);
        }

        public IGameObject[] InstantiateMany(int count)
        {
            if (count < 0)
            {
                return new IGameObject[0];
            }
            IGameObject[] results = new IGameObject[count];
            for (int i = 0; i < count; i++)
            {
                results[i] = GlobalFunctions.CreateObjectOutOfWorld(new ResourceKey(InstanceId, 0x319E4F1D, GroupId));
                if (results[i].ObjectId == ObjectGuid.InvalidObjectGuid)
                {
                    results[i].Destroy();
                    results[i] = GlobalFunctions.CreateObjectOutOfWorld(new ResourceKey(NameGuidMap.GetGuidByName(InstanceName), 0x319E4F1D, GroupId));
                }
            }
            return results;
        }
    }
}
