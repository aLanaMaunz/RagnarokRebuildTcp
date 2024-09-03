﻿using Assets.Scripts.Effects.EffectHandlers.StatusEffects;
using Assets.Scripts.Network.HandlerBase;
using Assets.Scripts.Sprites;
using RebuildSharedData.Enum;
using RebuildSharedData.Networking;
using UnityEngine;

namespace Assets.Scripts.Network.IncomingPacketHandlers.Character
{
    [ClientPacketHandler(PacketType.ApplyStatusEffect)]
    public class PacketApplyStatusEffect : ClientPacketHandlerBase
    {
        public override void ReceivePacket(ClientInboundMessage msg)
        {
            var id = msg.ReadInt32();
            var status = (CharacterStatusEffect)msg.ReadByte();

            if (Network.EntityList.TryGetValue(id, out var controllable))
            {
                Debug.Log($"Applying status {status} to {controllable.Name}");
                if (status == CharacterStatusEffect.Stun)
                {
                    StunEffect.AttachStunEffect(controllable);
                    // controllable.SpriteAnimator.Color = new Color(1, 0.5f, 0.5f);
                    // if(controllable.SpriteAnimator.CurrentMotion is SpriteMotion.Idle or SpriteMotion.Standby)
                    //     controllable.SpriteAnimator.AnimSpeed = 2f;
                }
                if (status == CharacterStatusEffect.TwoHandQuicken)
                {
                    controllable.SpriteAnimator.Color = new Color(1, 1, 0.5f);
                    RoSpriteTrailManager.Instance.AttachTrailToEntity(controllable);
                }
            }
        }
    }
}