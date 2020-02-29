using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class SSL : Indicator
    {
        [Parameter("Length", DefaultValue = 10)]
        public int len { get; set; }

        [Output("SSLDown", LineColor = "Red")]
        public IndicatorDataSeries sslDown { get; set; }
        [Output("SSLUp", LineColor = "Green")]
        public IndicatorDataSeries sslUp { get; set; }

        private SimpleMovingAverage smaHigh, smaLow;
        private IndicatorDataSeries hlv;

        protected override void Initialize()
        {
            smaHigh = Indicators.SimpleMovingAverage(MarketSeries.High, len);
            smaLow = Indicators.SimpleMovingAverage(MarketSeries.Low, len);
            hlv = CreateDataSeries();
        }

        public override void Calculate(int index)
        {
            hlv[index] = MarketSeries.Close[index] > smaHigh.Result[index] ? 1 : MarketSeries.Close[index] < smaLow.Result[index] ? -1 : hlv[index - 1];
            sslDown[index] = hlv[index] < 0 ? smaHigh.Result[index] : smaLow.Result[index];
            sslUp[index] = hlv[index] < 0 ? smaLow.Result[index] : smaHigh.Result[index];
        }
    }
}
