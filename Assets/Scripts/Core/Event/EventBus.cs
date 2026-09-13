using System;
using System.Collections.Generic;

namespace MintLandDemo.Core.Event
{
    /// <summary>
    /// 跨系统通信骨架（EventBus）。纯 C# 静态类。
    /// 本阶段只提供 Subscribe / Publish 骨架，不处理 UI 生命周期取消订阅（Part 5 再扩展）。
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _events = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null)
            {
                return;
            }

            if (_events.TryGetValue(typeof(T), out Delegate existing))
            {
                _events[typeof(T)] = Delegate.Combine(existing, handler);
            }
            else
            {
                _events[typeof(T)] = handler;
            }
        }

        public static void Publish<T>(T eventData) where T : struct
        {
            if (_events.TryGetValue(typeof(T), out Delegate existing) && existing is Action<T> action)
            {
                action.Invoke(eventData);
            }
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null)
            {
                return;
            }

            if (_events.TryGetValue(typeof(T), out Delegate existing))
            {
                Delegate result = Delegate.Remove(existing, handler);
                if (result == null)
                {
                    _events.Remove(typeof(T));
                }
                else
                {
                    _events[typeof(T)] = result;
                }
            }
        }
    }
}
