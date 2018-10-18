using System;

namespace Global.Timer
{
    /// <summary>
    /// expire 될때까지 매 interval 마다 실행을 반복하는 Job입니다.
    /// 해당 클래스는 IntervalTask 를 상속받습니다.
    /// </summary>
    public class ExpirableIntervalTask : IntervalTask
    {
        public ExpirableIntervalTask(Action task, Func<bool> checkCondition, Action endTask, float delay, float interval, float expire, bool startImmediatly = true)
            : base(task, checkCondition, endTask, delay, interval, startImmediatly)
        {
            GlobalDispatchTimer.Instance.PushTimerJob(Dispose, expire);
        }
    }
}