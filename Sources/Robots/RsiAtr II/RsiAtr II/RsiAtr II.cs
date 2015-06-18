﻿#region Licence
//The MIT License (MIT)
//Copyright (c) 2014 abdallah HACID, https://www.facebook.com/ab.hacid

//Permission is hereby granted, free of charge, to any person obtaining a copy of this software
//and associated documentation files (the "Software"), to deal in the Software without restriction,
//including without limitation the rights to use, copy, modify, merge, publish, distribute,
//sublicense, and/or sell copies of the Software, and to permit persons to whom the Software
//is furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all copies or
//substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
//BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
//NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
//DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

// Project Hosting for Open Source Software on Codeplex : https://calgobots.codeplex.com/
#endregion

#region cBot Infos
// -------------------------------------------------------------------------------
//
//		RsiAtrII (5 Aout 2014)
//		version 2.2014.8.5.13h00
//		Author : https://www.facebook.com/ab.hacid
//
// -------------------------------------------------------------------------------
#endregion

#region cBot Parameters Comments
// Robot using the indicators RSI and ATR
//	
//			Symbol							=	EURUSD
//			TimeFrame						=	H4
//
//			TP Factor						=	2.43					
//			Volatility Factor				=   2.7
//			MM Factor						=	5		//	Money Management
//			RSI Source						=	Close	
//			RSI Period						=	14						
//			RSI Ceil						=	1	
//
//			ATR Period						=	20
//			ATR MAType						=	VIDYA
//
//	Results :
//          Results				=	entre le 01/01/2014 et 5/8/2014 a 13h00 gain de 44559 euros(+89%).
//			Net profit			=	47599.19 euros
//			Ending Equity		=	47599.19 euros
//
// -------------------------------------------------------------------------------
//			Trading using leverage carries a high degree of risk to your capital, and it is possible to lose more than
//			your initial investment. Only speculate with money you can afford to lose.
// -------------------------------------------------------------------------------
#endregion

#region advertisement
// -------------------------------------------------------------------------------
//			Trading using leverage carries a high degree of risk to your capital, and it is possible to lose more than
//			your initial investment. Only speculate with money you can afford to lose.
// -------------------------------------------------------------------------------
#endregion


using System;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Lib;
using cAlgo.Indicators;
 
namespace cAlgo.Robots
{
    [Robot(TimeZone = TimeZones.UTC)]
    public class RsiAtrII : Robot
	{
		#region cBot parameters
		[Parameter("TP Factor", DefaultValue = 2.43, MinValue=0.1)]
		public double TPFactor { get; set; }

        [Parameter("Volatility Factor", DefaultValue = 2.7, MinValue=0.1)]
        public double VolFactor { get; set; }

        [Parameter("MM Factor", DefaultValue = 5, MinValue=0.1)]
        public double MMFactor { get; set; }

        [Parameter("RSI Source")]
		public DataSeries RsiSource { get; set; } // Close

        [Parameter("RSI Period", DefaultValue = 14, MinValue=1)]
        public int RsiPeriod { get; set; }

        [Parameter("RSI Ceil", DefaultValue = 1)]
        public int RsiCeil { get; set; }

        [Parameter("ATR Period", DefaultValue = 20, MinValue=1)]
        public int AtrPeriod { get; set; }
 
        [Parameter("ATR MAType", DefaultValue=4)]
        public MovingAverageType AtrMaType { get; set; } // VIDYA

		#endregion


		private RelativeStrengthIndex rsi;
        private PipsATRIndicator pipsATR;

		// Prefix commands the robot passes
		private const string botPrefix = "RSI-ATR-II";
		// Label orders the robot passes
		private string botLabel;
		double minPipsATR;
		double maxPipsATR;
		double ceilSignalPipsATR;     
		double minRSI;
		double maxRSI;

        protected override void OnStart()
        {
			botLabel = string.Format("{0}-{1} {2}", botPrefix, Symbol.Code, TimeFrame);
            rsi = Indicators.RelativeStrengthIndex(RsiSource, RsiPeriod);
			pipsATR = Indicators.GetIndicator<PipsATRIndicator>(TimeFrame, AtrPeriod, AtrMaType);

			minPipsATR = pipsATR.Result.Minimum(pipsATR.Result.Count);
			maxPipsATR = pipsATR.Result.Maximum(pipsATR.Result.Count);

        }
 
        protected override void OnTick()
        {
			double volatility = pipsATR.Result.lastRealValue(0);
			int minimaxPeriod = (int)((4.0/3.0)*volatility);

			minPipsATR = Math.Min(minPipsATR, pipsATR.Result.LastValue);
			maxPipsATR = Math.Max(maxPipsATR, pipsATR.Result.LastValue);
			minRSI = rsi.Result.Minimum(minimaxPeriod);
			maxRSI = rsi.Result.Maximum(minimaxPeriod);

			ceilSignalPipsATR = minPipsATR + ((maxPipsATR - minPipsATR)/9)*VolFactor;

			if (rsi.Result.LastValue< minRSI)
				this.closeAllSellPositions();
			else
				if (rsi.Result.LastValue>maxRSI)
					this.closeAllBuyPositions();

			// Do nothing if daily ATR > Max allowed
			if (pipsATR.Result.LastValue <= ceilSignalPipsATR)
			{

				if ((!(this.existBuyPositions())) && rsi.Result.HasCrossedAbove(minRSI+1, 0))
				{
					double stopLoss = VolFactor*volatility;
					this.closeAllSellPositions();
					ExecuteMarketOrder(TradeType.Buy, Symbol, this.moneyManagement(MMFactor/100, stopLoss), this.botName(), stopLoss, TPFactor * stopLoss);
				}
				else if (!(this.existSellPositions()) && rsi.Result.HasCrossedBelow(maxRSI-1, 0))
				{
					double stopLoss = VolFactor*volatility;
					this.closeAllBuyPositions();
					ExecuteMarketOrder(TradeType.Sell, Symbol, this.moneyManagement(MMFactor/100, stopLoss), this.botName(), stopLoss, TPFactor * stopLoss);
				}
			}
        }

		protected override void OnStop()
		{
			base.OnStop();
			this.closeAllPositions();

		}
      
    }
}