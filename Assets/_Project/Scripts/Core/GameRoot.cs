using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UrbanLegendBureau.InputSystemLayer;
using UrbanLegendBureau.Data;
using UrbanLegendBureau.Localization;
using UrbanLegendBureau.Save;
using UrbanLegendBureau.Systems;
using UrbanLegendBureau.UI;

namespace UrbanLegendBureau.Core
{
    /// <summary>
    /// 게임 전체의 진입점. 00_Boot 씬에 단 하나만 존재한다.
    /// 모든 코어 서비스를 만들고, 등록하고, 정해진 순서로 초기화한다.
    ///
    /// 여기서만 서비스를 생성한다. 다른 곳에서 new 하지 않는다.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        [Header("참조")]
        [Tooltip("Assets/InputSystem_Actions.inputactions")]
        [SerializeField] private InputActionAsset _inputActions;

        [Tooltip("Assets/_Project/Data/Config/GameDataCatalog.asset")]
        [SerializeField] private GameDataCatalogSO _gameDataCatalog;

        [Header("부팅 진단")]
        [Tooltip("켜면 부팅 시 플랫폼 정보와 Localization 동작 확인 로그를 출력한다.")]
        [SerializeField] private bool _verboseBootLog = true;

        // 부팅 진단에서 '없는 ID' 방어 동작을 확인할 때 쓰는 더미 키.
        private const string MissingIdProbe = "ui.does.not.exist";

        private readonly List<ITickable> _tickables = new List<ITickable>();

        public bool IsBooted { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[GameRoot] 이미 존재한다. 중복 인스턴스를 제거한다.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Boot();
        }

        private void Boot()
        {
            if (IsBooted) return;

            ScreenSetup.Apply();

            // --- 코어 서비스 등록 (등록 순서 = 초기화 순서) ---
            var localization = new LocalizationService();
            ServiceRegistry.Register(localization);

            var input = new InputService(_inputActions);
            ServiceRegistry.Register(input);

            var save = new SaveService();
            ServiceRegistry.Register(save);

            var ui = new UIService();
            ServiceRegistry.Register(ui);

            var legends = new LegendService(_gameDataCatalog);
            ServiceRegistry.Register(legends);

            var rules = new RuleService(_gameDataCatalog);
            ServiceRegistry.Register(rules);

            var internet = new InternetService(_gameDataCatalog);
            ServiceRegistry.Register(internet);

            var spread = new SpreadService(_gameDataCatalog);
            ServiceRegistry.Register(spread);

            var belief = new BeliefService();
            ServiceRegistry.Register(belief);

            var exorcism = new ExorcismService(_gameDataCatalog);
            ServiceRegistry.Register(exorcism);

            // --- 초기화 ---
            InitializeService(localization);
            InitializeService(input);
            InitializeService(save);
            InitializeService(ui);
            InitializeService(legends);
            InitializeService(rules);
            InitializeService(internet);
            InitializeService(spread);
            InitializeService(belief);
            InitializeService(exorcism);

            IsBooted = true;

            if (_verboseBootLog)
            {
                LogBootDiagnostics(localization);
            }

            EventBus.Publish(new GameBootCompletedEvent());
        }

        private void InitializeService<T>(T service) where T : class, IService
        {
            service.Initialize();

            if (service is ITickable tickable && !_tickables.Contains(tickable))
            {
                _tickables.Add(tickable);
            }
        }

        private void Update()
        {
            if (!IsBooted) return;

            float deltaTime = Time.unscaledDeltaTime;
            for (int i = 0; i < _tickables.Count; i++)
            {
                _tickables[i].Tick(deltaTime);
            }
        }

        private void LogBootDiagnostics(LocalizationService localization)
        {
            Debug.Log($"[GameRoot] 부팅 완료 | 플랫폼: {PlatformInfo.Describe()}");
            Debug.Log(
                "[GameRoot] Localization 확인\n" +
                $"  언어: {localization.CurrentLanguage} / 항목 {localization.EntryCount}개\n" +
                $"  ui.title.game_name  -> {localization.Get("ui.title.game_name")}\n" +
                $"  ui.common.ok        -> {localization.Get("ui.common.ok")}\n" +
                $"  ui.format.case_no   -> {localization.Get("ui.format.case_no", 1)}\n" +
                $"  dlg.hanyeong.test.001 -> {localization.Get("dlg.hanyeong.test.001")}\n" +
                $"  누락 ID 방어 동작    -> Has(ui.does.not.exist) = {localization.Has(MissingIdProbe)}");
        }

        /// <summary>
        /// 모바일에서 앱이 백그라운드로 갈 때, WebGL에서 탭이 가려질 때 호출된다.
        /// 이 시점을 놓치면 프로세스가 그대로 종료돼 저장이 사라질 수 있다.
        /// </summary>
        private void OnApplicationPause(bool paused)
        {
            if (!paused || !IsBooted) return;
            FlushPendingSave();
        }

        private void OnApplicationQuit()
        {
            FlushPendingSave();
        }

        private void FlushPendingSave()
        {
            if (ServiceRegistry.TryGet<SaveService>(out var save) && save.IsDirty)
            {
                save.AutoSave();
            }
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            // 등록 역순으로 종료한다.
            if (ServiceRegistry.TryGet<ExorcismService>(out var exorcism))
            {
                exorcism.Shutdown();
            }

            if (ServiceRegistry.TryGet<BeliefService>(out var belief))
            {
                belief.Shutdown();
            }

            if (ServiceRegistry.TryGet<SpreadService>(out var spread))
            {
                spread.Shutdown();
            }

            if (ServiceRegistry.TryGet<InternetService>(out var internet))
            {
                internet.Shutdown();
            }

            if (ServiceRegistry.TryGet<RuleService>(out var rules))
            {
                rules.Shutdown();
            }

            if (ServiceRegistry.TryGet<LegendService>(out var legends))
            {
                legends.Shutdown();
            }

            if (ServiceRegistry.TryGet<UIService>(out var ui))
            {
                ui.Shutdown();
            }

            if (ServiceRegistry.TryGet<SaveService>(out var save))
            {
                save.Shutdown();
            }

            if (ServiceRegistry.TryGet<InputService>(out var input))
            {
                input.Shutdown();
            }

            if (ServiceRegistry.TryGet<LocalizationService>(out var localization))
            {
                localization.Shutdown();
            }

            _tickables.Clear();
            ServiceRegistry.Clear();
            EventBus.Clear();
            Instance = null;
            IsBooted = false;
        }
    }
}
