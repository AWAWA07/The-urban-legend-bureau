using UnityEngine;

namespace UrbanLegendBureau.Core
{
    /// <summary>
    /// 짧은 효과음. Resources/Audio/Sfx/{이름} 에 소리 파일을 넣으면 그 이름으로 재생된다.
    /// 아직 파일이 없으면 아무 소리도 나지 않는다. 화면 연출(글자, 흔들림)은 따로 있으므로 소리 없이도 무엇이 일어났는지 안다.
    /// </summary>
    public static class Sfx
    {
        private static AudioSource _source;

        public static void Play(string name, float volume = 1f)
        {
            if (string.IsNullOrEmpty(name)) return;
            var clip = Resources.Load<AudioClip>("Audio/Sfx/" + name);
            if (clip == null) return;
            if (_source == null)
            {
                var go = new GameObject("Sfx");
                Object.DontDestroyOnLoad(go);
                _source = go.AddComponent<AudioSource>();
                _source.playOnAwake = false;
            }
            _source.PlayOneShot(clip, volume);
        }
    }
}
