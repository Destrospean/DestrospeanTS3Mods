using System;
using System.Collections.Generic;
using Destrospean.Enums.HairTrouble;
using Destrospean.HairTrouble;
using Destrospean.UI.Columns;
using Destrospean.Utils;
using Destrospean.Utils.HairTrouble;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.Decorations;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;

namespace Sims3.Gameplay.Destrospean.HairTrouble.Interactions
{
    public class StyleOwnHair : CutOwnHair
    {
        public new class Definition : CutOwnHair.Definition
        {
            public Definition()
            {
            }

            public Definition(HairGrowthStates hairGrowthState)
            {
                HairGrowthState = hairGrowthState;
            }
            
            public override void AddInteractions(InteractionObjectPair iop, Sim actor, IMirror target, List<InteractionObjectPair> results)
            {
                foreach (HairGrowthStates hairGrowthState in Enum.GetValues(typeof(HairGrowthStates)))
                {
                    if (hairGrowthState > HairGrowthStates.Unknown && hairGrowthState <= HairGrowthStates.VeryLong)
                    {
                        results.Add(new InteractionObjectPair(new Definition(hairGrowthState), target));
                    }
                }
            }

            public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
            {
                var styleOwnHair = new StyleOwnHair();
                styleOwnHair.SetHairGrowthState(HairGrowthState);
                styleOwnHair.Init(ref parameters);
                return styleOwnHair;
            }
            
            public override string GetInteractionName(Sim actor, IMirror target, InteractionObjectPair iop)
            {
                return Localization.LocalizeString(actor.IsFemale, LocalizationKey + "/Names:" + HairGrowthState);
            }

            public override string[] GetPath(bool isFemale)
            {
                return new[]
                {
                    Localization.LocalizeString(isFemale, LocalizationKey.Replace("/StyleOwnHair", ":Path")),
                    Localization.LocalizeString(isFemale, LocalizationKey + ":Path")
                };
            }
            
            public override void PopulatePieMenuPicker(ref InteractionInstanceParameters parameters, out List<ObjectPicker.TabInfo> selectedObjects, out List<ObjectPicker.HeaderInfo> headers, out int numSelectableRows)
            {
                numSelectableRows = 1;
                var casPartColumn = new CASPartColumn(typeof(ObjectPickerDialog).GetLocalizationKey().Replace(nameof(ObjectPickerDialog), "HairGrowthStateMapDialog"));
                headers = new List<ObjectPicker.HeaderInfo>
                    {
                        new ObjectPicker.HeaderInfo(casPartColumn.HeaderTextId, casPartColumn.HeaderTooltipId, casPartColumn.Width)
                    };
                selectedObjects = new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Localization.LocalizeString(parameters.Actor.IsFemale, LocalizationKey + "/Names:" + HairGrowthState), new List<ObjectPicker.RowInfo>())
                    };
                foreach (var hairClassification in SimHairGrowth.HairClassifications)
                {
                    if ((hairClassification.Value & ((Definition)parameters.InteractionDefinition).HairGrowthState) != HairGrowthStates.Unknown)
                    {
                        var hairstyle = new CASPart(S3PIResourceUtils.FromS3PIFormatKeyString(hairClassification.Key));
                        if (hairstyle.Key != ResourceKey.kInvalidResourceKey && (hairstyle.Age & parameters.Actor.SimDescription.Age) != CASAgeGenderFlags.None && (hairstyle.Gender & parameters.Actor.SimDescription.Gender) != CASAgeGenderFlags.None && (hairstyle.Species & parameters.Actor.SimDescription.Species) != CASAgeGenderFlags.None && (hairstyle.CategoryFlags & (uint)OutfitCategoriesExtended.IsHiddenInCAS) == 0u)
                        {
                            selectedObjects[0].RowInfo.Add(new ObjectPicker.RowInfo(hairstyle, new List<ObjectPicker.ColumnInfo>
                                {
                                    casPartColumn.GetValue(hairstyle)
                                }));
                        }
                    }
                }
            }

            public override bool Test(Sim actor, IMirror target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                if (actor.SimDescription == null || actor.CurrentOutfitCategory == OutfitCategories.Singed || actor.SimDescription.Age == CASAgeGenderFlags.Baby || actor.OccultManager.DisallowClothesChange())
                {
                    return false;
                }
                if (actor.BuffManager.HasTransformBuff())
                {
                    greyedOutTooltipCallback = CreateTooltipCallback(Localization.LocalizeString(actor.IsFemale, "Gameplay/Buffs/BuffTransformation:CantChangeClothesTooltip", actor));
                    return false;
                }
                var hairGrowthState = actor.SimDescription.GetHairGrowthState();
                if (HairGrowthState > hairGrowthState)
                {
                    greyedOutTooltipCallback = CreateTooltipCallback(Localization.LocalizeString(actor.IsFemale, LocalizationKey + "/Tooltips:SimHas" + (hairGrowthState == HairGrowthStates.Bald ? "NoHair" : "HairTooShort"), actor));
                    return false;
                }
                return true;
            }
        }

        public new static readonly string LocalizationKey = typeof(StyleOwnHair).GetLocalizationKey();

        public new static InteractionDefinition Singleton = new Definition();
        
        public override bool Run()
        {
            if (!Target.Route(this))
            {
                return false;
            }
            StandardEntry();
            Mirror.EnterStateMachine(this);
            AnimateSim("ChangeAppearance");
            if (!Actor.IsSelectable || SelectedObjects == null || SelectedObjects.Count != 1)
            {
                StandardExit();
                return false;
            }
            if (SelectedObjects[0] is CASPart hairstyle)
            {
                var hairGrowthState = Actor.SimDescription.GetHairGrowthState();
                if (HairGrowthState == hairGrowthState || SimHairGrowth.HairClassifications.TryGetValue(hairstyle.Key.ToS3PIFormatKeyString(), out var hairGrowthStates) && (hairGrowthStates & hairGrowthState) != HairGrowthStates.Unknown)
                {
                    Actor.SimDescription.ApplyToAllOutfits((simBuilder, outfitCategory, outfitIndex) =>
                        {
                            simBuilder.PrepareForOutfit(Actor.SimDescription.GetOutfit(outfitCategory, outfitIndex));
                            simBuilder.RemoveParts(BodyTypes.Hair);
                            simBuilder.AddPart(hairstyle);
                            OutfitUtils.InjectBodyHairColor(simBuilder, Actor.SimDescription.BodyHairColor.ActiveColor);
                            OutfitUtils.InjectEyeBrowHairColor(simBuilder, Actor.SimDescription.EyebrowColor.ActiveColor);
                            OutfitUtils.InjectHairColor(simBuilder, Array.ConvertAll(Actor.SimDescription.FacialHairColors, x => x.ActiveColor), BodyTypes.Beard);
                            OutfitUtils.InjectHairColor(simBuilder, Array.ConvertAll(Actor.SimDescription.HairColors, x => x.ActiveColor), BodyTypes.Hair);
                            return new SimOutfit(simBuilder.CacheOutfit($"ApplyHairGrowthState_{Actor.SimDescription.SimDescriptionId}_{outfitCategory}_{outfitIndex}"));
                        });
                }
                else
                {
                    Actor.SimDescription.SetHairGrowthState(HairGrowthState, HairGrowthStateChangeFlags.Haircut, true, hairstyle);
                }
            }
            StandardExit();
            return true;
        }
    }
}
