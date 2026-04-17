using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using nickeltin.Core.Runtime;
using UnityEngine;
using UnityEngine.Pool;


namespace nickeltin.DebugPanel.Runtime
{
    public class DebugPanelLogger : MonoBehaviour
    {
        [Flags]
        internal enum LogTypeFilter
        {
            None = 0,
            Log = 1,
            Warning = 1 << 1,
            Error = 1 << 2,
            Assert = 1 << 3,
            Exception = 1 << 4,
            All = Log | Warning | Error | Assert | Exception
        }
        
        public struct Log
        {
            public readonly string Condition;
            public readonly string StackTrace;
            public readonly LogType LogType;
            public readonly string FullRawMessage;
            public readonly string FullMessage;
            
            private Log(string condition, string stackTrace, LogType logType, string fullRawMessage,  string fullMessage)
            {
                Condition = condition;
                StackTrace = stackTrace;
                LogType = logType;
                FullRawMessage = fullRawMessage;
                FullMessage = fullMessage;
            }

            public static Log Create(string condition, string stackTrace, LogType type, bool appendStackTrace)
            {
                GenericPool<StringBuilder>.Get(out var sb);

                sb.Clear();
                
                sb.AppendLine($"\"{condition}\"");
                if (appendStackTrace) sb.Append(stackTrace);
                
                var fullRawMessage = sb.ToString();

                sb.Insert(0, $"[{DateTime.Now:T} {GetLogTypeShortName(type)}] ");
                sb.Insert(0, $"<color={GetColorKeyword(type)}>");
                sb.Append("</color>");
                
                var fullMessage = sb.ToString();
                
                GenericPool<StringBuilder>.Release(sb);
                
                return new Log(condition, stackTrace, type, fullRawMessage, fullMessage);
            }
        }

        [SerializeField] internal LogTypeFilter _captureLogsFromCategory = LogTypeFilter.All;
        [SerializeField] internal int _maxLogsCount = 100;
        [SerializeField] internal bool _logStacktrace = true;
        
        internal int CurrentLogsCount => _logs.Count; 
        
        // private readonly StringBuilder _stringBuilder = new StringBuilder();
        private readonly List<Log> _logs = new();

        public event Action onLogUpdate;
        
        public IList LogsSource => _logs;

        public IReadOnlyList<Log> Logs => _logs;
        

        private void OnEnable()
        {
            Application.logMessageReceived += LogMessage;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= LogMessage;
        }

        private void LogMessage(string condition, string stacktrace, LogType type)
        {
            if (!_captureLogsFromCategory.HasFlag(UnityLogTypeToLogFilterFlag(type)))
            {
                return;
            }
            
            var log = Log.Create(condition, stacktrace, type, _logStacktrace);
            
            if (_logs.Count >= _maxLogsCount)
            {
                _logs.ShiftLeft(1);
                // var oldLog =  _logs[^1];
                // oldLog.Dispose();
                _logs[^1] = log;
            }
            else
            {
                _logs.Add(log);
            }
            
            onLogUpdate?.Invoke();
        }

        public void Clear()
        {
            // foreach (var log in _logs) log.Dispose();
            _logs.Clear();
            onLogUpdate?.Invoke();
        }
        
        private static LogTypeFilter UnityLogTypeToLogFilterFlag(LogType unityLogType)
        {
            switch (unityLogType)
            {
                case LogType.Error: return LogTypeFilter.Error;
                case LogType.Assert: return LogTypeFilter.Assert;
                case LogType.Warning: return LogTypeFilter.Warning;
                case LogType.Log: return LogTypeFilter.Log;
                case LogType.Exception: return LogTypeFilter.Exception;
                default: return LogTypeFilter.None;
            }
        }
        
        private static string GetColorKeyword(LogType type)
        {
            switch (type)
            {
                case LogType.Warning:
                    return "yellow";
                case LogType.Log: 
                    return "white";
                case LogType.Assert:
                case LogType.Error:
                case LogType.Exception:
                    return "red";
                default:
                    return "magenta";
            }
        }

        private static string GetLogTypeShortName(LogType type)
        {
            switch (type)
            {
                case LogType.Exception:
                case LogType.Error: return "E";
                case LogType.Assert: return "A";
                case LogType.Warning: return "W";
                case LogType.Log: return "I";
                default: return "";
            }
        }
    }
}