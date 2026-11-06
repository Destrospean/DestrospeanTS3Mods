namespace Destrospean.HairTrouble
{
    public static class S3PIResourceUtils
    {
        public static Sims3.SimIFace.ResourceKey FromS3PIFormatKeyString(string key)
        {
            var tgi = key.Replace("0x", "").Split('-');
            return new Sims3.SimIFace.ResourceKey(System.Convert.ToUInt64(tgi[2], 16), System.Convert.ToUInt32(tgi[0], 16), System.Convert.ToUInt32(tgi[1], 16));
        }

        public static string ToS3PIFormatKeyString(this Sims3.SimIFace.ResourceKey key)
        {
            return $"0x{key.TypeId:X8}-0x{key.GroupId:X8}-0x{key.InstanceId:X16}";
        }
    }
}
