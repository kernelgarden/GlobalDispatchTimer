//#define NOLOG

using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Debug = UnityEngine.Debug;

namespace Global.Timer.Utils
{
    public static class Debugs
    {
        [Conditional("DEBUG")]
        public static void Log(params object[] message)
        {
#if NOLOG
            return;
#endif
            if (Debug.isDebugBuild)
            {
                StringBuilder sb = new StringBuilder(message.Length);
                for (int i = 0; i < message.Length; i++)
                {
                    sb.Append(message[i]);
                }

                Debug.Log(sb.ToString());
#if !UNITY_EDITOR
                if (MDNManager.Instance != null)
                {
                    MDNManager.Instance.ShowXcodeLog(sb.ToString());
                }
#endif
            }
        }

        [Conditional("DEBUG")]
        public static void LogFormat(string format, params object[] message)
        {
#if NOLOG
            return;
#endif
            if (Debug.isDebugBuild)
            {
                Debug.LogFormat(format, message);
#if !UNITY_EDITOR
                if (MDNManager.Instance != null)
                {
                    MDNManager.Instance.ShowXcodeLog(string.Format(format,message));
                }
#endif
            }

        }
        
        [Conditional("DEBUG")]
        public static void LogError(params object[] message)
        {
#if NOLOG
            return;
#endif
            if (Debug.isDebugBuild)
            {
                StringBuilder sb = new StringBuilder(message.Length);
                for (int i = 0; i < message.Length; i++)
                {
                    sb.Append(message[i]);
                }

                Debug.LogError(sb.ToString());
#if !UNITY_EDITOR
                if (MDNManager.Instance != null)
                {
                    MDNManager.Instance.ShowXcodeLog(sb.ToString());
                }
#endif
            }
        }

        [Conditional("DEBUG")]
        public static void LogErrorFormat(string format, params object[] message)
        {
#if NOLOG
            return;
#endif
            if (Debug.isDebugBuild)
            {
                Debug.LogErrorFormat(format, message);
#if !UNITY_EDITOR
                if (MDNManager.Instance != null)
                {
                    MDNManager.Instance.ShowXcodeLog(string.Format(format,message));
                }
#endif
            }

        }

        [Conditional("DEBUG")]
        public static void LogWarning(params object[] message)
        {
#if NOLOG
            return;
#endif
            if (Debug.isDebugBuild)
            {
                StringBuilder sb = new StringBuilder(message.Length);
                for (int i = 0; i < message.Length; i++)
                {
                    sb.Append(message[i]);
                }

                Debug.LogWarning(sb.ToString());
#if !UNITY_EDITOR
                if (MDNManager.Instance != null)
                {
                    MDNManager.Instance.ShowXcodeLog(sb.ToString());
                }
#endif
            }
        }

        [Conditional("DEBUG")]
        public static void LogWarningFormat(string format, params object[] message)
        {
#if NOLOG
            return;
#endif
            if (Debug.isDebugBuild)
            {
                Debug.LogWarningFormat(format, message);
#if !UNITY_EDITOR
                if (MDNManager.Instance != null)
                {
                    MDNManager.Instance.ShowXcodeLog(string.Format(format,message));
                }
#endif
            }

        }

        [Conditional("DEBUG")]
        public static void LogException(Exception exception)
        {
#if NOLOG
            return;
#endif
            if (Debug.isDebugBuild)
            {
                Debug.LogException(exception);
#if !UNITY_EDITOR
                if (MDNManager.Instance != null)
                {
                    MDNManager.Instance.ShowXcodeLog(exception.ToString());
                }
#endif
            }
        }

        /// <summary>
        /// 오브젝트 데이터를 Json문자열 형식으로 보여준다.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="indentLevel">깊이</param>
        [Conditional("DEBUG")]
        public static void LogObject<T>(T obj, int indentLevel = 4)
        {
#if NOLOG
            return;
#endif
            /*
            var json = JsonUtil.ToJson(obj, indentLevel);
            if (Debug.isDebugBuild)
            {
                Debug.Log(json);
            }*/
        }

        public static class LTLog
        {
            private static StringBuilder sb = new StringBuilder();
            [Conditional("DEBUG")]
            public static void AddDebug(object obj)
            {
#if NOLOG
            return;
#endif
                if (Debug.isDebugBuild)
                {
                    if (sb.Length != 0)
                    {
                        sb.AppendLine();
                    }

                    foreach (FieldInfo info in obj.GetType().GetFields())
                    {
                        sb.Append("[").Append(info.Name).Append("] = ").Append(info.GetValue(obj)).Append(", ");
                    }
                }
            }
            [Conditional("DEBUG")]
            public static void AddDebug(string str, object obj)
            {
#if NOLOG
            return;
#endif
                if (Debug.isDebugBuild)
                {
                    if (sb.Length != 0)
                    {
                        sb.AppendLine();
                    }

                    sb.Append("[").Append(str).Append("] = ").Append(obj).Append(", ");
                }
            }
            [Conditional("DEBUG")]
            public static void ShowLog()
            {
#if NOLOG
            return;
#endif
                if (Debug.isDebugBuild)
                {
                    if (sb.Length > 0)
                    {
                        Log(sb.ToString());
                        sb.Length = 0;
                    }
                }
            }
        }
    }
}