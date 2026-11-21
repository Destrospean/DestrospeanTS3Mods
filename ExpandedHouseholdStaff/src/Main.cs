extern alias NRaasTagger;
using System;
using System.Collections.Generic;
using Destrospean.ExpandedHouseholdStaff.Interactions;
using Destrospean.Utils;
using Destrospean.Utils.ExpandedHouseholdStaff;
using NRaasTagger::NRaas.TaggerSpace.Helpers;
using Sims3.Gameplay;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.ActorSystems.Children;
using Sims3.Gameplay.CAS;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff.Interactions;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff.Services;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Objects.Electronics;
using Sims3.Gameplay.Objects.Gardening;
using Sims3.Gameplay.Objects.Vehicles;
using Sims3.Gameplay.Socializing;
using Sims3.SimIFace;
using Queries = Sims3.Gameplay.Queries;

namespace Destrospean.ExpandedHouseholdStaff
{
    public class Main
    {
        [Tunable]
        protected static bool kInstantiator;

        static Main()
        {
            CommonUtils.ReplaceMethod<Replacements, Inventory>(nameof(Replacements.AddInternal_Original), nameof(Replacements.AddInternal));
            CommonUtils.ReplaceMethod<Inventory, Replacements>(nameof(Replacements.AddInternal));
            CommonUtils.ReplaceMethod<Replacements, CaregiverRoutingMonitor>(nameof(Replacements.CountBabiesToddlersAndCaregivers_Original), nameof(Replacements.CountBabiesToddlersAndCaregivers));
            CommonUtils.ReplaceMethod<CaregiverRoutingMonitor, Replacements>(nameof(Replacements.CountBabiesToddlersAndCaregivers));
            CommonUtils.ReplaceMethod<Replacements, Plant>(nameof(Replacements.GardenInteractionLotValidityTest_Original), nameof(Replacements.GardenInteractionLotValidityTest));
            CommonUtils.ReplaceMethod<Plant, Replacements>(nameof(Replacements.GardenInteractionLotValidityTest));
            CommonUtils.ReplaceMethod<Replacements, SocialComponent>(nameof(Replacements.IsInServicePreventingSocialization_Original), nameof(Replacements.IsInServicePreventingSocialization));
            CommonUtils.ReplaceMethod<SocialComponent, Replacements>(nameof(Replacements.IsInServicePreventingSocialization));
            CommonUtils.ReplaceMethod<Replacements, SimDescription>(nameof(Replacements.IsPerformingAnyServiceOfType_Original), nameof(Replacements.IsPerformingAnyServiceOfType));
            CommonUtils.ReplaceMethod<SimDescription, Replacements>(nameof(Replacements.IsPerformingAnyServiceOfType));
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                switch (assembly.GetName().Name)
                {
                    case "NRaasMasterController":
                        NRaasMasterControllerIntegration.Init();
                        break;
                    case "NRaasTagger":
                        IntegrateNRaasTagger();
                        break;
                }
            }
            InteractionObjectTypeUtils.InitTypes();
            LoadSaveManager.ObjectGroupsPreLoad += () => Phone.CallForServices.Singleton = new CallForServices.Definition();
            World.OnObjectPlacedInLotEventHandler += (sender, e) => DebugUtils.TryDisplayScriptError(() =>
                {
                    if (e is World.OnObjectPlacedInLotEventArgs onObjectPlacedInLotEventArgs)
                    {
                        var gameObject = GameObject.GetObject(onObjectPlacedInLotEventArgs.ObjectId);
                        gameObject.AddInteraction(ListInteractions.Singleton, true);
                        gameObject.AddInteraction(AddInventoryObjectToService.Singleton, true);
                        (gameObject as Car)?.AddInteraction(AssignCarToService.Singleton, true);
                        (gameObject as Mailbox).AddServiceProfileInteractions();
                    }
                });
            World.sOnWorldLoadFinishedEventHandler += (sender, e) => DebugUtils.TryDisplayScriptError(() =>
                {
                    foreach (var gameObject in Queries.GetObjects<GameObject>())
                    {
                        gameObject.AddInteraction(ListInteractions.Singleton, true);
                        gameObject.AddInteraction(AddInventoryObjectToService.Singleton, true);
                        (gameObject as Car)?.AddInteraction(AssignCarToService.Singleton, true);
                        (gameObject as Mailbox).AddServiceProfileInteractions();
                    }
                    foreach (var profile in new List<IServiceProfile>(ServiceUtils.ServiceProfiles))
                    {
                        if (profile.IsImmutable)
                        {
                            profile.RemoveServiceFromSaveGame();
                            continue;
                        }
                        CustomService.Init(profile);
                        profile.FixUp();
                    }
                });
            World.sOnWorldQuitEventHandler += (sender, e) =>
                {
                    foreach (var service in new List<CustomService>(ServiceUtils.CustomInstances.Values))
                    {
                        CustomService.Deinit(service.Profile, true);
                    }
                };
        }

        public static void IntegrateNRaasTagger()
        {
            CommonUtils.ReplaceMethod<TagDataHelper, Replacements>(nameof(Replacements.GetStatus));
        }
    }
}
