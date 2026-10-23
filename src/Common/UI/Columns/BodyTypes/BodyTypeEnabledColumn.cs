using Sims3.SimIFace.CAS;
using Sims3.UI;
using System.Collections.Generic;

namespace Destrospean.UI.Columns
{
    public class BodyTypeEnabledColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<BodyTypes>
    {
        readonly string mLocalizationPath;

        readonly List<BodyTypes> mBodyTypes;

        public BodyTypeEnabledColumn(string localizationPath, BodyTypes[] bodyTypes) : base(localizationPath + "/Headers/Enabled:Text", localizationPath + "/Headers/Enabled:Tooltip", 40)
        {
            mLocalizationPath = localizationPath;
            mBodyTypes = new List<BodyTypes>(bodyTypes);
        }

        public override ObjectPicker.ColumnInfo GetValue(BodyTypes bodyType)
        {
            return new ObjectPicker.TextColumn(Responder.Instance.LocalizationModel.LocalizeString(mLocalizationPath + "/Options/Enabled:" + mBodyTypes.Contains(bodyType)));
        }
    }
}
