using Sims3.UI;
using Destrospean.Misc;

namespace Destrospean.UI.Columns.ExpandedHouseholdStaff
{
    public class InventoryObjectCountColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<InventoryObjectCreationParameters>
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
