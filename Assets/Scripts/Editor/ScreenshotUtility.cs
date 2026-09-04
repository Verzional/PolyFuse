using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace PolyFuse.Editor
{
    public static class ScreenshotUtility
    {
        private static string GetScreenshotsDirectory()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string screenshotDir = Path.Combine(projectRoot, "Screenshots");
            if (!Directory.Exists(screenshotDir))
            {
                Directory.CreateDirectory(screenshotDir);
            }
            return screenshotDir;
        }

        private static string GenerateScreenshotPath(int superSize)
        {
            string dir = GetScreenshotsDirectory();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = superSize > 1 
                ? $"PolyFuse_{timestamp}_{superSize}x.png" 
                : $"PolyFuse_{timestamp}.png";
            return Path.Combine(dir, filename);
        }

        [MenuItem("Tools/PolyFuse/Capture Screenshot (1x Standard) _F12", false, 100)]
        public static void CaptureScreenshot1x()
        {
            Capture(1);
        }

        [MenuItem("Tools/PolyFuse/Capture Screenshot (2x Retina - 4K) %#k", false, 101)]
        public static void CaptureScreenshot2x()
        {
            Capture(2);
        }

        [MenuItem("Tools/PolyFuse/Capture Screenshot (4x Ultra HD Marketing)", false, 102)]
        public static void CaptureScreenshot4x()
        {
            Capture(4);
        }

        [MenuItem("Tools/PolyFuse/Open Screenshots Folder", false, 120)]
        public static void OpenFolder()
        {
            string dir = GetScreenshotsDirectory();
            EditorUtility.RevealInFinder(dir);
        }

        private static void Capture(int superSize)
        {
            string path = GenerateScreenshotPath(superSize);
            ScreenCapture.CaptureScreenshot(path, superSize);
            Debug.Log($"<color=#00E5FF><b>[PolyFuse]</b></color> High-res screenshot ({superSize}x) captured: <color=#F59E0B>{path}</color>");

            EditorApplication.delayCall += () =>
            {
                if (File.Exists(path))
                {
                    EditorUtility.RevealInFinder(path);
                }
            };
        }
    }
}
