using Sims3.UI;

namespace Destrospean.UI.Columns
{
    public class TextColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<string>
    {
        public TextColumn(string localizationPath, int width = 440) : base(localizationPath + "/Headers/Text:Text", localizationPath + "/Headers/Text:Tooltip", width)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(string text)
        {
            return new ObjectPicker.TextColumn(text ?? "");
        }
    }
}
