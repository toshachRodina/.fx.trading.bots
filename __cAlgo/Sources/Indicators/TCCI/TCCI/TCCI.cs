using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class TCCI : Indicator
    {
        [Parameter("Price", DefaultValue = 0, MinValue = 0)]
        public int Price { get; set; }

        [Parameter("Length", DefaultValue = 20)]
        public int Length { get; set; }

        [Parameter("Filter", DefaultValue = 0)]
        public int Filter { get; set; }

        [Parameter("Color", DefaultValue = 1)]
        public int Color { get; set; }

        [Parameter("ColorBarBack", DefaultValue = 1)]
        public int ColorBarBack { get; set; }

        [Parameter("Deviation", DefaultValue = 0.0)]
        public double Deviation { get; set; }

        [Output("Down Trend", Color = Colors.Red, PlotType = PlotType.DiscontinuousLine, Thickness = 3)]
        public IndicatorDataSeries downTrend { get; set; }

        [Output("Up Trend", Color = Colors.SkyBlue, PlotType = PlotType.DiscontinuousLine, Thickness = 3)]
        public IndicatorDataSeries upTrend { get; set; }


        int i = 0;
        int int_1 = 0;
        int new_size = 0;
        int int_2 = 4;
        double double_1 = 0;
        double double_2 = 0;
        double double_3 = 0;
        double double_4 = 0;
        double double_5 = 0;
        double double_6 = 0;
        double pi = 3.1415926535;

        IndicatorDataSeries new_array;
        IndicatorDataSeries buf_1;
        IndicatorDataSeries buf_2;


        protected override void Initialize()
        {
            new_array = CreateDataSeries();
            buf_1 = CreateDataSeries();
            buf_2 = CreateDataSeries();

            double_1 = 3.0 * pi;
            int_1 = Length - 1;
            new_size = Length * int_2 + int_1;
            double_5 = 0;

            for (i = 0; i < new_size; i++)
            {
                if (i <= int_1 - 1)
                    double_3 = 1.0 * i / (int_1 - 1);
                else
                    double_3 = (i - int_1 + 1) * (2.0 * int_2 - 1.0) / (int_2 * Length - 1.0) + 1.0;

                double_2 = Math.Cos(pi * double_3);
                double_6 = 1.0 / (double_1 * double_3 + 1.0);
                if (double_3 <= 0.5)
                    double_6 = 1;
                new_array[i] = double_6 * double_2;
                double_5 += new_array[i];
            }
        }

        public override void Calculate(int index)
        {
            int limit = 0;
            double double_7 = 0;
            int counted_bars = index;

            if (counted_bars > 0)
                limit = Bars - counted_bars;

            if (counted_bars < 0)
                return;

            if (counted_bars == 0)
                limit = Bars - new_size - 1;

            if (counted_bars < 1)
            {
                for (int id = 0; id < Length * int_2 + Length; id++)
                {
                    downTrend[id] = 0;
                    upTrend[id] = 0;
                    buf_1[id] = 0;
                }
            }

            double_4 = 0;

            for (int a = 0; a < new_size; a++)
            {
                if (Price == 0)
                    double_7 = MarketSeries.Close.Last(a);
                else
                {
                    if (Price == 1)
                        double_7 = MarketSeries.Open.Last(a);
                    else
                    {
                        if (Price == 2)
                            double_7 = MarketSeries.High.Last(a);
                        else
                        {
                            if (Price == 3)
                                double_7 = MarketSeries.Low.Last(a);
                            else
                            {
                                if (Price == 4)
                                    double_7 = (MarketSeries.High.Last(a) + (MarketSeries.Low.Last(a))) / 2.0;
                                else
                                {
                                    if (Price == 5)
                                        double_7 = (MarketSeries.High.Last(a) + (MarketSeries.Low.Last(a)) + (MarketSeries.Close.Last(a))) / 3.0;
                                    else if (Price == 6)
                                        double_7 = (MarketSeries.High.Last(a) + (MarketSeries.Low.Last(a)) + 2.0 * (MarketSeries.Close.Last(a))) / 4.0;
                                }
                            }
                        }
                    }
                }
                double_4 += new_array[a] * double_7;
            }

            if (double_5 > 0.0)
                buf_1[index] = (Deviation / 100.0 + 1.0) * double_4 / double_5;

            if (Filter > 0)
                if (Math.Abs(buf_1[index] - (buf_1[index - 1])) < Filter * Symbol.TickSize)
                    buf_1[index] = buf_1[index - 1];

            if (Color > 0)
            {
                buf_2[index] = buf_2[index - 1];

                if (buf_1[index] - (buf_1[index - 1]) > Filter * Symbol.TickSize)
                    buf_2[index] = 1;

                if (buf_1[index - 1] - buf_1[index] > Filter * Symbol.TickSize)
                    buf_2[index] = -1;

                if (buf_2[index] > 0.0)
                {
                    upTrend[index] = buf_1[index];

                    if (buf_2[index - ColorBarBack] < 0.0)
                        upTrend[index - ColorBarBack] = buf_1[index - ColorBarBack];

                    downTrend[index] = double.NaN;
                }
                if (buf_2[index] < 0.0)
                {
                    downTrend[index] = buf_1[index];

                    if (buf_2[index - ColorBarBack] > 0.0)
                        downTrend[index - ColorBarBack] = buf_1[index - ColorBarBack];

                    upTrend[index] = double.NaN;
                }
            }
        }

        private int Bars
        {
            get { return MarketSeries.Close.Count; }
        }

    }
}
