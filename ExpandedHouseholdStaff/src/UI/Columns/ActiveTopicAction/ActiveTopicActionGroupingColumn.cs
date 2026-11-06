using Destrospean.Utils;
using Destrospean.Utils.ExpandedHouseholdStaff;
using Sims3.Gameplay.Utilities;
using Sims3.UI;
using Sims3.UI.Controller;
using ComboSelectionDialog = Destrospean.UI.Dialogs.ComboSelectionDialog;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.UI.Columns.ExpandedHouseholdStaff
{
    public class ActiveTopicActionGroupingColumn : ObjectPickerDialog.CommonHeaderInfo<ServiceUtils.ActiveTopicAction>
    {
        public ActiveTopicActionGroupingColumn(string localizationPath) : base(localizationPath + "/Headers/Grouping:Text", localizationPath + "/Headers/Grouping:Tooltip", 220)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(ServiceUtils.ActiveTopicAction action)
        {
            return new ObjectPicker.TextColumn(action.Grouping == LongTermRelationshipTypes.BestFriendsForever ? Localization.LocalizeString(0xC86B0C71108DA632) : Localization.LocalizeString((action.Grouping == LongTermRelationshipTypes.All || action.Grouping == LongTermRelationshipTypes.Default || action.Grouping == LongTermRelationshipTypes.Ex || action.Grouping == LongTermRelationshipTypes.ExSpouse ? typeof(ComboSelectionDialog).GetLocalizationKey().Replace("ComboSelectionDialog", "LongTermRelationshipTypeDialog/Options:") : "Gameplay/Excel/Socializing/LTR:") + action.Grouping));
        }
    }
}
