using System.Collections.Generic;
using Destrospean.Enums.HairTrouble;
using Destrospean.Utils.HairTrouble;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.UI.Columns
{
    public class CASPartHairGrowthStateColumn : ObjectPickerDialog.CommonHeaderInfo<CASPart>
    {
        private readonly string mLocalizationPath;

        private readonly IDictionary<string, HairGrowthStates> mHairGrowthStateMap;

        public CASPartHairGrowthStateColumn(string localizationPath, IDictionary<string, HairGrowthStates> hairGrowthStateMap) : base(localizationPath + "/Headers/HairGrowthState:Text", localizationPath + "/Headers/HairGrowthState:Tooltip", 40)
        {
            mLocalizationPath = localizationPath;
            mHairGrowthStateMap = hairGrowthStateMap;
        }

        public override ObjectPicker.ColumnInfo GetValue(CASPart casPart)
        {
            return new ObjectPicker.TextColumn(Responder.Instance.LocalizationModel.LocalizeString(mLocalizationPath + "/Options/HairGrowthState:" + (mHairGrowthStateMap.TryGetValue(casPart.Key.ToS3PIFormatKeyString(), out var hairGrowthState) ? hairGrowthState : HairGrowthStates.Unknown)));
        }
    }
}
