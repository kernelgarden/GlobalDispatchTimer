using System.Collections;
using System.Collections.Generic;
using Global.Timer.Utils;
using UnityEngine;

namespace Global.Timer
{
	// 테스트용 클래스입니다..
	public class CustomTimerTaskObject : TimerJob
	{
		private static int issuedKey = 0;
		private int secretKey = 0;
		
		public CustomTimerTaskObject()
		{
			secretKey = ++issuedKey;

			float reservedTime = Time.realtimeSinceStartup + UnityEngine.Random.Range(3, 10);
			ExecutedTicks = reservedTime;
			Debugs.LogFormat("CustomTimerTaskObject #{0} reserved at {0}, current: {1}", reservedTime, Time.realtimeSinceStartup);
		}

		public override void Dispose()
		{
			Debugs.LogFormat("Disposed CustomTimerTaskObject #{0}", secretKey);
			base.Dispose();
		}
		
		public override void Execute()
		{
			int random = UnityEngine.Random.Range(0, 2);
			if (random % 2 == 0)
			{
				Dispose();
			}
			
			if (!isDeActivated)
			{
				Debugs.LogFormat("Executed CustomTimerTaskObject #{0}", secretKey);
			}
		}
	}
}
