using Destrospean.Enums.ExpandedHouseholdStaff;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.UI.Columns.ExpandedHouseholdStaff
{
    public class ServiceProfileFlagColumn : ObjectPickerDialog.CommonHeaderInfo<ServiceProfileFlags>
    {
        private readonly string mLocalizationPath;

        public ServiceProfileFlagColumn(string localizationPath) : base(localizationPath + "/Headers/ServiceProfileFlag:Text", localizationPath + "/Headers/ServiceProfileFlag:Tooltip", 400)
        {
            mLocalizationPath = localizationPath;
        }

        public override ObjectPicker.ColumnInfo GetValue(ServiceProfileFlags flag)
        {
            return new ObjectPicker.TextColumn(Responder.Instance.LocalizationModel.LocalizeString(mLocalizationPath + "/Options/ServiceProfileFlag:" + flag));
        }
    }
}
