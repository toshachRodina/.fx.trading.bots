using cAlgo.API;

namespace cAlgo.Indicators
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class Blank : Indicator
    {
        protected override void Initialize()
        {
            ChartObjects.DrawText("AlgoDeveloper", "Please download this indicator from AlgoDeveloper.com", StaticPosition.Center, Colors.Red);
        }

        public override void Calculate(int index)
        {
        }
    }
}
