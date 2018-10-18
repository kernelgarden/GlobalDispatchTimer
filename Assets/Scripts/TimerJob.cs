using System;
using System.Collections;
using System.Collections.Generic;
using Global.Timer.Utils;
using UnityEngine;

namespace Global.Timer
{
	/// <summary>
	/// Global Dispatch Timer에서 돌릴 수 있는 Job을 정의합니다.
	/// TimerJob을 상속받아서 CustomTimerJob을 만들 수 있습니다.
	/// </summary>
	public class TimerJob : IComparable<TimerJob>, IDisposable, IExecutable
	{
		/// <summary>
		/// 이 플래그로 현재 Job의 라이프 사이클을 관리합니다.
		/// </summary>
		protected bool isDeActivated;
		
		protected readonly Action action;
		
		/// <summary>
		/// Global Dispatch Timer에서 Task를 동작시킬 시간입니다.
		/// Global Dispatch TImer는 realTimeSinceStartUp을 기준 틱으로 쓰고 있으므로,
		/// 실행을 예약하려면 realTimeSinceStartUp + 실행까지 남은 시간(초)으로 설정해 주어야 합니다.
		/// </summary>
		public float ExecutedTicks { get; protected set; }
		/*
		{
			get { return executedTicks; }
			protected set { executedTicks = value; }
		}
		*/

		protected TimerJob()
		{ }

		/// <summary>
		/// executedTicks는 GlobalDispatchTimer.Normalize를 사용하면 됩니다.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="executedTicks"></param>
		public TimerJob(Action action, float executedTicks)
		{
			isDeActivated = false;
			
			this.ExecutedTicks = executedTicks;
			this.action = action;
		}

		/// <summary>
		/// 이 메서드가 호출되면, 실행 시점이 되더라도 TimerJob을 실행시키지 않습니다.
		/// </summary>
		public void MarkDisposed()
		{
			isDeActivated = true;
		}
		
		public int CompareTo(TimerJob rhs)
		{
			return ExecutedTicks.CompareTo(rhs.ExecutedTicks);
		}

		protected void SetExcutedTime(float _executedTicks)
		{
			ExecutedTicks = _executedTicks;
		}
		
#if UNITY_EDITOR
		public Action GetExecutor()
		{
			return action;
		}

		public bool IsDisposed()
		{
			return isDeActivated;
		}
#endif

		/// <summary>
		/// Job이 실행되지 않도록 비활성화합니다.
		/// </summary>
		public virtual void Dispose()
		{
			// deactivated 상태로 마킹해두고 실행될 시점에 실행이 안되도록하자..
			MarkDisposed();
			Debugs.Log("TimerJob Dispose 됨");
		}
		
		/// <summary>
		/// 실행 시점이 되었을때 Timer로부터 호출되는 메서드입니다.
		/// 실행 시점에 동작시킬 메서드를 작성해야합니다.
		/// </summary>
		public virtual void Execute()
		{
			if (!isDeActivated)
				action.Execute();
		}
	}
}
