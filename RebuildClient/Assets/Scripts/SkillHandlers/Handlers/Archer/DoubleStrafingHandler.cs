﻿using Assets.Scripts.Effects.EffectHandlers;
using Assets.Scripts.Network;
using Assets.Scripts.Objects;
using RebuildSharedData.Enum;
using UnityEngine;

namespace Assets.Scripts.SkillHandlers.Handlers
{
    [SkillHandler(CharacterSkill.DoubleStrafe)]
    public class DoubleStrafeHandler : SkillHandlerBase
    {
        public override void ExecuteSkillTargeted(ServerControllable src, ref AttackResultData attack)
        {
            DefaultSkillCastEffect.Create(src);
            src.PerformBasicAttackMotion();
            AudioManager.Instance.AttachSoundToEntity(src.Id, "ef_bash.ogg", src.gameObject);

            if (attack.Target == null)
                return;
            
            //Debug.Break();
            //Time.timeScale = 0.1f;
            
            ArcherArrow.CreateArrow(src, attack.Target.gameObject, attack.MotionTime, -0.1f + Random.Range(-0.1f, 0f));
            ArcherArrow.CreateArrow(src, attack.Target.gameObject, attack.MotionTime+Random.Range(-0.06f, 0.06f), 0.1f + Random.Range(0, 0.1f));
            //attack.Target.Messages.SendHitEffect(src, attack.MotionTime + arrow.Duration);
            if (attack.Result != AttackResult.Miss && attack.Result != AttackResult.Invisible)
            {
                attack.Target.Messages.SendHitEffect(src, attack.MotionTime);
                attack.Target.Messages.SendHitEffect(src, attack.MotionTime + 0.2f);
            }
        }
    }
}