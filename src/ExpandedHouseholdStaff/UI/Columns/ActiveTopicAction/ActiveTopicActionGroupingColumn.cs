using Sims3.Gameplay.Utilities;
using Sims3.UI.Controller;
using Sims3.UI;
using Destrospean.Enums;
using Destrospean.Utils;
using Destrospean.Utils.ExpandedHouseholdStaff;

namespace Destrospean.UI.Columns.ExpandedHouseholdStaff
{
    public class ActiveTopicActionGroupingColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<ServiceUtils.ActiveTopicAction>
    {
        public ActiveTopicActionGroupingColumn(string localizationPath) : base(localizationPath + "/Headers/Grouping:Text", localizationPath + "/Headers/Grouping:Tooltip", 220)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(ServiceUtils.ActiveTopicAction action)
        {
            return new ObjectPicker.TextColumn(action.Grouping == LongTermRelationshipTypes.BestFriendsForever ? Localization.LocalizeString(0xC86B0C71108DA632) : Localization.LocalizeString(((action.Grouping == LongTermRelationshipTypes.All || action.Grouping == LongTermRelationshipTypes.Default || action.Grouping == LongTermRelationshipTypes.Ex || action.Grouping == LongTermRelationshipTypes.ExSpouse) ? typeof(Dialogs.ComboSelectionDialog).GetLocalizationKey().Replace("ComboSelectionDialog", "LongTermRelationshipTypeDialog/Options:") : "Gameplay/Excel/Socializing/LTR:") + action.Grouping));
        }
    }
}
