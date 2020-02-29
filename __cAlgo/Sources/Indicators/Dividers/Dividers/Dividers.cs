using System;
using cAlgo.API;
using cAlgo.API.Internals;
using cAlgo.API.Indicators;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC)]
    public class Dividers : Indicator
    {
        [Parameter(DefaultValue = false)]
        public bool HideWeekDividers { get; set; }
        [Parameter(DefaultValue = false)]
        public bool HideDayDividers { get; set; }
        [Parameter(DefaultValue = false)]
        public bool HideDayLabels { get; set; }

        [Output("Dayend", PlotType = PlotType.Points, Thickness = 1, Color = Colors.Orange)]
        public IndicatorDataSeries Dayend { get; set; }
        [Output("Weekend", PlotType = PlotType.Points, Thickness = 1, Color = Colors.Red)]
        public IndicatorDataSeries Weekend { get; set; }

        private int PeriodDivisor;
        private int PeriodsPerDay;

        protected override void Initialize()
        {
            switch (Convert.ToString(TimeFrame))
            {
                case "Minute":
                    PeriodDivisor = 1;
                    break;
                case "Minute5":
                    PeriodDivisor = 5;
                    break;
                case "Minute10":
                    PeriodDivisor = 10;
                    break;
                case "Minute15":
                    PeriodDivisor = 15;
                    break;
                case "Minute30":
                    PeriodDivisor = 30;
                    break;
                case "Hour":
                    PeriodDivisor = 60;
                    break;
                default:
                    PeriodDivisor = 120;
                    break;
            }
            PeriodsPerDay = 720 / PeriodDivisor;
            //Used to place label
        }

        public override void Calculate(int index)
        {
            if (index - 1 < 0) return;

            DateTimeOffset CurrentDate = TradeDate(MarketSeries.OpenTime[index]);
            DateTimeOffset PreviousDate = TradeDate(MarketSeries.OpenTime[index - 1]);

            int DateDifference = (int)(CurrentDate.Date - PreviousDate.Date).TotalDays;

            int myOffset=(int)(CurrentDate.Offset.Hours+7);
            if (TimeFrame < TimeFrame.Hour12)
            {
//*** Dayend ***
                if ((CurrentDate.DayOfWeek != PreviousDate.DayOfWeek || DateDifference > 6) && PreviousDate.DayOfWeek != DayOfWeek.Sunday && CurrentDate.DayOfWeek != DayOfWeek.Saturday)
                {
                    if (!HideDayLabels)
                        ChartObjects.DrawText("DayLabel" + index, " " + CurrentDate.DayOfWeek + (myOffset>0?" +":" ") + myOffset, index, MarketSeries.Low.Minimum(PeriodsPerDay), VerticalAlignment.Bottom, HorizontalAlignment.Right);

                    if (!HideDayDividers)
                        ChartObjects.DrawVerticalLine("Dayend" + index, index, Colors.Orange, 1, LineStyle.DotsRare);

                    Dayend[index] = MarketSeries.Median[index];
                    //Print("CurDayOfWeek:"+CurrentDate.DayOfWeek+" CurrDate:"+CurrentDate.Date+" Diff:"+DateDifference);
                }
            }

//*** Weekend ***
            if (TimeFrame < TimeFrame.Weekly)
            {
                if (CurrentDate.DayOfWeek < PreviousDate.DayOfWeek || DateDifference > 6)
                {
                    if (!HideWeekDividers)
                        ChartObjects.DrawVerticalLine("Weekend" + index, index, Colors.Red, 1, LineStyle.DotsRare);

                    Weekend[index] = MarketSeries.Median[index];
                }
            }
        }



        DateTimeOffset TradeDate(DateTime d)
        {
            DateTimeOffset CurrentDateTime = DateTime.SpecifyKind(d, DateTimeKind.Utc);
            CurrentDateTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(CurrentDateTime, "Eastern Standard Time");
            CurrentDateTime = CurrentDateTime.AddHours(7);
            //return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(CurrentDateTime, "Arab Standard Time");
            return CurrentDateTime;
            //return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(CurrentDateTime, "E. Europe Standard Time");
        }
    }
}
