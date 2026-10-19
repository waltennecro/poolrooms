using UnityEngine;

namespace Poolrooms
{
    // Temporary transition feedback; no HUD is kept during normal exploration.
    [DisallowMultipleComponent,RequireComponent(typeof(PoolroomsExplorer))]
    public sealed class PoolroomsTravelLoading : MonoBehaviour
    {
        PoolroomsExplorer explorer;GUIStyle style;
        void Awake(){explorer=GetComponent<PoolroomsExplorer>();}
        void OnGUI()
        {
            if(explorer==null||explorer.world==null||explorer.IsPaused||explorer.InputCaptured||explorer.IsReady||explorer.world.ViewerReady)return;
            if(style==null)style=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,fontSize=18,wordWrap=true};
            int depth=GUI.depth;Color color=GUI.color;GUI.depth=-900;
            GUI.color=new Color(.035f,.055f,.06f,.96f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
            GUI.color=Color.white;GUI.Label(new Rect(Screen.width*.15f,Screen.height*.4f,Screen.width*.7f,130),"Cargando zona…\n"+explorer.world.StreamingStatus+".\nEsc abre el menú.",style);
            GUI.depth=depth;GUI.color=color;
        }
    }
}
