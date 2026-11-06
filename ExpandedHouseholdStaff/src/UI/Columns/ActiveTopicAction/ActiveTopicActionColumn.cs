using Destrospean.Utils.ExpandedHouseholdStaff;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.UI.Columns.ExpandedHouseholdStaff
{
    public class ActiveTopicActionColumn : ObjectPickerDialog.CommonHeaderInfo<ServiceUtils.ActiveTopicAction>
    {
        public ActiveTopicActionColumn(string localizationPath) : base(localizationPath + "/Headers/ActiveTopicAction:Text", localizationPath + "/Headers/ActiveTopicAction:Tooltip", 220)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(ServiceUtils.ActiveTopicAction action)
        {
            return new ObjectPicker.TextColumn(action?.Name ?? "");
        }
    }
}
