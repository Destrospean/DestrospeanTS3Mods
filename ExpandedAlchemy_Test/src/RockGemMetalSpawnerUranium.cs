namespace Sims3.Gameplay.Objects.Spawners
{
    public class RockGemMetalSpawnerUranium : RockGemMetalSpawner
    {
        protected static RockGemMetalSpawner.RgmSpawnerData kSpawnerData;

        public override RockGemMetalSpawner.RgmSpawnerData SpawnerData
        {
            get
            {
                return kSpawnerData;
            }
        }
    }
}
