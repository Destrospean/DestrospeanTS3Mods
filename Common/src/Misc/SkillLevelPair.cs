using Sims3.Gameplay.Skills;
using Sims3.SimIFace;

namespace Destrospean.Misc
{
    [Persistable]
    public class SkillLevelPair
    {
        private ulong mSkillName;

        public int SkillLevel;

        public SkillNames SkillName
        {
            get => (SkillNames)mSkillName;
            private set => mSkillName = (ulong)value;
        }

        public SkillLevelPair()
        {
        }

        /// <summary>
        /// Creates a new <see cref="Destrospean.Misc.SkillLevelPair"/> with a maximum value skill level.
        /// </summary>
        public SkillLevelPair(SkillNames skillName) : this(skillName, -1)
        {
        }

        public SkillLevelPair(SkillNames skillName, int skillLevel)
        {
            SkillName = skillName;
            SkillLevel = skillLevel;
        }
    }
}
