using System;
using System.Collections.Generic;
using Destrospean.Enums.HairTrouble;
using Destrospean.Misc;
using Destrospean.Utils;
using Destrospean.Utils.HairTrouble;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.Autonomy;
using Sims3.Gameplay.Destrospean.HairTrouble;
using Sims3.Gameplay.Destrospean.Utils;
using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.SimIFace.CustomContent;

namespace Destrospean.HairTrouble.Interactions
{
    public class ExportHairClassificationCollection : ImmediateInteraction<Sim, GameObject>
    {
        [DoesntRequireTuning]
        public class Definition : ImmediateInteractionDefinition<Sim, GameObject, ExportHairClassificationCollection>
        {
            public override string GetInteractionName(Sim actor, GameObject target, InteractionObjectPair iop)
            {
                return Localization.LocalizeString(actor.IsFemale, LocalizationKey + ":Name");
            }

            public override string[] GetPath(bool isFemale)
            {
                return new[]
                {
                    Localization.LocalizeString(isFemale, LocalizationKey.Replace("/ExportHairClassificationCollection", ":Path"))
                };
            }

            public override bool Test(Sim actor, GameObject target, bool isAutonomous, ref GreyedOutTooltipCallback greyedOutTooltipCallback)
            {
                return Settings.kShowCheatInteractions && new List<HairGrowthStates>(SimHairGrowth.HairClassifications.Values).Exists(x => x > HairGrowthStates.Unknown) && !isAutonomous;
            }
        }

        public static readonly string LocalizationKey = typeof(ExportHairClassificationCollection).GetLocalizationKey();

        public static InteractionDefinition Singleton = new Definition();

        public override bool Run()
        {
            return !DebugUtils.TryDisplayScriptError(() =>
                {
                    var hairClassificationList = new List<KeyValuePair<string, HairGrowthStates>>();
                    foreach (var hairClassification in SimHairGrowth.HairClassifications)
                    {
                        if (hairClassification.Value > HairGrowthStates.Unknown)
                        {
                            hairClassificationList.Add(hairClassification);
                        }
                    }
                    hairClassificationList.Sort((a, b) => a.Value.CompareTo(b.Value));
                    var hairClassifications = new SortedDictionary<string, HairGrowthStates>(new DummyComparer());
                    foreach (var hairClassification in hairClassificationList)
                    {
                        hairClassifications.Add(hairClassification.Key, hairClassification.Value);
                    }
                    var xml = FileUtils.Singleton.ToXml(hairClassifications);
                    if (!FileUtils.Singleton.ExportToFile(xml, out _))
                    {
                        return;
                    }
                    if (Settings.kExportHairClassificationCollectionsAsXMLs)
                    {
                        hairClassificationList.Clear();
                        foreach (var hairClassification in SimHairGrowth.HairClassifications)
                        {
                            if (hairClassification.Value > HairGrowthStates.Unknown && !DownloadContent.IsCustomContent(S3PIResourceUtils.FromS3PIFormatKeyString(hairClassification.Key)))
                            {
                                hairClassificationList.Add(hairClassification);
                            }
                        }
                        ExportXml("Vanilla");
                        foreach (var hairClassification in SimHairGrowth.HairClassifications)
                        {
                            if (hairClassification.Value > HairGrowthStates.Unknown && DownloadContent.IsCustomContent(S3PIResourceUtils.FromS3PIFormatKeyString(hairClassification.Key)))
                            {
                                hairClassificationList.Add(hairClassification);
                            }
                        }
                        ExportXml("CustomContent");
                    }
                    return;
                    void ExportXml(string name)
                    {
                        hairClassificationList.Sort((a, b) => a.Value.CompareTo(b.Value));
                        hairClassifications.Clear();
                        foreach (var entry in hairClassificationList)
                        {
                            hairClassifications.Add(entry.Key, entry.Value);
                        }
                        xml = FileUtils.Singleton.ToXml(hairClassifications);
                        var fileHandle = 0u;
                        Simulator.CreateExportFile(ref fileHandle, "HairClassifications_" + name + "_");
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
                            customXmlWriter.WriteComment(" These XMLs are only exported when `kExportHairClassificationCollectionsAsXMLs` is enabled in the tuning. ");
                            customXmlWriter.WriteToBuffer(xml.Substring(postDeclarationIndex + lineEndingLength));
                            customXmlWriter.WriteEndDocument();
                        }
                    }
                });
        }
    }
}
