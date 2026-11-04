using Sims3.Gameplay.Abstracts;
using Sims3.Gameplay.Actors;
using Sims3.Gameplay.ActorSystems;
using Sims3.Gameplay.Core;
using Sims3.Gameplay.Interfaces;
using Sims3.Gameplay.Utilities;
using Sims3.SimIFace;
using System;
using System.Collections.Generic;

namespace Sims3.Gameplay.Objects.Spawners
{
    public class Uranium : Gem, IAlchemyIngredient
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

        [Tunable]
        [TunableComment("How often the Uranium sparkle Vfx plays in Sim hours. Format: (min, max). These are float values.")]
        static float[] kVfxFrequency = new[]
            {
                0.5f,
                1f
            };
        
        AlarmHandle mVfxAlarm = AlarmHandle.kInvalidHandle;

        public override void Dispose()
        {
            RemoveAlarm(mVfxAlarm);
            mVfxAlarm = AlarmHandle.kInvalidHandle;
            base.Dispose();
        }

        public override void DoReset(ResetInformation resetInformation)
        {
            base.DoReset(resetInformation);
            ScheduleNextVfx();
        }

        public void OnAddToInventory(IGameObject gameObject, Inventory inventory)
        {
            if (inventory.Owner is Sim sim)
            {
                List<Uranium> uranium = inventory.FindAll<Uranium>(false);
                if (uranium.Count == 1)
                {
                    sim.BuffManager.AddElementPaused(BuffNames.Nauseous, Origin.None);
                }
            }
        }

        public void OnRemoveFromInventory(IGameObject gameObject, Inventory inventory)
        {
            if (inventory.Owner is Sim sim && !inventory.ContainsType(typeof(Uranium), 1))
            {
                sim.BuffManager.RemoveElement(BuffNames.Nauseous);
            }
        }

        public override void OnStartup()
        {
            base.OnStartup();
            ItemComp.OnAddToInventoryEvent += OnAddToInventory;
            ItemComp.OnRemoveFromInventoryEvent += OnRemoveFromInventory;
            ScheduleNextVfx();
        }

        public void PlayVfx()
        {
            if (InWorld && CutName != null)
            {
                VisualEffect visualEffect = VisualEffect.Create("ep1TiberiumCut");
                visualEffect.SetPosAndOrient(Position, ForwardVector, UpVector);
                visualEffect.SubmitOneShotEffect(VisualEffect.TransitionType.SoftTransition);
            }
            ScheduleNextVfx();
        }

        public void ScheduleNextVfx()
        {
            RemoveAlarm(mVfxAlarm);
            mVfxAlarm = AddAlarm(RandomUtil.GetFloat(kVfxFrequency[0], kVfxFrequency[1]), TimeUnit.Hours, PlayVfx, "Uranium VFX alarm", AlarmType.NeverPersisted);
        }
    }
}
