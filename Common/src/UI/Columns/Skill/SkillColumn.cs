using Destrospean.Misc;
using Sims3.Gameplay.Skills;
using Sims3.SimIFace;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.UI.Columns
{
    public class SkillColumn : ObjectPickerDialog.CommonHeaderInfo<SkillLevelPair>
    {
        public SkillColumn(string localizationPath) : base(localizationPath + "/Headers/Skill:Text", localizationPath + "/Headers/Skill:Tooltip", 400)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(SkillLevelPair skill)
        {
            var actualSkill = SkillManager.GetStaticSkill(skill.SkillName);
            return new ObjectPicker.ThumbAndTextColumn(new ThumbnailKey(actualSkill.IconKey, ThumbnailSize.Large), string.IsNullOrEmpty(actualSkill.Name) ? actualSkill.Guid.ToString() : actualSkill.Name);
        }
    }
}
