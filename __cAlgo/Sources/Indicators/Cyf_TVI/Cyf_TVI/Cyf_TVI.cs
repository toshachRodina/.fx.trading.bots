using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Cyf_TVI : Indicator
    {
        private IndicatorDataSeries UpTick;
        private IndicatorDataSeries DnTick;
        private IndicatorDataSeries TVI_Calculate;

        private ExponentialMovingAverage EMA_UpTick;
        private ExponentialMovingAverage EMA_DnTick;

        private ExponentialMovingAverage DEMA_UpTick;
        private ExponentialMovingAverage DEMA_DnTick;

        private ExponentialMovingAverage TVI;

        [Parameter("r", DefaultValue = 12)]
        public int EMA { get; set; }

        [Parameter("s", DefaultValue = 12)]
        public int DEMA { get; set; }

        [Parameter("u", DefaultValue = 5)]
        public int TEMA { get; set; }

        [Output("TVI_Up", PlotType = PlotType.Histogram, Color = Colors.AliceBlue, Thickness = 3)]
        public IndicatorDataSeries TVI_Draw_Up { get; set; }

        [Output("TVI_Dn", PlotType = PlotType.Histogram, Color = Colors.Red, Thickness = 3)]
        public IndicatorDataSeries TVI_Draw_Dn { get; set; }


        protected override void Initialize()
        {
            UpTick = CreateDataSeries();
            DnTick = CreateDataSeries();
            TVI_Calculate = CreateDataSeries();

            EMA_UpTick = Indicators.ExponentialMovingAverage(UpTick, EMA);
            EMA_DnTick = Indicators.ExponentialMovingAverage(DnTick, EMA);

            DEMA_UpTick = Indicators.ExponentialMovingAverage(EMA_UpTick.Result, DEMA);
            DEMA_DnTick = Indicators.ExponentialMovingAverage(EMA_DnTick.Result, DEMA);

            TVI = Indicators.ExponentialMovingAverage(TVI_Calculate, TEMA);
        }

        public override void Calculate(int index)
        {
            UpTick[index] = (MarketSeries.TickVolume[index] + (MarketSeries.Close[index] - MarketSeries.Open[index]) / Symbol.TickSize) / 2;
            DnTick[index] = MarketSeries.TickVolume[index] - UpTick[index];

            TVI_Calculate[index] = 100 * ((DEMA_UpTick.Result[index] - DEMA_DnTick.Result[index]) / (DEMA_UpTick.Result[index] + DEMA_DnTick.Result[index]));

            if (TVI.Result[index] > TVI.Result[index - 1])
            {
                TVI_Draw_Up[index] = TVI.Result[index];
            }
            else
            {
                TVI_Draw_Dn[index] = TVI.Result[index];
            }
        }
    }
}
