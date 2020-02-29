/* 
=====================================================================================================================================================
SUBJECT      : trade management
OBJECT TYPE  : cAlgo
OBJECT NAME  : LTN collarAnnounce
CREATED BY   : Harold Delaney
CREATED ON   : 20180503
SOURCE       : 
PREPERATION  : 
			   
REMARKS      : 1) based on specific economic announcements, enter buy/sell trade pairs
               2) once profit is realised, close out trade
               3) Difference between Buy/Sell Limit and Stop Orders 
               4) * Buy Limit and Buy Stop orders on a position are triggered when the Ask price reaches the order level.
               5) * Sell Limit and Sell Stop orders on a position are triggered when the Bid price reaches the order level.

STRATEGY     : 
THE COLLAR_ANNCE HAS A SINGLE TRADING COMPONENTS

** T - 5SEC (OR LATER DEPENDING ON TIME TO POST PENDING ORDERS TO SERVER)
1. PRE-ANNOUNCEMENT
   - PLACE 8 PENDING ORDERS TO CAPTURE BOTH SMALLER AND LARGER MOVEMENTS ON ANNOUNCEMENT
   - 1. SMALL MOVEMENT
        - X2 3 PIPS FROM CURRENT PRICE IN BUY/SELL DIRECTION
   - 2. SMALL/MEDIUM MOVEMENT
        - X2 5 PIPS FROM CURRENT PRICE IN BUY/SELL DIRECTION
   - 3. SMALL/MEDIUM MOVEMENT
        - X2 10 PIPS FROM CURRENT PRICE IN BUY/SELL DIRECTION
   - 4. LARGE MOVEMENT
        - X2 20 PIPS FROM CURRENT PRICE IN BUY/SELL DIRECTION
   
** RISK MANAGEMENT
- RISK IS MANAGED VIA:-
    1. STOP LOSS
    2. TRAILING STOP
    3. BREAK EVEN
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
    public class LTNcollarAnnounce : Robot
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

        [Parameter("Include Take Profit", DefaultValue = false)]
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
        //public List<double> askTicksList = new List<double>();
        private int pendingOrderStep = 0;
        // ORDER TYPE (MARKET / STOP / LIMIT)
        private readonly string orderType = "stop".ToLower();
        private readonly bool IsASync = true;

        private double setPricePips;
        private string labelAppend;

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
            // DETERMINE DIRECTION OF MOVEMENT BASED ON WHICH PENDING ORDER WAS TRIPPED
            // EG. IF A SELL IS TRIGGERED, WE WANT TO SIGNAL TO CANCEL ALL PENDING BUY ORDERS
            if (args.Position.Label == InstanceName + "PIP10")
            {
                foreach (var position in Positions)
                {
                    if (args.Position.TradeType.ToString().ToLower() == "sell")
                    {
                        tm.CancelPendingPositions(position.Label, "buy");
                    }
                    else if (args.Position.TradeType.ToString().ToLower() == "buy")
                    {
                        tm.CancelPendingPositions(position.Label, "sell");
                    }
                }
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
                foreach (var position in Positions)
                {
                    if (IncludeTrailingStop)
                    {
                        tm.SetTrailingStop(position.Label, TrailingStopTrigger, TrailingStopStep);
                    }
                    if (IncludeBreakEven)
                    {
                        tm.BreakEvenAdjustment(position.Label, BreakEvenPips, BreakEvenExtraPips);
                    }
                }
                // IF CURRENT SERVER TIME BETWEEN T - 3 SEC and T + 0 SEC
                // THEN SET BUY AND SELL PENDING ORDERS
                if (srvrTime >= _announceTime.AddSeconds(-4) && srvrTime <= _announceTime)
                {
                    Phase1();
                }

                // IF CURRENT SERVER TIME >= T + 10 SEC
                // CANCEL ANY PENDING POSITIONS THAT HAVE NOT BEEN TRIGGERED
                // -- REMAINING OPEN POSITIONS WILL CLOSE OUT ON STOP LOSS SETTINGS
                // set to 20 for testing - revert to 10 when ready
                if (srvrTime >= _announceTime.AddSeconds(20))
                {
                    Phase2();
                }
            }
            #endregion
        }
        #endregion

        #region PHASE 1 -- T MINUS 3 SEC ==================================================================
        private void Phase1()
        {
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

                    //double volatilityPriceSell = tm.GetPriceValuePlusVolatility(TradeType.Sell.ToString().ToLower(), priceRange / 2);
                    //double volatilityPriceBuy = tm.GetPriceValuePlusVolatility(TradeType.Buy.ToString().ToLower(), priceRange / 2);
                    // ONLY PLACE ORDER IF AUTOTRADING IS SET TO TRUE
                    if (IsAutoTrading)
                    {
                        for (int orders = 1; orders < 5; orders = orders + 1)
                        {
                            if (orders == 1)
                            {
                                setPricePips = 0.002;
                                // 20 PIPS - PIP DISTANCE FROM CURRENT PRICE
                                stopLoss = 10;
                                labelAppend = "PIP20";
                            }
                            else if (orders == 2)
                            {
                                setPricePips = 0.001;
                                // 10 PIPS - PIP DISTANCE FROM CURRENT PRICE
                                stopLoss = 5;
                                labelAppend = "PIP10";
                            }
                            else if (orders == 3)
                            {
                                setPricePips = 0.0005;
                                // 5 PIPS - PIP DISTANCE FROM CURRENT PRICE
                                stopLoss = 2;
                                labelAppend = "PIP5";

                            }
                            else if (orders == 4)
                            {
                                setPricePips = 0.0003;
                                // 3 PIPS - PIP DISTANCE FROM CURRENT PRICE
                                stopLoss = 1;
                                labelAppend = "PIP3";
                            }

                            double priceSell = tm.GetPriceValue(TradeType.Sell.ToString().ToLower(), setPricePips);
                            double priceBuy = tm.GetPriceValue(TradeType.Buy.ToString().ToLower(), setPricePips);

                            tm.OpenPosition(orderType, IsASync, priceSell, TradeType.Sell, _tradeVol, InstanceName + labelAppend, stopLoss, takeProfit, null);
                            tm.OpenPosition(orderType, IsASync, priceBuy, TradeType.Buy, _tradeVol, InstanceName + labelAppend, stopLoss, takeProfit, null);
                        }
                    }
                        /*
                            Print("PHASE 1 - PLACING ORDERS ===================================");
                            Print("Buy Stop : {0}", priceBuy);
                            Print("Ask Price: {0}", Symbol.Ask);
                            Print("Stop Loss: {0}", stopLoss);
                            Print("------------------------------------------------------------");
                            Print("Sell Stop: {0}", priceSell);
                            Print("Bid Price: {0}", Symbol.Bid);
                            Print("Stop Loss: {0}", stopLoss);
                            Print("============================================================");
                            //Print("Ask Price is {0}; Volatilty Price Buy is {1}; Bid Price is {2}; Volatilty Price Sell is {3}; Equity is {4}; ", Symbol.Ask, priceBuy, Symbol.Bid, volatilityPriceSell, Account.Equity);
                            */
                                    }
            }
        }

        #endregion

        #region PHASE 2 -- CLOSE PENDING POSITION =========================================================
        private void Phase2()
        {
            // RESET VARS AND CANCEL ANY UNFILLED PENDING ORDERS
            pendingOrderStep = 0;
            //CalculateOnBar = true;
            foreach (var order in PendingOrders)
            {
                tm.CancelPendingPositions(order.Label, "all");
            }

            //tm.CloseAllPositions(InstanceName);
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


