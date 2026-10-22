using System;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    public sealed partial class PoolroomsAvatar
    {
        public bool HasModel {get{return modelRoot!=null&&renderers.Length>0;}}
        // Rig adaptation is a transaction on a separate, inactive hierarchy. A
        // malformed anatomy never destroys the normalized static source or old skin.
        private GameObject PrepareDisplay(GameObject source,bool adapt,float height,out string report)
        {
            report="Modelo estático · visible sin huesos";
            if(!adapt)return source;
            GameObject draft=null;
            try
            {
                draft=Instantiate(source,transform,false);draft.name=source.name+" / adaptación";draft.SetActive(false);
                var rig=draft.AddComponent<PoolroomsHumanoidSkin>();rig.Build(Preferences,height);
                report=rig.Report;source.SetActive(false);Destroy(source);return draft;
            }
            catch(Exception error)
            {
                if(draft!=null){draft.SetActive(false);Destroy(draft);}
                report="Cargado como modelo estático. Adaptación no aplicada: "+error.GetBaseException().Message;
                return source;
            }
        }
        public void PreviewLoadedSkin()
        {
            if(!HasModel){Status="Primero importa un modelo. El resultado aparece junto al botón Importar.";return;}
            if(explorer==null)explorer=GetComponent<PoolroomsExplorer>();
            if(settings==null)settings=GetComponent<PoolroomsGameSettings>();
            if(settings!=null){settings.Preferences.perspective=CameraPerspective.NearThirdPerson;settings.Change(SettingsChange.Camera);}
            explorer.Views?.SetPerspective(CameraPerspective.NearThirdPerson);
            if(!Status.Contains("tercera persona"))Status+=" · Tercera persona activada; cierra el menú para ver el modelo.";
            SetVisible(true,false);
        }
    }
}
