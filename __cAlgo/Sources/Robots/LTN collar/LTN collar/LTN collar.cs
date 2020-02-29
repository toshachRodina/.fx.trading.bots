/* 
=====================================================================================================================================================
SUBJECT      : trade management
OBJECT TYPE  : cAlgo
OBJECT NAME  : LTN collar
CREATED BY   : Harold Delaney
CREATED ON   : 20180503
SOURCE       : 
PREPERATION  : 
			   
REMARKS      : 1) based on specific economic announcements, enter buy/sell trade pairs
               2) once profit is realised, close out trade and then enter back in on the rebound
               3) Difference between Buy/Sell Limit and Stop Orders 
               4) * Buy Limit and Buy Stop orders on a position are triggered when the Ask price reaches the order level.
               5) * Sell Limit and Sell Stop orders on a position are triggered when the Bid price reaches the order level.

STRATEGY     : 
THE COLLAR HAS 3 CORE TRADING COMPONENTS THAT HAVE A ROUGH TIME PERIOD (T = ANNOUNCEMENT TIME)

** T - 30MIN
1. PRE-ANNOUNCEMENT
   TRADE ON THE VOLATILITY SWING LEADING UP TO ANNOUNCEMENT
   - OBSERVATIONS SEEM TO SHOW THERE IS A DIRECTIONAL TREND LEADING UP TO ANNOUNCEMENT - MAY BE OPPORTUNITY TO ENTER MARKET PRIOR TO ANNOUNCEMENT
** T - 10-60SEC
   - ALL POSITIONS CLOSED PRIOR TO ANNOUNCEMENT
** T - 0-5SEC
2. PLACE 2 TRADES  PRIOR TO ANNOUNCEMENT COVERING BOTH DIRECTIONS AND WITH SL'S.
   - THERE ARE 2 STRATEGIES HERE
     1. PLACE 2 MARKET TRADES WITH STOP LOSSES (SAY 20 to 30 PIPS) AND ACCEPT THE LOSS ON ONE SIDE OF THE TRADE
        - STOP LOSS WILL EXIT TRADE IN OPPOSITE DIRECTION OF MOVEMENT
     2. PLACE 2 PENDING ORDERS WITH A PIP SPREAD (SAY 5 to 10 PIPS) AND ACCEPT THAT YOU LOSE OUT ON INITIAL PIP MOVEMENT BUT REDUCE RISK OF TAKING ANY LOSS
        - BOT WILL CLOSE TRADE IN OPPOSITE DIRECTION THE MOMENT THE TRADE IN THE RIGHT DIRECTION IS TRIGGERED
   - ADJUST BREAK EVENS AND TRAILING STOPS AS NEEDED ON MOVEMENT
** T + 20-30SEC
3. ONCE INITIAL MOVEMENT HAS COMPLETED, PLACE BUY/SELL (DEPENDING ON DIRECTION) TRADE TO TAKE ON ANY REBOUND BACK TO EQUILIBRIUM THAT MAY OCCUR
   - INCLUDE A SMALL TRAILING STOP - SAY 5 PIPS
   - ONCE PRICE BREAKS EVEN (COINFIRMED MOMENTUM), PLACE ONOTHER TRADE IN SAME DIRECTION WITH LARGER % EQUITY
** T + 60MIN
AFTER 60 MIN, TRADE IS LIKELY OVER AND POSITION SHOULD BE CLOSED
=====================================================================================================================================================
*/
#region referenced assemblies =============================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using tradeLibrary;
//using System.Data.SQLite;
#endregion

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.EAustraliaStandardTime, AccessRights = AccessRights.None)]
    public class LTNcollar : Robot
    {
        #region USER DEFINED PARAMATERS ===================================================================
        // INSTANCE & TRADE PARAMS ================================================================
        [Parameter("Instance Name", DefaultValue = "001")]
        public string InstanceName { get; set; }

        [Parameter("Activate Auto-Trading?", DefaultValue = false)]
        public bool IsAutoTrading { get; set; }

        [Parameter("Calculate OnBar", DefaultValue = false)]
        public bool CalculateOnBar { get; set; }

        [Parameter("Announcement Time", DefaultValue = "1900-01-01T00:00:00")]
        // SORTABLEDATETIME - String.Format("{0:s}", dt);  // "2008-03-09T16:05:07" 
        public string AnnounceDateTime { get; set; }

        [Parameter("Account Bal. Risk (%)", DefaultValue = 2.0, MinValue = 0.01, MaxValue = 5.0, Step = 0.1)]
        public double AccountRisk { get; set; }

        [Parameter("Pip Difference On Rebound", DefaultValue = 5, MinValue = 1)]
        public int PipDiff { get; set; }

        [Parameter("Rebound Stop Loss (Pips)", DefaultValue = 3, MinValue = 1)]
        public int ReboundStopLoss { get; set; }

        /*
        [Parameter("Include Trading Hours", DefaultValue = false)]
        public bool IncludeTradingHours { get; set; }

        [Parameter("Trading Starts (hour)", DefaultValue = 9, MinValue = 0, MaxValue = 24)]
        public int TradingStartHour { get; set; }

        [Parameter("Trading Ends (hour)", DefaultValue = 17, MinValue = 0, MaxValue = 24)]
        public int TradingEndsHour { get; set; }

        [Parameter("Send Email?", DefaultValue = false)]
        public bool SendEmail { get; set; }

        [Parameter("To Email", DefaultValue = "toshach@gmail.com")]
        public string SendToEmail { get; set; }

        [Parameter("From Email", DefaultValue = "lanoitan17@gmail.com")]
        public string SendFromToEmail { get; set; }
        */

        // STOP LOSS & TAKE PROFIT PARAMS =========================================================
        [Parameter("---- SL & TP PARAMS ----", DefaultValue = "")]
        public string DoNothing01 { get; set; }

        [Parameter("Include Stop Loss", DefaultValue = true)]
        public bool IncludeStopLoss { get; set; }

        [Parameter("Stop Loss (pips)", DefaultValue = 0)]
        // IF 0 - NO TAKE PROFIT WILL BE APPLIED
        public int StopLoss { get; set; }

        [Parameter("Include Take Profit", DefaultValue = true)]
        public bool IncludeTakeProfit { get; set; }

        [Parameter("Take Profit Trigger (pips)", DefaultValue = 0)]
        // IF 0 - NO TAKE PROFIT WILL BE APPLIED
        public int TakeProfit { get; set; }

        // TRAILING STOP PARAMS ===================================================================
        [Parameter("---- TRAILING STOP PARAMS ----", DefaultValue = "")]
        public string DoNothing02 { get; set; }

        [Parameter("Include Trailing Stop", DefaultValue = true)]
        public bool IncludeTrailingStop { get; set; }

        [Parameter("Trailing Stop Trigger (pips)", DefaultValue = 5)]
        // IF 0 - NO STOP LOSS WILL BE APPLIED
        public int TrailingStopTrigger { get; set; }

        [Parameter("Trailing Stop Step (pips)", DefaultValue = 1, MinValue = 1)]
        public int TrailingStopStep { get; set; }

        // BREAK EVEN PARAMS ======================================================================
        [Parameter("---- BREAK EVEN PARAMS ----", DefaultValue = "")]
        public string DoNothing03 { get; set; }
        [Parameter("Include Break Eve-Even", DefaultValue = false)]
        public bool IncludeBreakEven { get; set; }

        [Parameter("Break-Even Trigger (pips)", DefaultValue = 5, MinValue = 1)]
        public int BreakEvenPips { get; set; }

        [Parameter("Break-Even Extra (pips)", DefaultValue = 2, MinValue = 1)]
        public int BreakEvenExtraPips { get; set; }

        #endregion

        #region PRIVATE VARIABLE DECLARATIONS =============================================================
        private DateTime _announceTime { get; set; }
        private TradeManager tm;
        private AverageTrueRange _atr { get; set; }
        private double _slPips;
        private long _tradeVol;
        public int? stopLoss;
        public int? takeProfit;
        //private SQLiteConnection m_dbConnection;
        //private string connString;
        public List<double> tickVolumeList = new List<double>();
        public List<double> bidTicksList = new List<double>();
        //public List<double> askTicksList = new List<double>();
        private int pendingOrderStep = 0;
        private int reboundStep = 0;
        // ORDER TYPE (MARKET / STOP / LIMIT)
        private string orderType = "stop".ToLower();
        private bool IsASync = true;
        private string reboundDirection;

        #endregion

        #region cTRADER EVENTS ============================================================================
        protected override void OnStart()
        {
            Positions.Opened += OnPositionOpen;
            tm = new TradeManager(this);
            _announceTime = Convert.ToDateTime(AnnounceDateTime);
            _atr = Indicators.AverageTrueRange(14, MovingAverageType.Simple);
        }

        protected override void OnTick()
        {
            // ONLY RUN LOGIC IF PARAMATER CalculateOnBar IS SET FALSE (EG. WE WANT TO MANAGE POSITIONS ON THE TICK EVENT)
            if (CalculateOnBar)
            {
                return;
            }

            ManagePositions();

        }

        protected override void OnBar()
        {
            // ONLY RUN LOGIC IF PARAMATER CalculateOnBar IS SET TRUE (EG. WE WANT TO MANAGE POSITIONS ON THE BAR CLOSE EVENT)
            if (!CalculateOnBar)
            {
                return;
            }

            ManagePositions();
        }

        protected void OnPositionOpen(PositionOpenedEventArgs args)
        {
            tm.CancelAllPendingPositions(InstanceName);

            // DETERMINE DIRECTION OF REBOUND BASED ON DIRECTION OF INITIAL PENDING ORDER
            // EG. IF A SELL IS TRIGGERED, THE REBOUND DIRECTION WOULD BE A BUY
            if (args.Position.TradeType.ToString().ToLower() == "sell")
            {
                reboundDirection = "buy";
            }
            else
            {
                reboundDirection = "sell";
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
            // SET DATE TIME
            DateTime srvrTime = Server.Time;
            string tf = TimeFrame.ToString();
            // SET TRADE VOLUME BASED ON % BALANCE
            _slPips = (_atr.Result.Last(1) * 2) / Symbol.PipSize;
            _tradeVol = tm.GetTradeVolumeFromAccountRisk(_slPips, AccountRisk);

            #region TIME CHECK ON LAST BAR ================================================================
            // CHECK IF SERVER TIME IS WITHIN THE INSTANCE TIME FRAME DEFINED WHEN STARTING THE BOT
            // EG.  IF 15MIN TIMEFRAME - CHECK ONBAR CLOSE IF ITS THE LAST BAR BEFORE ANNOUNCEMENT
            // IF YES - START MONITORING TIME IN TICK INTERVALS
            // IF NO  - DO NOTHING AND CHECK AGAIN ON NEXT BAR COMPLETION

            // CHECK TIME INTERVAL AGAINST MONITOR TIME ===========================================
            // IF TIME TO ANNOUNCEMENT IS WITHIN THE TIME OF THE LAST BAR, MOVE TO TICK EVENTS            
            if (CalculateOnBar)
            {
                // DO IF TIME INTERVAL IS IN MINUTES
                if (tf.Contains("Minute"))
                {
                    int minToAnnce;
                    string nbrString = Regex.Match(tf, "\\d+").Value;
                    try
                    {
                        minToAnnce = Int32.Parse(nbrString);
                    } catch
                    {
                        minToAnnce = Int32.Parse("1");
                    }

                    int diffMin = (_announceTime.AddMinutes(-minToAnnce) - srvrTime).Minutes;

                    if (diffMin <= minToAnnce)
                    {
                        CalculateOnBar = false;
                    }

                }
                // DO IF TIME INTERVAL IS IN HOURS
                if (tf.Contains("Hour"))
                {
                    int hrToAnnce;
                    string nbrString = Regex.Match(tf, "\\d+").Value;
                    try
                    {
                        hrToAnnce = Int32.Parse(nbrString);
                    } catch
                    {
                        hrToAnnce = Int32.Parse("1");
                    }

                    int diffHr = (_announceTime.AddHours(-hrToAnnce) - srvrTime).Hours;

                    if (diffHr <= hrToAnnce)
                    {
                        CalculateOnBar = false;
                    }
                }
            }
            #endregion

            #region PROCESS ON LAST TICK ==================================================================
            if (!CalculateOnBar)
            {
                bidTicksList.Add(Symbol.Bid);
                //askTicksList.Add(Symbol.Ask);
                if (IncludeTrailingStop)
                {
                    tm.SetTrailingStop(InstanceName, TrailingStopTrigger, TrailingStopStep);
                }
                if (IncludeBreakEven)
                {
                    tm.BreakEvenAdjustment(InstanceName, BreakEvenPips, BreakEvenExtraPips);
                }

                // IF CURRENT SERVER TIME BETWEEN T - 5 SEC and T + 0 SEC
                // THEN SET BUY AND SELL PENDING ORDERS
                if (srvrTime >= _announceTime.AddSeconds(-5) && srvrTime <= _announceTime)
                {
                    Phase1();
                }
                // IF CURRENT SERVER TIME >= T + 10 SEC
                // THEN SET BUY AND SELL PENDING ORDERS
                if (srvrTime >= _announceTime.AddSeconds(10) && srvrTime <= _announceTime.AddMinutes(60))
                {
                    Phase2();

                }
                // IF CURRENT SERVER TIME >= T + 60 MIN
                // CLOSE ALL POSITIONS
                if (srvrTime >= _announceTime.AddMinutes(60))
                {
                    Phase3();
                }
            }
            #endregion
        }
        #endregion

        #region PHASE 1 -- T MINUS 5 SEC ==================================================================
        private void Phase1()
        {
            // DETERMINES VOLATILITY FROM THE BID AND ASK QUOTES COLLECTED UP TO THIS POINT
            var minBidTickPrice = bidTicksList.Min();
            var maxBidTickPrice = bidTicksList.Max();
            //var minAskTickPrice = askTicksList.Min();
            //var maxAskTickPrice = askTicksList.Max();


            // ENTRY LOGIC - ENTER 2 PENDING ORDERS (BUY AND SELL)
            // IF POSITION ENTRY TIME BETWEEN _userTime.AddSeconds(-5) AND _userTime THEN
            // CHECKS IF A BUY/SELL POSITION IS OPEN
            // - IF NOT - OPEN A BUY AND SELL PENDING POSITION
            // - IF YES - IGNORE - A CLOSE WILL BE INITIATED ON THESE OPEN POSITIONS VIA STOP LOSS OR TAKE PROFIT TRIGGERS                    
            if (pendingOrderStep == 0)
            {
                if (!tm.IsOrderPending(InstanceName) && !tm.IsPositionOpenByType(TradeType.Sell, InstanceName) && !tm.IsPositionOpenByType(TradeType.Buy, InstanceName))
                {
                    pendingOrderStep = 1;
                    // STOP LOSS
                    // IF SET TO 0 OR USER HAS NOT INCLUDED A STOP LOSS - SET TO NULL
                    if (StopLoss == 0 || IncludeStopLoss == false)
                    {
                        stopLoss = null;
                    }
                    else
                    {
                        stopLoss = StopLoss;
                    }
                    // TAKE PROFIT
                    // IF SET TO 0 OR USER HAS NOT INCLUDED TAKE PROFIT - SET TO NULL
                    if (TakeProfit == 0 || IncludeTakeProfit == false)
                    {
                        takeProfit = null;
                    }
                    else
                    {
                        takeProfit = TakeProfit;
                    }

                    double priceRange = maxBidTickPrice - minBidTickPrice;

                    double volatilityPriceSell = tm.GetPriceValuePlusVolatility(TradeType.Sell.ToString().ToLower(), priceRange / 2);
                    double volatilityPriceBuy = tm.GetPriceValuePlusVolatility(TradeType.Buy.ToString().ToLower(), priceRange / 2);

                    // ONLY PLACE ORDER IF AUTOTRADING IS SET TO TRUE
                    if (IsAutoTrading)
                    {
                        tm.OpenPosition(orderType, IsASync, volatilityPriceSell, TradeType.Sell, _tradeVol, InstanceName, stopLoss, takeProfit, null);
                        tm.OpenPosition(orderType, IsASync, volatilityPriceBuy, TradeType.Buy, _tradeVol, InstanceName, stopLoss, takeProfit, null);
                    }
                }
                    /*
                        Print("PHASE 1 - PLACING ORDERS ===================================");
                        Print("Buy Stop : {0}", volatilityPriceBuy);
                        Print("Ask Price: {0}", Symbol.Ask);
                        Print("Stop Loss: {0}", stopLoss);
                        Print("------------------------------------------------------------");
                        Print("Sell Stop: {0}", volatilityPriceSell);
                        Print("Bid Price: {0}", Symbol.Bid);
                        Print("Stop Loss: {0}", stopLoss);
                        Print("============================================================");
                        Print("Ask Price is {0}; Volatilty Price Buy is {1}; Bid Price is {2}; Volatilty Price Sell is {3}; Equity is {4}; ", Symbol.Ask, volatilityPriceBuy,  Symbol.Bid,  volatilityPriceSell, Account.Equity);
                        */
                            }
        }

        #endregion

        #region PHASE 2 -- T PLUS 10 SEC ==================================================================
        private void Phase2()
        {
            // CHECK IF ORDERS ARE STILL OPEN
            // IF YES - DO NOTHING
            // IF NO  - ENTER ORDER (MARKET OR LIMIT) FOR REBOUND
            // PENDING ORDER MAY ALLOW US TO MANAGE SITUATIONS WHERE IF ORDERS ARE CLOSED BUT PRICE IS STILL MOVING IN ORIGINAL DIRECTION
            // PLACING A LIMIT ORDER AT CURRENT PRICE (minus some) WILL CATCH REBOUND WHEN PRICE TURN IS CONFIRMED 

            if (tm.IsPositionOpenByType(TradeType.Sell, InstanceName) || tm.IsPositionOpenByType(TradeType.Buy, InstanceName) || tm.IsOrderPending(InstanceName))
            {
                return;
            }
            // IF TRADES ARE CLOSED THEN ENTER REBOUND PHASE
            // BASED ON DIRECTION OF TRADE THAT WAS OPEN, SET A REBOUND TRADE IN OPPOSITE DIRECTION
            if (IsAutoTrading)
            {
                // CATERS FOR ONLY 1 REBOUND ORDER
                if (reboundStep == 0)
                {
                    if (reboundDirection == null)
                    {
                        return;
                    }
                    else if (reboundDirection == "sell")
                    {
                        tm.OpenPosition(orderType, IsASync, Symbol.Bid - PipDiff * Symbol.PipSize, TradeType.Sell, _tradeVol, InstanceName, ReboundStopLoss, null, null);
                        /*
                        Print("PHASE 2 - PLACING REBOUND ==================================");
                        Print("Sell Stop : {0}", Symbol.Ask + PipDiff * Symbol.PipSize);
                        Print("Bid Price: {0}", Symbol.Bid);
                        Print("Stop Loss: {0}", ReboundStopLoss);
                        Print("============================================================");
                        */
                        reboundStep = 1;
                    }
                    else if (reboundDirection == "buy")
                    {
                        tm.OpenPosition(orderType, IsASync, Symbol.Ask + PipDiff * Symbol.PipSize, TradeType.Buy, _tradeVol, InstanceName, ReboundStopLoss, null, null);
                        /*
                        Print("PHASE 2 - PLACING REBOUND ==================================");
                        Print("Buy Stop : {0}", Symbol.Ask + PipDiff * Symbol.PipSize);
                        Print("Ask Price: {0}", Symbol.Ask);
                        Print("Stop Loss: {0}", ReboundStopLoss);
                        Print("============================================================");
                        */
                        reboundStep = 1;
                    }
                }

            }
        }
        #endregion

        #region PHASE 3 -- END TRADING ====================================================================
        private void Phase3()
        {
            // RESET VARS AND CLOSE ANY OPEN TRADES
            pendingOrderStep = 0;
            reboundStep = 0;
            CalculateOnBar = true;
            tm.CancelAllPendingPositions(InstanceName);
            tm.CloseAllPositions(InstanceName);
        }
        #endregion

        #region DATABASE MANAGEMENT =======================================================================

    }
    /*
        void ConnectToDatabase()
        {
            // connString PATH EXISTS AT THE SAME LOCATION ON BOTH MY (DSH) DEV MACHINE AND THE SERVER
            // NO NEED TO HANDLE DIFFERENT PATHS UNTIL OTHER USERS INSTALL
            // POSSIBLY SET THE connString AS A USER SET PARAMATER
            connString = "D:\\Sync\\__lanoitan\\@data\\PY_WEB_DATA.db;sqlite;Version=3;";

            m_dbConnection = new SQLiteConnection(connString);
            m_dbConnection.Open();
        }

        // Writes the highscores to the console sorted on score in descending order.
        void SqlQuery()
        {
            string sql = "select * from dual";
            SQLiteCommand command = new SQLiteCommand(sql, m_dbConnection);
            SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
                Console.WriteLine("Name: " + reader["name"] + "\tScore: " + reader["score"]);
            Console.ReadLine();
        }
        */
    #endregion

}

