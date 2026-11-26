using System;
using System.Collections.Generic;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI;
using ObjectPickerDialog = Destrospean.OutfitAssignment.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.OutfitAssignment
{
    public static class Common
    {
        public class SimColumn : ObjectPickerDialog.CommonHeaderInfo<SimDescription>
        {
            public SimColumn(string localizationPath) : base(localizationPath + "/Header:Text", localizationPath + "/Header:Tooltip", 440)
            {
            }

            public override ObjectPicker.ColumnInfo GetValue(SimDescription simDescription)
            {
                return new ObjectPicker.ThumbAndTextColumn(simDescription.GetEverydayThumbnail(ThumbnailSize.ExtraLarge), simDescription.FullName);
            }
        }
        
        public const string LocalizationPath = "Destrospean/OutfitAssignment";

        public delegate bool SimDescriptionFilterFunc(SimDescription simDescription);

        public static SimDescription GetSimDescription(this Sim sim)
        {
            return sim?.SimDescription;
        }

        public static string Localize(string entryKey)
        {
            return Localization.LocalizeString(LocalizationPath + entryKey);
        }

        public static string Localize(string entryKey, params object[] parameters)
        {
            return Localization.LocalizeString(LocalizationPath + entryKey, parameters);
        }

        public static string Localize(bool isFemale, string entryKey, params object[] parameters)
        {
            return Localization.LocalizeString(isFemale, LocalizationPath + entryKey, parameters);
        }

        public static void Notify(string message, SimDescription simDescription, StyledNotification.NotificationStyle style)
        {
            Notify(message, simDescription, style, true);
        }

        public static void Notify(string message, SimDescription fakeSimDescription, StyledNotification.NotificationStyle style, bool checkForFake)
        {
            var simDescription = fakeSimDescription;
            if (simDescription == null)
            {
                StyledNotification.Show(new StyledNotification.Format(message, style));
                return;
            }
            if (checkForFake)
            {
                simDescription = SimDescription.Find(fakeSimDescription.SimDescriptionId);
                if (simDescription == null)
                {
                    StyledNotification.Show(new StyledNotification.Format(message, style));
                    return;
                }
            }
            StyledNotification.Show(simDescription.CreatedSim == null ? new StyledNotification.Format(message, style) : new StyledNotification.Format(message, ObjectGuid.InvalidObjectGuid, simDescription.CreatedSim.ObjectId, style));
        }

        public static bool ShowSimListDialog(out SimDescription[] selectedSims, SimDescriptionFilterFunc filterBy = null)
        {
            try
            {
                const string localizationPath = LocalizationPath + "/Dialogs/SimListDialog";
                var selectedSimList = ObjectPickerDialog.Show(Responder.Instance.LocalizationModel.LocalizeString(localizationPath + ":Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), Household.EverySimDescription().FindAll(x => filterBy?.Invoke(x) ?? true).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<SimDescription>>
                    {
                        new SimColumn(localizationPath),
                    }, int.MaxValue, out var confirmed, out _);
                if (confirmed)
                {
                    selectedSims = selectedSimList.ToArray();
                    return true;
                }
                selectedSims = null;
                return false;
            }
            catch (Exception ex)
            {
                ((IScriptErrorWindow)AppDomain.CurrentDomain.GetData("ScriptErrorWindow")).DisplayScriptError(null, ex);
                selectedSims = null;
                return false;
            }
        }
    }
}
