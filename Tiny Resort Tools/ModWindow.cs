using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

namespace TR {

    public class ModWindow : MonoBehaviour {

        public static GameObject MapCanvas;

        private static GameObject BlankOptionsMenu;

        public Image background;

        public static void Initialize() {

            return;
            
            Debug.Log("INITIALIZING MOD WINDOW: " + SceneManager.GetActiveScene().name);

            // Creates a canvas object identical to the Dinkum MapCanvas object
            MapCanvas = new GameObject();
            MapCanvas.AddComponent<Canvas>();
            MapCanvas.AddComponent<GraphicRaycaster>();
            var scal = MapCanvas.AddComponent<CanvasScaler>();
            scal.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scal.referenceResolution = new Vector2(1200, 800);
            scal.matchWidthOrHeight = 0.4f;

            // Finds the Dinkum OptionsWindow and uses it to get all the parts of an options menu
            var WindowAnimators = Resources.FindObjectsOfTypeAll<WindowAnimator>().ToList();
            Debug.Log("ANIMATORS COUNT: " + WindowAnimators.Count);
            foreach (var Anim in WindowAnimators) {
                if (Anim.gameObject.name == "OptionWindow" && !Anim.gameObject.GetComponent<VerticalLayoutGroup>()) {
                    BlankOptionsMenu = Instantiate(Anim.gameObject, MapCanvas.transform);
                    Debug.Log("FOUND OPTION WINDOW");
                    break;
                }
            }
            
        }

        public static ModWindow CreateOptionsMenu(Vector2 topLeftPosition, Vector2 size) {
            var GO = new GameObject("Mod Window");
            GO.AddComponent<ModWindow>();
            var window = new ModWindow();
            
            return window;
        }

        public void Show() {
            
        }

        public void Hide() {
            
        }

        public static ModButton CreateButton() {
            return new ModButton();
        }

    }

    public class ModButton {
        
    }

}
