using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class SqueezeBreak : Indicator
    {
        [Parameter("Bollinger Period", DefaultValue = 20)]
        public int Boll_Period { get; set; }

        [Parameter("Bollinger Dev", DefaultValue = 2)]
        public double Boll_Dev { get; set; }

        [Parameter("Keltner Period", DefaultValue = 20)]
        public int Keltner_Period { get; set; }

        [Parameter("Keltner Mul", DefaultValue = 1.5)]
        public double Keltner_Mul { get; set; }

        [Parameter("Momentum_Period", DefaultValue = 12)]
        public int Momentum_Period { get; set; }

        [Output("Up", Color = Colors.ForestGreen, PlotType = PlotType.Histogram, Thickness = 4)]
        public IndicatorDataSeries Up { get; set; }

        [Output("Down", Color = Colors.Red, PlotType = PlotType.Histogram, Thickness = 4)]
        public IndicatorDataSeries Down { get; set; }

        [Output("Momentum", Color = Colors.Yellow, LineStyle = LineStyle.Solid, Thickness = 1)]
        public IndicatorDataSeries Momentum { get; set; }


        private MovingAverage MA1;
        private MovingAverage MA2;
        private MovingAverage MA3;
        private BollingerBands iBand;

        double MA_Hi = 0;
        double MA_Lo = 0;
        double Kelt_Mid_Band = 0;
        double Kelt_Upper_Band = 0;
        double Kelt_Lower_Band = 0;
        double Boll_Upper_Band = 0;
        double Boll_Lower_Band = 0;

        protected override void Initialize()
        {
            MA1 = Indicators.MovingAverage(MarketSeries.High, Keltner_Period, MovingAverageType.Simple);
            MA2 = Indicators.MovingAverage(MarketSeries.Low, Keltner_Period, MovingAverageType.Simple);
            MA3 = Indicators.MovingAverage(MarketSeries.Typical, Keltner_Period, MovingAverageType.Simple);
            iBand = iBand = Indicators.BollingerBands(MarketSeries.Close, Boll_Period, Boll_Dev, MovingAverageType.Simple);
        }

        public override void Calculate(int index)
        {
            MA_Hi = MA1.Result[index];
            MA_Lo = MA2.Result[index];
            Kelt_Mid_Band = MA3.Result[index];
            Kelt_Upper_Band = Kelt_Mid_Band + ((MA_Hi - MA_Lo) * Keltner_Mul);
            Kelt_Lower_Band = Kelt_Mid_Band - ((MA_Hi - MA_Lo) * Keltner_Mul);
            Boll_Upper_Band = iBand.Top[index];
            Boll_Lower_Band = iBand.Bottom[index];

            Momentum[index] = MarketSeries.Close[index] - MarketSeries.Close[index - Momentum_Period];

            if (Boll_Upper_Band >= Kelt_Upper_Band || Boll_Lower_Band <= Kelt_Lower_Band)
            {
                Up[index] = (Math.Abs(Boll_Upper_Band - Kelt_Upper_Band) + Math.Abs(Boll_Lower_Band - Kelt_Lower_Band));
            }
            else
            {
                Up[index] = 0;
            }


            if (Boll_Upper_Band < Kelt_Upper_Band && Boll_Lower_Band > Kelt_Lower_Band)
            {
                Down[index] = -(Math.Abs(Boll_Upper_Band - Kelt_Upper_Band) + Math.Abs(Boll_Lower_Band - Kelt_Lower_Band));
            }
            else
            {
                Down[index] = 0;
            }
        }
    }
}
