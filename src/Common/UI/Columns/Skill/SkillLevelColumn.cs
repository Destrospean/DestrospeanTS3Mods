using Sims3.UI;
using Destrospean.Misc;

namespace Destrospean.UI.Columns
{
    public class SkillLevelColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<SkillLevelPair>
    {
        public SkillLevelColumn(string localizationPath, SkillLevelPair[] skills) : base(localizationPath + "/Headers/Level:Text", localizationPath + "/Headers/Level:Tooltip", 40)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(SkillLevelPair skill)
        {
            return new ObjectPicker.NumberColumn(skill.SkillLevel);
        }
    }
}
