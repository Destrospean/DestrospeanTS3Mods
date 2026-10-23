using Sims3.Gameplay.Interactions;
using Sims3.Gameplay.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Destrospean.Utils
{
    public class InteractionObjectTypeUtils
    {
        static IDictionary<string, Type> sGameObjectTypes;

        static IDictionary<string, Type> sInteractionDefinitionTypes;

        public static IDictionary<string, Type> GameObjectTypes
        {
            get
            {
                if (sGameObjectTypes == null)
                {
                    InitTypes();
                }
                return sGameObjectTypes;
            }
        }

        public static IDictionary<string, Type> InteractionDefinitionTypes
        {
            get
            {
                if (sInteractionDefinitionTypes == null)
                {
                    InitTypes();
                }
                return sInteractionDefinitionTypes;
            }
        }

        public static void InitTypes()
        {
            IDictionary<string, Type> gameObjectTypes = new Dictionary<string, Type>();
            IDictionary<string, Type> interactionDefinitionTypes = new Dictionary<string, Type>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (typeof(IGameObject).IsAssignableFrom(type))
                    {
                        gameObjectTypes[type.FullName] = type;
                    }
                    if (typeof(InteractionDefinition).IsAssignableFrom(type))
                    {
                        interactionDefinitionTypes[type.FullName] = type;
                    }
                }
            }
            sGameObjectTypes = gameObjectTypes;
            sInteractionDefinitionTypes = interactionDefinitionTypes;
        }
    }
}
