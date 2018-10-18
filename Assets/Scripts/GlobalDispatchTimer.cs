// 이미 지난 시점의 작업을 허용 하지 않습니다.
#define __DISALLOW_PAST_TASK_ENQUEUE

// 로그를 사용합니다.
//#define __USE_TIMER_LOG

using System;
using System.Collections;
using System.Collections.Generic;
using Global.Timer.Utils;
using UnityEngine;

namespace Global.Timer
{
	/// <summary>
	/// Global Dispatch TImer의 상태
	/// </summary>
	public enum GDT_STATUS
	{
		UNINITIALIZED,	// 초기화 되지 않은 상태
		RUNNING,		// 실행중
		STOP,			// 멈춤
	}
	
	/// <summary>
	/// Unity API에 Safe한 글로벌 타이머 디스패처 입니다. 시스템 전역적으로 time에 의한 sync가 맞습니다.
	/// 다른 스레드에서 돌리고 싶었지만 어떻게 쓰일지 몰라 Unity API에 안전하게 메인 스레드에서 돌리도록 했습니다.
	/// 따라서, 이 타이머에 너무 오래 blocking 되는 작업을 dispatch하면 안됩니다.
	/// 등록된 작업이 실행되는 시점은(유니티 이벤트 실행 순서에서) LateUpdate입니다.
	/// 이 디스패처에서 흐르는 시간은 realtimeSinceStartUp을 기준으로 두고 있습니다.
	/// 마땅한 엔트리 포인트를 찾지 못하여 우선은, GameSettingManager가 Init되는 시점에 생성되도록 하였습니다.
	/// </summary>
	public sealed class GlobalDispatchTimer : MonoBehaviour
	{
		private static bool isInited = false;
		private static GlobalDispatchTimer instance;
		public static GlobalDispatchTimer Instance
		{
			get
			{
				if (instance == null || !isInited)
				{
					Debugs.Log("DispatchTimer 인스턴스가 존재 하지 않아 새로 생성합니다.");
					Init();
				}

				return instance;
			}
		}

		private static void Init()
		{
			if (!isInited)
			{
				if (!Application.isPlaying)
					return;

				GlobalDispatchTimer mainDispatcher = null;

				try
				{
					mainDispatcher = GameObject.FindObjectOfType<GlobalDispatchTimer>();
				}
				catch (Exception e)
				{
					Debugs.LogError("Failed to create DispatchTimer Instance.");
				}

				// 메인 디스패처가 없으면 만들어 준다.
				if (mainDispatcher == null)
				{
					new GameObject("GlobalDispatchTimer").AddComponent<GlobalDispatchTimer>();
				}
				else
				{
					// 있어도 강제로 Awake를 실행 시켜서 초기화 되도록 해준다.
					instance.Awake();
				}
			}
			else
			{
				Debugs.Log("DispatchTimer가 이미 초기화 되었습니다.");
			}
		}
		
		/// <summary>
		/// 디스패처의 현재 틱이다. (초)
		/// </summary>
		private float CurrentTicks
		{
			get { return Time.realtimeSinceStartup; }
		}

		private bool isCalledDestroyed;

		private bool isRunning;

		/// <summary>
		/// 스케줄링 되어야 하는 TimerJob 들을 관리하는 우선순위 큐 입니다.
		/// </summary>
		private PriorityQueue<TimerJob> timedJobQueue;

		/// <summary>
		/// 모니터링 할 Destroy Trigger 의 리스트 입니다.
		/// </summary>
		//private List<DestroyTrigger> triggerList;
		private LinkedList<DestroyTrigger> triggerList;
		
		/// <summary>
		/// Destroy Trigger 고유키, TimerJob의 WeakReference List
		/// </summary>
		private Dictionary<long, List<WeakReference>> triggerMap;

		private void Awake()
		{
			// 어찌되었든 Awake는 탄다.
			
			isCalledDestroyed = false;
			instance = this;
			isInited = true;
			
			timedJobQueue = new PriorityQueue<TimerJob>();
			triggerList = new LinkedList<DestroyTrigger>();
			triggerMap = new Dictionary<long, List<WeakReference>>();
			
			DontDestroyOnLoad(gameObject);
			
			isRunning = true;
		}

		/// <summary>
		/// delay 뒤의 시간을 Global Dispatch Timer가 사용하는 시간으로 정규화 합니다.
		/// </summary>
		/// <param name="delay"></param>
		/// <returns></returns>
		public static float NormalizeTime(float delay)
		{
			return Time.realtimeSinceStartup + delay;
		}

		/// <summary>
		/// 예약할 시간을 Global Dispatch Timer가 사용하는 시간으로 정규화 합니다.
		/// </summary>
		/// <param name="reserveTime"></param>
		/// <returns></returns>
		public static float NormalizeTime(DateTime reserveTime)
		{
			var interval = reserveTime - DateTime.Now;
			return Time.realtimeSinceStartup + (float)interval.TotalSeconds;
		}

		/// <summary>
		/// 남은 시간 간격 후의 시간을 Global Dispatch Timer가 사요하는 시간으로 정규화 합니다.
		/// </summary>
		/// <param name="timeSpan"></param>
		/// <returns></returns>
		public static float NormalizeTime(TimeSpan timeSpan)
		{
			return Time.realtimeSinceStartup + (float)timeSpan.TotalSeconds;
		}
		
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////
		// Interface

		/// <summary>
		/// Global Queue에 즉시 실행되도록 작업을 예약합니다.
		/// </summary>
		/// <param name="timerTask">실행할 작업</param>
		public void PushJob(Action timerTask)
		{
			timedJobQueue.Enqueue(new TimerJob(timerTask, CurrentTicks));
		}
		
		
		/// <summary>
		/// Global Queue에 delay초 뒤에 실행되도록 작업을 예약합니다.
		/// </summary>
		/// <param name="timerTask">실행할 작업</param>
		/// <param name="delay">실행까지 남은 초(float)</param>
		/// <returns>생성된 TimerJob의 할당 해제 가능한 IDisposable 객체를 반환합니다.</returns>
		public IDisposable PushTimerJob(Action timerTask, float delay)
		{
			if (!ValidateTime(delay))
				return null;

			var timerJob = new TimerJob(timerTask, CurrentTicks + delay);
			timedJobQueue.Enqueue(timerJob);
			return timerJob;
		}
		
		/// <summary>
		/// Global Queue에 delay만큼 뒤에 실행되도록 작업을 예약합니다.
		/// </summary>
		/// <param name="timerTask">실행할 작업</param>
		/// <param name="delay">실행까지 남은 시간(TimeSpan)</param>
		/// <returns>생성된 TimerJob의 할당 해제 가능한 IDisposable 객체를 반환합니다.</returns>
		public IDisposable PushTimerJob(Action timerTask, TimeSpan delay)
		{
			return PushTimerJob(timerTask, (float)delay.TotalSeconds);
		}
		
		/// <summary>
		/// Global Queue에 특정 시간에 실행되도록 작업을 예약합니다.
		/// </summary>
		/// <param name="timerTask">실행할 작업</param>
		/// <param name="excuteTime">실행될 시간</param>
		/// <returns>생성된 TimerJob의 할당 해제 가능한 IDisposable 객체를 반환합니다.</returns>
		public IDisposable PushTimerJob(Action timerTask, DateTime excuteTime)
		{
			if (!ValidateTime(excuteTime))
				return null;
			
			TimeSpan interval = excuteTime - DateTime.Now;
			return PushTimerJob(timerTask, (float)interval.TotalSeconds);
		}

		/// <summary>
		/// Global Queue에 delay초 뒤에 실행되도록 작업을 예약합니다.
		/// GameObject인 bindTarget이 파괴되면 작업도 취소됩니다.
		/// </summary>
		/// <param name="timerTask">실행할 작업</param>
		/// <param name="delay">실행까지 남은 초(float)</param>
		/// <param name="bindTarget">라이프 사이클을 함께할 GameObject</param>
		/// <returns>생성된 TimerJob의 할당 해제 가능한 IDisposable 객체를 반환합니다.</returns>
		public IDisposable PushTimerJob(Action timerTask, float delay, GameObject bindTarget)
		{
			if (!ValidateTime(delay))
				return null;
			
			if ((object)bindTarget == null)
			{
				Debugs.LogError("bindTarget이 null입니다.");
				return null;
			}

			var destroyTrigger = bindTarget.GetComponent<DestroyTrigger>();
			if (destroyTrigger == null)
			{
				#if __USE_TIMER_LOG
				Debugs.Log("Create New DestroyTrigger");
				#endif
				destroyTrigger = bindTarget.AddComponent<DestroyTrigger>();
			}
			
			#if __USE_TIMER_LOG
			Debugs.Log("Attatch DestroyTrigger to gameObject");
			#endif

			var timerJob = new TimerJob(timerTask, CurrentTicks + delay);

			if (!RegisterTrigger(destroyTrigger, bindTarget, timerJob))
				return null;
			
			timedJobQueue.Enqueue(timerJob);
			
			return timerJob;
		}
		
		/// <summary>
		/// Global Queue에 delay만큼 뒤에 실행되도록 작업을 예약합니다.
		/// GameObject인 bindTarget이 파괴되면 작업도 취소됩니다.
		/// </summary>
		/// <param name="timerTask">실행한 작업</param>
		/// <param name="delay">실행까지 남은 시간(TimeSpan)</param>
		/// <param name="bindTarget">라이프 사이클을 함께할 GameObject</param>
		/// <returns>생성된 TimerJob의 할당 해제 가능한 IDisposable 객체를 반환합니다.</returns>
		public IDisposable PushTimerJob(Action timerTask, TimeSpan delay, GameObject bindTarget)
		{
			return PushTimerJob(timerTask, (float)delay.TotalSeconds, bindTarget);
		}

		/// <summary>
		/// Global Queue에 특정 시간에 실행되도록 작업을 예약합니다.
		/// GameObject인 bindTarget이 파괴되면 작업도 취소됩니다.
		/// </summary>
		/// <param name="timerTask">실행한 작업</param>
		/// <param name="excuteTime">실행될 시간</param>
		/// <param name="bindTarget">라이프 사이클을 함께할 GameObject</param>
		/// <returns>생성된 TimerJob의 할당 해제 가능한 IDisposable 객체를 반환합니다.</returns>
		public IDisposable PushTimerJob(Action timerTask, DateTime excuteTime, GameObject bindTarget)
		{
			if (!ValidateTime(excuteTime))
				return null;
				
			TimeSpan interval = excuteTime - DateTime.Now;
			return PushTimerJob(timerTask, (float)interval.TotalSeconds, bindTarget);
		}
		
		/// <summary>
		/// TimerJob을 Global Dispatch Timer에 등록합니다.
		/// 이 메서드는 동작 시간의 유효성을 따로 검사해주지 않습니다.
		/// </summary>
		/// <param name="job">실행할 TimerJob 객체</param>
		public void PushTimerJob(TimerJob job)
		{
			if (job == null)
			{
				Debugs.LogError("Job이 null입니다.");
				return;
			}
			
			timedJobQueue.Enqueue(job);
		}

		/// <summary>
		/// TimerJob을 Global Dispatch Timer에 등록합니다.
		/// GameObject인 bindTarget이 파괴되면 작업도 취소됩니다.
		/// 이 메서드는 동작 시간의 유효성을 따로 검사해주지 않습니다.
		/// </summary>
		/// <param name="job">실행할 TimerJob 객체</param>
		/// <param name="bindTarget">라이프 사이클을 함께할 GameObject</param>
		public void PushTimerJob(TimerJob job, GameObject bindTarget)
		{
			if (job == null)
			{
				Debugs.LogError("Job이 null입니다.");
				return;
			}
			
			if (bindTarget == null)
			{
				Debugs.LogError("bindTarget이 null입니다.");
				return;
			}

			var destroyTrigger = bindTarget.GetComponent<DestroyTrigger>();
			if (destroyTrigger == null)
			{
				#if __USE_TIMER_LOG
				Debugs.Log("Create New DestroyTrigger");
				#endif
				destroyTrigger = bindTarget.AddComponent<DestroyTrigger>();
			}
			
			#if __USE_TIMER_LOG
			Debugs.Log("Attatch DestroyTrigger to gameObject");
			#endif

			if (RegisterTrigger(destroyTrigger, bindTarget, job))
				timedJobQueue.Enqueue(job);
		}
		
		
		// ////////////////////////////////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// 남은 시간이 양수인지 체크
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		private bool ValidateTime(DateTime time)
		{
#if __DISALLOW_PAST_TASK_ENQUEUE
            var condition = time.CompareTo(DateTime.Now) > 0;
			if (!condition)
				Debugs.LogWarning("현재 시간보다 전의 시간으로 등록할 수 없습니다.");
			return condition;
#else
				return true;
#endif
		}

		private bool ValidateTime(float delay)
		{
#if __DISALLOW_PAST_TASK_ENQUEUE
            if (delay < 0)
            {
                Debugs.LogWarning("현재 시간보다 전의 시간으로 등록할 수 없습니다.");
                return false;
            }
            return true;
#else 
			return true;
#endif
		}

		private bool RegisterTrigger(DestroyTrigger trigger, GameObject bindTarget, TimerJob timerJob)
		{
			if (bindTarget == null)
			{
				Debugs.LogError("bindTarget이 이미 null입니다.");
				return false;
			}

			if (timerJob == null)
			{
				Debugs.LogError("timerJob이 이미 null입니다.");
				return false;
			}

			if (triggerMap.ContainsKey(trigger.Key))
			{
				triggerMap[trigger.Key].Add(new WeakReference(timerJob));
			}
			else
			{
				triggerList.AddLast(trigger);
				triggerMap.SafetyAdd(trigger.Key, new List<WeakReference>());
				triggerMap[trigger.Key].Add(new WeakReference(timerJob));
			}

			return true;
		}

		/// <summary>
		/// TimerJob과 라이프 사이클을 같이 하는 GameObject를 순회하면서 체크합니다.
		/// </summary>
		private void CheckDestroyedGameObject()
		{
			var markedList = new List<long>();
			
			if (triggerList.Count > 0)
			{
				markedList.Clear();

				var ptr = triggerList.First;
				while (ptr != null)
				{
					var current = ptr;
					var trigger = current.Value;

					if (!trigger.IsActivated)
					{
#if __USE_TIMER_LOG
						Debugs.Log("Bind된 gameobject Destroy! TimerJob을 비활성화 합니다.");
#endif
						markedList.Add(trigger.Key);
						
						//Linked List 이므로 O(1)...
						triggerList.Remove(current);
					}
					
					ptr = ptr.Next;
				}

				foreach (var triggerKey in markedList)
				{
					// priorityQueue를 사용하여 TimerJob 큐를 스케줄링 하므로 TimerJob에 마크만 해두고 
					// 해당 Job의 tick이 되어 실행될때 비활성화 시켜주는게 낫다.
					List<WeakReference> triggeredJobWeakRefList;
					if (triggerMap.TryGetValue(triggerKey, out triggeredJobWeakRefList))
					{
						foreach (WeakReference triggeredJobRef in triggeredJobWeakRefList)
						{
							TimerJob triggeredJob = triggeredJobRef.Target as TimerJob;
							if (triggeredJob != null)
							{
								triggeredJob.MarkDisposed();
							}
						}
						
						triggerMap.Remove(triggerKey);
					}
				}
			}
		}
		
		/*
		/// <summary>
		/// TimerJob의 라이프 사이클을 관리하는 trigger을 순회하면서 체크합니다.
		/// </summary>
		/// <returns></returns>
		private IEnumerator ObserveTrigger()
		{
			//List<DestroyTrigger> markedList = new List<DestroyTrigger>();
			List<long> markedList = new List<long>();
			
			while (true)
			{
				markedList.Clear();
				
				foreach (var trigger in triggerList)
				{
					if (!trigger.IsActivated)
					{
						#if __USE_TIMER_LOG
						Debugs.Log("Bind된 gameobject Destroy! TimerJob을 비활성화 합니다.");
						#endif
						//markedList.Add(trigger);
						markedList.Add(trigger.Key);
					}
				}

				triggerList.RemoveAll(trigger => trigger == null);

				foreach (var trigger in markedList)
				{
					// priorityQueue를 사용하여 TimerJob 큐를 스케줄링 하므로 TimerJob에 마크만 해두고 
					// 해당 Job의 tick이 되어 실행될때 비활성화 시켜주는게 낫다.
					List<WeakReference> triggeredJobWeakRefList;
					if (triggerMap.TryGetValue(trigger, out triggeredJobWeakRefList))
					{
						foreach (WeakReference triggeredJobRef in triggeredJobWeakRefList)
						{
							TimerJob triggeredJob = triggeredJobRef.Target as TimerJob;
							if (triggeredJob != null)
							{
								triggeredJob.MarkDisposed();
							}
						}
						
						triggerMap.Remove(trigger);
					}
				}
				
				yield return null;
			}
		}
		*/

		/// <summary>
		/// dispatch Timer를 멈춥니다.
		/// </summary>
		public void Stop()
		{
			isRunning = false;
		}

		/// <summary>
		/// dispatch Timer를 재개합니다.
		/// </summary>
		public void Resume()
		{
			isRunning = true;
		}

		/// <summary>
		/// dispatch Timer에 예약 되어 있는 작업들을 모두 클리어합니다.
		/// </summary>
		public void Reset()
		{
			timedJobQueue.Clear();
			triggerList.Clear();
			triggerMap.Clear();
		}

		/// <summary>
		/// dispatch Timer에 예약 되어 있는 작업들의 수를 반환합니다.
		/// </summary>
		/// <returns></returns>
		public long Count()
		{
			return timedJobQueue.Count;
		}

		/// <summary>
		/// GlobalD Dispatch Timer의 현재 상태를 반환합니다.
		/// </summary>
		/// <returns></returns>
		public GDT_STATUS Status()
		{
			if (!isInited)
				return GDT_STATUS.UNINITIALIZED;
			return isRunning ? GDT_STATUS.RUNNING : GDT_STATUS.STOP;
		}
		
#if UNITY_EDITOR
		/// <summary>
		/// 현재 task list를 반환합니다.
		/// </summary>
		/// <returns></returns>
		public List<PriorityQueue<TimerJob>.IndexedItem> GetTaskList()
		{
			return timedJobQueue.indexedItemList;
		}
#endif

		private void LateUpdate()
		{
			if (isCalledDestroyed)
				return;

			if (!isRunning)
				return;
			
			CheckDestroyedGameObject();
				
			while (timedJobQueue.Count > 0)
			{
				var timedJob = timedJobQueue.Peek();

				if (CurrentTicks < timedJob.ExecutedTicks)
					break;

				try
				{
					#if __USE_TIMER_LOG
					Debugs.LogFormat("Execute Timer Job !!! - currentTicks {0}, job_excutedTicks {1}", CurrentTicks,
						timedJob.ExecutedTicks);
					#endif
					timedJob.Execute();
				}
				catch (Exception ex)
				{
					Debugs.LogError(string.Concat(ex.Message, ex.StackTrace));
				}
				finally
				{
					// 어쩄든 큐에서 뺴자
					timedJobQueue.Dequeue();
				}
			}
		}

		private void OnDestroy()
		{
			Reset();

			isCalledDestroyed = true;

			instance = null;
			isInited = false;
			isRunning = false;
		}
	}
}
