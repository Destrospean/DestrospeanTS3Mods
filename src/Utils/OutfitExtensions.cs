using Sims3.Gameplay;
﻿using Sims3.Gameplay.Actors;
using Sims3.Gameplay.CAS;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI.CAS;

namespace Destrospean.Utils
{
    public static class OutfitExtensions
    {
        public delegate bool EditSpecialOutfitFunc(Sim sim, string specialOutfitKey);

        public delegate SimOutfit OutfitFunc(SimBuilder simBuilder, OutfitCategories outfitCategory, int outfitIndex);

        public static EditSpecialOutfitFunc EditSpecialOutfit = (sim, specialOutfitKey) =>
            {
                SimDescription simDescription = sim.SimDescription;
                if (!simDescription.HasSpecialOutfit(specialOutfitKey))
                {
                    simDescription.AddSpecialOutfit(simDescription.GetOutfit(OutfitCategories.Everyday, 0), specialOutfitKey);
                }
                OutfitCategories previousOutfitCategory = sim.CurrentOutfitCategory;
                int previousOutfitIndex = sim.CurrentOutfitIndex;
                simDescription.AddOutfit(simDescription.GetSpecialOutfit(specialOutfitKey), OutfitCategories.Everyday, 0);
                simDescription.RemoveSpecialOutfit(specialOutfitKey);
                sim.SwitchToOutfitWithoutSpin(OutfitCategories.Everyday, 0);
                CASLogic casLogic = CASLogic.GetSingleton();
                casLogic.ShowUI += OnShowUI;
                casLogic.UseTempSimDesc = true;
                casLogic.LoadSim(simDescription, sim.CurrentOutfitCategory, sim.CurrentOutfitIndex);
                CASChangeReporter.Instance.ClearChanges();
                GameStates.TransitionToCASStylistMode();
                while (GameStates.NextInWorldStateId != 0)
                {
                    Simulator.Sleep(0u);
                }
                CASChangeReporter.Instance.SendChangedEvents(sim);
                casLogic.ShowUI -= OnShowUI;
                simDescription.AddSpecialOutfit(simDescription.GetOutfit(OutfitCategories.Everyday, 0), specialOutfitKey);
                simDescription.RemoveOutfit(OutfitCategories.Everyday, 0, true);
                sim.SwitchToOutfitWithoutSpin(previousOutfitCategory, previousOutfitIndex);
                return !CASChangeReporter.Instance.CasCancelled;
            };

        public static void ApplyToAllOutfits(this SimDescription simDescription, OutfitFunc outfitFunc, bool spin = false)
        {
            using (SimBuilder simBuilder = new SimBuilder
                {
                    UseCompression = true
                })
            {
                ApplyToAllOutfits(simDescription, simBuilder, outfitFunc, spin);
            }
        }

        public static void ApplyToAllOutfits(this SimDescription simDescription, SimBuilder simBuilder, OutfitFunc outfitFunc, bool spin = false)
        {
            OutfitCategories lastOutfitCategory = 0,
            tempOutfitCategory = OutfitCategories.Everyday;
            int lastOutfitIndex = 0,
            tempOutfitIndex = simDescription.GetOutfitCount(tempOutfitCategory);
            if (simDescription.CreatedSim != null)
            {
                lastOutfitCategory = simDescription.CreatedSim.CurrentOutfitCategory;
                lastOutfitIndex = simDescription.CreatedSim.CurrentOutfitIndex;
                if (spin)
                {
                    simDescription.AddOutfit(new SimOutfit(simDescription.CreatedSim.CurrentOutfit.Key), tempOutfitCategory);
                    simDescription.CreatedSim.SwitchToOutfitWithoutSpin(tempOutfitCategory, tempOutfitIndex);
                    SimOutfit outfit = outfitFunc(simBuilder, lastOutfitCategory, lastOutfitIndex);
                    if (outfit.IsValid)
                    {
                        simDescription.ReplaceOutfit(lastOutfitCategory, lastOutfitIndex, outfit);
                        using (Sim.SwitchOutfitHelper switchOutfitHelper = new Sim.SwitchOutfitHelper(simDescription.CreatedSim, Sim.ClothesChangeReason.Force, lastOutfitCategory, lastOutfitIndex, false))
                        {
                            simDescription.CreatedSim.SwitchToOutfitWithSpin(switchOutfitHelper);
                        }
                        simDescription.RemoveOutfit(tempOutfitCategory, tempOutfitIndex, true);
                    }
                }
            }
            foreach (OutfitCategories outfitCategory in simDescription.ListOfCategories)
            {
                System.Collections.ArrayList outfits = simDescription.Outfits[outfitCategory] as System.Collections.ArrayList;
                if (outfits == null)
                {
                    continue;
                }
                for (int i = outfits.Count - 1; i > -1; i--)
                {
                    if (simDescription.CreatedSim == null || outfitCategory != lastOutfitCategory || i != lastOutfitIndex || !spin)
                    {
                        simDescription.ReplaceOutfit(outfitCategory, i, outfitFunc(simBuilder, outfitCategory, i));
                    }
                }
            }
            if (simDescription.CreatedSim != null && !spin)
            {
                simDescription.CreatedSim.UpdateOutfitInfo();
                simDescription.CreatedSim.RefreshCurrentOutfit(false);
            }
        }

        public static void OnShowUI(bool toShow)
        {
            if (!toShow)
            {
                return;
            }
            CASDresserSheet casDresserSheet = CASDresserSheet.gSingleton;
            if (casDresserSheet == null || casDresserSheet.mButtons == null)
            {
                return;
            }
            for (int i = 1; i < casDresserSheet.mButtons.Length; i++)
            {
                if (casDresserSheet.mButtons[i] != null)
                {
                    casDresserSheet.mButtons[i].Visible = false;
                }
                if (casDresserSheet.mButtonText[i] != null)
                {
                    casDresserSheet.mButtonText[i].Visible = false;
                }
            }
            CASDresserClothing casDresserClothing = CASDresserClothing.gSingleton;
            if (casDresserClothing == null || casDresserClothing.mOutfitButtons == null || casDresserClothing.mDeleteOutfitButtons == null)
            {
                return;
            }
            for (int i = 1; i < casDresserClothing.mOutfitButtons.Length; i++)
            {
                casDresserClothing.mOutfitButtons[i].Visible = false;
                casDresserClothing.mDeleteOutfitButtons[i].Visible = false;
            }
            casDresserClothing.mAddOutfitButton.Visible = false;
        }

        public static void PrepareForOutfit(this SimBuilder simBuilder, SimOutfit outfit)
        {
            simBuilder.Clear();
            OutfitUtils.SetAutomaticModifiers(simBuilder);
            OutfitUtils.SetOutfit(simBuilder, outfit, null);
        }

        public static void ReplaceOutfit(this SimDescription simDescription, OutfitCategories outfitCategory, int outfitIndex, SimOutfit newOutfit)
        {
            if (newOutfit != null && newOutfit.IsValid)
            {
                simDescription.RemoveOutfitInternal(outfitCategory, outfitIndex, true, simDescription.IsUsingMaternityOutfits);
                simDescription.AddOutfitInternal(newOutfit, outfitCategory, outfitIndex, simDescription.IsUsingMaternityOutfits, false);
            }
        }
    }
}
