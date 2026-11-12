using System.Collections.Generic;
using Destrospean.Enums.HairTrouble;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.UI.Columns.HairTrouble
{
    public class HairGrowthStateEnabledColumn : ObjectPickerDialog.CommonHeaderInfo<HairGrowthStates>
    {
        private readonly string mLocalizationPath;

        private readonly List<HairGrowthStates> mHairGrowthStatesEnabled;

        public HairGrowthStateEnabledColumn(string localizationPath, HairGrowthStates[] hairGrowthStatesEnabled) : base(localizationPath + "/Headers/Enabled:Text", localizationPath + "/Headers/Enabled:Tooltip", 40)
        {
            mLocalizationPath = localizationPath;
            mHairGrowthStatesEnabled = new List<HairGrowthStates>(hairGrowthStatesEnabled);
        }

        public override ObjectPicker.ColumnInfo GetValue(HairGrowthStates hairGrowthState)
        {
            return new ObjectPicker.TextColumn(Responder.Instance.LocalizationModel.LocalizeString(mLocalizationPath + "/Options/Enabled:" + mHairGrowthStatesEnabled.Contains(hairGrowthState)));
        }
    }
}
