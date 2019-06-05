# GlobalDispatchTimer

## Overview

Unity API에 Safe한 글로벌 타이머 디스패처 입니다. 시스템 전역적으로 time에 의한 sync가 맞습니다.
다른 스레드에서 돌리고 싶었지만 어떻게 쓰일지 몰라 Unity API에 안전하게 메인 스레드에서 돌리도록 했습니다.
따라서, 이 타이머에 너무 오래 blocking 되는 작업을 dispatch하면 안됩니다.
등록된 작업이 실행되는 시점은(유니티 이벤트 실행 순서에서) LateUpdate입니다.
이 디스패처에서 흐르는 시간은 realtimeSinceStartUp을 기준으로 두고 있습니다.
마땅한 엔트리 포인트를 찾지 못하여 우선은, GameSettingManager가 Init되는 시점에 생성되도록 하였습니다.

## API

Generates a task that will run immediately:

```c#
GlobalDispatchTimer.Instance.PushJob(() => { Debug.Log("Task A"); });
```

Generates a task to run after n seconds(with float data type):

```c#
IDisposable disposal = GlobalDispatchTimer.Instance.PushTimerJob(() => { Debug.Log("Calleed after 10 seconds"); }, 10);
```

Generates a task to run after n seconds(with TimeSpan data type):

```c#
DateTime dateTimeNow = DateTime.Now;
TimeSpan timeSpan = dateTimeReminder - dateTimeNow;
IDisposable disposal = GlobalDispatchTimer.Instance.PushTimerJob(() => { Debug.Log("Calleed"); }, timeSpan);
```


Generates a task to run at a specific time:

```c#
DateTime dateTime = DateTime.Today.AddDays(1);
IDisposable disposal = GlobalDispatchTimer.Instance.PushTimerJob(() => { Debug.Log("Calleed after 1 day"); }, dateTime);
```

Generates a task to run after n seconds(with float data type), when a job is canceled if the gameObject bindTarget is destroyed:

```c#
GameObject go = GameObject.Find("Player");
IDisposable disposal = GlobalDispatchTimer.Instance.PushTimerJob(() => { Debug.Log("Calleed after 10 seconds"); }, 10, go);
```

Generates a task to run after n seconds(with TimeSpan data type), when a job is canceled if the gameObject bindTarget is destroyed:

```c#
GameObject go = GameObject.Find("Player");
DateTime dateTimeNow = DateTime.Now;
TimeSpan timeSpan = dateTimeReminder - dateTimeNow;
IDisposable disposal = GlobalDispatchTimer.Instance.PushTimerJob(() => { Debug.Log("Calleed"); }, timeSpan, go);
```

Generates a task to run at a specific time, when a job is canceled if the gameObject bindTarget is destroyed:

```c#
GameObject go = GameObject.Find("Player");
DateTime dateTime = DateTime.Today.AddDays(1);
IDisposable disposal = GlobalDispatchTimer.Instance.PushTimerJob(() => { Debug.Log("Calleed after 1 day"); }, dateTime, go);
```

## Custom Task

Define the jobs available in the Global Dispatch Timer. You can create a custom TimerJob by inheriting a TimerJob.

### IntervalTask

How to define a task that repeats at a specific interval:

```c#
public class IntervalTask : TimerJob
{
    public float Interval { get; private set; }

    /// <summary>
    /// It's an exit condition. If you return false, you end iteration.
    /// </summary>
    private readonly Func<bool> CheckCondition;

    /// <summary>
    /// Events to be triggered when the task is finished.
    /// </summary>
    private readonly Action EndTask;

    /// <summary>
    /// If startImmediatly is true, the first operation starts after delay. 
    /// If false, the job starts at the delay + interval. 
    /// If checkCondition is false, end the repeat.
    /// </summary>
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
            // If not disposed, repeat after interval.
            SetInterval();
            GlobalDispatchTimer.Instance.PushTimerJob(this);
        }
        else
        {
            EndTask.Execute();
        }
    }
}
```

### ExpirableIntervalTask

How to define a task that repeats at a certain interval until it expires:

```c#
public class ExpirableIntervalTask : IntervalTask
{
    public ExpirableIntervalTask(Action task, Func<bool> checkCondition, Action endTask, float delay, float interval, float expire, bool startImmediatly = true)
        : base(task, checkCondition, endTask, delay, interval, startImmediatly)
    {
        GlobalDispatchTimer.Instance.PushTimerJob(Dispose, expire);
    }
}
```