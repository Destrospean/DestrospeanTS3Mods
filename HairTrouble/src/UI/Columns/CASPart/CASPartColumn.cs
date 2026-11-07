using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using Sims3.UI.CAS;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.UI.Columns
{
    public class CASPartColumn : ObjectPickerDialog.CommonHeaderInfo<CASPart>
    {
        public CASPartColumn(string localizationPath) : base(localizationPath + "/Headers/CASPart:Text", localizationPath + "/Headers/CASPart:Tooltip", 220)
        {
        }

        public override ObjectPicker.ColumnInfo GetValue(CASPart casPart)
        {
            var index = CASUtils.PartDataNumPresets(casPart.Key) == 0u ? uint.MaxValue : 0u;
            var preset = new CASPartPreset(casPart, CASUtils.PartDataGetPresetId(casPart.Key, index), CASUtils.PartDataGetPreset(casPart.Key, index));
            return new ObjectPicker.ThumbAndTextColumn(new ThumbnailKey(preset.mPart.Key, preset.mPresetId != uint.MaxValue ? (int)preset.mPresetId : -1, (uint)preset.mPart.BodyType, (uint)preset.mPart.AgeGenderSpecies, ThumbnailSize.Large), casPart.Key.ToString());
        }
    }
}
