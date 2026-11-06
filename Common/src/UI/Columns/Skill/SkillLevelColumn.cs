using Destrospean.Misc;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.UI.Columns
{
    public class SkillLevelColumn : ObjectPickerDialog.CommonHeaderInfo<SkillLevelPair>
    {
        public SkillLevelColumn(string localizationPath) : base(localizationPath + "/Headers/Level:Text", localizationPath + "/Headers/Level:Tooltip", 40)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(SkillLevelPair skill)
        {
            return new ObjectPicker.NumberColumn(skill.SkillLevel);
        }
    }
}
