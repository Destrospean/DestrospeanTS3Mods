using s3pi.Interfaces;

namespace Destrospean.MoreFavorites.Generator
{
    public class ResourceKey : IResourceKey
    {
        public ulong Instance
        {
            get;
            set;
        }

        public uint ResourceGroup
        {
            get;
            set;
        }

        public uint ResourceType
        {
            get;
            set;
        }

        public ResourceKey(uint type, uint group, ulong instance)
        {
            Instance = instance;
            ResourceGroup = group;
            ResourceType = type;
        }

        public int CompareTo(IResourceKey other)
        {
            var result = ResourceType.CompareTo(other.ResourceType);
            if (result != 0 || (result = ResourceGroup.CompareTo(other.ResourceGroup)) != 0)
            {
                return result;
            }
            return Instance.CompareTo(other.Instance);
        }

        public bool Equals(IResourceKey a, IResourceKey b)
        {
            return a.Equals(b);
        }

        public bool Equals(IResourceKey other)
        {
            return CompareTo(other) == 0;
        }

        public override int GetHashCode()
        {
            return ResourceType.GetHashCode() ^ ResourceGroup.GetHashCode() ^ Instance.GetHashCode();
        }

        public int GetHashCode(IResourceKey resourceKey)
        {
            return resourceKey.GetHashCode();
        }
    }
}
