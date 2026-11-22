using System;
using System.Collections.Generic;
using Destrospean.Misc;
using Destrospean.Utils;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace.CAS;
using Sims3.UI.Controller;

namespace Destrospean.UI.Dialogs
{
    public static class LongTermRelationshipTypeDialog
    {
        public static bool TryUIGetLongTermRelationshipType(string title, CASAgeGenderFlags gender, out LongTermRelationshipTypes longTermRelationshipType, LongTermRelationshipTypes defaultValue = LongTermRelationshipTypes.Default)
        {
            var entryKey = typeof(LongTermRelationshipTypeDialog).GetLocalizationKey() + "/Options:";
            const string longTermRelationshipEntryKey = "Gameplay/Excel/Socializing/LTR:";
            if (ComboSelectionDialog.Show(title, new SortedDictionary<string, object>(new DummyComparer())
                {
                    {
                        Localization.LocalizeString(entryKey + LongTermRelationshipTypes.All),
                        nameof(LongTermRelationshipTypes.All)
                    },
                    {
                        Localization.LocalizeString(entryKey + LongTermRelationshipTypes.Default),
                        nameof(LongTermRelationshipTypes.Default)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.Stranger),
                        nameof(LongTermRelationshipTypes.Stranger)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.Acquaintance),
                        nameof(LongTermRelationshipTypes.Acquaintance)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.DistantFriend),
                        nameof(LongTermRelationshipTypes.DistantFriend)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.Friend),
                        nameof(LongTermRelationshipTypes.Friend)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.GoodFriend),
                        nameof(LongTermRelationshipTypes.GoodFriend)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.BestFriend),
                        nameof(LongTermRelationshipTypes.BestFriend)
                    },
                    {
                        Localization.LocalizeString(0xC86B0C71108DA632),
                        nameof(LongTermRelationshipTypes.BestFriendsForever)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.OldFriend),
                        nameof(LongTermRelationshipTypes.OldFriend)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.RomanticInterest),
                        nameof(LongTermRelationshipTypes.RomanticInterest)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.Partner),
                        nameof(LongTermRelationshipTypes.Partner)
                    },
                    {
                        Localization.LocalizeString((gender & CASAgeGenderFlags.Male) == 0 ? 0x560E2FCA95B005B2 : 0xA85D4CDDDC0663AE),
                        nameof(LongTermRelationshipTypes.Fiancee)
                    },
                    {
                        Localization.LocalizeString(entryKey + LongTermRelationshipTypes.Spouse),
                        nameof(LongTermRelationshipTypes.Spouse)
                    },
                    {
                        Localization.LocalizeString(entryKey + LongTermRelationshipTypes.Ex),
                        nameof(LongTermRelationshipTypes.Ex)
                    },
                    {
                        Localization.LocalizeString(entryKey + LongTermRelationshipTypes.ExSpouse),
                        nameof(LongTermRelationshipTypes.ExSpouse)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.Disliked),
                        nameof(LongTermRelationshipTypes.Disliked)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.Enemy),
                        nameof(LongTermRelationshipTypes.Enemy)
                    },
                    {
                        Localization.LocalizeString(longTermRelationshipEntryKey + LongTermRelationshipTypes.OldEnemies),
                        nameof(LongTermRelationshipTypes.OldEnemies)
                    }
                }, defaultValue.ToString()) is string text)
            {
                
                longTermRelationshipType = (LongTermRelationshipTypes)Enum.Parse(typeof(LongTermRelationshipTypes), text);
                return true;
            }
            longTermRelationshipType = defaultValue;
            return false;
        }
    }
}
