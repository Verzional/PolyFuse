#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using PolyFuse.Grid;
using PolyFuse.Gameplay;
using PolyFuse.Interaction;
using PolyFuse.Juice;
using PolyFuse.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace PolyFuse.Editor
{
    public static class PolyFuseSceneSetup
    {
        [MenuItem("PolyFuse/Setup PolyFuse Scene", false, 1)]
        public static void SetupScene()
        {
            // 1. Camera
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }
            cam.transform.position = new Vector3(0f, -0.6f, -10f);
            cam.orthographic = true;
            cam.orthographicSize = 7.0f;
            cam.backgroundColor = new Color(0.06f, 0.07f, 0.10f, 1.0f);

            if (cam.GetComponent<Physics2DRaycaster>() == null)
            {
                cam.gameObject.AddComponent<Physics2DRaycaster>();
            }

            // 2. EventSystem
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<InputSystemUIInputModule>();
#else
                esObj.AddComponent<StandaloneInputModule>();
#endif
                Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
            }

            // 3. Board
            HexBoard board = Object.FindFirstObjectByType<HexBoard>();
            if (board == null)
            {
                GameObject boardObj = new GameObject("HexBoard");
                board = boardObj.AddComponent<HexBoard>();
                Undo.RegisterCreatedObjectUndo(boardObj, "Create HexBoard");
            }
            board.GenerateBoard();

            // 4. Tray Manager
            HandTrayManager tray = Object.FindFirstObjectByType<HandTrayManager>();
            if (tray == null)
            {
                GameObject trayObj = new GameObject("HandTrayManager");
                tray = trayObj.AddComponent<HandTrayManager>();
                Undo.RegisterCreatedObjectUndo(trayObj, "Create HandTrayManager");
            }

            // 5. Game Manager & Subsystems
            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            if (gm == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                gm = gmObj.AddComponent<GameManager>();
                gmObj.AddComponent<GreedEngine>();
                gmObj.AddComponent<PieceSpawner>();
                gmObj.AddComponent<JuiceController>();
                gmObj.AddComponent<ProceduralAudio>();
                Undo.RegisterCreatedObjectUndo(gmObj, "Create GameManager");
            }

            // 6. UI
            GameUI ui = Object.FindFirstObjectByType<GameUI>();
            if (ui == null)
            {
                GameObject uiObj = new GameObject("GameUI");
                ui = uiObj.AddComponent<GameUI>();
                Undo.RegisterCreatedObjectUndo(uiObj, "Create GameUI");
            }

            EditorUtility.SetDirty(board);
            Debug.Log("[PolyFuse] PolyFuse Scene setup completed successfully!");
        }
    }
}
#endif
