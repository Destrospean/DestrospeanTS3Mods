using System.Collections.Generic;
using Sims3.SimIFace.CAS;
using Sims3.UI;

namespace Destrospean.UI.Columns
{
    public class CASAgeGenderFlagEnabledColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<CASAgeGenderFlags>
    {
        private readonly string mLocalizationPath;

        private readonly List<CASAgeGenderFlags> mTraitsEnabled;

        public CASAgeGenderFlagEnabledColumn(string localizationPath, CASAgeGenderFlags[] flagsEnabled) : base(localizationPath + "/Headers/Enabled:Text", localizationPath + "/Headers/Enabled:Tooltip", 40)
        {
            mLocalizationPath = localizationPath;
            mTraitsEnabled = new List<CASAgeGenderFlags>(flagsEnabled);
        }

        public override ObjectPicker.ColumnInfo GetValue(CASAgeGenderFlags flag)
        {
            return new ObjectPicker.TextColumn(Responder.Instance.LocalizationModel.LocalizeString(mLocalizationPath + "/Options/Enabled:" + mTraitsEnabled.Contains(flag)));
        }
    }
}
