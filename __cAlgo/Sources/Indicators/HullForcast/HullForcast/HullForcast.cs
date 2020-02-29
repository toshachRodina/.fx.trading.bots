using System;
using System.IO;
using System.Collections.Generic;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, AutoRescale = false, AccessRights = AccessRights.FullAccess)]
    public class HullForcast : Indicator
    {
        [Parameter("Hull Coverage Period", DefaultValue = 35)]
        public int HullCoveragePeriod { get; set; }

        [Parameter("Hull Coverage Period Devisor", DefaultValue = 1.7)]
        public double HullPeriodDivisor { get; set; }


        [Parameter()]
        public DataSeries Price { get; set; }

        [Parameter("Play Notification", DefaultValue = false)]
        public bool PlayNotifiy { get; set; }

        [Parameter("Notification Sound File", DefaultValue = "")]
        public string Notify { get; set; }


        [Output("Up", PlotType = PlotType.Points, LineColor = "White", Thickness = 4)]
        public IndicatorDataSeries UpSeries { get; set; }

        [Output("Down", PlotType = PlotType.Points, LineColor = "Red", Thickness = 4)]
        public IndicatorDataSeries DownSeries { get; set; }

        private DateTime _openTime;

        private HullMovingAverage _movingAverage1;
        private HullMovingAverage _movingAverage2;
        private HullMovingAverage _movingAverage3;
        private IndicatorDataSeries _dataSeries;
        private IndicatorDataSeries _trend;


        protected override void Initialize()
        {
            _dataSeries = CreateDataSeries();
            _trend = CreateDataSeries();

            var period1 = (int)Math.Floor(HullCoveragePeriod / HullPeriodDivisor);
            var period2 = (int)Math.Floor(Math.Sqrt(HullCoveragePeriod));

            _movingAverage1 = Indicators.GetIndicator<HullMovingAverage>(Price, period1);
            _movingAverage2 = Indicators.GetIndicator<HullMovingAverage>(Price, HullCoveragePeriod);
            _movingAverage3 = Indicators.GetIndicator<HullMovingAverage>(_dataSeries, period2);

        }

        private void PlayNotification()
        {
            if (PlayNotifiy)
            {
                if (!File.Exists(Notify))
                {
                    var windowsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
                    Notifications.PlaySound(Path.Combine(windowsFolder, "Media", "tada.wav"));
                }
                else
                {
                    Notifications.PlaySound(Notify);
                }
            }
        }

        public override void Calculate(int index)
        {
            if (index < 1)
                return;

            _dataSeries[index] = 2.0 * _movingAverage1.Result[index] - _movingAverage2.Result[index];
            _trend[index] = _trend[index - 1];

            if (_movingAverage3.Result[index] > _movingAverage3.Result[index - 1])
                _trend[index] = 1;
            else if (_movingAverage3.Result[index] < _movingAverage3.Result[index - 1])
                _trend[index] = -1;

            if (_trend[index] > 0)
            {
                UpSeries[index] = _movingAverage3.Result[index];

                if (_trend[index - 1] < 0.0)
                {
                    UpSeries[index - 1] = _movingAverage3.Result[index - 1];

                    if (IsLastBar)
                    {
                        if (MarketSeries.OpenTime[index] != _openTime)
                        {
                            _openTime = MarketSeries.OpenTime[index];

                            PlayNotification();
                        }
                    }
                }
                DownSeries[index] = double.NaN;
            }
            else if (_trend[index] < 0)
            {
                DownSeries[index] = _movingAverage3.Result[index];

                if (_trend[index - 1] > 0.0)
                {
                    DownSeries[index - 1] = _movingAverage3.Result[index - 1];

                    if (IsLastBar)
                    {
                        if (MarketSeries.OpenTime[index] != _openTime)
                        {
                            _openTime = MarketSeries.OpenTime[index];
                            PlayNotification();

                        }
                    }
                }

                UpSeries[index] = double.NaN;
            }

        }

    }
}
