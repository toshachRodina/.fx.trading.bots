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
                1	ATR	            Risk/Protection	
                2	BASELINE	    Trend Magic Main Line
                3	CONFIRMATION 1	Trend Magic Up/Down Trend
                4	CONFIRMATION 2	Hull Forecast Up/Down Trend
                5	CONFIRMATION 3	Aroon Oscillator
                6	CONFIRMATION 4	TSI
                7	VOLUME	        Average True Range (ATR)
                8	EXIT            Hull Forecast Up/Down Trend
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
    public class RfreSetup1 : Robot
    {
        #region USER DEFINED PARAMATERS ===================================================================

        // INSTANCE & TRADE PARAMS ================================================================
        [Parameter("Instance Name", DefaultValue = "001")]
        public string InstanceName { get; set; }

        [Parameter("Test Mode", DefaultValue = true)]
        public bool TestBool { get; set; }

        [Parameter("Calculate OnBar", DefaultValue = true)]
        public bool CalculateOnBar { get; set; }

        [Parameter("Account Bal. Risk (%)", DefaultValue = 0.02, MinValue = 0.01, MaxValue = 0.08, Step = 0.01)]
        public double AccountRisk { get; set; }

        [Parameter("Reserve Funds", DefaultValue = 0)]
        public int ReserveFunds { get; set; }

        [Parameter("Max Volume", DefaultValue = 5000000, MaxValue = 10000000)]
        public int MaxVolume { get; set; }

        [Parameter("Commission in Pips", DefaultValue = 0.0, MinValue = 0.0, MaxValue = 5.0, Step = 0.01)]
        public double CommissionPips { get; set; }

        // EMAIL PARAMATERS =======================================================================
        [Parameter("-- EMAIL DETAILS ----------------------", DefaultValue = "")]
        public string DoNothing01 { get; set; }

        [Parameter("From Email", DefaultValue = "lanoitan17@gmail.com")]
        public String FromEmail { get; set; }

        [Parameter("To Email", DefaultValue = "toshach@gmail.com")]
        public String ToEmail { get; set; }

        // INDICATOR PARAMATERS ===================================================================
        [Parameter("-- INDICATORS -------------------------", DefaultValue = "")]
        public string DoNothing04 { get; set; }

        [Parameter("ATR Period", DefaultValue = 14, MinValue = 10, MaxValue = 20)]
        public int ATRPeriod { get; set; }

        [Parameter("TrendMagic CCI", DefaultValue = 45, MinValue = 25, MaxValue = 65)]
        public int TMCCI { get; set; }

        [Parameter("TrendMagic ATR", DefaultValue = 5, MinValue = 3, MaxValue = 15)]
        public int TMATR { get; set; }

        [Parameter("Aroon Oscillator Period", DefaultValue = 10, MinValue = 8, MaxValue = 25)]
        public int ArroonPeriod { get; set; }

        [Parameter("Hull Forecast Period", DefaultValue = 10, MinValue = 8, MaxValue = 20)]
        public int HFPeriod { get; set; }

        [Parameter("Hull Coverage Period", DefaultValue = 0.8, MinValue = 0.6, MaxValue = 1.4)]
        public double HFCPeriod { get; set; }

        [Parameter("ROC Period", DefaultValue = 16, MinValue = 10, MaxValue = 26)]
        public int ROCPeriod { get; set; }

        [Parameter("TSI Short Period", DefaultValue = 13, MinValue = 10, MaxValue = 20)]
        public int TSIShortPeriod { get; set; }

        [Parameter("TSI Long Period", DefaultValue = 25, MinValue = 20, MaxValue = 35)]
        public int TSILongPeriod { get; set; }

        [Parameter("TSI Signal Period", DefaultValue = 7, MinValue = 5, MaxValue = 15)]
        public int TSISignalPeriod { get; set; }

        #endregion


        #region PRIVATE INDICATOR DECLARATIONS ============================================================

        private AverageTrueRange Vatr { get; set; }

        // DECLERATION OF CUSTOM INDICATOR
        //private OSMA Vosma;
        //private FisherTransform Vfishert;
        private RateOfChange Vroc;
        private HullForcast Vhfc;
        private TrendMagic Vtm;
        private AroonOscilator Vao;
        private TSI Vtsi;

        // DECLERATIONS GENERIC
        public int? stopLoss;
        public int? takeProfit;

        //private int i = 0;
        private int barcount = 0;
        private int tradecount = 0;

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
            //VindIchi = Indicators.IchimokuKinkoHyo(9, KijunSen, 52);
            // AVERAGE TRUE RANGE
            Vatr = Indicators.AverageTrueRange(ATRPeriod, MovingAverageType.Simple);

            //  CUSTOM INDICATORS
            // AROON OSCILLATOR
            Vao = Indicators.GetIndicator<AroonOscilator>(ArroonPeriod);
            // HULL FORECAST
            Vhfc = Indicators.GetIndicator<HullForcast>(HFPeriod, HFCPeriod, MarketSeries.Close, false, "");
            // INCORRECT NUMBER OF PARAMATERS
            // RATE OF CHANGE
            Vroc = Indicators.GetIndicator<RateOfChange>(MarketSeries.Close, ROCPeriod);
            // TREND MAGIC
            Vtm = Indicators.GetIndicator<TrendMagic>(TMCCI, TMATR);
            // TRUE STRENGTH INDEX
            Vtsi = Indicators.GetIndicator<TSI>(MarketSeries.Close, TSIShortPeriod, TSILongPeriod, TSISignalPeriod, MovingAverageType.Weighted);
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
            // MarketSeries.Open.Count - 1;
            // INITIALISE ELEMENTS ONBAR CLOSE
            // INDEX: 0 - CURRENT LIVE BAR (NOT FINALISED)
            //        1 - MOST RECENT FINALISED CLOSE BAR
            //        2 - 2ND MOST RECENT FINALISED CLOSE BAR
            //        etc.
            // ======================================================================================================================================
            // CURRENT COMPLETE PERIOD (-1 bar from current) 
            // ======================================================================================================================================

            // LOGIC DECLERATIONS
            var index = 1;

            // CONVERT NaN's
            double tmUpTrend = Vtm.UpTrend.Last(index);
            double tmDownTrend = Vtm.DownTrend.Last(index);
            double hfUpSeries = Vhfc.UpSeries.Last(index);
            double hfDownSeries = Vhfc.DownSeries.Last(index);
            double arVal = Vao.Positive.Last(index) + Vao.Negative.Last(index);

            if (Double.IsNaN(tmUpTrend))
            {
                tmUpTrend = 0;
            }
            if (Double.IsNaN(tmDownTrend))
            {
                tmDownTrend = 0;
            }
            if (Double.IsNaN(hfUpSeries))
            {
                hfUpSeries = 0;
            }
            if (Double.IsNaN(hfDownSeries))
            {
                hfDownSeries = 0;
            }


            //string screenDetails = String.Join(";", "Current Balance / Equity : " + Account.Balance.ToString() + " / " + Account.Equity.ToString(), "Close Price : " + MarketSeries.Close.Last(index), "Average True Range : " + Math.Round(Vatr.Result[index], 3, MidpointRounding.AwayFromZero).ToString(), "Trend Magic Main / Up / Down : " + Math.Round(Vtm.MTrend.Last(index), 3, MidpointRounding.AwayFromZero).ToString() + " / " + Math.Round(Vtm.UpTrend.Last(index), 3, MidpointRounding.AwayFromZero).ToString() + Math.Round(Vtm.DownTrend.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Hull Forecast Up / Down : " + Math.Round(Vhfc.UpSeries.Last(index), 3, MidpointRounding.AwayFromZero).ToString() + " / " + Math.Round(Vhfc.DownSeries.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Aroon Oscillator : " + Math.Round(Math.Abs(Vao.Negative.Last(index)), 3, MidpointRounding.AwayFromZero).ToString(), "True Strength Index : " + Math.Round(Vtsi.Tsi.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Rate Of Change : " + Math.Round(Vroc.rocline.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Bar / Trade Count : " + barcount.ToString() + " / " + tradecount.ToString());
            //PrintOutput(screenDetails, "screen");

            string screenDetails = String.Join(";", "Bar / Trade Count : " + barcount.ToString() + " / " + tradecount.ToString(), "Current Balance / Equity : " + Account.Balance.ToString() + " / " + Account.Equity.ToString(), "Close Price : " + MarketSeries.Close.Last(1), "ATR / 1.5 Pip / 2.0 Pip : " + Math.Round(Vatr.Result.LastValue, 5).ToString() + " / " + Math.Round(CalculateSLDistance(Vatr.Result.LastValue, ""), 0, MidpointRounding.ToEven).ToString() + " / " + Math.Round(CalculateSLDistance(Vatr.Result.LastValue, "continuation"), 0, MidpointRounding.ToEven).ToString(), "TrendMagic Line / Up / Down : " + Math.Round(Vtm.MTrend.Last(index), 5, MidpointRounding.AwayFromZero).ToString() + " / " + Math.Round(tmUpTrend, 5, MidpointRounding.AwayFromZero).ToString() + " / " + Math.Round(tmDownTrend, 5, MidpointRounding.AwayFromZero).ToString(), "Hull Forecast Up / Down : " + Math.Round(hfUpSeries, 5).ToString() + " / " + Math.Round(hfDownSeries, 5).ToString(), "Aroon Oscillator : " + Math.Round(arVal, 5, MidpointRounding.AwayFromZero).ToString(), "True Strength Index : " + Math.Round(Vtsi.Tsi.Last(index), 5, MidpointRounding.AwayFromZero).ToString(), "Rate Of Change : " + Math.Round(Vroc.rocline.Last(index), 5, MidpointRounding.AwayFromZero).ToString());
            PrintOutput(screenDetails, "screen");

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // BELOW THIS POINT TRADES WILL BE MADE IF ISTESTING IS SET FALSE
            // * TEST VARIABLES AND VALUES ABOVE THIS SECTION
            // 
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            #region EXIT LOGIC ========================================================================

            // ========================================================================================
            // SHORT POSITIONS
            //
            // ========================================================================================
            if (IsPositionOpen(TradeType.Sell))
            {
                if (MarketSeries.Close.Last(1) > Vtm.MTrend.Last(index))
                {
                    // BASELINE IS CROSSED IN OPPOSING TREND DIRECTION AND MUST CLOSE
                    if (MarketSeries.Close.Last(2) < Vtm.MTrend.Last(index))
                    {
                        CloseAllPositions();
                        // RESET BARCOUNT ON CLOSE OF POSITION
                        barcount = 0;
                        // RESET TRADECOUNT ON CLOSE OF NEW POSITION AND NEW TREND DIRECTION
                        tradecount = 0;
                    }
                }
                // REMAINS UNDER BASELINE BUT CHECK IF EITHER CONFIRMATIONS HAVE CROSSED UNFAVOURABLY AND MUST CLOSE
                else if (hfUpSeries > hfDownSeries)
                {
                    CloseAllPositions();
                    // RESET BARCOUNT ON CLOSE OF POSITION
                    barcount = 0;
                }
                // TRADE CONTINUES - CHECK FOR TP AND SL THRESHOLDS AND ADJUST ACCORDINGLY
                else
                {
                    // !!!!! THIS ISNT AT OPEN AND IS CURRENT ATRx2 !!!!!
                    if (MarketSeries.Close.Last(1) <= LastResult.Position.EntryPrice - CalculateSLDistance(Vatr.Result.LastValue, "continuation"))
                    {
                        // TAKE 50% OF TRADE AND THEN ADJUST SL TO 1.5 ATR AND REPEAT FOR NEXT BAR (MINUS TP STEP) 
                        var position = Positions.Find(InstanceName, SymbolName);

                        if (position != null)
                        {
                            ClosePosition(position, (CalculateVolume() / 2));
                            // !!!!! NEED TO CHECK THIS VOLUME IS CORRECT - NEED TO CALCULATE 50% OF ENTERED TRADE AND CLOSE CRIERIA WITH THAT !!!!!!
                            ModifyPosition(position, MarketSeries.Close.Last(index) + CalculateSLDistance(Vatr.Result.LastValue, "continuation"), null);
                            // AS PER ENTRY TRADE - NO TP IS SET AS OUR RULES DICTATE WHEN WE BREAK OUT OF TRADE 
                        }
                    }
                }
            }
            // ========================================================================================
            // LONG POSITIONS
            //
            // ========================================================================================
            else if (IsPositionOpen(TradeType.Buy))
            {
                if (MarketSeries.Close.Last(1) < Vtm.MTrend.Last(index))
                {
                    // BASELINE IS CROSSED IN OPPOSING TREND DIRECTION AND MUST CLOSE
                    if (MarketSeries.Close.Last(2) > Vtm.MTrend.Last(index))
                    {
                        CloseAllPositions();
                        // RESET BARCOUNT ON CLOSE OF POSITION
                        barcount = 0;
                        // RESET TRADECOUNT ON CLOSE OF NEW POSITION AND NEW TREND DIRECTION
                        tradecount = 0;
                    }
                }
                // REMAINS OVER BASELINE BUT CHECK IF EITHER CONFIRMATIONS HAVE CROSSED UNFAVOURABLY AND MUST CLOSE
                else if (hfUpSeries < hfDownSeries)
                {
                    CloseAllPositions();
                    // RESET BARCOUNT ON CLOSE OF POSITION
                    barcount = 0;
                }
                // TRADE CONTINUES - CHECK FOR TP AND SL THRESHOLDS AND ADJUST ACCORDINGLY
                else
                {
                    if (MarketSeries.Close.Last(1) >= LastResult.Position.EntryPrice + CalculateSLDistance(Vatr.Result.LastValue, "continuation"))
                    {
                        // TAKE 50% OF TRADE AND THEN ADJUST SL TO 1.5 ATR AND REPEAT FOR NEXT BAR (MINUS TP STEP) 
                        var position = Positions.Find(InstanceName, SymbolName);

                        if (position != null)
                        {
                            ClosePosition(position, (position.VolumeInUnits / 2));
                            // !!!!! NEED TO CHECK THIS VOLUME IS CORRECT - NEED TO CALCULATE 50% OF ENTERED TRADE AND CLOSE CRIERIA WITH THAT !!!!!!
                            ModifyPosition(position, MarketSeries.Close.Last(1) - CalculateSLDistance(Vatr.Result.LastValue, "continuation"), null);
                            // AS PER ENTRY TRADE - NO TP IS SET AS OUR RULES DICTATE WHEN WE BREAK OUT OF TRADE 
                        }
                    }
                }
            }

            #endregion

            #region ENTRY LOGIC =======================================================================

            // ========================================================================================
            // SHORT POSITIONS
            //
            // ========================================================================================
            // BASELINE CROSS
            if (MarketSeries.Close.Last(1) < Math.Round(Vtm.MTrend.Last(index), 5, MidpointRounding.AwayFromZero))
            {
                // CHECKS FOR TREND DIRECTION CHANGE
                if (MarketSeries.Close.Last(2) > Vtm.MTrend.Last(index))
                {
                    // INITIALISE BARCOUNT IF IN NEW DIRECTIONAL TREND FROM BASELINE IS SHORT
                    barcount = 0;
                    // INITIALISE TRADECOUNT IF IN NEW DIRECTIONAL TREND FROM BASELINE IS SHORT
                    tradecount = 0;

                }

                // ONLY 1 TRADE OPEN PER PAIR PER SESSION
                // NEEDS LOGIC TO MAKE SURE RISK ON PAIR IS MINIMILISED   (EG. LONG ON AUDUSD and GBPUSD MEANS WE HAVE INCREASED RISK ON USD)
                // NEEDS LOGIC TO MAKE SURE TRADE DOESNT COUNTER ANOTHER TRADE (EG. LONG ON AUDUSD and USDCAD MEANS IF ACTION ON USD, ONE TRADE MAY CANCEL THE OTHER IN TERMS OF MOVEMENT)
                if (Positions.FindAll(InstanceName, SymbolName).Length < 1)
                {
                    // CONTINUATION TRADE
                    // TRADECOUNT HAS NOT BEEN RESET DUE TO MARKET CHANGING TREND DIRECTION THEREFORE POSSIBLE CONTINUATION
                    if (tradecount > 0)
                    {
                        if (MarketSeries.Close.Last(1) < Vtm.MTrend.Last(index) && tmUpTrend < tmDownTrend && hfUpSeries < hfDownSeries && arVal < 0 && Vtsi.Tsi.Last(index) < 0)
                        {
                            // ENTER TRADE
                            ExecuteMarketOrder(TradeType.Sell, SymbolName, CalculateVolume(), InstanceName, MarketSeries.Close.Last(1) + CalculateSLDistance(Vatr.Result.LastValue, ""), null);

                            string logDetails = String.Join(";", "ENTER SHORT ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^", "Current Balance : " + Account.Balance.ToString(), "Equity : " + Account.Equity.ToString(), "Close Price : " + MarketSeries.Close.Last(1), "Trend Magic Main Line: " + Math.Round(Vtm.MTrend.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Trend Magic Up Trend: " + Math.Round(Vtm.UpTrend.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Trend Magic Down Trend:  : " + Math.Round(Vtm.DownTrend.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Hull Forecast Up Series : " + Math.Round(Vhfc.UpSeries.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Hull Forecast Down Series : " + Math.Round(Vhfc.DownSeries.Last(index), 3, MidpointRounding.AwayFromZero).ToString(),
                            "Aroon Oscillator : " + Math.Round(Math.Abs(Vao.Negative.Last(index)), 3, MidpointRounding.AwayFromZero).ToString(), "True Strength Index : " + Math.Round(Vtsi.Tsi.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Rate Of Change : " + Math.Round(Vroc.rocline.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Average True Range : " + Math.Round(Vatr.Result[index], 3, MidpointRounding.AwayFromZero).ToString());

                            PrintOutput(logDetails, "log");

                            // RESET BARCOUNT ONCE A TRADE IS ENTERED
                            barcount = 0;

                            // SET COUNT FOR TRADE IN THIS TREND
                            tradecount += 1;
                        }
                    }
                    // INITIAL TRADE
                    else if (barcount < 7 && MarketSeries.Close.Last(1) < Vtm.MTrend.Last(index) && Vroc.rocline.Last(index) < 0 && tmUpTrend < tmDownTrend && hfUpSeries < hfDownSeries && arVal < 0 && Vtsi.Tsi.Last(index) < 0)
                    {
                        // ENTER TRADE
                        ExecuteMarketOrder(TradeType.Sell, SymbolName, CalculateVolume(), InstanceName, MarketSeries.Close.Last(1) + CalculateSLDistance(Vatr.Result.LastValue, ""), null);

                        string logDetails = String.Join(";", "ENTER SHORT ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^", "Current Balance : " + Account.Balance.ToString(), "Equity : " + Account.Equity.ToString(), "Close Price : " + MarketSeries.Close.Last(1), "Trend Magic Main Line: " + Math.Round(Vtm.MTrend.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Trend Magic Up Trend: " + Math.Round(Vtm.UpTrend.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Trend Magic Down Trend:  : " + Math.Round(Vtm.DownTrend.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Hull Forecast Up Series : " + Math.Round(Vhfc.UpSeries.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Hull Forecast Down Series : " + Math.Round(Vhfc.DownSeries.Last(index), 3, MidpointRounding.AwayFromZero).ToString(),
                        "Aroon Oscillator : " + Math.Round(Math.Abs(Vao.Negative.Last(index)), 3, MidpointRounding.AwayFromZero).ToString(), "True Strength Index : " + Math.Round(Vtsi.Tsi.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Rate Of Change : " + Math.Round(Vroc.rocline.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Average True Range : " + Math.Round(Vatr.Result[index], 3, MidpointRounding.AwayFromZero).ToString());

                        PrintOutput(logDetails, "log");

                        // RESET BARCOUNT ONCE A TRADE IS ENTERED
                        barcount = 0;
                        // SET COUNT FOR TRADE IN THIS TREND
                        tradecount = 1;
                    }
                    else
                    {
                        // BARCOUNT INCREASE TO TRACK CONSECUTIVE BARS UNDER BASELINE AND WITHOUT TRADE
                        barcount += 1;
                    }
                }
            }
            // ========================================================================================
            // LONG POSITIONS
            //
            // ========================================================================================
            else if (MarketSeries.Close.Last(1) > Math.Round(Vtm.MTrend.Last(index), 5, MidpointRounding.AwayFromZero))
            {
                if (MarketSeries.Close.Last(2) < Vtm.MTrend.Last(index))
                {
                    // INITIALISE BARCOUNT IF IN NEW DIRECTIONAL TREND FROM BASELINE IS SHORT
                    barcount = 0;
                    // INITIALISE TRADECOUNT IF IN NEW DIRECTIONAL TREND FROM BASELINE IS SHORT
                    tradecount = 0;
                }

                // ONLY 1 TRADE OPEN PER PAIR PER SESSION
                // NEEDS LOGIC TO MAKE SURE WE DONT INCREASE RISK ON PAIR   (EG. LONG ON AUDUSD and GBPUSD MEANS WE HAVE INCREASED RISK ON USD)
                // NEEDS LOGIC TO MAKE SURE WE DONT TRADE AGAINST OURSELVES (EG. LONG ON AUDUSD and USDCAD MEANS IF ACTION ON USD, ONE TRADE MAY CANCEL THE OTHER IN TERMS OF MOVEMENT)
                if (Positions.FindAll(InstanceName, SymbolName).Length == 0)
                {
                    // CONTINUATION TRADE
                    if (tradecount > 0)
                    {
                        if (MarketSeries.Close.Last(1) > Vtm.MTrend.Last(index) && tmUpTrend > tmDownTrend && hfUpSeries > hfDownSeries && arVal > 0 && Vtsi.Tsi.Last(index) > 0)
                        {
                            // ENTER TRADE
                            ExecuteMarketOrder(TradeType.Buy, SymbolName, CalculateVolume(), InstanceName, MarketSeries.Close.Last(1) - CalculateSLDistance(Vatr.Result.LastValue, ""), null);

                            string logDetails = String.Join(";", "ENTER LONG ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^", "Current Balance : " + Account.Balance.ToString(), "Equity : " + Account.Equity.ToString(), "Close Price : " + MarketSeries.Close.Last(1), "Trend Magic Main Line: " + Math.Round(Vtm.MTrend.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Trend Magic Up Trend: " + Math.Round(Vtm.UpTrend.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Trend Magic Down Trend:  : " + Math.Round(Vtm.DownTrend.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Hull Forecast Up Series : " + Math.Round(Vhfc.UpSeries.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Hull Forecast Down Series : " + Math.Round(Vhfc.DownSeries.Last(index), 3, MidpointRounding.AwayFromZero).ToString(),
                            "Aroon Oscillator : " + Math.Round(Math.Abs(Vao.Negative.Last(index)), 3, MidpointRounding.AwayFromZero).ToString(), "True Strength Index : " + Math.Round(Vtsi.Tsi.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Rate Of Change : " + Math.Round(Vroc.rocline.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Average True Range : " + Math.Round(Vatr.Result[index], 3, MidpointRounding.AwayFromZero).ToString());

                            PrintOutput(logDetails, "log");

                            // RESET BARCOUNT ONCE A TRADE IS ENTERED
                            barcount = 0;

                            // SET COUNT FOR TRADE IN THIS TREND
                            tradecount += 1;
                        }
                    }
                    // INITIAL TRADE
                    // && indATRFast > indATRSlow && MarketSeries.Close.Last(1) < (indKijunSen_curr + indATRMid))
                    else if (barcount < 7 && MarketSeries.Close.Last(1) > Vtm.MTrend.Last(index) && Vroc.rocline.Last(index) > 0 && tmUpTrend > tmDownTrend && hfUpSeries > hfDownSeries && arVal > 0 && Vtsi.Tsi.Last(index) > 0)
                    {
                        // ENTER TRADE
                        ExecuteMarketOrder(TradeType.Buy, SymbolName, CalculateVolume(), InstanceName, MarketSeries.Close.Last(1) - CalculateSLDistance(Vatr.Result.LastValue, ""), null);

                        string logDetails = String.Join(";", "ENTER LONG ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^", "Current Balance : " + Account.Balance.ToString(), "Equity : " + Account.Equity.ToString(), "Close Price : " + MarketSeries.Close.Last(index), "Trend Magic Main Line: " + Math.Round(Vtm.MTrend.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Trend Magic Up Trend: " + Math.Round(Vtm.UpTrend.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Trend Magic Down Trend:  : " + Math.Round(Vtm.DownTrend.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Hull Forecast Up Series : " + Math.Round(Vhfc.UpSeries.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Hull Forecast Down Series : " + Math.Round(Vhfc.DownSeries.Last(index), 3, MidpointRounding.AwayFromZero).ToString(),
                        "Aroon Oscillator : " + Math.Round(Math.Abs(Vao.Negative.Last(index)), 3, MidpointRounding.AwayFromZero).ToString(), "True Strength Index : " + Math.Round(Vtsi.Tsi.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Rate Of Change : " + Math.Round(Vroc.rocline.Last(index), 3, MidpointRounding.AwayFromZero).ToString(), "Average True Range : " + Math.Round(Vatr.Result[index], 3, MidpointRounding.AwayFromZero).ToString());

                        PrintOutput(logDetails, "log");

                        // RESET BARCOUNT ONCE A TRADE IS ENTERED
                        barcount = 0;
                        // SET COUNT FOR TRADE IN THIS TREND
                        tradecount = 1;
                    }
                    else
                    {
                        // BARCOUNT INCREASE TO TRACK CONSECUTIVE BARS UNDER BASELINE AND WITHOUT TRADE
                        barcount += 1;
                    }
                }
            }

            #endregion
        }

        #endregion


        #region TRADE MANAGEMENT ==========================================================================

        public void PrintOutput(string msg, string dest)
        {
            int i = 1;

            String[] seperator = 
            {
                ";"
            };
            String[] msgArray = msg.Split(seperator, StringSplitOptions.RemoveEmptyEntries);

            foreach (String s in msgArray)
            {
                if (dest == "log")
                {
                    Print(s);
                }
                else if (dest == "screen")
                {
                    Chart.DrawStaticText(i.ToString(), String.Concat(Enumerable.Repeat("\n", i - 1)) + s, VerticalAlignment.Top, HorizontalAlignment.Right, Color.AntiqueWhite);
                    i++;
                }
                else
                {
                    Print(msg);
                    Chart.DrawStaticText(i.ToString(), String.Concat(Enumerable.Repeat("\n", i - 1)) + s, VerticalAlignment.Top, HorizontalAlignment.Right, Color.AntiqueWhite);
                    i++;
                }

            }
        }

        public void CloseAllPositions()
        {
            var p = Positions.Find(InstanceName, SymbolName);

            if (p != null)
            {
                ClosePosition(p);
            }
        }

        public bool IsPositionOpen(TradeType type)
        {
            var p = Positions.FindAll(InstanceName, SymbolName, type);

            if (p.Count() >= 1)
            {
                return true;
            }

            return false;
        }

        private double CalculateVolume()
        {
            // Our total balance is our account balance plus any reserve funds. We do not always keep all our money in the trading account. 
            double totalBalance = Account.Balance + ReserveFunds;

            // Calculate the total risk allowed per trade.
            double riskPerTrade = (totalBalance * AccountRisk) / 100;

            double stopLoss = CalculateSLDistance(Vatr.Result.LastValue, "");
            //MarketSeries.Close.Last(1) + Vatr.Result.LastValue;
            // Add the stop loss, commission pips and spread to get the total pips used for the volume calculation.
            double totalPips = stopLoss + CommissionPips + Symbol.Spread;

            // Calculate the exact volume to be traded. Then round the volume to the nearest 100,000 and convert to an int so that it can be returned to the caller.
            double exactVolume = Math.Round(riskPerTrade / (Symbol.PipValue * totalPips), 2);

            //Choose your own rounding mode
            exactVolume = (double)Symbol.NormalizeVolumeInUnits(exactVolume, RoundingMode.Down);

            // Finally, check that the calculated volume is not greater than the MaxVolume parameter. If it is greater, reduce the volume to the MaxVolume value.
            if (exactVolume > (double)MaxVolume)
            {
                exactVolume = (double)MaxVolume;
            }

            return exactVolume;
        }

        static double CalculateSLDistance(double atrVal, string lookup)
        {
            int decPlcs = atrVal.ToString().Length - (((int)atrVal).ToString().Length + 1);
            int multiplier;
            double distMult;


            if (decPlcs <= 3)
            {
                multiplier = 100;
            }
            else
            {
                multiplier = 10000;
            }

            if (lookup == "continuation")
            {
                distMult = 2.0;
            }
            else
            {
                distMult = 1.5;
            }


            double result = (atrVal * multiplier) * distMult;
            return result;
        }

        /*
        static double CalculateRiskAmount(double balance, double? reserveFunds, double acctRisk)
        {
            if (string.IsNullOrEmpty(reserveFunds.ToString()))
            {
                reserveFunds = Convert.ToDouble(0);
            }

            double result = (Convert.ToDouble(balance) + Convert.ToDouble(reserveFunds)) * acctRisk;

            return result;
        }
        */
        static decimal CountDecimalPlaces(decimal dec)
        {
            //https://stackoverflow.com/questions/6092243/c-sharp-check-if-a-decimal-has-more-than-3-decimal-places
            int[] bits = Decimal.GetBits(dec);
            int exponent = bits[3] >> 16;
            int result = exponent;
            long lowDecimal = bits[0] | (bits[1] >> 8);
            while ((lowDecimal % 10) == 0)
            {
                result--;
                lowDecimal /= 10;
            }

            return result;
        }

        #endregion

    }
}
