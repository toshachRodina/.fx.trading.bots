
using System;
using cAlgo.API;
using cAlgo.API.Indicators;
namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = false, AccessRights = AccessRights.None)]
    public class CHOP : Indicator
    {
        [Output("Result", Color = Colors.Yellow)]
        public IndicatorDataSeries Result { get; set; }

        [Output("rangebound", Color = Colors.Turquoise)]
        public IndicatorDataSeries rangebound { get; set; }

        [Output("trending", Color = Colors.Red)]
        public IndicatorDataSeries trending { get; set; }

        [Parameter(DefaultValue = MovingAverageType.Simple)]
        public MovingAverageType atrMaType { get; set; }

        [Parameter(DefaultValue = 14, MinValue = 2)]
        public int Period { get; set; }

        [Parameter(DefaultValue = 38.2, Step = 1)]
        public double lowerLimit { get; set; }

        [Parameter(DefaultValue = 61.8, Step = 1)]
        public double upperLimit { get; set; }

        private AverageTrueRange _atr;


        protected override void Initialize()
        {
            _atr = Indicators.AverageTrueRange(Period, atrMaType);
        }

        public override void Calculate(int index)
        {
            rangebound[index] = upperLimit;
            trending[index] = lowerLimit;
            Result[index] = (100 * Math.Log10(_atr.Result.Sum(Period) / (MarketSeries.High.Maximum(Period) - MarketSeries.Low.Minimum(Period)))) / Math.Log10(Period);
        }
    }
}
