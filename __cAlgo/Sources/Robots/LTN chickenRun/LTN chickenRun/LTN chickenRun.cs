/* 
=====================================================================================================================================================
SUBJECT      : risk management
OBJECT TYPE  : cAlgo
OBJECT NAME  : LTN chickenRun
CREATED BY   : Harold Delaney
CREATED ON   : 20180424
SOURCE       : 
PREPERATION  : 
			   
REMARKS      : 1) using RSI, SMA, Momentum, RVI and possibly ATR (average true range) to signal entry and exit points
               2) 
               3) 

STRATEGY     :
CHICKEN RUN USES A COMBINATION OF INDICATORS TO DETERMINE ENTRY AND EXIT POINTS OVER A 15MIN TIMEFRAME.
* INDICATOR VALUES AS PER BELOW
* CHANGE IN DIRECTION AND GRADIENT OF CHANGE
 
***************************************************************************************************
ENTRY ON SHORT (DOWN) -----------------------------------------------------------------------------
* WHEN CURR DTTM <= ECONOMIC NEWS DTTM - 3HRS
*  AND RSI > 70 (TEST FOR SMALLER RANGE - EG. 67 MIGHT PROVIDE BETTER REAULTS)
*  AND RSI <  PREVIOUS RSI (DOWN)
*  AND RVI <= PREVIOUS RVI (DOWN)
*  AND CLOSE PRICE > SMA HIGH(14)
*  AND VOL > 1000
*  -- OTHER THINGS TO TEST ----------------------
*  AND MOM > 100 ?????
*  AND ATR > ?????

EXIT ON SHORT (DOWN) ------------------------------------------------------------------------------
CONSERVATIVE
* WHEN RVI > RVISIG (UP)
AGGRESSIVE
* #1 WHEN RVI >= PREVIOUS RVI (UP) 
* #2 WHEN PRICE(CLOSE) > SMALOW(14)  
OTHER EXITS
* WHEN TIME IN TRADE > 2H 45MI (TEST ALSO FOR 3H)

***************************************************************************************************
ENTRY ON LONG (UP) --------------------------------------------------------------------------------
* WHEN CURR DTTM <= ECONOMIC NEWS DTTM - 3HRS
*  AND RSI < 30
*  AND RSI > PREVIOUS RSI (UP)
*  AND RVI >= PREVIOUS RVI (UP)
*  AND CLOSE PRICE < SMA LOW(14)
*  AND VOL > 1000
*  -- OTHER THINGS TO TEST ----------------------
*  AND MOM < 99.77 ?????
*  AND ATR > ?????

EXIT ON LONG (UP) ---------------------------------------------------------------------------------
CONSERVATIVE
* WHEN RVI < RVISIG (DOWN)
AGGRESSIVE
* #1 WHEN RVI <= PREVIOUS RSI (DOWN)
* #2 WHEN PRICE(CLOSE) > SMALOW(14)  
OTHER EXITS
* WHEN TIME IN TRADE > 2H 45MI (TEST ALSO FOR 3H)
 

***************************************************************************************************
=====================================================================================================================================================
*/
#region referenced assemblies =============================================================================
using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using tradeLibrary;
#endregion

namespace cAlgo
{
    // TIMEZONE ATTRIBUTE
    // ALL DATES AND TIMES WITHIN THE ROBOT OR INDICATOR WILL BE CONVERTED TO THIS TIMEZONE
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class LTNchickenRun : Robot
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

        [Parameter("Aggressive Strategy?", DefaultValue = false)]
        public bool AggressiveStrategy { get; set; }

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

        // RSI ----------------------------------
        [Parameter("RSI Period", DefaultValue = 14, MinValue = 1)]
        public int RSIPeriod { get; set; }

        [Parameter("RSI LOW Level", DefaultValue = 30, MaxValue = 50, MinValue = 1)]
        public int RSILowLevel { get; set; }

        [Parameter("RSI HIGH Level", DefaultValue = 70, MaxValue = 99, MinValue = 60)]
        public int RSIHighLevel { get; set; }

        [Parameter("RSI Source")]
        public DataSeries RSISource { get; set; }

        // SMA ----------------------------------
        [Parameter("SMA HIGH Period", DefaultValue = 14, MinValue = 1)]
        public int HighSMAPeriod { get; set; }

        [Parameter("SMA HIGH Source")]
        public DataSeries HighSMASource { get; set; }

        [Parameter("SMA LOW Period", DefaultValue = 14, MinValue = 1)]
        public int LowSMAPeriod { get; set; }

        [Parameter("SMA LOW Source")]
        public DataSeries LowSMASource { get; set; }

        // MOM ----------------------------------
        [Parameter("MOM Period", DefaultValue = 14, MinValue = 1)]
        public int MOMPeriod { get; set; }

        [Parameter("MOM Source")]
        public DataSeries MOMSource { get; set; }

        // RVI ----------------------------------
        [Parameter("RVI Period", DefaultValue = 10, MinValue = 1)]
        public int RVIPeriod { get; set; }

        // ATR ----------------------------------
        [Parameter("ATR Period", DefaultValue = 10, MinValue = 1)]
        public int ATRPeriod { get; set; }

        [Parameter("ATR Multiplier", DefaultValue = 2, MinValue = 1)]
        public int ATRMultiplier { get; set; }

        // VOLUME ------------------------------
        [Parameter("Volume Threshold", DefaultValue = 1000, MinValue = 1)]
        public int VOLThreshold { get; set; }

        #endregion

        #region PRIVATE INDICATOR DECLARATIONS ============================================================
        private RelativeStrengthIndex _rsi { get; set; }
        private SimpleMovingAverage _smaHIGH { get; set; }
        private SimpleMovingAverage _smaLOW { get; set; }
        private MomentumOscillator _mom { get; set; }
        private AverageTrueRange _atr { get; set; }
        private RelativeVigorIndex _rvi { get; set; }
        private MarketDepth _md { get; set; }

        private TradeManager tm;
        private double _slPips;
        private long _tradeVol;

        public int? stopLoss;
        public int? takeProfit;

        #endregion

        #region cTRADER EVENTS ============================================================================
        /// <summary>
        /// CALLED WHEN THE ROBOT FIRST STARTS, IT IS ONLY CALLED ONCE.
        /// </summary>
        protected override void OnStart()
        {
            // CONSTRUCT THE INDICATORS
            _rsi = Indicators.RelativeStrengthIndex(RSISource, RSIPeriod);

            _smaHIGH = Indicators.SimpleMovingAverage(HighSMASource, HighSMAPeriod);
            _smaLOW = Indicators.SimpleMovingAverage(LowSMASource, LowSMAPeriod);

            _mom = Indicators.MomentumOscillator(MOMSource, MOMPeriod);

            _atr = Indicators.AverageTrueRange(ATRPeriod, MovingAverageType.Simple);

            _md = MarketData.GetMarketDepth(Symbol);

            _rvi = Indicators.GetIndicator<RelativeVigorIndex>(RVIPeriod, MovingAverageType.Simple);

            tm = new TradeManager(this);

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

            try
            {
                ManagePositions();
            } catch (Exception e)
            {
                Print("{0} ", e.StackTrace);
            }

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
            TimeSpan timeInTrade;
            int timeInTradeMin;
            // INITIALISE ELEMENTS ONBAR CLOSE
            // INDEX: 0 - CURRENT LIVE BAR (NOT FINALISED)
            //        1 - MOST RECENT FINALISED CLOSE BAR
            //        2 - 2ND MOST RECENT FINALISED CLOSE BAR
            var _rsi_curr = _rsi.Result.Last(1);
            var _rsi_prev = _rsi.Result.Last(2);
            var _sma_high_curr = _smaHIGH.Result.Last(1);
            var _sma_low_curr = _smaLOW.Result.Last(1);
            var _price_curr = MarketSeries.Close.Last(1);
            var _rvi_curr = _rvi.Result.Last(1);
            var _rvi_prev = _rvi.Result.Last(2);
            var _rviSig_curr = _rvi.Signal.Last(1);
            var _rviSig_prev = _rvi.Signal.Last(2);
            // TOTAL COUNT OF TICK VOLUME SERIES
            // LAST TICK VOLUME !!!!!!!!! I DONT THINK THIS IS CORRECT !!!!!!!!!
            var count = MarketSeries.TickVolume.Count;
            // int
            var _volume = MarketSeries.TickVolume[count - 1];
            var position = Positions.Find(InstanceName, Symbol);
            // CALCULATE VOLUME OF TRADE TO BE APPLIED BASED ON CURRENT % OF BALANCE
            _slPips = (_atr.Result.Last(1) * ATRMultiplier) / Symbol.PipSize;
            _tradeVol = tm.GetTradeVolumeFromAccountRisk(_slPips, AccountRisk);

                        /*
if (!tm.IsPositionOpenByType(TradeType.Sell, InstanceName))
{
    tm.OpenPosition(TradeType.Sell, _tradeVol, InstanceName, null, null);
}
*/

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // BELOW THIS POINT TRADES WILL BE MADE IF ISTESTING IS SET FALSE
            // * TEST VARIABLES AND VALUES ABOVE THIS SECTION
            // 
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            #region EXIT LOGIC ========================================================================

if (IsAutoTrading)
            {
                // EXIT TRADE LOGIC - SELL ========================================================
                // - TWO METHODS FOR EXIT (CONSERVATIVE, AGGRESSIVE)
                // - PARAMATISED TO TEST RESULTS FOR EACH
                // AGGRESSIVE ---------------------------------------------------------------------
                // * WHEN RVI >= PREVIOUS RVI(UP)
                // CONSERVATIVE -------------------------------------------------------------------
                // * WHEN RVI > RVISIG(UP)
                // OTHER EXITS --------------------------------------------------------------------
                // * WHEN TIME IN TRADE > 2H 45MI(TEST ALSO FOR 3H) 
                if (tm.IsPositionOpenByType(TradeType.Sell, InstanceName))
                {
                    timeInTrade = Server.Time - position.EntryTime;
                    timeInTradeMin = timeInTrade.Minutes;

                    if (AggressiveStrategy)
                    {
                        // need to add in the 3H time limit logic
                        if (_rvi_curr >= _rvi_prev || (timeInTradeMin >= 29 && _price_curr > _sma_high_curr))
                        {
                            tm.ClosePosition(TradeType.Sell, InstanceName);
                        }
                    }
                    else
                    {
                        // need to add in the 3H time limit logic
                        if (_rvi_curr > _rviSig_curr)
                        {
                            tm.ClosePosition(TradeType.Sell, InstanceName);
                        }
                    }

                }
                // EXIT TRADE LOGIC - BUY =========================================================
                // - TWO METHODS FOR EXIT (CONSERVATIVE, AGGRESSIVE)
                // -PARAMATISED TO TEST RESULTS FOR EACH
                // AGGRESSIVE ---------------------------------------------------------------------
                // * WHEN RVI <= PREVIOUS RVI(UP)
                // CONSERVATIVE -------------------------------------------------------------------
                // * WHEN RVI < RVISIG(UP)
                // OTHER EXITS --------------------------------------------------------------------
                // * WHEN TIME IN TRADE > 2H 45MI(TEST ALSO FOR 3H)
                if (tm.IsPositionOpenByType(TradeType.Buy, InstanceName))
                {
                    timeInTrade = Server.Time - position.EntryTime;
                    timeInTradeMin = timeInTrade.Minutes;

                    if (AggressiveStrategy)
                    {
                        // need to add in the 3H time limit logic
                        if (_rvi_curr <= _rvi_prev || (timeInTradeMin >= 29 && _price_curr < _sma_low_curr))
                        {
                            tm.ClosePosition(TradeType.Buy, InstanceName);
                        }
                    }
                    else
                    {
                        // need to add in the 3H time limit logic
                        if (_rvi_curr < _rviSig_curr)
                        {
                            tm.ClosePosition(TradeType.Buy, InstanceName);
                        }
                    }
                }
                #endregion

                #region ENTRY LOGIC =======================================================================
                // SELL LOGIC - ENTRY ON SHORT
                // && _volume > VOLThreshold)
                if (_rsi_curr > RSIHighLevel && _rsi_curr < _rsi_prev && _price_curr > _sma_high_curr && _rvi_curr <= _rvi_prev)
                {
                    // MAINTAINS ONLY 1 TRADE OPEN AT ANY GIVEN TIME
                    // CHECKS IF A SELL POSITION IS OPEN
                    // - IF NOT - OPEN SELL POSITION AND CLOSE BUY POSITION
                    // - IF YES - CLOSE BUY POSITION 
                    // ?????  DO WE WANT TO HAVE MULTIPLE TRADES OPEN AT A TIME?  
                    // ?????  IN THIS CASE, I THINK NOT.  IF WE ARE ENTERING A SELL MARKET, THE BUY HAS GONE COLD (INDICATORS HAVE TURNED) AND SHOULD BE CLOSED
                    if (TrailingStopTrigger == 0 || IncludeTrailingStop == false)
                    {
                        stopLoss = null;
                    }
                    else
                    {
                        stopLoss = TrailingStopTrigger;
                    }
                    // TAKE PROFIT
                    // IF SET TO 0 OR USER HAS NOT INCLUDED TAKE PROFIT - SET TO NULL
                    if (TakeProfitTrigger == 0 || IncludeTakeProfit == false)
                    {
                        takeProfit = null;
                    }
                    else
                    {
                        takeProfit = TakeProfitTrigger;
                    }

                    if (!tm.IsPositionOpenByType(TradeType.Sell, InstanceName))
                    {
                        tm.OpenPosition("market", true, 0, TradeType.Sell, _tradeVol, InstanceName, null, null, null);
                    }

                    tm.ClosePosition(TradeType.Buy, InstanceName);
                }
                // BUY LOGIC - ENTRY ON LONG
                // && _volume > VOLThreshold)
                if (_rsi_curr < RSILowLevel && _rsi_curr > _rsi_prev && _price_curr < _sma_low_curr && _rvi_curr >= _rvi_prev)
                {
                    // MAINTAINS ONLY 1 TRADE OPEN AT ANY GIVEN TIME
                    // CHECKS IF A BUY POSITION IS OPEN
                    // - IF NOT - OPEN BUY POSITION AND CLOSE SELL POSITION
                    // - IF YES - CLOSE SELL POSITION 
                    // ?????  DO WE WANT TO HAVE MULTIPLE TRADES OPEN AT A TIME?  
                    // ?????  IN THIS CASE, I THINK NOT.  IF WE ARE ENTERING A BUY MARKET, THE SELL HAS GONE COLD (INDICATORS HAVE TURNED) AND SHOULD BE CLOSED
                    if (TrailingStopTrigger == 0 || IncludeTrailingStop == false)
                    {
                        stopLoss = null;
                    }
                    else
                    {
                        stopLoss = TrailingStopTrigger;
                    }
                    // TAKE PROFIT
                    // IF SET TO 0 OR USER HAS NOT INCLUDED TAKE PROFIT - SET TO NULL
                    if (TakeProfitTrigger == 0 || IncludeTakeProfit == false)
                    {
                        takeProfit = null;
                    }
                    else
                    {
                        takeProfit = TakeProfitTrigger;
                    }

                    if (!tm.IsPositionOpenByType(TradeType.Buy, InstanceName))
                    {
                        tm.OpenPosition("market", true, 0, TradeType.Buy, _tradeVol, InstanceName, null, null, null);
                    }

                    tm.ClosePosition(TradeType.Sell, InstanceName);
                }
                #endregion
            }
        }
        #endregion


    }
}
