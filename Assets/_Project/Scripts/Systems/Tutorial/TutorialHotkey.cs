using UnityEngine;
using UnityEngine.InputSystem;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 튜토리얼을 처음부터 다시 돌리는 단축키.
    ///
    /// 만드는 동안 같은 흐름을 몇 번이고 다시 봐야 한다.
    /// 그때마다 타이틀로 나갔다 들어오는 것이 번거로워 어디서든 바로 되돌리게 해 둔다.
    ///
    /// 내놓는 빌드에서는 동작하지 않는다. 플랫폼 분기(#if) 대신 실행 중에 판단한다.
    /// 그 분기는 PlatformInfo 한 곳에만 두기로 했기 때문이다.
    /// </summary>
    public class TutorialHotkey : MonoBehaviour
    {
        [SerializeField] private TutorialDirector _tutorial;

        [Tooltip("누르면 튜토리얼이 처음부터 다시 도는 키.")]
        [SerializeField] private Key _key = Key.F5;

        private void Update()
        {
            // 에디터와 개발용 빌드에서만 듣는다.
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            if (_tutorial == null) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard[_key].wasPressedThisFrame) _tutorial.RestartTutorial();
        }
    }
}
