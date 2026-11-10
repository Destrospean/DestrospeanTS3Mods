using System.Collections.Generic;
using Sims3.SimIFace;
using Sims3.UI.Hud;

namespace Sims3.Gameplay.Skills.Destrospean.RocketScienceSkill
{
    public class RocketScienceSkill : Skill
    {
        public class RocketsBuilt : ITrackedStat
        {
            private RocketScienceSkill mSkill;

            public string Description => "Number of rockets built: " + mSkill.mNumRocketsBuilt;

            public RocketsBuilt(RocketScienceSkill skill)
            {
                mSkill = skill;
            }
        }

        [Persistable(false)]
        private List<ILifetimeOpportunity> mLifetimeOpportunities;

        private uint mNumRocketsBuilt;
        
        [Persistable(false)]
        private List<ITrackedStat> mTrackedStats;

        public override List<ILifetimeOpportunity> LifetimeOpportunities => mLifetimeOpportunities;
        
        public static SkillNames RocketScienceSkillGuid = (SkillNames)ResourceUtils.HashString64("RocketScienceSkill");

        public override List<ITrackedStat> TrackedStats => mTrackedStats;
        
        public RocketScienceSkill()
        {
        }
        
        public RocketScienceSkill(SkillNames guid) : base(guid)
        {
        }

        public override void CreateSkillJournalInfo()
        {
            mTrackedStats = new List<ITrackedStat>
            {
                new RocketsBuilt(this)
            };
        }

        public override bool ExportContent(IPropertyStreamWriter writer)
        {
            base.ExportContent(writer);
            writer.WriteUint32(0x56CD8442, mNumRocketsBuilt);
            return true;
        }

        public override bool ImportContent(IPropertyStreamReader reader)
        {
            base.ImportContent(reader);
            reader.ReadUint32(0x56CD8442, out mNumRocketsBuilt, 0u);
            return true;
        }
        
        public void IncrementRocketsBuiltStat()
        {
            mNumRocketsBuilt++;
        }

        public override void MergeTravelData(Skill skill)
        {
            base.MergeTravelData(skill);
            if (skill is RocketScienceSkill rocketScienceSkill)
            {
                mNumRocketsBuilt = rocketScienceSkill.mNumRocketsBuilt;
            }
        }
    }
}
