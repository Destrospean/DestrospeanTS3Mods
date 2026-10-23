using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Objects.Insect;
using System;

namespace Sims3.Gameplay.Destrospean.ExpandedAlchemy.Spawners
{
    public class NaniteTerrarium : Objects.Insect.NaniteTerrarium, IAlchemyIngredient
    {
        string IAlchemyIngredient.IngredientTypeName
        {
            get
            {
                foreach (string name in Enum.GetNames(typeof(InsectType)))
                {
                    if (name == mInsectType.ToString())
                    {
                        return name;
                    }
                }
                return mData.mName.Substring(mData.mName.LastIndexOf(':') + 1);
            }
        }
    }
}
