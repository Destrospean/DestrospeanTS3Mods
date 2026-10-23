using Sims3.UI;
using System;

namespace Destrospean.UI.Columns
{
    public class TypeColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<Type>
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
