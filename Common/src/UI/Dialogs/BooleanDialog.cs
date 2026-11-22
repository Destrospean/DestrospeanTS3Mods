using System.Collections.Generic;
using Destrospean.Misc;
using Sims3.Gameplay.Utilities;

namespace Destrospean.UI.Dialogs
{
    public static class BooleanDialog
    {
        public static bool TryUIGetBooleanValue(string title, out bool boolean, bool defaultValue = false)
        {
            if (ComboSelectionDialog.Show(title, new SortedDictionary<string, object>(new DummyComparer())
                {
                    {
                        Localization.LocalizeString(0xC83C121BA92C23BF),
                        true.ToString()
                    },
                    {
                        Localization.LocalizeString(0x918EEA8E85AB6760),
                        false.ToString()
                    }
                }, defaultValue.ToString()) is string text)
            {
                boolean = bool.Parse(text);
                return true;
            }
            boolean = false;
            return false;
        }
    }
}
