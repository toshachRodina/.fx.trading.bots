using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class AbsoluteStrength : Indicator
    {
        [Parameter("Periods", DefaultValue = 14)]
        public int per { get; set; }
        [Parameter("Smoothing", DefaultValue = 4)]
        public int smt { get; set; }
        [Parameter("Mode", DefaultValue = 1)]
        public int mode { get; set; }

        [Output("Bulls", Color = Colors.DarkGreen)]
        public IndicatorDataSeries BullsSma { get; set; }
        [Output("Bears", Color = Colors.Red)]
        public IndicatorDataSeries BearsSma { get; set; }

        private IndicatorDataSeries bulls, bears;
        private ExponentialMovingAverage avgBulls, avgBears, smtBulls, smtBears;


        protected override void Initialize()
        {
            mode = mode > 2 ? 2 : mode < 1 ? 1 : mode;

            bulls = CreateDataSeries();
            bears = CreateDataSeries();

            avgBulls = Indicators.ExponentialMovingAverage(bulls, per);
            avgBears = Indicators.ExponentialMovingAverage(bears, per);

            smtBulls = Indicators.ExponentialMovingAverage(avgBulls.Result, smt);
            smtBears = Indicators.ExponentialMovingAverage(avgBears.Result, smt);
        }

        public override void Calculate(int index)
        {
            bulls[index] = mode == 1 ? 0.5 * (Math.Abs(MarketSeries.Close[index] - MarketSeries.Close[index - 1]) + (MarketSeries.Close[index] - MarketSeries.Close[index - 1])) : MarketSeries.Close[index] - MarketSeries.Low.Minimum(per);
            bears[index] = mode == 1 ? 0.5 * (Math.Abs(MarketSeries.Close[index] - MarketSeries.Close[index - 1]) - (MarketSeries.Close[index] - MarketSeries.Close[index - 1])) : MarketSeries.High.Maximum(per) - MarketSeries.Close[index];

            BullsSma[index] = smtBulls.Result[index];
            BearsSma[index] = smtBears.Result[index];


        }
    }
}
