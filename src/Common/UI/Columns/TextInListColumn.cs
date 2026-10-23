using Sims3.UI;
using System.Collections.Generic;

namespace Destrospean.UI.Columns
{
    public class TextInListColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<string>
    {
        readonly string mLocalizationPath;

        readonly List<string> mStringsInList;

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
