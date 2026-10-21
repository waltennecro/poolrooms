using System;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Poolrooms
{
    public sealed partial class PoolroomsInventory
    {
        private readonly long[] workshopVisits = new long[WorkshopCatalog.Count];
        private readonly long[] workshopFinished = new long[WorkshopCatalog.Count];
        private readonly long[] workshopMoves = new long[WorkshopCatalog.Count];
        private readonly int[] workshopBest = new int[WorkshopCatalog.Count];
        private readonly Queue<string> workshopNotesLeft = new Queue<string>();

        private void RegisterWorkshopVisit(PoolroomsStation station)
        {
            int index = WorkshopCatalog.Index(station.kind);
            if (workshopVisits[index] < long.MaxValue) workshopVisits[index]++;
        }

        private void RegisterWorkshopFinished(PoolroomsStation station, WorkshopBoard board)
        {
            int index = WorkshopCatalog.Index(station.kind);
            bool first = workshopFinished[index] == 0;
            if (workshopFinished[index] < long.MaxValue) workshopFinished[index]++;
            workshopMoves[index] = SaturatingWorkshopSum(workshopMoves[index], board.Moves);
            if (first || board.Moves < workshopBest[index]) workshopBest[index] = board.Moves;
            string message = WorkshopCatalog.Name(station.kind) + " · " + board.Moves + " movimientos";
            if (station.world != null)
            {
                Vector3 position = station.transform.position;
                message += " · cerca de " + Mathf.RoundToInt(position.x) + ", " + Mathf.RoundToInt(position.z);
            }
            workshopNotesLeft.Enqueue(message);
            while (workshopNotesLeft.Count > 16) workshopNotesLeft.Dequeue();
        }

        private static long SaturatingWorkshopSum(long left, long right)
        {
            if (right < 0) return left;
            return left > long.MaxValue - right ? long.MaxValue : left + right;
        }

        private void OpenWorkshopJournal()
        {
            BeginInterface("Cuaderno de talleres");
            var scroll = new ScrollView();
            scroll.style.maxHeight = Mathf.Max(220, Screen.height / panel.scale - 180);
            content.Add(scroll);
            Text("Pequeñas cosas que puedes dejar preparadas", "en-subtitle", scroll);
            Text("Los talleres aparecen en las mismas salas que exploras. Se usan con E y no necesitan modo maestro. Unos cuidan el jardín; otros preparan el próximo encuentro.", "en-reading", scroll);
            long visits = 0, finished = 0;
            int discovered = 0;
            for (int i = 0; i < WorkshopCatalog.Count; i++)
            {
                visits = SaturatingWorkshopSum(visits, workshopVisits[i]);
                finished = SaturatingWorkshopSum(finished, workshopFinished[i]);
                if (workshopVisits[i] > 0) discovered++;
            }
            Text(discovered + " / " + WorkshopCatalog.Count + " tipos visitados · " + visits + " visitas · " + finished + " sesiones terminadas", "en-invitation", scroll);
            Text("No hay que completarlos todos. Las pistas, deshacer y reiniciar están disponibles en cada tablero.", "en-note", scroll);
            for (int i = 0; i < WorkshopCatalog.Count; i++)
            {
                var kind = (StationKind)(WorkshopCatalog.First + i);
                var card = Element("en-workshop-entry", scroll);
                Text(WorkshopCatalog.Name(kind), "en-subtitle", card);
                if (workshopVisits[i] == 0)
                {
                    Text("Aún por encontrar durante el paseo.", "en-note", card);
                }
                else
                {
                    Text("Visitas: " + workshopVisits[i] + " · terminados: " + workshopFinished[i], "en-note", card);
                    if (workshopFinished[i] > 0)
                    {
                        long average = workshopMoves[i] / workshopFinished[i];
                        Text("Menor número de movimientos: " + workshopBest[i] + " · promedio: " + average,
                            "en-note", card);
                    }
                }
                Text(WorkshopCatalog.Instructions(kind), "en-note", card);
            }
            if (workshopNotesLeft.Count > 0)
            {
                Text("Últimos rincones preparados", "en-subtitle", scroll);
                foreach (string entry in workshopNotesLeft) Text(entry, "en-note", scroll);
            }
            Text("Memoria de tableros: " + workshops.Count + " / " + workshops.Capacity +
                " · tableros antiguos liberados: " + workshops.Evictions, "en-note", scroll);
            Text("Reabrir un taller conserva sus piezas mientras permanezca entre los 128 usados más recientemente. Al liberar uno, sus piezas vuelven a la distribución inicial la próxima vez; no se altera el resto del mundo.", "en-note", scroll);
            Text("Todo pertenece a esta partida abierta. Cerrar el juego borra este cuaderno y sus tableros. Las cifras registran sesiones terminadas, no salas nuevas ni modelos distintos.", "en-note", scroll);
            var navigation = Element("en-tabs", content);
            Button("Mis objetos", OpenInventory, navigation);
            Button("Cuaderno de viaje", OpenJournal, navigation, "en-quiet");
            Button("Seguir explorando", CloseInterface, navigation, "en-quiet");
        }
    }
}
