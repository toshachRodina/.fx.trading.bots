/* 
=====================================================================================================================================================
SUBJECT      : trade management
OBJECT TYPE  : cAlgo
OBJECT NAME  : LTN insideBar
CREATED BY   : Harold Delaney
CREATED ON   : 20180505
SOURCE       : 
PREPERATION  : 
			   
REMARKS      : 1) 3-bar inside bar patter recognition
               2) traded correctly, proportedly supposed to have a 85% win ratio
               3) 

STRATEGY     :
THE INSIDE BAR STRATEGY HAS A SIMILAR BUY AND SELL PATTERN AND DIFFERS ONLY IN THE DIRECTION AS DESCRIBED BELOW

STRATEGY "MUST" CONSIST OF 3 BARS AND ITS PROPERTIES COSIST OF:-
H - HIGH
L - LOW
O - OPEN
C - CLOSE

THE TIME PERIODS ARE INDEXED AS:- 
[0] CURRENT (INNER BAR) - MUST HAVE A LOWER HIGH THAN PREVIOUS HIGH AND HIGHER LOW THATN THE PREVIOUS LOW
[1] PREVIOUS BAR - MUST HAVE A HIGHER HIGH TO INNER
[2] 2 PREVIOUS BAR - MUST      AND WE NEED TO UNDERSTAND THE -

THEREFORE H[0] = HIGH OF THE CURRENT (INNER BAR)
          L[1] = LOW OF THE PREVIOUS BAR TO CURRENT
          L[2] = LOW OF THE BAR 2 PREVIOUS TO CURRENT


** BUY STRATEGY ****************************************************************************************
WHEN H[0] < H[1]
 AND L[0] > L[1]
 AND H[1] > H[2]
 
** SELL STRATEGY ***************************************************************************************
WHEN H[0] < H[1]
 AND L[0] > L[1]
 AND L[1] < L[2]

=====================================================================================================================================================
*/

using System;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;

namespace cAlgo
{
    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.None)]
    public class LTNinsideBar : Robot
    {
        [Parameter(DefaultValue = 0.0)]
        public double Parameter { get; set; }

        protected override void OnStart()
        {
            // Put your initialization logic here
        }

        protected override void OnTick()
        {
            // Put your core logic here
        }

        protected override void OnStop()
        {
            // Put your deinitialization logic here
        }
    }
}
