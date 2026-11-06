using System;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.UI.Columns
{
    public class TypeColumn : ObjectPickerDialog.CommonHeaderInfo<Type>
    {
        public TypeColumn(string localizationPath) : base(localizationPath + "/Headers/Type:Text", localizationPath + "/Headers/Type:Tooltip", 440)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(Type type)
        {
            return new ObjectPicker.TextColumn(type?.FullName ?? "");
        }
    }
}
