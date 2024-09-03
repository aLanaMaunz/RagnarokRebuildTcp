﻿using System.Diagnostics;
using System.Numerics;
using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RebuildSharedData.Networking;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.Logging;
using RoRebuildServer.Simulation;
using RoRebuildServer.Simulation.Skills;

namespace RoRebuildServer.Networking.PacketHandlers.Character
{
    [ClientPacketHandler(PacketType.Skill)]
    public class PacketSkill : IClientPacketHandler
    {
        public void Process(NetworkConnection connection, InboundMessage msg)
        {
            if (!connection.IsPlayerAlive)
                return;

            Debug.Assert(connection.Player != null);
            Debug.Assert(connection.Character != null);
            Debug.Assert(connection.Character.Map != null);

            if (!connection.Player.CanPerformCharacterActions())
                return;

            var type = (SkillTarget)msg.ReadByte();

            switch (type)
            {
                case SkillTarget.Ground:
                    ProcessGroundTargetedSkill(connection, msg);
                    return;
                case SkillTarget.Enemy: 
                    ProcessSingleTargetSkill(connection, msg);
                    return;
                case SkillTarget.Self:
                    ProcessSelfTargetedSkill(connection, msg);
                    return;
            }

            ServerLogger.Log($"Player {connection.Character.Name} is attempting to use a skill of type {type}, but no handler exists for this class of skill");
            return;
        }

        private void ProcessSelfTargetedSkill(NetworkConnection connection, InboundMessage msg)
        {
            Debug.Assert(connection.Player != null, "connection.Player != null");

            var skill = (CharacterSkill)msg.ReadInt16();
            var lvl = msg.ReadByte();

            if (!connection.Player.DoesCharacterKnowSkill(skill, lvl))
            {
                CommandBuilder.SkillFailed(connection.Player, SkillValidationResult.Failure);
                return;
            }

            var target = SkillHandler.GetSkillAttributes(skill).SkillTarget;
            if (target == SkillTarget.Passive)
            {
                ServerLogger.LogWarning($"Player {connection.Character.Name} is incorrectly trying to target self with the skill {skill}.");
                return;
            }

            connection.Player.CombatEntity.AttemptStartSelfTargetSkill(skill, lvl);
        }

        private void ProcessGroundTargetedSkill(NetworkConnection connection, InboundMessage msg)
        {
            Debug.Assert(connection.Player != null, "connection.Player != null");

            var caster = connection.Character;
            var groundTarget= msg.ReadPosition();
            var map = caster?.Map;

            if (map == null || caster == null)
                throw new Exception($"Cannot ProcessGroundTargetedSkill, player or map is invalid.");
            
            if (!map.WalkData.HasLineOfSight(caster.Position, groundTarget))
            {
                CommandBuilder.SkillFailed(connection.Player, SkillValidationResult.NoLineOfSight);
                return;
            }

            var skill = (CharacterSkill)msg.ReadByte();
            var lvl = (int)msg.ReadByte();

            if (!connection.Player.DoesCharacterKnowSkill(skill, lvl))
            {
                CommandBuilder.SkillFailed(connection.Player, SkillValidationResult.Failure);
                return;
            }

            var target = SkillHandler.GetSkillAttributes(skill).SkillTarget;
            if (target != SkillTarget.Ground)
            {
                ServerLogger.LogWarning($"Player {connection.Character.Name} is incorrectly trying to target self with the skill {skill}.");
                return;
            }

            caster.ResetSpawnImmunity();
            caster.CombatEntity.AttemptStartGroundTargetedSkill(groundTarget, skill, lvl);
        }

        private void ProcessSingleTargetSkill(NetworkConnection connection, InboundMessage msg)
        {
            Debug.Assert(connection.Player != null, "connection.Player != null");

            var caster = connection.Character;
            var targetEntity = World.Instance.GetEntityById(msg.ReadInt32());
            var target = targetEntity.GetIfAlive<CombatEntity>();
            if (target == null || caster == null)
                return;
            
            var skill = (CharacterSkill)msg.ReadByte();
            var lvl = (int)msg.ReadByte();

            if (!connection.Player.DoesCharacterKnowSkill(skill, lvl))
            {
                CommandBuilder.SkillFailed(connection.Player, SkillValidationResult.Failure);
                return;
            }

            var isAlly = target.IsValidAlly(caster.CombatEntity);
            var targetType = SkillHandler.GetSkillAttributes(skill).SkillTarget;
            var isValidTarget = targetType == SkillTarget.Any;
            if (isAlly && (targetType == SkillTarget.Ally || targetType == SkillTarget.Any))
                isValidTarget = true;
            if (!isAlly & (targetType == SkillTarget.Enemy || targetType == SkillTarget.Any))
                isValidTarget = true;
            if (caster.Id == target.Character.Id && (targetType == SkillTarget.Self || targetType == SkillTarget.Ally ||
                                                     targetType == SkillTarget.Any))
                isValidTarget = true;

            if (!isValidTarget)
            {
                ServerLogger.LogWarning($"Player '{connection.Character.Name}' is incorrectly trying to target '{target.Character.Name}' with the skill {skill}.");
                return;
            }

            caster.ResetSpawnImmunity();
            caster.CombatEntity.AttemptStartSingleTargetSkillAttack(target, skill, lvl);
        }
    }
}
