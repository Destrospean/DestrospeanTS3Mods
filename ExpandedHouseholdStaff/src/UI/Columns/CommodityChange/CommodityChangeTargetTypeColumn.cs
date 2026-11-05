using Destrospean.Utils.ExpandedHouseholdStaff;
using Sims3.UI;

namespace Destrospean.UI.Columns.ExpandedHouseholdStaff
{
    public class CommodityChangeTargetTypeColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<ServiceUtils.CommodityChange>
    {
        public CommodityChangeTargetTypeColumn(string localizationPath) : base(localizationPath + "/Headers/TargetType:Text", localizationPath + "/Headers/TargetType:Tooltip", 330)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(ServiceUtils.CommodityChange output)
        {
            return new ObjectPicker.TextColumn(output?.TargetType ?? "");
        }
    }
}
