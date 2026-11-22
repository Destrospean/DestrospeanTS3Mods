using System;
using System.Collections.Generic;
using Destrospean.UI.Columns;
using Destrospean.Utils;
using Sims3.SimIFace.CAS;
using Sims3.UI;

namespace Destrospean.UI.Dialogs
{
    public static class CASAgeGenderFlagListDialog
    {
        public static bool ShowCASAgeGenderFlagListDialog(out CASAgeGenderFlags flags, CASAgeGenderFlags? preSelectedFlags = null, CASAgeGenderFlags mask = CASAgeGenderFlags.AgeMask | CASAgeGenderFlags.GenderMask, string title = null, string entryKey = null, bool okayButtonAlwaysEnabled = true)
        {
            flags = CASAgeGenderFlags.None;
            entryKey = entryKey ?? typeof(CASAgeGenderFlagListDialog).GetLocalizationKey();
            var flagList = new List<CASAgeGenderFlags>(Array.FindAll((CASAgeGenderFlags[])Enum.GetValues(typeof(CASAgeGenderFlags)), x => preSelectedFlags.HasValue ? (preSelectedFlags & mask & x) == x && x != CASAgeGenderFlags.None && x != CASAgeGenderFlags.AgeMask && x != CASAgeGenderFlags.GenderMask : false));
            while (true)
            {
                if (flagList.Count == 0 && okayButtonAlwaysEnabled)
                {
                    foreach (CASAgeGenderFlags flag in Enum.GetValues(typeof(CASAgeGenderFlags)))
                    {
                        if ((mask & flag) == flag && flag != CASAgeGenderFlags.None && flag != CASAgeGenderFlags.AgeMask && flag != CASAgeGenderFlags.GenderMask)
                        {
                            flagList.Add(flag);
                        }
                    }
                }
                var selectedFlags = ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + ":Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), new List<CASAgeGenderFlags>(Array.FindAll((CASAgeGenderFlags[])Enum.GetValues(typeof(CASAgeGenderFlags)), x => (x & mask) == x && x != CASAgeGenderFlags.None && x != CASAgeGenderFlags.AgeMask && x != CASAgeGenderFlags.GenderMask)).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, okayButtonAlwaysEnabled ? new List<ObjectPickerDialog.CommonHeaderInfo<CASAgeGenderFlags>>
                    {
                        new CASAgeGenderFlagColumn(entryKey),
                        new CASAgeGenderFlagEnabledColumn(entryKey, flagList.ToArray())
                    } : new List<ObjectPickerDialog.CommonHeaderInfo<CASAgeGenderFlags>>
                    {
                        new CASAgeGenderFlagColumn(entryKey),
                    }, 1, out var confirmed, out var cancelled, okayButtonAlwaysEnabled);
                if (cancelled)
                {
                    return false;
                }
                if (confirmed)
                {
                    if (!okayButtonAlwaysEnabled)
                    {
                        flagList.Add(selectedFlags[0]);
                    }
                    foreach (var flag in flagList)
                    {
                        flags |= flag;
                    }
                    return true;
                }
                if (flagList.Contains(selectedFlags[0]))
                {
                    flagList.Remove(selectedFlags[0]);
                }
                else
                {
                    flagList.Add(selectedFlags[0]);
                }
            }
        }
    }
}
