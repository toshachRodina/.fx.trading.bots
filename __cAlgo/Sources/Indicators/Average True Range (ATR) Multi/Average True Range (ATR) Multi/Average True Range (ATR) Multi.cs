// -------------------------------------------------------------------------------
//							Average True Range (ATR)
// based on indicator 'True Range' and overlayed by simple Moving Average (SMA)
// -------------------------------------------------------------------------------

using System;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator("Average True Range", IsOverlay = false, ScalePrecision = 5, AccessRights = AccessRights.None)]
    public class AverageTrueRange : Indicator
    {

        [Parameter("Fast Period", DefaultValue = 8)]
        public int FPeriods { get; set; }
        [Parameter("Mid Period", DefaultValue = 14)]
        public int MPeriods { get; set; }
        [Parameter("Slow Period", DefaultValue = 40)]
        public int SPeriods { get; set; }



        [Output("Fast ATR", LineColor = "#008000")]
        public IndicatorDataSeries FATR { get; set; }
        [Output("Mid ATR", LineColor = "#FF595959")]
        public IndicatorDataSeries MATR { get; set; }
        [Output("Slow ATR", LineColor = "FFFF7F50", LineStyle = LineStyle.LinesDots)]
        public IndicatorDataSeries SATR { get; set; }


        private IndicatorDataSeries tr;
        private TrueRange tri;
        private MovingAverage TRMAF;
        private MovingAverage TRMAS;
        private MovingAverage TRMAM;

        protected override void Initialize()
        {
            // Initialize and create nested indicators
            tr = CreateDataSeries();
            tri = Indicators.TrueRange();
            TRMAF = Indicators.MovingAverage(tr, FPeriods, MovingAverageType.Simple);
            TRMAM = Indicators.MovingAverage(tr, MPeriods, MovingAverageType.Simple);
            TRMAS = Indicators.MovingAverage(tr, SPeriods, MovingAverageType.Simple);


        }

        public override void Calculate(int index)
        {
            // Calculate value at specified index            
            tr[index] = tri.Result[index];
            FATR[index] = TRMAF.Result[index];
            MATR[index] = TRMAM.Result[index];
            SATR[index] = TRMAS.Result[index];

        }
    }
}
