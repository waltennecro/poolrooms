using System;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    // One bounded set of physical doors and blinds per chunk, with interaction
    // state keyed by world address. No second sun, probe or per-window light.
    public static class PoolroomsCourtyard188
    {
        public static void Attach(PoolroomsWorld world,Room room,Transform parent)
        {
            var plan=room.Plan?.Quiet?.Courtyard;if(plan==null||plan.Court)return;
            var owner=parent.GetComponent<PoolroomsChunk>();if(owner==null)return;
            string prefix="courtyard188:"+world.CurrentLayout.Config.Seed+":"+room.X+":"+room.Z+":";
            var actor=world.viewer!=null?world.viewer.GetComponentInParent<PoolroomsExplorer>():null;
            var inventory=actor!=null?PoolroomsInventory.For(actor):null;
            int slot=0,windows=0;GameObject doorDisplay=null,blindDisplay=null;
            foreach(var opening in CourtyardDistrictArchitecture.Openings(room,world.CurrentLayout.Config))
            {
                int id=slot++;
                if(!opening.Door&&windows++%4!=0)continue;
                var root=new GameObject(opening.Door?"Hotel room door":"Adjustable window blind");root.transform.SetParent(parent,false);
                root.transform.localPosition=new Vector3(opening.Center.X,opening.Center.Y,opening.Center.Z);
                root.transform.localRotation=Quaternion.Euler(0,opening.Yaw,0);
                var hinge=new GameObject("Hinge");hinge.transform.SetParent(root.transform,false);
                var mesh=new MeshData{Theme=SurfaceTheme.CourtyardStone};
                if(opening.Door)
                {
                    hinge.transform.localPosition=new Vector3(-opening.Width*.5f,0,0);
                    mesh.Box(new V3(.025f,.025f,-.035f),new V3(opening.Width-.025f,opening.Height-.025f,.035f),Surface.Accent,.7f);
                    // Recessed panel borders and an actual handle on both sides.
                    foreach(float z in new[]{-.043f,.043f})
                    {
                        mesh.Box(new V3(.14f,.25f,z-.009f),new V3(opening.Width-.14f,opening.Height-.32f,z+.009f),Surface.Slide,.65f);
                        mesh.Box(new V3(opening.Width-.22f,1.03f,z-.045f),new V3(opening.Width-.08f,1.065f,z+.045f),Surface.Metal,.3f);
                    }
                }
                else
                {
                    hinge.transform.localPosition=Vector3.up*opening.Height;
                    // Slats gather vertically below their fixed headrail; the glass and its safety
                    // collider stay fixed, so opening a blind cannot cause a fall.
                    int slats=17;for(int i=0;i<slats;i++)
                    {
                        float y=-(i+.5f)*opening.Height/slats;
                        mesh.Box(new V3(-opening.Width*.5f+.06f,y-.035f,-.12f),new V3(opening.Width*.5f-.06f,y+.035f,-.09f),Surface.Slide,.5f);
                    }
                }
                var prototype=opening.Door?doorDisplay:blindDisplay;
                if(prototype!=null)UnityEngine.Object.Instantiate(prototype,hinge.transform,false);
                else
                {
                    var display=MeshFactory.Create(opening.Door?"Panel and handles":"Blind slats",mesh,hinge.transform,world.profile,owner,false);
                    if(opening.Door)doorDisplay=display;else blindDisplay=display;
                }
                // A moving box collider follows the visible door or gathered blind.
                var collider=hinge.AddComponent<BoxCollider>();
                collider.center=opening.Door?new Vector3(opening.Width*.5f,opening.Height*.5f,0):new Vector3(0,-opening.Height*.5f,-.11f);
                collider.size=new Vector3(opening.Width-.05f,opening.Height-.05f,.09f);
                var interaction=hinge.AddComponent<PoolroomsInteractable>();interaction.world=world;interaction.activity=RoomActivity.Window;
                interaction.key=prefix+id;interaction.title=opening.Door?"Habitación "+opening.Number:"Persiana del patio";
                interaction.movingPart=hinge.transform;interaction.closedRotation=Quaternion.identity;interaction.sideHinge=opening.Door;interaction.rollBlind=!opening.Door;
                if(inventory!=null)interaction.Initialize(inventory);
            }
        }
    }
}
