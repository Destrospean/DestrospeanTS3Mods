using System.Collections.Generic;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.UI.Columns
{
    public class TextInListColumn : ObjectPickerDialog.CommonHeaderInfo<string>
    {
        private readonly string mLocalizationPath;

        private readonly List<string> mStringsInList;

        public TextInListColumn(string localizationPath, string[] strings, int width = 440) : base(localizationPath + "/Headers/TextInList:Text", localizationPath + "/Headers/TextInList:Tooltip", width)
        {
            mLocalizationPath = localizationPath;
            mStringsInList = new List<string>(strings);
        }

        public override ObjectPicker.ColumnInfo GetValue(string text)
        {
            return new ObjectPicker.TextColumn(Responder.Instance.LocalizationModel.LocalizeString(mLocalizationPath + "/Options/TextInList:" + mStringsInList.Contains(text)));
        }
    }
}
