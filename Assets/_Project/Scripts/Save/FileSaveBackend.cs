using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace UrbanLegendBureau.Save
{
    /// <summary>
    /// Windows / Android 용. Application.persistentDataPath 아래에 파일로 저장한다.
    /// 쓰기는 임시 파일 -> 교체 방식이라 쓰는 도중 종료돼도 기존 저장이 깨지지 않는다.
    /// </summary>
    public class FileSaveBackend : ISaveBackend
    {
        private const string Extension = ".json";
        private const string TempExtension = ".tmp";
        private const string FolderName = "Saves";

        // BOM 없는 UTF-8. BOM이 붙으면 외부 도구나 다른 파서가 첫 글자에서 걸린다.
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        private readonly string _directory;

        public FileSaveBackend()
        {
            _directory = Path.Combine(Application.persistentDataPath, FolderName);
        }

        public string Name => "File";

        public string DescribeLocation(string key) => PathFor(key);

        public bool Exists(string key)
        {
            try
            {
                return File.Exists(PathFor(key));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileSaveBackend] Exists 실패: {ex.Message}");
                return false;
            }
        }

        public string Read(string key)
        {
            var path = PathFor(key);
            try
            {
                if (!File.Exists(path)) return null;
                return File.ReadAllText(path, Utf8NoBom);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileSaveBackend] 읽기 실패 ({path}): {ex.Message}");
                return null;
            }
        }

        public bool Write(string key, string json)
        {
            var path = PathFor(key);
            var tempPath = path + TempExtension;

            try
            {
                EnsureDirectory();

                File.WriteAllText(tempPath, json, Utf8NoBom);

                // 원자적 교체. File.Replace는 대상이 없으면 실패하므로 분기한다.
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                File.Move(tempPath, path);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileSaveBackend] 쓰기 실패 ({path}): {ex.Message}");
                TryDeleteTemp(tempPath);
                return false;
            }
        }

        public void Flush()
        {
            // 파일 시스템은 WriteAllText 시점에 이미 반영된다. 할 일 없음.
        }

        public bool Delete(string key)
        {
            var path = PathFor(key);
            try
            {
                if (!File.Exists(path)) return false;
                File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileSaveBackend] 삭제 실패 ({path}): {ex.Message}");
                return false;
            }
        }

        private string PathFor(string key) => Path.Combine(_directory, key + Extension);

        private void EnsureDirectory()
        {
            if (!Directory.Exists(_directory))
            {
                Directory.CreateDirectory(_directory);
            }
        }

        private static void TryDeleteTemp(string tempPath)
        {
            try
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }
            catch
            {
                // 임시 파일 정리 실패는 무시한다.
            }
        }
    }
}
