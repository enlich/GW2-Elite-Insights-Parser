﻿using System.Collections.Generic;
using GW2EIEvtcParser.Extensions;
using static GW2EIEvtcParser.ArcDPSEnums;
using static GW2EIEvtcParser.EIData.Buff;

namespace GW2EIEvtcParser.EIData
{
    internal static class FirebrandHelper
    {
        internal static readonly List<InstantCastFinder> InstantCastFinder = new List<InstantCastFinder>()
        {
            new DamageCastFinder(46618,46618,EIData.InstantCastFinder.DefaultICD), // Flame Rush
            new DamageCastFinder(46616,46616,EIData.InstantCastFinder.DefaultICD), // Flame Surge
            //new DamageCastFinder(42360,42360,InstantCastFinder.DefaultICD), // Echo of Truth
            //new DamageCastFinder(44008,44008,InstantCastFinder.DefaultICD), // Voice of Truth
            new EXTHealingCastFinder(41714, 41714, EIData.InstantCastFinder.DefaultICD), // Mantra of Solace
        };

        internal static readonly List<DamageModifier> DamageMods = new List<DamageModifier>
        {
        };

        internal static readonly List<Buff> Buffs = new List<Buff>
        {
                new Buff("Ashes of the Just",41957, ParserHelper.Source.Firebrand, BuffStackType.StackingConditionalLoss, 25, BuffNature.OffensiveBuffTable, "https://wiki.guildwars2.com/images/6/6d/Epilogue-_Ashes_of_the_Just.png"),
                new Buff("Eternal Oasis",44871, ParserHelper.Source.Firebrand, BuffNature.DefensiveBuffTable, "https://wiki.guildwars2.com/images/5/5f/Epilogue-_Eternal_Oasis.png"),
                new Buff("Unbroken Lines",43194, ParserHelper.Source.Firebrand, BuffStackType.Stacking, 3, BuffNature.DefensiveBuffTable, "https://wiki.guildwars2.com/images/d/d8/Epilogue-_Unbroken_Lines.png"),
                new Buff("Tome of Justice",40530, ParserHelper.Source.Firebrand, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/a/ae/Tome_of_Justice.png"),
                new Buff("Tome of Courage",43508,ParserHelper.Source.Firebrand, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/5/54/Tome_of_Courage.png"),
                new Buff("Tome of Resolve",46298, ParserHelper.Source.Firebrand, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/a/a9/Tome_of_Resolve.png"),
                new Buff("Quickfire",45123, ParserHelper.Source.Firebrand, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/d/d6/Quickfire.png"),
        };

    }
}
