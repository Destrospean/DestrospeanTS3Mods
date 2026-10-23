using Sims3.Gameplay.ActorSystems;
using Sims3.SimIFace;
using Sims3.UI;

namespace Destrospean.UI.Columns
{
    public class TraitColumn : Dialogs.ObjectPickerDialog.CommonHeaderInfo<Trait>
    {
        readonly bool mIsFemale;

        public TraitColumn(string localizationPath, bool isFemale = false) : base(localizationPath + "/Headers/Trait:Text", localizationPath + "/Headers/Trait:Tooltip", 400)
        {
            mIsFemale = isFemale;
        }

        public override ObjectPicker.ColumnInfo GetValue(Trait trait)
        {
            string name = trait.TraitName(mIsFemale);
            return new ObjectPicker.ThumbAndTextColumn(new ThumbnailKey(trait.IconKey, ThumbnailSize.Large), string.IsNullOrEmpty(name) ? trait.Guid.ToString() : name);
        }
    }
}
