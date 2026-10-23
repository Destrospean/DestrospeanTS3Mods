using Sims3.UI;
using Destrospean.Utils.ExpandedHouseholdStaff;

namespace Destrospean.UI.Columns.ExpandedHouseholdStaff
{
    public class ActiveTopicActionColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<ServiceUtils.ActiveTopicAction>
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
