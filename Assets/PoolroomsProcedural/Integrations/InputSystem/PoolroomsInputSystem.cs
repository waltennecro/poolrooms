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
        private void OnEnable(){explorer=GetComponent<PoolroomsExplorer>();explorer.externalInput=true;}
        private void OnDisable()
        {
            if(explorer==null)return;
            explorer.FeedInput(Vector2.zero,Vector2.zero,false,false,false,false);
            explorer.FeedUtilityInput(false,false,false,false,false,false);
            explorer.FeedCarryInput(false,false);explorer.FeedSlideInput(false);explorer.FeedRenderingShortcut(false);
            explorer.externalInput=false;
        }
        private void Update()
        {
            var k=Keyboard.current;var m=Mouse.current;
            Vector2 axis=k==null?Vector2.zero:new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
            explorer.FeedInput(axis,m!=null?m.delta.ReadValue():Vector2.zero,k!=null&&k.spaceKey.wasPressedThisFrame,
                k!=null&&k.leftShiftKey.isPressed,k!=null&&k.escapeKey.wasPressedThisFrame,m!=null&&m.leftButton.wasPressedThisFrame);
            explorer.FeedUtilityInput(k!=null&&k.yKey.wasPressedThisFrame,k!=null&&k.mKey.wasPressedThisFrame,
                k!=null&&k.fKey.wasPressedThisFrame,k!=null&&k.vKey.wasPressedThisFrame,k!=null&&k.spaceKey.isPressed,k!=null&&k.leftCtrlKey.isPressed);
            explorer.FeedCarryInput(k!=null&&k.eKey.wasPressedThisFrame,k!=null&&k.qKey.wasPressedThisFrame);
            explorer.FeedSlideInput(k!=null&&k.leftCtrlKey.isPressed);
            explorer.FeedRenderingShortcut(k!=null&&k.f7Key.wasPressedThisFrame);
        }
    }
}
#endif
