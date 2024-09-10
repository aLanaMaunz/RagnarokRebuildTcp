﻿using Assets.Scripts.Network;
using Assets.Scripts.Objects;
using RebuildSharedData.Enum;

namespace Assets.Scripts.SkillHandlers.Handlers
{
    [SkillHandler(CharacterSkill.Endure)]
    public class EndureSkillHandler : SkillHandlerBase
    {
        public override void ExecuteSkillTargeted(ServerControllable src, ref AttackResultData attack)
        {
            AudioManager.Instance.OneShotSoundEffect(src.Id, "ef_endure.ogg", src.transform.position, 0.7f);
            src.PerformSkillMotion();
        }
    }
}