using System.IO;
using UnityEditor;
using UnityEngine;
using UrbanLegendBureau.Core;
using UrbanLegendBureau.Save;
using UrbanLegendBureau.Systems;

namespace UrbanLegendBureau.EditorTools
{
    /// <summary>
    /// 튜토리얼을 처음부터 다시 보기 위한 개발용 메뉴.
    ///
    /// 튜토리얼을 봤는지는 저장본의 storyFlags 에 남는다.
    /// 그래서 저장 파일을 손으로 지우는 방법밖에 없었는데, 실행 중에 지우면
    /// 메모리에 남아 있던 표시가 자동 저장으로 다시 쓰여 소용이 없었다.
    /// 여기서는 메모리와 파일을 함께 지운다.
    /// </summary>
    public static class TutorialDevMenu
    {
        // 뒤의 %#t 는 Ctrl+Shift+T 를 뜻한다. 에디터 어디서나 듣는다.
        private const string MenuPath = "UrbanLegendBureau/Dev/튜토리얼 처음부터 다시 보기 %#t";

        [MenuItem(MenuPath)]
        private static void ResetTutorial()
        {
            bool clearedMemory = ClearInMemoryFlag();
            bool clearedFile = ClearFileFlag();

            var msg = "[TutorialDevMenu] 튜토리얼 표시 해제" +
                      "\n  실행 중 저장본: " + (clearedMemory ? "지움" : "해당 없음") +
                      "\n  저장 파일: " + (clearedFile ? "지움" : "해당 없음");

            if (Application.isPlaying)
            {
                msg += "\n  지금 실행 중이다. 타이틀의 시작 버튼을 누르면 튜토리얼이 처음부터 돈다.";
            }

            Debug.Log(msg);
        }

        /// <summary>실행 중이라면 메모리에 있는 표시부터 지운다. 자동 저장이 되살리지 못하게 한다.</summary>
        private static bool ClearInMemoryFlag()
        {
            if (!Application.isPlaying) return false;
            if (!ServiceRegistry.TryGet<SaveService>(out var save) || save.Current == null) return false;
            if (!save.Current.storyFlags.Contains(TutorialDirector.TutorialFlag)) return false;

            save.Current.storyFlags.Remove(TutorialDirector.TutorialFlag);
            save.MarkDirty();
            save.Save();
            return true;
        }

        /// <summary>저장 파일에서도 지운다. 파일을 통째로 지우지 않아 다른 진행은 남는다.</summary>
        private static bool ClearFileFlag()
        {
            var path = Path.Combine(Application.persistentDataPath, "Saves",
                SaveService.DefaultSlotKey + ".json");

            if (!File.Exists(path)) return false;

            try
            {
                var json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null || !data.storyFlags.Contains(TutorialDirector.TutorialFlag)) return false;

                data.storyFlags.Remove(TutorialDirector.TutorialFlag);
                File.WriteAllText(path, JsonUtility.ToJson(data, true));
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[TutorialDevMenu] 저장 파일을 고치지 못했다: " + ex.Message);
                return false;
            }
        }
    }
}
