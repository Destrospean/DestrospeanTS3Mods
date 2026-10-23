using Sims3.SimIFace;
using Sims3.UI;
using Destrospean.Misc;

namespace Destrospean.UI.Columns.ExpandedHouseholdStaff
{
    public class InventoryObjectGroupColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<InventoryObjectCreationParameters>
    {
        public InventoryObjectGroupColumn(string localizationPath) : base(localizationPath + "/Headers/Group:Text", localizationPath + "/Headers/Group:Tooltip", 220)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(InventoryObjectCreationParameters inventoryObjectCreationParameters)
        {
            return new ObjectPicker.TextColumn("0x" + (inventoryObjectCreationParameters?.GroupId ?? 0x00000000).ToString("X8"));
        }
    }
}
