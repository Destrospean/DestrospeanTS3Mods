using System;
using Sims3.SimIFace;

namespace Destrospean.Utils.HairTrouble
{
    public static class S3PIResourceUtils
    {
        public static ResourceKey FromS3PIFormatKeyString(string key)
        {
            var tgi = key.Replace("0x", "").Split('-');
            return new ResourceKey(Convert.ToUInt64(tgi[2], 16), Convert.ToUInt32(tgi[0], 16), Convert.ToUInt32(tgi[1], 16));
        }

        public static string ToS3PIFormatKeyString(this ResourceKey key)
        {
            return $"0x{key.TypeId:X8}-0x{key.GroupId:X8}-0x{key.InstanceId:X16}";
        }
    }
}
