using System;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems.Children;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Objects;
using Sims3.Gameplay.Situations;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Tuning = Sims3.Gameplay.Destrospean.OutfitAssignment;

namespace Destrospean.OutfitAssignment
{
    public class Replacements
    {
        public static void GetCategoryAndIndexToUse(Sim sim, Sim.ClothesChangeReason reason, ref OutfitCategories category, out int index)
        {
            index = -1;
            if (category == sim.CurrentOutfitCategory)
            {
                index = sim.CurrentOutfitIndex;
                return;
            }
            if (sim.SimDescription.IsVisuallyPregnant && category == OutfitCategories.Outerwear && sim.SimDescription.GetOutfitCount(category) < 1)
            {
                sim.CreateRandomOuterwear();
                index = 0;
                return;
            }
            if ((sim.SimDescription.IsVisuallyPregnant || sim.SimDescription.GetOutfitCount(category) > 0) && category != 0)
            {
                index = category == OutfitCategories.Career ? sim.SimDescription.CareerOutfitIndex : Tuning.kPickRandomOutfitIndex ? RandomUtil.GetInt(sim.SimDescription.GetOutfitCount(category) - 1) : 0;
                if (sim.BuffManager?.TransformBuffInst != null)
                {
                    sim.BuffManager.TransformBuffInst.GenerateTransformOutfit(sim.SimDescription.GetOutfit(category, index));
                }
                return;
            }
            index = Tuning.kPickRandomOutfitIndex ? RandomUtil.GetInt(sim.SimDescription.GetOutfitCount(category) - 1) : 0;
            switch (category)
            {
                case OutfitCategories.Naked:
                case OutfitCategories.Everyday:
                case OutfitCategories.Formalwear:
                case OutfitCategories.Sleepwear:
                case OutfitCategories.Swimwear:
                case OutfitCategories.Athletic:
                case OutfitCategories.Career:
                    {
                        using (var simBuilder = new SimBuilder())
                        {
                            simBuilder.TextureSize = 1024;
                            simBuilder.UseCompression = true;
                            SimOutfit newOutfit = null;
                            OutfitUtils.MakeCategoryOutfitUsingEveryday(simBuilder, category, sim.SimDescription, ref newOutfit);
                        }
                        break;
                    }
                case OutfitCategories.Outerwear:
                    if (GameUtils.IsInstalled(ProductVersion.EP8))
                    {
                        sim.CreateRandomOuterwear();
                    }
                    else
                    {
                        category = OutfitCategories.Everyday;
                    }
                    break;
                case OutfitCategories.Special:
                    if (reason == Sim.ClothesChangeReason.GoingToBed)
                    {
                        if (SlumberParty.ShouldWearSlumberPartyPajama(sim, out var tempIndex))
                        {
                            index = tempIndex;
                        }
                    }
                    break;
                default:
                    category = OutfitCategories.Everyday;
                    break;
            }
        }

        public void Init(Sim sim, Sim.ClothesChangeReason reason)
        {
            var switchOutfitHelper = (Sim.SwitchOutfitHelper)(object)this;
            switchOutfitHelper.mInitMethod = Sim.SwitchOutfitHelper.InitMethod.SimReason;
            switchOutfitHelper.mInitReason = reason;
            switchOutfitHelper.mSim = sim;
            switchOutfitHelper.mNoChange = !switchOutfitHelper.mSim.GetOutfitForClothingChange(reason, out switchOutfitHelper.mCategory) || !switchOutfitHelper.mSim.SwitchToOutfitTraitTest(reason, ref switchOutfitHelper.mCategory) || !switchOutfitHelper.mSim.AcceptableClothingCategoryForAge(switchOutfitHelper.mCategory) || switchOutfitHelper.mSim.OccultManager.DisallowClothesChange();
            if (!switchOutfitHelper.mNoChange)
            {
                var category = switchOutfitHelper.mCategory;
                GetCategoryAndIndexToUse(switchOutfitHelper.mSim, reason, ref category, out var index);
                switchOutfitHelper.mCategory = category;
                switchOutfitHelper.mIndex = index;
                switchOutfitHelper.mNoChange = switchOutfitHelper.mCategory == switchOutfitHelper.mSim.CurrentOutfitCategory;
            }
        }

        public void Init(Sim sim, Sim.ClothesChangeReason reason, OutfitCategories category)
        {
            var switchOutfitHelper = (Sim.SwitchOutfitHelper)(object)this;
            switchOutfitHelper. mInitMethod = Sim.SwitchOutfitHelper.InitMethod.SimReasonCategory;
            switchOutfitHelper.mInitReason = reason;
            switchOutfitHelper.mInitCategory = category;
            switchOutfitHelper.mSim = sim;
            switchOutfitHelper.mCategory = category;
            switchOutfitHelper.mNoChange = !switchOutfitHelper.mSim.SwitchToOutfitTraitTest(reason, ref switchOutfitHelper.mCategory) || !switchOutfitHelper.mSim.AcceptableClothingCategoryForAge(switchOutfitHelper.mCategory) || switchOutfitHelper.mSim.OccultManager.DisallowClothesChange();
            if (!switchOutfitHelper.mNoChange)
            {
                var tempCategory = switchOutfitHelper.mCategory;
                GetCategoryAndIndexToUse(switchOutfitHelper.mSim, reason, ref tempCategory, out var index);
                switchOutfitHelper.mCategory = tempCategory;
                switchOutfitHelper.mIndex = index;
                switchOutfitHelper.mNoChange = switchOutfitHelper.mCategory == switchOutfitHelper.mSim.CurrentOutfitCategory;
            }
        }

        public ChildUtils.Return StartBuildingOutfit()
        {
            var changeToddlerClothes = (ChangeToddlerClothes)(object)this;
            if (changeToddlerClothes.mSwitchOutfitHelper != null)
            {
                return ChildUtils.Return.Continue;
            }
            ResourceKey outfitKey;
            if (changeToddlerClothes.Reason != Sim.ClothesChangeReason.Force)
            {
                SimOutfit simOutfit = null;
                if (!changeToddlerClothes.Target.GetOutfitForClothingChange(changeToddlerClothes.Reason, out var category))
                {
                    return ChildUtils.Return.True;
                }

                GetCategoryAndIndexToUse(changeToddlerClothes.Target, changeToddlerClothes.Reason, ref category, out var index);
                simOutfit = changeToddlerClothes.Target.SimDescription.GetOutfit(category, index);
                outfitKey = simOutfit.Key;
            }
            else
            {
                if (changeToddlerClothes.SelectedObjects == null || changeToddlerClothes.SelectedObjects.Count != 1)
                {
                    return ChildUtils.Return.False;
                }
                outfitKey = (ResourceKey)changeToddlerClothes.SelectedObjects[0];
            }
            changeToddlerClothes.mSwitchOutfitHelper = new Sim.SwitchOutfitHelper(changeToddlerClothes.Target, outfitKey);
            changeToddlerClothes.mSwitchOutfitHelper.Start();
            return ChildUtils.Return.Continue;
        }

        public bool SwitchToOutfitDelayed(Sim.ClothesChangeReason reason, CASUtils.SwapReadyCallback finishedCallback)
        {
            var sim = (Sim)(object)this;
            if (sim.GetOutfitForClothingChange(reason, out var category))
            {
                GetCategoryAndIndexToUse(sim, reason, ref category, out var index);
                return sim.SwitchToOutfitDelayed(category, index, finishedCallback);
            }
            return false;
        }

        public void SwitchToOutfitWithoutSpin(Sim.ClothesChangeReason reason, OutfitCategories category, bool ignoreCurrentCategory)
        {
            var sim = (Sim)(object)this;
            if (sim.SwitchToOutfitTraitTest(reason, ref category) && sim.AcceptableClothingCategoryForAge(category) && !sim.SimDescription.OccultManager.DisallowClothesChange() && !sim.BuffManager.DisallowClothesChange(reason, category) && (ignoreCurrentCategory || category != sim.CurrentOutfitCategory))
            {
                GetCategoryAndIndexToUse(sim, reason, ref category, out var index);
                sim.SwitchToOutfitWithoutSpin(category, index);
            }
        }

        public void SwitchToOutfitWithoutSpin(OutfitCategories category)
        {
            var sim = (Sim)(object)this;
            GetCategoryAndIndexToUse(sim, Sim.ClothesChangeReason.Force, ref category, out var index);
            sim.SwitchToOutfitWithoutSpin(category, index);
        }

        public void SwitchToOutfitWithSpin(Sim.ClothesChangeReason reason)
        {
            var sim = (Sim)(object)this;
            var didSpinAlready = false;
            if (sim.GetOutfitForClothingChange(reason, out var category))
            {
                GetCategoryAndIndexToUse(sim, reason, ref category, out _);
                didSpinAlready = sim.SwitchToOutfitWithSpin(reason, category, true);
            }
            sim.DoCarriedToddlerSpinIfNeeded(reason, didSpinAlready);
        }

        public bool SwitchToOutfitWithSpin(Sim.ClothesChangeReason reason, OutfitCategories category, bool ignoreCurrentCategory, bool placeLaundryPile, bool ignoreTraitEffects)
        {
            var sim = (Sim)(object)this;
            if ((ignoreTraitEffects || sim.SwitchToOutfitTraitTest(reason, ref category)) && sim.AcceptableClothingCategoryForAge(category) && sim.AcceptableClothingForCareer(reason) && (ignoreCurrentCategory || category != sim.CurrentOutfitCategory))
            {
                GetCategoryAndIndexToUse(sim, reason, ref category, out var index);
                return sim.SwitchToOutfitWithSpin(new Sim.SwitchOutfitHelper(sim, reason, category, index, placeLaundryPile));
            }
            return false;
        }

        public bool SwitchToOutfitWithSpin(Sim.SwitchOutfitHelper spin, bool mirrored)
        {
            var sim = (Sim)(object)this;
            if (!OutfitAssignmentUtils.TimeToChangeBackList.Contains(sim.SimDescription) && sim.CurrentInteraction != null && (sim.SimDescription.TryGetOutfitAssignment(sim.CurrentInteraction, out var outfitAssignment) || sim.SimDescription.TryGetGlobalOutfitAssignment(sim.CurrentInteraction, out outfitAssignment)))
            {
                if (OutfitAssignmentUtils.AssignedOutfits.ContainsKey(outfitAssignment.SpecialOutfitKey))
                {
                    sim.AddAssignedOutfit(outfitAssignment.SpecialOutfitKey);
                }
                string categoryForGlobalKey = null;
                var outfitCategory = outfitAssignment.SpecialOutfitKey.StartsWith(OutfitAssignmentUtils.OutfitAssignmentCategoryPrefix) || outfitAssignment.SpecialOutfitKey.StartsWith(categoryForGlobalKey = sim.GetGlobalAssignedOutfitPrefix(true)) ? (OutfitCategories)Enum.Parse(typeof(OutfitCategories), outfitAssignment.SpecialOutfitKey.Substring((categoryForGlobalKey ?? OutfitAssignmentUtils.OutfitAssignmentCategoryPrefix).Length)) : OutfitCategories.Special;
                if (outfitCategory == 0)
                {
                    return false;
                }
                sim.SimDescription.CreateOutfitForCategoryIfNecessary(outfitCategory);
                spin = new Sim.SwitchOutfitHelper(sim, outfitCategory, outfitAssignment.SpecialOutfitKey.StartsWith(categoryForGlobalKey ?? OutfitAssignmentUtils.OutfitAssignmentCategoryPrefix) ? Tuning.kPickRandomOutfitIndex ? RandomUtil.GetInt(sim.SimDescription.GetOutfitCount(outfitCategory) - 1) : 0 : sim.SimDescription.GetSpecialOutfitIndexFromKey(ResourceUtils.HashString32(outfitAssignment.SpecialOutfitKey)));
            }
            spin.Start();
            spin.Wait(true);
            if (!spin.WillChange)
            {
                return false;
            }
            if (sim.SimDescription.ChildOrAbove)
            {
                if (mirrored && !sim.SimDescription.IsHorse)
                {
                    mirrored = false;
                }
                if (sim.Posture is Hoverboard.RidingHoverboardPosture)
                {
                    spin.AddScriptEventHandler(sim.Posture.CurrentStateMachine);
                    sim.Posture.CurrentStateMachine.RequestState("x", "ChangeClothes");
                    sim.Posture.CurrentStateMachine.RequestState("x", "Hold");
                }
                else
                {
                    var stateMachineClient = StateMachineClient.Acquire(sim, "solo_generic");
                    if (string.IsNullOrEmpty(spin.OverrideAnimation))
                    {
                        stateMachineClient.SetParameter("AnimationName", "a_clothesChange" + (mirrored ? "_mirrored" : "") + (sim.IsHuman ? "" : "_x"), sim.IsHuman ? ProductVersion.BaseGame : ProductVersion.EP5);
                    }
                    else
                    {
                        stateMachineClient.SetParameter("AnimationName", spin.OverrideAnimation, spin.OverrideProductVersion);
                    }
                    spin.AddScriptEventHandler(stateMachineClient);
                    stateMachineClient.SetActor("x", sim);
                    stateMachineClient.EnterState("x", "Enter");
                    stateMachineClient.RequestState("x", "Play Animation");
                    stateMachineClient.RequestState("x", "Exit");
                }
            }
            else
            {
                spin.ChangeOutfit();
            }
            return true;
        }
    }
}
