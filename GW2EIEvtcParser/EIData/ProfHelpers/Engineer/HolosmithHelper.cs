﻿using System.Collections.Generic;
using static GW2EIEvtcParser.ArcDPSEnums;
using static GW2EIEvtcParser.EIData.Buff;
using static GW2EIEvtcParser.EIData.DamageModifier;
using static GW2EIEvtcParser.ParserHelper;

namespace GW2EIEvtcParser.EIData
{
    internal static class HolosmithHelper
    {
        internal static readonly List<InstantCastFinder> InstantCastFinder = new List<InstantCastFinder>()
        {
            new BuffGainCastFinder(43937, 41037, EIData.InstantCastFinder.DefaultICD), // Overheat
            new BuffGainCastFinder(42938, 43708, EIData.InstantCastFinder.DefaultICD), // Photon Forge
            new BuffLossCastFinder(41123, 43708, EIData.InstantCastFinder.DefaultICD), // Deactivate Photon Forge - red or blue irrevelant
            new BuffGainCastFinder(41218, 43066, EIData.InstantCastFinder.DefaultICD), // Spectrum Shield
            new DamageCastFinder(43630, 43630, EIData.InstantCastFinder.DefaultICD), // Thermal Release Valve
        };

        internal static readonly List<DamageModifier> DamageMods = new List<DamageModifier>
        {
            new BuffApproximateDamageModifier(44414, "Laser's Edge", "15%", DamageSource.NoPets, 15.0, DamageType.Strike, DamageType.All, Source.Holosmith, ByPresence, "https://wiki.guildwars2.com/images/5/5d/Laser%27s_Edge.png", 0, 97950, DamageModifierMode.PvE),
        };

        internal static readonly List<Buff> Buffs = new List<Buff>
        {
                new Buff("Cooling Vapor",46444, Source.Holosmith, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/b/b1/Coolant_Blast.png"),
                new Buff("Photon Wall Deployed",46094, Source.Holosmith, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/e/ea/Photon_Wall.png"),
                new Buff("Spectrum Shield",43066, Source.Holosmith, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/2/29/Spectrum_Shield.png"),
                new Buff("Photon Forge",43708, Source.Holosmith, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/d/dd/Engage_Photon_Forge.png"),
                new Buff("Laser's Edge",44414, Source.Holosmith, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/5/5d/Laser%27s_Edge.png",0 , 97950),
                new Buff("Afterburner",42210, Source.Holosmith, BuffStackType.StackingConditionalLoss, 25, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/5/51/Solar_Focusing_Lens.png"),
                new Buff("Heat Therapy",40694, Source.Holosmith, BuffStackType.Stacking, 10, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/3/34/Heat_Therapy.png"),
                new Buff("Overheat", 41037, Source.Holosmith, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/4/4b/Overheat.png"),          
                new Buff("Big Boomer", 59601, Source.Holosmith, BuffStackType.Queue, 3, BuffNature.GraphOnlyBuff, "https://wiki.guildwars2.com/images/8/83/Big_Boomer.png"),

        };
    }
}
