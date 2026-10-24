using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.InteractionsShared;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Services;
using Sims3.Gameplay.Skills;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff.Services;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using Sims3.SimIFace.CustomContent;
using Sims3.UI;
using Sims3.UI.Controller;
using Sims3.UI.Hud;
using System;
using System.Collections.Generic;
using System.Globalization;
using Destrospean.Enums;
using Destrospean.Enums.ExpandedHouseholdStaff;
using Destrospean.ExpandedHouseholdStaff.Interactions;
using Destrospean.Misc;
using Destrospean.UI.Columns;
using Destrospean.UI.Columns.ExpandedHouseholdStaff;
using ObjectPickerDialog = Destrospean.UI.Dialogs.ObjectPickerDialog;

namespace Destrospean.Utils.ExpandedHouseholdStaff
{
    public static class ServiceUtils
    {
        public delegate string GetUniformNameDelegate(SimDescription simDescription);

        [Persistable]
        public class ActiveTopicAction
        {
            short mGrouping;

            public LongTermRelationshipTypes Grouping
            {
                get
                {
                    return (LongTermRelationshipTypes)mGrouping;
                }
                set
                {
                    mGrouping = (short)value;
                }
            }

            /// <summary>
            /// If set to <c>true</c>, the action is for the first person actor; otherwise, it's for the second person actor.
            /// </summary>
            public bool IsActive;

            public string Name;

            public ActiveTopicAction()
            {
            }

            /// <summary>
            /// Initializes a new instance of the
            /// <see cref="Destrospean.Utils.ExpandedHouseholdStaff.ServiceUtils+ActiveTopicAction"/> class.
            /// </summary>
            /// <param name="name">Name.</param>
            /// <param name="grouping">Grouping.</param>
            /// <param name="isActive">If set to <c>true</c>,the action is for the first person actor; otherwise, it's for the second person actor.</param>
            public ActiveTopicAction(string name, LongTermRelationshipTypes grouping = LongTermRelationshipTypes.Default, bool isActive = false)
            {
                Name = name;
                Grouping = grouping;
                IsActive = isActive;
            }
        }

        [Persistable]
        public class CommodityChange
        {
            int mUpdateAboveAndBelowZero;

            int mUpdateType;

            public float ActualValue;

            public float ConstantChange;

            public string InteractionDefinitionType;

            public bool Locked;

            public string TargetType;

            public bool TimeDependsOnCommodityFilling;

            public UpdateAboveAndBelowZeroType UpdateAboveAndBelowZero
            {
                get
                {
                    return (UpdateAboveAndBelowZeroType)mUpdateAboveAndBelowZero;
                }
                set
                {
                    mUpdateAboveAndBelowZero = (int)value;
                }
            }

            public bool UpdateEvenOnFailure;

            public OutputUpdateType UpdateType
            {
                get
                {
                    return (OutputUpdateType)mUpdateType;
                }
                set
                {
                    mUpdateType = (int)value;
                }
            }

            public CommodityChange()
            {
            }

            public CommodityChange(string interactionDefinitionType, string targetType, float constantChange, bool locked, float actualValue, OutputUpdateType updateType, bool timeDependsOnCommodityFilling = false, bool updateEvenOnFailure = false, UpdateAboveAndBelowZeroType updateAboveAndBelowZero = UpdateAboveAndBelowZeroType.Either)
            {
                InteractionDefinitionType = interactionDefinitionType;
                TargetType = targetType;
                ConstantChange = constantChange;
                Locked = locked;
                ActualValue = actualValue;
                UpdateType = updateType;
                TimeDependsOnCommodityFilling = timeDependsOnCommodityFilling;
                UpdateEvenOnFailure = updateEvenOnFailure;
                UpdateAboveAndBelowZero = updateAboveAndBelowZero;
            }

            public CommodityChange(Type interactionDefinitionType, Type targetType, float constantChange, bool locked, float actualValue, OutputUpdateType updateType, bool timeDependsOnCommodityFilling = false, bool updateEvenOnFailure = false, UpdateAboveAndBelowZeroType updateAboveAndBelowZero = UpdateAboveAndBelowZeroType.Either) : this(interactionDefinitionType.FullName, targetType.FullName, constantChange, locked, actualValue, updateType, timeDependsOnCommodityFilling, updateEvenOnFailure, updateAboveAndBelowZero)
            {
            }
        }

        [Persistable]
        public class ServiceProfile : IServiceProfile
        {
            List<ActiveTopicAction> mActions = new List<ActiveTopicAction>();

            uint mCarGroupId = 0x00000000;

            ulong mCarInstanceId = 0x0000000000000000;

            float mCheckTime = 5f;

            float mDelayBeforeArriving = 1f;

            float mDelayBeforeLeaving = 20f;

            float mDriveTime = 5f;

            float mExtraWaitTimeAfterSocializing = 30f;

            ulong mFlags = 0uL;

            List<ulong> mHiddenTraits = new List<ulong>();

            List<InventoryObjectCreationParameters> mInventory = new List<InventoryObjectCreationParameters>();

            bool mIsImmutable = false;

            List<int> mMotives = new List<int>();

            List<CommodityChange> mOutputs = new List<CommodityChange>();

            int mPotentialTraitCount = 0;

            List<ulong> mPotentialTraits = new List<ulong>();

            float mRelationshipLevelForQuit = -50f;

            int mServiceMotive = 0;

            ulong mServiceType = 1uL;

            List<SkillLevelPair> mSkills = new List<SkillLevelPair>();

            Dictionary<string, string> mStrings = new Dictionary<string, string>();

            float mTimeToSpendWorking = 8f;

            float mTimeWaitBeforePutawayLeftovers = 60f;

            List<ulong> mTraits = new List<ulong>();

            float mUseObjectInSameRoomAsSleeperMultiplier = 0.1f;

            uint mValidAges = 48u;

            uint mValidGenders = 0u;

            /// <summary>
            /// The list of actions for the service topic.
            /// </summary>
            public List<ActiveTopicAction> Actions
            {
                get
                {
                    return mActions;
                }
                set
                {
                    mActions = value; 
                }
            }

            /// <summary>
            /// If set to <c>true</c>, the service always tries to send the same NPC to a specific household.
            /// </summary>
            public bool AlwaysTryToSendTheSameSim
            {
                get
                {
                    return (mFlags & (ulong)ServiceProfileFlags.AlwaysTryToSendTheSameSim) != 0uL;
                }
                set
                {
                    if (value)
                    {
                        mFlags |= (ulong)ServiceProfileFlags.AlwaysTryToSendTheSameSim;
                    }
                    else
                    {
                        mFlags &= ulong.MaxValue ^ (ulong)ServiceProfileFlags.AlwaysTryToSendTheSameSim;
                    }
                }
            }

            /// <summary>
            /// The message that shows when the service is cancelled.
            /// </summary>
            public string CancelledMessage
            {
                get
                {
                    string value;
                    return mStrings.TryGetValue("CancelledMessage", out value) ? value : null;
                }
                set
                {
                    mStrings["CancelledMessage"] = value;
                }
            }

            /// <summary>
            /// The message that shows when the service is cancelled and there is already a service NPC of that service on the lot.
            /// Note: this will only be used when <see cref="IsLiveInService"/> is set to <c>false</c>.
            /// <see cref="CancelledMessage"/> will be used instead if <see cref="IsLiveInService"/> is set to <c>true</c>.
            /// </summary>
            public string CancelledWhileActiveMessage
            {
                get
                {
                    string value;
                    return mStrings.TryGetValue("CancelledWhileActiveMessage", out value) ? value : null;
                }
                set
                {
                    mStrings["CancelledWhileActiveMessage"] = value;
                }
            }

            /// <summary>
            /// The group ID of the car the service NPC arrives in. This is for when the car is a Store, expansion pack, or stuff pack item.
            /// </summary>
            public uint CarGroupId
            {
                get
                {
                    return mCarGroupId;
                }
                set
                {
                    mCarGroupId = value;
                }
            }

            /// <summary>
            /// The instance ID of the car the service NPC arrives in.
            /// </summary>
            public ulong CarInstanceId
            {
                get
                {
                    ulong nameGuid = NameGuidMap.GetGuidByName(string.IsNullOrEmpty(CarInstanceName) ? "CarServiceSedan" : CarInstanceName);
                    return nameGuid == NameGuidMap.kInvalidNameGuid ? mCarInstanceId : nameGuid;
                }
                set
                {
                    mCarInstanceId = value;
                }
            }

            /// <summary>
            /// The instance name of the car the service NPC arrives in. If <c>null</c> or empty, the service NPC will arrive and leave by foot.
            /// </summary>
            public string CarInstanceName
            {
                get
                {
                    string value;
                    return mStrings.TryGetValue("CarInstanceName", out value) ? value : null;
                }
                set
                {
                    mStrings["CarInstanceName"] = value;
                }
            }

            /// <summary>
            /// The preset of the car the service NPC arrives in.
            /// </summary>
            public string CarPreset
            {
                get
                {
                    string value;
                    return mStrings.TryGetValue("CarPreset", out value) ? value : null;
                }
                set
                {
                    mStrings["CarPreset"] = value;
                }
            }

            /// <summary>
            /// Length of time (in minutes) between checks that everything is done.
            /// </summary>
            public float CheckTime
            {
                get
                {
                    return mCheckTime;
                }
                set
                {
                    mCheckTime = value;
                }
            }

            public int Cost = 50;

            /// <summary>
            /// Length of time (in minutes) that the service NPC waits before routing to lot.
            /// </summary>
            public float DelayBeforeArriving
            {
                get
                {
                    return mDelayBeforeArriving;
                }
                set
                {
                    mDelayBeforeArriving = value;
                }
            }

            /// <summary>
            /// Length of time (in minutes) that the service NPC waits before leaving the lot, after their work is done.
            /// </summary>
            public float DelayBeforeLeaving
            {
                get
                {
                    return mDelayBeforeLeaving;
                }
                set
                {
                    mDelayBeforeLeaving = value;
                }
            }

            /// <summary>
            /// Length of time (in minutes) that the service NPC takes to drive to lot.
            /// </summary>
            public float DriveTime
            {
                get
                {
                    return mDriveTime;
                }
                set
                {
                    mDriveTime = value;
                }
            }

            /// <summary>
            /// Extra time (in minutes) to wait before leaving if the service NPC is socialized with.
            /// </summary>
            public float ExtraWaitTimeAfterSocializing
            {
                get
                {
                    return mExtraWaitTimeAfterSocializing;
                }
                set
                {
                    mExtraWaitTimeAfterSocializing = value;
                }
            }
                
            public bool GetUniformFromName = false;

            public GetUniformNameDelegate GetUniformNameCallback = null;

            /// <summary>
            /// The list of hidden traits for the service NPC.
            /// Note: do not add or remove elements from this property directly, as that will not work. Use the <see cref="AddHiddenTraits"/> and <see cref="RemoveHiddenTraits"/> methods instead.
            /// </summary>
            public List<TraitNames> HiddenTraits
            {
                get
                {
                    return mHiddenTraits.ConvertAll(x => (TraitNames)x);
                }
                set
                {
                    mHiddenTraits = value.ConvertAll(x => (ulong)x);
                }
            }

            /// <summary>
            /// The items the service NPC spawns with.
            /// </summary>
            public List<InventoryObjectCreationParameters> Inventory
            {
                get
                {
                    return mInventory;
                }
                set
                {
                    mInventory = value;
                }
            }

            /// <summary>
            /// If set to <c>true</c>, the service NPC will care for babies, toddlers, and children.
            /// </summary>
            public bool IsBabysittingService
            {
                get
                {
                    return (mFlags & (ulong)ServiceProfileFlags.BabysittingService) != 0uL;
                }
                set
                {
                    if (value)
                    {
                        mFlags |= (ulong)ServiceProfileFlags.BabysittingService;
                    }
                    else
                    {
                        mFlags &= ulong.MaxValue ^ (ulong)ServiceProfileFlags.BabysittingService;
                    }
                }
            }

            /// <summary>
            /// If set to <c>true</c>, this service charges <see cref="Cost"/> only if falsely called.
            /// </summary>
            public bool IsEmergencyService
            {
                get
                {
                    return (mFlags & (ulong)ServiceProfileFlags.EmergencyService) != 0uL;
                }
                set
                {
                    if (value)
                    {
                        mFlags |= (ulong)ServiceProfileFlags.EmergencyService;
                    }
                    else
                    {
                        mFlags &= ulong.MaxValue ^ (ulong)ServiceProfileFlags.EmergencyService;
                    }
                }
            }

            /// <summary>
            /// If set to <c>true</c>, the service cannot be changed in the game.
            /// </summary>
            public bool IsImmutable
            {
                get
                {
                    return mIsImmutable;
                }
                set
                {
                    mIsImmutable = value;
                }
            }

            /// <summary>
            /// If set to <c>true</c>, the service NPC will stay with the household that requested them.
            /// Interactions for setting/unsetting their bed will be available to the service NPC with this property set to <c>true</c>.
            /// </summary>
            public bool IsLiveInService
            {
                get
                {
                    return (mFlags & (ulong)ServiceProfileFlags.LiveInService) != 0uL;
                }
                set
                {
                    if (value)
                    {
                        mFlags |= (ulong)ServiceProfileFlags.LiveInService;
                    }
                    else
                    {
                        mFlags &= ulong.MaxValue ^ (ulong)ServiceProfileFlags.LiveInService;
                    }
                }
            }

            /// <summary>
            /// If set to <c>true</c>, the service NPC will avoid interacting with objects in the same room as a sleeping Sim.
            /// </summary>
            public bool IsQuietAroundSleepingSims
            {
                get
                {
                    return (mFlags & (ulong)ServiceProfileFlags.QuietAroundSleepingSims) != 0uL;
                }
                set
                {
                    if (value)
                    {
                        mFlags |= (ulong)ServiceProfileFlags.QuietAroundSleepingSims;
                    }
                    else
                    {
                        mFlags &= ulong.MaxValue ^ (ulong)ServiceProfileFlags.QuietAroundSleepingSims;
                    }
                }
            }

            /// <summary>
            /// Whether the service is recurrent (<c>true</c>) or one-off (<c>false</c>). Recurrent services are the only type which can be cancelled through the phone dialog.
            /// </summary>
            public bool IsRecurrent
            {
                get
                {
                    return (mFlags & (ulong)ServiceProfileFlags.Recurrent) != 0uL;
                }
                set
                {
                    if (value)
                    {
                        mFlags |= (ulong)ServiceProfileFlags.Recurrent;
                    }
                    else
                    {
                        mFlags &= ulong.MaxValue ^ (ulong)ServiceProfileFlags.Recurrent;
                    }
                }
            }

            /// <summary>
            /// If set to <c>true</c>, the service NPC will quit when they are in close proximity to Bonehilda, and will scream and run away dramatically.
            /// </summary>
            public bool IsScaredOfBonehilda
            {
                get
                {
                    return (mFlags & (ulong)ServiceProfileFlags.ScaredOfBonehilda) != 0uL;
                }
                set
                {
                    if (value)
                    {
                        mFlags |= (ulong)ServiceProfileFlags.ScaredOfBonehilda;
                    }
                    else
                    {
                        mFlags &= ulong.MaxValue ^ (ulong)ServiceProfileFlags.ScaredOfBonehilda;
                    }
                }
            }

            public int MaxNumNPCsInPool = 1;

            /// <summary>
            /// The service motives. If assigning manually (rather than as a parameter in the constructor) be sure include the <see cref="ServiceMotive"/> property.
            /// Note: do not add or remove elements from this property directly, as that will not work. Use the <see cref="AddMotives"/> and <see cref="RemoveMotives"/> methods instead.
            /// </summary>
            public List<CommodityKind> Motives
            {
                get
                {
                    return mMotives.ConvertAll(x => (CommodityKind)x);
                }
                set
                {
                    mMotives = value.ConvertAll(x => (int)x);
                }
            }

            /// <summary>
            /// The internal name of the service. Must be unique.
            /// </summary>
            public string Name
            {
                get
                {
                    string value;
                    return mStrings.TryGetValue("Name", out value) ? value : null;
                }
                set
                {
                    mStrings["Name"] = value.Replace(' ', '_');
                }
            }

            /// <summary>
            /// The list of outputs added to interaction tunings for the service motive.
            /// </summary>
            public List<CommodityChange> Outputs
            {
                get
                {
                    return mOutputs;
                }
                set
                {
                    mOutputs = value;
                }
            }

            /// <summary>
            /// The number of potential traits to randomly pick from the list of potential traits (see <see cref="PotentialTraits"/>) for the service NPC.
            /// </summary>
            public int PotentialTraitCount
            {
                get
                {
                    return mPotentialTraitCount;
                }
                set
                {
                    mPotentialTraitCount = value;
                }
            }

            /// <summary>
            /// The list of potential traits for the service NPC; will be randomly picked from, the number of which is specified by <see cref="PotentialTraitCount"/>.
            /// Note: do not add or remove elements from this property directly, as that will not work. Use the <see cref="AddPotentialTraits"/> and <see cref="RemovePotentialTraits"/> methods instead.
            /// </summary>
            public List<TraitNames> PotentialTraits
            {
                get
                {
                    return mPotentialTraits.ConvertAll(x => (TraitNames)x);
                }
                set
                {
                    mPotentialTraits = value.ConvertAll(x => (ulong)x);
                }
            }

            /// <summary>
            /// If set to <c>true</c>, the service NPC will have standard social interactions unavailable.
            /// </summary>
            public bool PreventSocialization
            {
                get
                {
                    return (mFlags & (ulong)ServiceProfileFlags.PreventSocialization) != 0uL;
                }
                set
                {
                    if (value)
                    {
                        mFlags |= (ulong)ServiceProfileFlags.PreventSocialization;
                    }
                    else
                    {
                        mFlags &= ulong.MaxValue ^ (ulong)ServiceProfileFlags.PreventSocialization;
                    }
                }
            }

            // <summary>
            /// If the service NPC's relationship with any YAE falls below this level, they will quit.
            /// </summary>
            public float RelationshipLevelForQuit
            {
                get
                {
                    return mRelationshipLevelForQuit;
                }
                set
                {
                    mRelationshipLevelForQuit = value;
                }
            }

            /// <summary>
            /// If set to <c>true</c>, the service NPC will call emergency services when there is a fire on the lot they're assigned to.
            /// </summary>
            public bool ReportsFires
            {
                get
                {
                    return (mFlags & (ulong)ServiceProfileFlags.ReportsFires) != 0uL;
                }
                set
                {
                    if (value)
                    {
                        mFlags |= (ulong)ServiceProfileFlags.ReportsFires;
                    }
                    else
                    {
                        mFlags &= ulong.MaxValue ^ (ulong)ServiceProfileFlags.ReportsFires;
                    }
                }
            }
                
            /// <summary>
            /// The message that shows when a service NPC is requested.
            /// </summary>
            public string RequestedMessage
            {
                get
                {
                    string value;
                    return mStrings.TryGetValue("RequestedMessage", out value) ? value : null;
                }
                set
                {
                    mStrings["RequestedMessage"] = value;
                }
            }

            /// <summary>
            /// If set to <c>true</c>, the service NPC is required to be in the same room as a Sim they are caring for.
            /// </summary>
            public bool RequireBeInSameRoom
            {
                get
                {
                    return (mFlags & (ulong)ServiceProfileFlags.RequireBeInSameRoom) != 0uL;
                }
                set
                {
                    if (value)
                    {
                        mFlags |= (ulong)ServiceProfileFlags.RequireBeInSameRoom;
                    }
                    else
                    {
                        mFlags &= ulong.MaxValue ^ (ulong)ServiceProfileFlags.RequireBeInSameRoom;
                    }
                }
            }
                
            /// <summary>
            /// Note: this can (and should) be omitted for the UI for creating/editing service profiles.
            /// </summary>
            public CommodityKind ServiceMotive
            {
                get
                {
                    return (CommodityKind)mServiceMotive;
                }
                set
                {
                    mServiceMotive = (int)value;
                }
            }

            /// <summary>
            /// Includes cost, whether the service is recurring, whether the service is an emergency service, etc.
            /// </summary>
            public Service.ServiceTuning ServiceTuning
            {
                get
                {
                    return new Service.ServiceTuning(MaxNumNPCsInPool, Cost, IsEmergencyService, IsRecurrent, AlwaysTryToSendTheSameSim);
                }
                set
                {
                    AlwaysTryToSendTheSameSim = value.kAlwaysTryToSendSameSim;
                    Cost = value.kCost;
                    IsEmergencyService = value.kIsEmergencyService;
                    IsRecurrent = value.kIsRecurrent;
                    MaxNumNPCsInPool = value.kMaxNumNPCsInPool;
                }
            }
                
            /// <summary>
            /// Note: this can (and should) be omitted for the UI for creating/editing service profiles.
            /// </summary>
            public ServiceType ServiceType
            {
                get
                {
                    return (ServiceType)mServiceType;
                }
                set
                {
                    mServiceType = (ulong)value;
                }
            }
                
            /// <summary>
            /// The list of skills (and their levels) for the service NPC to start out with.
            /// </summary>
            public List<SkillLevelPair> Skills
            {
                get
                {
                    return mSkills;
                }
                set
                {
                    mSkills = value;
                }
            }

            /// <summary>
            /// Length of time (in hours) that the service NPC spends performing their duties before they charge for their service and leave.
            /// Note: this will only be used when <see cref="IsLiveInService"/> is set to <c>false</c>.
            /// </summary>
            public float TimeToSpendWorking
            {
                get
                {
                    return mTimeToSpendWorking;
                }
                set
                {
                    mTimeToSpendWorking = value;
                }
            }
                
            /// <summary>
            /// How old leftovers can be out in minutes before the service NPC will put it away.
            /// Note: this will only be used when <see cref="WaitsBeforePuttingAwayLeftovers"/> is set to <c>true</c>.
            /// </summary>
            public float TimeWaitBeforePutawayLeftovers
            {
                get
                {
                    return mTimeWaitBeforePutawayLeftovers;
                }
                set
                {
                    mTimeWaitBeforePutawayLeftovers = value;
                }
            }

            /// <summary>
            /// The display name of the service.
            /// </summary>
            public string Title
            {
                get
                {
                    string value;
                    return mStrings.TryGetValue("Title", out value) ? value : null;
                }
                set
                {
                    mStrings["Title"] = value;
                }
            }
                
            /// <summary>
            /// The list of explicit traits for the service NPC.
            /// Note: do not add or remove elements from this property directly, as that will not work. Use the <see cref="AddTraits"/> and <see cref="RemoveTraits"/> methods instead.
            /// </summary>
            public List<TraitNames> Traits
            {
                get
                {
                    return mTraits.ConvertAll(x => (TraitNames)x);
                }
                set
                {
                    mTraits = value.ConvertAll(x => (ulong)x);
                }
            }

            /// <summary>
            /// Multiplier for interactions in a room where a Sim is sleeping.
            /// Note: this will only be used when <see cref="IsQuietAroundSleepingSims"/> is set to <c>true</c>.
            /// </summary>
            public float UseObjectInSameRoomAsSleeperMultiplier
            {
                get
                {
                    return mUseObjectInSameRoomAsSleeperMultiplier;
                }
                set
                {
                    mUseObjectInSameRoomAsSleeperMultiplier = value;
                }
            }

            /// <summary>
            /// The allowed range of ages that the service NPC can be.
            /// </summary>
            public CASAgeGenderFlags ValidAges
            {
                get
                {
                    return (CASAgeGenderFlags)mValidAges;
                }
                set
                {
                    mValidAges = (uint)(value & CASAgeGenderFlags.AgeMask);
                }
            }

            /// <summary>
            /// The allowed range of genders that the service NPC can be.
            /// </summary>
            public CASAgeGenderFlags ValidGenders
            {
                get
                {
                    return (CASAgeGenderFlags)mValidGenders;
                }
                set
                {
                    CASAgeGenderFlags gender = value & CASAgeGenderFlags.GenderMask;
                    mValidGenders = gender == CASAgeGenderFlags.GenderMask ? 0u : (uint)gender;
                }
            }

            /// <summary>
            /// The version string used to accommodate for changes to default values between versions of the Expanded Household Staff mod.
            /// </summary>
            public string VersionString
            {
                get
                {
                    string value;
                    return mStrings.TryGetValue("VersionString", out value) ? value : "-1";
                }
                set
                {
                    mStrings["VersionString"] = value;
                }
            }

            /// <summary>
            /// If set to <c>true</c>, the service NPC waits a bit after a meal is prepared before putting it away.
            /// </summary>
            public bool WaitsBeforePuttingAwayLeftovers
            {
                get
                {
                    return (mFlags & (ulong)ServiceProfileFlags.WaitsBeforePuttingAwayLeftovers) != 0uL;
                }
                set
                {
                    if (value)
                    {
                        mFlags |= (ulong)ServiceProfileFlags.WaitsBeforePuttingAwayLeftovers;
                    }
                    else
                    {
                        mFlags &= ulong.MaxValue ^ (ulong)ServiceProfileFlags.WaitsBeforePuttingAwayLeftovers;
                    }
                }
            }

            public ServiceProfile()
            {
            }

            public ServiceProfile(string name, string title) : this(name, title, additionalMotives: null)
            {
            }

            public ServiceProfile(string name, string title, List<CommodityKind> additionalMotives = null) : this(name, title, serviceMotive: null, additionalMotives: additionalMotives)
            {
            }

            public ServiceProfile(string name, string title, string requestedMessage = null, string cancelledMessage = null, string cancelledWhileActiveMessage = null, List<CommodityKind> additionalMotives = null, List<CommodityChange> outputs = null, List<TraitNames> traits = null, List<TraitNames> hiddenTraits = null, List<TraitNames> potentialTraits = null, int potentialTraitCount = 0, List<SkillLevelPair> skills = null) : this(name, title, requestedMessage, cancelledMessage, cancelledWhileActiveMessage, null, additionalMotives, outputs, traits, hiddenTraits, potentialTraits, potentialTraitCount, skills)
            {
            }

            public ServiceProfile(string name, string title, string requestedMessage = null, string cancelledMessage = null, string cancelledWhileActiveMessage = null, CommodityKind? serviceMotive = null, List<CommodityKind> additionalMotives = null, List<CommodityChange> outputs = null, List<TraitNames> traits = null, List<TraitNames> hiddenTraits = null, List<TraitNames> potentialTraits = null, int potentialTraitCount = 0, List<SkillLevelPair> skills = null)
            {
                VersionString = CurrentVersion.ToString();
                Name = name;
                Title = title;
                string entryKey = typeof(CustomService).GetLocalizationKey();
                RequestedMessage = requestedMessage ?? Localization.LocalizeString(entryKey + ":ServiceRequested", title);
                CancelledMessage = cancelledMessage ?? Localization.LocalizeString(entryKey + ":ServiceCancelled", title);
                CancelledWhileActiveMessage = cancelledWhileActiveMessage ?? Localization.LocalizeString(entryKey + ":ServiceCancelledWhileActive", title);
                ServiceMotive = serviceMotive ?? CommonUtils.GetCommodityKind("Be" + name, CommodityKindType.Motive);
                Motives = additionalMotives ?? new List<CommodityKind>();
                Outputs = outputs ?? new List<CommodityChange>();
                if (!mMotives.Contains(mServiceMotive))
                {
                    mMotives.Add(mServiceMotive);
                }
                Traits = traits ?? new List<TraitNames>();
                HiddenTraits = hiddenTraits ?? new List<TraitNames>();
                PotentialTraits = potentialTraits ?? new List<TraitNames>();
                PotentialTraitCount = potentialTraitCount;
                Skills = skills ?? new List<SkillLevelPair>();
            }

            public void AddActions(params ActiveTopicAction[] actions)
            {
                mActions.AddRange(actions);
            }

            public void AddHiddenTraits(params TraitNames[] traits)
            {
                foreach (TraitNames trait in traits)
                {
                    mHiddenTraits.Add((ulong)trait);
                }
            }

            public void AddMotives(params CommodityKind[] motives)
            {
                foreach (CommodityKind motive in motives)
                {
                    mMotives.Add((int)motive);
                }
            }

            public void AddOutputs(params CommodityChange[] outputs)
            {
                mOutputs.AddRange(outputs);
            }

            public void AddPotentialTraits(params TraitNames[] traits)
            {
                foreach (TraitNames trait in traits)
                {
                    mPotentialTraits.Add((ulong)trait);
                }
            }

            public void AddSkills(params SkillLevelPair[] skills)
            {
                mSkills.AddRange(skills);
            }

            public void AddTraits(params TraitNames[] traits)
            {
                foreach (TraitNames trait in traits)
                {
                    mTraits.Add((ulong)trait);
                }
            }

            public ServiceProfileFlags GetFlags()
            {
                return (ServiceProfileFlags)mFlags;
            }

            public bool HasFlags(ServiceProfileFlags flags)
            {
                return ((ServiceProfileFlags)mFlags & flags) == flags;
            }

            public void RemoveActions(params ActiveTopicAction[] actions)
            {
                foreach (ActiveTopicAction action in actions)
                {
                    mActions.Remove(action);
                }
            }

            public void RemoveActions(Predicate<ActiveTopicAction> predicate = null)
            {
                mActions.RemoveAll(predicate ?? (x => true));
            }

            public void RemoveHiddenTraits(Predicate<TraitNames> predicate = null)
            {
                mHiddenTraits.RemoveAll(x => predicate == null || predicate((TraitNames)x));
            }

            public void RemoveHiddenTraits(params TraitNames[] traits)
            {
                foreach (TraitNames trait in traits)
                {
                    mHiddenTraits.Remove((ulong)trait);
                }
            }

            public void RemoveMotives(params CommodityKind[] motives)
            {
                foreach (CommodityKind motive in motives)
                {
                    mMotives.Remove((int)motive);
                }
            }

            public void RemoveMotives(Predicate<CommodityKind> predicate = null)
            {
                mMotives.RemoveAll(x => predicate == null || predicate((CommodityKind)x));
            }

            public void RemoveOutputs(params CommodityChange[] outputs)
            {
                foreach (CommodityChange output in outputs)
                {
                    mOutputs.Remove(output);
                }
            }

            public void RemoveOutputs(Predicate<CommodityChange> predicate = null)
            {
                mOutputs.RemoveAll(predicate ?? (x => true));
            }

            public void RemovePotentialTraits(Predicate<TraitNames> predicate = null)
            {
                mPotentialTraits.RemoveAll(x => predicate == null || predicate((TraitNames)x));
            }

            public void RemovePotentialTraits(params TraitNames[] traits)
            {
                foreach (TraitNames trait in traits)
                {
                    mPotentialTraits.Remove((ulong)trait);
                }
            }

            public void RemoveSkills(Predicate<SkillLevelPair> predicate = null)
            {
                mSkills.RemoveAll(predicate ?? (x => true));
            }

            public void RemoveSkills(params SkillLevelPair[] skills)
            {
                foreach (SkillLevelPair skill in skills)
                {
                    Skills.Remove(skill);
                }
            }

            public void RemoveTraits(Predicate<TraitNames> predicate = null)
            {
                mTraits.RemoveAll(x => predicate == null || predicate((TraitNames)x));
            }

            public void RemoveTraits(params TraitNames[] traits)
            {
                foreach (TraitNames trait in traits)
                {
                    mTraits.Remove((ulong)trait);
                }
            }

            public void SetFlags(ServiceProfileFlags flags)
            {
                mFlags = (ulong)flags;
            }
        }

        public const int CurrentVersion = 0;
            
        [PersistableStatic(true)]
        public static Dictionary<string, CustomService> CustomInstances = new Dictionary<string, CustomService>();

        public static List<Type> LoadedTypes = new List<Type>();

        public static Dictionary<string, Service> PredefinedInstances = new Dictionary<string, Service>();

        public static Dictionary<Type, CommodityKind> ServiceMotives = new Dictionary<Type, CommodityKind>();

        [PersistableStatic(true)]
        public static List<IServiceProfile> ServiceProfiles = new List<IServiceProfile>();

        public static void AddServiceProfileInteractions(this GameObject gameObject)
        {
            if (gameObject != null)
            {
                gameObject.AddInteraction(CreateServiceProfile.Singleton, true);
                gameObject.AddInteraction(CloneServiceProfile.Singleton, true);
                gameObject.AddInteraction(DeleteServiceProfile.Singleton, true);
                gameObject.AddInteraction(EditServiceProfile.Singleton, true);
                gameObject.AddInteraction(EditServiceUniform.Singleton, true);
                gameObject.AddInteraction(ResetServiceUniform.Singleton, true);
                gameObject.AddInteraction(ResetServiceCar.Singleton, true);
                gameObject.AddInteraction(AddActiveTopicAction.Singleton, true);
                gameObject.AddInteraction(RemoveActiveTopicAction.Singleton, true);
                gameObject.AddInteraction(AddAutonomousInteraction.Singleton, true);
                gameObject.AddInteraction(RemoveAutonomousInteraction.Singleton, true);
                gameObject.AddInteraction(AddInventoryObject.Singleton, true);
                gameObject.AddInteraction(RemoveInventoryObject.Singleton, true);
                gameObject.AddInteraction(ImportServiceCollection.Singleton, true);
                gameObject.AddInteraction(ExportServiceCollection.Singleton, true);
                gameObject.AddInteraction(DeleteServiceCollection.Singleton, true);
            }
        }

        /// <summary>
        /// Adds a custom service with the specified profile to the savegame, requestable via the <see cref="Destrospean.ExpandedHouseholdStaff.Interactions.CallForServices"/> interaction.
        /// </summary>
        public static void AddServiceToSaveGame(this IServiceProfile profile)
        {
            if (CanAddServiceToSaveGame(profile))
            {
                ServiceProfiles.Add(profile);
                CustomService.Init(profile);
            }
        }

        /// <summary>
        /// Gets whether a custom service with the specified profile name can be added to the savegame.
        /// </summary>
        public static bool CanAddServiceToSaveGame(string name)
        {
            return !CustomInstances.ContainsKey(name) && !ServiceProfiles.Exists(x => x.Name == name);
        }

        /// <summary>
        /// Gets whether a custom service with the specified profile can be added to the savegame.
        /// </summary>
        public static bool CanAddServiceToSaveGame(this IServiceProfile profile)
        {
            return !CustomInstances.ContainsKey(profile.Name) && !ServiceProfiles.Contains(profile);
        }

        /// <summary>
        /// Gets whether a custom service with the specified profile name can be removed from the savegame.
        /// </summary>
        public static bool CanRemoveServiceFromSaveGame(string name)
        {
            CustomService service;
            return CustomInstances.TryGetValue(name, out service) && ServiceProfiles.Contains(service.Profile);
        }

        /// <summary>
        /// Gets whether a custom service with the specified profile can be removed from the savegame.
        /// </summary>
        public static bool CanRemoveServiceFromSaveGame(this IServiceProfile profile)
        {
            return CanRemoveServiceFromSaveGame(profile.Name);
        }

        public static IServiceProfile ClonePartially(this IServiceProfile profile, string newName)
        {
            return new ServiceProfile(newName, profile.Title)
            {
                Actions = profile.Actions.ConvertAll(x => new ActiveTopicAction(x.Name, x.Grouping, x.IsActive)),
                CancelledMessage = profile.CancelledMessage,
                CancelledWhileActiveMessage = profile.CancelledWhileActiveMessage,
                CarGroupId = profile.CarGroupId,
                CarInstanceId = profile.CarInstanceId,
                CarInstanceName = profile.CarInstanceName,
                CarPreset = profile.CarPreset,
                CheckTime = profile.CheckTime,
                DelayBeforeArriving = profile.DelayBeforeArriving,
                DelayBeforeLeaving = profile.DelayBeforeLeaving,
                DriveTime = profile.DriveTime,
                ExtraWaitTimeAfterSocializing = profile.ExtraWaitTimeAfterSocializing,
                GetUniformFromName = (profile as ServiceProfile)?.GetUniformFromName ?? false,
                GetUniformNameCallback = (profile as ServiceProfile)?.GetUniformNameCallback,
                HiddenTraits = profile.HiddenTraits,
                Inventory = profile.Inventory.ConvertAll(x => new InventoryObjectCreationParameters(x.InstanceName, x.InstanceId, x.GroupId, x.Count, x.Preset)),
                IsBabysittingService = profile.IsBabysittingService,
                IsLiveInService = profile.IsLiveInService,
                IsQuietAroundSleepingSims = profile.IsQuietAroundSleepingSims,
                IsScaredOfBonehilda = profile.IsScaredOfBonehilda,
                Outputs = profile.Outputs.ConvertAll(x => new CommodityChange(x.InteractionDefinitionType, x.TargetType, x.ConstantChange, x.Locked, x.ActualValue, x.UpdateType, x.TimeDependsOnCommodityFilling, x.UpdateEvenOnFailure, x.UpdateAboveAndBelowZero)),
                PotentialTraitCount = profile.PotentialTraitCount,
                PotentialTraits = profile.PotentialTraits,
                PreventSocialization = profile.PreventSocialization,
                RelationshipLevelForQuit = profile.RelationshipLevelForQuit,
                ReportsFires = profile.ReportsFires,
                RequestedMessage = profile.RequestedMessage,
                RequireBeInSameRoom = profile.RequireBeInSameRoom,
                ServiceTuning = profile.ServiceTuning,
                ServiceType = profile.ServiceType,
                Skills = profile.Skills.ConvertAll(x => new SkillLevelPair(x.SkillName, x.SkillLevel)),
                TimeToSpendWorking = profile.TimeToSpendWorking,
                TimeWaitBeforePutawayLeftovers = profile.TimeWaitBeforePutawayLeftovers,
                Traits = profile.Traits,
                UseObjectInSameRoomAsSleeperMultiplier = profile.UseObjectInSameRoomAsSleeperMultiplier,
                ValidAges = profile.ValidAges,
                ValidGenders = profile.ValidGenders,
                VersionString = profile.VersionString,
                WaitsBeforePuttingAwayLeftovers = profile.WaitsBeforePuttingAwayLeftovers
            };
        }

        public static void FixUp(this IServiceProfile profile)
        {
            IServiceProfile dummyProfile = new ServiceProfile("DummyService", "Dummy Service");
            if (int.Parse(profile.VersionString) < 0)
            {
                profile.DelayBeforeArriving = dummyProfile.DelayBeforeArriving;
                profile.DelayBeforeLeaving = dummyProfile.DelayBeforeLeaving;
                profile.ExtraWaitTimeAfterSocializing = dummyProfile.ExtraWaitTimeAfterSocializing;
            }
            profile.VersionString = CurrentVersion.ToString();
        }

        public static bool IsFromExpandedHouseholdStaff<Service>() where Service : Sims3.Gameplay.Services.Service
        {
            return IsFromExpandedHouseholdStaff(typeof(Service));
        }

        public static bool IsFromExpandedHouseholdStaff(this Service service)
        {
            return IsFromExpandedHouseholdStaff(service.GetType());
        }

        public static bool IsFromExpandedHouseholdStaff(Type type)
        {
            return typeof(Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff.IService).IsAssignableFrom(type);
        }

        /// <summary>
        /// Removes a custom service with the specified profile name from the savegame.
        /// </summary>
        public static void RemoveServiceFromSaveGame(string name)
        {
            if (CanRemoveServiceFromSaveGame(name))
            {
                IServiceProfile profile = CustomInstances[name].Profile;
                CustomService.Deinit(profile);
                CustomInstances.Remove(name);
                ServiceProfiles.Remove(profile);
            }
        }

        /// <summary>
        /// Removes a custom service with the specified profile from the savegame.
        /// </summary>
        public static void RemoveServiceFromSaveGame(this IServiceProfile profile)
        {
            RemoveServiceFromSaveGame(profile.Name);
        }

        public static bool ServiceMotiveExists(CommodityKind serviceMotive)
        {
            foreach (CommodityKind value in Enum.GetValues(typeof(CommodityKind)))
            {
                if (serviceMotive == value)
                {
                    return true;
                }
            }
            foreach (IServiceProfile profile in ServiceProfiles)
            {
                if (serviceMotive == profile.ServiceMotive)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool ShowCostDialog(ref string costString, bool isLiveInService)
        {
            string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "CostDialog");
            costString = StringInputDialog.Show(Localization.LocalizeString(entryKey + ":Title"), Localization.LocalizeString(entryKey + "/Prompts:" + (isLiveInService ? "Weekly" : "Daily")), costString, -1, ThumbnailKey.kInvalidThumbnailKey, new Vector2(-1f, -1f), StringInputDialog.Validation.Number, false, ModalDialog.PauseMode.PauseSimulator, false, true);
            return costString != null;
        }

        public static bool ShowServiceProfileFlagListDialog(this IServiceProfile profile, out ServiceProfileFlags flags, ServiceProfileFlags? preSelectedFlags = null)
        {
            flags = 0;
            string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "ServiceProfileFlagListDialog");
            List<ServiceProfileFlags> flagList = new List<ServiceProfileFlags>(Array.FindAll((ServiceProfileFlags[])Enum.GetValues(typeof(ServiceProfileFlags)), x => preSelectedFlags.HasValue ? (preSelectedFlags & x) == x : (profile as ServiceProfile)?.HasFlags(x) ?? false));
            bool cancelled, confirmed;
            while (true)
            {
                List<ServiceProfileFlags> selectedFlags = ObjectPickerDialog.Show(Responder.Instance.LocalizationModel.LocalizeString(entryKey + ":Title"), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), new List<ServiceProfileFlags>((ServiceProfileFlags[])Enum.GetValues(typeof(ServiceProfileFlags))).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<ServiceProfileFlags>>
                    {
                        new ServiceProfileFlagColumn(entryKey),
                        new ServiceProfileFlagEnabledColumn(entryKey, flagList.ToArray())
                    }, 1, out confirmed, out cancelled, true);
                if (cancelled)
                {
                    return false;
                }
                if (confirmed)
                {
                    foreach (ServiceProfileFlags flag in flagList)
                    {
                        flags |= flag;
                    }
                    return true;
                }
                if (flagList.Contains(selectedFlags[0]))
                {
                    flagList.Remove(selectedFlags[0]);
                }
                else
                {
                    flagList.Add(selectedFlags[0]);
                }
            }
        }

        public static bool ShowTimeToSpendWorkingDialog(ref string timeToSpendWorkingString)
        {
            string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "TimeToSpendWorkingDialog");
            timeToSpendWorkingString = StringInputDialog.Show(Localization.LocalizeString(entryKey + ":Title"), Localization.LocalizeString(entryKey + ":Prompt"), timeToSpendWorkingString, -1, ThumbnailKey.kInvalidThumbnailKey, new Vector2(-1f, -1f), StringInputDialog.Validation.FloatNumber, false, ModalDialog.PauseMode.PauseSimulator, false, true);
            return timeToSpendWorkingString != null;
        }

        /// <summary>
        /// Opens a series of dialogs to add an action to a service topic of the specified profile.
        /// </summary>
        /// <returns><c>true</c>, if the an action was added, <c>false</c> otherwise.</returns>
        public static bool TryUIAddAction(this IServiceProfile profile, string title)
        {
            string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "");
            string name = null;
            LongTermRelationshipTypes grouping = 0;
            ActiveTopicActionActiveness activeTopicActionActiveness = 0;
            byte step = 0;
            while (true)
            {
                if (step == 0)
                {
                    List<ActiveTopicAction> allActions = new List<ActiveTopicAction>();
                    foreach (var activeTopic in ActionAvailabilityData.sActiveTopicInteractions)
                    {
                        foreach (KeyValuePair<LongTermRelationshipTypes, Dictionary<bool, List<string>>> groups in activeTopic.Value)
                        {
                            foreach (KeyValuePair<bool, List<string>> group in groups.Value)
                            {
                                foreach (string action in group.Value)
                                {
                                    if (!allActions.Exists(x => x.Name == action))
                                    {
                                        allActions.Add(new ActiveTopicAction(action, groups.Key, group.Key));
                                    }
                                }
                            }
                        }
                    }
                    ActiveTopicAction[] actions;
                    if (!TryUIGetSelectedActions(out actions, allActions.ToArray(), title, 1))
                    {
                        return false;
                    }
                    name = actions[0].Name;
                    grouping = actions[0].Grouping;
                    activeTopicActionActiveness = actions[0].IsActive ? ActiveTopicActionActiveness.FPA : ActiveTopicActionActiveness.SPA;
                    step++;
                }
                if (step == 1)
                {
                    if (!CommonUtils.TryUIGetActiveTopicActionActiveness(Localization.LocalizeString(entryKey + "ActiveTopicActionActivenessDialog:Title"), out activeTopicActionActiveness, activeTopicActionActiveness))
                    {
                        step--;
                        continue;
                    }
                    step++;
                }
                if (step == 2)
                {
                    if (!CommonUtils.TryUIGetLongTermRelationshipType(Localization.LocalizeString(entryKey + "LongTermRelationshipTypeDialog:Title"), profile.ValidGenders, out grouping, grouping))
                    {
                        step--;
                        continue;
                    }
                    step++;
                }
                bool isActive = activeTopicActionActiveness == ActiveTopicActionActiveness.FPA;
                profile.AddActions(new ActiveTopicAction(name, grouping, isActive));
                CommonUtils.AddActions(profile.Name + " Service", grouping, isActive, name);
                return true;
            }
        }

        /// <summary>
        /// Opens a dialog to add an item to the inventory that the service NPC spawns with.
        /// </summary>
        /// <returns><c>true</c>, if an item was added, <c>false</c> otherwise.</returns>
        public static bool TryUIAddInventoryObject(this IServiceProfile profile)
        {
            string[] results = new[]
                {
                    "",
                    "0x00000000",
                    "1"
                };
            string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "AddInventoryObjectDialog");
            results = ThreeStringInputDialog.Show(Localization.LocalizeString(entryKey + ":Title"), new string[]
                {
                    Localization.LocalizeString(entryKey + "/Prompts:SetInstanceName"),
                    Localization.LocalizeString(entryKey + "/Prompts:SetGroup"),
                    Localization.LocalizeString(entryKey + "/Prompts:SetCount")
                }, results, int.MaxValue, new Vector2(-1, -1), ThreeStringInputDialog.Validation.None, ModalDialog.PauseMode.PauseSimulator, false);
            uint groupId;
            int count;
            if (results == null || Array.Exists(results, string.IsNullOrEmpty) || !uint.TryParse(results[1].Replace("0x", ""), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out groupId) || !int.TryParse(results[2], out count))
            {
                return false;
            }
            profile.Inventory.Add(new InventoryObjectCreationParameters(results[0], NameGuidMap.GetGuidByName(results[0]), groupId, count));
            return true;
        }

        /// <summary>
        /// Opens a series of dialogs to add an output to an interaction for a service motive of the specified profile.
        /// </summary>
        /// <returns><c>true</c>, if the an output was added, <c>false</c> otherwise.</returns>
        public static bool TryUIAddOutput(this IServiceProfile profile)
        {
            string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "");
            Type[] interactionDefinitionTypes = null;
            Type[] targetTypes = null;
            string advertisedString = null;
            string actualString = null;
            OutputUpdateType updateType = OutputUpdateType.ImmediateDelta;
            byte step = 0;
            while (true)
            {
                if (step == 0)
                {
                    if (!CommonUtils.TryUIGetSelectedTypes(out interactionDefinitionTypes, new List<InteractionTuning>(InteractionTuning.sAllTunings.Values).ConvertAll(x => InteractionObjectTypeUtils.InteractionDefinitionTypes.ContainsKey(x.FullInteractionName) ? InteractionObjectTypeUtils.InteractionDefinitionTypes[x.FullInteractionName] : null).FindAll(x => x != null).ToArray(), Localization.LocalizeString(entryKey + "NamespaceListDialog/Titles:InteractionDefinition"), Localization.LocalizeString(entryKey + "TypeListDialog/Titles:InteractionDefinition"), 1))
                    {
                        return false;
                    }
                    step++;
                }
                if (step == 1)
                {
                    if (!CommonUtils.TryUIGetSelectedTypes(out targetTypes, new List<InteractionTuning>(InteractionTuning.sAllTunings.Values).FindAll(x => x.FullInteractionName == interactionDefinitionTypes[0].FullName).ConvertAll(x => InteractionObjectTypeUtils.GameObjectTypes[x.FullObjectName]).ToArray(), Localization.LocalizeString(entryKey + "NamespaceListDialog/Titles:Target"), Localization.LocalizeString(entryKey + "TypeListDialog/Titles:Target"), 1))
                    {
                        step--;
                        continue;
                    }
                    step++;
                }
                if (step == 2)
                {
                    advertisedString = StringInputDialog.Show(Localization.LocalizeString(entryKey + "AdvertisedValueDialog:Title"), Localization.LocalizeString(entryKey + "AdvertisedValueDialog:Prompt"), "200", -1, ThumbnailKey.kInvalidThumbnailKey, new Vector2(-1f, -1f), StringInputDialog.Validation.FloatNumber, false, ModalDialog.PauseMode.PauseSimulator, false, true);
                    if (advertisedString == null)
                    {
                        step--;
                        continue;
                    }
                    step += (byte)(Settings.kShowAdvancedOutputDialogs ? 1 : 3);
                }
                if (step == 3)
                {
                    actualString = StringInputDialog.Show(Localization.LocalizeString(entryKey + "ActualValueDialog:Title"), Localization.LocalizeString(entryKey + "ActualValueDialog:Prompt"), "200", -1, ThumbnailKey.kInvalidThumbnailKey, new Vector2(-1f, -1f), StringInputDialog.Validation.FloatNumber, false, ModalDialog.PauseMode.PauseSimulator, false, true);
                    if (actualString == null)
                    {
                        step--;
                        continue;
                    }
                    step++;
                }
                if (step == 4)
                {
                    if (!CommonUtils.TryUIGetUpdateType(Localization.LocalizeString(entryKey + "UpdateTypeDialog:Title"), out updateType))
                    {
                        step--;
                        continue;
                    }
                    step++;
                }
                CustomService service;
                bool serviceInSaveGame = CustomInstances.TryGetValue(profile.Name, out service);
                if (serviceInSaveGame)
                {
                    service.RemoveOutputs();
                }
                float advertised = ParserFunctions.ParseFloat(advertisedString, 200f);
                float actual = ParserFunctions.ParseFloat(actualString, advertised);
                bool locked = actual == advertised;
                profile.RemoveOutputs(x => x.InteractionDefinitionType == interactionDefinitionTypes[0].FullName && x.TargetType == targetTypes[0].FullName && x.ConstantChange == advertised && x.Locked == locked && x.ActualValue == actual && x.UpdateType == updateType);
                profile.AddOutputs(new CommodityChange(interactionDefinitionTypes[0], targetTypes[0], advertised, locked, actual, updateType));
                if (serviceInSaveGame)
                {
                    service.AddOutputs();
                }
                return true;
            }
        }

        /// <summary>
        /// Tries to clone a service profile.
        /// </summary>
        /// <returns><c>true</c>, if a service profile was cloned, <c>false</c> otherwise.</returns>
        public static bool TryUICloneServiceProfile(this IServiceProfile profile, string newName, out IServiceProfile newProfile)
        {
            newProfile = null;
            string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "CreateServiceProfileDialog");
            if (string.IsNullOrEmpty(newName))
            {
                SimpleMessageDialog.Show(Localization.LocalizeString(entryKey + ":ServiceCreationFailed"), Localization.LocalizeString(entryKey + ":NameEmpty"));
                return false;
            }
            if (!CanAddServiceToSaveGame(newName))
            {
                SimpleMessageDialog.Show(Localization.LocalizeString(entryKey + ":ServiceCreationFailed"), Localization.LocalizeString(entryKey + ":NotUnique"));
                return false;
            }
            newProfile = profile.ClonePartially(newName);
            string serviceMotiveName = "Be" + newProfile.Name;
            while (ServiceMotiveExists(newProfile.ServiceMotive))
            {
                newProfile.ServiceMotive = CommonUtils.GetCommodityKind(serviceMotiveName, CommodityKindType.Motive);
            }
            CommonUtils.AddEnumValue<CommodityKind>(serviceMotiveName, newProfile.ServiceMotive);
            newProfile.Motives = new List<CommodityKind>
                {
                    newProfile.ServiceMotive
                };
            newProfile.AddMotives(profile.Motives.FindAll(x => x != profile.ServiceMotive).ToArray());
            return true;
        }

        /// <summary>
        /// Opens a dialog to create a service profile.
        /// </summary>
        /// <returns><c>true</c>, if a service profile was created, <c>false</c> otherwise.</returns>
        public static bool TryUICreateServiceProfile(out IServiceProfile profile)
        {
            string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "CreateServiceProfileDialog");
            profile = null;
            string serviceMotiveName = null;
            string name = "";
            while (true)
            {
                if (!string.IsNullOrEmpty(serviceMotiveName))
                {
                    CommonUtils.RemoveEnumValue<CommodityKind>(serviceMotiveName);
                }
                name = StringInputDialog.Show(Localization.LocalizeString(entryKey + ":Title"), Localization.LocalizeString(entryKey + "/Prompts:Name"), name, -1, ThumbnailKey.kInvalidThumbnailKey, new Vector2(-1, -1), StringInputDialog.Validation.None, false, ModalDialog.PauseMode.PauseSimulator, false, true);
                if (name == null)
                {
                    return false;
                }
                if (string.IsNullOrEmpty(name))
                {
                    SimpleMessageDialog.Show(Localization.LocalizeString(entryKey + ":ServiceCreationFailed"), Localization.LocalizeString(entryKey + ":NameEmpty"));
                    return false;
                }
                if (!CanAddServiceToSaveGame(name))
                {
                    SimpleMessageDialog.Show(Localization.LocalizeString(entryKey + ":ServiceCreationFailed"), Localization.LocalizeString(entryKey + ":NotUnique"));
                    return false;
                }
                profile = profile?.ClonePartially(name) ?? new ServiceProfile(name, null);
                serviceMotiveName = "Be" + profile.Name;
                while (ServiceMotiveExists(profile.ServiceMotive))
                {
                    profile.ServiceMotive = CommonUtils.GetCommodityKind(serviceMotiveName, CommodityKindType.Motive);
                }
                CommonUtils.AddEnumValue<CommodityKind>(serviceMotiveName, profile.ServiceMotive);
                profile.Motives = new List<CommodityKind>
                    {
                        profile.ServiceMotive
                    };
                profile.Actions = new List<ActiveTopicAction>
                    {
                        new ActiveTopicAction("Dismiss"),
                        new ActiveTopicAction("Fire")
                    };
                profile.Outputs = new List<CommodityChange>
                    {
                        new CommodityChange(typeof(SitAndWait.Definition), typeof(GameObject), 1f, true, 1f, OutputUpdateType.ImmediateDelta)
                    };
                if (!TryUIEditServiceProfile(profile, true))
                {
                    continue;
                }
                return true;
            }
        }

        /// <summary>
        /// Opens a series of dialogs to make changes to a service profile.
        /// </summary>
        /// <returns><c>true</c>, if the changes to the service profile were confirmed, <c>false</c> otherwise.</returns>
        public static bool TryUIEditServiceProfile(this IServiceProfile profile, bool saveWhenCancelled = false)
        {
            string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "");

            CASAgeGenderFlags age = profile.ValidAges;
            string costString = profile.ServiceTuning.kCost.ToString();
            string[] delaysAsStrings = new[]
                {
                    profile.DelayBeforeArriving.ToString(),
                    profile.DelayBeforeLeaving.ToString(),
                    profile.ExtraWaitTimeAfterSocializing.ToString()
                };
            CASAgeGenderFlags gender = profile.ValidGenders;
            List<Trait> hiddenTraits = profile.HiddenTraits.ConvertAll(TraitManager.GetTraitFromDictionary);
            string[] messages = new[]
                {
                    profile.RequestedMessage,
                    profile.CancelledMessage,
                    profile.CancelledWhileActiveMessage
                };
            CommodityKind[] motives = profile.Motives.ToArray();
            string potentialTraitCountString = profile.PotentialTraitCount.ToString();
            List<Trait> potentialTraits = profile.PotentialTraits.ConvertAll(TraitManager.GetTraitFromDictionary);
            ServiceProfileFlags serviceProfileFlags = profile.GetFlags();
            List<SkillLevelPair> skills = profile.Skills.ConvertAll(x => new SkillLevelPair(x.SkillName, x.SkillLevel));
            string timeToSpendWorkingString = profile.TimeToSpendWorking.ToString();
            string title = profile.Title;
            List<Trait> traits = profile.Traits.ConvertAll(TraitManager.GetTraitFromDictionary);

            sbyte step = 0;
            while (true)
            {
                if (step == -1 && !saveWhenCancelled)
                {
                    return false;
                }

                // The following code sets the display name of the service.
                if (step == 0)
                {
                    string tempTitle = StringInputDialog.Show(Localization.LocalizeString(entryKey + "EditServiceProfileDialog:Title"), Localization.LocalizeString(entryKey + "EditServiceProfileDialog:Prompt"), title, -1, ThumbnailKey.kInvalidThumbnailKey, new Vector2(-1, -1), StringInputDialog.Validation.None, false, ModalDialog.PauseMode.PauseSimulator, false, true);
                    if (tempTitle == "")
                    {
                        SimpleMessageDialog.Show(Localization.LocalizeString(entryKey + "EditServiceProfileDialog:TitleChangeFailed"), Localization.LocalizeString(entryKey + "CreateServiceProfileDialog:TitleEmpty"));
                    }
                    if (string.IsNullOrEmpty(tempTitle))
                    {
                        step--;
                        continue;
                    }
                    step++;
                    if (title == null)
                    {
                        string customServiceEntryKey = typeof(CustomService).GetLocalizationKey();
                        messages[0] = Localization.LocalizeString(customServiceEntryKey + ":ServiceRequested", tempTitle);
                        messages[1] = Localization.LocalizeString(customServiceEntryKey + ":ServiceCancelled", tempTitle);
                        messages[2] = Localization.LocalizeString(customServiceEntryKey + ":ServiceCancelledWhileActive", tempTitle);
                    }
                    title = tempTitle;
                }

                // The following code sets phone call feedback messages when requesting and cancelling services.
                if (step == 1)
                {
                    string[] tempMessages = (string[])messages.Clone();
                    if (!TryUISetPhoneCallFeedback(ref tempMessages))
                    {
                        step--;
                        continue;
                    }
                    step++;
                    messages = tempMessages;
                }

                // The following code sets the delays of the arrival and departure of the service NPC.
                if (step == 2)
                {
                    string[] tempDelaysAsStrings = (string[])delaysAsStrings.Clone();
                    if (!TryUISetDelays(ref tempDelaysAsStrings))
                    {
                        step--;
                        continue;
                    }
                    step++;
                    delaysAsStrings = tempDelaysAsStrings;
                }

                // The following code sets the valid range of ages the service NPC can be.
                if (step == 3)
                {
                    CASAgeGenderFlags tempAge;
                    if (!CommonUtils.ShowCASAgeGenderFlagListDialog(out tempAge, age, CASAgeGenderFlags.AgeMask ^ CASAgeGenderFlags.Baby ^ CASAgeGenderFlags.Toddler, Localization.LocalizeString(entryKey + "CASAgeGenderFlagListDialog/Titles:Age")))
                    {
                        step--;
                        continue;
                    }
                    step++;
                    age = tempAge;
                }

                // The following code sets the valid range of genders the service NPC can be.
                if (step == 4)
                {
                    CASAgeGenderFlags tempGender;
                    if (!CommonUtils.ShowCASAgeGenderFlagListDialog(out tempGender, gender, CASAgeGenderFlags.GenderMask, Localization.LocalizeString(entryKey + "CASAgeGenderFlagListDialog/Titles:Gender")))
                    {
                        step--;
                        continue;
                    }
                    step++;
                    gender = tempGender;
                }

                // The following code sets the service profile flags.
                if (step == 5)
                {
                    ServiceProfileFlags tempServiceProfileFlags;
                    if (!profile.ShowServiceProfileFlagListDialog(out tempServiceProfileFlags))
                    {
                        step--;
                        continue;
                    }
                    step++;
                    serviceProfileFlags = tempServiceProfileFlags;
                }

                bool isLiveInService = (serviceProfileFlags & ServiceProfileFlags.LiveInService) == ServiceProfileFlags.LiveInService;

                // The following code sets the time the service NPC spends working.
                if (step == 6)
                {
                    string tempTimeToSpendWorkingString = timeToSpendWorkingString;
                    if (!isLiveInService && !ShowTimeToSpendWorkingDialog(ref tempTimeToSpendWorkingString))
                    {
                        step--;
                        continue;
                    }
                    step++;
                    timeToSpendWorkingString = tempTimeToSpendWorkingString;
                }

                // The following code sets the cost of the service.
                if (step == 7)
                {
                    string tempCostString = costString;
                    if (!ShowCostDialog(ref tempCostString, isLiveInService))
                    {
                        step--;
                        if (isLiveInService)
                        {
                            step--;
                        }
                        continue;
                    }
                    step++;
                    costString = tempCostString;
                }

                // The following code sets the motives the service NPC will always have.
                if (step == 8)
                {
                    CommodityKind[] tempMotives;
                    if (!CommonUtils.ShowCommodityKindListDialog(out tempMotives, new List<object>(ParserFunctions.sCaseSensitiveEnumParsers[typeof(CommodityKind)].mLookup.Values).FindAll(x => CommodityTest.IsMotive((CommodityKind)x)).ConvertAll(x => (CommodityKind)x).ToArray(), motives, null, entryKey + "MotiveListDialog"))
                    {
                        step--;
                        continue;
                    }
                    step++;
                    motives = tempMotives;
                }

                // The following code sets the traits the service NPC will always come with.
                if (step == 9)
                {
                    List<Trait> tempTraits = new List<Trait>(traits);
                    if (!CommonUtils.ShowTraitListDialog(age, gender, CASAgeGenderFlags.Human, tempTraits, null, Localization.LocalizeString(entryKey + "TraitListDialog/Titles:Explicit")))
                    {
                        step--;
                        continue;
                    }
                    step++;
                    traits = tempTraits;
                }

                // The following code sets the traits the service NPC will randomly pick from.
                if (step == 10)
                {
                    List<Trait> tempTraits = new List<Trait>(potentialTraits);
                    if (!CommonUtils.ShowTraitListDialog(age, gender, CASAgeGenderFlags.Human, tempTraits, null, Localization.LocalizeString(entryKey + "TraitListDialog/Titles:Potential")))
                    {
                        step--;
                        continue;
                    }
                    step++;
                    potentialTraits = tempTraits;
                }

                // The following code sets how many of the potential traits the service NPC will randomly pick.
                if (step == 11)
                {
                    if (potentialTraits.Count > 0)
                    {
                        string tempPotentialTraitCountString = StringInputDialog.Show(Localization.LocalizeString(entryKey + "PotentialTraitCountDialog:Title"), Localization.LocalizeString(entryKey + "PotentialTraitCountDialog:Prompt"), potentialTraitCountString, -1, ThumbnailKey.kInvalidThumbnailKey, new Vector2(-1f, -1f), StringInputDialog.Validation.Number, false, ModalDialog.PauseMode.PauseSimulator, false, true);
                        if (tempPotentialTraitCountString == null)
                        {
                            step--;
                            continue;
                        }
                        potentialTraitCountString = tempPotentialTraitCountString;
                    }
                    step++;
                }

                // The following code sets the hidden traits the service NPC will come with.
                if (step == 12)
                {
                    List<Trait> tempTraits = new List<Trait>(hiddenTraits);
                    if (!CommonUtils.ShowTraitListDialog(age, gender, CASAgeGenderFlags.Human, tempTraits, new List<Trait>(TraitManager.GetDictionaryTraits).FindAll(x => x.IsHidden || x.IsReward), Localization.LocalizeString(entryKey + "TraitListDialog/Titles:Hidden")))
                    {
                        step--;
                        if (potentialTraits.Count == 0)
                        {
                            step--;
                        }
                        continue;
                    }
                    step++;
                    hiddenTraits = tempTraits;
                }

                // The following code sets the skills the service NPC has.
                if (step == 13)
                {
                    List<SkillLevelPair> tempSkills = skills.ConvertAll(x => new SkillLevelPair(x.SkillName, x.SkillLevel));
                    if (!CommonUtils.ShowSkillListDialog(age, CASAgeGenderFlags.Human, tempSkills))
                    {
                        step--;
                        continue;
                    }
                    step++;
                    skills = tempSkills;
                }

                profile.Title = title;
                profile.RequestedMessage = string.IsNullOrEmpty(messages[0]) ? profile.RequestedMessage : messages[0];
                profile.CancelledMessage = string.IsNullOrEmpty(messages[1]) ? profile.RequestedMessage : messages[1];
                profile.CancelledWhileActiveMessage = string.IsNullOrEmpty(messages[2]) ? profile.RequestedMessage : messages[2];
                profile.DelayBeforeArriving = ParserFunctions.ParseFloat(delaysAsStrings[0], profile.DelayBeforeArriving);
                profile.DelayBeforeLeaving = ParserFunctions.ParseFloat(delaysAsStrings[1], profile.DelayBeforeLeaving);
                profile.ExtraWaitTimeAfterSocializing = ParserFunctions.ParseFloat(delaysAsStrings[2], profile.ExtraWaitTimeAfterSocializing);
                profile.ValidAges = age;
                profile.ValidGenders = gender;
                profile.SetFlags(serviceProfileFlags);
                profile.TimeToSpendWorking = ParserFunctions.ParseFloat(timeToSpendWorkingString, profile.TimeToSpendWorking);
                profile.ServiceTuning = new Service.ServiceTuning(profile.ServiceTuning.kMaxNumNPCsInPool, int.Parse(costString), profile.ServiceTuning.kIsEmergencyService, profile.ServiceTuning.kIsRecurrent, profile.ServiceTuning.kAlwaysTryToSendSameSim);
                profile.Motives = new List<CommodityKind>(motives);
                profile.Traits = traits.ConvertAll(x => (TraitNames)x.TraitGuid);
                profile.PotentialTraits = potentialTraits.ConvertAll(x => (TraitNames)x.TraitGuid);
                profile.PotentialTraitCount = int.Parse(potentialTraitCountString);
                profile.HiddenTraits = hiddenTraits.ConvertAll(x => (TraitNames)x.TraitGuid);
                profile.Skills = skills.ConvertAll(x => new SkillLevelPair(x.SkillName, x.SkillLevel));

                return step > -1;
            }
        }

        public static bool TryUIGetSelectedActions(out ActiveTopicAction[] selectedActions, ActiveTopicAction[] allActions, string title = null, int selectableRowCount = int.MaxValue)
        {
            string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "ActiveTopicActionListDialog");
            bool cancelled, confirmed;
            while (true)
            {
                selectedActions = (ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + "/Titles:" + (selectableRowCount == 1 ? "Singular" : "Plural")), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), new List<ActiveTopicAction>(allActions).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<ActiveTopicAction>>
                    {
                        new ActiveTopicActionColumn(entryKey),
                        new ActiveTopicActionGroupingColumn(entryKey),
                        new ActiveTopicActionActivenessColumn(entryKey)
                    }, selectableRowCount, out confirmed, out cancelled) ?? new List<ActiveTopicAction>()).ToArray();
                if (cancelled)
                {
                    selectedActions = null;
                    return false;
                }
                if (confirmed)
                {
                    return true;
                }
            }
        }

        public static bool TryUIGetSelectedInventoryObjects(out InventoryObjectCreationParameters[] selectedInventoryObjects, InventoryObjectCreationParameters[] allInventoryObjects, string title = null, int selectableRowCount = int.MaxValue)
        {
            string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "InventoryObjectListDialog");
            bool cancelled, confirmed;
            while (true)
            {
                selectedInventoryObjects = (ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + "/Titles:" + (selectableRowCount == 1 ? "Singular" : "Plural")), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), new List<InventoryObjectCreationParameters>(allInventoryObjects).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<InventoryObjectCreationParameters>>
                    {
                        new InventoryObjectInstanceNameColumn(entryKey),
                        new InventoryObjectGroupColumn(entryKey),
                        new InventoryObjectCountColumn(entryKey)
                    }, selectableRowCount, out confirmed, out cancelled) ?? new List<InventoryObjectCreationParameters>()).ToArray();
                if (cancelled)
                {
                    selectedInventoryObjects = null;
                    return false;
                }
                if (confirmed)
                {
                    return true;
                }
            }
        }

        public static bool TryUIGetSelectedOutputs(out CommodityChange[] selectedOutputs, CommodityChange[] allOutputs, string title = null, int selectableRowCount = int.MaxValue)
        {
            string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "OutputListDialog");
            bool cancelled, confirmed;
            while (true)
            {
                selectedOutputs = (ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + "/Titles:" + (selectableRowCount == 1 ? "Singular" : "Plural")), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), new List<CommodityChange>(allOutputs).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<CommodityChange>>
                    {
                        new CommodityChangeInteractionDefinitionTypeColumn(entryKey),
                        new CommodityChangeTargetTypeColumn(entryKey)
                    }, selectableRowCount, out confirmed, out cancelled) ?? new List<CommodityChange>()).ToArray();
                if (cancelled)
                {
                    selectedOutputs = null;
                    return false;
                }
                if (confirmed)
                {
                    return true;
                }
            }
        }

        public static bool TryUIGetSelectedServiceProfiles(out IServiceProfile[] selectedProfiles, IServiceProfile[] allProfiles, string title = null, int selectableRowCount = int.MaxValue)
        {
            string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "ServiceProfileListDialog");
            bool cancelled, confirmed;
            while (true)
            {
                selectedProfiles = (ObjectPickerDialog.Show(title ?? Responder.Instance.LocalizationModel.LocalizeString(entryKey + "/Titles:" + (selectableRowCount == 1 ? "Singular" : "Plural")), new List<ObjectPicker.TabInfo>
                    {
                        new ObjectPicker.TabInfo("shop_all_r2", Responder.Instance.LocalizationModel.LocalizeString("Ui/Caption/ObjectPicker:All"), new List<IServiceProfile>(allProfiles).ConvertAll(x => new ObjectPicker.RowInfo(x, new List<ObjectPicker.ColumnInfo>())))
                    }, new List<ObjectPickerDialog.CommonHeaderInfo<IServiceProfile>>
                    {
                        new ServiceProfileNameColumn(entryKey),
                        new ServiceProfileTitleColumn(entryKey)
                    }, selectableRowCount, out confirmed, out cancelled) ?? new List<IServiceProfile>()).ToArray();
                if (cancelled)
                {
                    selectedProfiles = null;
                    return false;
                }
                if (confirmed)
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Opens a series of dialogs to remove an action from a service topic of the specified profile.
        /// </summary>
        /// <returns><c>true</c>, if the an action was removed, <c>false</c> otherwise.</returns>
        public static bool TryUIRemoveAction(this IServiceProfile profile, string title)
        {
            ActiveTopicAction[] actions;
            if (TryUIGetSelectedActions(out actions, profile.Actions.ToArray(), title))
            {
                foreach (ActiveTopicAction action in actions)
                {
                    profile.RemoveActions(x => x.Name == action.Name && x.Grouping == action.Grouping && x.IsActive == action.IsActive);
                    CommonUtils.RemoveActions(profile.Name + " Service", action.Grouping, action.IsActive, action.Name);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Opens a series of dialogs to remove an output from an interaction for a service motive of the specified profile.
        /// </summary>
        /// <returns><c>true</c>, if the an output was removed, <c>false</c> otherwise.</returns>
        public static bool TryUIRemoveOutput(this IServiceProfile profile, string title)
        {
            CommodityChange[] outputs;
            if (TryUIGetSelectedOutputs(out outputs, profile.Outputs.ToArray(), title))
            {
                CustomService service;
                bool serviceInSaveGame = CustomInstances.TryGetValue(profile.Name, out service);
                if (serviceInSaveGame)
                {
                    service.RemoveOutputs();
                }
                profile.RemoveOutputs(outputs);
                if (serviceInSaveGame)
                {
                    service.AddOutputs();
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Opens a dialog to remove custom services from the savegame.
        /// </summary>
        /// <returns><c>true</c>, if any custom services were removed from the savegame, <c>false</c> otherwise.</returns>
        public static bool TryUIRemoveServicesFromSaveGame(string title)
        {
            IServiceProfile[] selectedProfiles;
            if (TryUIGetSelectedServiceProfiles(out selectedProfiles, ServiceProfiles.FindAll(x => !x.IsImmutable).ToArray(), title))
            {
                foreach (IServiceProfile profile in new List<IServiceProfile>(selectedProfiles))
                {
                    profile.RemoveServiceFromSaveGame();
                    foreach (OutfitAssignmentUtils.OutfitAssignment outfitAssignment in new List<OutfitAssignmentUtils.OutfitAssignment>(OutfitAssignmentUtils.OutfitAssignments))
                    {
                        if (outfitAssignment.ServiceName == profile.Name)
                        {
                            OutfitAssignmentUtils.OutfitAssignments.Remove(outfitAssignment);
                            OutfitAssignmentUtils.AssignedOutfits.Remove(outfitAssignment.SpecialOutfitKey);
                        }
                    }
                    OutfitAssignmentUtils.IndexOutfitAssignments();
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Opens a dialog to set the delays for the arrival and departure of the service NPC and the extra wait time after they socialize before they leave.
        /// </summary>
        /// <returns><c>true</c>, if the delays were set, <c>false</c> otherwise.</returns>
        public static bool TryUISetDelays(ref string[] delaysAsStrings)
        {
            string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "DelaysDialog");
            delaysAsStrings = ThreeStringInputDialog.Show(Localization.LocalizeString(entryKey + ":Title"), new string[]
                {
                    Localization.LocalizeString(entryKey + "/Prompts:SetDelayBeforeArriving"),
                    Localization.LocalizeString(entryKey + "/Prompts:SetDelayBeforeLeaving"),
                    Localization.LocalizeString(entryKey + "/Prompts:SetExtraWaitTimeAfterSocializing")
                }, delaysAsStrings, int.MaxValue, new Vector2(-1, -1), ThreeStringInputDialog.Validation.None, ModalDialog.PauseMode.PauseSimulator, false);
            return delaysAsStrings != null && !Array.Exists(delaysAsStrings, string.IsNullOrEmpty);
        }

        /// <summary>
        /// Opens a dialog to set feedback messages for Sims requesting and cancelling services.
        /// </summary>
        /// <returns><c>true</c>, if feedback messages were set, <c>false</c> otherwise.</returns>
        public static bool TryUISetPhoneCallFeedback(ref string[] messages)
        {
            string entryKey = typeof(ObjectPickerDialog).GetLocalizationKey().Replace("ObjectPickerDialog", "PhoneCallFeedbackDialog");
            messages = ThreeStringInputDialog.Show(Localization.LocalizeString(entryKey + ":Title"), new string[]
                {
                    Localization.LocalizeString(entryKey + "/Prompts:SetRequestedMessage"),
                    Localization.LocalizeString(entryKey + "/Prompts:SetCancelledMessage"),
                    Localization.LocalizeString(entryKey + "/Prompts:SetCancelledWhileActiveMessage")
                }, messages, int.MaxValue, new Vector2(-1, -1), ThreeStringInputDialog.Validation.None, ModalDialog.PauseMode.PauseSimulator, false);
            return messages != null && !Array.Exists(messages, string.IsNullOrEmpty);
        }
    }
}
