﻿using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RebuildSharedData.Networking;
using RoRebuildServer.EntityComponents.Character;

namespace RoRebuildServer.Networking.PacketHandlers.Admin;

[AdminClientPacketHandler(PacketType.AdminChangeAppearance)]
public class PacketAdminChangeAppearance : IClientPacketHandler
{
    public void Process(NetworkConnection connection, InboundMessage msg)
    {
        if (!connection.IsOnlineAdmin)
            return;

        var p = connection.Player;

        if (p == null) return;

        var id = msg.ReadInt32();
        var val = msg.ReadInt32();
        
        switch (id)
        {
            default:
            case 0:
                p.SetData(PlayerStat.Head, GameRandom.NextInclusive(0, 31));
                p.SetData(PlayerStat.Gender, GameRandom.NextInclusive(0, 1));
                break;
            case 1:
                if(val >= 0 && val <= 31)
                    p.SetData(PlayerStat.Head, val);
                else
                    p.SetData(PlayerStat.Head, GameRandom.NextInclusive(0, 31));
                break;
            case 2:
                if(val >= 0 && val <= 1)
                    p.SetData(PlayerStat.Gender, val);
                else
                    p.SetData(PlayerStat.Gender, GameRandom.NextInclusive(0, 1));
                break;
            case 3:
                if (val >= 0 && val <= 6)
                    p.ChangeJob(val);
                else
                    p.ChangeJob(GameRandom.Next(0, 6));
                return; //return as we don't want to double refresh (change job will refresh)
            case 4:
                if (val >= 0 && val <= 12)
                    p.SetWeaponClassOverride(val);
                else
                    p.SetWeaponClassOverride(0);
                break;
        }

        connection.Character.Map.RefreshEntity(p.Character);
        p.UpdateStats();
    }
}