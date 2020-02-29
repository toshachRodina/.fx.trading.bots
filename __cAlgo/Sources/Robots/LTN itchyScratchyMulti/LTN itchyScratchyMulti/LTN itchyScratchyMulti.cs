/* 
=====================================================================================================================================================
SUBJECT      : risk management technical trading
OBJECT TYPE  : cAlgo
OBJECT NAME  : LTN itchyScratchyMulti
CREATED BY   : Harold Delaney
CREATED ON   : 20191002
SOURCE       : 
PREPERATION  : 
			   
REMARKS      : 1) 
               2) 
               3) 

STRATEGY     :
USES INCHIMOKU KINKO HYO INDICATOR AS PRIMARY DRIVER OF TRADE DECISION.
DEPENDING ON RISK APPETITE, DIFFERING OPEN AND CLOSE STRATEGIES CAN BE INVOKED AT RUN TIME
* SEE FOR TRADE STRATEGY: D:\Sync\__lanoitan_docs\__strategies\SwingTrading\__ichimoku\ichimoku1.Requirements.docx
* SEE FOR TRADE STRATEGY: D:\Sync\__lanoitan_docs\__strategies\SwingTrading\__ichimoku\ichimokuScratchyMulti.Requirements.docx
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
    public class LTNitchyScratchyMulti : Robot
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

        [Parameter("Max Volume", DefaultValue = 5000000, MaxValue = 10000000)]
        public int MaxVolume { get; set; }

        [Parameter("Stop Loss", DefaultValue = 10.0, MinValue = 5.0, MaxValue = 100.0, Step = 1.0)]
        public double StopLoss { get; set; }

        [Parameter("Commission in Pips", DefaultValue = 1.0)]
        public double CommissionPips { get; set; }

        [Parameter("Reserve Funds", DefaultValue = 0)]
        public int ReserveFunds { get; set; }

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

        public int tradeRealTimeBool = 0;
        // 0 = NO TRADE; 1 = TRADE
        public string cloudDirection;
        public int _volume;

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

            CalculateVolume();
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
            double _Tickvolume = MarketSeries.TickVolume[count - 1];

            var position = Positions.FindAll(InstanceName, SymbolName);


            // LOGIC DECLERATIONS

            double cloudDepth = Math.Round((indSenkouSpanA_curr - indSenkouSpanB_curr), 5);

            if (cloudDepth > 0)
            {
                cloudDirection = "posCloud";
            }
            else if (cloudDepth < 0)
            {
                cloudDirection = "negCloud";
            }
            else
            {
                cloudDirection = "flatCloud";
            }


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

            // ========================================================================================
            // SHORT POSITIONS
            //
            // ========================================================================================

            // CURRENT cloud is POSITIVE
            if (cloudDirection == "posCloud")
            {
                if (indTenkanSen_curr < indKijunSen_curr)
                {
                    // Price closes BELOW cloud
                    if (MarketSeries.Close.Last(1) < indSenkouSpanB_curr)
                    {
                        // ENTER POSITION IF AUTOTRADING SET TO TRUE
                        if (IsAutoTrading)
                        {
                            ExecuteMarketOrder(TradeType.Sell, SymbolName, _volume, InstanceName, StopLoss + CommissionPips, null);
                        }
                        // LOG DETAILS OF SIGNAL TO SCREEN AND EMAIL SIGNALS
                        else
                        {
                            // LOG TO SCREEN
                            Print("ENTER SHORT -------------------------------------------------------------------");
                            //Print("Current Balance is {0}, Equity is {1}.", Account.Balance, Account.Equity);
                            Print("Previous Indicators - TenkenSen ({0}); KijunSen ({1}); ChickouSpan ({2}); SpanA ({3}); SpanB({4})", Math.Round(indTenkanSen_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indKijunSen_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indChikouSpan_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanA_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanB_prev, 3, MidpointRounding.AwayFromZero));
                            Print("Current Indicators  - TenkenSen ({0}); KijunSen ({1}); ChickouSpan ({2}); SpanA ({3}); SpanB({4})", Math.Round(indTenkanSen_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indKijunSen_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indChikouSpan_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanA_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanB_curr, 3, MidpointRounding.AwayFromZero));
                            Print("Cloud Direction ({0}) - Cloud Depth [ {0} pips ]", cloudDirection, cloudDepth);
                            // EMAIL SIGNAL DETAILS
                            //
                            //
                            //
                        }
                    }
                }
            }
            // CURRENT cloud is NEGATIVE
            else if (cloudDirection == "negCloud")
            {
                // Tenken Sen crosses BELOW Kijun Sen
                if (indTenkanSen_curr < indKijunSen_curr)
                {
                    if (MarketSeries.Close.Last(1) < indSenkouSpanA_curr)
                    {
                        // ENTER POSITION IF AUTOTRADING SET TO TRUE
                        if (IsAutoTrading)
                        {
                            // ENTER SHORT POSITION
                            ExecuteMarketOrder(TradeType.Sell, SymbolName, _volume, InstanceName, StopLoss + CommissionPips, null);
                        }
                        // LOG DETAILS OF SIGNAL TO SCREEN AND EMAIL SIGNALS
                        else
                        {
                            // LOG TO SCREEN
                            Print("ENTER SHORT -------------------------------------------------------------------");
                            //Print("Current Balance is {0}, Equity is {1}.", Account.Balance, Account.Equity);
                            Print("Previous Indicators - TenkenSen ({0}); KijunSen ({1}); ChickouSpan ({2}); SpanA ({3}); SpanB({4})", Math.Round(indTenkanSen_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indKijunSen_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indChikouSpan_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanA_prev, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanB_prev, 3, MidpointRounding.AwayFromZero));
                            Print("Current Indicators  - TenkenSen ({0}); KijunSen ({1}); ChickouSpan ({2}); SpanA ({3}); SpanB({4})", Math.Round(indTenkanSen_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indKijunSen_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indChikouSpan_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanA_curr, 3, MidpointRounding.AwayFromZero), Math.Round(indSenkouSpanB_curr, 3, MidpointRounding.AwayFromZero));
                            Print("Cloud Direction ({0}) - Cloud Depth [ {0} pips ]", cloudDirection, cloudDepth);
                            // EMAIL SIGNAL DETAILS
                            //
                            //
                            //
                        }
                    }
                }
            }

            // ========================================================================================
            // LONG POSITIONS
            //
            // ========================================================================================



            #endregion

        }

        #endregion


        #region TRADE MANAGEMENT ==========================================================================

        public void CalculateVolume()
        {
            // Our total balance is our account balance plus any reserve funds. We do not always keep all our money in the trading account. 
            double totalBalance = Account.Balance + ReserveFunds;

            // Calculate the total risk allowed per trade.
            double riskPerTrade = (totalBalance * AccountRisk) / 100;

            // Add the stop loss, commission pips and spread to get the total pips used for the volume calculation.
            double totalPips = StopLoss + CommissionPips + Symbol.Spread;

            // Calculate the exact volume to be traded. Then round the volume to the nearest 100,000 and convert to an int so that it can be returned to the caller.
            double exactVolume = Math.Round(riskPerTrade / (Symbol.PipValue * totalPips), 2);
            _volume = (((int)exactVolume) / 100000) * 100000;

            // Finally, check that the calculated volume is not greater than the MaxVolume parameter. If it is greater, reduce the volume to the MaxVolume value.
            if (_volume > MaxVolume)
                _volume = MaxVolume;
        }

        #endregion

    }
}
