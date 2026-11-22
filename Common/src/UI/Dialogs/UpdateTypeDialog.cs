using System;
using System.Collections.Generic;
using Destrospean.Misc;
using Destrospean.Utils;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Utilities;

namespace Destrospean.UI.Dialogs
{
    public static class UpdateTypeDialog
    {
        public static bool TryUIGetUpdateType(string title, out OutputUpdateType updateType, OutputUpdateType defaultValue = OutputUpdateType.ContinuousFlow)
        {
            var entryKey = typeof(UpdateTypeDialog).GetLocalizationKey() + "/Options:";
            if (ComboSelectionDialog.Show(title, new SortedDictionary<string, object>(new DummyComparer())
                {
                    {
                        Localization.LocalizeString(entryKey + OutputUpdateType.ContinuousFlow),
                        OutputUpdateType.ContinuousFlow.ToString()
                    },
                    {
                        Localization.LocalizeString(entryKey + OutputUpdateType.ImmediateDelta),
                        nameof(OutputUpdateType.ImmediateDelta)
                    }
                }, defaultValue.ToString()) is string text)
            {
                updateType = (OutputUpdateType)Enum.Parse(typeof(OutputUpdateType), text);
                return true;
            }
            updateType = defaultValue;
            return false;
        }
    }
}
