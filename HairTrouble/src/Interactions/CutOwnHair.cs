using System;
using System.Collections.Generic;
using Destrospean.Enums.HairTrouble;
using Destrospean.HairTrouble;
using Destrospean.Utils;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.Decorations;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;

namespace Sims3.Gameplay.Destrospean.HairTrouble.Interactions
{
    public class CutOwnHair : Interaction<Sim, IMirror>
    {
        public class Definition : InteractionDefinition<Sim, IMirror, CutOwnHair>
        {
            public HairGrowthStates HairGrowthState;

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
                    if (hairGrowthState > HairGrowthStates.Unknown && hairGrowthState < HairGrowthStates.VeryLong)
                    {
                        results.Add(new InteractionObjectPair(new Definition(hairGrowthState), target));
                    }
                }
            }

            public override InteractionInstance CreateInstance(ref InteractionInstanceParameters parameters)
            {
                var cutOwnHair = new CutOwnHair();
                cutOwnHair.SetHairGrowthState(HairGrowthState);
                cutOwnHair.Init(ref parameters);
                return cutOwnHair;
            }
            
            public override string GetInteractionName(Sim actor, IMirror target, InteractionObjectPair iop)
            {
                return Localization.LocalizeString(actor.IsFemale, LocalizationKey + "/Names:" + HairGrowthState);
            }

            public override string[] GetPath(bool isFemale)
            {
                return new[]
                {
                    Localization.LocalizeString(isFemale, LocalizationKey.Replace("/CutOwnHair", ":Path")),
                    Localization.LocalizeString(isFemale, LocalizationKey + ":Path")
                };
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
                if (HairGrowthStates.Bald == actor.SimDescription.GetHairGrowthState())
                {
                    greyedOutTooltipCallback = CreateTooltipCallback(Localization.LocalizeString(actor.IsFemale, LocalizationKey + "/Tooltips:SimHasNoHair", actor));
                    return false;
                }
                if (HairGrowthState > actor.SimDescription.GetHairGrowthState())
                {
                    greyedOutTooltipCallback = CreateTooltipCallback(Localization.LocalizeString(actor.IsFemale, LocalizationKey + "/Tooltips:SimHasHairTooShort", actor));
                    return false;
                }
                if (HairGrowthState == actor.SimDescription.GetHairGrowthState())
                {
                    greyedOutTooltipCallback = CreateTooltipCallback(Localization.LocalizeString(actor.IsFemale, LocalizationKey + "/Tooltips:SimHasHairSameLength", actor));
                    return false;
                }
                return true;
            }
        }
        
        public HairGrowthStates HairGrowthState;

        public static readonly string LocalizationKey = typeof(CutOwnHair).GetLocalizationKey();

        public static InteractionDefinition Singleton = new Definition();
        
        public override bool Run()
        {
            if (!Target.Route(this))
            {
                return false;
            }
            StandardEntry();
            Mirror.EnterStateMachine(this);
            AnimateSim("ChangeAppearance");
            if (!Actor.IsSelectable)
            {
                StandardExit();
                return false;
            }
            Actor.SimDescription.SetHairGrowthState(HairGrowthState, HairGrowthStateChangeFlags.Haircut, true);
            StandardExit();
            return true;
        }
        
        public void SetHairGrowthState(HairGrowthStates hairGrowthState)
        {
            HairGrowthState = hairGrowthState;
        }
    }
}
