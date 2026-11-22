using System.Collections.Generic;
using Destrospean.UI.Columns;
using Destrospean.Utils;
using Sims3.Gameplay.ActorSystems;
using Sims3.SimIFace.CAS;
using Sims3.UI;

namespace Destrospean.UI.Dialogs
{
    public static class TraitListDialog
    {
        public static bool ShowTraitListDialog(CASAgeGenderFlags age, CASAgeGenderFlags gender, CASAgeGenderFlags species, List<Trait> currentTraits, List<Trait> allTraits = null, string title = null)
        {
            var ageSpecies = CASUtils.CASAGSAvailabilityFlagsFromCASAgeGenderFlags(age | species);
            if (allTraits == null)
            {
                allTraits = new List<Trait>();
                foreach (var trait in TraitManager.GetDictionaryTraits)
                {
                    if (trait.TraitValidForAgeSpecies(ageSpecies) && !trait.IsHidden && !trait.IsReward)
                    {
                        allTraits.Add(trait);
                    }
                }
            }
            else
            {
                allTraits.RemoveAll(x => !x.TraitValidForAgeSpecies(ageSpecies));
            }
            var entryKey = typeof(TraitListDialog).GetLocalizationKey();
            var traitList = new List<Trait>(currentTraits);
            while (true)
            {
                var selectedTraits = ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + ":Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), allTraits.ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<Trait>>
                    {
                        new TraitColumn(entryKey, gender == CASAgeGenderFlags.Female),
                        new TraitEnabledColumn(entryKey, traitList.ToArray())
                    }, 1, out var confirmed, out var cancelled, true);
                if (cancelled)
                {
                    return false;
                }
                if (confirmed)
                {
                    currentTraits.Clear();
                    currentTraits.AddRange(traitList);
                    return true;
                }
                if (traitList.Contains(selectedTraits[0]))
                {
                    traitList.Remove(selectedTraits[0]);
                }
                else
                {
                    traitList.Add(selectedTraits[0]);
                }
            }
        }
    }
}
