using System.Collections.Generic;
using Destrospean.UI.Columns;
using Destrospean.Utils;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Utilities;
using Sims3.UI;

namespace Destrospean.UI.Dialogs
{
    public static class CommodityKindListDialog
    {
        public static bool ShowCommodityKindListDialog(out CommodityKind[] selectedCommodityKinds, CommodityKind[] allCommodityKinds, CommodityKind[] preSelectedCommodityKinds = null, string title = null, string entryKey = null)
        {
            var allNames = new List<string>();
            var preSelectedNames = new List<string>();
            foreach (var entry in ParserFunctions.sCaseSensitiveEnumParsers[typeof(CommodityKind)].mLookup)
            {
                foreach (var motive in allCommodityKinds)
                {
                    if ((int)entry.Value == (int)motive)
                    {
                        allNames.Add(entry.Key);
                    }
                }
                foreach (var motive in preSelectedCommodityKinds ?? new CommodityKind[0])
                {
                    if ((int)entry.Value == (int)motive)
                    {
                        preSelectedNames.Add(entry.Key);
                    }
                }
            }
            entryKey = entryKey ?? typeof(CommodityKindListDialog).GetLocalizationKey();
            var nameList = new List<string>(preSelectedNames);
            while (true)
            {
                var selectedNames = ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + ":Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), allNames.ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<string>>
                    {
                        new TextColumn(entryKey, 400),
                        new TextInListColumn(entryKey, nameList.ToArray(), 40)
                    }, 1, out var confirmed, out var cancelled, true);
                if (cancelled)
                {
                    selectedCommodityKinds = null;
                    return false;
                }
                if (confirmed)
                {
                    selectedCommodityKinds = nameList.ConvertAll(x => (CommodityKind)ParserFunctions.sCaseSensitiveEnumParsers[typeof(CommodityKind)].mLookup[x]).ToArray();
                    return true;
                }
                if (nameList.Contains(selectedNames[0]))
                {
                    nameList.Remove(selectedNames[0]);
                }
                else
                {
                    nameList.Add(selectedNames[0]);
                }
            }
        }
    }
}
