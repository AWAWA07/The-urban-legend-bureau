using System;
using System.Collections.Generic;
using UnityEngine;

namespace UrbanLegendBureau.Core
{
    /// <summary>
    /// 타입 기반 전역 이벤트 버스.
    /// 서비스끼리 직접 참조하지 않게 만드는 장치. 새 규칙이 생기면 구독자만 추가하면 된다.
    /// 주의: 구독한 쪽은 반드시 Unsubscribe 할 것. 씬 전환으로 파괴되는 객체는 OnDisable에서 해제.
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> Handlers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler) where T : struct, IGameEvent
        {
            if (handler == null) return;

            var key = typeof(T);
            if (Handlers.TryGetValue(key, out var existing))
            {
                Handlers[key] = Delegate.Combine(existing, handler);
            }
            else
            {
                Handlers[key] = handler;
            }
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct, IGameEvent
        {
            if (handler == null) return;

            var key = typeof(T);
            if (!Handlers.TryGetValue(key, out var existing)) return;

            var remaining = Delegate.Remove(existing, handler);
            if (remaining == null)
            {
                Handlers.Remove(key);
            }
            else
            {
                Handlers[key] = remaining;
            }
        }

        public static void Publish<T>(T evt) where T : struct, IGameEvent
        {
            if (!Handlers.TryGetValue(typeof(T), out var existing)) return;

            // 핸들러 하나가 던진 예외가 나머지 구독자를 막지 않도록 개별 호출한다.
            var list = existing.GetInvocationList();
            for (int i = 0; i < list.Length; i++)
            {
                try
                {
                    ((Action<T>)list[i]).Invoke(evt);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[EventBus] {typeof(T).Name} 처리 중 예외: {ex}");
                }
            }
        }

        public static void Clear()
        {
            Handlers.Clear();
        }
    }
}
