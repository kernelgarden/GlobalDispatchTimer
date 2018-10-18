using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Global.Timer;
using Global.Timer.Utils;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(GlobalDispatchTimer))]
public class GlobalDispatchMonitor : Editor
{
    public GlobalDispatchTimer This { get; set; }

    private void OnEnable()
    {
        This = (GlobalDispatchTimer)target;
    }
    
    //TODO: 예약된 task list 보는 기능 추가 및 모니터 개선

    public override void OnInspectorGUI()
    {
        if (!Application.isPlaying)
            return;

        EditorGUILayout.TextField(string.Format("GlobalDispatchTimer 상태: {0}, 예약된 task 수: {1}", 
            GlobalDispatchTimer.Instance.Status().ToString(), GlobalDispatchTimer.Instance.Count()));
        
        if (GUILayout.Button("글로벌 타이머 중지"))
        {
            GlobalDispatchTimer.Instance.Stop();
        }

        if (GUILayout.Button("글로벌 타이머 재개"))
        {
            GlobalDispatchTimer.Instance.Resume();
        }

        if (GUILayout.Button("글로벌 타이머 클리어"))
        {
            GlobalDispatchTimer.Instance.Reset();
        }
        
        if (GUILayout.Button("글로벌 타이머 테스트"))
        {
            SimpleTimerTest();
        }
        
        if (GUILayout.Button("글로벌 타이머(오브젝트 바인딩) 테스트 (단일)"))
        {
            SingleObjectTimerTest();
        }

        if (GUILayout.Button("글로벌 타이머(오브젝트 바인딩) 테스트 (멀티)"))
        {
            MultiObjectTimerTest();
        }

        if (GUILayout.Button("글로벌 타이머 예약 테스트"))
        {
            TimeReserveTest();
        }

        if (GUILayout.Button("Dispose 테스트"))
        {
            DisposeTest();
        }

        if (GUILayout.Button("Push Once"))
        {
            PushOnce();
        }

        if (GUILayout.Button("CustomTimerTask 테스트"))
        {
            TestCustomTimerTask();
        }

        if (GUILayout.Button("IntervalTask 테스트"))
        {
            TestIntervalTask();
        }

        if (GUILayout.Button("Global Timer Task List"))
        {
            LogTaskList();
        }
    }

    private void SimpleTimerTest()
    {
        Stopwatch sw = new Stopwatch();
        Debugs.LogFormat("Start Timer Test - {0}", Time.realtimeSinceStartup);
            
        for (int i = 0; i < 1000; i++)
        {
            int index = i;
            int randSec = UnityEngine.Random.Range(0, 100);
            Debugs.LogWarningFormat("I am {0}, reserve Timer - {1}", index, randSec.ToString());
            sw.Start();
            GlobalDispatchTimer.Instance.PushTimerJob(() =>
            {
                Debugs.LogErrorFormat("PPI BBIB - I am {0}, take {1} seconds", index, randSec);
            }, randSec);
            sw.Stop();
        }
            
        Debugs.LogFormat("End Timer Test - {0}", Time.realtimeSinceStartup);
        UnityEngine.Debug.Log("Time : " + sw.ElapsedMilliseconds + "ms");
    }
    
    private void SingleObjectTimerTest()
    {
        Debugs.Log("SingleObjectTimerTest 시작");
        Queue<GameObject> gameObjQueue = new Queue<GameObject>();
        
        while (gameObjQueue.Count > 0)
        {
            var go = gameObjQueue.Dequeue();
            Destroy(go);
        }

        for (int i = 0; i < 1000; i++)
        {
            var newGo = new GameObject(string.Concat("SingleMainDispatchTimer", i));
            gameObjQueue.Enqueue(newGo);
        }
            
        Stopwatch sw = new Stopwatch();
        Debugs.LogFormat("Start GO Timer Test - {0}", Time.realtimeSinceStartup);

        for (int i = 0; i < 1000; i++)
        {
            int index = i;
            int randSec = UnityEngine.Random.Range(0, 100);
            var go = gameObjQueue.Dequeue();
            Debugs.LogWarningFormat("I am {0}, reserve GO Timer - {1}", index, randSec.ToString());
            sw.Start();
            GlobalDispatchTimer.Instance.PushTimerJob(() =>
            {
                Debugs.LogErrorFormat("GO PPI BBIB - I am {0}, take {1} seconds", index, randSec);
            }, randSec, go);
            sw.Stop();
                
            Destroy(go);
        }
            
        Debugs.LogFormat("End GO Timer Test - {0}", Time.realtimeSinceStartup);
        Debugs.Log("Time : " + sw.ElapsedMilliseconds + "ms");
        
        Debugs.Log("---------------------------------------------------------------------");
    }

    private void MultiObjectTimerTest()
    {
        Debugs.Log("MultiObjectBindTimerTest 시작");
        List<GameObject> gameObjList = new List<GameObject>();

        for (int i = 0; i < gameObjList.Count; i++)
        {
            Destroy(gameObjList[i]);
        }

        gameObjList.Clear();
        
        for (int i = 0; i < 10; i++)
        {
            var newGo = new GameObject(string.Concat("MultiMainDispatchTimer", i));
            gameObjList.Add(newGo);
        }
            
        Stopwatch sw = new Stopwatch();
        Debugs.LogFormat("Start GO Timer Test - {0}", Time.realtimeSinceStartup);

        for (int i = 0; i < 1000; i++)
        {
            int index = i;
            int randSec = UnityEngine.Random.Range(0, 100);
            int randIdx = UnityEngine.Random.Range(0, 10);
            var go = gameObjList[randIdx];
            Debugs.LogWarningFormat("I am {0}, reserve GO Timer - {1} Attatched To Object {2}", index, randSec.ToString(), randIdx);
            sw.Start();
            GlobalDispatchTimer.Instance.PushTimerJob(() =>
            {
                Debugs.LogErrorFormat("GO PPI BBIB - I am {0}, take {1} seconds", index, randSec);
            }, randSec, go);
            sw.Stop();
                
            Destroy(go);
        }

        /*
        GlobalDispatchTimer.Instance.PushTimerJob(() =>
        {
            for (int i = 0; i < 10; i++)
                Destroy(gameObjList[i]);
        }, 10);
        */
            
        Debugs.LogFormat("End GO Timer Test - {0}", Time.realtimeSinceStartup);
        Debugs.Log("Time : " + sw.ElapsedMilliseconds + "ms");
        
        Debugs.Log("---------------------------------------------------------------------");
    }

    private void TimeReserveTest()
    {
        Debugs.Log("TimeReserveTest 시작");

        List<GameObject> gameObjList = new List<GameObject>();
        for (int i = 0; i < gameObjList.Count; i++)
        {
            Destroy(gameObjList[i]);
        }

        gameObjList.Clear();
        
        for (int i = 0; i < 10; i++)
        {
            var newGo = new GameObject(string.Concat("MultiMainDispatchTimer", i));
            gameObjList.Add(newGo);
        }
            
        Stopwatch sw = new Stopwatch();
        Debugs.LogFormat("Start Reserve Timer Test - {0}", Time.realtimeSinceStartup);

        for (int i = 0; i < 1000; i++)
        {
            int index = i;
            int randSec = UnityEngine.Random.Range(0, 10);
            DateTime reserveTime = DateTime.Now.AddSeconds(randSec);
            Debugs.LogWarningFormat("I am {0}, reserve GO Timer - Reserve Time: {1}", index, randSec.ToString(), reserveTime);
            sw.Start();
            GlobalDispatchTimer.Instance.PushTimerJob(() =>
            {
                Debugs.LogErrorFormat("GO PPI BBIB - I am {0}, Difference: {1}ms", index, 
                    (DateTime.Now.Ticks - reserveTime.Ticks) / 10000L);
            }, reserveTime);
            sw.Stop();
        }
        
        Debugs.Log("Time : " + sw.ElapsedMilliseconds + "ms");
        
        //Debugs.Log("---------------------------------------------------------------------");
        
        Debugs.Log("---------------------------------------------------------------------");
    }

    private void DisposeTest()
    {
        Stopwatch sw = new Stopwatch();
        Debugs.LogFormat("Start Timer Test - {0}", Time.realtimeSinceStartup);
            
        for (int i = 0; i < 1000; i++)
        {
            int index = i;
            int randSec = UnityEngine.Random.Range(0, 100);
            Debugs.LogWarningFormat("I am {0}, reserve Timer - {1}", index, randSec.ToString());
            sw.Start();
            var disposal = GlobalDispatchTimer.Instance.PushTimerJob(() =>
            {
                Debugs.LogErrorFormat("PPI BBIB - I am {0}, take {1} seconds", index, randSec);
            }, randSec);
            disposal.DisposeNullChk();
            sw.Stop();
        }
            
        Debugs.LogFormat("End Timer Test - {0}", Time.realtimeSinceStartup);
        UnityEngine.Debug.Log("Time : " + sw.ElapsedMilliseconds + "ms");
    }

    private void PushOnce()
    {
        Debugs.LogFormat("Reserve At {0}", DateTime.Now);
        GlobalDispatchTimer.Instance.PushTimerJob(() => { Debugs.LogErrorFormat("Calleed At {0}", DateTime.Now); }, 10);
    }

    private void TestCustomTimerTask()
    {
        for (int i = 0; i < 10; i++)
        {
            CustomTimerTaskObject customTimerTask = new CustomTimerTaskObject();
            GlobalDispatchTimer.Instance.PushTimerJob(customTimerTask);

            /*
            if (UnityEngine.Random.Range(0, 2) % 2 == 0)
            {
                Debugs.LogFormat("Timer#{0} Disposed Now!!", i);
                customTimerTask.Dispose();
            }
            */
        }
    }

    private void TestIntervalTask()
    {
        /*
        IntervalTask intervalTask = new IntervalTask(() =>
        {
            Debugs.LogErrorFormat("IntervalTask 호출됨!!! - {0}", DateTime.Now);
        }, 5f, 5f, startImmediatly: true);

        GlobalDispatchTimer.Instance.PushTimerJob(intervalTask);
        Debugs.LogError("IntervalTask Push됨");
        */
    }

    private void TestFrameTask()
    {
        
    }

    private void LogTaskList()
    {
        var log = new StringBuilder();
        
        var taskList = GlobalDispatchTimer.Instance.GetTaskList();

        foreach (var task in taskList)
        {
            TimerJob timerJob = task.Value;
            var executor = timerJob.GetExecutor();

            log.AppendFormat("task: {0}, executedTick: {1}, is_disposed: {2}\n",
                string.Concat(executor.Target, " - ", executor.Method),
                timerJob.ExecutedTicks, timerJob.IsDisposed());
        }

        log.Insert(0, string.Format("currentTick: {0}\n", Time.realtimeSinceStartup));
        
        Debugs.Log(log.ToString());
    }
}
#endif

