using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class DeMACD : Indicator
    {
        private DeMarker longDeMarker;
        private DeMarker shortDeMarker;
        private ExponentialMovingAverage signal;

        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("Long Cycle", DefaultValue = 26, MinValue = 1)]
        public int LongCycle { get; set; }

        [Parameter("Short Cycle", DefaultValue = 12, MinValue = 1)]
        public int ShortCycle { get; set; }

        [Parameter("Signal Periods", DefaultValue = 9, MinValue = 1)]
        public int Periods { get; set; }

        [Output("DeMACD", Color = Colors.Blue, LineStyle = LineStyle.Solid)]
        public IndicatorDataSeries MACD { get; set; }

        [Output("Signal", Color = Colors.Red, LineStyle = LineStyle.Lines)]
        public IndicatorDataSeries Signal { get; set; }

        protected override void Initialize()
        {
            longDeMarker = Indicators.GetIndicator<DeMarker>(LongCycle);
            shortDeMarker = Indicators.GetIndicator<DeMarker>(ShortCycle);
            signal = Indicators.ExponentialMovingAverage(MACD, Periods);
        }

        public override void Calculate(int index)
        {
            MACD[index] = shortDeMarker.Result[index] - longDeMarker.Result[index];
            Signal[index] = signal.Result[index];
        }
    }
}
