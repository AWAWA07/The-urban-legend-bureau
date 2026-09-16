using UnityEngine;

namespace UrbanLegendBureau.Systems
{
    /// <summary>
    /// 현장 한 곳이 도중에 다른 자리로 바뀌는 것을 다룬다.
    ///
    /// 막차 사건이 그렇다. 처음에는 승강장에 서 있다가, 승강장을 조사하면 열차에 올라탄다.
    /// 탄 뒤로는 승강장이 보이지 않고 열차 안이 보여야 한다.
    ///
    /// 장소를 새로 만들지 않고, 같은 현장 안에서 보이는 것만 갈아 끼운다.
    /// 조사 지점은 그대로 두므로 사건 진행과 단서 조건은 건드리지 않는다.
    /// </summary>
    public class FieldSceneSwap : MonoBehaviour
    {
        [Tooltip("타기 전에만 보이는 것들. 승강장이 여기에 들어간다.")]
        [SerializeField] private GameObject[] _beforeRoots;

        [Tooltip("타고 난 뒤에만 보이는 것들. 열차 안이 여기에 들어간다.")]
        [SerializeField] private GameObject[] _afterRoots;

        /// <summary>지금 열차 안인가.</summary>
        public bool IsAfter { get; private set; }

        private void Awake()
        {
            Apply(false);
        }

        private void OnEnable()
        {
            // 현장을 다시 열어도 마지막 상태를 그대로 보여준다.
            Apply(IsAfter);
        }

        /// <summary>열차에 탄 상태로 바꾼다. 되돌릴 수도 있다.</summary>
        public void SetAfter(bool after)
        {
            IsAfter = after;
            Apply(after);
        }

        private void Apply(bool after)
        {
            SetAll(_beforeRoots, !after);
            SetAll(_afterRoots, after);
        }

        private static void SetAll(GameObject[] roots, bool on)
        {
            if (roots == null) return;

            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null) roots[i].SetActive(on);
            }
        }
    }
}
