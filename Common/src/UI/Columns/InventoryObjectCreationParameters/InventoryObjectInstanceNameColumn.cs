using Destrospean.Misc;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.UI.Columns
{
    public class InventoryObjectInstanceNameColumn : ObjectPickerDialog.CommonHeaderInfo<InventoryObjectCreationParameters>
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
