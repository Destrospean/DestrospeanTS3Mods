using Sims3.Gameplay;
using Sims3.Gameplay.Interfaces;
using Sims3.SimIFace;

namespace Destrospean.Misc
{
    [Persistable]
    public class InventoryObjectCreationParameters
    {
        public int Count;

        public uint GroupId;

        public ulong InstanceId;

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
            var results = new IGameObject[count];
            for (var i = 0; i < count; i++)
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
