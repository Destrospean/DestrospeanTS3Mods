using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CustomContent;
using Sims3.UI;
using System.Collections.Generic;
using Destrospean.Utils;
using Destrospean.Utils.ExpandedHouseholdStaff;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.ExpandedHouseholdStaff.Interactions
{
    public class CloneServiceProfile : ImmediateInteraction<Sim, GameObject>
    {
        [DoesntRequireTuning]
        public class Definition : ImmediateInteractionDefinition<Sim, GameObject, CloneServiceProfile>
        {
            public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
            {
                return Localization.LocalizeString(actor.IsFemale, LocalizationKey + ":Name");
            }

            public override string[] GetPath(bool isFemale)
            {
                return new[]
                {
                    Localization.LocalizeString(isFemale, LocalizationKey + ":Path")
                };
            }

            public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return !isAutonomous && ServiceUtils.ServiceProfiles.Count > 0;
            }
        }

        public static readonly string LocalizationKey = typeof(CloneServiceProfile).GetLocalizationKey();

        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            while (true)
            {
                IServiceProfile[] profiles;
                if (!ServiceUtils.TryUIGetSelectedServiceProfiles(out profiles, ServiceUtils.ServiceProfiles.ToArray(), Localization.LocalizeString(LocalizationKey + ":Name"), 1))
                {
                    return true;
                }
                string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "CloneServiceProfileDialog");
                IServiceProfile profile;
                string name = StringInputDialog.Show(Localization.LocalizeString(entryKey + ":Title"), Localization.LocalizeString(entryKey + ":Prompt"), profiles[0].Name + "_" + DownloadContent.GenerateGUID(), -1, ThumbnailKey.kInvalidThumbnailKey, new Vector2(-1, -1), StringInputDialog.Validation.None, false, ModalDialog.PauseMode.PauseSimulator, false, true);
                if (name == null)
                {
                    continue;
                }
                if (profiles[0].TryUICloneServiceProfile(name, out profile))
                {
                    profile.AddServiceToSaveGame();
                    foreach (OutfitAssignmentUtils.OutfitAssignment outfitAssignment in new List<OutfitAssignmentUtils.OutfitAssignment>(OutfitAssignmentUtils.OutfitAssignments))
                    {
                        if (outfitAssignment.ServiceName == profiles[0].Name)
                        {
                            string specialOutfitKey = outfitAssignment.SpecialOutfitKey.Replace(profiles[0].Name, profile.Name);
                            OutfitAssignmentUtils.OutfitAssignments.Add(new OutfitAssignmentUtils.OutfitAssignment(outfitAssignment.SimDescription, specialOutfitKey, profile));
                            OutfitAssignmentUtils.AssignedOutfits[specialOutfitKey] = new OutfitAssignmentUtils.AssignedOutfit(OutfitAssignmentUtils.AssignedOutfits[outfitAssignment.SpecialOutfitKey]);
                        }
                    }
                    OutfitAssignmentUtils.IndexOutfitAssignments();
                }
                return true;
            }
        }
    }
}
