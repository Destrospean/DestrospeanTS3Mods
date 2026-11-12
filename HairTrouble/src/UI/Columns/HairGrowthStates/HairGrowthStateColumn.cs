using Destrospean.Enums.HairTrouble;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.UI.Columns.HairTrouble
{
    public class HairGrowthStateColumn : ObjectPickerDialog.CommonHeaderInfo<HairGrowthStates>
    {
        private readonly string mLocalizationPath;

        public HairGrowthStateColumn(string localizationPath) : base(localizationPath + "/Headers/HairGrowthState:Text", localizationPath + "/Headers/HairGrowthState:Tooltip", 400)
        {
            mLocalizationPath = localizationPath;
        }

        public override ObjectPicker.ColumnInfo GetValue(HairGrowthStates hairGrowthState)
        {
            return new ObjectPicker.TextColumn(Responder.Instance.LocalizationModel.LocalizeString(mLocalizationPath + "/Options/HairGrowthState:" + hairGrowthState));
        }
    }
}
