using System;
using System.Collections.Generic;
using System.Reflection;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.EventSystem;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.Appliances;
using Sims3.Gameplay.Objects.CookingObjects;
using Sims3.Gameplay.Objects.Counters;
using Sims3.Gameplay.Objects.Electronics;
using Sims3.Gameplay.Objects.FoodObjects;
using Sims3.Gameplay.Objects.Resort;
using Sims3.Gameplay.Objects.Seating;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.SimIFace.Enums;
using Sims3.UI;
using Sims3.UI.CAS;

namespace Destrospean.MoreFavorites
{
    public class Replacements
    {
        public class ChangeFavoritesDialogPatch : ChangeFavoritesDialog
        {
            public ChangeFavoritesDialogPatch(ISimDescription sim) : base(sim)
            {
            }

            public new void OnFavoritesVisibilityChange(WindowBase sender, UIVisibilityChangeEventArgs args)
            {
                if (args.Visible)
                {
                    sender.Tick += OnFavoritesVisibilityTick;
                    switch (sender.ID)
                    {
                        case 100663297:
                            {
                                Array visibleFavorites = Array.FindAll((FavoriteFoodType[])GetInstalledFavoriteFoodList(), x => !x.IsBlacklisted() && !x.IsHidden());
                                PopulateFavoritesGrid(mGridFavoriteFood, visibleFavorites, 0, visibleFavorites.Length);
                                break;
                            }
                        case 100663301:
                            {
                                Array visibleFavorites = Array.FindAll((FavoriteMusicType[])GetInstalledFavoriteMusicList(), x => !x.IsBlacklisted() && !x.IsHidden());
                                PopulateFavoritesGrid(mGridFavoriteMusic, visibleFavorites, 0, visibleFavorites.Length);
                                break;
                            }
                        case 100663305:
                            {
                                Array visibleFavorites = Array.FindAll(CASCharacter.kColors, x => !x.IsBlacklisted() && !x.IsHidden());
                                PopulateFavoritesGrid(mGridFavoriteColor, visibleFavorites, 0, visibleFavorites.Length);
                                break;
                            }
                    }
                }
                else
                {
                    sender.Tick -= OnFavoritesVisibilityTick;
                }
            }

            public new void PopulateFavoritesGrid(ItemGrid grid, Array favoritesList, int startIndex, int endIndex)
            {
                grid.Clear();
                grid.ForceCellTooltips = true;
                Color favoriteColor = mResult.mFavoriteColor;
                for (int i = startIndex; i < endIndex; i++)
                {
                    object favorite = favoritesList.GetValue(i);
                    Layout layout = UIManager.LoadLayout(ResourceKey.CreateUILayoutKey("FavoritesGridItem", 0));
                    Window window = (Window)layout.GetWindowByExportID(1);
                    ImageDrawable imageDrawable = (ImageDrawable)((Window)window.GetChildByID(32, false)).Drawable;
                    bool isCurrentFavorite = false;
                    if (favorite is FavoriteFoodType)
                    {
                        FavoriteFoodType favoriteFoodType = (FavoriteFoodType)favorite;
                        IRecipe recipe = Responder.Instance.CASModel.GetRecipe(favoriteFoodType) ?? (FavoritesUtils.FavoriteFoodDictionary.ContainsKey(favoriteFoodType) ? FavoritesUtils.FavoriteFoodDictionary[favoriteFoodType].Recipe : null);
                        if (recipe != null)
                        {
                            if (mSim.IsVegetarian)
                            {
                                if (!recipe.IsVegetarian && !recipe.HasVegetarianAlternative)
                                {
                                    continue;
                                }
                                window.TooltipText = recipe.GenericVegetarianName;
                            }
                            else
                            {
                                window.TooltipText = recipe.GenericName;
                            }
                            imageDrawable.Image = CASCharacter.GetFavoriteFoodIcon(favoriteFoodType);
                            isCurrentFavorite = favoriteFoodType == mResult.mFavoriteFood;
                        }
                    }
                    else if (favorite is FavoriteMusicType)
                    {
                        FavoriteMusicType favoriteMusicType = (FavoriteMusicType)favorite;
                        window.TooltipText = CASCharacter.GetFavoriteMusicName(favoriteMusicType);
                        imageDrawable.Image = CASCharacter.GetFavoriteMusicIcon(favoriteMusicType);
                        isCurrentFavorite = favoriteMusicType == mResult.mFavoriteMusic;
                    }
                    else if (favorite is CASCharacter.NameColorPair)
                    {
                        CASCharacter.NameColorPair nameColorPair = (CASCharacter.NameColorPair)favorite;
                        window.TooltipText = CASCharacter.GetLocalizedColorName(nameColorPair.mName);
                        imageDrawable.Image = CASCharacter.GetFavoriteColorIcon(nameColorPair.mColor);
                        isCurrentFavorite = nameColorPair.mColor.ARGB == favoriteColor.ARGB;
                    }
                    grid.AddItem(new ItemGridCellItem(window, favorite));
                    if (isCurrentFavorite)
                    {
                        grid.SelectedItem = grid.Count - 1;
                    }
                }
            }

            public new void RandomizeAllFavorites(bool missingFavoritesOnly)
            {
                Random random = new Random();
                FavoriteFoodType favoriteFood = mResult.mFavoriteFood;
                if (!missingFavoritesOnly || favoriteFood == FavoriteFoodType.None)
                {
                    Array visibleFavoriteFoodList = Array.FindAll((FavoriteFoodType[])GetInstalledFavoriteFoodList(), x => !x.IsBlacklisted() && !x.IsHidden());
                    favoriteFood = (FavoriteFoodType)visibleFavoriteFoodList.GetValue(random.Next(1, visibleFavoriteFoodList.Length));
                    if (mSim.IsVegetarian)
                    {
                        IRecipe recipe = Responder.Instance.CASModel.GetRecipe(favoriteFood);
                        while (!recipe.IsVegetarian && !recipe.HasVegetarianAlternative)
                        {
                            favoriteFood = (FavoriteFoodType)visibleFavoriteFoodList.GetValue(random.Next(1, visibleFavoriteFoodList.Length));
                            recipe = Responder.Instance.CASModel.GetRecipe(favoriteFood);
                        }
                    }
                }
                FavoriteMusicType favoriteMusic = mResult.mFavoriteMusic;
                Array visibleFavoriteMusicList = Array.FindAll((FavoriteMusicType[])GetInstalledFavoriteMusicList(), x => !x.IsBlacklisted() && !x.IsHidden());
                for (favoriteMusic = FavoriteMusicType.Custom; favoriteMusic == FavoriteMusicType.Custom; favoriteMusic = (FavoriteMusicType)visibleFavoriteMusicList.GetValue(random.Next(1, visibleFavoriteMusicList.Length)))
                {
                }
                Color favoriteColor = mResult.mFavoriteColor;
                if (!missingFavoritesOnly || favoriteColor.ARGB == 0)
                {
                    Array visibleFavoriteColorList = Array.FindAll(CASCharacter.kColors, x => !x.IsBlacklisted() && !x.IsHidden());
                    favoriteColor = ((CASCharacter.NameColorPair)visibleFavoriteColorList.GetValue(random.Next(0, visibleFavoriteColorList.Length))).mColor;
                }
                mResult.mFavoriteFood = favoriteFood;
                mResult.mFavoriteMusic = favoriteMusic;
                mResult.mFavoriteColor = favoriteColor;
                UpdateFavoritesButtons();
            }
        }

        public class EatHeldFoodPatch : EatHeldFood
        {
            public override bool Run()
            {
                if (Actor.Posture != null && !Actor.Posture.Satisfies(CommodityKind.Sitting, null) && !(Target is PoisonedApple))
                {
                    Actor.Wander(ServingContainerGroup.kMinDistanceToMoveAwayAfterGrabbingPlate, ServingContainerGroup.kMaxDistanceToMoveAwayAfterGrabbingPlate, true, RouteDistancePreference.NoPreference, true);
                }
                Target.BeforeEatingCallback(Actor);
                if (!InteractionTest(Actor, Target))
                {
                    if (Target.Parent == Actor)
                    {
                        Actor.InteractionQueue.PushAsContinuation(CarrySystem.PutDownHeldObject.Singleton, Target, false, Actor.InheritedPriority(), true);
                    }
                    return false;
                }
                bool addToUseList = true;
                if (Target.Parent == Actor)
                {
                    addToUseList = false;
                }
                if (Autonomous)
                {
                    mPriority = new InteractionPriority(InteractionPriorityLevel.UserDirected);
                }
                StandardEntry(addToUseList);
                ChairDining chairDining = Actor.Posture.Container as ChairDining;
                if (chairDining != null && chairDining.Parent != null && chairDining.ChairState == ChairDining.State.Angled && Target.Parent != Actor)
                {
                    SitTransitionAngledToStraight.Singleton.CreateInstance(Actor.Posture.Container, Actor, GetPriority(), false, false).RunInteraction();
                }
                Food.PreEat(Actor, Target as GameObject, ref mIsSufficientlyFullForStuffed, ref mHasFatDelta);
                mCurrentStateMachine = StateMachineClient.Acquire(Actor, "eat", AnimationPriority.kAPCarryRightPlus);
                SetActor("x", Actor);
                SetParameter("isWerewolf", chairDining != null && Actor.BuffManager.HasElement(BuffNames.Werewolf));
                SetActor("ServingContainer", Target);
                SetActor("thingToEat", Target.ThingToEat);
                Counter counter = Target.Parent as Counter;
                if (counter != null)
                {
                    SetActor("Counter", counter);
                    SetParameter("IKSuffix", counter.IkSuffix);
                }
                mCurrentStateMachine.EnterState("x", "Enter");
                if (Actor.Posture != null && Actor.Posture.Satisfies(CommodityKind.Sitting, Target))
                {
                    SetActor("sitTemplate", Actor.Posture.Container);
                    SetParameter("sitTemplateSuffix", ((SittingPosture)Actor.Posture).Part.Target.IKSuffix);
                }
                EatingPosture eatingPosture = GetPostureParam();
                SetParameter("eatPosture", eatingPosture);
                SetParameter("isFavorite", Target.Recipe.IsFavoriteOfSim(Actor.SimDescription));
                SetParameter("isSloppy", Actor.HasTrait(TraitNames.Slob));
                SetParameter("isSpoiled", Target.Spoiled);
                SetParameter("isIceCream", Target is SnackIceCream);
                NectarBottle.NectarGlass nectarGlass = Target as NectarBottle.NectarGlass;
                if (nectarGlass != null)
                {
                    nectarGlass.SetParameters(Actor, this);
                }
                if (Target is FutureBar.FutureBarGlass && Target.Spoiled)
                {
                    SetParameter("isServoJuice", true);
                }
                UtensilType utensilType = Target.UtensilType;
                if (Target.UtensilName == "fork" && (Actor.HasTrait(TraitNames.CultureChina) || GameUtils.GetCurrentWorld() == WorldName.China))
                {
                    utensilType = UtensilType.chopsticks;
                }
                SetParameter("UtensilType", utensilType);
                if (Target.CountsAsFood && !Target.IsDrinkable)
                {
                    switch (eatingPosture)
                    {
                        case EatingPosture.diningIn:
                            if (utensilType == UtensilType.fork || utensilType == UtensilType.hand)
                            {
                                mAllowsThrowScrap = true;
                            }
                            break;
                        case EatingPosture.diningOut:
                            if (utensilType == UtensilType.fork)
                            {
                                mAllowsThrowScrap = true;
                            }
                            break;
                    }
                    mPetWatchSimEatingHelper.Watchable = true;
                    mPetWatchSimEatingHelper.TriggerReactionBroadcaster(Actor);
                }
                mPropHandle = ObjectGuid.InvalidObjectGuid;
                if (utensilType != UtensilType.hand)
                {
                    if (utensilType == UtensilType.chopsticks)
                    {
                        mPropHandle = GlobalFunctions.CreateProp("UtensilChopsticks", Vector3.OutOfWorld, 0, Vector3.UnitZ);
                    }
                    else
                    {
                        mPropHandle = GlobalFunctions.CreateProp(GetUtensilMedatorName(Target.UtensilName), Vector3.OutOfWorld, 0, Vector3.UnitZ);
                    }
                    if (mPropHandle != ObjectGuid.InvalidObjectGuid)
                    {
                        mCurrentStateMachine.SetPropActor("utensil", mPropHandle);
                    }
                }
                mCurrentStateMachine.AddOneShotScriptEventHandler(101, new ObjectHideHelper(Target).Callback);
                mCurrentStateMachine.AddOneShotScriptEventHandler(105, ParentFoodToContainer);
                mCurrentStateMachine.AddPersistentScriptEventHandler(501, StartSloppyVFX);
                mCurrentStateMachine.AddPersistentScriptEventHandler(502, StopSloppyVFX);
                if (eatingPosture == EatingPosture.living)
                {
                    Seat.EnsureLivingChairPosture(Actor);
                }
                AnimateSim(Target.EatLoopStateName);
                SetParameter("wasInterrupted", true);
                AddMotiveArrow(CommodityKind.Hunger, true);
                BeginCommodityUpdates();
                if (Target.IsVampireFood)
                {
                    AddMotiveDelta(CommodityKind.VampireThirst, SnackVampireJuice.kThirstPerHour);
                }
                ReactionBroadcaster reactionBroadcaster = null;
                if (Target.Recipe != null && Target.Recipe.Key == "BrainFreeze")
                {
                    AddMotiveDelta(CommodityKind.BeAZombie, BuffZombieFreeze.kZombieCommodityChangeRate);
                    reactionBroadcaster = new ReactionBroadcaster(Actor, BuffZombieFreeze.kBrainFreezeCreepedOutBroadcasterParams, OnEnterCreepedOut);
                }
                Actor.RegisterGroupTalk();
                OccultImaginaryFriend.GrantMilestoneBuff(Actor, BuffNames.ImaginaryFriendAteFood, Origin.FromImaginaryFriendFirstTime, false, true, false);
                bool succeeded = DoLoop(ExitReason.Default, LoopCallback, mCurrentStateMachine);
                HotBeverageMachine.Cup cup = Target.ThingToEat as HotBeverageMachine.Cup;
                if (succeeded && GameUtils.IsInstalled(ProductVersion.EP9) && cup != null && cup.IsEnergyDrink)
                {
                    Actor.BuffManager.AddElement(BuffNames.LiquidEnergy, Origin.FromEnergyDrink);
                }
                Actor.UnregisterGroupTalk();
                EndCommodityUpdates(succeeded);
                RemoveMotiveDelta(CommodityKind.VampireThirst);
                RemoveMotiveDelta(CommodityKind.BeAZombie);
                Herb.HerbData herbData = default(Herb.HerbData);
                if (Target.GetAddedHerb(ref herbData))
                {
                    Herb.ApplyHerbEffects(Actor, Herb.IngestionType.EatWithFood, herbData);
                }
                if (reactionBroadcaster != null)
                {
                    reactionBroadcaster.EndBroadcast();
                    reactionBroadcaster.Dispose();
                    reactionBroadcaster = null;
                }
                SetParameter("wasInterrupted", !Actor.HasExitReason(ExitReason.Finished));
                mCurrentStateMachine.RemoveEventHandler(StartSloppyVFX);
                mCurrentStateMachine.RemoveEventHandler(StopSloppyVFX);
                AnimateSim("Exit");
                mPetWatchSimEatingHelper.Watchable = false;
                if (mPropHandle != ObjectGuid.InvalidObjectGuid)
                {
                    Simulator.DestroyObject(mPropHandle);
                }
                if (Actor.HasExitReason())
                {
                    Actor.RemoveExitReason(ExitReason.MoodFailure);
                    if (Actor.OnlyHasExitReason(ExitReason.Finished))
                    {
                        Target.FinishedEatingCallback(Actor);
                        if (Autonomous && Actor.Posture.Container != null && Actor.Posture.Container.Parent != null)
                        {
                            IEatingSurface eatingSurface = Actor.Posture.Container.Parent as IEatingSurface;
                            if (eatingSurface != null && eatingSurface.AllowWaitForOthers && (eatingSurface.NumOtherSimsAtSurface(Actor) > 0 || ((GameObject)eatingSurface).ReferenceList.Count > 0))
                            {
                                Actor.LoopIdle();
                                Actor.RegisterGroupTalk();
                                mTimeWaitForOtherStarted = SimClock.CurrentTime();
                                Actor.RemoveExitReason(ExitReason.Finished);
                                Actor.TryGroupTalk();
                                DoLoop(ExitReason.Default, WaitForOthersLoopCallback, null, 5);
                                Actor.UnregisterGroupTalk();
                            }
                        }
                        if (Target.CanBeCleanedUp && !ShouldDestroyContainer)
                        {
                            if (RandomUtil.RandomChance(Actor.HasTrait(TraitNames.Slob) || Target.Recipe != null && Actor.SimDescription.IsGhost && Target.Recipe.Key == "Ambrosia" || Actor.LotCurrent.IsCommunityLot && Target is IPaperContainer ? 0 : Actor.HasTrait(TraitNames.Neat) ? 100 : Food.CleanupChance))
                            {
                                TryPushCleanup();
                            }
                            else if (Target.Parent == Actor)
                            {
                                Actor.InteractionQueue.PushAsContinuation(CarrySystem.PutDownHeldObject.Singleton, Target, false, new InteractionPriority(InteractionPriorityLevel.UserDirected), true);
                            }
                        }
                        ISingleServingContainer singleServingContainer = Target as ISingleServingContainer;
                        if (singleServingContainer != null && singleServingContainer.FromResortBuffetTable && !Actor.BuffManager.HasElement(BuffNames.Stuffed) && Actor.Motives.GetMotiveValue(CommodityKind.Hunger) <= (float)ResortBuffetTable.kHungerToStopFeeding)
                        {
                            ResortBuffetTable.ConsumeMoreFood(Actor);
                        }
                    }
                    else
                    {
                        ServingContainer servingContainer = Target as ServingContainer;
                        if (Actor.HasTrait(TraitNames.Vegetarian) && servingContainer != null)
                        {
                            TraitFunctions.VegetarianTraitEatingMeatCallback(Actor, servingContainer.CookingProcess.StartedByVegetarian, servingContainer.CookingProcess.Recipe);
                        }
                        else if (Target.Parent == Actor && !ShouldDestroyContainer)
                        {
                            Actor.InteractionQueue.PushAsContinuation(CarrySystem.PutDownHeldObject.Singleton, Target, false, new InteractionPriority(InteractionPriorityLevel.UserDirected), true);
                        }
                    }
                }
                Food.PostEat(Actor, Target as GameObject, mIsSufficientlyFullForStuffed, HungerGiven > 0, mHasFatDelta);
                if (Target.Recipe != null && !Target.Recipe.IsSnack)
                {
                    Target.Recipe.LearnFromEating(Actor);
                }
                bool isSpoiledGlass = false;
                Bar.Glass glass = Target as Bar.Glass;
                if (glass != null)
                {
                    isSpoiledGlass = glass.Spoiled;
                }
                if (Actor.SimDescription.IsBonehilda && Target is IGlass)
                {
                    Sims3.Gameplay.Controllers.PuddleManager.AddPuddle(Actor.Position);
                }
                if (nectarGlass != null)
                {
                    nectarGlass.DoPostDrink(Actor, GetPriority().Level == InteractionPriorityLevel.Autonomous);
                }
                else if (glass != null && !isSpoiledGlass)
                {
                    Actor.BuffManager.AddElement(BuffNames.SugarRush, Origin.FromJuice);
                }
                if (glass != null)
                {
                    Actor.BuffManager.RemoveElement(BuffNames.TooSpicy);
                    if (Actor.BuffManager.HasElement(BuffNames.Spicy) && Actor.Motives.GetValue(CommodityKind.Hunger) == Actor.Motives.GetMax(CommodityKind.Hunger))
                    {
                        Actor.BuffManager.RemoveElement(BuffNames.Spicy);
                    }
                }
                if (!Target.CanBeCleanedUp)
                {
                    CarrySystem.ExitCarry(Actor);
                    addToUseList = false;
                    DestroyObject(Target);
                }
                if (isSpoiledGlass && RandomUtil.RandomChance(kChanceToThrowUpFromBarGlass))
                {
                    Actor.PlayReaction(ReactionTypes.ThrowUp, new InteractionPriority(InteractionPriorityLevel.High), null, ReactionSpeed.AfterInteraction);
                }
                if (Actor.LotCurrent.IsCommunityLot && Target is IPaperContainer || ShouldDestroyContainer)
                {
                    Actor.InteractionQueue.PushAsContinuation(FinishEatingOutside.Singleton, Target, true);
                }
                Actor.BuffManager.RemoveElement(BuffNames.MintyBreath);
                StandardExit(addToUseList, addToUseList);
                return succeeded;
            }

            public bool RunIcarusAllSortsOverride()
            {
                if (Actor.Posture != null && !Actor.Posture.Satisfies(CommodityKind.Sitting, null))
                {
                    Actor.Wander(ServingContainerGroup.kMinDistanceToMoveAwayAfterGrabbingPlate, ServingContainerGroup.kMaxDistanceToMoveAwayAfterGrabbingPlate, true, RouteDistancePreference.NoPreference, true);
                }
                PreparedFood preparedFood = (PreparedFood)Target;
                CookingProcess cookingProcess = preparedFood.CookingProcess;
                List<CookedIngredient> chosenIngredients = cookingProcess.ChosenIngredients;
                if (Actor.HasTrait(TraitNames.Vegetarian) && !cookingProcess.Recipe.IsVegetarian && (!cookingProcess.Recipe.HasVegetarianAlternative || !cookingProcess.StartedByVegetarian))
                {
                    Actor.PlayReaction(ReactionTypes.Retch, preparedFood, ReactionSpeed.ImmediateWithoutOverlay);
                }
                if (!InteractionTest(Actor, Target))
                {
                    if (Target.Parent == Actor)
                    {
                        Actor.InteractionQueue.PushAsContinuation(CarrySystem.PutDownHeldObject.Singleton, Target, false, Actor.InheritedPriority(), true);
                    }
                    return false;
                }
                bool addToUseList = Target.Parent != Actor;
                if (Autonomous)
                {
                    mPriority = new InteractionPriority(InteractionPriorityLevel.UserDirected);
                }
                StandardEntry(addToUseList);
                ChairDining chairDining = Actor.Posture.Container as ChairDining;
                if (chairDining != null && chairDining.Parent != null && chairDining.ChairState == ChairDining.State.Angled && addToUseList)
                {
                    InteractionInstance interactionInstance = SitTransitionAngledToStraight.Singleton.CreateInstance(chairDining, Actor, GetPriority(), false, false);
                    interactionInstance.RunInteraction();
                }
                Food.PreEat(Actor, preparedFood, ref mIsSufficientlyFullForStuffed, ref mHasFatDelta);
                mCurrentStateMachine = StateMachineClient.Acquire(Actor, "eat", AnimationPriority.kAPCarryRightPlus);
                SetActor("x", Actor);
                SetParameter("isWerewolf", chairDining != null && Actor.BuffManager.HasElement(BuffNames.Werewolf));
                SetActor("ServingContainer", Target);
                SetActor("thingToEat", Target.ThingToEat);
                Counter counter = Target.Parent as Counter;
                if (counter != null)
                {
                    SetActor("Counter", counter);
                    SetParameter("IKSuffix", counter.IkSuffix);
                }
                mCurrentStateMachine.EnterState("x", "Enter");
                if (Actor.Posture != null && Actor.Posture.Satisfies(CommodityKind.Sitting, Target))
                {
                    SetActor("sitTemplate", Actor.Posture.Container);
                    SetParameter("sitTemplateSuffix", ((SittingPosture)Actor.Posture).Part.Target.IKSuffix);
                }
                bool isSloppy = Actor.HasTrait(TraitNames.Slob);
                EatingPosture eatingPosture = GetPostureParam();
                SetParameter("eatPosture", eatingPosture);
                SetParameter("isFavorite", cookingProcess.Recipe.IsFavoriteOfSim(Actor.SimDescription));
                SetParameter("isSloppy", isSloppy);
                SetParameter("isSpoiled", cookingProcess.IsSpoiled);
                SetParameter("isIceCream", Target is SnackIceCream);
                UtensilType utensilType = cookingProcess.Recipe.Utensil == "chopsticks" || Target.UtensilName == "fork" && !cookingProcess.Recipe.IsDessert && (Actor.TraitManager.HasAnyElement(TraitNames.CultureChina, (TraitNames)15572421960754265911uL) || GameUtils.GetCurrentWorld() == WorldName.China) ? UtensilType.chopsticks : Target.UtensilType;
                SetParameter("UtensilType", utensilType);
                if (!Target.IsDrinkable)
                {
                    switch (eatingPosture)
                    {
                        case EatingPosture.diningIn:
                            if (utensilType == UtensilType.fork || utensilType == UtensilType.hand)
                            {
                                mAllowsThrowScrap = true;
                            }
                            break;
                        case EatingPosture.diningOut:
                            if (utensilType == UtensilType.fork)
                            {
                                mAllowsThrowScrap = true;
                            }
                            break;
                    }
                    mPetWatchSimEatingHelper.Watchable = true;
                    mPetWatchSimEatingHelper.TriggerReactionBroadcaster(Actor);
                }
                mPropHandle = ObjectGuid.InvalidObjectGuid;
                if (utensilType != UtensilType.hand)
                {
                    mPropHandle = GlobalFunctions.CreateProp((utensilType == UtensilType.chopsticks) ? "UtensilChopsticks" : GetUtensilMedatorName(Target.UtensilName), Vector3.OutOfWorld, 0, Vector3.UnitZ);
                    if (mPropHandle != ObjectGuid.InvalidObjectGuid)
                    {
                        mCurrentStateMachine.SetPropActor("utensil", mPropHandle);
                    }
                }
                mCurrentStateMachine.AddOneShotScriptEventHandler(101, new ObjectHideHelper(Target).Callback);
                mCurrentStateMachine.AddOneShotScriptEventHandler(105, ParentFoodToContainer);
                mCurrentStateMachine.AddPersistentScriptEventHandler(501, StartSloppyVFX);
                mCurrentStateMachine.AddPersistentScriptEventHandler(502, StopSloppyVFX);
                if (eatingPosture == EatingPosture.living)
                {
                    Seat.EnsureLivingChairPosture(Actor);
                }
                AnimateSim(Target.EatLoopStateName);
                SetParameter("wasInterrupted", true);
                FieldInfo commodityToChangeField = GetType().GetField("mCommodityToChange"),
                hungerPerBiteOrSipField = GetType().GetField("mHungerPerBiteOrSip"),
                hungerValueThresholdField = GetType().GetField("mHungerValueThreshold");
                if (Actor.Motives.HasMotive(CommodityKind.VampireThirst) && cookingProcess.IsVampireFood)
                {
                    commodityToChangeField.SetValue(this, CommodityKind.VampireThirst);
                }
                AddMotiveArrow((CommodityKind)commodityToChangeField.GetValue(this), true);
                BeginCommodityUpdates();
                ReactionBroadcaster reactionBroadcaster = null;
                if (cookingProcess.Recipe.Key == "BrainFreeze")
                {
                    AddMotiveDelta(CommodityKind.BeAZombie, BuffZombieFreeze.kZombieCommodityChangeRate);
                    reactionBroadcaster = new ReactionBroadcaster(Actor, BuffZombieFreeze.kBrainFreezeCreepedOutBroadcasterParams, OnEnterCreepedOut);
                }
                Actor.RegisterGroupTalk();
                OccultImaginaryFriend.GrantMilestoneBuff(Actor, BuffNames.ImaginaryFriendAteFood, Origin.FromImaginaryFriendFirstTime, false, true, false);
                if (Target.IsDrinkable)
                {
                    hungerPerBiteOrSipField.SetValue(this, kHungerValuePerSipForJuice);
                    hungerValueThresholdField.SetValue(this, Target is SnackVampireJuice && (CommodityKind)commodityToChangeField.GetValue(this) == CommodityKind.VampireThirst ? kHungerValueForMeal : kHungerValueForJuice);
                }
                else if (cookingProcess.Recipe.IsSnack)
                {
                    hungerPerBiteOrSipField.SetValue(this, kHungerValuePerBiteForSnack);
                    hungerValueThresholdField.SetValue(this, kHungerValueForSnack);
                }
                else
                {
                    hungerPerBiteOrSipField.SetValue(this, kHungerValuePerBiteForMeal);
                    hungerValueThresholdField.SetValue(this, kHungerValueForMeal);
                }
                float hungerMultiplier = 1;
                if (Actor.SimDescription.IsMermaid && !cookingProcess.IsMermaidFood)
                {
                    hungerMultiplier = MathUtils.Clamp((int)GetType().GetMethod("CountFishInFood").Invoke(this, new object[]
                        {
                            chosenIngredients,
                            cookingProcess.Recipe
                        }) * ((bool)GetType().GetField("kHungerGivenIsPerFish", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) ? ((float)GetType().GetField("kHungerPerFish", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) / MathUtils.Clamp(Food.FoodUnitsFullServing * kHungerValuePerBiteForMeal, 1, kHungerValueForMeal)) : 1), OccultMermaid.kHungerValueMultiplier, 1);
                }
                if (Actor.SimDescription.IsFrankenstein)
                {
                    hungerMultiplier *= kHungerValueMultiplierFrankenstein;
                }
                hungerPerBiteOrSipField.SetValue(this, (float)hungerPerBiteOrSipField.GetValue(this) * hungerMultiplier);
                hungerValueThresholdField.SetValue(this, (float)hungerValueThresholdField.GetValue(this) * hungerMultiplier);
                bool isFlaming = (bool)GetType().GetMethod("IsFlaming").Invoke(null, new object[]
                    {
                        preparedFood,
                        false
                    });
                GetType().GetField("mGivePyroBenefits").SetValue(this, isFlaming && !cookingProcess.IsSpoiled && Actor.HasTrait(TraitNames.Pyromaniac));
                bool succeeded = DoLoop(ExitReason.Default, LoopCallback, mCurrentStateMachine);
                Actor.UnregisterGroupTalk();
                EndCommodityUpdates(succeeded);
                RemoveMotiveDelta(CommodityKind.BeAZombie);
                if (reactionBroadcaster != null)
                {
                    reactionBroadcaster.EndBroadcast();
                    reactionBroadcaster.Dispose();
                }
                SetParameter("wasInterrupted", !Actor.HasExitReason(ExitReason.Finished));
                mCurrentStateMachine.RemoveEventHandler(StartSloppyVFX);
                mCurrentStateMachine.RemoveEventHandler(StopSloppyVFX);
                AnimateSim("Exit");
                mPetWatchSimEatingHelper.Watchable = false;
                if (mPropHandle != ObjectGuid.InvalidObjectGuid)
                {
                    Simulator.DestroyObject(mPropHandle);
                }
                bool pushAsContinuation = false;
                if (Actor.HasExitReason())
                {
                    Actor.RemoveExitReason(ExitReason.MoodFailure);
                    if (Actor.OnlyHasExitReason(ExitReason.Finished))
                    {
                        if (HungerGiven > 0)
                        {
                            if (cookingProcess.Recipe.Key == "Ambrosia" || Lazy.Count(chosenIngredients) == 0 && (isFlaming || cookingProcess.Recipe.Key != "BakedAngelFoodCake"))
                            {
                                Target.FinishedEatingCallback(Actor);
                            }
                            else
                            {
                                EventTracker.SendEvent(EventTypeId.kAteMeal, Actor, Target);
                                ServingContainerSingle servingContainerSingle = Target as ServingContainerSingle;
                                if (servingContainerSingle != null)
                                {
                                    if (cookingProcess.ObjectClickedOn is IFutureFoodSynthesizer)
                                    {
                                        Actor.BuffManager.AddElement(BuffNames.HungerResolved, Origin.FromFoodSynthesizer);
                                        EventTracker.SendEvent(EventTypeId.kAteSynthMeal, Actor, Target);
                                    }
                                    if (!cookingProcess.IsSpoiled)
                                    {
                                        servingContainerSingle.AddEatingBuffs(Actor);
                                    }
                                    servingContainerSingle.mDirtyAlarm = servingContainerSingle.AddAlarm(ServingContainerSingle.kMinutesUntilDirty, TimeUnit.Minutes, servingContainerSingle.AddDirtyCallback, "Serving Container: Make Dirty", AlarmType.DeleteOnReset);
                                }
                                if (!cookingProcess.Recipe.IsVegetarian)
                                {
                                    foreach (Sim sim in Actor.LotCurrent.GetSims())
                                    {
                                        if (sim.RoomId == Actor.RoomId && sim.TraitManager.HasElement(TraitNames.Vegetarian))
                                        {
                                            Sims3.Gameplay.Socializing.ActiveTopic.AddToSim(sim, "Disgusted By Meat");
                                        }
                                    }
                                }
                                if ((cookingProcess.IsSpoiled || cookingProcess.FoodState == FoodCookState.Burnt) && !isSloppy && !Actor.HasTrait(TraitNames.StinkySim))
                                {
                                    Actor.BuffManager.AddElement(BuffNames.Nauseous, Origin.FromDisgustingFood);
                                }
                            }
                        }
                        if (Autonomous && Actor.Posture.Container != null)
                        {
                            IEatingSurface eatingSurface = Actor.Posture.Container.Parent as IEatingSurface;
                            if (eatingSurface != null && eatingSurface.AllowWaitForOthers && (eatingSurface.NumOtherSimsAtSurface(Actor) > 0 || ((GameObject)eatingSurface).ReferenceList.Count > 0))
                            {
                                Actor.LoopIdle();
                                Actor.RegisterGroupTalk();
                                mTimeWaitForOtherStarted = SimClock.CurrentTime();
                                Actor.RemoveExitReason(ExitReason.Finished);
                                Actor.TryGroupTalk();
                                DoLoop(ExitReason.Default, WaitForOthersLoopCallback, null, 5);
                                Actor.UnregisterGroupTalk();
                            }
                        }
                        if (Target.CanBeCleanedUp && !ShouldDestroyContainer)
                        {
                            if (Actor.HasTrait(TraitNames.Neat) || !isSloppy && (!Actor.SimDescription.IsGhost || cookingProcess.Recipe.Key != "Ambrosia") && (!Actor.LotCurrent.IsCommunityLot || !(Target is IPaperContainer)) && RandomUtil.RandomChance(Food.CleanupChance))
                            {
                                TryPushCleanup();
                            }
                            else
                            {
                                pushAsContinuation = Target.Parent == Actor;
                            }
                        }
                        if (preparedFood is ISingleServingContainer && ((ISingleServingContainer)preparedFood).FromResortBuffetTable && !Actor.BuffManager.HasElement(BuffNames.Stuffed) && Actor.Motives.GetMotiveValue((CommodityKind)commodityToChangeField.GetValue(this)) <= (float)ResortBuffetTable.kHungerToStopFeeding)
                        {
                            ResortBuffetTable.ConsumeMoreFood(Actor);
                        }
                    }
                    else
                    {
                        pushAsContinuation = Target.Parent == Actor && !ShouldDestroyContainer;
                    }
                }
                if (pushAsContinuation)
                {
                    Actor.InteractionQueue.PushAsContinuation(CarrySystem.PutDownHeldObject.Singleton, Target, false, new InteractionPriority(InteractionPriorityLevel.UserDirected), true);
                }
                if (HungerGiven > 0)
                {
                    if (!cookingProcess.IsSpoiled && cookingProcess.Recipe.Key == "Cake Slice")
                    {
                        Actor.BuffManager.AddElement(BuffNames.Meal, (int)(Sims3.Gameplay.Skills.Cooking.GoodMealBuffTuning.kBuffThreshold * HungerGiven / (float)hungerValueThresholdField.GetValue(this)), Sims3.Gameplay.Skills.Cooking.GoodMealBuffTuning.kBuffDuration * HungerGiven / (float)hungerValueThresholdField.GetValue(this), Origin.FromBirthdayCake);
                    }
                    if (isFlaming)
                    {
                        GetType().GetMethod("AddBuff").Invoke(this, new object[]
                            {
                                BuffNames.FlameOn,
                                cookingProcess.Recipe.Key == "BakedAngelFoodCake" ? Origin.FromAngelFoodCake : Origin.None,
                                Actor,
                                HungerGiven / (float)hungerValueThresholdField.GetValue(this)
                            });
                    }
                    TraitFunctions.VegetarianTraitEatingMeatCallback(Actor, cookingProcess.StartedByVegetarian, cookingProcess.Recipe);
                    Food.PostEat(Actor, preparedFood, mIsSufficientlyFullForStuffed, true, mHasFatDelta);
                    if (Target is Snack && cookingProcess.IsVampireFood && !Actor.SimDescription.IsVampire)
                    {
                        Actor.BuffManager.AddElement(BuffNames.Nauseous, Origin.None);
                    }
                    GetType().GetMethod("AddExtraBuffs").Invoke(this, new object[]
                        {
                            preparedFood,
                            chosenIngredients
                        });
                    if (!cookingProcess.Recipe.IsSnack)
                    {
                        cookingProcess.Recipe.LearnFromEating(Actor);
                    }
                    Actor.BuffManager.RemoveElement(BuffNames.MintyBreath);
                }
                if (!Target.CanBeCleanedUp)
                {
                    CarrySystem.ExitCarry(Actor);
                    addToUseList = false;
                    DestroyObject(Target);
                }
                if (Actor.LotCurrent.IsCommunityLot && Target is IPaperContainer || ShouldDestroyContainer)
                {
                    Actor.InteractionQueue.PushAsContinuation(FinishEatingOutside.Singleton, Target, true);
                }
                StandardExit(addToUseList, addToUseList);
                return succeeded;
            }
        }

        public class OrderDrinksPatch : FutureBar.OrderDrinks
        {
            public override bool Run()
            {
                Definition definition = (Definition)InteractionDefinition;
                if (definition.IsPushedRound)
                {
                    FutureBar.FutureBarGlass glass = definition.GlassObjectId.ObjectFromId<FutureBar.FutureBarGlass>();
                    if (glass != null)
                    {
                        glass.SetPosition(glass.GetSlotPosition(Slot.ContainmentSlot_0));
                        glass.SetOpacity(0, 0);
                        glass.AddToWorld();
                        glass.FadeIn();
                        VisualEffect.FireOneShotEffect("ep11BarDrinkPoof_main", Target, Slot.FXJoint_0, VisualEffect.TransitionType.SoftTransition);
                        glass.ParentToSlot(Target, Slot.ContainmentSlot_0);
                    }
                    return true;
                }
                if (!Target.RouteAndCheckInUse(Actor))
                {
                    return false;
                }
                bool isSeated = Actor.Posture is SittingPosture;
                StandardEntry();
                BeginCommodityUpdates();
                GameObject gameObject = GameObject.GetObject(FutureBar.ContainmentSlotInUse(Target));
                if (gameObject != null)
                {
                    gameObject.FadeOut();
                    gameObject.Destroy();
                }
                string materialStateName = definition.GetMaterialStateName();
                FutureBar.FutureBarGlass futureBarGlass = (FutureBar.FutureBarGlass)GlobalFunctions.CreateObjectOutOfWorld("accessoryGlassEP11", ProductVersion.EP11);
                futureBarGlass.SetOpacity(0, 0);
                if (definition.DrinkName == "ServoJuice")
                {
                    futureBarGlass.SetGeometryState("servojuice");
                    futureBarGlass.mCurrentGeoState = "servojuice";
                    futureBarGlass.mCurrentMatState = "drinkAqua";
                }
                else
                {
                    futureBarGlass.SetGeometryState("full");
                    futureBarGlass.SetMaterial(materialStateName);
                    futureBarGlass.mCurrentGeoState = "full";
                    futureBarGlass.mCurrentMatState = materialStateName;
                }
                futureBarGlass.AddToWorld();
                futureBarGlass.Contents.mDrinkName = definition.DrinkName;
                futureBarGlass.Contents.mObjectCreatorId = Target.ObjectId;
                EnterStateMachine("FutureBar", "EnterFutureBar", "x", "futurebar");
                SetParameter("isSeated", isSeated);
                SetParameter("Glass", futureBarGlass);
                AnimateSim("OrderDrink");
                Slots.AttachToSlot(futureBarGlass.ObjectId, Target.ObjectId, 2820733094, true);
                VisualEffect.FireOneShotEffect("ep11BarDrinkPoof_main", Target, Slot.FXJoint_0, VisualEffect.TransitionType.SoftTransition);
                futureBarGlass.FadeIn();
                if (definition.ServingType == FutureBar.ServingType.Servo)
                {
                    FutureBar.DoServoJuiceAction(Actor, futureBarGlass, isSeated);
                }
                else
                {
                    if (definition.IsMultipleServing)
                    {
                        Lot lotCurrent = Actor.LotCurrent;
                        EventTracker.SendEvent(EventTypeId.kOrderedARound, Actor, Target);
                        foreach (FutureBar futureBar in lotCurrent.GetObjects<FutureBar>())
                        {
                            if (futureBar != Target)
                            {
                                gameObject = GameObject.GetObject(FutureBar.ContainmentSlotInUse(futureBar));
                                if (gameObject != null)
                                {
                                    gameObject.FadeOut();
                                    gameObject.Destroy();
                                }
                                FutureBar.FutureBarGlass glass = (FutureBar.FutureBarGlass)GlobalFunctions.CreateObjectOutOfWorld("accessoryGlassEP11", ProductVersion.EP11);
                                if (definition.DrinkName == "ServoJuice")
                                {
                                    glass.SetGeometryState("servojuice");
                                    glass.mCurrentGeoState = "servojuice";
                                    glass.mCurrentMatState = "drinkAqua";
                                }
                                else
                                {
                                    glass.SetGeometryState("full");
                                    glass.SetMaterial(materialStateName);
                                    glass.mCurrentGeoState = "full";
                                    glass.mCurrentMatState = materialStateName;
                                }
                                glass.AddToWorld();
                                glass.Contents.mDrinkName = definition.DrinkName;
                                glass.Contents.mObjectCreatorId = futureBar.ObjectId;
                                FutureBar.OrderDrinks orderDrinks = (FutureBar.OrderDrinks)SingletonRoundPush.CreateInstance(futureBar, null, new InteractionPriority(InteractionPriorityLevel.NonCriticalNPCBehavior), true, true);
                                orderDrinks.SetDrinkNameAndColor(definition.ServingType, definition.DrinkName, definition.DrinkColor, glass.ObjectId);
                                orderDrinks.Run();
                            }
                        }
                    }
                    AnimateSim("ExitFutureBar");
                    if (Actor.SimDescription.IsEP11Bot && definition.DrinkName == "ServoJuice")
                    {
                        FutureBar.DoServoJuiceAction(Actor, futureBarGlass, isSeated);
                    }
                    else if (!Actor.SimDescription.IsEP11Bot && definition.DrinkName != "ServoJuice" && CarrySystem.PickUpWithoutRouting(Actor, futureBarGlass, true))
                    {
                        futureBarGlass.PushDrinkAsContinuation(Actor);
                    }
                }
                EndCommodityUpdates(true);
                StandardExit();
                EventTracker.SendEvent(EventTypeId.kOrderedSynthDrink, Actor, Target);
                return true;
            }
        }

        public abstract class StereoPatch : Stereo
        {
            public new void AddEnjoyingMusicCallback(Sim sim, ReactionBroadcaster reactionBroadcaster)
            {
                if (!sim.IsPet)
                {
                    ulong id = 0;
                    bool isPeripheralStereoSpeaker = false;
                    Stereo stereo = reactionBroadcaster.BroadcastingObject as Stereo;
                    IPeripheralStereoSpeaker peripheralStereoSpeaker = reactionBroadcaster.BroadcastingObject as IPeripheralStereoSpeaker;
                    if (peripheralStereoSpeaker != null && stereo == null)
                    {
                        isPeripheralStereoSpeaker = true;
                        stereo = peripheralStereoSpeaker.StereoMaster as Stereo;
                        id = peripheralStereoSpeaker.ObjectId.Value;
                    }
                    else if (stereo != null)
                    {
                        id = stereo.ObjectId.Value;
                    }
                    if (sim.IsSleeping)
                    {
                        bool isDisturbed = true;
                        if (mCurrentLotStereoRanking == 0 && sim.RoomId != RoomId)
                        {
                            foreach (IPeripheralStereoSpeaker additionalSpeaker in mAdditionalSpeakers)
                            {
                                if (sim.RoomId == additionalSpeaker.RoomId && !additionalSpeaker.IsOn)
                                {
                                    isDisturbed = false;
                                    break;
                                }
                            }
                        }
                        if (isDisturbed)
                        {
                            Sims3.Gameplay.InteractionsShared.ReactToDisturbance.NoiseBroadcastCallback(sim, reactionBroadcaster);
                        }
                    }
                    else if (sim.LotCurrent == mLotCurrent)
                    {
                        int moodScore = 0;
                        if (!isPeripheralStereoSpeaker || isPeripheralStereoSpeaker && peripheralStereoSpeaker.RoomId == stereo.RoomId)
                        {
                            moodScore += TuningStereo.MoodScoreForEnjoyMusicBuff + WallMountedSpeaker.SpeakerBoostForStereo(this);
                        }
                        else if (isPeripheralStereoSpeaker)
                        {
                            moodScore += peripheralStereoSpeaker.StandaloneMoodScore;
                        }
                        if (mPlayingStationsData.IsFavoriteOfSim(sim.SimDescription))
                        {
                            moodScore += BuffEnjoyingMusic.FavoriteMusicMoodScore;
                        }
                        if (Upgradable.SoupUpSpeakers)
                        {
                            moodScore += BuffEnjoyingMusic.IncreasedMoodScore;
                        }
                        sim.BuffManager.RemoveElement(BuffNames.EnjoyingMusic, id);
                        sim.BuffManager.AddElement(BuffNames.EnjoyingMusic, moodScore, Origin.FromStereo, id);
                        if (stereo != null && mPlayingStationsData != null && mPlayingStationsData.IsKidsStation)
                        {
                            if (!stereo.mSimsWithinStereoBroadcast.Contains(sim))
                            {
                                stereo.mSimsWithinStereoBroadcast.Add(sim);
                            }
                            if (sim.SimDescription.IsPregnant)
                            {
                                sim.SimDescription.Pregnancy.StartListeningKidsRadio();
                            }
                        }
                    }
                    sim.RemoveInteractionByType(typeof(DanceTogetherA.Definition));
                    sim.AddInteraction(isPeripheralStereoSpeaker ? new DanceTogetherA.Definition(peripheralStereoSpeaker.GetIDanceable()) : new DanceTogetherA.Definition(stereo), true);
                }
                else if (sim.HasTrait(TraitNames.QuietPet))
                {
                    sim.BuffManager.AddElement(BuffNames.TooNoisyPet, Origin.FromStereo);
                }
            }
        }

        public static void AddDrinkEffects(Sim actor, FutureBar target, string drinkName)
        {
            if (drinkName != "GrossJuice")
            {
                actor.BuffManager.AddElement(BuffNames.SugarRush, Origin.FromFutureBar);
            }
            if (drinkName == "GrossJuice")
            {
                actor.BuffManager.AddElement(BuffNames.OilyTaste, Origin.FromFutureBar);
            }
            else if (Array.Exists(FutureBar.kFavesAndOpposites, x => x.mFaveDrink == FutureBar.GetFavouriteDrinkName(actor, target).mDrinkName && (drinkName == x.mOppositeDrink || FavoritesUtils.FavoriteColorParentDictionary[drinkName] == x.mOppositeDrink)) || Array.Exists(FutureBar.kFavesAndOpposites, x => x.mOppositeDrink == FutureBar.GetFavouriteDrinkName(actor, target).mDrinkName && (drinkName == x.mFaveDrink || FavoritesUtils.FavoriteColorParentDictionary[drinkName] == x.mFaveDrink)))
            {
                actor.BuffManager.AddElement(BuffNames.ItIsRevolting, Origin.FromFutureBar);
            }
            else if (FutureBar.GetFavouriteDrinkName(actor, target).mDrinkName == drinkName || FutureBar.GetFavouriteDrinkName(actor, target).mDrinkName == FavoritesUtils.FavoriteColorParentDictionary[drinkName])
            {
                actor.BuffManager.AddElement(BuffNames.FavouriteDrink, Origin.FromFutureBar);
            }
        }

        public void ChangeFavoriteFoodToVegetarian()
        {
            try
            {
                CASLogic self = (CASLogic)(object)this;
                IRecipe favoriteRecipe = self.GetRecipe(self.FavoriteFoodType);
                if (favoriteRecipe != null && (favoriteRecipe.IsVegetarian || favoriteRecipe.HasVegetarianAlternative))
                {
                    return;
                }
                List<FavoriteFoodType> favoriteFoodTypes = new List<FavoriteFoodType>();
                Array installedFavoriteFoodList = CASCharacter.GetInstalledFavoriteFoodList();
                for (int i = 1; i < installedFavoriteFoodList.Length - 1; i++)
                {
                    FavoriteFoodType favoriteFoodType = (FavoriteFoodType)installedFavoriteFoodList.GetValue(i);
                    IRecipe recipe = self.GetRecipe(favoriteFoodType);
                    if (recipe != null && (recipe.IsVegetarian || recipe.HasVegetarianAlternative))
                    {
                        favoriteFoodTypes.Add(favoriteFoodType);
                    }
                }
                self.ExecuteSetFavoriteFood(favoriteFoodTypes[RandomUtil.GetInt(favoriteFoodTypes.Count - 1)]);
            }
            catch (Exception ex)
            {
                StyledNotification.Show(new StyledNotification.Format(ex.Message, StyledNotification.NotificationStyle.kDebugAlert));
            }
        }

        public void CreateDrinkList()
        {
            foreach (CASCharacter.NameColorPair nameColorPair in CASCharacter.kColors)
            {
                if (!nameColorPair.IsBlacklisted() && !nameColorPair.IsHidden())
                {
                    FutureBar.DrinkData drinkDatum = default(FutureBar.DrinkData);
                    drinkDatum.mDrinkName = nameColorPair.mName;
                    drinkDatum.mDrinkColor = nameColorPair.mColor;
                    ((FutureBar)(object)this).mDrinkData.Add(drinkDatum);
                }
            }
        }

        public static string GetFavoriteFood(FavoriteFoodType foodType)
        {
            return foodType == FavoriteFoodType.None ? Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/CAS/Favorites:None") : FavoritesUtils.FavoriteFoodDictionary.ContainsKey(foodType) ? FavoritesUtils.FavoriteFoodDictionary[foodType].Recipe == null ? null : FavoritesUtils.FavoriteFoodDictionary[foodType].Recipe.GenericName : Responder.Instance.LocalizationModel.LocalizeString("Gameplay/Excel/RecipeMasterList/Data:" + foodType);
        }

        public static string GetFavoriteFoodPngName(FavoriteFoodType foodType)
        {
            if (foodType == FavoriteFoodType.None)
            {
                foodType = FavoriteFoodType.Hamburger;
            }
            FavoritesUtils.FavoriteFood favoriteFood;
            return FavoritesUtils.FavoriteFoodDictionary.TryGetValue(foodType, out favoriteFood) ? string.IsNullOrEmpty(favoriteFood.IconKey) ? "cas_favorites_food_i_hamburger_r2" : favoriteFood.IconKey : "cas_favorites_food_i_" + foodType + "_r2";
        }

        public static UIImage GetFavoriteFoodSmallIcon(FavoriteFoodType foodType)
        {
            FavoritesUtils.FavoriteFood favoriteFood;
            return UIManager.LoadUIImage(ResourceKey.CreatePNGKey(FavoritesUtils.FavoriteFoodDictionary.TryGetValue(foodType, out favoriteFood) ? string.IsNullOrEmpty(favoriteFood.SmallIconKey) ? "cas_favorites_food_i_hamburger_s_r2" : favoriteFood.SmallIconKey : "cas_favorites_food_i_" + foodType + "_s_r2", 0));
        }

        public static string GetFavoriteMusic(FavoriteMusicType musicType)
        {
            return musicType == FavoriteMusicType.None ? Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/CAS/Favorites:None") : Responder.Instance.LocalizationModel.LocalizeString("Gameplay/Excel/Stereo/Stations:" + (FavoritesUtils.FavoriteMusicDictionary.ContainsKey(musicType) ? FavoritesUtils.FavoriteMusicDictionary[musicType].Name : musicType.ToString()));
        }

        public static string GetFavoriteMusicPngName(FavoriteMusicType musicType)
        {
            if (musicType == FavoriteMusicType.None)
            {
                musicType = FavoriteMusicType.Electronica;
            }
            FavoritesUtils.FavoriteMusic favoriteMusic;
            if (FavoritesUtils.FavoriteMusicDictionary.TryGetValue(musicType, out favoriteMusic))
            {
                return string.IsNullOrEmpty(favoriteMusic.IconKey) ? "cas_favorites_music_i_electronica_r2" : favoriteMusic.IconKey;
            }
            switch (musicType)
            {
                case FavoriteMusicType.Electronica:
                case FavoriteMusicType.Pop:
                case FavoriteMusicType.Latin:
                case FavoriteMusicType.Indie:
                case FavoriteMusicType.Classical:
                case FavoriteMusicType.Kids:
                case FavoriteMusicType.France:
                case FavoriteMusicType.China:
                case FavoriteMusicType.Egypt:
                case FavoriteMusicType.Roots:
                case FavoriteMusicType.Soul:
                case FavoriteMusicType.Rockabilly:
                case FavoriteMusicType.Custom:
                    return "cas_favorites_music_i_" + musicType + "_r2";
            }
            return "cas_favs_music_i_" + musicType;
        }

        public static UIImage GetFavoriteMusicSmallIcon(FavoriteMusicType musicType)
        {
            FavoritesUtils.FavoriteMusic favoriteMusic;
            if (FavoritesUtils.FavoriteMusicDictionary.TryGetValue(musicType, out favoriteMusic))
            {
                return GetMusicIcon(string.IsNullOrEmpty(favoriteMusic.SmallIconKey) ? "cas_favorites_music_i_electronica_s_r2" : favoriteMusic.SmallIconKey, musicType);
            }
            switch (musicType)
            {
                case FavoriteMusicType.Electronica:
                case FavoriteMusicType.Pop:
                case FavoriteMusicType.Latin:
                case FavoriteMusicType.Indie:
                case FavoriteMusicType.Classical:
                case FavoriteMusicType.Kids:
                case FavoriteMusicType.France:
                case FavoriteMusicType.China:
                case FavoriteMusicType.Egypt:
                case FavoriteMusicType.Roots:
                case FavoriteMusicType.Soul:
                case FavoriteMusicType.Rockabilly:
                case FavoriteMusicType.Custom:
                    return GetMusicIcon("cas_favorites_music_i_" + musicType + "_s_r2", musicType);
            }
            return GetMusicIcon("cas_favs_music_i_" + musicType + "_s", musicType);
        }

        public static Array GetInstalledFavoriteFoodList()
        {
            List<FavoriteFoodType> favoriteFoodTypes = new List<FavoriteFoodType>();
            foreach (FavoriteFoodType foodType in Enum.GetValues(typeof(FavoriteFoodType)))
            {
                if (foodType != FavoriteFoodType.VampireFood && foodType != FavoriteFoodType.Kelp && Responder.Instance.CASModel.GetRecipe(foodType) != null)
                {
                    favoriteFoodTypes.Add(foodType);
                }
            }
            favoriteFoodTypes.AddRange(new List<FavoriteFoodType>(FavoritesUtils.FavoriteFoodDictionary.Keys).FindAll(x => !Array.Exists(Enum.GetNames(typeof(FavoriteFoodType)), y => y == FavoritesUtils.FavoriteFoodDictionary[x].Name)));
            return favoriteFoodTypes.ToArray();
        }

        public static Array GetInstalledFavoriteMusicList()
        {
            List<FavoriteMusicType> favoriteMusicTypes = new List<FavoriteMusicType>();
            foreach (FavoriteMusicType musicType in Enum.GetValues(typeof(FavoriteMusicType)))
            {
                if (Responder.Instance.CASModel.IsMusicTypeInstalled(musicType))
                {
                    favoriteMusicTypes.Add(musicType);
                }
            }
            favoriteMusicTypes.AddRange(new List<FavoriteMusicType>(FavoritesUtils.FavoriteMusicDictionary.Keys).FindAll(x => !Array.Exists(Enum.GetNames(typeof(FavoriteMusicType)), y => y == FavoritesUtils.FavoriteMusicDictionary[x].Name)));
            return favoriteMusicTypes.ToArray();
        }

        public static UIImage GetMusicIcon(string imageFileName, FavoriteMusicType musicType)
        {
            return UIManager.LoadUIImage(ResourceKey.CreatePNGKey(imageFileName, FavoritesUtils.FavoriteMusicDictionary.ContainsKey(musicType) ? 0 : ResourceUtils.ProductVersionToGroupId(Responder.Instance.GetProductVersionForStereoStation(musicType))));
        }

        public IRecipe GetRecipe(FavoriteFoodType foodType)
        {
            Recipe recipe;
            FavoritesUtils.FavoriteFood favoriteFood;
            return FavoritesUtils.FavoriteFoodDictionary.TryGetValue(foodType, out favoriteFood) ? favoriteFood.Recipe : Recipe.NameToRecipeHash.TryGetValue(foodType.ToString(), out recipe) ? recipe : null;
        }

        public static string GetStationName(FavoriteMusicType musicType)
        {
            List<string> stereoStationNames = new List<string>();
            foreach (string key in StereoStationData.sStereoStationDictionary.Keys)
            {
                if (StereoStationData.sStereoStationDictionary[key].mFavouriteMusicType == musicType)
                {
                    stereoStationNames.Add(key);
                }
            }
            FavoritesUtils.FavoriteMusic favoriteMusic;
            if (FavoritesUtils.FavoriteMusicDictionary.TryGetValue(musicType, out favoriteMusic))
            {
                stereoStationNames.Add("Gameplay/Excel/Stereo/Stations:" + favoriteMusic.Name);
            }
            return stereoStationNames.Count == 0 ? null : RandomUtil.GetRandomObjectFromList(stereoStationNames);
        }

        public void OnFavoritesVisibilityChange(WindowBase sender, UIVisibilityChangeEventArgs args)
        {
            CASCharacter self = (CASCharacter)(object)this;
            if (args.Visible)
            {
                sender.Tick += self.OnFavoritesVisibilityTick;
                switch (sender.ID)
                {
                    case 100663297:
                        {
                            Array visibleFavorites = Array.FindAll((FavoriteFoodType[])GetInstalledFavoriteFoodList(), x => !x.IsBlacklisted() && !x.IsHidden());
                            self.PopulateFavoritesGrid(self.mGridFavoriteFood, visibleFavorites, 0, visibleFavorites.Length);
                            break;
                        }
                    case 100663301:
                        {
                            Array visibleFavorites = Array.FindAll((FavoriteMusicType[])GetInstalledFavoriteMusicList(), x => !x.IsBlacklisted() && !x.IsHidden());
                            self.PopulateFavoritesGrid(self.mGridFavoriteMusic, visibleFavorites, 0, visibleFavorites.Length);
                            break;
                        }
                    case 100663305:
                        {
                            Array visibleFavorites = Array.FindAll(CASCharacter.kColors, x => !x.IsBlacklisted() && !x.IsHidden());
                            self.PopulateFavoritesGrid(self.mGridFavoriteColor, visibleFavorites, 0, visibleFavorites.Length);
                            break;
                        }
                }
            }
            else
            {
                sender.Tick -= self.OnFavoritesVisibilityTick;
            }
        }

        public static void PopulateFavoritesGrid(ItemGrid grid, Array favoritesList, int startIndex, int endIndex, FavoriteFoodType favoriteFood, FavoriteMusicType favoriteMusic, Color favoriteColor, bool isVegetarian)
        {
            grid.Clear();
            grid.ForceCellTooltips = true;
            /*
            uint rowCount = (uint)Math.Ceiling((endIndex - startIndex) / (float)grid.VisibleColumns);
            if (rowCount != grid.VisibleRows)
            {
                int emptyRowCount = (int)(rowCount - grid.VisibleRows);
                grid.VisibleRows = rowCount;
                float toAddToHeight = (float)emptyRowCount * (grid.CellArea.y + grid.CellPadding.Height);
                Rect area = grid.Area;
                area.Height += toAddToHeight;
                grid.Area = area;
                area = grid.Parent.Area;
                area.Height += toAddToHeight;
                grid.Parent.Area = area;
            }
            */
            for (int i = startIndex; i < endIndex; i++)
            {
                object favorite = favoritesList.GetValue(i);
                Layout layout = UIManager.LoadLayout(ResourceKey.CreateUILayoutKey("FavoritesGridItem", 0));
                Window window = (Window)layout.GetWindowByExportID(1);
                ImageDrawable imageDrawable = (ImageDrawable)((Window)window.GetChildByID(32, false)).Drawable;
                bool isCurrentFavorite = false;
                if (favorite is FavoriteFoodType)
                {
                    FavoriteFoodType favoriteFoodType = (FavoriteFoodType)favorite;
                    IRecipe recipe = Responder.Instance.CASModel.GetRecipe(favoriteFoodType) ?? (FavoritesUtils.FavoriteFoodDictionary.ContainsKey(favoriteFoodType) ? FavoritesUtils.FavoriteFoodDictionary[favoriteFoodType].Recipe : null);
                    if (recipe != null)
                    {
                        if (isVegetarian)
                        {
                            if (!recipe.IsVegetarian && !recipe.HasVegetarianAlternative)
                            {
                                continue;
                            }
                            window.TooltipText = recipe.GenericVegetarianName;
                        }
                        else
                        {
                            window.TooltipText = recipe.GenericName;
                        }
                        imageDrawable.Image = CASCharacter.GetFavoriteFoodIcon(favoriteFoodType);
                        isCurrentFavorite = favoriteFoodType == favoriteFood;
                    }
                }
                else if (favorite is FavoriteMusicType)
                {
                    FavoriteMusicType favoriteMusicType = (FavoriteMusicType)favorite;
                    window.TooltipText = CASCharacter.GetFavoriteMusicName(favoriteMusicType);
                    imageDrawable.Image = CASCharacter.GetFavoriteMusicIcon(favoriteMusicType);
                    isCurrentFavorite = favoriteMusicType == favoriteMusic;
                }
                else if (favorite is CASCharacter.NameColorPair)
                {
                    CASCharacter.NameColorPair nameColorPair = (CASCharacter.NameColorPair)favorite;
                    window.TooltipText = CASCharacter.GetLocalizedColorName(nameColorPair.mName);
                    imageDrawable.Image = CASCharacter.GetFavoriteColorIcon(nameColorPair.mColor);
                    isCurrentFavorite = nameColorPair.mColor.ARGB == favoriteColor.ARGB;
                }
                grid.AddItem(new ItemGridCellItem(window, favorite));
                if (isCurrentFavorite)
                {
                    grid.SelectedItem = grid.Count - 1;
                }
            }
        }

        public static void RandomizeAllFavorites(bool missingFavoritesOnly)
        {
            ICASModel casModel = Responder.Instance.CASModel;
            Random random = new Random();
            FavoriteFoodType favoriteFood = casModel.FavoriteFoodType;
            if (!missingFavoritesOnly || favoriteFood == FavoriteFoodType.None)
            {
                Array visibleFavorites = Array.FindAll((FavoriteFoodType[])GetInstalledFavoriteFoodList(), x => !x.IsBlacklisted() && !x.IsHidden());
                favoriteFood = (FavoriteFoodType)visibleFavorites.GetValue(random.Next(1, visibleFavorites.Length));
                if (casModel.IsVegetarian())
                {
                    IRecipe recipe = casModel.GetRecipe(favoriteFood);
                    while (!recipe.IsVegetarian && !recipe.HasVegetarianAlternative)
                    {
                        favoriteFood = (FavoriteFoodType)visibleFavorites.GetValue(random.Next(1, visibleFavorites.Length));
                        recipe = casModel.GetRecipe(favoriteFood);
                    }
                }
            }
            FavoriteMusicType favoriteMusic = casModel.FavoriteMusicType;
            if (!missingFavoritesOnly || favoriteMusic == FavoriteMusicType.None)
            {
                Array visibleFavorites = Array.FindAll((FavoriteMusicType[])GetInstalledFavoriteMusicList(), x => !x.IsBlacklisted() && !x.IsHidden());
                for (favoriteMusic = FavoriteMusicType.Custom; favoriteMusic == FavoriteMusicType.Custom; favoriteMusic = (FavoriteMusicType)visibleFavorites.GetValue(random.Next(1, visibleFavorites.Length)))
                {
                }
            }
            Color favoriteColor = casModel.FavoriteColor;
            if (!missingFavoritesOnly || favoriteColor.ARGB == 0)
            {
                Array visibleFavorites = Array.FindAll(CASCharacter.kColors, x => !x.IsBlacklisted() && !x.IsHidden());
                favoriteColor = ((CASCharacter.NameColorPair)visibleFavorites.GetValue(random.Next(0, visibleFavorites.Length))).mColor;
            }
            casModel.RequestRandomFavorites(favoriteFood, favoriteMusic, favoriteColor);
        }

        public void RandomizeFavoriteMusic()
        {
            SimDescription self = (SimDescription)(object)this;
            Array visibleFavoriteMusicList = Array.FindAll((FavoriteMusicType[])GetInstalledFavoriteMusicList(), x => !x.IsBlacklisted() && !x.IsHidden());
            self.mFavouriteMusicType = FavoriteMusicType.None;
            while (self.mFavouriteMusicType == FavoriteMusicType.Custom || self.mFavouriteMusicType == FavoriteMusicType.None)
            {
                self.mFavouriteMusicType = (FavoriteMusicType)visibleFavoriteMusicList.GetValue(RandomUtil.GetInt(1, visibleFavoriteMusicList.Length - 1));
            }
        }

        public void RandomizePreferences()
        {
            SimDescription self = (SimDescription)(object)this;
            Array visibleFavoriteColorList = Array.FindAll(CASCharacter.kColors, x => !x.IsBlacklisted() && !x.IsHidden()),
            visibleFavoriteFoodList = Array.FindAll((FavoriteFoodType[])GetInstalledFavoriteFoodList(), x => !x.IsBlacklisted() && !x.IsHidden());
            self.mFavouriteColor = ((CASCharacter.NameColorPair)visibleFavoriteColorList.GetValue(RandomUtil.GetInt(0, visibleFavoriteColorList.Length - 1))).mColor;
            self.mFavouriteFoodType = (FavoriteFoodType)visibleFavoriteFoodList.GetValue(RandomUtil.GetInt(1, visibleFavoriteFoodList.Length - 1));
            self.RandomizeFavoriteMusic();
            self.mZodiacSign = (Zodiac)RandomUtil.GetInt(0, 11);
        }
    }
}
