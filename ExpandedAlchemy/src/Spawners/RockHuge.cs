using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Interfaces;
using System;

namespace Sims3.Gameplay.Destrospean.ExpandedAlchemy.Spawners
{
    public class RockHuge : Objects.Spawners.RockHuge, IAlchemyIngredient
    {
        string IAlchemyIngredient.IngredientTypeName
        {
            get
            {
                foreach (string name in Enum.GetNames(typeof(RockGemMetal)))
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
