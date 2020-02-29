using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using tradeLibrary;

namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.EAustraliaStandardTime, AccessRights = AccessRights.None)]
    public class LTNrapidFire : Robot
    {
        #region USER PROMPTED PARAMATERS ==================================================================
        // INSTANCE & TRADE PARAMS ================================================================
        [Parameter("Instance Name", DefaultValue = "001")]
        public string InstanceName { get; set; }

        [Parameter("Activate Auto-Trading?", DefaultValue = false)]
        public bool IsAutoTrading { get; set; }

        [Parameter("Calculate OnBar", DefaultValue = false)]
        public bool CalculateOnBar { get; set; }

        [Parameter("Account Bal. Risk (%)", DefaultValue = 0.2, MinValue = 0.01, MaxValue = 5.0, Step = 0.1)]
        public double AccountRisk { get; set; }

        // STOP LOSS & TAKE PROFIT PARAMS =========================================================

        [Parameter("---- SL & TP PARAMS ----", DefaultValue = "")]
        public string DoNothing01 { get; set; }

        [Parameter("Include Stop Loss", DefaultValue = true)]
        public bool IncludeStopLoss { get; set; }

        [Parameter("Stop Loss (pips)", DefaultValue = 10)]
        // IF 0 - NO TAKE PROFIT WILL BE APPLIED
        public int StopLoss { get; set; }

        [Parameter("Include Take Profit", DefaultValue = false)]
        public bool IncludeTakeProfit { get; set; }

        [Parameter("Take Profit Trigger (pips)", DefaultValue = 10)]
        // IF 0 - NO TAKE PROFIT WILL BE APPLIED
        public int TakeProfit { get; set; }
        public int TakeProfitTrigger { get; set; }

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

        // INDICATOR PARAMATERS ===================================================================
        [Parameter("-- INDICATORS -------------------------", DefaultValue = "")]
        public string DoNothing04 { get; set; }

        // SMA ----------------------------------
        [Parameter("SMA Period", DefaultValue = 60, MinValue = 40, MaxValue = 100)]
        public int SMAPeriod { get; set; }

        [Parameter("SMA HIGH Source")]
        public DataSeries SMASource { get; set; }

        // SAR ----------------------------------
        [Parameter("SAR MIN AF", DefaultValue = 0.02, MinValue = 0.01, MaxValue =0.10)]
        public double SARMINAF { get; set; }

        [Parameter("SAR MAX AF", DefaultValue = 0.02, MinValue = 0.01, MaxValue = 0.10)]
        public double SARMAXAF { get; set; }

        [Parameter("SAR Trail", DefaultValue = 3, MinValue = 1, MaxValue = 10)]
        public int SARTrail { get; set; }

        // DMS ----------------------------------
        [Parameter("DMS Period", DefaultValue = 14, MinValue = 10, MaxValue = 60)]
        public int DMSPeriod { get; set; }

        [Parameter("DMS Threshold", DefaultValue = 30, MinValue = 20, MaxValue = 50)]
        public int DMSThreshold { get; set; }

        // VOLUME -------------------------------
        [Parameter("Volume Threshold", DefaultValue = 50, MinValue = 40, MaxValue = 70)]
        public int VOLThreshold { get; set; }

        #endregion

        #region PRIVATE VARIABLE DECLARATIONS =============================================================
        private DateTime VannounceTime { get; set; }
        private TradeManager tm;

        // INDICATORS =====================================================================================
        private AverageTrueRange IndAtr { get; set; }
        private ParabolicSAR IndSAR { get; set; }
        private SimpleMovingAverage IndSMA { get; set; }
        private DirectionalMovementSystem IndDMS { get; set; }

        // USER DEFINED ===================================================================================
        private double Vsl_pips;
        private long Vtrade_vol;
        public int? Vstop_loss;
        public int? Vtake_profit;

        // ORDER TYPE (MARKET / STOP / LIMIT)
        private readonly string orderType = "stop".ToLower();
        private readonly bool IsASync = true;

        private double setPricePips;
        private string labelAppend;

        private bool VSAR_above_ind = true;
        private int VSAR_count = 0;

        #endregion


        #region cTRADER EVENTS ============================================================================
        protected override void OnStart()
        {
            Positions.Opened += OnPositionOpen;
            tm = new TradeManager(this);
            tm.PrintTestMessage();

            IndAtr = Indicators.AverageTrueRange(14, MovingAverageType.Simple);
            IndSMA = Indicators.SimpleMovingAverage(SMASource, SMAPeriod);
            IndSAR = Indicators.ParabolicSAR(SARMINAF, SARMAXAF);
            IndDMS = Indicators.DirectionalMovementSystem(DMSPeriod);

            /*
            ===============================================================================================
            DETERMINE STOP LOSS AND TAKE PROFIT VALUES
            ===============================================================================================
            */
            if (TrailingStopTrigger == 0 || IncludeTrailingStop == false)
            {
                Vstop_loss = null;
            }
            else
            {
                Vstop_loss = TrailingStopTrigger;
            }
            // TAKE PROFIT
            // IF SET TO 0 OR USER HAS NOT INCLUDED TAKE PROFIT - SET TO NULL
            if (TakeProfitTrigger == 0 || IncludeTakeProfit == false)
            {
                Vtake_profit = null;
            }
            else
            {
                Vtake_profit = TakeProfitTrigger;
            }
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
        /*
        ===================================================================================================
        POSITION MANAGEMENT
        - TEST STRATEGY LOGIC AND OPEN/CLOSE POSITIONS

        ===================================================================================================
        */
        private void ManagePositions()
        {
            /*
            =============================================================================================== 
            INITIALISATION OF VARS ON EACH BAR CLOSE OR TICK
            ===============================================================================================
            */
            // SET CURRENT SERVER DATE TIME
            DateTime Vsrvr_time = Server.Time;
            // SET TRADE VOLUME BASED ON % BALANCE
            Vsl_pips = (IndAtr.Result.Last(1) * 2) / Symbol.PipSize;
            Vtrade_vol = tm.GetTradeVolumeFromAccountRisk(Vsl_pips, AccountRisk);

            // INDEX: 0 - CURRENT LIVE BAR (NOT FINALISED)
            //        1 - MOST RECENT FINALISED CLOSE BAR
            //        2 - 2ND MOST RECENT FINALISED CLOSE BAR
            var LIndSMA_CURR1 = IndSMA.Result.Last(1);
            var LIndSAR_CURR1 = IndSAR.Result.Last(1);
            var LIndSMA_PREV1 = IndSMA.Result.Last(2);
            var LIndSAR_PREV1 = IndSAR.Result.Last(2);
            
            var LBARCloseVal_CURR1 = MarketSeries.Close.Last(1);
            var LBARCloseVal_PREV1 = MarketSeries.Close.Last(2);

            var LVolume_CURR1 = MarketSeries.TickVolume.Last(1);

            /*
            ===============================================================================================
            PRINT VARS TO LOG
            ===============================================================================================
            */
            Print("BAR CLOSE - START ===============================================================");
            Print("Simple Moving Average (PREV) - {0}", LIndSMA_PREV1);
            Print("Parabolic SAR (PREV) - {0}", LIndSAR_PREV1);
            Print("Close Price (PREV) - {0}", LBARCloseVal_PREV1);
            Print("------------------------------------------------");
            Print("Simple Moving Average (CURR) - {0}", LIndSMA_CURR1);
            Print("Parabolic SAR (CURR) - {0}", LIndSAR_CURR1);
            Print("Close Price (CURR) - {0}", LBARCloseVal_CURR1);
            Print("Tick Volume (CURR) - {0}", LVolume_CURR1);
            Print("BAR CLOSE - END =================================================================");


            #region EXIT LOGIC ========================================================================
            /*
            ===========================================================================================
            EXIT LOGIC FOR "LONG" POSITION
            - EXIT IS TRIPPED WHEN 
            - STOP LOSS OR TAKE PROFIT IS HIT
            - OR 
            - SAR VALUE MOVES ABOVE CLOSE BAR PRICE
            ===========================================================================================
            */
            if (tm.IsPositionOpenByType(TradeType.Buy, InstanceName))
            {
                if(LBARCloseVal_CURR1 < LIndSAR_CURR1)
                {
                    //Print("BUY LBARCloseVal_CURR1 : {0}", LBARCloseVal_CURR1);
                    //Print("BUY LIndSAR_CURR1 : {0}", LIndSAR_CURR1);

                    tm.CloseAllPositionsByType(InstanceName, TradeType.Buy);
                    //tm.ClosePosition(TradeType.Buy, InstanceName);
                }
            }

            /*
            ===========================================================================================
            EXIT LOGIC FOR "SHORT" POSITION
            - EXIT IS TRIPPED WHEN 
            - STOP LOSS OR TAKE PROFIT IS HIT
            - OR 
            - SAR VALUE MOVES BELOW CLOSE BAR PRICE
            ===========================================================================================
            */

            if (tm.IsPositionOpenByType(TradeType.Sell, InstanceName))
            {
                if (LBARCloseVal_CURR1 > LIndSAR_CURR1)
                {
                    //Print("SELL LBARCloseVal_CURR1 : {0}", LBARCloseVal_CURR1);
                    //Print("SELL LIndSAR_CURR1 : {0}", LIndSAR_CURR1);

                    tm.CloseAllPositionsByType(InstanceName, TradeType.Sell);
                    //tm.ClosePosition(TradeType.Sell, InstanceName);
                }
            }
            
            #endregion

            #region ENTRY LOGIC =======================================================================

            /*
            ===========================================================================================
            ENTRY LOGIC FOR "LONG" POSITION
            WHEN MARKET PRICE > SMA
                AND MARKET PRICE > SAR 
            - SET A STOP LOSS AND A TAKE PROFIT
            ===========================================================================================
            */

            if (!tm.IsPositionOpenByType(TradeType.Buy, InstanceName) && LBARCloseVal_PREV1 < LIndSAR_PREV1 && LBARCloseVal_CURR1 > LIndSMA_CURR1 && LBARCloseVal_CURR1 > LIndSAR_CURR1 && LVolume_CURR1 > VOLThreshold) // LIndDMS_ADX_CURR > DMSThreshold && LBARCloseVal_PREV1 < LIndSAR_PREV1 && 
            {
                int n = 0;

                // CHECK IF PREVIOUS VALUES MEET CRITERIA (REMOVE MOVES THAT ONLY LAST 1 OR 2 BARS (NOISE))
                // i = 2 (STARTING PREV INDEX - EG.  PREVIOUS VALUE 2 BARS PRIOR)
                for (int i = 2; i < SARTrail + 2; i++)
                {
                    var LIndSAR_PREV = IndSAR.Result.Last(i);
                    var LBARCloseVal_PREV = MarketSeries.Close.Last(i);
                
                    if (LBARCloseVal_PREV < LIndSAR_PREV)
                    {
                        n = n + 1;    
                    }
                }
                // IF NBR OF SARS PREVIOUS IS ABOVE CLOSING PRICE (STRING DOWNWARD INDICATION) THEN PLACE TRADE ON REVERSAL)
                if ( n == SARTrail )
                {
                    tm.OpenPosition("market", true, 0, TradeType.Buy, Vtrade_vol, InstanceName, Vstop_loss, Vtake_profit, null);
                }
                
            }

            /*
            ===========================================================================================
            ENTRY LOGIC FOR "SHORT" POSITION
            WHEN MARKET PRICE < SMA
             AND MARKET PRICE < SAR 
            - SET A STOP LOSS AND A TAKE PROFIT
            ===========================================================================================
            */

            if (!tm.IsPositionOpenByType(TradeType.Sell, InstanceName) && LBARCloseVal_PREV1 > LIndSAR_PREV1 && LBARCloseVal_CURR1 < LIndSMA_CURR1 && LBARCloseVal_CURR1 < LIndSAR_CURR1 && LVolume_CURR1 > VOLThreshold) // LIndDMS_ADX_CURR > DMSThreshold && LBARCloseVal_PREV1 > LIndSAR_PREV1 && 
            {
                int n = 0;

                // CHECK IF PREVIOUS VALUES MEET CRITERIA (REMOVE MOVES THAT ONLY LAST 1 OR 2 BARS (NOISE))
                // i = 2 (STARTING PREV INDEX - EG.  PREVIOUS VALUE 2 BARS PRIOR)
                for (int i = 2; i < SARTrail + 2; i++)
                {
                    var LIndSAR_PREV = IndSAR.Result.Last(i);
                    var LBARCloseVal_PREV = MarketSeries.Close.Last(i);

                    if (LBARCloseVal_PREV > LIndSAR_PREV)
                    {
                        n = n + 1;
                    }
                }
                // IF NBR OF SARS PREVIOUS IS ABOVE CLOSING PRICE (STRING DOWNWARD INDICATION) THEN PLACE TRADE ON REVERSAL)
                if (n == SARTrail)
                {
                    tm.OpenPosition("market", true, 0, TradeType.Sell, Vtrade_vol, InstanceName, Vstop_loss, Vtake_profit, null);
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
                

            }

            #endregion
        }

        #endregion



    }
}
