# GlobalDispatchTimer

## Overview

This is a safe global timer Dispatcher on Unity API. System globally correct sync by time.
I wanted to make it work on a different thread, but I didn't know how to use it.
So, I decided to work it safely on the main thread in Unity API.
Therefore, you should not be able to dispatch the task that blocks the timer for too long.
When the registered task runs (in the order in which it runs) is "Late Update".
The time that flows from this Dispatcher is based on "realtimeSinceStartUp".

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