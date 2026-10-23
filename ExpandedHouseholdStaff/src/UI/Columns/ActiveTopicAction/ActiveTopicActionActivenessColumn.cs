using Sims3.UI;
using Destrospean.Enums;
using Destrospean.Utils.ExpandedHouseholdStaff;
using Sims3.Gameplay.Utilities;

namespace Destrospean.UI.Columns.ExpandedHouseholdStaff
{
    public class ActiveTopicActionActivenessColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<ServiceUtils.ActiveTopicAction>
    {
        readonly string mLocalizationPath;

        public ActiveTopicActionActivenessColumn(string localizationPath) : base(localizationPath + "/Headers/Activeness:Text", localizationPath + "/Headers/Activeness:Tooltip", 220)
        {
            mLocalizationPath = localizationPath;
        }

        public override ObjectPicker.ColumnInfo GetValue(ServiceUtils.ActiveTopicAction action)
        {
            return new ObjectPicker.TextColumn(Localization.LocalizeString(mLocalizationPath + "/Options/Activeness:" + (action?.IsActive ?? false ? ActiveTopicActionActiveness.FPA : ActiveTopicActionActiveness.SPA)));
        }
    }
}
