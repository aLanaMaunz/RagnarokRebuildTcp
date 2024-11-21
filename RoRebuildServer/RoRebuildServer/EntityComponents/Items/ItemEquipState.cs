﻿using System.Diagnostics;
using RebuildSharedData.ClientTypes;
using RebuildSharedData.Data;
using RebuildSharedData.Enum;
using RebuildSharedData.Enum.EntityStats;
using RebuildSharedData.Util;
using RoRebuildServer.Data;
using RoRebuildServer.Data.CsvDataTypes;
using RoRebuildServer.Data.Player;
using RoRebuildServer.EntityComponents.Character;
using RoRebuildServer.Logging;
using RoRebuildServer.Networking;
using RoRebuildServer.Simulation.StatusEffects.Setup;
using RoRebuildServer.Simulation.Util;
using Wintellect.PowerCollections;

namespace RoRebuildServer.EntityComponents.Items;

public struct EquipStatChange : IEquatable<EquipStatChange>
{
    public int Change;
    public CharacterStat Stat;
    public EquipSlot Slot;

    public bool Equals(EquipStatChange other)
    {
        return Stat == other.Stat && Slot == other.Slot;
    }

    public override bool Equals(object? obj)
    {
        return obj is EquipStatChange other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Stat, Change);
    }
}

public class ItemEquipState
{
    public Player Player;
    public int[] ItemSlots = new int[10];
    public int[] ItemIds = new int[10];
    public int AmmoType;
    public bool IsDualWielding;
    public AttackElement WeaponElement;
    public CharacterElement ArmorElement;
    private readonly SwapList<EquipStatChange> equipmentEffects = new();
    private EquipSlot activeSlot;
    private readonly HeadgearPosition[] headgearMultiSlotInfo = new HeadgearPosition[3];
    private bool isTwoHandedWeapon;

    public void Reset()
    {
        for (var i = 0; i < ItemSlots.Length; i++)
            ItemSlots[i] = 0;
        for (var i = 0; i < 3; i++)
            headgearMultiSlotInfo[i] = 0;
        AmmoType = -1;
    }

    public int GetEquipmentIdBySlot(EquipSlot slot) => ItemIds[(int)slot];

    public bool IsItemEquipped(int bagId)
    {
        for (var i = 0; i < 10; i++)
            if (ItemSlots[i] == bagId)
                return true;
        return false;
    }

    public void UnequipAllItems()
    {
        for (var i = 0; i < 10; i++)
        {
            if (ItemSlots[i] <= 0)
                continue;

            UnEquipEvent((EquipSlot)i);
            CommandBuilder.PlayerEquipItem(Player, ItemSlots[i], (EquipSlot)i, false);

            ItemSlots[i] = -1;
            ItemIds[i] = -1;
            AmmoType = -1;
            IsDualWielding = false;
            isTwoHandedWeapon = false;
        }
        for (var i = 0; i < 3; i++)
            headgearMultiSlotInfo[i] = HeadgearPosition.None;

    }

    public void UpdateAppearanceIfNecessary(EquipSlot slot)
    {
        switch (slot)
        {
            case EquipSlot.HeadTop:
            case EquipSlot.HeadBottom:
            case EquipSlot.HeadMid:
            case EquipSlot.Weapon:
            case EquipSlot.Shield:
                CommandBuilder.UpdatePlayerAppearanceAuto(Player);
                break;
        }
    }

    public void UnEquipItem(int bagId)
    {
        if (Player.Inventory == null || !Player.Inventory.UniqueItems.TryGetValue(bagId, out var item))
            return;

        var itemData = DataManager.ItemList[item.Id];
        var updateAppearance = false;
        if (itemData.ItemClass == ItemClass.Weapon)
        {
            UnEquipItem(EquipSlot.Weapon);
            updateAppearance = true;
        }
        else
        {
            var equipInfo = DataManager.ArmorInfo[item.Id];
            if (equipInfo.EquipPosition.HasFlag(EquipPosition.Headgear))
            {
                if (equipInfo.HeadPosition.HasFlag(HeadgearPosition.Top)) UnEquipItem(EquipSlot.HeadTop);
                if (equipInfo.HeadPosition.HasFlag(HeadgearPosition.Mid)) UnEquipItem(EquipSlot.HeadMid);
                if (equipInfo.HeadPosition.HasFlag(HeadgearPosition.Bottom)) UnEquipItem(EquipSlot.HeadBottom);
                updateAppearance = true;
            }

            if (equipInfo.EquipPosition.HasFlag(EquipPosition.Shield))
            {
                UnEquipItem(EquipSlot.Shield);
                updateAppearance = true;
            }
            if (equipInfo.EquipPosition.HasFlag(EquipPosition.Armor)) UnEquipItem(EquipSlot.Body);
            if (equipInfo.EquipPosition.HasFlag(EquipPosition.Garment)) UnEquipItem(EquipSlot.Garment);
            if (equipInfo.EquipPosition.HasFlag(EquipPosition.Boots)) UnEquipItem(EquipSlot.Footgear);
            if (equipInfo.EquipPosition.HasFlag(EquipPosition.Accessory))
            {
                if (ItemSlots[(int)EquipSlot.Accessory1] == bagId) UnEquipItem(EquipSlot.Accessory1);
                if (ItemSlots[(int)EquipSlot.Accessory2] == bagId) UnEquipItem(EquipSlot.Accessory2);
            }
        }

        if (Player.CombatEntity.TryGetStatusContainer(out var status))
            status.OnChangeEquipment();
        if (updateAppearance)
            CommandBuilder.UpdatePlayerAppearanceAuto(Player);
        Player.UpdateStats();
    }

    private void UnEquipItem(EquipSlot slot)
    {
        var bagId = ItemSlots[(int)slot];
        if (bagId == 0)
            return;

        UnEquipEvent(slot);
        ItemSlots[(int)slot] = 0;
        ItemIds[(int)slot] = 0;
        CommandBuilder.PlayerEquipItem(Player, bagId, slot, false);
        if (slot == EquipSlot.Weapon)
            isTwoHandedWeapon = false;
        if (slot == EquipSlot.Shield)
            IsDualWielding = false; //probably not but may as well
        if (slot == EquipSlot.HeadTop || slot == EquipSlot.HeadMid || slot == EquipSlot.HeadBottom)
            headgearMultiSlotInfo[(int)slot] = HeadgearPosition.None;
    }

    private EquipSlot EquipSlotForWeapon(WeaponInfo weapon)
    {
        //if an assassin is using a one-handed weapon and they are currently equipped with one weapon and no shield, place in shield slot
        if (Player.Character.ClassId == 11 && !weapon.IsTwoHanded && ItemSlots[(int)EquipSlot.Weapon] > 0 &&
            ItemSlots[(int)EquipSlot.Shield] == 0)
            return EquipSlot.Shield;
        return EquipSlot.Weapon;
    }

    private EquipSlot EquipSlotForEquipment(ArmorInfo info, int itemId)
    {
        if ((info.EquipPosition & EquipPosition.Headgear) != 0)
        {
            if (info.HeadPosition.HasFlag(HeadgearPosition.Top)) return EquipSlot.HeadTop;
            if (info.HeadPosition.HasFlag(HeadgearPosition.Mid)) return EquipSlot.HeadMid;
            if (info.HeadPosition.HasFlag(HeadgearPosition.Bottom)) return EquipSlot.HeadBottom;
        }
        if (info.EquipPosition.HasFlag(EquipPosition.Body)) return EquipSlot.Body;
        if (info.EquipPosition.HasFlag(EquipPosition.Garment)) return EquipSlot.Garment;
        if (info.EquipPosition.HasFlag(EquipPosition.Shield)) return EquipSlot.Shield;
        if (info.EquipPosition.HasFlag(EquipPosition.Boots)) return EquipSlot.Footgear;
        if (info.EquipPosition.HasFlag(EquipPosition.Accessory))
            if (ItemSlots[(int)EquipSlot.Accessory1] > 0 && ItemSlots[(int)EquipSlot.Accessory2] <= 0)
                return EquipSlot.Accessory2;
            else
                return EquipSlot.Accessory1;

        throw new Exception($"Invalid equipment position for item {itemId}!");
    }

    private bool IsValidSlotForEquipment(ArmorInfo info, EquipSlot slot)
    {
        return slot switch
        {
            EquipSlot.HeadTop => (info.EquipPosition & EquipPosition.Headgear) > 0 && info.HeadPosition.HasFlag(HeadSlots.Top),
            EquipSlot.HeadMid => (info.EquipPosition & EquipPosition.Headgear) > 0 && info.HeadPosition.HasFlag(HeadSlots.Mid),
            EquipSlot.HeadBottom => (info.EquipPosition & EquipPosition.Headgear) > 0 && info.HeadPosition.HasFlag(HeadSlots.Bottom),
            EquipSlot.Weapon => false,
            EquipSlot.Shield => info.EquipPosition.HasFlag(EquipPosition.Shield),
            EquipSlot.Body => info.EquipPosition.HasFlag(EquipPosition.Armor),
            EquipSlot.Garment => info.EquipPosition.HasFlag(EquipPosition.Garment),
            EquipSlot.Footgear => info.EquipPosition.HasFlag(EquipPosition.Footgear),
            EquipSlot.Accessory1 => info.EquipPosition.HasFlag(EquipPosition.Accessory),
            EquipSlot.Accessory2 => info.EquipPosition.HasFlag(EquipPosition.Accessory),
            _ => false
        };
    }

    private EquipChangeResult EquipWeapon(int bagId, EquipSlot equipSlot, ItemInfo itemData)
    {
        var weaponInfo = DataManager.WeaponInfo[itemData.Id];

        if (equipSlot == EquipSlot.None)
            equipSlot = EquipSlotForWeapon(weaponInfo);

        if (equipSlot != EquipSlot.Weapon && equipSlot != EquipSlot.Shield)
            return EquipChangeResult.InvalidItem;

        if (Player.GetStat(CharacterStat.Level) < weaponInfo.MinLvl)
            return EquipChangeResult.LevelTooLow;

        if (!DataManager.IsJobInEquipGroup(weaponInfo.EquipGroup, Player.Character.ClassId))
            return EquipChangeResult.NotApplicableJob;

        if (equipSlot == EquipSlot.Shield && Player.Character.ClassId != 11 && !weaponInfo.IsTwoHanded)
            return EquipChangeResult.InvalidItem;

        //make sure they don't equip the same weapon in both hands
        if ((equipSlot == EquipSlot.Shield && ItemSlots[(int)EquipSlot.Weapon] == bagId)
            || (equipSlot == EquipSlot.Weapon && ItemSlots[(int)EquipSlot.Shield] == bagId))
            return EquipChangeResult.InvalidItem;
        
        if (weaponInfo.IsTwoHanded)
            UnEquipItem(EquipSlot.Shield);

        UnEquipItem(equipSlot);

        IsDualWielding = equipSlot == EquipSlot.Shield;
        isTwoHandedWeapon = weaponInfo.IsTwoHanded;

        ItemSlots[(int)equipSlot] = bagId;
        ItemIds[(int)equipSlot] = itemData.Id;

        OnEquipEvent(equipSlot);
        CommandBuilder.UpdatePlayerAppearanceAuto(Player);

        if (Player.CombatEntity.TryGetStatusContainer(out var status))
            status.OnChangeEquipment();

        CommandBuilder.PlayerEquipItem(Player, bagId, equipSlot, true);

        return EquipChangeResult.Success;
    }

    public EquipChangeResult EquipArmorOrAccessory(int bagId, EquipSlot equipSlot, ItemInfo itemData)
    {
        var equipInfo = DataManager.ArmorInfo[itemData.Id];
        if (Player.GetStat(CharacterStat.Level) < equipInfo.MinLvl)
            return EquipChangeResult.LevelTooLow;

        if (!DataManager.IsJobInEquipGroup(equipInfo.EquipGroup, Player.Character.ClassId))
            return EquipChangeResult.NotApplicableJob;


        if (equipSlot == EquipSlot.None || equipInfo.EquipPosition == EquipPosition.Headgear)
            equipSlot = EquipSlotForEquipment(equipInfo, itemData.Id);
        else if (!IsValidSlotForEquipment(equipInfo, equipSlot))
            return EquipChangeResult.InvalidPosition;

        //make sure they don't equip the same accessory in both slots
        if (equipSlot == EquipSlot.Accessory1)
        {
            if (ItemSlots[(int)EquipSlot.Accessory2] == bagId)
                return EquipChangeResult.InvalidItem;
        }
        else if (equipSlot == EquipSlot.Accessory2)
        {
            if (ItemSlots[(int)EquipSlot.Accessory1] == bagId)
                return EquipChangeResult.InvalidItem;
        }

        if (equipInfo.EquipPosition == EquipPosition.Shield && isTwoHandedWeapon)
            UnEquipItem(EquipSlot.Weapon);

        UnEquipItem(equipSlot);

        if (equipInfo.EquipPosition == EquipPosition.Headgear)
        {
            for (var i = 0; i < 3; i++)
            {
                //if any of the other headgear block this slot, we'll need to unequip them as well
                if ((equipInfo.HeadPosition & headgearMultiSlotInfo[i]) > 0)
                    UnEquipItem((EquipSlot)i);
            }

            headgearMultiSlotInfo[(int)equipSlot] = equipInfo.HeadPosition;
        }

        ItemSlots[(int)equipSlot] = bagId;
        ItemIds[(int)equipSlot] = itemData.Id;

        OnEquipEvent(equipSlot);
        CommandBuilder.UpdatePlayerAppearanceAuto(Player);

        if (Player.CombatEntity.TryGetStatusContainer(out var status))
            status.OnChangeEquipment();

        CommandBuilder.PlayerEquipItem(Player, bagId, equipSlot, true);

        return EquipChangeResult.Success;
    }

    public EquipChangeResult EquipItem(int bagId, EquipSlot equipSlot = EquipSlot.None)
    {
        if (Player.Inventory == null || !Player.Inventory.UniqueItems.TryGetValue(bagId, out var item))
            return EquipChangeResult.InvalidItem;

        var itemData = DataManager.ItemList[item.Id];

        if (itemData.ItemClass == ItemClass.Weapon)
            return EquipWeapon(bagId, equipSlot, itemData);

        if (itemData.ItemClass == ItemClass.Equipment)
            return EquipArmorOrAccessory(bagId, equipSlot, itemData);

        return EquipChangeResult.InvalidItem;
    }

    private void OnEquipEvent(EquipSlot slot)
    {
        Debug.Assert(Player.Inventory != null);

        if (slot == EquipSlot.None || ItemSlots[(int)slot] <= 0)
            return;

        activeSlot = slot;
        var bagId = ItemSlots[(int)slot];
        var item = Player.Inventory.UniqueItems[bagId];
        if (!DataManager.ItemList.TryGetValue(item.Id, out var data))
        {
            ServerLogger.LogWarning($"Player {Player.Character.Name} has an itemId {item} equipped but we don't have such an item in our item database.");
            return;
        }

        if (data.ItemClass == ItemClass.Weapon)
        {
            if (!DataManager.WeaponInfo.TryGetValue(item.Id, out var weapon))
                Player.WeaponClass = 0;
            else
            {
                Player.WeaponClass = weapon.WeaponClass;
                Player.SetStat(CharacterStat.Attack, weapon.Attack * 90 / 100);
                Player.SetStat(CharacterStat.Attack2, weapon.Attack * 110 / 100);
                Player.RefreshWeaponMastery();
                WeaponElement = weapon.Element;
            }
        }

        if (data.ItemClass == ItemClass.Equipment)
        {
            if (DataManager.ArmorInfo.TryGetValue(item.Id, out var armor))
            {
                Player.AddStat(CharacterStat.Def, armor.Defense);
                Player.AddStat(CharacterStat.MDef, armor.MagicDefense);
            }
        }

        data.Interaction?.OnEquip(Player, Player.CombatEntity, this, item, slot);
        for (var j = 0; j < 4; j++)
        {
            unsafe //all this trouble to ensure all 4 slots are always allocated in sequence in the struct
            {
                var slotItem = item.Data[j];
                if (slotItem <= 0)
                    continue;
                if (!DataManager.ItemList.TryGetValue(slotItem, out var slotData))
                    throw new Exception($"Attempting to run RunAllOnEquip event for item {slotItem} (socketed in a {item.Id}), but it doesn't appear to exist in the item database."); ;

                slotData.Interaction?.OnEquip(Player, Player.CombatEntity, this, default, slot);
            }
        }
    }

    private void UnEquipEvent(EquipSlot slot)
    {
        Debug.Assert(Player.Inventory != null);

        if (ItemSlots[(int)slot] <= 0)
            return;

        activeSlot = slot;
        var bagId = ItemSlots[(int)slot];
        var item = Player.Inventory.UniqueItems[bagId];
        if (!DataManager.ItemList.TryGetValue(item.Id, out var data))
            throw new Exception($"Attempting to run RunAllOnEquip event for item {item.Id}, but it doesn't appear to exist in the item database.");

        if (data.ItemClass == ItemClass.Weapon)
        {
            Player.WeaponClass = 0;
            Player.SetStat(CharacterStat.Attack, 0);
            Player.SetStat(CharacterStat.Attack2, 0);
            WeaponElement = AttackElement.Neutral;
            Player.RefreshWeaponMastery();
        }
        
        if (data.ItemClass == ItemClass.Equipment)
        {
            if (DataManager.ArmorInfo.TryGetValue(item.Id, out var armor))
            {
                Player.SubStat(CharacterStat.Def, armor.Defense);
                Player.SubStat(CharacterStat.MDef, armor.MagicDefense);
            }
        }

        data.Interaction?.OnUnequip(Player, Player.CombatEntity, this, item, slot);
        for (var j = 0; j < 4; j++)
        {
            unsafe //all this trouble to ensure all 4 slots are always allocated in sequence in the struct
            {
                var slotItem = item.Data[j];
                if (slotItem <= 0)
                    continue;

                if (!DataManager.ItemList.TryGetValue(slotItem, out var slotData))
                    throw new Exception($"Attempting to run RunAllOnEquip event for item {item.Id} (socketed in a {item.Id}), but it doesn't appear to exist in the item database."); ;

                slotData.Interaction?.OnUnequip(Player, Player.CombatEntity, this, default, slot);
            }
        }

        //remove saved item effects from the player
        for (var i = 0; i < equipmentEffects.Count; i++)
        {
            var effect = equipmentEffects[i];
            if (effect.Slot == slot)
            {
                Player.CombatEntity.SubStat(effect.Stat, effect.Change);
                equipmentEffects.Remove(i);
                i--; //we've moved the last element into our current position, so we step the enumerator back by 1
            }
        }
    }

    public void RunAllOnEquip()
    {
        if (Player.Inventory == null)
        {
#if DEBUG
            for (var i = 0; i < 10; i++)
                if (ItemSlots[i] > 0)
                    throw new Exception($"Player inventory is empty, but we still have items in our equip state!");
#endif
            return;
        }

        for (var i = 0; i < 10; i++)
            OnEquipEvent((EquipSlot)i);
    }

    public void AddStat(CharacterStat stat, int change)
    {
#if DEBUG
        if (stat >= CharacterStat.Str && stat <= CharacterStat.Luk)
            ServerLogger.LogWarning($"Warning! Adding directly to a base stat {stat} in equip handler for {Player.Inventory?.UniqueItems[(int)activeSlot]}! You probably want AddStat.");
#endif
        var equipState = new EquipStatChange()
        {
            Slot = activeSlot,
            Change = change,
            Stat = stat
        };
        equipmentEffects.Add(ref equipState);
        Player.CombatEntity.AddStat(stat, change);
    }

    public void SubStat(CharacterStat stat, int change) => AddStat(stat, -change); //lol

    public void AddStatusEffect(CharacterStatusEffect statusEffect, int duration, int val1 = 0, int val2 = 0)
    {
        var status = StatusEffectState.NewStatusEffect(statusEffect, duration, val1, val2);
        Player.CombatEntity.AddStatusEffect(status);
    }
    
    public void Serialize(IBinaryMessageWriter bw)
    {
        if (Player.Inventory == null)
            return;

        foreach (var itemId in ItemSlots)
        {
            bw.Write(itemId > 0);
            if (itemId > 0)
                bw.Write(Player.Inventory.GetGuidByUniqueItemId(itemId).ToByteArray()); //we have a bag id, we want to store the guid
        }
        bw.Write(AmmoType);
    }

    public void DeSerialize(IBinaryMessageReader br, CharacterBag bag)
    {
        for (var i = 0; i < 10; i++)
        {
            if (br.ReadBoolean())
            {
                var guid = new Guid(br.ReadBytes(16)); //we have a guid, we want to store a bag id
                var bagId = bag.GetUniqueItemByGuid(guid, out var item);

                if (bagId > 0)
                {
                    ItemSlots[i] = bagId;
                    ItemIds[i] = item.Id;
                }

                if (i < 3)
                {
                    var equipInfo = DataManager.ArmorInfo[item.Id];
                    headgearMultiSlotInfo[i] = equipInfo.HeadPosition;
                }
            }
        }

        AmmoType = br.ReadInt32();
    }
}