using Destrospean.Enums;
using Sims3.Gameplay.Autonomy;
using Sims3.SimIFace;

namespace Destrospean.Utils
{
    public static class CommodityUtils
    {
        /// <summary>
        /// Gets a valid commodity kind value from a string.
        /// </summary>
        /// <returns>The commodity kind.</returns>
        /// <param name="name">The name of the commodity kind.</param>
        /// <param name="type">The type of commodity.</param>
        public static CommodityKind GetCommodityKind(string name, CommodityKindType type)
        {
            var retVal = ResourceUtils.HashString32(name);
            switch (type)
            {
                case CommodityKindType.Motive:
                    retVal = retVal & 0xF0FFFFFF | 0x01000000;
                    break;
                case CommodityKindType.Skill:
                    retVal = retVal & 0x0FFFFFFF | 0x20000000;
                    break;
                case CommodityKindType.Posture:
                    retVal = retVal & 0x00FFFFFF | 0x04000000;
                    break;
                case CommodityKindType.PostureCheck:
                    retVal = retVal & 0x00FFFFFF | 0x05000000;
                    break;
                case CommodityKindType.Trait:
                    retVal = retVal & 0x0FFFFFFF | 0x10000000;
                    break;
            }
            return (CommodityKind)retVal;
        }
    }
}
