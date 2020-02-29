
using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = false, AccessRights = AccessRights.None)]
    public class RateOfChange : Indicator
    {
        [Parameter()]
        public DataSeries Source { get; set; }

        [Output("Rate Of Change", IsHistogram = true, LineColor = "FF99DFBA")]
        public IndicatorDataSeries roc { get; set; }

        [Output("ROCline", LineColor = "FF65FE66", LineStyle = LineStyle.Solid)]
        public IndicatorDataSeries rocline { get; set; }

        [Output("0", LineColor = "FFFEFF99", LineStyle = LineStyle.LinesDots)]
        public IndicatorDataSeries zero { get; set; }

        [Parameter(DefaultValue = 14)]
        public int Period { get; set; }

        public override void Calculate(int index)
        {
            zero[index] = 0;
            int barsAgo = Math.Min(index, Period);
            roc[index] = (((Source[index] - Source[index - barsAgo]) / Source[index - barsAgo]) * 100);
            rocline[index] = roc[index];
        }
    }
}
