using System;
using System.Collections;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    public sealed class PoolroomsMallPortal : MonoBehaviour
    {
        private PoolroomsMallSystem malls;
        private Room room;
        private bool exit;
        public void Use(){if(malls==null)return;if(exit)malls.Exit();else malls.Enter(room);}
        public static PoolroomsMallPortal Create(PoolroomsMallSystem system,Room room,bool exit,Vector3 position,Transform parent,PoolroomsProfile profile)
        {
            var go=new GameObject(exit?"E · Salir del centro comercial":"E · Entrar al centro comercial 2010");go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var portal=go.AddComponent<PoolroomsMallPortal>();portal.malls=system;portal.room=room;portal.exit=exit;
            var owner=go.AddComponent<PoolroomsChunk>();owner.waterRegions=new WaterRegion[0];
            var geometry=new MeshData{RetailPalette=1,PaletteVariant=3,EmissionTint=new V3(.8f,.92f,1),EmissionStrength=.08f};
            foreach(int side in new[]{-1,1})MallArchitecture.Box(geometry,side*1.15f,0,.15f,.15f,0,2.8f,Surface.Metal);
            MallArchitecture.Box(geometry,0,0,2.45f,.15f,2.8f,3.3f,Surface.Slide);
            string text=exit?"E SALIR":"E CENTRO 2010";MallGlyphs.Write(geometry,text,new V3(-text.Length*.085f,2.95f,-.09f),.028f);
            MeshFactory.Create("Gateway frame",geometry,go.transform,profile,owner,false);
            var trigger=go.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.center=Vector3.up*1.45f;trigger.size=new Vector3(2.4f,2.9f,.2f);
            return portal;
        }
    }
}
