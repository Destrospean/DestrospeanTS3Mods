using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.UI;

namespace Destrospean.UI.Columns.ExpandedHouseholdStaff
{
    public class ServiceProfileNameColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<IServiceProfile>
    {
        public ServiceProfileNameColumn(string localizationPath) : base(localizationPath + "/Headers/Name:Text", localizationPath + "/Headers/Name:Tooltip", 220)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(IServiceProfile profile)
        {
            return new ObjectPicker.TextColumn(profile?.Name ?? "");
        }
    }
}
