/* 
=====================================================================================================================================================
SUBJECT      : risk management technical trading
OBJECT TYPE  : cAlgo
OBJECT NAME  : LTN itchyScratchy
CREATED BY   : Harold Delaney
CREATED ON   : 20181021
SOURCE       : 
PREPERATION  : 
			   
REMARKS      : 1) 
               2) 
               3) 

STRATEGY     :
USES CONSERVATIVE INDICATORS EXISTING ON THE INCHIMOKU KINKO HYO STRATEGY.
* SEE FOR TRADE STRATEGY: D:\Sync\__lanoitan_docs\__strategies\SwingTrading\__ichimoku\ichimoku1.Requirements.docx
* 
=====================================================================================================================================================
*/

#region referenced assemblies =============================================================================

using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
//using tradeLibrary;

#endregion

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class LTNitchyScratchy : Robot
    {

        #region USER DEFINED PARAMATERS ===================================================================

        // INSTANCE & TRADE PARAMS ================================================================
        [Parameter("Instance Name", DefaultValue = "001")]
        public string InstanceName { get; set; }

        [Parameter("Calculate OnBar", DefaultValue = true)]
        public bool CalculateOnBar { get; set; }

        [Parameter("Activate Auto-Trading?", DefaultValue = false)]
        public bool IsAutoTrading { get; set; }

        [Parameter("Account Bal. Risk (%)", DefaultValue = 2.0, MinValue = 0.01, MaxValue = 5.0, Step = 0.1)]
        public double AccountRisk { get; set; }

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

        [Parameter("Tenkan Sen Periods", DefaultValue = 9, MinValue = 1, MaxValue = 27)]
        public int TenkanSen { get; set; }

        [Parameter("Kijun Sen Periods", DefaultValue = 26, MinValue = 12, MaxValue = 40)]
        public int KijunSen { get; set; }

        [Parameter("Senkou Span B Periods", DefaultValue = 42, MinValue = 42, MaxValue = 72)]
        public int SenkouSpanB { get; set; }

        [Parameter("Min. Kimu Cloud Depth", DefaultValue = 0.002, MinValue = 0.002, MaxValue = 1.5, Step = 0.001)]
        public double MinCloudDepth { get; set; }

        #endregion


        #region PRIVATE INDICATOR DECLARATIONS ============================================================

        private IchimokuKinkoHyo VindIchi { get; set; }

        //private TradeManager tm;
        //private readonly double l_slPips;
        //private readonly long l_tradeVol;

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
            //tm = new TradeManager(this);
            // CONSTRUCT THE INDICATORS
            VindIchi = Indicators.IchimokuKinkoHyo(TenkanSen, KijunSen, SenkouSpanB);

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
            // INITIALISE ELEMENTS ONBAR CLOSE
            // INDEX: 0 - CURRENT LIVE BAR (NOT FINALISED)
            //        1 - MOST RECENT FINALISED CLOSE BAR
            //        2 - 2ND MOST RECENT FINALISED CLOSE BAR

            // ======================================================================================================================================
            // CURRENT COMPLETE PERIOD (-1 bar from current) 
            // ======================================================================================================================================
            double indTenkanSen_curr = VindIchi.TenkanSen.Last(1);
            double indKijunSen_curr = VindIchi.KijunSen.Last(1);
            // minus 26 - 1 (27) for -1 bar 
            double indSenkouSpanA_curr = VindIchi.SenkouSpanA.Last(27);
            double indSenkouSpanB_curr = VindIchi.SenkouSpanB.Last(27);
            double indChikouSpan_curr = VindIchi.ChikouSpan.Last(0);

            // ======================================================================================================================================
            // CURRENT COMPLETE PERIOD (-2 bar from current) 
            // ======================================================================================================================================
            double indTenkanSen_prev = VindIchi.TenkanSen.Last(2);
            double indKijunSen_prev = VindIchi.KijunSen.Last(2);
            // minus 27 - 1 (28) for CURR-1 bar 
            double indSenkouSpanA_prev = VindIchi.SenkouSpanA.Last(28);
            double indSenkouSpanB_prev = VindIchi.SenkouSpanB.Last(28);
            double indChikouSpan_prev = VindIchi.ChikouSpan.Last(1);

            // ======================================================================================================================================
            // LAST 5 BARS OF THE KUMO CLOUD
            // ======================================================================================================================================
            // 0 shows end of cloud -0 periods (26 periods in future) 
            double indSenkouSpanA_futr0 = VindIchi.SenkouSpanA.Last(0);
            double indSenkouSpanB_futr0 = VindIchi.SenkouSpanB.Last(0);
            // 1 shows end of cloud -1 periods (25 periods in future) 
            double indSenkouSpanA_futr1 = VindIchi.SenkouSpanA.Last(1);
            double indSenkouSpanB_futr1 = VindIchi.SenkouSpanB.Last(1);
            // 2 shows end of cloud -2 periods (24 periods in future) 
            double indSenkouSpanA_futr2 = VindIchi.SenkouSpanA.Last(2);
            double indSenkouSpanB_futr2 = VindIchi.SenkouSpanB.Last(2);
            // 3 shows end of cloud -3 periods (23 periods in future) 
            double indSenkouSpanA_futr3 = VindIchi.SenkouSpanA.Last(3);
            double indSenkouSpanB_futr3 = VindIchi.SenkouSpanB.Last(3);
            // 4 shows end of cloud -4 periods (22 periods in future) 
            double indSenkouSpanA_futr4 = VindIchi.SenkouSpanA.Last(4);
            double indSenkouSpanB_futr4 = VindIchi.SenkouSpanB.Last(4);


            // TOTAL COUNT OF TICK VOLUME SERIES
            // LAST TICK VOLUME !!!!!!!!! I DONT THINK THIS IS CORRECT !!!!!!!!!
            int count = MarketSeries.TickVolume.Count;
            // int
            double _volume = MarketSeries.TickVolume[count - 1];
            var position = Positions.Find(InstanceName, SymbolName);

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // BELOW THIS POINT TRADES WILL BE MADE IF ISTESTING IS SET FALSE
            // * TEST VARIABLES AND VALUES ABOVE THIS SECTION
            // 
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            #region EXIT LOGIC ========================================================================

            if (IsAutoTrading)
            {

            }

            #endregion

            #region ENTRY LOGIC =======================================================================

            double cloudDepth = Math.Round((indSenkouSpanA_curr - indSenkouSpanB_curr), 5);

            if (Math.Abs(cloudDepth) > MinCloudDepth)
            {
                // POSITIVE CLOUD - CHICKOU SPAN A APPEARS ABOVE CHICKOU SPAN B
                if (cloudDepth > 0)
                {
                    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                    // LONG LOGIC - POSITIVE CLOUD
                    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                    // CLOSE PRICE ABOVE SPANA AND TENKEN SEN CROSSES ABOVE KIJUN SEN (PREVIOUS TENKAN SEN BELOW AND CROSSES ABOVE THE KIJUN SEN)
                    if (MarketSeries.Close.Last(1) > indSenkouSpanA_curr && indTenkanSen_prev < indKijunSen_curr && indTenkanSen_curr > indKijunSen_curr)
                    {
                        if (IsAutoTrading)
                        {
                            //tm.OpenPosition("market", true, 0, TradeType.Buy, _tradeVol, InstanceName, null, null, null);
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
                    // SHORT LOGIC - POSITIVE CLOUD
                    // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                    // CLOSE PRICE BELOW SPANB AND TENKEN SEN CROSSES BELOW KIJUN SEN (PREVIOUS TENKAN SEN ABOVE AND CROSSES BELOW THE KIJUN SEN)
                    else if (MarketSeries.Close.Last(1) < indSenkouSpanB_curr && indTenkanSen_prev > indKijunSen_prev && indTenkanSen_curr < indKijunSen_curr)
                    {
                        if (IsAutoTrading)
                        {
                            //tm.OpenPosition("market", true, 0, TradeType.Buy, _tradeVol, InstanceName, null, null, null);
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
                            //tm.OpenPosition("market", true, 0, TradeType.Buy, _tradeVol, InstanceName, null, null, null);
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
                            //tm.OpenPosition("market", true, 0, TradeType.Buy, _tradeVol, InstanceName, null, null, null);
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
