using System.Collections;
using TMPro;
using UnityEngine;

namespace UrbanLegendBureau.UI
{
    /// <summary>
    /// 잠깐 떴다 사라지는 알림 한 줄.
    ///
    /// 무엇이 열렸다는 것처럼, 읽고 나면 그만인 소식에 쓴다.
    /// 누를 것이 없으므로 아래 화면을 가리지도 막지도 않는다. 시간이 지나면 스스로 닫는다.
    /// </summary>
    public class ToastScreen : UIScreen
    {
        [SerializeField] private TMP_Text _text;

        [Tooltip("떠 있는 시간(초).")]
        [SerializeField] private float _seconds = 2.2f;

        private Coroutine _timer;

        /// <summary>
        /// 한 줄을 띄운다. 이미 떠 있으면 새 줄로 갈아 끼우고 시간을 다시 센다.
        /// 문구를 고르는 것은 부르는 쪽의 일이다. 여기서는 받은 글을 그대로 보여준다.
        /// </summary>
        public void Show(string line)
        {
            if (_text != null) _text.text = line;

            UiFade.Show(gameObject);

            if (_timer != null) StopCoroutine(_timer);
            if (isActiveAndEnabled) _timer = StartCoroutine(HideLater());
        }

        private IEnumerator HideLater()
        {
            // 잠깐 두었다가 옅어지며 사라진다. 갑자기 없어지면 사라진 것을 보지 못한다.
            yield return UiFade.Play(gameObject, null, _seconds, UiFade.Fade);

            _timer = null;
            Closed?.Invoke(this);
        }

        /// <summary>시간이 다 됐다. 화면 스택을 아는 쪽이 닫는다.</summary>
        public System.Action<ToastScreen> Closed;

        protected override void OnClose()
        {
            if (_timer != null)
            {
                StopCoroutine(_timer);
                _timer = null;
            }
        }
    }
}
