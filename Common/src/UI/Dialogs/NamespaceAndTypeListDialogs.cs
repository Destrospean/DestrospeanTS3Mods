using System;
using System.Collections.Generic;
using Destrospean.UI.Columns;
using Destrospean.Utils;
using Sims3.UI;

namespace Destrospean.UI.Dialogs
{
    public static class NamespaceAndTypeListDialogs
    {
        public static bool TryUIGetSelectedTypes(out Type[] selectedTypes, Type[] allTypes, string namespaceListTitle = null, string typeListTitle = null, int selectableRowCount = int.MaxValue)
        {
            var entryKey = typeof(NamespaceAndTypeListDialogs).GetLocalizationKey().Replace(nameof(NamespaceAndTypeListDialogs), "");
            Array.Sort(allTypes, (a, b) => string.Compare(a.FullName, b.FullName, StringComparison.Ordinal));
            var namespaces = new List<string>();
            foreach (var type in allTypes)
            {
                if (!namespaces.Contains(type.Namespace))
                {
                    namespaces.Add(type.Namespace);
                }
            }
            while (true)
            {
                var selectedNamespaces = ObjectPickerDialog.Show(namespaceListTitle ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + "NamespaceListDialog:Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), namespaces.ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<string>>
                    {
                        new TextColumn(entryKey + "NamespaceListDialog")
                    }, 1, out var confirmed, out var cancelled);
                if (cancelled)
                {
                    selectedTypes = null;
                    return false;
                }
                selectedTypes = (ObjectPickerDialog.Show(typeListTitle ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + "TypeListDialog:Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", selectedNamespaces[0], new List<Type>(allTypes).FindAll(x => x.Namespace == selectedNamespaces[0]).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<Type>>
                    {
                        new TypeColumn(entryKey + "TypeListDialog")
                    }, selectableRowCount, out confirmed, out cancelled) ?? new List<Type>()).ToArray();
                if (confirmed)
                {
                    return true;
                }
            }
        }
    }
}
