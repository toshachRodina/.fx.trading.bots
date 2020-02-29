// -------------------------------------------------------------------------------------------------
//
//    This code is a cAlgo API MACD Crossover indicator provided by naoki.shinya@gmail.com on Dec 2015.
//
//    Based on indicator "MACD Crossover with color" from njardim.
//
// -------------------------------------------------------------------------------------------------

using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = false, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class MACDHistogramwithColors : Indicator
    {

        public MacdHistogram MacdHistogram;

        [Parameter()]

        public DataSeries Source { get; set; }

        [Parameter("Long Cycle", DefaultValue = 26)]
        public int LongCycle { get; set; }

        [Parameter("Short Cycle", DefaultValue = 12)]
        public int ShortCycle { get; set; }

        [Parameter("Signal Periods", DefaultValue = 9)]
        public int Periods { get; set; }


        [Output("Histogram > 0", PlotType = PlotType.Histogram, Color = Colors.Green, Thickness = 2)]
        public IndicatorDataSeries HistogramPositive { get; set; }


        [Output("Histogram < 0", PlotType = PlotType.Histogram, Color = Colors.Red, Thickness = 2)]
        public IndicatorDataSeries HistogramNegative { get; set; }

        [Output("Signal", Color = Colors.Purple, LineStyle = LineStyle.Lines)]
        public IndicatorDataSeries Signal { get; set; }

        protected override void Initialize()
        {

            MacdHistogram = Indicators.MacdHistogram(Source, LongCycle, ShortCycle, Periods);
        }

        public override void Calculate(int index)
        {

            if (MacdHistogram.Histogram[index] > 0)
            {
                HistogramPositive[index] = MacdHistogram.Histogram[index];
            }

            if (MacdHistogram.Histogram[index] < 0)
            {
                HistogramNegative[index] = MacdHistogram.Histogram[index];
            }

            Signal[index] = MacdHistogram.Signal[index];
        }
    }
}
