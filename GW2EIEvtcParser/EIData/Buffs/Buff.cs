﻿using System;
using System.Collections.Generic;
using System.IO;
using GW2EIEvtcParser.Interfaces;
using GW2EIEvtcParser.ParsedData;
using static GW2EIEvtcParser.ArcDPSEnums;
using static GW2EIEvtcParser.ParserHelper;

namespace GW2EIEvtcParser.EIData
{
    public class Buff : IVersionable
    {


        public const long NumberOfConditionsID = -3;
        public const long NumberOfBoonsID = -2;
        public const long NumberOfActiveCombatMinions = -17;
        public const long NoBuff = -4;

        // Weaver attunements
        public const long FireWater = -5;
        public const long FireAir = -6;
        public const long FireEarth = -7;
        public const long WaterFire = -8;
        public const long WaterAir = -9;
        public const long WaterEarth = -10;
        public const long AirFire = -11;
        public const long AirWater = -12;
        public const long AirEarth = -13;
        public const long EarthFire = -14;
        public const long EarthWater = -15;
        public const long EarthAir = -16;

        public const long FireDual = 43470;
        public const long WaterDual = 41166;
        public const long AirDual = 42264;
        public const long EarthDual = 44857;

        // Boon
        public enum BuffNature
        {
            Condition,
            Boon,
            OffensiveBuffTable,
            DefensiveBuffTable,
            SupportBuffTable,
            GearBuffTable,
            GraphOnlyBuff,
            Consumable,
            Unknown
        };

        public enum BuffType
        {
            Duration = 0,
            Intensity = 1,
            Unknown = 2,
        }

        // Fields
        public string Name { get; }
        public long ID { get; }
        public BuffNature Nature { get; }
        public ParserHelper.Source Source { get; }
        public BuffStackType StackType { get; }
        public BuffType Type
        {
            get
            {
                switch (StackType)
                {
                    case BuffStackType.Queue:
                    case BuffStackType.Regeneration:
                    case BuffStackType.CappedDuration:
                    case BuffStackType.Force:
                        return BuffType.Duration;
                    case BuffStackType.Stacking:
                    case BuffStackType.StackingConditionalLoss:
                        return BuffType.Intensity;
                    default:
                        return BuffType.Unknown;
                }
            }
        }

        private ulong _maxBuild { get; } = ulong.MaxValue;
        private ulong _minBuild { get; } = ulong.MinValue;
        public int Capacity { get; }
        public string Link { get; }

        /// <summary>
        /// Buff constructor
        /// </summary>
        /// <param name="name">The name of the boon</param>
        /// <param name="id">The id of the buff</param>
        /// <param name="source">Source of the buff <see cref="ParserHelper.Source"/></param>
        /// <param name="type">Stack Type of the buff<see cref="BuffStackType"/></param>
        /// <param name="capacity">Maximun amount of buff in stack</param>
        /// <param name="nature">Nature of the buff, dictates in which category the buff will appear <see cref="BuffNature"/></param>
        /// <param name="link">URL to the icon of the buff</param>
        internal Buff(string name, long id, ParserHelper.Source source, BuffStackType type, int capacity, BuffNature nature, string link)
        {
            Name = name;
            ID = id;
            Source = source;
            StackType = type;
            Capacity = capacity;
            Nature = nature;
            Link = link;
        }

        internal Buff(string name, long id, ParserHelper.Source source, BuffNature nature, string link) : this(name, id, source, BuffStackType.Force, 1, nature, link)
        {
        }

        internal Buff(string name, long id, ParserHelper.Source source, BuffStackType type, int capacity, BuffNature nature, string link, ulong minBuild, ulong maxBuild) : this(name, id, source, type, capacity, nature, link)
        {
            _maxBuild = maxBuild;
            _minBuild = minBuild;
        }

        internal Buff(string name, long id, ParserHelper.Source source, BuffNature nature, string link, ulong minBuild, ulong maxBuild) : this(name, id, source, BuffStackType.Force, 1, nature, link, minBuild, maxBuild)
        {
        }

        public Buff(string name, long id, string link)
        {
            Name = name;
            ID = id;
            Source = Source.Unknown;
            StackType = BuffStackType.Unknown;
            Capacity = 1;
            Nature = BuffNature.Unknown;
            Link = link;
        }

        internal static Buff CreateCustomConsumable(string name, long id, string link, int capacity)
        {
            return new Buff(name + " " + id, id, Source.Item, capacity > 1 ? BuffStackType.Stacking : BuffStackType.Force, capacity, BuffNature.Consumable, link);
        }

        internal void VerifyBuffInfoEvent(BuffInfoEvent buffInfoEvent, ParserController operation)
        {
            if (buffInfoEvent.BuffID != ID)
            {
                return;
            }
            if (Capacity != buffInfoEvent.MaxStacks)
            {
                operation.UpdateProgressWithCancellationCheck("Adjusted capacity for " + Name + " from " + Capacity + " to " + buffInfoEvent.MaxStacks);
            }
            /*if (buffInfoEvent.StackingType != StackType)
            {
                //_stackType = buffInfoEvent.StackingType; // might be unreliable due to its absence on some logs
                operation.UpdateProgressWithCancellationCheck("Incoherent stack type for " + Name + ": is " + StackType + " but expected " + buffInfoEvent.StackingType);
            }*/
        }
        internal AbstractBuffSimulator CreateSimulator(ParsedEvtcLog log, bool forceNoId)
        {
            BuffInfoEvent buffInfoEvent = log.CombatData.GetBuffInfoEvent(ID);
            int capacity = Capacity;
            if (buffInfoEvent != null && buffInfoEvent.MaxStacks != capacity)
            {
                capacity = buffInfoEvent.MaxStacks;
            }
            if (!log.CombatData.HasStackIDs || forceNoId)
            {

                switch (Type)
                {
                    case BuffType.Intensity: 
                        return new BuffSimulatorIntensity(log, this, capacity);
                    case BuffType.Duration: 
                        return new BuffSimulatorDuration(log, this, capacity);
                    case BuffType.Unknown:
                    default:
                        throw new InvalidDataException("Buffs can not be stackless");
                }
            }
            switch (Type)
            {
                case BuffType.Intensity:
                    return new BuffSimulatorIDIntensity(log, this, capacity);
                case BuffType.Duration:
                    return new BuffSimulatorIDDuration(log, this);
                case BuffType.Unknown:
                default:
                    throw new InvalidDataException("Buffs can not be stackless");
            }
        }

        internal static BuffSourceFinder GetBuffSourceFinder(ulong version, HashSet<long> boonIds)
        {
            if (version > 115189)
            {
                return new BuffSourceFinder20210511(boonIds);
            }
            if (version > 99526)
            {
                return new BuffSourceFinder20191001(boonIds);
            }
            if (version > 95112)
            {
                return new BuffSourceFinder20190305(boonIds);
            }
            return new BuffSourceFinder20181211(boonIds);
        }

        public bool Available(ulong gw2Build)
        {
            return gw2Build < _maxBuild && gw2Build >= _minBuild;
        }


        internal static readonly List<Buff> Boons = new List<Buff>
        {
                new Buff("Might", 740, Source.Common, BuffStackType.Stacking, 25, BuffNature.Boon, "https://wiki.guildwars2.com/images/7/7c/Might.png"),
                new Buff("Fury", 725, Source.Common, BuffStackType.Queue, 9, BuffNature.Boon, "https://wiki.guildwars2.com/images/4/46/Fury.png"/*, 0, 115190*/),
                new Buff("Quickness", 1187, Source.Common, BuffStackType.Queue, 5, BuffNature.Boon, "https://wiki.guildwars2.com/images/b/b4/Quickness.png"/*, 0, 115190*/),
                new Buff("Alacrity", 30328, Source.Common, BuffStackType.Queue, 9, BuffNature.Boon, "https://wiki.guildwars2.com/images/4/4c/Alacrity.png"/*, 0, 115190*/),
                new Buff("Protection", 717, Source.Common, BuffStackType.Queue, 5, BuffNature.Boon, "https://wiki.guildwars2.com/images/6/6c/Protection.png"/*, 0 , 115190*/),
                new Buff("Regeneration", 718, Source.Common, BuffStackType.Regeneration, 5, BuffNature.Boon, "https://wiki.guildwars2.com/images/5/53/Regeneration.png"),
                new Buff("Vigor", 726, Source.Common, BuffStackType.Queue, 5, BuffNature.Boon, "https://wiki.guildwars2.com/images/f/f4/Vigor.png"/*, 0, 115190*/),
                new Buff("Aegis", 743, Source.Common, BuffStackType.Queue, 9, BuffNature.Boon, "https://wiki.guildwars2.com/images/e/e5/Aegis.png"),
                new Buff("Stability", 1122, Source.Common, BuffStackType.StackingConditionalLoss, 25, BuffNature.Boon, "https://wiki.guildwars2.com/images/a/ae/Stability.png"),
                new Buff("Swiftness", 719, Source.Common, BuffStackType.Queue, 9, BuffNature.Boon, "https://wiki.guildwars2.com/images/a/af/Swiftness.png"/*, 0, 115190*/),
                new Buff("Retaliation", 873, Source.Common, BuffStackType.Queue, 5, BuffNature.Boon, "https://wiki.guildwars2.com/images/5/53/Retaliation.png", 0, 115190),
                new Buff("Resistance", 26980, Source.Common, BuffStackType.Queue, 5, BuffNature.Boon, "https://wiki.guildwars2.com/images/4/4b/Resistance.png"/*, 0, 115190*/),
                //
                /*new Buff("Fury", 725, Source.Common, BuffStackType.CappedDuration, 0, BuffNature.Boon, "https://wiki.guildwars2.com/images/4/46/Fury.png", 115190, ulong.MaxValue),
                new Buff("Quickness", 1187, Source.Common, BuffStackType.CappedDuration, 0, BuffNature.Boon, "https://wiki.guildwars2.com/images/b/b4/Quickness.png", 115190, ulong.MaxValue),
                new Buff("Alacrity", 30328, Source.Common, BuffStackType.CappedDuration, 0, BuffNature.Boon, "https://wiki.guildwars2.com/images/4/4c/Alacrity.png", 115190, ulong.MaxValue),
                new Buff("Protection", 717, Source.Common, BuffStackType.CappedDuration, 0, BuffNature.Boon, "https://wiki.guildwars2.com/images/6/6c/Protection.png", 115190, ulong.MaxValue),
                new Buff("Vigor", 726, Source.Common, BuffStackType.CappedDuration, 0, BuffNature.Boon, "https://wiki.guildwars2.com/images/f/f4/Vigor.png", 115190, ulong.MaxValue),
                new Buff("Swiftness", 719, Source.Common, BuffStackType.CappedDuration, 0, BuffNature.Boon, "https://wiki.guildwars2.com/images/a/af/Swiftness.png", 115190, ulong.MaxValue),
                new Buff("Resolution", 873, Source.Common, BuffStackType.CappedDuration, 0, BuffNature.Boon, "https://wiki.guildwars2.com/images/0/06/Resolution.png", 115190, ulong.MaxValue),
                new Buff("Resistance", 26980, Source.Common, BuffStackType.CappedDuration, 0, BuffNature.Boon, "https://wiki.guildwars2.com/images/4/4b/Resistance.png", 115190, ulong.MaxValue),*/
                new Buff("Resolution", 873, Source.Common, BuffStackType.Queue, 5, BuffNature.Boon, "https://wiki.guildwars2.com/images/0/06/Resolution.png", 115190, ulong.MaxValue),
                //
                new Buff("Number of Boons", NumberOfBoonsID, Source.Common, BuffStackType.Stacking, 0, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/4/44/Boon_Duration.png"),
        };

        internal static readonly List<Buff> Conditions = new List<Buff>
        {
                new Buff("Bleeding", 736, Source.Common, BuffStackType.Stacking, 1500, BuffNature.Condition, "https://wiki.guildwars2.com/images/3/33/Bleeding.png"),
                new Buff("Burning", 737, Source.Common, BuffStackType.Stacking, 1500, BuffNature.Condition, "https://wiki.guildwars2.com/images/4/45/Burning.png"),
                new Buff("Confusion", 861, Source.Common, BuffStackType.Stacking, 1500, BuffNature.Condition, "https://wiki.guildwars2.com/images/e/e6/Confusion.png"),
                new Buff("Poison", 723, Source.Common, BuffStackType.Stacking, 1500, BuffNature.Condition, "https://wiki.guildwars2.com/images/1/11/Poisoned.png"),
                new Buff("Torment", 19426, Source.Common, BuffStackType.Stacking, 1500, BuffNature.Condition, "https://wiki.guildwars2.com/images/0/08/Torment.png"),
                new Buff("Blind", 720, Source.Common, BuffStackType.Queue, 9, BuffNature.Condition, "https://wiki.guildwars2.com/images/3/33/Blinded.png"/*, 0, 115190*/),
                new Buff("Chilled", 722, Source.Common, BuffStackType.Queue, 5, BuffNature.Condition, "https://wiki.guildwars2.com/images/a/a6/Chilled.png"/*, 0, 115190*/),
                new Buff("Crippled", 721, Source.Common, BuffStackType.Queue, 5, BuffNature.Condition, "https://wiki.guildwars2.com/images/f/fb/Crippled.png"/*, 0, 115190*/),
                new Buff("Fear", 791, Source.Common, BuffStackType.Queue, 5, BuffNature.Condition, "https://wiki.guildwars2.com/images/e/e6/Fear.png"),
                new Buff("Immobile", 727, Source.Common, BuffStackType.Queue, 3, BuffNature.Condition, "https://wiki.guildwars2.com/images/3/32/Immobile.png"/*, 0, 115190*/),
                new Buff("Slow", 26766, Source.Common, BuffStackType.Queue, 9, BuffNature.Condition, "https://wiki.guildwars2.com/images/f/f5/Slow.png"/*, 0, 115190*/),
                new Buff("Weakness", 742, Source.Common, BuffStackType.Queue, 5, BuffNature.Condition, "https://wiki.guildwars2.com/images/f/f9/Weakness.png"/*, 0, 115190*/),
                new Buff("Taunt", 27705, Source.Common, BuffStackType.Queue, 5, BuffNature.Condition, "https://wiki.guildwars2.com/images/c/cc/Taunt.png"),
                new Buff("Vulnerability", 738, Source.Common, BuffStackType.Stacking, 25, BuffNature.Condition, "https://wiki.guildwars2.com/images/a/af/Vulnerability.png"),
                //          
               /* new Buff("Blind", 720, Source.Common, BuffStackType.CappedDuration, 0, BuffNature.Condition, "https://wiki.guildwars2.com/images/3/33/Blinded.png", 115190, ulong.MaxValue),
                new Buff("Chilled", 722, Source.Common, BuffStackType.CappedDuration, 0, BuffNature.Condition, "https://wiki.guildwars2.com/images/a/a6/Chilled.png", 115190, ulong.MaxValue),
                new Buff("Crippled", 721, Source.Common, BuffStackType.CappedDuration, 0, BuffNature.Condition, "https://wiki.guildwars2.com/images/f/fb/Crippled.png", 115190, ulong.MaxValue),
                new Buff("Immobile", 727, Source.Common, BuffStackType.CappedDuration, 0, BuffNature.Condition, "https://wiki.guildwars2.com/images/3/32/Immobile.png", 115190, ulong.MaxValue),
                new Buff("Slow", 26766, Source.Common, BuffStackType.CappedDuration, 0, BuffNature.Condition, "https://wiki.guildwars2.com/images/f/f5/Slow.png", 115190, ulong.MaxValue),
                new Buff("Weakness", 742, Source.Common, BuffStackType.CappedDuration, 0, BuffNature.Condition, "https://wiki.guildwars2.com/images/f/f9/Weakness.png", 115190, ulong.MaxValue),*/
                //
                new Buff("Number of Conditions", NumberOfConditionsID, Source.Common, BuffStackType.Stacking, 0, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/3/38/Condition_Duration.png"),
        };

        internal static readonly List<Buff> Commons = new List<Buff>
        {
                new Buff("Number of Active Combat Minions", NumberOfActiveCombatMinions, Source.Common, BuffStackType.Stacking, 0, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/a/ad/Superior_Rune_of_the_Ranger.png"),
                new Buff("Downed", 770, Source.Common, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/d/dd/Downed.png"),
                new Buff("Exhaustion", 46842, Source.Common, BuffStackType.Queue, 3, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/8/88/Exhaustion.png"),
                new Buff("Stealth", 13017, Source.Common, BuffStackType.Queue, 5, BuffNature.SupportBuffTable, "https://wiki.guildwars2.com/images/1/19/Stealth.png"),
                new Buff("Hide in Shadows", 10269, Source.Common, BuffStackType.Queue, 25, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/1/19/Stealth.png"),
                new Buff("Revealed", 890, Source.Common, BuffNature.SupportBuffTable, "https://wiki.guildwars2.com/images/d/db/Revealed.png"),
                new Buff("Superspeed", 5974, Source.Common, BuffNature.SupportBuffTable,"https://wiki.guildwars2.com/images/1/1a/Super_Speed.png", 0, 116210),
                new Buff("Superspeed", 5974, Source.Common, BuffStackType.Queue, 9, BuffNature.SupportBuffTable,"https://wiki.guildwars2.com/images/1/1a/Super_Speed.png", 116210, ulong.MaxValue),
                new Buff("Determined (762)", 762, Source.Common, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/e/eb/Determined.png"),
                new Buff("Determined (788)", 788, Source.Common, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/e/eb/Determined.png"),
                new Buff("Determined (895)", 895, Source.Common, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/e/eb/Determined.png"),
                new Buff("Determined (3892)", 3892, Source.Common, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/e/eb/Determined.png"),
                new Buff("Determined (31450)", 31450, Source.Common, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/e/eb/Determined.png"),
                new Buff("Determined (52271)", 52271, Source.Common, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/e/eb/Determined.png"),
                new Buff("Invulnerability (757)", 757, Source.Common, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/e/eb/Determined.png"),
                new Buff("Invulnerability (801)", 801, Source.Common, BuffStackType.Queue, 25, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/e/eb/Determined.png"),
                new Buff("Spawn Protection?", 34113, Source.Common, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/e/eb/Determined.png"),
                new Buff("Stun", 872, Source.Common, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/9/97/Stun.png"),
                new Buff("Daze", 833, Source.Common, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/7/79/Daze.png"),
                new Buff("Exposed (48209)", 48209, Source.Common, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/6/6b/Exposed.png"),
                new Buff("Exposed (31589)", 31589, Source.Common, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/6/6b/Exposed.png"),
                new Buff("Unblockable",36781, Source.Common, BuffStackType.StackingConditionalLoss, 25, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/f/f0/Unblockable_%28effect%29.png",102321 , ulong.MaxValue),
                //Auras
                new Buff("Chaos Aura", 10332, Source.Common, BuffNature.SupportBuffTable,"https://wiki.guildwars2.com/images/e/ec/Chaos_Aura.png"),
                new Buff("Fire Aura", 5677, Source.Common, BuffNature.SupportBuffTable,"https://wiki.guildwars2.com/images/c/ce/Fire_Aura.png"),
                new Buff("Frost Aura", 5579, Source.Common, BuffNature.SupportBuffTable,"https://wiki.guildwars2.com/images/8/87/Frost_Aura_%28effect%29.png"),
                new Buff("Light Aura", 25518, Source.Common, BuffNature.SupportBuffTable,"https://wiki.guildwars2.com/images/5/5a/Light_Aura.png"),
                new Buff("Magnetic Aura", 5684, Source.Common, BuffNature.SupportBuffTable, "https://wiki.guildwars2.com/images/0/0b/Magnetic_Aura_%28effect%29.png"),
                new Buff("Shocking Aura", 5577, Source.Common, BuffNature.SupportBuffTable,"https://wiki.guildwars2.com/images/5/5d/Shocking_Aura_%28effect%29.png"),
                new Buff("Dark Aura", 39978, Source.Common, BuffNature.SupportBuffTable,"https://wiki.guildwars2.com/images/e/ef/Dark_Aura.png", 96406, ulong.MaxValue),
                //race
                new Buff("Take Root", 12459, Source.Common, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/b/b2/Take_Root.png"),
                new Buff("Become the Bear",12426, Source.Common, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/7/7e/Become_the_Bear.png"),
                new Buff("Become the Raven",12405, Source.Common, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/2/2c/Become_the_Raven.png"),
                new Buff("Become the Snow Leopard",12400, Source.Common, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/7/78/Become_the_Snow_Leopard.png"),
                new Buff("Become the Wolf",12393, Source.Common, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/f/f1/Become_the_Wolf.png"),
                new Buff("Avatar of Melandru", 12368, Source.Common, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/3/30/Avatar_of_Melandru.png"),
                new Buff("Power Suit",12326, Source.Common, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/8/89/Summon_Power_Suit.png"),
                new Buff("Reaper of Grenth", 12366, Source.Common, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/0/07/Reaper_of_Grenth.png"),
                new Buff("Charrzooka",43503, Source.Common, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/1/17/Charrzooka.png"),
                // Fractals 
                new Buff("Rigorous Certainty", 33652, Source.Common, BuffNature.DefensiveBuffTable,"https://wiki.guildwars2.com/images/6/60/Desert_Carapace.png"),
                //
                new Buff("Guild Item Research", 33833, Source.Common, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/c/c6/Guild_Magic_Find_Banner_Boost.png"),
        };

        internal static readonly List<Buff> Gear = new List<Buff>
        {
                new Buff("Sigil of Concentration", 33719, Source.Gear, BuffNature.GearBuffTable, "https://wiki.guildwars2.com/images/b/b3/Superior_Sigil_of_Concentration.png",0 , 93543),
                new Buff("Superior Rune of the Monk", 53285, Source.Gear, BuffStackType.Stacking, 10, BuffNature.GearBuffTable, "https://wiki.guildwars2.com/images/1/18/Superior_Rune_of_the_Monk.png", 93543, ulong.MaxValue),
                new Buff("Sigil of Corruption", 9374, Source.Gear, BuffStackType.Stacking, 25, BuffNature.GearBuffTable, "https://wiki.guildwars2.com/images/1/18/Superior_Sigil_of_Corruption.png"),
                new Buff("Sigil of Life", 9386, Source.Gear, BuffStackType.Stacking, 25, BuffNature.GearBuffTable, "https://wiki.guildwars2.com/images/a/a7/Superior_Sigil_of_Life.png"),
                new Buff("Sigil of Perception", 9385, Source.Gear, BuffStackType.Stacking, 25, BuffNature.GearBuffTable, "https://wiki.guildwars2.com/images/c/cc/Superior_Sigil_of_Perception.png"),
                new Buff("Sigil of Bloodlust", 9286, Source.Gear, BuffStackType.Stacking, 25, BuffNature.GearBuffTable, "https://wiki.guildwars2.com/images/f/fb/Superior_Sigil_of_Bloodlust.png"),
                new Buff("Sigil of Bounty", 38588, Source.Gear, BuffStackType.Stacking, 25, BuffNature.GearBuffTable, "https://wiki.guildwars2.com/images/f/f8/Superior_Sigil_of_Bounty.png"),
                new Buff("Sigil of Benevolence", 9398, Source.Gear, BuffStackType.Stacking, 25, BuffNature.GearBuffTable, "https://wiki.guildwars2.com/images/5/59/Superior_Sigil_of_Benevolence.png"),
                new Buff("Sigil of Momentum", 22144, Source.Gear, BuffStackType.Stacking, 25, BuffNature.GearBuffTable, "https://wiki.guildwars2.com/images/3/30/Superior_Sigil_of_Momentum.png"),
                new Buff("Sigil of the Stars", 46953, Source.Gear, BuffStackType.Stacking, 25, BuffNature.GearBuffTable, "https://wiki.guildwars2.com/images/d/dc/Superior_Sigil_of_the_Stars.png"),
                new Buff("Sigil of Severance", 43930, Source.Gear, BuffNature.GearBuffTable, "https://wiki.guildwars2.com/images/c/c2/Superior_Sigil_of_Severance.png"),
        };

        internal static readonly List<Buff> FractalInstabilities = new List<Buff>()
        {
            new Buff("Mistlock Instability: Adrenaline Rush", 36341, Source.FractalInstability, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/7/72/Mistlock_Instability_Adrenaline_Rush.png"),
            new Buff("Mistlock Instability: Afflicted", 22228, Source.FractalInstability, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/3/3f/Mistlock_Instability_Afflicted.png"),
            new Buff("Mistlock Instability: Boon Overload", 53673, Source.FractalInstability, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/d/d7/Mistlock_Instability_Boon_Overload.png"),
            new Buff("Mistlock Instability: Flux Bomb", 36386, Source.FractalInstability, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/3/3f/Mistlock_Instability_Flux_Bomb.png"),
            new Buff("Mistlock Instability: Fractal Vindicators", 48296, Source.FractalInstability, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/4/48/Mistlock_Instability_Fractal_Vindicators.png"),
            new Buff("Mistlock Instability: Frailty", 54477, Source.FractalInstability, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/d/d6/Mistlock_Instability_Frailty.png"),
            new Buff("Mistlock Instability: Hamstrung", 47323, Source.FractalInstability, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/9/99/Mistlock_Instability_Hamstrung.png"),
            new Buff("Mistlock Instability: Last Laugh", 22293, Source.FractalInstability, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/5/58/Mistlock_Instability_Last_Laugh.png"),
            new Buff("Mistlock Instability: Mists Convergence", 36224, Source.FractalInstability, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/9/95/Mistlock_Instability_Mists_Convergence.png"),
            new Buff("Mistlock Instability: No Pain, No Gain", 22277, Source.FractalInstability, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/c/c3/Mistlock_Instability_No_Pain%2C_No_Gain.png"),
            new Buff("Mistlock Instability: Outflanked", 54084, Source.FractalInstability, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/0/0c/Mistlock_Instability_Outflanked.png"),
            new Buff("Mistlock Instability: Social Awkwardness", 32942, Source.FractalInstability, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/d/d2/Mistlock_Instability_Social_Awkwardness.png"),
            new Buff("Mistlock Instability: Stick Together", 53932, Source.FractalInstability, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/5/59/Mistlock_Instability_Stick_Together.png"),
            new Buff("Mistlock Instability: Sugar Rush", 54237, Source.FractalInstability, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/4/4c/Mistlock_Instability_Sugar_Rush.png"),
            new Buff("Mistlock Instability: Toxic Trail", 36204, Source.FractalInstability, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/d/df/Mistlock_Instability_Toxic_Trail.png"),
            new Buff("Mistlock Instability: Vengeance", 46865, Source.FractalInstability, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/c/c6/Mistlock_Instability_Vengeance.png"),
            new Buff("Mistlock Instability: We Bleed Fire", 54719, Source.FractalInstability, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/2/24/Mistlock_Instability_We_Bleed_Fire.png"),
            new Buff("Mistlock Instability: Toxic Sickness", 47288, Source.FractalInstability, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/6/6f/Mistlock_Instability_Toxic_Sickness.png"),
        };

        internal static readonly List<Buff> FightSpecific = new List<Buff>
        {
                new Buff("Spectral Agony", 38077, Source.FightSpecific,BuffStackType.Stacking, 25, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/7/70/Spectral_Agony.png" ),
                new Buff("Agony", 15773, Source.FightSpecific,BuffStackType.Stacking, 25, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/b/be/Agony.png" ),
                // Whisper of Jormalg
                new Buff("Whisper Teleport Out", 59223, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/7/78/Vengeance_%28Mordrem%29.png" ),
                new Buff("Whisper Teleport Back", 59054, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/7/78/Vengeance_%28Mordrem%29.png" ),
                new Buff("Frigid Vortex", 59105, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/6/65/Windfall.png" ),
                new Buff("Chains of Frost Active", 59100, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/6/63/Use_Soul_Binder.png" ),
                new Buff("Chains of Frost Application", 59120, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/5/5f/Monster_Skill.png" ),
                new Buff("Brain Freeze", 59073, Source.FightSpecific, BuffStackType.Stacking, 20, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/6/6a/Frostbite_%28Bitterfrost_Frontier%29.png" ),
                // Voice and Claw            
                new Buff("Enraged", 58619, Source.FightSpecific, BuffStackType.Stacking, 25, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/7/78/Vengeance_%28Mordrem%29.png" ),
                // Fraenir of Jormag
                new Buff("Frozen", 58376, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/6/6a/Frostbite_%28Bitterfrost_Frontier%29.png" ),
                new Buff("Snowblind", 58276, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/6/6a/Frostbite_%28Bitterfrost_Frontier%29.png" ),
                // Twisted Castle
                new Buff("Spatial Distortion", 34918, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/7/7a/Bloodstone_Blessed.png" ),
                new Buff("Madness", 35006, Source.FightSpecific, BuffStackType.Stacking, 99, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/e/ee/Madness.png" ),
                new Buff("Still Waters", 35106, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/5/5c/Still_Waters_%28effect%29.png" ),
                new Buff("Soothing Waters", 34955, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/8/8f/Soothing_Waters.png" ),
                new Buff("Chaotic Haze", 34963, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/4/48/Lava_Font.png" ),
                new Buff("Timed Bomb", 31485, Source.FightSpecific, BuffStackType.Queue, 1, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/9/91/Time_Bomb.png" ),
                // Deimos
                new Buff("Unnatural Signet",38224, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/2/20/Unnatural_Signet.png"),
                new Buff("Weak Minded",38187, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/3/38/Unseen_Burden_%28Deimos%29.png"),
                new Buff("Tear Instability",37733, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/1/11/Tear_Instability.png"),
                // KC
                new Buff("Compromised",35096, Source.FightSpecific, BuffStackType.Stacking, 25, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/4/48/Compromised.png"),
                new Buff("Xera's Boon",35025, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/0/04/Xera%27s_Boon.png"),
                new Buff("Xera's Fury",35103, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/d/dd/Xera%27s_Fury.png"),
                new Buff("Statue Fixated",34912, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/d/dd/Xera%27s_Fury.png"),
                new Buff("Crimson Attunement (Orb)",35091, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/3/3e/Crimson_Attunement.png"),
                new Buff("Radiant Attunement (Orb)",35109, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/6/68/Radiant_Attunement.png"),
                new Buff("Magic Blast",35119, Source.FightSpecific, BuffStackType.Stacking, 35, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/a/a9/Magic_Blast_Intensity.png"),
                // Xera      
                new Buff("Derangement",34965, Source.FightSpecific, BuffStackType.Stacking, 99, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/c/ca/Derangement.png"),
                new Buff("Bending Chaos",35084, Source.FightSpecific, BuffStackType.Stacking, 11, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/3/39/Target%21.png"),
                new Buff("Shifting Chaos",35162, Source.FightSpecific, BuffStackType.Stacking, 11, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/0/04/Shifting_Chaos.png"),
                new Buff("Twisting Chaos",35032, Source.FightSpecific, BuffStackType.Stacking, 11, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/6/60/Twisting_Chaos.png"),
                new Buff("Intervention",35000, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/a/a2/Intervention_%28effect%29.png"),
                new Buff("Bloodstone Protection",35168, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/4/4e/Bloodstone_Protection.png"),
                new Buff("Bloodstone Blessed",34917, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/7/7a/Bloodstone_Blessed.png"),
                // Gorseval
                new Buff("Spirited Fusion",31722, Source.FightSpecific, BuffStackType.Stacking, 99, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/e/eb/Spirited_Fusion.png"),
                new Buff("Protective Shadow", 31877, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/8/87/Protective_Shadow.png"),
                new Buff("Vivid Echo", 31548, Source.FightSpecific, BuffStackType.Queue, 5, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/4/4f/Vivid_Echo.png"),
                new Buff("Spectral Darkness", 31498, Source.FightSpecific, BuffStackType.Stacking, 10, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/a/a8/Spectral_Darkness.png"),
                // Sabetha    
                new Buff("Shell-Shocked", 34108, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/3/39/Shell-Shocked.png"),
                // Matthias
                new Buff("Blood Shield Abo",34376, Source.FightSpecific, BuffStackType.Stacking, 18, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/a/a6/Blood_Shield.png"),
                new Buff("Blood Shield",34518, Source.FightSpecific, BuffStackType.Stacking, 18, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/a/a6/Blood_Shield.png"),
                new Buff("Blood Fueled",34422, Source.FightSpecific, BuffStackType.Stacking, 1, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/d/d3/Blood_Fueled.png"),
                new Buff("Blood Fueled Abo",34428, Source.FightSpecific, BuffStackType.Stacking, 15, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/d/d3/Blood_Fueled.png"),
                // Qadim
                new Buff("Flame Armor",52568, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/e/e7/Magma_Orb.png"),
                new Buff("Fiery Surge",52588, Source.FightSpecific, BuffStackType.Stacking, 99, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/f/f9/Fiery_Surge.png"),
                // Soulless Horror
                new Buff("Necrosis",47414, Source.FightSpecific, BuffStackType.Stacking, 25, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/4/47/Ichor.png"),
                // CA
                new Buff("Greatsword Power",52667 , Source.FightSpecific, BuffStackType.Stacking, 10, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/3/3b/Greatsword_Power_%28effect%29.png"),
                new Buff("Fractured - Enemy",53030, Source.FightSpecific, BuffStackType.Stacking, 10, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/7/78/Branded_Aura.png"),
                new Buff("Fractured - Allied",52213, Source.FightSpecific, BuffStackType.Stacking, 2, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/7/78/Branded_Aura.png"),
                new Buff("Conjured Shield",52754 , Source.FightSpecific, BuffStackType.Stacking, 10, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/8/83/Conjured_Shield_%28effect%29.png"),
                new Buff("Conjured Protection",52973 , Source.FightSpecific, BuffStackType.Stacking, 25, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/8/83/Bloodstone-Infused_shield.png"),
                new Buff("Shielded",53003 , Source.FightSpecific, BuffStackType.Stacking, 10, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/4/47/Golem-Powered_Shielding.png"),
                new Buff("Augmented Power",52074  , Source.FightSpecific, BuffStackType.Stacking, 10, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/4/47/Golem-Powered_Shielding.png"),
                new Buff("Scepter Lock-on",53075  , Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/d/d3/Blood_Fueled.png"),
                new Buff("CA Invul",52255 , Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/d/d3/Blood_Fueled.png"),
                new Buff("Arm Up",52430 , Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/d/d3/Blood_Fueled.png"),
                // Twin Largos
                //new Buff("Aquatic Detainment",52931 , ParseHelper.Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/d/d3/Blood_Fueled.png"),
                new Buff("Aquatic Aura (Kenut)",52211 , Source.FightSpecific, BuffStackType.Stacking, 80, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/4/44/Expose_Weakness.png"),
                new Buff("Aquatic Aura (Nikare)",52929 , Source.FightSpecific, BuffStackType.Stacking, 80, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/f/fd/Fractured_%28effect%29.png"),
                new Buff("Waterlogged",51935 , Source.FightSpecific, BuffStackType.Stacking, 10, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/8/89/Waterlogged.png"),
                // Slothasor
                new Buff("Narcolepsy", 34467, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/e/eb/Determined.png"),
                // VG
                new Buff("Blue Pylon Power", 31413, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/6/6e/Blue_Pylon_Power.png"),
                new Buff("Pylon Attunement: Red", 31695, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/9/94/Pylon_Attunement-_Red.png"),
                new Buff("Pylon Attunement: Blue", 31317, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/6/6e/Blue_Pylon_Power.png"),
                new Buff("Pylon Attunement: Green", 31852, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/a/aa/Pylon_Attunement-_Green.png"),
                new Buff("Unstable Pylon: Red", 31539, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/3/36/Unstable_Pylon_%28Red%29.png"),
                new Buff("Unstable Pylon: Blue", 31884, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/c/c3/Unstable_Pylon_%28Blue%29.png"),
                new Buff("Unstable Pylon: Green", 31828, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/9/9d/Unstable_Pylon_%28Green%29.png"),
                new Buff("Unbreakable", 34979, Source.FightSpecific, BuffStackType.Stacking, 2, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/5/56/Xera%27s_Embrace.png"),
                // Trio
                new Buff("Not the Bees!", 34434, Source.FightSpecific, BuffStackType.Stacking, 25, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/0/08/Throw_Jar.png"),
                new Buff("Targeted", 34392, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/2/24/Targeted.png"),
                // River
                new Buff("Soul Siphon", 47470, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/f/f7/Soul_Siphon.png"),
                new Buff("Desmina's Protection", 47219, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/b/b3/Desmina%27s_Protection.png"),
                new Buff("Follower's Asylum", 47122, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/b/b3/Desmina%27s_Protection.png"),
                // Broken King          
                new Buff("Frozen Wind", 47776, Source.FightSpecific, BuffStackType.Stacking, 25, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/3/3a/Frozen_Wind.png"),
                // Dhuum
                new Buff("Spirit Transfrom", 48281, Source.FightSpecific, BuffStackType.Stacking, 30, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/4/48/Compromised.png"),
                new Buff("Fractured Spirit", 46950, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/c/c3/Fractured_Spirit.png"),
                new Buff("Residual Affliction", 47476, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/1/12/Residual_Affliction.png"),
                new Buff("Arcing Affliction", 47646, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/f/f0/Arcing_Affliction.png"),
                // Adina
                new Buff("Pillar Pandemonium", 56204, Source.FightSpecific, BuffStackType.Stacking, 99, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/d/d9/Captain%27s_Inspiration.png"),
                new Buff("Radiant Blindness", 56593, Source.FightSpecific, BuffStackType.Queue, 25, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/6/6c/Radiant_Blindness.png"),
                new Buff("Diamond Palisade (Damage)", 56099, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/5/5f/Monster_Skill.png"),
                new Buff("Diamond Palisade", 56636, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/5/5f/Monster_Skill.png"),
                new Buff("Eroding Curse", 56440, Source.FightSpecific, BuffStackType.Stacking, 99, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/d/de/Toxic_Gas.png"),
                // Sabir
                new Buff("Ion Shield", 56100, Source.FightSpecific, BuffStackType.Stacking, 80, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/9/94/Ion_Shield.png"),
                new Buff("Violent Currents", 56123, Source.FightSpecific, BuffStackType.Stacking, 5, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/0/06/Violent_Currents.png"),
                new Buff("Repulsion Field", 56172, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/2/24/Targeted.png"),
                new Buff("Electrical Repulsion", 56391, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/7/71/Electrical_Repulsion.png"),
                new Buff("Electro-Repulsion", 56474, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/7/74/Unbridled_Chaos.png"),
                // Peerless Qadim
                new Buff("Erratic Energy", 56582, Source.FightSpecific, BuffStackType.Stacking, 25, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/4/45/Unstable.png"),
                new Buff("Power Share", 56104, Source.FightSpecific, BuffStackType.Stacking, 3, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/2/24/Targeted.png"),
                new Buff("Sapping Surge", 56118, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/6/6f/Guilt_Exploitation.png"),
                new Buff("Chaos Corrosion", 56182, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/f/fd/Fractured_%28effect%29.png"),
                new Buff("Peerless Fixated", 56510, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/6/66/Fixated.png"),
                new Buff("Magma Drop", 56475, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/2/24/Targeted.png"),
                new Buff("Critical Mass", 56424, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/b/bf/Orb_of_Ascension_%28effect%29.png"),
                new Buff("Kinetic Abundance", 56609, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/0/06/Values_Mastery.png"),
                new Buff("Unbridled Chaos", 56467, Source.FightSpecific, BuffStackType.Stacking, 3, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/4/42/Exposed_%28Statue_of_Darkness%29.png"),
                // Cairn        
                new Buff("Shared Agony", 38049, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/5/53/Shared_Agony.png"),
                new Buff("Enraged (Cairn)", 37675, Source.FightSpecific, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/7/78/Vengeance_%28Mordrem%29.png"),
                new Buff("Unseen Burden", 38153, Source.FightSpecific, BuffStackType.Stacking , 99, BuffNature.GraphOnlyBuff,"https://wiki.guildwars2.com/images/b/b9/Unseen_Burden.png"),
                // Ai, Keeper of the Peak
                new Buff("Tidal Barrier", 61402, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/b/b1/Primed_Bottle.png"),
                new Buff("Whirlwind Shield", 61224, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/b/b1/Primed_Bottle.png"),
                new Buff("Resilient Form", 61220, Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/1/13/Crowd_Favor.png"),
                new Buff("Cacophonous Mind", 61435, Source.FightSpecific, BuffStackType.Stacking, 20, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/1/13/Crowd_Favor.png"),
                new Buff("Crushing Guilt", 61208, Source.FightSpecific, BuffStackType.Stacking, 10, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/1/13/Crowd_Favor.png"),
                //new Buff("Sunqua Fixated 1", 61503, ParserHelper.Source.FightSpecific, BuffStackType.Stacking, 10, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/1/13/Crowd_Favor.png"),
                //new Buff("Sunqua Fixated 2", 61506, ParserHelper.Source.FightSpecific, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/1/13/Crowd_Favor.png"), // what are those exactly?
                new Buff("Charged Leap", 61444, Source.FightSpecific, BuffStackType.StackingConditionalLoss, 3, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/1/13/Crowd_Favor.png"),
                new Buff("Tidal Bargain", 61512, Source.FightSpecific, BuffStackType.Stacking, 10, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/1/13/Crowd_Favor.png"),
        };

        internal static readonly List<Buff> NormalFoods = new List<Buff>
        {
                new Buff("Malnourished",46587, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/6/67/Malnourished.png"),
                new Buff("Plate of Truffle Steak",9769, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/4/4c/Plate_of_Truffle_Steak.png"),
                new Buff("Bowl of Sweet and Spicy Butternut Squash Soup",17825, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/d/df/Bowl_of_Sweet_and_Spicy_Butternut_Squash_Soup.png"),
                new Buff("Bowl Curry Butternut Squash Soup",9829, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/1/16/Bowl_of_Curry_Butternut_Squash_Soup.png"),
                new Buff("Red-Lentil Saobosa",46273, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/a/a8/Red-Lentil_Saobosa.png"),
                new Buff("Super Veggie Pizza",10008, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/6/66/Super_Veggie_Pizza.png"),
                new Buff("Rare Veggie Pizza",10009, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/a/a0/Rare_Veggie_Pizza.png"),
                new Buff("Bowl of Garlic Kale Sautee",-1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/0/04/Bowl_of_Garlic_Kale_Sautee.png"),
                new Buff("Koi Cake",-1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/1/14/Koi_Cake.png"),
                new Buff("Prickly Pear Pie",24800, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/0/0a/Prickly_Pear_Pie.png"),
                new Buff("Bowl of Nopalitos Sauté",-1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/f/f1/Bowl_of_Nopalitos_Saut%C3%A9.png"),
                new Buff("Loaf of Candy Cactus Cornbread",24797, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/b/b2/Loaf_of_Candy_Cactus_Cornbread.png"),
                new Buff("Delicious Rice Ball",26529, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/5d/Delicious_Rice_Ball.png"),
                new Buff("Slice of Allspice Cake",33792, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/1/13/Slice_of_Allspice_Cake.png"),
                new Buff("Fried Golden Dumpling",26530, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/1/14/Fried_Golden_Dumpling.png"),
                new Buff("Bowl of Seaweed Salad",10080, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/1/1c/Bowl_of_Seaweed_Salad.png"),
                new Buff("Bowl of Orrian Truffle and Meat Stew",10096, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/b/b8/Bowl_of_Orrian_Truffle_and_Meat_Stew.png"),
                new Buff("Plate of Mussels Gnashblade",33476, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/7/7b/Plate_of_Mussels_Gnashblade.png"),
                new Buff("Spring Roll",26534, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/d/da/Spring_Roll.png"),
                new Buff("Plate of Beef Rendang",49686, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/d/d0/Plate_of_Beef_Rendang.png"),
                new Buff("Dragon's Revelry Starcake",19451, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/2/2b/Dragon%27s_Revelry_Starcake.png"),
                new Buff("Avocado Smoothie",50091, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/8/83/Avocado_Smoothie.png"),
                new Buff("Carrot Souffle",-1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/7/71/Carrot_Souffl%C3%A9.png"), //same as Dragon's_Breath_Bun
                new Buff("Plate of Truffle Steak Dinner",-1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/9/92/Plate_of_Truffle_Steak_Dinner.png"), //same as Dragon's Breath Bun
                new Buff("Dragon's Breath Bun",9750, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/53/Dragon%27s_Breath_Bun.png"),
                new Buff("Karka Egg Omelet",9756, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/9/9e/Karka_Egg_Omelet.png"),
                new Buff("Steamed Red Dumpling",26536, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/8/8c/Steamed_Red_Dumpling.png"),
                new Buff("Saffron Stuffed Mushroom",-1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/e/e2/Saffron_Stuffed_Mushroom.png"), //same as Karka Egg Omelet
                new Buff("Soul Pastry",53222, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/2/2c/Soul_Pastry.png"),
                new Buff("Bowl of Fire Meat Chili",10119, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/b/ba/Bowl_of_Fire_Meat_Chili.png"),
                new Buff("Plate of Fire Flank Steak",9765, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/2/27/Plate_of_Fire_Flank_Steak.png"),
                new Buff("Plate of Orrian Steak Frittes",9773, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/4/4d/Plate_of_Orrian_Steak_Frittes.png"),
                new Buff("Spicier Flank Steak",9764, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/0/01/Spicier_Flank_Steak.png"),
                new Buff("Mango Pie",9993, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/3/3d/Mango_Pie.png"),
        };

        internal static readonly List<Buff> Utilities = new List<Buff>
        {     
                // UTILITIES 
                // 1h versions have the same ID as 30 min versions 
                new Buff("Diminished",46668, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/7/71/Diminished.png"),
                new Buff("Rough Sharpening Stone", 9958, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/1/14/Rough_Sharpening_Stone.png"),
                new Buff("Simple Sharpening Stone", 9959, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/e/ef/Simple_Sharpening_Stone.png"),
                new Buff("Standard Sharpening Stone", 9960, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/8/82/Standard_Sharpening_Stone.png"),
                new Buff("Quality Sharpening Stone", 9961, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/6/68/Quality_Sharpening_Stone.png"),
                new Buff("Hardened Sharpening Stone", 9962, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/8/8d/Hardened_Sharpening_Stone.png"),
                new Buff("Superior Sharpening Stone", 9963, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/7/78/Superior_Sharpening_Stone.png"),
                //new Buff("Ogre Sharpening Stone", 9963, ParseHelper.Source.Item, BuffType.Duration, 1, BuffNature.Consumable, "https://wiki.guildwars2.com/images/7/78/Superior_Sharpening_Stone.png"),
                new Buff("Apprentice Maintenance Oil", 10111, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/51/Apprentice_Maintenance_Oil.png"),
                new Buff("Journeyman Maintenance Oil", 10112, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/b/b1/Journeyman_Maintenance_Oil.png"),
                new Buff("Standard Maintenance Oil", 9971, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/a/a6/Standard_Maintenance_Oil.png"),
                new Buff("Artisan Maintenance Oil", 9970, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/7/75/Artisan_Maintenance_Oil.png"),
                new Buff("Quality Maintenance Oil", 9969, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/2/23/Quality_Maintenance_Oil.png"),
                new Buff("Master Maintenance Oil", 9968, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/5b/Master_Maintenance_Oil.png"),
                //new Buff("Hylek Maintenance Oil", 9968, ParseHelper.Source.Item, BuffType.Duration, 1, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/5b/Master_Maintenance_Oil.png"),
                new Buff("Apprentice Tuning Crystal", 10113, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/7/7d/Apprentice_Tuning_Crystal.png"),
                new Buff("Journeyman Tuning Crystal", 10114, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/1/1e/Journeyman_Tuning_Crystal.png"),
                new Buff("Standard Tuning Crystal", 9964, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/1/1e/Standard_Tuning_Crystal.png"),
                new Buff("Artisan Tuning Crystal", 9965, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/e/eb/Artisan_Tuning_Crystal.png"),
                new Buff("Quality Tuning Crystal", 9966, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/3/3b/Quality_Tuning_Crystal.png"),
                new Buff("Master Tuning Crystal", 9967, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/58/Master_Tuning_Crystal.png"),
                //new Buff("Krait Tuning Crystal", 9967, ParseHelper.Source.Item, BuffType.Duration, 1, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/58/Master_Tuning_Crystal.png"),
                new Buff("Compact Hardened Sharpening Stone", 34657, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/1/1f/Compact_Hardened_Sharpening_Stone.png"),
                new Buff("Tin of Fruitcake", 34211, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/a/af/Tin_of_Fruitcake.png"),
                new Buff("Bountiful Sharpening Stone", 25880, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/7/78/Superior_Sharpening_Stone.png"),
                new Buff("Toxic Sharpening Stone", 21826, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/d/db/Toxic_Sharpening_Stone.png"),
                new Buff("Magnanimous Sharpening Stone", 38522, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/a/aa/Magnanimous_Sharpening_Stone.png"),
                new Buff("Corsair Sharpening Stone", 46925, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/0/06/Corsair_Sharpening_Stone.png"),
                new Buff("Furious Sharpening Stone", 25882, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/7/78/Superior_Sharpening_Stone.png"),
                new Buff("Holographic Super Cheese", 50320, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/f/fa/Holographic_Super_Cheese.png"),
                new Buff("Compact Quality Maintenance Oil", 34671, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/d/d8/Compact_Quality_Maintenance_Oil.png"),
                new Buff("Peppermint Oil", 34187, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/b/bc/Peppermint_Oil.png"),
                new Buff("Toxic Maintenance Oil", 21827, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/a/a6/Toxic_Maintenance_Oil.png"),
                new Buff("Magnanimous Maintenance Oil", 38605, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/53/Magnanimous_Maintenance_Oil.png"),
                new Buff("Enhanced Lucent Oil", 53304, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/e/ee/Enhanced_Lucent_Oil.png"),
                new Buff("Potent Lucent Oil", 53374, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/1/16/Potent_Lucent_Oil.png"),
                new Buff("Corsair Maintenance Oil", 47734, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/53/Magnanimous_Maintenance_Oil.png"),
                new Buff("Furious Maintenance Oil", 25881, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/5b/Master_Maintenance_Oil.png"),
                new Buff("Holographic Super Drumstick", 50302, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/1/1d/Holographic_Super_Drumstick.png"),
                new Buff("Bountiful Maintenance Oil", 25879, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/5b/Master_Maintenance_Oil.png"),
                new Buff("Compact Quality Tuning Crystal", 34677, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/b/ba/Compact_Quality_Tuning_Crystal.png"),
                new Buff("Tuning Icicle", 34206, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/7/75/Tuning_Icicle.png"),
                new Buff("Bountiful Tuning Crystal", 25877, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/58/Master_Tuning_Crystal.png"),
                new Buff("Toxic Focusing Crystal", 21828, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/d/de/Toxic_Focusing_Crystal.png"),
                new Buff("Magnanimous Tuning Crystal", 38678, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/2/23/Magnanimous_Tuning_Crystal.png"),
                new Buff("Furious Tuning Crystal", 25878, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/58/Master_Tuning_Crystal.png"),
                new Buff("Corsair Tuning Crystal", 48348, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/f/f7/Corsair_Tuning_Crystal.png"),
                new Buff("Holographic Super Apple", 50307, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/e/ee/Holographic_Super_Apple.png"),
                new Buff("Sharpening Skull", 25630, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/e/ee/Sharpening_Skull.png"),
                new Buff("Flask of Pumpkin Oil", 25632, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/7/71/Flask_of_Pumpkin_Oil.png"),
                new Buff("Lump of Crystallized Nougat", 25631, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/8/8f/Lump_of_Crystallized_Nougat.png"),
                new Buff("Skale Venom (Consumable)", 972, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/9/96/Skale_Venom_%28consumable%29.png"),
                new Buff("Swift Moa Feather", 23239, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/f/f0/Swift_Moa_Feather.png"),
        };

        internal static readonly List<Buff> Writs = new List<Buff>
        {

                new Buff("Writ of Basic Strength", 33160, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/7/7e/Writ_of_Basic_Strength.png"),
                new Buff("Writ of Strength", 32105, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/5e/Writ_of_Strength.png"),
                new Buff("Writ of Studied Strength", 33647, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/2/23/Writ_of_Studied_Strength.png"),
                new Buff("Writ of Calculated Strength", 32401, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/8/8d/Writ_of_Calculated_Strength.png"),
                new Buff("Writ of Learned Strength", 32044, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/8/8d/Writ_of_Calculated_Strength.png"),
                new Buff("Writ of Masterful Strength", 33297, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/2/2b/Writ_of_Masterful_Strength.png"),
                new Buff("Writ of Basic Accuracy", 33572, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/8/82/Writ_of_Basic_Accuracy.png"),
                new Buff("Writ of Accuracy", 32805, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/2/29/Writ_of_Accuracy.png"),
                new Buff("Writ of Studied Accuracy", 32429, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/a/ad/Writ_of_Studied_Accuracy.png"),
                new Buff("Writ of Calculated Accuracy", 33798, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/2/27/Writ_of_Calculated_Accuracy.png"),
                new Buff("Writ of Learned Accuracy", 32374, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/5a/Writ_of_Masterful_Accuracy.png"),
                new Buff("Writ of Masterful Accuracy", 31970, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/5a/Writ_of_Masterful_Accuracy.png"),
                new Buff("Writ of Basic Malice", 33310, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/9/9e/Writ_of_Basic_Malice.png"),
                new Buff("Writ of Malice", 33803, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/c/c4/Writ_of_Malice.png"),
                new Buff("Writ of Studied Malice", 32927, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/b/bd/Writ_of_Studied_Malice.png"),
                new Buff("Writ of Calculated Malice", 32316, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/7/75/Writ_of_Calculated_Malice.png"),
                new Buff("Writ of Learned Malice", 31959, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/9/9b/Writ_of_Learned_Malice.png"),
                new Buff("Writ of Masterful Malice", 33836, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/2/20/Writ_of_Masterful_Malice.png"),
                new Buff("Writ of Basic Speed", 33776, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/e/e6/Writ_of_Basic_Speed.png"),
                new Buff("Writ of Studied Speed", 33005, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/d/d1/Recipe_sheet_fine_boots.png"),
                new Buff("Writ of Masterful Speed", 33040, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/8/8e/Recipe_sheet_masterwork_boots.png"),
        };

        internal static readonly List<Buff> Potions = new List<Buff>
        {
                new Buff("Potion Of Karka Toughness", 18704, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/f/fb/Powerful_Potion_of_Inquest_Slaying.png"),
                // Slaying Potions
                new Buff("Powerful Potion of Flame Legion Slaying",9925, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/e/e2/Powerful_Potion_of_Flame_Legion_Slaying.png"),
                new Buff("Powerful Potion of Halloween Slaying",15279, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/f/fe/Powerful_Potion_of_Halloween_Slaying.png"),
                new Buff("Powerful Potion of Centaur Slaying",9845, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/3/3b/Powerful_Potion_of_Centaur_Slaying.png"),
                new Buff("Powerful Potion of Krait Slaying",9885, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/b/b4/Powerful_Potion_of_Krait_Slaying.png"),
                new Buff("Powerful Potion of Ogre Slaying",9877, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/b/b5/Powerful_Potion_of_Ogre_Slaying.png"),
                new Buff("Powerful Potion of Elemental Slaying",9893, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/5f/Powerful_Potion_of_Elemental_Slaying.png"),
                new Buff("Powerful Potion of Destroyer Slaying",9869, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/b/bd/Powerful_Potion_of_Destroyer_Slaying.png"),
                new Buff("Powerful Potion of Nightmare Court Slaying",9941, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/7/74/Powerful_Potion_of_Nightmare_Court_Slaying.png"),
                new Buff("Powerful Potion of Slaying Scarlet's Armies",23228, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/e/ee/Powerful_Potion_of_Demon_Slaying.png"),
                new Buff("Powerful Potion of Undead Slaying",9837, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/b/bd/Powerful_Potion_of_Undead_Slaying.png"),
                new Buff("Powerful Potion of Dredge Slaying",9949, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/9/9a/Powerful_Potion_of_Dredge_Slaying.png"),
                new Buff("Powerful Potion of Inquest Slaying",9917, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/f/fb/Powerful_Potion_of_Inquest_Slaying.png"),
                new Buff("Powerful Potion of Demon Slaying",9901, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/e/ee/Powerful_Potion_of_Demon_Slaying.png"),
                new Buff("Powerful Potion of Grawl Slaying",9853, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/1/15/Powerful_Potion_of_Grawl_Slaying.png"),
                new Buff("Powerful Potion of Sons of Svanir Slaying",9909, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/3/33/Powerful_Potion_of_Sons_of_Svanir_Slaying.png"),
                new Buff("Powerful Potion of Outlaw Slaying",9933, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/e/ec/Powerful_Potion_of_Outlaw_Slaying.png"),
                new Buff("Powerful Potion of Ice Brood Slaying",9861, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/0/0d/Powerful_Potion_of_Ice_Brood_Slaying.png"),
                // Fractals 
                new Buff("Fractal Mobility", 33024, Source.Item, BuffStackType.Stacking, 5, BuffNature.Consumable,"https://wiki.guildwars2.com/images/thumb/2/22/Mist_Mobility_Potion.png/40px-Mist_Mobility_Potion.png"),
                new Buff("Fractal Defensive", 32134, Source.Item, BuffStackType.Stacking, 5, BuffNature.Consumable,"https://wiki.guildwars2.com/images/thumb/e/e6/Mist_Defensive_Potion.png/40px-Mist_Defensive_Potion.png"),
                new Buff("Fractal Offensive", 32473, Source.Item, BuffStackType.Stacking, 5, BuffNature.Consumable,"https://wiki.guildwars2.com/images/thumb/8/8d/Mist_Offensive_Potion.png/40px-Mist_Offensive_Potion.png"),

        };

        internal static readonly List<Buff> AscendedFood = new List<Buff>
        {
                // Ascended Food
                // Feasts with yet unknown IDs are also added with ID of -1, the IDs can be added later on demand
                new Buff("Bowl of Fruit Salad with Cilantro Garnish", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/0/08/Bowl_of_Fruit_Salad_with_Cilantro_Garnish.png"),
                new Buff("Bowl of Fruit Salad with Mint Garnish", 57100, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/4/47/Bowl_of_Fruit_Salad_with_Mint_Garnish.png"),
                new Buff("Bowl of Fruit Salad with Orange-Clove Syrup", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/d/dc/Bowl_of_Fruit_Salad_with_Orange-Clove_Syrup.png"),
                new Buff("Bowl of Sesame Fruit Salad", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/0/01/Bowl_of_Sesame_Fruit_Salad.png"),
                new Buff("Bowl of Spiced Fruit Salad", 57276, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/9/9c/Bowl_of_Spiced_Fruit_Salad.png"),
                new Buff("Cilantro Lime Sous-Vide Steak", 57244, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/6/65/Cilantro_Lime_Sous-Vide_Steak.png"),
                new Buff("Cilantro and Cured Meat Flatbread", 57409, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/8/87/Cilantro_and_Cured_Meat_Flatbread.png"),
                new Buff("Clove and Veggie Flatbread", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/6/6e/Clove_and_Veggie_Flatbread.png"),
                new Buff("Clove-Spiced Creme Brulee", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/a/a2/Clove-Spiced_Creme_Brulee.png"),
                new Buff("Clove-Spiced Eggs Benedict", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/7/7d/Clove-Spiced_Eggs_Benedict.png"),
                new Buff("Clove-Spiced Pear and Cured Meat Flatbread", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/c/c5/Clove-Spiced_Pear_and_Cured_Meat_Flatbread.png"),
                new Buff("Eggs Benedict with Mint-Parsley Sauce", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/9/92/Eggs_Benedict_with_Mint-Parsley_Sauce.png"),
                new Buff("Mango Cilantro Creme Brulee", 57267, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/3/3d/Mango_Cilantro_Creme_Brulee.png"),
                new Buff("Mint Creme Brulee", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/3/31/Mint_Creme_Brulee.png"),
                new Buff("Mint Strawberry Cheesecake", 57384, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/6/64/Mint_Strawberry_Cheesecake.png"),
                new Buff("Mint and Veggie Flatbread", 57263, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/f/f9/Mint_and_Veggie_Flatbread.png"),
                new Buff("Mint-Pear Cured Meat Flatbread", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/6/60/Mint-Pear_Cured_Meat_Flatbread.png"),
                new Buff("Mushroom Clove Sous-Vide Steak", 57393, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/b/ba/Mushroom_Clove_Sous-Vide_Steak.png"),
                new Buff("Orange Clove Cheesecake", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/3/3f/Orange_Clove_Cheesecake.png"),
                new Buff("Peppercorn and Veggie Flatbread", 57382, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/9/9d/Peppercorn_and_Veggie_Flatbread.png"),
                new Buff("Peppercorn-Crusted Sous-Vide Steak", 57051, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/2/2e/Peppercorn-Crusted_Sous-Vide_Steak.png"),
                new Buff("Peppercorn-Spiced Eggs Benedict", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/c/c6/Peppercorn-Spiced_Eggs_Benedict.png"),
                new Buff("Peppered Cured Meat Flatbread", 57127, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/2/2d/Peppered_Cured_Meat_Flatbread.png"),
                new Buff("Plate of Beef Carpaccio with Mint Garnish", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/4/42/Plate_of_Beef_Carpaccio_with_Mint_Garnish.png"),
                new Buff("Plate of Clear Truffle and Cilantro Ravioli", 57295, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/0/05/Plate_of_Clear_Truffle_and_Cilantro_Ravioli.png"),
                new Buff("Plate of Clear Truffle and Mint Ravioli", 57112, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/9/9e/Plate_of_Clear_Truffle_and_Mint_Ravioli.png"),
                new Buff("Plate of Clear Truffle and Sesame Ravioli", 57213, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/d/de/Plate_of_Clear_Truffle_and_Sesame_Ravioli.png"),
                new Buff("Plate of Clove-Spiced Beef Carpaccio", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/4/44/Plate_of_Clove-Spiced_Beef_Carpaccio.png"),
                new Buff("Plate of Clove-Spiced Clear Truffle Ravioli", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/c/c2/Plate_of_Clove-Spiced_Clear_Truffle_Ravioli.png"),
                new Buff("Plate of Clove-Spiced Coq Au Vin", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/d/dc/Plate_of_Clove-Spiced_Coq_Au_Vin.png"),
                new Buff("Plate of Clove-Spiced Poultry Aspic", 57302, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/50/Plate_of_Clove-Spiced_Poultry_Aspic.png"),
                new Buff("Plate of Coq Au Vin with Mint Garnish", 57362, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/7/7c/Plate_of_Coq_Au_Vin_with_Mint_Garnish.png"),
                new Buff("Plate of Coq Au Vin with Salsa", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/8/80/Plate_of_Coq_Au_Vin_with_Salsa.png"),
                new Buff("Plate of Peppercorn-Spiced Beef Carpaccio", 57114, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/4/40/Plate_of_Peppercorn-Spiced_Beef_Carpaccio.png"),
                new Buff("Plate of Peppercorn-Spiced Coq Au Vin", 57260, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/4/43/Plate_of_Peppercorn-Spiced_Coq_Au_Vin.png"),
                new Buff("Plate of Peppercorn-Spiced Poultry Aspic", 57299, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/4/4f/Plate_of_Peppercorn-Spiced_Poultry_Aspic.png"),
                new Buff("Plate of Peppered Clear Truffle Ravioli", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/f/fe/Plate_of_Peppered_Clear_Truffle_Ravioli.png"),
                new Buff("Plate of Poultry Aspic with Mint Garnish", 57178, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/9/91/Plate_of_Poultry_Aspic_with_Mint_Garnish.png"),
                new Buff("Plate of Poultry Aspic with Salsa Garnish", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/5b/Plate_of_Poultry_Aspic_with_Salsa_Garnish.png"),
                new Buff("Plate of Sesame Poultry Aspic", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/6/64/Plate_of_Sesame_Poultry_Aspic.png"),
                new Buff("Plate of Sesame-Crusted Coq Au Vin", 57290, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/3/3e/Plate_of_Sesame-Crusted_Coq_Au_Vin.png"),
                new Buff("Plate of Sesame-Ginger Beef Carpaccio", 57231, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/b/b7/Plate_of_Sesame-Ginger_Beef_Carpaccio.png"),
                new Buff("Salsa Eggs Benedict", 57117, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/7/79/Salsa_Eggs_Benedict.png"),
                new Buff("Salsa-Topped Veggie Flatbread", 57269, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/f/f3/Salsa-Topped_Veggie_Flatbread.png"),
                new Buff("Sesame Cheesecake", 57328, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/e/ef/Sesame_Cheesecake.png"),
                new Buff("Sesame Creme Brulee", 57194, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/6/63/Sesame_Creme_Brulee.png"),
                new Buff("Sesame Eggs Benedict", 57084, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/f/f5/Sesame_Eggs_Benedict.png"),
                new Buff("Sesame Veggie Flatbread", 57050, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/d/db/Sesame_Veggie_Flatbread.png"),
                new Buff("Sesame-Asparagus and Cured Meat Flatbread", 57222, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/8/87/Sesame-Asparagus_and_Cured_Meat_Flatbread.png"),
                new Buff("Sous-Vide Steak with Mint-Parsley Sauce", 57342, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/9/99/Sous-Vide_Steak_with_Mint-Parsley_Sauce.png"),
                new Buff("Soy-Sesame Sous-Vide Steak", 57241, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/d/da/Soy-Sesame_Sous-Vide_Steak.png"),
                new Buff("Spherified Cilantro Oyster Soup", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/e/e1/Spherified_Cilantro_Oyster_Soup.png"),
                new Buff("Spherified Clove-Spiced Oyster Soup", 57374, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/f/fa/Spherified_Clove-Spiced_Oyster_Soup.png"),
                new Buff("Spherified Oyster Soup with Mint Garnish", 57201, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/6/63/Spherified_Oyster_Soup_with_Mint_Garnish.png"),
                new Buff("Spherified Peppercorn-Spiced Oyster Soup", 57165, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/4/43/Spherified_Peppercorn-Spiced_Oyster_Soup.png"),
                new Buff("Spherified Sesame Oyster Soup", 57037, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/5/51/Spherified_Sesame_Oyster_Soup.png"),
                new Buff("Spiced Pepper Creme Brulee", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/b/ba/Spiced_Pepper_Creme_Brulee.png"),
                new Buff("Spiced Peppercorn Cheesecake",-1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/9/9c/Spiced_Peppercorn_Cheesecake.png"),
                new Buff("Strawberry Cilantro Cheesecake", -1, Source.Item, BuffNature.Consumable, "https://wiki.guildwars2.com/images/8/8d/Strawberry_Cilantro_Cheesecake.png"),
        };

    }
}
