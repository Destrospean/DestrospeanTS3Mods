using System.Collections.Generic;
using Destrospean.Misc;
using Destrospean.UI.Columns;
using Destrospean.Utils;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;

namespace Destrospean.UI.Dialogs
{
    public static class SkillListDialog
    {
        public static bool ShowSkillListDialog(CASAgeGenderFlags age, CASAgeGenderFlags species, List<SkillLevelPair> currentSkills, List<SkillLevelPair> allSkills = null, string title = null)
        {
            var ageSpecies = CASUtils.CASAGSAvailabilityFlagsFromCASAgeGenderFlags(age | species);
            if (allSkills == null)
            {
                allSkills = new List<SkillLevelPair>(currentSkills);
                foreach (var skill in SkillManager.SkillDictionary)
                {
                    if ((skill.AvailableAgeSpecies & ageSpecies) != 0 && !allSkills.Exists(x => x.SkillName == skill.Guid))
                    {
                        allSkills.Add(new SkillLevelPair(skill.Guid, 0));
                    }
                }
            }
            else
            {
                allSkills.RemoveAll(x => (SkillManager.GetStaticSkill(x.SkillName).AvailableAgeSpecies & ageSpecies) == 0);
            }
            var entryKey = typeof(SkillListDialog).GetLocalizationKey();
            var skillList = new List<SkillLevelPair>(currentSkills);
            while (true)
            {
                var selectedSkills = ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + ":Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), allSkills.ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<SkillLevelPair>>
                    {
                        new SkillColumn(entryKey),
                        new SkillLevelColumn(entryKey)
                    }, 1, out var confirmed, out var cancelled, true);
                if (cancelled)
                {
                    return false;
                }
                if (confirmed)
                {
                    currentSkills.Clear();
                    currentSkills.AddRange(skillList);
                    return true;
                }
                var skillLevel = StringInputDialog.Show(Localization.LocalizeString(entryKey + "/SubmenuTitles:SkillLevel"), Localization.LocalizeString(entryKey + "/SubmenuPrompts:SkillLevel"), selectedSkills[0].SkillLevel.ToString(), -1, ThumbnailKey.kInvalidThumbnailKey, new Vector2(-1f, -1f), StringInputDialog.Validation.Number, false, ModalDialog.PauseMode.PauseSimulator, false, true);
                selectedSkills[0].SkillLevel = skillLevel == null ? selectedSkills[0].SkillLevel : int.Parse(skillLevel);
                var skill = SkillManager.GetStaticSkill(selectedSkills[0].SkillName);
                if (selectedSkills[0].SkillLevel == -1)
                {
                    selectedSkills[0].SkillLevel = skill.MaxSkillLevel;
                }
                else if (selectedSkills[0].SkillLevel > skill.MaxSkillLevel)
                {
                    selectedSkills[0].SkillLevel = skill.MaxSkillLevel;
                }
                if (selectedSkills[0].SkillLevel == 0 && skillList.Contains(selectedSkills[0]))
                {
                    skillList.Remove(selectedSkills[0]);
                    continue;
                }
                if (selectedSkills[0].SkillLevel != 0 && !skillList.Contains(selectedSkills[0]))
                {
                    skillList.Add(selectedSkills[0]);
                }
            }
        }
    }
}
