using Sims3.UI;
using Destrospean.Misc;

namespace Destrospean.UI.Columns.ExpandedHouseholdStaff
{
    public class InventoryObjectInstanceNameColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<InventoryObjectCreationParameters>
    {
        public InventoryObjectInstanceNameColumn(string localizationPath) : base(localizationPath + "/Headers/InstanceName:Text", localizationPath + "/Headers/InstanceName:Tooltip", 220)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(InventoryObjectCreationParameters inventoryObjectCreationParameters)
        {
            return new ObjectPicker.TextColumn(inventoryObjectCreationParameters?.InstanceName ?? "");
        }
    }
}
