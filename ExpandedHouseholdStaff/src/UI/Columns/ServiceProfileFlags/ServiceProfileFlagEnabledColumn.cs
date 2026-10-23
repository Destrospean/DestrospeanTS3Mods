using Sims3.UI;
using System.Collections.Generic;
using Destrospean.Enums.ExpandedHouseholdStaff;

namespace Destrospean.UI.Columns.ExpandedHouseholdStaff
{
    public class ServiceProfileFlagEnabledColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<ServiceProfileFlags>
    {
        readonly string mLocalizationPath;

        readonly List<ServiceProfileFlags> mFlagsEnabled;

        public ServiceProfileFlagEnabledColumn(string localizationPath, ServiceProfileFlags[] flagsEnabled) : base(localizationPath + "/Headers/Enabled:Text", localizationPath + "/Headers/Enabled:Tooltip", 40)
        {
            mLocalizationPath = localizationPath;
            mFlagsEnabled = new List<ServiceProfileFlags>(flagsEnabled);
        }

        public override ObjectPicker.ColumnInfo GetValue(ServiceProfileFlags flag)
        {
            return new ObjectPicker.TextColumn(Responder.Instance.LocalizationModel.LocalizeString(mLocalizationPath + "/Options/Enabled:" + mFlagsEnabled.Contains(flag)));
        }
    }
}
