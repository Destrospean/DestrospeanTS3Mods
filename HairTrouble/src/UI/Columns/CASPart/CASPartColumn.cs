using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using Sims3.UI.CAS;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.UI.Columns
{
    public class CASPartColumn : ObjectPickerDialog.CommonHeaderInfo<CASPart>
    {
        public CASPartColumn(string localizationPath) : base(localizationPath + "/Headers/CASPart:Text", localizationPath + "/Headers/CASPart:Tooltip", 400)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(CASPart casPart)
        {
            var preset = new CASPartPreset(casPart, CASUtils.PartDataGetPresetId(casPart.Key, 0u), CASUtils.PartDataGetPreset(casPart.Key, 0u));
            return new ObjectPicker.ThumbAndTextColumn(new ThumbnailKey(preset.mPart.Key, preset.mPresetId != uint.MaxValue ? (int)preset.mPresetId : -1, (uint)preset.mPart.BodyType, (uint)preset.mPart.AgeGenderSpecies, ThumbnailSize.Large), CASUtils.PartDataGetName(casPart.Key));
        }
    }
}
