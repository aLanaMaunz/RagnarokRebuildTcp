﻿namespace RoRebuildServer.EntityComponents.Character;

public enum PlayerStat
{
    Level,
    Status,
    Gender,
    Head,
    Job,
    Exp,
    Experience = Exp,
    JobExp,
    JobExperience = JobExp,
    Str,
    Strength = Str,
    Dex,
    Dexterity = Dex,
    Agi,
    Agility = Agi,
    Int,
    Intelligence = Int,
    Vit,
    Vitality = Vit,
    Luk,
    Luck = Luk,
    Facing,
    Hp,
    Mp,
    SkillPoints,
    StatPoints,
    PlayerStatsMax
}

public enum CharacterStat
{
    Level,
    Hp,
    MaxHp,
    Sp,
    MaxSp,
    Range,
    Attack,
    Attack2,
    MagicAtkMin,
    MagicAtkMax,
    Str,
    Strength = Str,
    Dex,
    Dexterity = Dex,
    Agi,
    Agility = Agi,
    Int,
    Intelligence = Int,
    Vit,
    Vitality = Vit,
    Luk,
    Luck = Luk,
    Def,
    Defense = Def,
    MDef,
    MagicDefense = MDef,
    AspdBonus,
    MoveSpeedBonus,
    AddStr,
    AddAgi,
    AddDex,
    AddInt,
    AddVit,
    AddLuk,
    AddDef,
    AddMDef,
    AddFlee,
    AddHit,
    MonsterStatsMax, //any stats after this are stats only players will be able to hold.

    WeaponMastery,
    DoubleAttackChance,
    PercentVsDemon,
    PercentVsUndead,
    ReductionFromDemon,
    ReductionFromUndead,
    CharacterStatsMax
}

public enum TimingStat
{
    MoveSpeed,
    AttackMotionTime,
    AttackDelayTime,
    HitDelayTime,
    SpriteAttackTiming,
    TimingStatsMax
}