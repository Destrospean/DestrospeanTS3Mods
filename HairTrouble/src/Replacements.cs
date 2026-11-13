extern alias NRaasMasterController;
using System;
using System.Collections.Generic;
using Destrospean.Enums.HairTrouble;
using NRaasMasterController::NRaas;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Socializing;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.UI;
using Sims3.UI.CAS;
using Sims3.UI.Store;

namespace Destrospean.HairTrouble
{
    public class Replacements
    {
        public static Color[] InheritHairColor(SimBuilder target, SimDescription[] parentSims, Random random)
        {
            var genealogies = new List<Genealogy>();
            foreach (var parentSim in parentSims)
            {
                if (parentSim.Genealogy != null)
                {
                    genealogies.Add(parentSim.Genealogy);
                }
            }
            if (genealogies.Count == 0)
            {
                return Genetics.GeneticColors[random.Next(Genetics.GeneticColors.Length)] as Color[];
            }
            if (genealogies.Count == 1)
            {
                genealogies.Add(genealogies[0]);
            }
            var parentGenealogies = new List<Genealogy>();
            foreach (var genealogy in genealogies)
            {
                foreach (var parent in genealogy.Parents)
                {
                    if (parent.SimDescription != null)
                    {
                        parentGenealogies.Add(parent);
                    }
                }
            }
            if ((float)random.NextDouble() * 100 < Genetics.kMutateHairColorChance)
            {
                return Genetics.GeneticColors[random.Next(Genetics.GeneticColors.Length)] as Color[];
            }
            SimDescription simDescription;
            if (parentGenealogies.Count > 0 && (float)random.NextDouble() * 100 < Genetics.kHairColorChooseGrandparentChance)
            {
                simDescription = parentGenealogies[random.Next(parentGenealogies.Count)].SimDescription;
            }
            else
            {
                simDescription = genealogies[random.Next(genealogies.Count)].SimDescription;
            }
            var colors = new Color[10];
            for (var i = 0; i < 4; i++)
            {
                colors[i] = simDescription.GetOriginalHairColors()[i].Genetic;
            }
            colors[4] = simDescription.GetOriginalEyebrowColor().Genetic;
            for (var i = 0; i < 4; i++)
            {
                colors[i + 5] = simDescription.GetOriginalFacialHairColors()[i].Genetic;
            }
            colors[9] = simDescription.GetOriginalBodyHairColor().Genetic;
            return colors;
        }
        
        public static void PopulateTypesGrid(CASHair casHair)
        {
            if (casHair == null)
            {
                return;
            }
            var casModel = Responder.Instance.CASModel;
            var colors = new[]
                {
                    new Color(0u),
                    new Color(0u),
                    new Color(0u),
                    new Color(0u)
                };
            casHair.mHairTypesGrid.Clear();
            var wornPart = casHair.GetWornPart();
            var resourceKey = ResourceKey.CreateUILayoutKey("GenericCasItem", 0u);
            var isHat = false;
            var saveButtonEnabled = false;
            if (casHair.mHairType == CASHair.HairType.Hat)
            {
                casHair.mHatsShareButton.Enabled = false;
                casHair.mHatsDeleteButton.Enabled = false;
                casHair.mDesignButton.Enabled = CASHair.PartIsHat(wornPart);
                isHat = true;
            }
            var casFeaturedStoreItems = Responder.Instance.StoreUI.GetCASFeaturedStoreItems(BodyTypes.Hair, casModel.OutfitCategory, casModel.Age | casModel.Species | casModel.Gender, isHat);
            casHair.mContentTypeFilter.FilterObjects(casFeaturedStoreItems, out var shouldEnableCatalogProductFilter);
            if (!MasterController.Settings.mCompactHatCAS)
            {
                foreach (IFeaturedStoreItem featuredStoreItem in casFeaturedStoreItems)
                {
                    if (casHair.mHairType == CASHair.HairType.Hat != ((featuredStoreItem.CategoryFlags & 0x400000) != 0))
                    {
                        continue;
                    }
                    var windowByExportId = UIManager.LoadLayout(resourceKey).GetWindowByExportID(1);
                    if (windowByExportId == null)
                    {
                        continue;
                    }
                    windowByExportId.Tag = featuredStoreItem;
                    windowByExportId.GetChildByID(35u, true);
                    var window = windowByExportId.GetChildByID(32u, true) as Window;
                    if (window?.Drawable is ImageDrawable imageDrawable)
                    {
                        imageDrawable.Image = UIUtils.GetUIImageFromThumbnailKey(featuredStoreItem.ThumbKey);
                        window.Invalidate();
                    }
                    window = (Window)windowByExportId.GetChildByID(768u, true);
                    window.Tag = featuredStoreItem;
                    window.CreateTooltipCallbackFunction = casHair.StoreItemCreateTooltip;
                    window.Visible = true;
                    ((Window)windowByExportId.GetChildByID(771u, true)).Visible = featuredStoreItem.IsSale;
                    var button = (Button)windowByExportId.GetChildByID(769u, true);
                    button.Caption = featuredStoreItem.PriceString;
                    button.Tag = windowByExportId;
                    button.Click += casHair.OnBuyButtonClick;
                    button.FocusAcquired += casHair.OnBuyButtonFocusAcquired;
                    button.FocusLost += casHair.OnBuyButtonFocusLost;
                    casHair.mHairTypesGrid.AddItem(new ItemGridCellItem(windowByExportId, featuredStoreItem));
                }
            }
            var simHairGrowthState = ((CASLogic)casModel).mSimDescEditExisting.GetHairGrowthState();
            foreach (CASPart casPart in casHair.mPartsList)
            {
                if (simHairGrowthState > HairGrowthStates.Unknown && !(casPart.TryGetHairGrowthState(out var partHairGrowthState) && (simHairGrowthState & partHairGrowthState) != 0))
                {
                    continue;
                }
                var isWardrobePart = Responder.Instance.CASModel.ActiveWardrobeContains(casPart);
                var numPresets = CASUtils.PartDataNumPresets(casPart.Key);
                UIUtils.GetCustomContentType(casPart.Key);
                if (UIUtils.IsContentTypeDisabled(UIUtils.GetCustomContentType(casPart.Key)))
                {
                    continue;
                }
                ObjectDesigner.SetCASPart(casPart.Key);
                var defaultPresetString = ObjectDesigner.GetDesignPreset(ObjectDesigner.GetDesignPresetIndexFromId(ObjectDesigner.DefaultPresetId));
                if (string.IsNullOrEmpty(defaultPresetString))
                {
                    defaultPresetString = Simulator.LoadXMLString(new ResourceKey(casPart.Key.InstanceId, 0x0333406C, casPart.Key.GroupId));
                }
                var casPartPreset = new CASPartPreset(casPart, defaultPresetString);
                var wornPartPresetString = "";
                var replacedHairColorsPresetString = "";
                if (wornPart.Key == casPartPreset.mPart.Key)
                {
                    wornPartPresetString = Responder.Instance.CASModel.GetDesignPreset(wornPart);
                    replacedHairColorsPresetString = CASUtils.ReplaceHairColors(wornPartPresetString, colors);
                }
                if (casPartPreset.Valid && (casHair.mHairType == CASHair.HairType.Hair || ObjectDesigner.DefaultPresetId == uint.MaxValue))
                {
                    casHair.AddHairTypeGridItem(casHair.mHairTypesGrid, resourceKey, casPartPreset, isWardrobePart, ref shouldEnableCatalogProductFilter);
                    if (casPartPreset.mPart.Key == wornPart.Key && (casHair.mHairType == CASHair.HairType.Hair || CASUtils.DesignPresetCompare(wornPartPresetString, defaultPresetString)))
                    {
                        casHair.mHairTypesGrid.SelectedItem = casHair.mHairTypesGrid.Count - 1;
                        saveButtonEnabled = true;
                    }
                }
                if (casHair.mHairType != CASHair.HairType.Hat)
                {
                    continue;
                }
                if (MasterController.Settings.mCompactHatCAS)
                {
                    numPresets = 1u;
                }
                for (var i = 0u; i < numPresets; i++)
                {
                    var presetId = CASUtils.PartDataGetPresetId(casPart.Key, i);
                    UIUtils.GetCustomContentType(casPart.Key, presetId);
                    casPartPreset = new CASPartPreset(casPart, presetId, CASUtils.PartDataGetPreset(casPart.Key, i));
                    if (!casPartPreset.Valid)
                    {
                        continue;
                    }
                    var hairTypeGridItemAdded = casHair.AddHairTypeGridItem(casHair.mHairTypesGrid, resourceKey, casPartPreset, isWardrobePart, ref shouldEnableCatalogProductFilter);
                    if (!(wornPart.Key == casPartPreset.mPart.Key) || !CASUtils.DesignPresetCompare(replacedHairColorsPresetString, CASUtils.ReplaceHairColors(casPartPreset.mPresetString, colors)))
                    {
                        continue;
                    }
                    casHair.mSavedPresetId = casPartPreset.mPresetId;
                    saveButtonEnabled = true;
                    if (hairTypeGridItemAdded)
                    {
                        casHair.mHairTypesGrid.SelectedItem = casHair.mHairTypesGrid.Count - 1;
                        if (ObjectDesigner.IsUserDesignPreset(i))
                        {
                            casHair.mHatsShareButton.Enabled = true;
                            casHair.mHatsDeleteButton.Enabled = true;
                        }
                    }
                }
            }
            casHair.mHairTypesGrid.Tag = shouldEnableCatalogProductFilter;
            if (casHair.mHairStylesGrid.Tag == null)
            {
                casHair.mHairStylesGrid.Tag = false;
            }
            casHair.mSortButton.Tag = (bool)casHair.mHairTypesGrid.Tag || (bool)casHair.mHairStylesGrid.Tag;
            if (saveButtonEnabled)
            {
                casHair.mSaveButton.Enabled = false;
            }
            else if (casHair.mHairType == CASHair.HairType.Hat && CASHair.PartIsHat(wornPart))
            {
                var windowByExportId = UIManager.LoadLayout(resourceKey).GetWindowByExportID(1);
                if (windowByExportId != null)
                {
                    var window = windowByExportId.GetChildByID(32u, true) as Window;
                    if (window != null)
                    {
                        window.Visible = false;
                    }
                    window = windowByExportId.GetChildByID(36u, true) as Window;
                    if (window != null)
                    {
                        window.Visible = true;
                    }
                    casHair.mHairTypesGrid.AddTempItem(new ItemGridCellItem(windowByExportId, null));
                }
                casHair.mSaveButton.Enabled = true;
            }
            casHair.mUndoOnDelete = false;
            casHair.mContentTypeFilter.UpdateFilterButtonState();
        }
        
        public void PopulateTypesGrid(bool isHat)
        {
            var self = (CASHair)(object)this;
            var casModel = Responder.Instance.CASModel;
            var colors = new[]
                {
                    new Color(0u),
                    new Color(0u),
                    new Color(0u),
                    new Color(0u)
                };
            self.mHairTypesGrid.Clear();
            var wornPart = self.GetWornPart();
            var resourceKey = ResourceKey.CreateUILayoutKey("GenericCasItem", 0u);
            var saveButtonEnabled = false;
            if (self.mHairType == CASHair.HairType.Hat)
            {
                self.mHatsShareButton.Enabled = false;
                self.mHatsDeleteButton.Enabled = false;
                self.mDesignButton.Enabled = CASHair.PartIsHat(wornPart);
            }
            var casFeaturedStoreItems = Responder.Instance.StoreUI.GetCASFeaturedStoreItems(BodyTypes.Hair, casModel.OutfitCategory, casModel.Age | casModel.Species | casModel.Gender, isHat);
            self.mContentTypeFilter.FilterObjects(casFeaturedStoreItems, out var shouldEnableCatalogProductFilter);
            foreach (IFeaturedStoreItem featuredStoreItem in casFeaturedStoreItems)
            {
                if (self.mHairType == CASHair.HairType.Hat != ((featuredStoreItem.CategoryFlags & 0x400000) != 0))
                {
                    continue;
                }
                var windowByExportId = UIManager.LoadLayout(resourceKey).GetWindowByExportID(1);
                if (windowByExportId == null)
                {
                    continue;
                }
                windowByExportId.Tag = featuredStoreItem;
                windowByExportId.GetChildByID(35u, true);
                var window = windowByExportId.GetChildByID(32u, true) as Window;
                if (window?.Drawable is ImageDrawable imageDrawable)
                {
                    imageDrawable.Image = UIUtils.GetUIImageFromThumbnailKey(featuredStoreItem.ThumbKey);
                    window.Invalidate();
                }
                window = (Window)windowByExportId.GetChildByID(768u, true);
                window.Tag = featuredStoreItem;
                window.CreateTooltipCallbackFunction = self.StoreItemCreateTooltip;
                window.Visible = true;
                ((Window)windowByExportId.GetChildByID(771u, true)).Visible = featuredStoreItem.IsSale;
                var button = (Button)windowByExportId.GetChildByID(769u, true);
                button.Caption = featuredStoreItem.PriceString;
                button.Tag = windowByExportId;
                button.Click += self.OnBuyButtonClick;
                button.FocusAcquired += self.OnBuyButtonFocusAcquired;
                button.FocusLost += self.OnBuyButtonFocusLost;
                self.mHairTypesGrid.AddItem(new ItemGridCellItem(windowByExportId, featuredStoreItem));
            }
            var simHairGrowthState = ((CASLogic)casModel).mSimDescEditExisting.GetHairGrowthState();
            foreach (CASPart casPart in self.mPartsList)
            {
                if (simHairGrowthState > HairGrowthStates.Unknown && !(casPart.TryGetHairGrowthState(out var partHairGrowthState) && (simHairGrowthState & partHairGrowthState) != 0))
                {
                    continue;
                }
                var isWardrobePart = Responder.Instance.CASModel.ActiveWardrobeContains(casPart);
                var numPresets = CASUtils.PartDataNumPresets(casPart.Key);
                var customContentType = UIUtils.GetCustomContentType(casPart.Key);
                if (UIUtils.IsContentTypeDisabled(customContentType))
                {
                    continue;
                }
                ObjectDesigner.SetCASPart(casPart.Key);
                var defaultPresetId = ObjectDesigner.DefaultPresetId;
                var designPresetIndexFromId = ObjectDesigner.GetDesignPresetIndexFromId(defaultPresetId);
                var defaultPresetString = ObjectDesigner.GetDesignPreset(designPresetIndexFromId);
                if (string.IsNullOrEmpty(defaultPresetString))
                {
                    defaultPresetString = Simulator.LoadXMLString(new ResourceKey(casPart.Key.InstanceId, 0x0333406C, casPart.Key.GroupId));
                }
                var casPartPreset = new CASPartPreset(casPart, defaultPresetString);
                var wornPartPresetString = "";
                var replacedHairColorsPresetString = "";
                if (wornPart.Key == casPartPreset.mPart.Key)
                {
                    wornPartPresetString = Responder.Instance.CASModel.GetDesignPreset(wornPart);
                    replacedHairColorsPresetString = CASUtils.ReplaceHairColors(wornPartPresetString, colors);
                }
                if (casPartPreset.Valid && (self.mHairType == CASHair.HairType.Hair || ObjectDesigner.DefaultPresetId == uint.MaxValue))
                {
                    self.AddHairTypeGridItem(self.mHairTypesGrid, resourceKey, casPartPreset, isWardrobePart, ref shouldEnableCatalogProductFilter);
                    if (casPartPreset.mPart.Key == wornPart.Key && (self.mHairType == CASHair.HairType.Hair || CASUtils.DesignPresetCompare(wornPartPresetString, defaultPresetString)))
                    {
                        self.mHairTypesGrid.SelectedItem = self.mHairTypesGrid.Count - 1;
                        saveButtonEnabled = true;
                    }
                }
                if (self.mHairType != CASHair.HairType.Hat)
                {
                    continue;
                }
                for (var i = 0u; i < numPresets; i++)
                {
                    var presetId = CASUtils.PartDataGetPresetId(casPart.Key, i);
                    UIUtils.GetCustomContentType(casPart.Key, presetId);
                    casPartPreset = new CASPartPreset(casPart, presetId, CASUtils.PartDataGetPreset(casPart.Key, i));
                    if (!casPartPreset.Valid)
                    {
                        continue;
                    }
                    var hairTypeGridItemAdded = self.AddHairTypeGridItem(self.mHairTypesGrid, resourceKey, casPartPreset, isWardrobePart, ref shouldEnableCatalogProductFilter);
                    if (wornPart.Key != casPartPreset.mPart.Key || !CASUtils.DesignPresetCompare(replacedHairColorsPresetString, CASUtils.ReplaceHairColors(casPartPreset.mPresetString, colors)))
                    {
                        continue;
                    }
                    self.mSavedPresetId = casPartPreset.mPresetId;
                    saveButtonEnabled = true;
                    if (hairTypeGridItemAdded)
                    {
                        self.mHairTypesGrid.SelectedItem = self.mHairTypesGrid.Count - 1;
                        if (ObjectDesigner.IsUserDesignPreset(i))
                        {
                            self.mHatsShareButton.Enabled = true;
                            self.mHatsDeleteButton.Enabled = true;
                        }
                    }
                }
            }
            self.mHairTypesGrid.Tag = shouldEnableCatalogProductFilter;
            if (self.mHairStylesGrid.Tag == null)
            {
                self.mHairStylesGrid.Tag = false;
            }
            self.mSortButton.Tag = (bool)self.mHairTypesGrid.Tag || (bool)self.mHairStylesGrid.Tag;
            if (saveButtonEnabled)
            {
                self.mSaveButton.Enabled = false;
            }
            else if (self.mHairType == CASHair.HairType.Hat && CASHair.PartIsHat(wornPart))
            {
                var windowByExportId = UIManager.LoadLayout(resourceKey).GetWindowByExportID(1);
                if (windowByExportId != null)
                {
                    var window = windowByExportId.GetChildByID(32u, true) as Window;
                    if (window != null)
                    {
                        window.Visible = false;
                    }
                    window = windowByExportId.GetChildByID(36u, true) as Window;
                    if (window != null)
                    {
                        window.Visible = true;
                    }
                    self.mHairTypesGrid.AddTempItem(new ItemGridCellItem(windowByExportId, null));
                }
                self.mSaveButton.Enabled = true;
            }
            self.mUndoOnDelete = false;
            self.mContentTypeFilter.UpdateFilterButtonState();
        }
    }
}
