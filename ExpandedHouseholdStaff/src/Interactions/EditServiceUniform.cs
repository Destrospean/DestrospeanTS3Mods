using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using System.Collections.Generic;
using Destrospean.Misc;
using Destrospean.Utils;
using Destrospean.Utils.ExpandedHouseholdStaff;
using Gameflow = Sims3.Gameplay.Gameflow;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.ExpandedHouseholdStaff.Interactions
{
    public class EditServiceUniform : ImmediateInteraction<Sim, GameObject>
    {
        [DoesntRequireTuning]
        public class Definition : ImmediateInteractionDefinition<Sim, GameObject, EditServiceUniform>
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
                return !isAutonomous && ServiceUtils.ServiceProfiles.Exists(x => !x.IsImmutable);
            }
        }

        public static readonly string LocalizationKey = typeof(EditServiceUniform).GetLocalizationKey();

        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            bool retVal;
            return !DebugUtils.TryDisplayScriptError(() =>
                {
                    IServiceProfile[] profiles;
                    if (ServiceUtils.TryUIGetSelectedServiceProfiles(out profiles, ServiceUtils.ServiceProfiles.FindAll(x => !x.IsImmutable).ToArray(), Localization.LocalizeString(LocalizationKey + ":Name"), 1))
                    {
                        string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "ServiceOutfitDemographicDialog");
                        CASAgeGenderFlags age;
                        if (!CommonUtils.ShowCASAgeGenderFlagListDialog(out age, CASAgeGenderFlags.None, (profiles[0].ValidAges & CASAgeGenderFlags.YoungAdult) == 0 ? profiles[0].ValidAges : profiles[0].ValidAges ^ CASAgeGenderFlags.YoungAdult | CASAgeGenderFlags.Adult, Localization.LocalizeString(entryKey + "/Titles:Age"), entryKey, false))
                        {
                            return true;
                        }
                        CASAgeGenderFlags gender;
                        if (!CommonUtils.ShowCASAgeGenderFlagListDialog(out gender, CASAgeGenderFlags.None, profiles[0].ValidGenders == CASAgeGenderFlags.None ? CASAgeGenderFlags.GenderMask : profiles[0].ValidGenders, Localization.LocalizeString(entryKey + "/Titles:Gender"), entryKey, false))
                        {
                            return true;
                        }
                        /*
                        SimDescription simDescription;
                        using (SimBuilder simBuilder = new SimBuilder
                            {
                                UseCompression = true
                            })
                        {
                            WorldName homeWorld = GameUtils.GetCurrentWorld();
                            simDescription = Genetics.MakeSim(simBuilder, age, gender, new ResourceKey(0x4AC87CCEBDD2B1A2, 0x0354796A, 0x00000000), 0.28f, Genetics.GetGeneticHairColor(homeWorld), homeWorld, uint.MaxValue, false);
                        }
                        */
                        SimDescription simDescription = Genetics.MakeSim(age, gender, GameUtils.GetCurrentWorld(), uint.MaxValue);
                        EventTracker.AddListener(EventTypeId.kSimInstantiated, evt =>
                            {
                                ListenerAction listenerAction = ListenerAction.Keep;
                                DebugUtils.TryDisplayScriptError(() =>
                                    {
                                        if (evt.TargetObject as Sim != simDescription.CreatedSim)
                                        {
                                            return;
                                        }
                                        string specialOutfitKey = simDescription.GetGlobalAssignedOutfitPrefix() + profiles[0].Name;
                                        OutfitAssignmentUtils.OutfitAssignment outfitAssignment;
                                        BodyTypes[] preSelectedPartOverrides = null;
                                        if (simDescription.TryGetGlobalOutfitAssignment(profiles[0], out outfitAssignment))
                                        {
                                            simDescription.AddAssignedOutfit(OutfitAssignmentUtils.AssignedOutfits[outfitAssignment.SpecialOutfitKey], specialOutfitKey);
                                            preSelectedPartOverrides = OutfitAssignmentUtils.AssignedOutfits[specialOutfitKey].PartOverrides.ToArray();
                                        }
                                        Gameflow.GameSpeed previousGameSpeed = Gameflow.CurrentGameSpeed;
                                        if (OutfitExtensions.EditSpecialOutfit(simDescription.CreatedSim, specialOutfitKey))
                                        {
                                            OutfitAssignmentUtils.AssignOutfitToService(null, specialOutfitKey, profiles[0], simDescription);
                                            ActionTask.Start(() => DebugUtils.TryDisplayScriptError(() =>
                                                {
                                                    BodyTypes[] partOverrides;
                                                    if (OutfitAssignmentUtils.ShowPartOverrideListDialog(OutfitAssignmentUtils.AssignedOutfits[specialOutfitKey] = new OutfitAssignmentUtils.AssignedOutfit(simDescription.GetSpecialOutfit(specialOutfitKey)), out partOverrides, preSelectedPartOverrides))
                                                    {
                                                        OutfitAssignmentUtils.AssignedOutfits[specialOutfitKey].PartOverrides = new List<BodyTypes>(partOverrides);
                                                    }
                                                    Gameflow.SetGameSpeed(previousGameSpeed, Gameflow.SetGameSpeedContext.GameStates);
                                                }));
                                        }
                                        simDescription.CreatedSim.RemoveFromWorld();
                                        AlarmManager.Global.AddAlarm(1, TimeUnit.Minutes, () =>
                                            {
                                                ulong simDescriptionId = simDescription.SimDescriptionId;
                                                Household.TouristHousehold.RemoveTemporary(simDescription);
                                                simDescription.Dispose();
                                            }, "Remove SimDescription: " + simDescription.SimDescriptionId, AlarmType.AlwaysPersisted, simDescription);
                                        Gameflow.SetGameSpeed(Gameflow.GameSpeed.Pause, Gameflow.SetGameSpeedContext.GameStates);
                                        listenerAction = ListenerAction.Remove;
                                    });
                                return listenerAction;
                            });
                        Household.CreateTouristHousehold();
                        Household.TouristHousehold.AddTemporary(simDescription);
                        simDescription.Init(simDescription.GetOutfit(OutfitCategories.Everyday, 0));
                        simDescription.Instantiate(Actor.LotHome);
                    }
                    return true;
                }, out retVal) && retVal;
        }
    }
}
