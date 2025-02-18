﻿using System;
using System.Collections.Generic;
using System.Linq;
using GW2EIEvtcParser.EIData;
using GW2EIEvtcParser.ParsedData;
using static GW2EIEvtcParser.Extensions.HealingStatsExtensionHandler;

namespace GW2EIEvtcParser.Extensions
{
    public class EXTFinalIncomingHealingStat
    {
        public int Healed { get; internal set; }
        public int HealingPowerHealed { get; internal set; }
        public int ConversionHealed { get; internal set; }
        public int HybridHealed { get; internal set; }

        internal EXTFinalIncomingHealingStat(ParsedEvtcLog log, long start, long end, AbstractSingleActor actor, AbstractSingleActor target)
        {
            foreach (EXTAbstractHealingEvent healingEvent in actor.EXTHealing.GetIncomingHealEvents(target, log, start, end))
            {
                Healed += healingEvent.HealingDone;
                switch (healingEvent.GetHealingType(log))
                {
                    case EXTHealingType.ConversionBased:
                        ConversionHealed += healingEvent.HealingDone;
                        break;
                    case EXTHealingType.Hybrid:
                        HybridHealed += healingEvent.HealingDone;
                        break;
                    case EXTHealingType.HealingPower:
                        HealingPowerHealed += healingEvent.HealingDone;
                        break;
                    default:
                        break;
                }
            }
        }

    }
    
}
