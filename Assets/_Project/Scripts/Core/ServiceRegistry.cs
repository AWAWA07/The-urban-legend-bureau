using System;
using System.Collections.Generic;
using UnityEngine;

namespace UrbanLegendBureau.Core
{
    /// <summary>
    /// 전역 서비스 보관소. 싱글톤을 여기저기 만들지 않기 위한 단일 창구.
    /// 서비스의 생성/초기화 책임은 GameRoot에 있고, 여기는 조회만 담당한다.
    /// </summary>
    public static class ServiceRegistry
    {
        private static readonly Dictionary<Type, IService> Services = new Dictionary<Type, IService>();

        public static void Register<T>(T service) where T : class, IService
        {
            if (service == null)
            {
                Debug.LogError($"[ServiceRegistry] {typeof(T).Name} 등록 실패: null");
                return;
            }

            var key = typeof(T);
            if (Services.ContainsKey(key))
            {
                Debug.LogWarning($"[ServiceRegistry] {key.Name} 가 이미 등록되어 있어 덮어쓴다.");
            }

            Services[key] = service;
        }

        public static T Get<T>() where T : class, IService
        {
            if (Services.TryGetValue(typeof(T), out var service))
            {
                return service as T;
            }

            Debug.LogError($"[ServiceRegistry] {typeof(T).Name} 가 등록되지 않았다. GameRoot 부팅 순서를 확인할 것.");
            return null;
        }

        public static bool TryGet<T>(out T service) where T : class, IService
        {
            if (Services.TryGetValue(typeof(T), out var found))
            {
                service = found as T;
                return service != null;
            }

            service = null;
            return false;
        }

        public static bool IsRegistered<T>() where T : class, IService
        {
            return Services.ContainsKey(typeof(T));
        }

        public static void Clear()
        {
            Services.Clear();
        }
    }
}
