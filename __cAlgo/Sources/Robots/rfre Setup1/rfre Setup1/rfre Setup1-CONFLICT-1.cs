/* 
=====================================================================================================================================================
SUBJECT      : risk management technical trading
OBJECT TYPE  : cAlgo
OBJECT NAME  : rfre Setup1
CREATED BY   : Harold Delaney
CREATED ON   : 20191002
SOURCE       : 
PREPERATION  : 
			   
REMARKS      : 1) 
               2) 
               3) 

STRATEGY     : see documentation @ D:\Sync\__rodinaFre\__strategies\__NNFX\
               USING
                1	ATR			
                2	BASELINE	Kijun-Sen	(12)
                3	CONFIRMATION 1	Chaikin Money Flow	(12)
                4	CONFIRMATION 2	Oscillator of Moving Average (OsMA)	(12, 26, 9)
                5	VOLUME	Average True Range (ATR) Multi	(14, 365)	
                6	EXIT	Oscillator of Moving Average (OsMA)	(12, 26, 9)

=====================================================================================================================================================
*/

#region referenced assemblies =============================================================================

using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using AverageTrueRange = cAlgo.Indicators.AverageTrueRange;
//using AverageTrueRange = cAlgo.API.Indicators.AverageTrueRange;
//using tradeLibrary;

#endregion

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class RfreSetup1 : Robot
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

        // STOP LOSS WILL BE CALCULATED BASED ON ATR & RISK SETTINGS
        //[Parameter("Stop Loss", DefaultValue = 10.0, MinValue = 5.0, MaxValue = 100.0, Step = 1.0)]
        //public double StopLoss { get; set; }

        //[Parameter("Commission in Pips", DefaultValue = 1.0)]
        //public double CommissionPips { get; set; }

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
        // TRAILING STOPS WILL BE CALCULATED BASED ON ATR AND TREND AT THE END OF EACH BAR CLOSE

        //[Parameter("-- TRAILING STOPS ---------------------", DefaultValue = "")]
        //public string DoNothing02 { get; set; }

        //[Parameter("Include Trailing Stop", DefaultValue = false)]
        //public bool IncludeTrailingStop { get; set; }

        //[Parameter("Trailing Stop Trigger (pips)", DefaultValue = 20)]
        //public int TrailingStopTrigger { get; set; }

        //[Parameter("Trailing Stop Step (pips)", DefaultValue = 10)]
        //public int TrailingStopStep { get; set; }

        // TAKE PROFIT PARAMS =====================================================================
        // TAKE PROFIT WILL BE CALCULATED BASED ON ATR AND TREND AT THE END OF EACH BAR CLOSE

        //[Parameter("-- TAKE PROFIT / BREAK EVEN -----------", DefaultValue = "")]
        //public string DoNothing03 { get; set; }

        //[Parameter("Include Take Profit", DefaultValue = false)]
        //public bool IncludeTakeProfit { get; set; }

        //[Parameter("Take Profit Trigger (pips)", DefaultValue = 0)]
        //public int TakeProfitTrigger { get; set; }

        // BREAK EVEN PARAMS ======================================================================
        // BREAK EVEN WILL BE CALCULATED BASED ON ATR AND TREND AT THE END OF EACH BAR CLOSE

        //[Parameter("Include Break Even", DefaultValue = false)]
        //public bool IncludeBreakEven { get; set; }

        //[Parameter("Break-Even Trigger (pips)", DefaultValue = 10, MinValue = 1)]
        //public int BreakEvenPips { get; set; }

        //[Parameter("Break-Even Extra (pips)", DefaultValue = 2, MinValue = 1)]
        //public int BreakEvenExtraPips { get; set; }

        // INDICATOR PARAMATERS ===================================================================
        [Parameter("-- INDICATORS -------------------------", DefaultValue = "")]
        public string DoNothing04 { get; set; }

        [Parameter("Kijun Sen Periods", DefaultValue = 12, MinValue = 8, MaxValue = 30)]
        public int KijunSen { get; set; }

        [Parameter("Chaikin MF Period", DefaultValue = 12, MinValue = 8, MaxValue = 20)]
        public int ChaikinPeriod { get; set; }

        [Parameter("OsMA Short Cycle", DefaultValue = 12, MinValue = 10, MaxValue = 20)]
        public int OsMAShort { get; set; }

        [Parameter("OsMA Long Cycle", DefaultValue = 26, MinValue = 20, MaxValue = 35)]
        public int OsMALong { get; set; }

        [Parameter("OsMA Signal Period", DefaultValue = 9, MinValue = 7, MaxValue = 15)]
        public int OsMASignal { get; set; }

        [Parameter("ATR Fast Period", DefaultValue = 14, MinValue = 7, MaxValue = 21)]
        public int ATRFast { get; set; }

        [Parameter("ATR Slow Period", DefaultValue = 366, MinValue = 300, MaxValue = 400)]
        public int ATRSlow { get; set; }

        #endregion


        #region PRIVATE INDICATOR DECLARATIONS ============================================================

        private IchimokuKinkoHyo VindIchi { get; set; }
        private ChaikinMoneyFlow VchaikinMF { get; set; }

        private OSMA Vosma;
        // DECLERATION OF CUSTOM INDICATOR
        private AverageTrueRange Vatr;
        // DECLERATION OF CUSTOM INDICATOR
        public int? stopLoss;
        public int? takeProfit;

        public int tradeRealTimeBool = 0;
        // 0 = NO TRADE; 1 = TRADE
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

            // INSTANTIATE THE INDICATORS
            VindIchi = Indicators.IchimokuKinkoHyo(9, KijunSen, 52);
            VchaikinMF = Indicators.ChaikinMoneyFlow(ChaikinPeriod);
            //  CUSTOM INDICATORS
            Vosma = Indicators.GetIndicator<OSMA>(OsMAShort, OsMALong, OsMASignal);
            Vatr = Indicators.GetIndicator<AverageTrueRange>(ATRFast, ATRSlow);

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
            double indKijunSen_curr = VindIchi.KijunSen.Last(1);
            
            // ======================================================================================================================================
            // CURRENT COMPLETE PERIOD (-2 bar from current) 
            // ======================================================================================================================================
            double indKijunSen_prev = VindIchi.KijunSen.Last(2);
            
            
            // TOTAL COUNT OF TICK VOLUME SERIES
            // LAST TICK VOLUME !!!!!!!!! I DONT THINK THIS IS CORRECT !!!!!!!!!
            int count = MarketSeries.TickVolume.Count;
            // int
            double _Tickvolume = MarketSeries.TickVolume[count - 1];

            var position = Positions.FindAll(InstanceName, SymbolName);


            // LOGIC DECLERATIONS
            
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
