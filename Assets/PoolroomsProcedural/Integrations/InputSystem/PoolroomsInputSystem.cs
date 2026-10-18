#if POOLROOMS_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine;
using UnityEngine.InputSystem;
namespace Poolrooms
{
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(PoolroomsExplorer))]
    public sealed class PoolroomsInputSystem : MonoBehaviour
    {
        private PoolroomsExplorer explorer;
        private Keyboard layoutKeyboard;
        private string layoutName;
        private UnityEngine.InputSystem.Controls.KeyControl runKey;
        private void OnEnable(){explorer=GetComponent<PoolroomsExplorer>();explorer.externalInput=true;}
        private void OnDisable()
        {
            if(explorer==null)return;
            explorer.FeedInput(Vector2.zero,Vector2.zero,false,false,false,false);
            explorer.FeedUtilityInput(false,false,false,false,false,false);
            explorer.FeedCarryInput(false,false);explorer.FeedSlideInput(false);explorer.FeedRenderingShortcut(false);
            explorer.FeedDashInput(false);explorer.FeedInspectInput(false);
            explorer.FeedViewInput(false,false,false,false,false,0,0,0,false);
            explorer.externalInput=false;
        }
        private void Update()
        {
            var k=Keyboard.current;var m=Mouse.current;
            if(k!=layoutKeyboard||k!=null&&k.keyboardLayout!=layoutName)
            {layoutKeyboard=k;layoutName=k!=null?k.keyboardLayout:null;runKey=k!=null?(k.FindKeyOnCurrentKeyboardLayout("|")??k.backslashKey):null;}
            Vector2 axis=k==null?Vector2.zero:new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
            explorer.FeedInput(axis,m!=null?m.delta.ReadValue():Vector2.zero,k!=null&&k.spaceKey.wasPressedThisFrame,
                runKey!=null&&runKey.isPressed,k!=null&&k.escapeKey.wasPressedThisFrame,m!=null&&m.leftButton.wasPressedThisFrame);
            explorer.FeedDashInput(k!=null&&k.leftShiftKey.wasPressedThisFrame);
            explorer.FeedUtilityInput(k!=null&&k.yKey.wasPressedThisFrame,k!=null&&k.mKey.wasPressedThisFrame,
                k!=null&&k.fKey.wasPressedThisFrame,k!=null&&k.vKey.wasPressedThisFrame,k!=null&&k.spaceKey.isPressed,k!=null&&k.leftCtrlKey.isPressed);
            explorer.FeedCarryInput(k!=null&&k.eKey.wasPressedThisFrame,k!=null&&k.qKey.wasPressedThisFrame);
            explorer.FeedSlideInput(k!=null&&k.leftCtrlKey.isPressed);explorer.FeedInspectInput(k!=null&&k.rKey.isPressed);
            explorer.FeedRenderingShortcut(k!=null&&k.f7Key.wasPressedThisFrame);
            explorer.FeedViewInput(k!=null&&k.f5Key.wasPressedThisFrame,k!=null&&k.f6Key.wasPressedThisFrame,k!=null&&k.f9Key.wasPressedThisFrame,k!=null&&k.f8Key.wasPressedThisFrame,
                m!=null&&m.rightButton.isPressed,k==null?0:(k.eKey.isPressed?1:0)-(k.qKey.isPressed?1:0),k==null?0:(k.cKey.isPressed?1:0)-(k.zKey.isPressed?1:0),m!=null?m.scroll.ReadValue().y/120:0,k!=null&&k.leftShiftKey.isPressed);
        }
    }
}
#endif
