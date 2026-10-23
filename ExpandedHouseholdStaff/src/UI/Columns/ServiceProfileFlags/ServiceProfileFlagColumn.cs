using Sims3.UI;
using Destrospean.Enums.ExpandedHouseholdStaff;

namespace Destrospean.UI.Columns.ExpandedHouseholdStaff
{
    public class ServiceProfileFlagColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<ServiceProfileFlags>
    {
        readonly string mLocalizationPath;

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
