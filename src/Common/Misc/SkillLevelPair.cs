using Sims3.Gameplay.Skills;
using Sims3.SimIFace;
using Destrospean.Utils;

namespace Destrospean.Misc
{
    [Persistable]
    public class SkillLevelPair
    {
        ulong mSkillName = 0uL;

        public int SkillLevel = 0;

        public SkillNames SkillName
        {
            get
            {
                return (SkillNames)mSkillName;
            }
            set
            {
                mSkillName = (ulong)value;
            }
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
