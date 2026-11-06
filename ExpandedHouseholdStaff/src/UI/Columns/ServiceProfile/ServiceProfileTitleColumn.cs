using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.UI.Columns.ExpandedHouseholdStaff
{
    public class ServiceProfileTitleColumn : ObjectPickerDialog.CommonHeaderInfo<IServiceProfile>
    {
        public ServiceProfileTitleColumn(string localizationPath) : base(localizationPath + "/Headers/Title:Text", localizationPath + "/Headers/Title:Tooltip", 220)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(IServiceProfile profile)
        {
            return new ObjectPicker.TextColumn(profile?.Title ?? "");
        }
    }
}
