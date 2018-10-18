using System;
using Global.Timer.Utils;
using System.Collections;
using System.Collections.Generic;
using Global.Timer;
using UnityEngine;

namespace Global.Timer
{
    /// <summary>
    /// 매 interval 마다 실행을 반복하는 Job입니다.
    /// </summary>
    public class IntervalTask : TimerJob
    {
        public float Interval { get; private set; }

        /// <summary>
        /// 종료 조건이다. false를 리턴하면 반복을 종료한다.
        /// </summary>
        private readonly Func<bool> CheckCondition;

        /// <summary>
        /// 종료될 때 발동될 이벤트 입니다.
        /// </summary>
        private readonly Action EndTask;

        /// <summary>
        /// startImmediatly가 true이면, delay후에 첫 작업이 시작되고
        /// false라면, delay + interval 시점에 작업이 시작됩니다.
        /// checkCondition이 false면 반복을 종료합니다.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="checkCondition"></param>
        /// <param name="endTask"></param>
        /// <param name="delay"></param>
        /// <param name="interval"></param>
        /// <param name="startImmediatly"></param>
        public IntervalTask(Action task, Func<bool> checkCondition, Action endTask, float delay, float interval,
            bool startImmediatly = true) : base(task, delay)
        {
            Interval = interval;
            CheckCondition = checkCondition ?? (() => true);
            EndTask = endTask;

            var entryReserveTime = startImmediatly
                ? GlobalDispatchTimer.NormalizeTime(delay)
                : GlobalDispatchTimer.NormalizeTime(interval);

            ExecutedTicks = entryReserveTime;
        }

        private void SetInterval()
        {
            ExecutedTicks = GlobalDispatchTimer.NormalizeTime(Interval);
        }

        public override void Execute()
        {
            base.Execute();

            if (!isDeActivated && CheckCondition())
            {
                // Dispose된게 아니라면 interval 후에 다시 반복한다.
                SetInterval();
                GlobalDispatchTimer.Instance.PushTimerJob(this);
            }
            else
            {
                EndTask.Execute();
            }
        }
    }
}
