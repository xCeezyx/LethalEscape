using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LethalEscape
{
	internal class LETimerFunctions : MonoBehaviour
	{
		public static float MinuteEscapeTimerPuffer;
		public static float MinuteEscapeTimerBracken;
		public static float MinuteEscapeTimerHoardingBug;

		public static LETimerFunctions instance;
		private void Awake()
		{
			instance = this;
		}
		private void Update()
		{
			MinuteEscapeTimerBracken += Time.deltaTime;
			MinuteEscapeTimerHoardingBug += Time.deltaTime;
			MinuteEscapeTimerPuffer += Time.deltaTime;
		}
	}

}
