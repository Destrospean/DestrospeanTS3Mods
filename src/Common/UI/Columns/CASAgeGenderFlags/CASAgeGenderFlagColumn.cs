using Sims3.SimIFace.CAS;
using Sims3.UI;

namespace Destrospean.UI.Columns
{
    public class CASAgeGenderFlagColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<CASAgeGenderFlags>
    {
        readonly string mLocalizationPath;

        public CASAgeGenderFlagColumn(string localizationPath) : base(localizationPath + "/Headers/CASAgeGenderFlag:Text", localizationPath + "/Headers/CASAgeGenderFlag:Tooltip", 400)
        {
            mLocalizationPath = localizationPath;
        }

        public override ObjectPicker.ColumnInfo GetValue(CASAgeGenderFlags flag)
        {
            return new ObjectPicker.TextColumn(Responder.Instance.LocalizationModel.LocalizeString(mLocalizationPath + "/Options/CASAgeGenderFlag:" + flag));
        }
    }
}
