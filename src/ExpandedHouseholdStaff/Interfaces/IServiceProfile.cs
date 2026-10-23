using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Services;
using Sims3.SimIFace;
using Sims3.SimIFace.CAS;
using System;
using System.Collections.Generic;
using Destrospean.Misc;
using Destrospean.Utils.ExpandedHouseholdStaff;
using Destrospean.Enums.ExpandedHouseholdStaff;

namespace Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff
{
    [Persistable]
    public interface IServiceProfile 
    {
        /// <summary>
        /// The list of actions for the service topic.
        /// </summary>
        List<ServiceUtils.ActiveTopicAction> Actions
        {
            get;
            set;
        }

        /// <summary>
        /// The message that shows when the service is cancelled.
        /// </summary>
        string CancelledMessage
        {
            get;
            set;
        }

        /// <summary>
        /// The message that shows when the service is cancelled and there is already a service NPC of that service on the lot.
        /// Note: this will only be used when <see cref="IsLiveInService"/> is set to <c>false</c>.
        /// <see cref="CancelledMessage"/> will be used instead if <see cref="IsLiveInService"/> is set to <c>true</c>.
        /// </summary>
        string CancelledWhileActiveMessage
        {
            get;
            set;
        }

        /// <summary>
        /// The group ID of the car the service NPC arrives in. This is for when the car is a Store, expansion pack, or stuff pack item.
        /// </summary>
        uint CarGroupId
        {
            get;
            set;
        }

        /// <summary>
        /// The instance ID of the car the service NPC arrives in.
        /// </summary>
        ulong CarInstanceId
        {
            get;
            set;
        }

        /// <summary>
        /// The instance name of the car the service NPC arrives in. If <c>null</c> or empty, the service NPC will arrive and leave by foot.
        /// </summary>
        string CarInstanceName
        {
            get;
            set;
        }

        /// <summary>
        /// The preset of the car the service NPC arrives in.
        /// </summary>
        string CarPreset
        {
            get;
            set;
        }

        /// <summary>
        /// Length of time (in minutes) between checks that everything is done.
        /// </summary>
        float CheckTime
        {
            get;
            set;
        }

        /// <summary>
        /// Length of time (in minutes) that the service NPC waits before routing to lot.
        /// </summary>
        float DelayBeforeArriving
        {
            get;
            set;
        }

        /// <summary>
        /// Length of time (in minutes) that the service NPC waits before leaving the lot, after their work is done.
        /// </summary>
        float DelayBeforeLeaving
        {
            get;
            set;
        }

        /// <summary>
        /// Length of time (in minutes) that the service NPC takes to drive to lot.
        /// </summary>
        float DriveTime
        {
            get;
            set;
        }

        /// <summary>
        /// Extra time (in minutes) to wait before leaving if the service NPC is socialized with.
        /// </summary>
        float ExtraWaitTimeAfterSocializing
        {
            get;
            set;
        }

        /// <summary>
        /// The list of hidden traits for the service NPC.
        /// Note: do not add or remove elements from this property directly, as that will not work. Use the <see cref="AddHiddenTraits"/> and <see cref="RemoveHiddenTraits"/> methods instead.
        /// </summary>
        List<TraitNames> HiddenTraits
        {
            get;
            set;
        }

        /// <summary>
        /// The items the service NPC spawns with.
        /// </summary>
        List<InventoryObjectCreationParameters> Inventory
        {
            get;
            set;
        }

        /// <summary>
        /// If set to <c>true</c>, the service cannot be changed in the game.
        /// </summary>
        bool IsImmutable
        {
            get;
            set;
        }

        /// <summary>
        /// If set to <c>true</c>, the service NPC will care for babies, toddlers, and children.
        /// </summary>
        bool IsBabysittingService
        {
            get;
            set;
        }

        /// <summary>
        /// If set to <c>true</c>, the service NPC will stay with the household that requested them.
        /// Interactions for setting/unsetting their bed will be available to the service NPC with this property set to <c>true</c>.
        /// </summary>
        bool IsLiveInService
        {
            get;
            set;
        }

        /// <summary>
        /// If set to <c>true</c>, the service NPC will avoid interacting with objects in the same room as a sleeping Sim.
        /// </summary>
        bool IsQuietAroundSleepingSims
        {
            get;
            set;
        }

        /// <summary>
        /// If set to <c>true</c>, the service NPC will quit when they are in close proximity to Bonehilda, and will scream and run away dramatically.
        /// </summary>
        bool IsScaredOfBonehilda
        {
            get;
            set;
        }

        /// <summary>
        /// The service motives. If assigning manually (rather than as a parameter in the constructor) be sure include the <see cref="ServiceMotive"/> property.
        /// Note: do not add or remove elements from this property directly, as that will not work. Use the <see cref="AddMotives"/> and <see cref="RemoveMotives"/> methods instead.
        /// </summary>
        List<CommodityKind> Motives
        {
            get;
            set;
        }

        /// <summary>
        /// The internal name of the service. Must be unique.
        /// </summary>
        string Name
        {
            get;
            set;
        }

        /// <summary>
        /// The list of outputs added to interaction tunings for the service motive.
        /// </summary>
        List<ServiceUtils.CommodityChange> Outputs
        {
            get;
            set;
        }

        /// <summary>
        /// The number of potential traits to randomly pick from the list of potential traits (see <see cref="PotentialTraits"/>) for the service NPC.
        /// </summary>
        int PotentialTraitCount
        {
            get;
            set;
        }

        /// <summary>
        /// The list of potential traits for the service NPC; will be randomly picked from, the number of which is specified by <see cref="PotentialTraitCount"/>.
        /// Note: do not add or remove elements from this property directly, as that will not work. Use the <see cref="AddPotentialTraits"/> and <see cref="RemovePotentialTraits"/> methods instead.
        /// </summary>
        List<TraitNames> PotentialTraits
        {
            get;
            set;
        }

        /// <summary>
        /// If set to <c>true</c>, the service NPC will have standard social interactions unavailable.
        /// </summary>
        bool PreventSocialization
        {
            get;
            set;
        }

        /// <summary>
        /// If the service NPC's relationship with any YAE falls below this level, they will quit.
        /// </summary>
        float RelationshipLevelForQuit
        {
            get;
            set;
        }
            
        /// <summary>
        /// If set to <c>true</c>, the service NPC will call emergency services when there is a fire on the lot they're assigned to.
        /// </summary>
        bool ReportsFires
        {
            get;
            set;
        }
            
        /// <summary>
        /// The message that shows when a service NPC is requested.
        /// </summary>
        string RequestedMessage
        {
            get;
            set;
        }

        /// <summary>
        /// If set to <c>true</c>, the service NPC is required to be in the same room as a Sim they are caring for.
        /// </summary>
        bool RequireBeInSameRoom
        {
            get;
            set;
        }

        /// <summary>
        /// Note: this can (and should) be omitted for the UI for creating/editing service profiles.
        /// </summary>
        CommodityKind ServiceMotive
        {
            get;
            set;
        }

        /// <summary>
        /// Includes cost, whether the service is recurring, whether the service is an emergency service, etc.
        /// </summary>
        Service.ServiceTuning ServiceTuning
        {
            get;
            set;
        }

        /// <summary>
        /// Note: this can (and should) be omitted for the UI for creating/editing service profiles.
        /// </summary>
        ServiceType ServiceType
        {
            get;
            set;
        }

        /// <summary>
        /// The list of skills (and their levels) for the service NPC to start out with.
        /// </summary>
        List<SkillLevelPair> Skills
        {
            get;
            set;
        }

        /// <summary>
        /// Length of time (in hours) that the service NPC spends performing their duties before they charge for their service and leave.
        /// Note: this will only be used when <see cref="IsLiveInService"/> is set to <c>false</c>.
        /// </summary>
        float TimeToSpendWorking
        {
            get;
            set;
        }

        /// <summary>
        /// How old leftovers can be out in minutes before the service NPC will put it away.
        /// Note: this will only be used when <see cref="WaitsBeforePuttingAwayLeftovers"/> is set to <c>true</c>.
        /// </summary>
        float TimeWaitBeforePutawayLeftovers
        {
            get;
            set;
        }

        /// <summary>
        /// The display name of the service.
        /// </summary>
        string Title
        {
            get;
            set;
        }

        /// <summary>
        /// The list of explicit traits for the service NPC.
        /// Note: do not add or remove elements from this property directly, as that will not work. Use the <see cref="AddTraits"/> and <see cref="RemoveTraits"/> methods instead.
        /// </summary>
        List<TraitNames> Traits
        {
            get;
            set;
        }

        /// <summary>
        /// Multiplier for interactions in a room where a Sim is sleeping.
        /// Note: this will only be used when <see cref="IsQuietAroundSleepingSims"/> is set to <c>true</c>.
        /// </summary>
        float UseObjectInSameRoomAsSleeperMultiplier
        {
            get;
            set;
        }

        /// <summary>
        /// The allowed range of ages that the service NPC can be.
        /// </summary>
        CASAgeGenderFlags ValidAges
        {
            get;
            set;
        }

        /// <summary>
        /// The allowed range of genders that the service NPC can be.
        /// </summary>
        CASAgeGenderFlags ValidGenders
        {
            get;
            set;
        }

        /// <summary>
        /// The version string used to accommodate for changes to default values between versions of the Expanded Household Staff mod.
        /// </summary>
        string VersionString
        {
            get;
            set;
        }

        /// <summary>
        /// If set to <c>true</c>, the service NPC waits a bit after a meal is prepared before putting it away.
        /// </summary>
        bool WaitsBeforePuttingAwayLeftovers
        {
            get;
            set;
        }

        void AddActions(params ServiceUtils.ActiveTopicAction[] actions);

        void AddHiddenTraits(params TraitNames[] traits);

        void AddMotives(params CommodityKind[] motives);

        void AddOutputs(params ServiceUtils.CommodityChange[] outputs);

        void AddPotentialTraits(params TraitNames[] traits);

        void AddSkills(params SkillLevelPair[] skills);

        void AddTraits(params TraitNames[] traits);

        ServiceProfileFlags GetFlags();

        void RemoveActions(params ServiceUtils.ActiveTopicAction[] actions);

        void RemoveActions(Predicate<ServiceUtils.ActiveTopicAction> predicate);

        void RemoveHiddenTraits(Predicate<TraitNames> predicate);

        void RemoveHiddenTraits(params TraitNames[] traits);

        void RemoveMotives(params CommodityKind[] motives);

        void RemoveMotives(Predicate<CommodityKind> predicate);

        void RemoveOutputs(params ServiceUtils.CommodityChange[] outputs);

        void RemoveOutputs(Predicate<ServiceUtils.CommodityChange> predicate);

        void RemovePotentialTraits(Predicate<TraitNames> predicate);

        void RemovePotentialTraits(params TraitNames[] traits);

        void RemoveSkills(Predicate<SkillLevelPair> predicate);

        void RemoveSkills(params SkillLevelPair[] skills);

        void RemoveTraits(Predicate<TraitNames> predicate);

        void RemoveTraits(params TraitNames[] traits);

        void SetFlags(ServiceProfileFlags flags);
    }
}
