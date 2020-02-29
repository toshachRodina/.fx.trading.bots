using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;
using cAlgo.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class ShowCandles : Indicator
    {
        [Parameter()]
        public TimeFrame CandleTimeFrame { get; set; }

        [Output("H1 CandleHigh", Color = Colors.Teal, LineStyle = LineStyle.Solid, PlotType = PlotType.Line)]
        public IndicatorDataSeries H1CandleHigh { get; set; }

        [Output("H1 CandleLow", Color = Colors.Teal, LineStyle = LineStyle.Solid, PlotType = PlotType.Line)]
        public IndicatorDataSeries H1CandleLow { get; set; }

        MarketSeries h1;

        protected override void Initialize()
        {
            h1 = this.MarketData.GetSeries(CandleTimeFrame);
        }

        int lastH1Index = 0;
        public override void Calculate(int index)
        {
            var h1Index = h1.OpenTime.GetIndexByTime(this.MarketSeries.OpenTime[index]);
            if (lastH1Index != h1Index)
            {
                H1CandleHigh[index] = h1.High[h1Index];
                H1CandleLow[index] = h1.Low[h1Index];
                lastH1Index = h1Index;
            }
            else
            {
                // Update current candle when High/Lows move
                var startOfBar = this.MarketSeries.OpenTime.GetIndexByTime(h1.OpenTime[lastH1Index]);
                while (startOfBar <= index)
                {
                    H1CandleHigh[startOfBar] = h1.High[h1Index];
                    H1CandleLow[startOfBar] = h1.Low[h1Index];
                    startOfBar++;
                }
            }
        }
    }
}
