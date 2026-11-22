using System;
using System.Collections.Generic;
using Sims3.Gameplay.Socializing;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using Sims3.UI.Controller;

namespace Destrospean.Utils
{
    public class SocialUtils
    {
        /// <summary>
        /// Adds actions to an active topic.
        /// </summary>
        /// <param name="activeTopic">Active topic.</param>
        /// <param name="grouping">Grouping.</param>
        /// <param name="isActive">If set to <c>true</c>, the action is for the first person actor; otherwise, it's for the second person actor.</param>
        /// <param name="actionsToAdd">Actions to add.</param>
        public static void AddActions(string activeTopic, LongTermRelationshipTypes grouping, bool isActive, params string[] actionsToAdd)
        {
            if (!ActionAvailabilityData.sActiveTopicInteractions.TryGetValue(activeTopic, out var groups))
            {
                groups = new Dictionary<LongTermRelationshipTypes, Dictionary<bool, List<string>>>();
                ActionAvailabilityData.sActiveTopicInteractions.Add(activeTopic, groups);
            }
            if (!groups.TryGetValue(grouping, out var group))
            {
                group = new Dictionary<bool, List<string>>();
                groups.Add(grouping, group);
            }
            if (!group.TryGetValue(isActive, out var actions))
            {
                actions = new List<string>();
                group.Add(isActive, actions);
            }
            actions.AddRange(actionsToAdd);
        }
        
        public static void LoadSocialData(string instanceName)
        {
            if (!string.IsNullOrEmpty(instanceName))
            {
                var isEp5Installed = GameUtils.IsInstalled(ProductVersion.EP5);
                var xmlDocument = Simulator.LoadXML(instanceName);
                foreach (var element in new XmlElementLookup(xmlDocument)["Action"])
                {
                    ParserFunctions.TryParseEnum(element.GetAttribute("com"), out var commodityTypes, CommodityTypes.Undefined);
                    ActionData.Add(new ActionData(element.GetAttribute("key"), commodityTypes, ProductVersion.BaseGame, new XmlElementLookup(element), isEp5Installed));
                }
            }
        }

        public static void LoadSocializingActionAvailability(string instanceName)
        {
            var data = XmlDbData.ReadData(instanceName);
            if (data != null)
            {
                if (data.Tables.ContainsKey("SAA"))
                {
                    SocialManager.ParseStcActionAvailability(data);
                }
                if (data.Tables.ContainsKey("TAA"))
                {
                    SocialManager.ParseActiveTopic(data);
                }
            }
        }
        
        /// <summary>
        /// Removes actions from an active topic.
        /// </summary>
        /// <param name="activeTopic">Active topic.</param>
        /// <param name="grouping">Grouping.</param>
        /// <param name="isActive">If set to <c>true</c>, the action is for the first person actor; otherwise, it's for the second person actor.</param>
        /// <param name="actionsToRemove">Actions to remove.</param>
        public static void RemoveActions(string activeTopic, LongTermRelationshipTypes grouping, bool isActive, params string[] actionsToRemove)
        {
            if (ActionAvailabilityData.sActiveTopicInteractions.TryGetValue(activeTopic, out var groups) && groups.TryGetValue(grouping, out var group) && group.TryGetValue(isActive, out var actions))
            {
                actions.RemoveAll(x => Array.Exists(actionsToRemove, y => y == x));
            }
        }
    }
}
