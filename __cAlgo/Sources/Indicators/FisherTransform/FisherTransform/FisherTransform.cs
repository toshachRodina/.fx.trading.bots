using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = false, AccessRights = AccessRights.None)]
    public class FisherTransform : Indicator
    {
        [Output("FisherTransform", LineColor = "Orange")]
        public IndicatorDataSeries Fish { get; set; }

        [Output("Trigger", LineColor = "FF67D097")]
        public IndicatorDataSeries trigger { get; set; }

        [Parameter(DefaultValue = 13, MinValue = 2)]
        public int Len { get; set; }

        double MaxH;
        double MinL;
        private IndicatorDataSeries Value1;

        protected override void Initialize()
        {
            Value1 = CreateDataSeries();
        }
        public override void Calculate(int index)
        {
            if (index <= Len + 1)
            {
                Value1[index - 1] = 1;
                Fish[index - 1] = 0;
            }

            MaxH = MarketSeries.Median.Maximum(Len);
            MinL = MarketSeries.Median.Minimum(Len);
            Value1[index] = 0.5 * 2 * ((MarketSeries.Median[index] - MinL) / (MaxH - MinL) - 0.5) + 0.5 * Value1[index - 1];

            if (Value1[index] > 0.9999)
                Value1[index] = 0.9999;
            else if (Value1[index] < -0.9999)
                Value1[index] = -0.9999;

            Fish[index] = 0.25 * Math.Log((1 + Value1[index]) / (1 - Value1[index])) + 0.5 * Fish[index - 1];
            trigger[index] = Fish[index - 1];
        }
    }
}
