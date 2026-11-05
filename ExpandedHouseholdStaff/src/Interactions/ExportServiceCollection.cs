using System;
using Destrospean.Utils;
using Destrospean.Utils.ExpandedHouseholdStaff;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Destrospean.ExpandedHouseholdStaff;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;

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
            return !DebugUtils.TryDisplayScriptError(() =>
                {
                    while (true)
                    {
                        if (ServiceUtils.TryUIGetSelectedServiceProfiles(out var profiles, ServiceUtils.ServiceProfiles.ToArray(), Localization.LocalizeString(LocalizationKey + ":Name")))
                        {
                            var xml = FileUtils.GetXml(profiles);
                            if (!FileUtils.ExportToFile(xml, out var found))
                            {
                                if (found)
                                {
                                    break;
                                }
                                continue;
                            }
                            if (Settings.kExportServiceCollectionsAsXMLs)
                            {
                                var fileHandle = 0u;
                                Simulator.CreateExportFile(ref fileHandle, "ServiceProfiles");
                                if (fileHandle != 0u)
                                {
                                    var customXmlWriter = new CustomXmlWriter(fileHandle);
                                    var postDeclarationIndex = xml.IndexOf("\r\n", StringComparison.Ordinal);
                                    var lineEndingLength = 2;
                                    if (postDeclarationIndex == -1)
                                    {
                                        postDeclarationIndex = xml.IndexOf("\n", StringComparison.Ordinal);
                                        lineEndingLength = 1;
                                    }
                                    customXmlWriter.WriteToBuffer(xml.Remove(postDeclarationIndex + lineEndingLength));
                                    customXmlWriter.WriteComment(" These XMLs are only exported when `kExportServiceCollectionsAsXMLs` is enabled in the tuning. ");
                                    customXmlWriter.WriteToBuffer(xml.Substring(postDeclarationIndex + lineEndingLength));
                                    customXmlWriter.WriteEndDocument();
                                }
                            }
                        }
                        break;
                    }
                });
        }
    }
}
