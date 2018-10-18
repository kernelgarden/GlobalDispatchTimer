using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Global.Timer
{
	/// <summary>
	/// Job의 라이프 사이클을 gameObject와 같이 사용할 수 있도록 하기 위해서
	/// DestroyTrigger을 대상이 될 gameObject에 붙여서 사용합니다.
	/// </summary>
	public class DestroyTrigger : MonoBehaviour
	{
		private static long _issuedKey = long.MinValue;
		
		/// <summary> 트리거 고유 마다 발급된 키 /// </summary>
		public long Key { get; private set; }
		
		/// <summary> 한번 이상 활성화되어 유니티 엔진의 관리를 받고 있는 상태인가 </summary>
		public bool IsManagedByUnity { get; private set; }
		public bool IsActivated { get; private set; }

		private void Awake()
		{
			//Key = Interlocked.Increment(ref _issuedKey);
			Key = ++_issuedKey;
			IsActivated = true;
		}

		private void OnEnable()
		{
			IsManagedByUnity = true;
		}

		private void OnDisable()
		{
			if (!IsManagedByUnity)
				IsActivated = false;
		}

		private void OnDestroy()
		{
			IsActivated = false;
		}
	}
}
