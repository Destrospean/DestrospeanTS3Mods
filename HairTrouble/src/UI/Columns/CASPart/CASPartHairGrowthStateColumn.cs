using System;
using System.Collections.Generic;
using Destrospean.Enums.HairTrouble;
using Destrospean.Utils.HairTrouble;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.UI.Columns.HairTrouble
{
    public class CASPartHairGrowthStateColumn : ObjectPickerDialog.CommonHeaderInfo<CASPart>
    {
        private readonly string mLocalizationPath;

        private readonly IDictionary<string, HairGrowthStates> mHairGrowthStateMap;

        public CASPartHairGrowthStateColumn(string localizationPath, IDictionary<string, HairGrowthStates> hairGrowthStateMap) : base(localizationPath + "/Headers/HairGrowthState:Text", localizationPath + "/Headers/HairGrowthState:Tooltip", 110)
        {
            mLocalizationPath = localizationPath;
            mHairGrowthStateMap = hairGrowthStateMap;
        }

        public override ObjectPicker.ColumnInfo GetValue(CASPart casPart)
        {
            mHairGrowthStateMap.TryGetValue(casPart.Key.ToS3PIFormatKeyString(), out var hairGrowthStates);
            return new ObjectPicker.TextColumn(string.Join(", ", Array.ConvertAll(hairGrowthStates.ToString().Split(','), x => Responder.Instance.LocalizationModel.LocalizeString(mLocalizationPath + "/Options/HairGrowthState:" + x.Trim(' ')))));
        }
    }
}
