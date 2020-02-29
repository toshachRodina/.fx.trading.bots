/* 
=====================================================================================================================================================
SUBJECT      : risk management technical trading
OBJECT TYPE  : cAlgo
OBJECT NAME  : LTN longTrendItchyBoll
CREATED BY   : Harold Delaney
CREATED ON   : 20190408
SOURCE       : 
PREPERATION  : 
			   
REMARKS      : 1) 
               2) 
               3) 

NOTES        : * may need different strategies for different pairs
               IN -----
               * determine market type using ichimoku (BULLISH, BEARISH or RANGE)
               * get in at top or bottom of bollinger bands depending (relative to market type
               * ensure RSI is at a level which allows room to move in relevant direction
               * RSI direction (up/down)?
               * acceleration/velocity and size of movement?  may indicate bounce in opposite direction - check where previous price levels sit
               * set stop loss based on BOLLINGER bands or ICHIMOKU depending on "aggressiveness"
               
               OUT ----
               * out is based on BOLLINGER BAND +/- threshold
               * may be different depending on market conditions - eg. tighter SL if certain conditions are met (volatile), wider if in gentle trend
               
STRATEGY     : EURUSD
               - m30
               - BEAR MARKET - IN ---
               * when ICHIMOKU CLOUD (CURRENT) is BEARISH ... AND ...
               * price CLOSES below SENKOU SPAN A ... AND ...
               * price closes below MAIN bollinger band ... AND ...
               * TENKAN SEN is below the KIJUN SEN


=====================================================================================================================================================
*/

#region referenced assemblies =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using tradeLibrary;

#endregion

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class LTNlongTrendItchyBoll : Robot
    {

        #region USER DEFINED PARAMATERS ===================================================================

        // INSTANCE & TRADE PARAMS ================================================================
        [Parameter("Instance Name", DefaultValue = "001")]
        public string InstanceName { get; set; }

        [Parameter("Calculate OnBar", DefaultValue = true)]
        public bool CalculateOnBar { get; set; }

        [Parameter("Bar History", DefaultValue = 50, MinValue = 50, MaxValue = 2000, Step = 50)]
        public int BarHistory { get; set; }

        [Parameter("(A)lert or (T)rade?", DefaultValue = "A")]
        public string AlertOrTrade { get; set; }

        [Parameter("Lot Size", DefaultValue = 0.2, MinValue = 0.01, MaxValue = 2.0, Step = 0.1)]
        public double LotSize { get; set; }

        // EMAIL PARAMATERS =======================================================================
        [Parameter("-- EMAIL DETAILS ----------------------", DefaultValue = "")]
        public string DoNothing01 { get; set; }

        [Parameter("From Email", DefaultValue = "lanoitan17@gmail.com")]
        public String FromEmail { get; set; }

        [Parameter("To Email", DefaultValue = "toshach@gmail.com")]
        public String ToEmail { get; set; }

        // TRAILING STOP PARAMS ===================================================================
        [Parameter("-- TRAILING STOPS ---------------------", DefaultValue = "")]
        public string DoNothing02 { get; set; }

        [Parameter("Include Trailing Stop", DefaultValue = false)]
        public bool IncludeTrailingStop { get; set; }

        [Parameter("Trailing Stop Trigger (pips)", DefaultValue = 20)]
        public int TrailingStopTrigger { get; set; }

        [Parameter("Trailing Stop Step (pips)", DefaultValue = 10)]
        public int TrailingStopStep { get; set; }

        // TAKE PROFIT PARAMS =====================================================================
        [Parameter("-- TAKE PROFIT / BREAK EVEN -----------", DefaultValue = "")]
        public string DoNothing03 { get; set; }

        [Parameter("Include Take Profit", DefaultValue = false)]
        public bool IncludeTakeProfit { get; set; }

        [Parameter("Take Profit Trigger (pips)", DefaultValue = 0)]
        public int TakeProfitTrigger { get; set; }

        // BREAK EVEN PARAMS ======================================================================
        [Parameter("Include Break Even", DefaultValue = false)]
        public bool IncludeBreakEven { get; set; }

        [Parameter("Break-Even Trigger (pips)", DefaultValue = 10, MinValue = 1)]
        public int BreakEvenPips { get; set; }

        [Parameter("Break-Even Extra (pips)", DefaultValue = 2, MinValue = 1)]
        public int BreakEvenExtraPips { get; set; }

        // INDICATOR PARAMATERS ===================================================================
        [Parameter("-- INDICATORS -------------------------", DefaultValue = "")]
        public string DoNothing04 { get; set; }

        // ICHIMOKU ------------------------------
        [Parameter("Tenkan Sen Periods", DefaultValue = 9, MinValue = 1, MaxValue = 17)]
        public int TenkanSen { get; set; }

        [Parameter("Kijun Sen Periods", DefaultValue = 26, MinValue = 12, MaxValue = 40)]
        public int KijunSen { get; set; }

        [Parameter("Senkou Span B Periods", DefaultValue = 52, MinValue = 42, MaxValue = 62)]
        public int SenkouSpanB { get; set; }

        [Parameter("Min. Kimu Cloud Depth", DefaultValue = 0.002, MinValue = 0.001, MaxValue = 1.5, Step = 0.001)]
        public double MinCloudDepth { get; set; }

        // BOLLINGER -----------------------------
        [Parameter("Bollinger Periods", DefaultValue = 20, MinValue = 10, MaxValue = 30)]
        public int BollPeriod { get; set; }

        [Parameter("Bollinger StdDev", DefaultValue = 2, MinValue = 1, MaxValue = 9)]
        public int BollSD { get; set; }

        [Parameter("Bollinger Source")]
        public DataSeries BollDS { get; set; }

        [Parameter("Bollinger MAType")]
        public MovingAverageType BollMAT { get; set; }

        #endregion


        #region PRIVATE INDICATOR DECLARATIONS ============================================================

        private IchimokuKinkoHyo IndIchi { get; set; }
        private BollingerBands IndBoll { get; set; }

        private TradeManager tm;
        private double l_slPips;
        private long l_tradeVol;

        public int? stopLoss;
        public int? takeProfit;

        #endregion


        #region cTRADER EVENTS ============================================================================
        /// <summary>
        /// CALLED WHEN THE ROBOT FIRST STARTS, IT IS ONLY CALLED ONCE.
        /// </summary>
        protected override void OnStart()
        {
            // TRADE MANAGER DECLERATION
            tm = new TradeManager(this);
            // INDICATOR DECLERATIONS
            IndIchi = Indicators.IchimokuKinkoHyo(TenkanSen, KijunSen, SenkouSpanB);
            IndBoll = Indicators.BollingerBands(BollDS, BollPeriod, BollSD, BollMAT);
        }

        /// <summary>
        /// CALLED ONTICK WHEN CALCULATE ON BAR IS SET TO FALSE
        /// </summary>
        protected override void OnTick()
        {
            // ONLY RUN LOGIC IF PARAMATER CalculateOnBar IS SET FALSE (EG. WE WANT TO MANAGE POSITIONS ON THE TICK EVENT)
            if (CalculateOnBar)
            {
                return;
            }

            ManagePositions();
        }

        /// <summary>
        /// CALLED ONBAR COMPLETION WHEN CALCULATE ON BAR IS SET TO TRUE
        /// </summary>
        protected override void OnBar()
        {
            // ONLY RUN LOGIC IF PARAMATER CalculateOnBar IS SET TRUE (EG. WE WANT TO MANAGE POSITIONS ON THE BAR CLOSE EVENT)
            if (!CalculateOnBar)
            {
                return;
            }

            ManagePositions();
        }
        /// <summary>
        /// CALLED WHEN BOT IS STOPPED (PERFORM GARBAGE COLLECTION AND TIDY UP)
        /// </summary>
        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }

        #endregion


        #region POSITION MANAGEMENT =======================================================================

        private void ManagePositions()
        {
            // COLLECT & STORE HISTORICAL ONBAR ELEMENTS IN DICTIONARIES/LISTS
            // TKey   - BAR NUMBER (EG. 0 = CURRENT BAR, 1 = CURRENT - 1 BAR, 2 = CURRENT - 2 BARS ETC)
            // TValue - ACTUAL VALUE OF BAR AT POSITION

            // OPEN/CLOSE PRICE
            Dictionary<int, double> priceOpenDict = new Dictionary<int, double>();
            Dictionary<int, double> priceCloseDict = new Dictionary<int, double>();

            // ICHIMOKU KINKO HYO
            Dictionary<int, double> tenkanSenDict = new Dictionary<int, double>();
            Dictionary<int, double> kijunSenDict = new Dictionary<int, double>();
            Dictionary<int, double> senkouSpanADict = new Dictionary<int, double>();
            Dictionary<int, double> senkouSpanBDict = new Dictionary<int, double>();
            Dictionary<int, double> chikouSpanDict = new Dictionary<int, double>();

            // BOLLINGER BANDS
            Dictionary<int, double> bollMainDict = new Dictionary<int, double>();
            Dictionary<int, double> bollTopDict = new Dictionary<int, double>();
            Dictionary<int, double> bollBottomDict = new Dictionary<int, double>();

            for (int i = 0; i < BarHistory; i++)
            {
                // ADD KEY/VALUE PAIRS FOR EACH INDICATOR INTO DICTIONARIES 
                priceOpenDict.Add(i, MarketSeries.Open.Last(i));
                priceCloseDict.Add(i, MarketSeries.Close.Last(i));

                tenkanSenDict.Add(i, IndIchi.TenkanSen.Last(i));
                kijunSenDict.Add(i, IndIchi.KijunSen.Last(i));
                senkouSpanADict.Add(i, IndIchi.SenkouSpanA.Last(i));
                senkouSpanBDict.Add(i, IndIchi.SenkouSpanB.Last(i));
                chikouSpanDict.Add(i, IndIchi.ChikouSpan.Last(i));

                bollMainDict.Add(i, IndBoll.Main.Last(i));
                bollTopDict.Add(i, IndBoll.Top.Last(i));
                bollBottomDict.Add(i, IndBoll.Bottom.Last(i));
            }

            // CALCULATIONS
            var cloudDepth = Math.Round((senkouSpanADict[27] - senkouSpanBDict[27]), 5);
            var cloudDirection;

            if (cloudDepth < 0 - MinCloudDepth)
            {
                cloudDirection = "Bearish";
            }
            else if (cloudDepth > 0 + MinCloudDepth)
            {
                cloudDirection = "Bullish";
            }
            else
            {
                cloudDirection = "Sideways";
            }

            // IF 0 CLOUD IS IN RIGT DRECTION FOR 2 BARS - THEN ENTER


                        /*
            // INITIALISE ELEMENTS ONBAR CLOSE
            // INDEX: 0 - CURRENT LIVE BAR (NOT FINALISED)
            //        1 - MOST RECENT FINALISED CLOSE BAR
            //        2 - 2ND MOST RECENT FINALISED CLOSE BAR

            // ======================================================================================================================================
            // CURRENT COMPLETE PERIOD (-1 bar from current) 
            // ======================================================================================================================================
            var indTenkanSen_curr = IndIchi.TenkanSen.Last(1);
            var indKijunSen_curr = IndIchi.KijunSen.Last(1);
            // minus 26 - 1 (27) for -1 bar 
            var indSenkouSpanA_curr = IndIchi.SenkouSpanA.Last(27);
            var indSenkouSpanB_curr = IndIchi.SenkouSpanB.Last(27);
            var indChikouSpan_curr = IndIchi.ChikouSpan.Last(0);

            // ======================================================================================================================================
            // CURRENT COMPLETE PERIOD (-2 bar from current) 
            // ======================================================================================================================================
            var indTenkanSen_prev = IndIchi.TenkanSen.Last(2);
            var indKijunSen_prev = IndIchi.KijunSen.Last(2);
            // minus 27 - 1 (28) for CURR-1 bar 
            var indSenkouSpanA_prev = IndIchi.SenkouSpanA.Last(28);
            var indSenkouSpanB_prev = IndIchi.SenkouSpanB.Last(28);
            var indChikouSpan_prev = IndIchi.ChikouSpan.Last(1);

            // ======================================================================================================================================
            // LAST 5 BARS OF THE KUMO CLOUD
            // ======================================================================================================================================
            // 0 shows end of cloud -0 periods (26 periods in future) 
            var indSenkouSpanA_futr0 = IndIchi.SenkouSpanA.Last(0);
            var indSenkouSpanB_futr0 = IndIchi.SenkouSpanB.Last(0);
            // 1 shows end of cloud -1 periods (25 periods in future) 
            var indSenkouSpanA_futr1 = IndIchi.SenkouSpanA.Last(1);
            var indSenkouSpanB_futr1 = IndIchi.SenkouSpanB.Last(1);
            // 2 shows end of cloud -2 periods (24 periods in future) 
            var indSenkouSpanA_futr2 = IndIchi.SenkouSpanA.Last(2);
            var indSenkouSpanB_futr2 = IndIchi.SenkouSpanB.Last(2);
            // 3 shows end of cloud -3 periods (23 periods in future) 
            var indSenkouSpanA_futr3 = IndIchi.SenkouSpanA.Last(3);
            var indSenkouSpanB_futr3 = IndIchi.SenkouSpanB.Last(3);
            // 4 shows end of cloud -4 periods (22 periods in future) 
            var indSenkouSpanA_futr4 = IndIchi.SenkouSpanA.Last(4);
            var indSenkouSpanB_futr4 = IndIchi.SenkouSpanB.Last(4);
            */

            // TOTAL COUNT OF TICK VOLUME SERIES
            // LAST TICK VOLUME !!!!!!!!! I DONT THINK THIS IS CORRECT !!!!!!!!!
var count = MarketSeries.TickVolume.Count;
            // int
            var _volume = MarketSeries.TickVolume[count - 1];
            var position = Positions.Find(InstanceName, Symbol);

                        /* ================================================================
            USE THE FOLLOWING TO CONVERT UNITS TO LOTS - REQUIRED FOR OPENING MARKET ORDERS

            double lots = 5.34;
            long volume = Symbol.QuantityToVolumeInUnits(lots);
            Print(volume);
            */

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // BELOW THIS POINT TRADES WILL BE MADE IF AlertOrTrade IS SET TO "T"
            // * TEST VARIABLES AND VALUES ABOVE THIS SECTION
            // 
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            #region EXIT LOGIC ========================================================================

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!



            #endregion

            #region ENTRY LOGIC =======================================================================

var cloudDepth = Math.Round((indSenkouSpanA_curr - indSenkouSpanB_curr), 5);

            if (Math.Abs(cloudDepth) > MinCloudDepth)
            {
                // POSITIVE CLOUD - CHICKOU SPAN A APPEARS ABOVE CHICKOU SPAN B
                if (cloudDepth > 0.0)
                {
                    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    // LONG LOGIC - BULLISH CLOUD
                    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                    // CLOSE PRICE ABOVE SPANA AND TENKEN SEN CROSSES ABOVE KIJUN SEN (PREVIOUS TENKAN SEN BELOW AND CROSSES ABOVE THE KIJUN SEN)
                    if (MarketSeries.Close.Last(1) > indSenkouSpanA_curr && indTenkanSen_prev < indKijunSen_curr && indTenkanSen_curr > indKijunSen_curr)
                    {
                        if (AlertOrTrade == "T")
                        {
                            tm.OpenPosition("market", true, 0, TradeType.Buy, _tradeVol, InstanceName, null, null, null);
                        }
                        else
                        {
                            Print("BUY LONG -------------------------------------------------------------------");
                            //Print("Current Balance is {0}, Equity is {1}.", Account.Balance, Account.Equity);
                            Print("Previous Indicators - TenkenSen ({0}); KijunSen ({1}); ChickouSpan ({2}); SpanA ({3}); SpanB({4})", Math.Round(indTenkanSen_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indKijunSen_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indChikouSpan_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanA_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanB_prev, 3, MidpointRounding.AwayFromZero));
                            Print("Current Indicators  - TenkenSen ({0}); KijunSen ({1}); ChickouSpan ({2}); SpanA ({3}); SpanB({4})", Math.Round(indTenkanSen_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indKijunSen_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indChikouSpan_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanA_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanB_curr, 3, MidpointRounding.AwayFromZero));
                            Print("Bullish Cloud - Cloud Depth [ {0} pips ]", cloudDepth);
                        }
                    }
                    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    // SHORT LOGIC - BEARISH CLOUD
                    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                    // CLOSE PRICE BELOW SPANB AND TENKEN SEN CROSSES BELOW KIJUN SEN (PREVIOUS TENKAN SEN ABOVE AND CROSSES BELOW THE KIJUN SEN)
                    else if (MarketSeries.Close.Last(1) < indSenkouSpanB_curr && indTenkanSen_prev > indKijunSen_prev && indTenkanSen_curr < indKijunSen_curr)
                    {
                        if (IsAutoTrading)
                        {
                            tm.OpenPosition("market", true, 0, TradeType.Buy, _tradeVol, InstanceName, null, null, null);
                        }
                        else
                        {
                            Print("SELL SHORT -------------------------------------------------------------------");
                            Print("Previous Indicators - TenkenSen ({0}); KijunSen ({1}); ChickouSpan ({2}); SpanA ({3}); SpanB({4})", Math.Round(indTenkanSen_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indKijunSen_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indChikouSpan_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanA_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanB_prev, 3, MidpointRounding.AwayFromZero));
                            Print("Current Indicators  - TenkenSen ({0}); KijunSen ({1}); ChickouSpan ({2}); SpanA ({3}); SpanB({4})", Math.Round(indTenkanSen_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indKijunSen_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indChikouSpan_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanA_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanB_curr, 3, MidpointRounding.AwayFromZero));
                            Print("Bullish Cloud - Cloud Depth [ {0} pips ]", cloudDepth);
                        }
                    }
                }
                else if (cloudDepth < 0)
                {
                    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    // LONG LOGIC - NEGATIVE CLOUD
                    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                    // CLOSE PRICE ABOVE SPANB AND TENKEN SEN CROSSES ABOVE KIJUN SEN (PREVIOUS TENKAN SEN BELOW AND CROSSES ABOVE THE KIJUN SEN)
                    if (MarketSeries.Close.Last(1) > indSenkouSpanB_curr && indTenkanSen_prev < indKijunSen_curr && indTenkanSen_curr > indKijunSen_curr)
                    {
                        if (IsAutoTrading)
                        {
                            tm.OpenPosition("market", true, 0, TradeType.Buy, _tradeVol, InstanceName, null, null, null);
                        }
                        else
                        {
                            Print("BUY LONG -------------------------------------------------------------------");
                            //Print("Current Balance is {0}, Equity is {1}.", Account.Balance, Account.Equity);
                            Print("Previous Indicators - TenkenSen ({0}); KijunSen ({1}); ChickouSpan ({2}); SpanA ({3}); SpanB({4})", Math.Round(indTenkanSen_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indKijunSen_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indChikouSpan_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanA_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanB_prev, 3, MidpointRounding.AwayFromZero));
                            Print("Current Indicators  - TenkenSen ({0}); KijunSen ({1}); ChickouSpan ({2}); SpanA ({3}); SpanB({4})", Math.Round(indTenkanSen_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indKijunSen_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indChikouSpan_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanA_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanB_curr, 3, MidpointRounding.AwayFromZero));
                            Print("Bearish Cloud - Cloud Depth [ {0} pips ]", cloudDepth);
                        }
                    }
                    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    // SHORT LOGIC - NEGATIVE CLOUD
                    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                    // CLOSE PRICE BELOW SPANB AND TENKEN SEN CROSSES BELOW KIJUN SEN (PREVIOUS TENKAN SEN ABOVE AND CROSSES BELOW THE KIJUN SEN)
                    else if (MarketSeries.Close.Last(1) < indSenkouSpanA_curr && indTenkanSen_prev > indKijunSen_prev && indTenkanSen_curr < indKijunSen_curr)
                    {
                        if (IsAutoTrading)
                        {
                            tm.OpenPosition("market", true, 0, TradeType.Buy, _tradeVol, InstanceName, null, null, null);
                        }
                        else
                        {
                            Print("SELL SHORT -------------------------------------------------------------------");
                            Print("Previous Indicators - TenkenSen ({0}); KijunSen ({1}); ChickouSpan ({2}); SpanA ({3}); SpanB({4})", Math.Round(indTenkanSen_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indKijunSen_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indChikouSpan_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanA_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanB_prev, 3, MidpointRounding.AwayFromZero));
                            Print("Current Indicators  - TenkenSen ({0}); KijunSen ({1}); ChickouSpan ({2}); SpanA ({3}); SpanB({4})", Math.Round(indTenkanSen_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indKijunSen_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indChikouSpan_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanA_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanB_curr, 3, MidpointRounding.AwayFromZero));
                            Print("Bearish Cloud - Cloud Depth [ {0} pips ]", cloudDepth);
                        }
                    }

                }
            }

            #endregion

        }

        #endregion

    }
}
