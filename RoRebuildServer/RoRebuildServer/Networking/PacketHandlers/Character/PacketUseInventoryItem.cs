﻿using RebuildSharedData.Enum;
using RebuildSharedData.Networking;
using RoRebuildServer.Data;
using RoRebuildServer.EntityComponents;
using RoRebuildServer.EntityComponents.Util;
using RoRebuildServer.Logging;
using System.Diagnostics;

namespace RoRebuildServer.Networking.PacketHandlers.Character;

[ClientPacketHandler(PacketType.UseInventoryItem)]
public class PacketUseInventoryItem : IClientPacketHandler
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

        var character = connection.Character;
        var player = connection.Player;

        var itemId = msg.ReadInt32();

        //obviously you should check if the item is in your inventory, but we have no inventory!
        
        player.AddActionDelay(CooldownActionType.UseItem);

        if (!DataManager.ItemList.TryGetValue(itemId, out var item))
        {
            ServerLogger.LogError($"User is attempting to use invalid item id {itemId}. Due to the error, the player will be disconnected.");
            NetworkManager.DisconnectPlayer(connection);
            return;
        }

        if (!item.IsUseable)
        {
            ServerLogger.LogWarning($"User is attempting to use item {item.Code}, but it is not usable.");
            return;
        }

        if (item.Interaction == null)
            return;

        if (item.Effect >= 0)
        {
            character.Map.AddVisiblePlayersAsPacketRecipients(character);
            CommandBuilder.SendEffectOnCharacterMulti(character, item.Effect);
            CommandBuilder.ClearRecipients();
        }

        item.Interaction.OnUse(character.Player, character.CombatEntity);
    }
}