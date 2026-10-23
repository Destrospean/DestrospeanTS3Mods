using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.SimIFace;
using Destrospean.Misc;
using Destrospean.Utils;
using Destrospean.Utils.ExpandedHouseholdStaff;

namespace Destrospean.ExpandedHouseholdStaff.Interactions
{
    public class RemoveInventoryObject : ImmediateInteraction<Sim, GameObject>
    {
        [DoesntRequireTuning]
        public class Definition : ImmediateInteractionDefinition<Sim, GameObject, RemoveInventoryObject>
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
                return !isAutonomous && ServiceUtils.ServiceProfiles.Exists(x => !x.IsImmutable && x.Inventory.Count > 0);
            }
        }

        public static readonly string LocalizationKey = typeof(RemoveInventoryObject).GetLocalizationKey();

        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            DebugUtils.TryDisplayScriptError(() =>
                {
                    IServiceProfile[] profiles;
                    if (ServiceUtils.TryUIGetSelectedServiceProfiles(out profiles, ServiceUtils.ServiceProfiles.FindAll(x => !x.IsImmutable && x.Inventory.Count > 0).ToArray(), Localization.LocalizeString(LocalizationKey + ":Name"), 1))
                    {
                        InventoryObjectCreationParameters[] inventoryObjects;
                        if (ServiceUtils.TryUIGetSelectedInventoryObjects(out inventoryObjects, profiles[0].Inventory.ToArray(), Localization.LocalizeString(LocalizationKey + ":Name")))
                        {
                            foreach (InventoryObjectCreationParameters inventoryObjectCreationParameters in inventoryObjects)
                            {
                                profiles[0].Inventory.Remove(inventoryObjectCreationParameters);
                            }
                        }
                    }
                });
            return true;
        }
    }
}
