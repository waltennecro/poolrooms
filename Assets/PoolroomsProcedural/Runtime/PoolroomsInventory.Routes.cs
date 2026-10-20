using System;
using System.Collections;
using System.Threading.Tasks;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Poolrooms
{
    public sealed partial class PoolroomsInventory
    {
        private bool findingRoute;
        private EncounterRoute activeRoute;
        private int routeStep;
        private void AddRoutes(VisualElement parent)
        {
            Text("Caminos para seguir a pie","en-subtitle",parent);
            Text("Puedes pedir una orientación. No mueve al jugador: busca puertas y pasajes que conectan de verdad.","en-note",parent);
            var status=Text("","en-note",parent);var controls=Element("en-tabs",parent);
            Button("Otro ambiente cercano",()=>StartRoute(RouteDestination.DifferentBiome,status),controls);
            Button("Centro comercial",()=>StartRoute(RouteDestination.Mall,status),controls);
            Button("Colinas · nivel 94",()=>StartRoute(RouteDestination.Meadow,status),controls);
            var names=new System.Collections.Generic.List<string>();foreach(LiminalEnvironment environment in Enum.GetValues(typeof(LiminalEnvironment)))names.Add(EnvironmentName(environment));
            var target=new DropdownField(names,0);parent.Add(target);
            Button("Buscar el ambiente elegido",()=>StartRoute(RouteDestination.SpecificBiome,status,(LiminalEnvironment)target.index),parent,"en-quiet");
            if(activeRoute!=null)Text("Orientación activa: "+activeRoute.Label+" · "+Mathf.Max(0,activeRoute.Directions.Length-routeStep)+" pasos entre salas.","en-note",parent);
        }
        private void StartRoute(RouteDestination destination,Label label,LiminalEnvironment selected=LiminalEnvironment.Poolrooms)
        {if(findingRoute){label.text="Ya estoy buscando un camino.";return;}StartCoroutine(FindRoute(destination,label,selected));}
        private IEnumerator FindRoute(RouteDestination destination,Label label,LiminalEnvironment selected)
        {
            var world=explorer.world;var room=world!=null&&world.CurrentLayout!=null?world.RoomAtViewer():null;
            if(room==null||world.Terrain!=null&&world.Terrain.IsInside||world.InsideMall){label.text="Vuelve al acceso conectado para pedir una ruta por las galerías.";yield break;}
            findingRoute=true;label.text="Buscando un camino conectado…";var config=world.CurrentLayout.Config;int x=room.X,z=room.Z;
            var job=Task.Run(()=>EncounterRoutes.Find(config,x,z,destination,selected));while(!job.IsCompleted)yield return null;findingRoute=false;
            if(job.IsFaulted){if(label.panel!=null)label.text="No se pudo preparar el camino: "+job.Exception.GetBaseException().Message;yield break;}
            var route=job.Result;
            if(route==null){if(label.panel!=null)label.text="No encontré un camino cercano dentro del área de búsqueda. Puedes volver a intentarlo tras avanzar.";yield break;}
            if(world!=explorer.world||world.CurrentLayout==null||world.CurrentLayout.Config!=config)yield break;
            activeRoute=route;routeStep=0;string text=route.Directions.Length==0?"Ya estás en ese ambiente.":"Camino encontrado: "+route.Directions.Length+" conexiones. "+RouteHint();
            if(label.panel!=null)label.text=text;Toast(text,6);
        }
        private string RouteHint()
        {string[] names={"Norte","Este","Sur","Oeste"};return activeRoute!=null&&routeStep<activeRoute.Directions.Length?"Próxima salida: "+names[activeRoute.Directions[routeStep]]+".":"Has llegado.";}
        private void UpdateRoute(Room room)
        {
            if(activeRoute==null||routeStep>=activeRoute.Directions.Length)return;
            int x=activeRoute.StartX,z=activeRoute.StartZ;
            for(int i=0;i<routeStep;i++){x+=Directions.DX[activeRoute.Directions[i]];z+=Directions.DZ[activeRoute.Directions[i]];}
            int next=activeRoute.Directions[routeStep];
            if(room.X==x+Directions.DX[next]&&room.Z==z+Directions.DZ[next])
            {routeStep++;Toast(routeStep==activeRoute.Directions.Length?"Has llegado al acceso. Continúa caminando para explorarlo.":RouteHint(),5);}
            else if(room.X!=x||room.Z!=z){activeRoute=null;Toast("Te has desviado del camino. Puedes pedir otra orientación en el cuaderno.");}
        }
    }
}
