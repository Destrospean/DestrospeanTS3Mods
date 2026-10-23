using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Utilities;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.SimIFace;
using Destrospean.Utils;
using Destrospean.Utils.ExpandedHouseholdStaff;

namespace Destrospean.ExpandedHouseholdStaff.Interactions
{
    public class ExportServiceCollection : ImmediateInteraction<Sim, GameObject>
    {
        [DoesntRequireTuning]
        public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ExportServiceCollection>
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
                return !isAutonomous && ServiceUtils.ServiceProfiles.Count > 0;
            }
        }

        public static readonly string LocalizationKey = typeof(ExportServiceCollection).GetLocalizationKey();

        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            DebugUtils.TryDisplayScriptError(() =>
                {
                    IServiceProfile[] profiles;
                    if (ServiceUtils.TryUIGetSelectedServiceProfiles(out profiles, ServiceUtils.ServiceProfiles.ToArray(), Localization.LocalizeString(LocalizationKey + ":Name")))
                    {
                        string xml = FileUtils.GetXml(profiles);
                        FileUtils.ExportToFile(xml);
                        if (Settings.kExportServiceCollectionsAsXMLs)
                        {
                            uint fileHandle = 0u;
                            Simulator.CreateExportFile(ref fileHandle, "ServiceProfiles");
                            if (fileHandle != 0u)
                            {
                                CustomXmlWriter customXmlWriter = new CustomXmlWriter(fileHandle);
                                int postDeclarationIndex = xml.IndexOf("\r\n");
                                int lineEndingLength = 2;
                                if (postDeclarationIndex == -1)
                                {
                                    postDeclarationIndex = xml.IndexOf("\n");
                                    lineEndingLength = 1;
                                }
                                customXmlWriter.WriteToBuffer(xml.Remove(postDeclarationIndex + lineEndingLength));
                                customXmlWriter.WriteComment(" These XMLs are only exported when `kExportServiceCollectionsAsXMLs` is enabled in the tuning. ");
                                customXmlWriter.WriteToBuffer(xml.Substring(postDeclarationIndex + lineEndingLength));
                                customXmlWriter.WriteEndDocument();
                            }
                        }
                    }
                });
            return true;
        }
    }
}
