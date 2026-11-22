using System;
using System.Collections.Generic;
using Destrospean.Enums;
using Destrospean.Misc;
using Destrospean.Utils;
using Sims3.Gameplay.Utilities;

namespace Destrospean.UI.Dialogs
{
    public static class ActiveTopicActionActivenessDialog
    {
        public static bool TryUIGetActiveTopicActionActiveness(string title, out ActiveTopicActionActiveness activeTopicActionActiveness, ActiveTopicActionActiveness defaultValue = ActiveTopicActionActiveness.SPA)
        {
            var entryKey = typeof(ActiveTopicActionActivenessDialog).GetLocalizationKey() + "/Options:";
            if (ComboSelectionDialog.Show(title, new SortedDictionary<string, object>(new DummyComparer())
                {
                    {
                        Localization.LocalizeString(entryKey + ActiveTopicActionActiveness.FPA),
                        nameof(ActiveTopicActionActiveness.FPA)
                    },
                    {
                        Localization.LocalizeString(entryKey + ActiveTopicActionActiveness.SPA),
                        nameof(ActiveTopicActionActiveness.SPA)
                    }
                }, defaultValue.ToString()) is string text)
            {
                activeTopicActionActiveness = (ActiveTopicActionActiveness)Enum.Parse(typeof(ActiveTopicActionActiveness), text);
                return true;
            }
            activeTopicActionActiveness = defaultValue;
            return false;
        }
    }
}
