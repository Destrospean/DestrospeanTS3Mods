using System.Collections.Generic;
using Sims3.Gameplay.ActorSystems;
using Sims3.UI;

namespace Destrospean.UI.Columns
{
    public class TraitEnabledColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<Trait>
    {
        private readonly string mLocalizationPath;

        private readonly List<Trait> mTraitsEnabled;

        public TraitEnabledColumn(string localizationPath, Trait[] traitsEnabled) : base(localizationPath + "/Headers/Enabled:Text", localizationPath + "/Headers/Enabled:Tooltip", 40)
        {
            mLocalizationPath = localizationPath;
            mTraitsEnabled = new List<Trait>(traitsEnabled);
        }

        public override ObjectPicker.ColumnInfo GetValue(Trait trait)
        {
            return new ObjectPicker.TextColumn(Responder.Instance.LocalizationModel.LocalizeString(mLocalizationPath + "/Options/Enabled:" + mTraitsEnabled.Contains(trait)));
        }
    }
}
