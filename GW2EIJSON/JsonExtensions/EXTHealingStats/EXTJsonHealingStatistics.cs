﻿

namespace GW2EIJSON
{
    /// <summary>
    /// Class representing general statistics
    /// </summary>
    public static class EXTJsonHealingStatistics
    {
        /// <summary>
        /// Outgoing healing statistics
        /// </summary>
        public class EXTJsonOutgoingHealingStatistics
        {
            /// <summary>
            /// Total hps
            /// </summary>
            public int Hps { get; set; }
            /// <summary>
            /// Total healing
            /// </summary>
            public int Healing { get; set; }
            /// <summary>
            /// Total healing power based hps
            /// </summary>
            public int HealingPowerHps { get; set; }
            /// <summary>
            /// Total healing power based healing
            /// </summary>
            public int HealingPowerHealing { get; set; }
            /// <summary>
            /// Total conversion based hps
            /// </summary>
            public int ConversionHps { get; set; }
            /// <summary>
            /// Total conversion based healing
            /// </summary>
            public int ConversionHealing { get; set; }
            /// <summary>
            /// Total hybrid hps
            /// </summary>
            public int HybridHps { get; set; }
            /// <summary>
            /// Total hybrid healing
            /// </summary>
            public int HybridHealing { get; set; }


            /// <summary>
            /// Total actor only hps
            /// </summary>
            public int ActorHps { get; set; }
            /// <summary>
            /// Total actor only healing
            /// </summary>
            public int ActorHealing { get; set; }
            /// <summary>
            /// Total actor only healing power based hps
            /// </summary>
            public int ActorHealingPowerHps { get; set; }
            /// <summary>
            /// Total actor only healing power based healing
            /// </summary>
            public int ActorHealingPowerHealing { get; set; }
            /// <summary>
            /// Total actor only conversion based hps
            /// </summary>
            public int ActorConversionHps { get; set; }
            /// <summary>
            /// Total actor only conversion based healing
            /// </summary>
            public int ActorConversionHealing { get; set; }
            /// <summary>
            /// Total actor only hybrid hps
            /// </summary>
            public int ActorHybridHps { get; set; }
            /// <summary>
            /// Total actor only hybrid healing
            /// </summary>
            public int ActorHybridHealing { get; set; }

            public EXTJsonOutgoingHealingStatistics()
            {

            }
        }

        /// <summary>
        /// Incoming healing statistics
        /// </summary>
        public class EXTJsonIncomingHealingStatistics
        {
            /// <summary>
            /// Total received healing
            /// </summary>
            public int Healed { get; set; }
            /// <summary>
            /// Total received healing power based healing
            /// </summary>
            public int HealingPowerHealed { get; set; }
            /// <summary>
            /// Total received conversion based healing
            /// </summary>
            public int ConversionHealed { get; set; }
            /// <summary>
            /// Total received hybrid healing
            /// </summary>
            public int HybridHealed { get; set; }

            public EXTJsonIncomingHealingStatistics()
            {

            }
        }
    }
}
