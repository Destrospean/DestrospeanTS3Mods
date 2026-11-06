using Destrospean.Misc;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.UI.Columns.ExpandedHouseholdStaff
{
    public class InventoryObjectCountColumn : ObjectPickerDialog.CommonHeaderInfo<InventoryObjectCreationParameters>
    {
        public InventoryObjectCountColumn(string localizationPath) : base(localizationPath + "/Headers/Count:Text", localizationPath + "/Headers/Count:Tooltip", 220)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(InventoryObjectCreationParameters inventoryObjectCreationParameters)
        {
            return new ObjectPicker.TextColumn(inventoryObjectCreationParameters?.Count.ToString() ?? "0");
        }
    }
}
