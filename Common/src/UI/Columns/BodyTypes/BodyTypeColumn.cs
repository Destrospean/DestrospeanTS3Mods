using Sims3.SimIFace.CAS;
using Sims3.UI;

namespace Destrospean.UI.Columns
{
    public class BodyTypeColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<BodyTypes>
    {
        readonly string mLocalizationPath;

        public BodyTypeColumn(string localizationPath) : base(localizationPath + "/Headers/PartType:Text", localizationPath + "/Headers/PartType:Tooltip", 400)
        {
            mLocalizationPath = localizationPath;
        }

        public override ObjectPicker.ColumnInfo GetValue(BodyTypes bodyType)
        {
            return new ObjectPicker.TextColumn(Responder.Instance.LocalizationModel.LocalizeString(mLocalizationPath + "/Options/PartType:" + bodyType));
        }
    }
}
