using System;
using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Interfaces;

namespace Sims3.Gameplay.Destrospean.ExpandedAlchemy.Spawners
{
    public class Rock : Objects.Spawners.Rock, IAlchemyIngredient
    {
        string IAlchemyIngredient.IngredientTypeName
        {
            get
            {
                foreach (var name in Enum.GetNames(typeof(RockGemMetal)))
                {
                    if (name == mGuid.ToString())
                    {
                        return name;
                    }
                }
                return mData.mName.Substring(mData.mName.LastIndexOf(':') + 1);
            }
        }
    }
}
