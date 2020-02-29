using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class WaddahAttarExplosion : Indicator
    {
        [Parameter("Sensetive", DefaultValue = 150)]
        public int Sensetive { get; set; }

        [Parameter("DeadZonePip", DefaultValue = 30)]
        public int DeadZonePip { get; set; }

        [Parameter("ExplosionPower", DefaultValue = 3)]
        public int ExplosionPower { get; set; }

        [Parameter("TrendPower", DefaultValue = 15)]
        public int TrendPower { get; set; }

        [Output("ind_Trend1", Color = Colors.Lime, PlotType = PlotType.Histogram, Thickness = 4)]
        public IndicatorDataSeries ind_Trend1 { get; set; }

        [Output("ind_iTrend1", Color = Colors.Red, PlotType = PlotType.Histogram, Thickness = 4)]
        public IndicatorDataSeries ind_iTrend1 { get; set; }

        [Output("ind_Explo1", Color = Colors.Sienna, Thickness = 2)]
        public IndicatorDataSeries ind_Explo1 { get; set; }

        [Output("ind_Dead", Color = Colors.Blue, Thickness = 1)]
        public IndicatorDataSeries ind_Dead { get; set; }

        public IndicatorDataSeries Result;

        private MacdCrossOver iMACD;
        private BollingerBands iBands;

        private double Trend1, Trend2, Explo1, Explo2, Dead;

        protected override void Initialize()
        {
            Result = CreateDataSeries();

            iMACD = Indicators.MacdCrossOver(26, 12, 9);
            iBands = Indicators.BollingerBands(MarketSeries.Close, 20, 2, MovingAverageType.Simple);
        }

        public override void Calculate(int index)
        {
            Trend1 = (iMACD.MACD[index] - iMACD.MACD[index - 1]) * Sensetive;
            Trend2 = (iMACD.MACD[index - 1] - iMACD.MACD[index - 2]) * Sensetive;

            Explo1 = (iBands.Top[index] - iBands.Bottom[index]);
            Explo2 = (iBands.Top[index - 1] - iBands.Bottom[index - 1]);

            Dead = Symbol.PipSize * DeadZonePip / 10;

            if (Trend1 >= 0 && iMACD.Histogram[index] > 0)
            {
                ind_Trend1[index] = Trend1;
                ind_iTrend1[index] = 0;
            }

            if (Trend1 < 0 && iMACD.Histogram[index] < 0)
            {
                ind_iTrend1[index] = (-1 * Trend1);
                ind_Trend1[index] = 0;
            }

            ind_Explo1[index] = Explo1;
            ind_Dead[index] = Dead;
        }
    }
}
