using Sims3.UI;
using Destrospean.Utils.ExpandedHouseholdStaff;

namespace Destrospean.UI.Columns.ExpandedHouseholdStaff
{
    public class CommodityChangeInteractionDefinitionTypeColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<ServiceUtils.CommodityChange>
    {
        public CommodityChangeInteractionDefinitionTypeColumn(string localizationPath) : base(localizationPath + "/Headers/InteractionDefinitionType:Text", localizationPath + "/Headers/InteractionDefinitionType:Tooltip", 330)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(ServiceUtils.CommodityChange output)
        {
            return new ObjectPicker.TextColumn(output?.InteractionDefinitionType ?? "");
        }
    }
}
