using System;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Poolrooms
{
    public sealed partial class PoolroomsInventory
    {
        private readonly WorkshopSessions workshops = new WorkshopSessions(128);
        private int workshopLease;
        private int workshopPlayback;
        private int selectedReservoir = -1;
        private static readonly string[] workshopColours = { "Arena", "Musgo", "Azul", "Terracota" };
        private static readonly string[] workshopDirections = { "Norte", "Este", "Sur", "Oeste" };
        private static readonly string[] workshopNotes = { "DO", "RE", "MI", "FA", "SOL", "LA", "SI", "DO alto" };

        private int WorkshopWorldSeed(PoolroomsStation station)
        {
            return station.world != null && station.world.profile != null ? station.world.profile.seed : 0;
        }

        private void RestoreWorkshop(PoolroomsStation station)
        {
            if (station == null || !WorkshopCatalog.IsWorkshop(station.kind)) return;
            // Looking at a station never fills the session cache. Only used
            // boards have state; untouched ones are generated deterministically.
            if (workshops.TryPeek(station.key, WorkshopWorldSeed(station), station.kind, out var board))
                PoolroomsWorkshopDisplay.Show(station, board);
        }

        private bool WorkshopAvailable(PoolroomsStation station, int lease)
        {
            return lease == workshopLease && InterfaceOpen && explorer != null && !explorer.IsPaused &&
                explorer.IsReady && station != null && Valid(station);
        }

        private void OpenWorkshop(PoolroomsStation station)
        {
            var board = workshops.Get(station.key, WorkshopWorldSeed(station), station.kind);
            selectedReservoir = -1;
            BeginInterface(WorkshopCatalog.Name(station.kind));
            int lease = workshopLease;
            RegisterWorkshopVisit(station);
            var page = new ScrollView();
            page.style.maxHeight = Mathf.Max(220, Screen.height / panel.scale - 170);
            content.Add(page);
            Text(WorkshopCatalog.Story(station.kind), "en-reading", page);
            Text(WorkshopCatalog.Instructions(station.kind), "en-note", page);
            Text("Sin cronómetro, penalizaciones ni recompensas obligatorias. Puedes dejarlo a medias y seguir explorando.", "en-note", page);
            var status = Text("", "en-invitation", page);
            var reference = Element("en-tabs", page);
            var controls = Element("en-workshop-controls", page);
            var actions = Element("en-tabs", page);
            var hint = Text("", "en-note", page);
            Action redraw = null;
            Action<WorkshopMove> apply = move =>
            {
                if (!WorkshopAvailable(station, lease)) return;
                bool changed = board.Apply(move);
                if (station.kind == StationKind.BellSequence)
                    sound?.PlayInteractiveNote(move.Cell, station.SoundPosition);
                if (!changed)
                {
                    hint.text = RejectedWorkshopMove(station.kind);
                    return;
                }
                if (station.kind != StationKind.BellSequence)
                    SoundStation(station, WorkshopCatalog.Sound(station.kind));
                Animate(HumanoidGesture.Adjust, .65f);
                RecordWorkshopCompletion(station, board);
                hint.text = "";
                redraw();
            };
            redraw = () =>
            {
                if (!WorkshopAvailable(station, lease)) return;
                status.text = board.Status + " · movimientos: " + board.Moves;
                if (board.Complete) status.text += " · terminado";
                reference.Clear();
                controls.Clear();
                DrawWorkshopReference(station.kind, board, reference);
                DrawWorkshopControls(station.kind, board, controls, apply, redraw);
                PoolroomsWorkshopDisplay.Show(station, board);
            };
            Button("Deshacer", () =>
            {
                if (!WorkshopAvailable(station, lease)) return;
                if (!board.Undo()) { hint.text = "Todavía no hay movimientos para deshacer."; return; }
                selectedReservoir = -1;
                hint.text = "Último movimiento deshecho.";
                redraw();
            }, actions, "en-quiet");
            Button("Ver una pista", () =>
            {
                if (!WorkshopAvailable(station, lease)) return;
                hint.text = board.Hint(out var next) ? DescribeWorkshopHint(station.kind, next) : "Todo está preparado. Puedes continuar tu paseo.";
            }, actions, "en-quiet");
            Button("Aplicar una pista", () =>
            {
                if (!WorkshopAvailable(station, lease)) return;
                if (board.Hint(out var next)) apply(next);
            }, actions, "en-quiet");
            Button("Reiniciar este tablero", () =>
            {
                if (!WorkshopAvailable(station, lease)) return;
                board.Restart();
                selectedReservoir = -1;
                hint.text = "Tablero devuelto a su estado inicial.";
                redraw();
            }, actions, "en-quiet");
            if (station.kind == StationKind.BellSequence)
                Button("Escuchar la tarjeta", () => PlayWorkshopPhrase(station, board, lease), actions);
            Text("El estado es de esta sesión: se conservan los 128 talleres usados más recientemente. No sustituye a un guardado en disco.", "en-note", page);
            redraw();
        }

        private void RecordWorkshopCompletion(PoolroomsStation station, WorkshopBoard board)
        {
            if (!board.Complete) return;
            int seed = WorkshopWorldSeed(station);
            string identity = "workshop:" + seed + ":" + (int)station.kind + ":" + station.key;
            if (State(identity)) return;
            SetState(identity, true);
            RegisterWorkshopFinished(station, board);
            Journal.Record(RoomActivity.Game, identity);
            Animate(HumanoidGesture.Place, 1.2f);
            Toast("Taller terminado. Lo dejas preparado para la próxima visita.", 5);
        }

        private static string RejectedWorkshopMove(StationKind kind)
        {
            switch (kind)
            {
                case StationKind.StarChart: return "Busca el siguiente número; el dibujo que ya llevas queda intacto.";
                case StationKind.BellSequence: return "La campana suena libremente. La siguiente nota de la tarjeta sigue esperando.";
                case StationKind.TideLocks: return "Ese traslado no cabe: el origen debe tener agua y el destino espacio libre.";
                case StationKind.SeedNursery: return "El semillero ya alcanzó su límite. Puedes usar el control contrario.";
                default: return "Ese movimiento no cambia el tablero.";
            }
        }

        private static string DescribeWorkshopHint(StationKind kind, WorkshopMove move)
        {
            if (kind == StationKind.TideLocks)
                return "Traslada una medida del depósito " + (move.Cell / 4 + 1) + " al " + (move.Cell % 4 + 1) + ".";
            if (kind == StationKind.BellSequence) return "La siguiente campana es " + workshopNotes[move.Cell] + ".";
            string direction = move.Direction > 0 ? "avanzar" : "retroceder";
            if (kind == StationKind.SeedNursery) direction = move.Direction > 0 ? "regar" : "vaciar";
            return "Usa " + direction + " en el control " + (move.Cell + 1) + ".";
        }

        private void DrawWorkshopReference(StationKind kind, WorkshopBoard board, VisualElement root)
        {
            Text("Tarjeta de referencia", "en-note", root);
            switch (kind)
            {
                case StationKind.LanternCircuit:
                    Text("Meta: los nueve faroles encendidos.", "en-note", root);
                    break;
                case StationKind.WaterChannels:
                    Text("Entrada: oeste de la pieza 1. Salida: oeste de la pieza 13. Conecta las 16 piezas.", "en-note", root);
                    break;
                case StationKind.ParcelBalance:
                    Text("Paquete: " + ((BalanceBoard)board).DesiredWeight + " medidas.", "en-note", root);
                    break;
                case StationKind.Herbarium:
                    for (int i = 0; i < 6; i++) Text((i + 1) + ". " + HerbariumBoard.Species(i), "en-note", root);
                    break;
                case StationKind.StarChart:
                    Text("Une 1 → 2 → 3 → 4 → 5 → 6 → 7 → 8 → 9.", "en-note", root);
                    break;
                case StationKind.BellSequence:
                    for (int i = 0; i < board.CellCount; i++)
                        Text((i + 1) + ": " + workshopNotes[board.Target(i)], "en-invitation", root);
                    break;
                default:
                    for (int i = 0; i < board.CellCount; i++)
                    {
                        string value = WorkshopReferenceValue(kind, board.Target(i));
                        Text((i + 1) + ": " + value, "en-note", root);
                    }
                    break;
            }
        }

        private static string WorkshopReferenceValue(StationKind kind, int value)
        {
            if (kind == StationKind.Weaving || kind == StationKind.Bookbinding) return workshopColours[value];
            if (kind == StationKind.WindRose) return workshopDirections[value];
            if (kind == StationKind.Sundial) return (value == 0 ? 12 : value).ToString();
            return value.ToString();
        }

        private void DrawWorkshopControls(StationKind kind, WorkshopBoard board, VisualElement root,
            Action<WorkshopMove> apply, Action redraw)
        {
            switch (kind)
            {
                case StationKind.LanternCircuit: DrawLanterns(board, root, apply); break;
                case StationKind.WaterChannels: DrawChannels((ChannelBoard)board, root, apply); break;
                case StationKind.Weaving: DrawWeaving(board, root, apply); break;
                case StationKind.Herbarium: DrawHerbarium(board, root, apply); break;
                case StationKind.Sundial: DrawDials(board, root, apply, false); break;
                case StationKind.ParcelBalance: DrawBalance((BalanceBoard)board, root, apply); break;
                case StationKind.StarChart: DrawStars(board, root, apply); break;
                case StationKind.BellSequence: DrawBells((BellBoard)board, root, apply); break;
                case StationKind.TideLocks: DrawTides(board, root, apply, redraw); break;
                case StationKind.Bookbinding: DrawBinding(board, root, apply); break;
                case StationKind.WindRose: DrawDials(board, root, apply, true); break;
                case StationKind.SeedNursery: DrawNursery(board, root, apply); break;
            }
        }

        private void DrawLanterns(WorkshopBoard board, VisualElement root, Action<WorkshopMove> apply)
        {
            for (int row = 0; row < 3; row++)
            {
                var line = Element("en-tabs", root);
                for (int col = 0; col < 3; col++)
                {
                    int cell = row * 3 + col;
                    var button = Button((cell + 1) + (board.Value(cell) == 1 ? " · encendido" : " · apagado"),
                        () => apply(new WorkshopMove(cell)), line);
                    button.style.minWidth = 125;
                }
            }
        }

        private void DrawChannels(ChannelBoard board, VisualElement root, Action<WorkshopMove> apply)
        {
            for (int row = 0; row < 4; row++)
            {
                var line = Element("en-tabs", root);
                for (int col = 0; col < 4; col++)
                {
                    int cell = row * 4 + col;
                    Button((cell + 1) + " · " + PipeLabel(board.Mask(cell)),
                        () => apply(new WorkshopMove(cell)), line);
                }
            }
        }

        private static string PipeLabel(int mask)
        {
            string text = "";
            if ((mask & 1) != 0) text += "N";
            if ((mask & 2) != 0) text += "E";
            if ((mask & 4) != 0) text += "S";
            if ((mask & 8) != 0) text += "O";
            return text;
        }

        private void DrawWeaving(WorkshopBoard board, VisualElement root, Action<WorkshopMove> apply)
        {
            for (int row = 0; row < 4; row++)
            {
                var line = Element("en-tabs", root);
                for (int col = 0; col < 4; col++)
                {
                    int cell = row * 4 + col;
                    var button = Button((cell + 1) + ": " + workshopColours[board.Value(cell)],
                        () => apply(new WorkshopMove(cell)), line);
                    button.style.borderBottomColor = PoolroomsWorkshopMaterials.Tint(board.Value(cell));
                    button.style.borderBottomWidth = 4;
                }
            }
        }

        private void DrawHerbarium(WorkshopBoard board, VisualElement root, Action<WorkshopMove> apply)
        {
            for (int i = 0; i < board.CellCount; i++)
            {
                int cell = i;
                var line = Element("en-tabs", root);
                Text((i + 1) + ": " + HerbariumBoard.Species(board.Value(i)), "en-note", line);
                Button("Cambiar con la siguiente", () => apply(new WorkshopMove(cell)), line, "en-quiet");
            }
        }

        private void DrawDials(WorkshopBoard board, VisualElement root, Action<WorkshopMove> apply, bool compass)
        {
            for (int i = 0; i < board.CellCount; i++)
            {
                int cell = i;
                var line = Element("en-tabs", root);
                string value = compass ? workshopDirections[board.Value(i)] : (board.Value(i) == 0 ? 12 : board.Value(i)).ToString();
                Text("Disco " + (i + 1) + ": " + value, "en-invitation", line);
                Button("← Retroceder", () => apply(new WorkshopMove(cell, -1)), line);
                Button("Avanzar →", () => apply(new WorkshopMove(cell)), line);
            }
        }

        private void DrawBalance(BalanceBoard board, VisualElement root, Action<WorkshopMove> apply)
        {
            for (int i = 0; i < 6; i++)
            {
                int cell = i;
                string label = (board.Value(i) == 1 ? "Retirar " : "Colocar ") + board.Weight(i) + " medidas";
                Button(label, () => apply(new WorkshopMove(cell)), root);
            }
        }

        private void DrawStars(WorkshopBoard board, VisualElement root, Action<WorkshopMove> apply)
        {
            for (int row = 0; row < 3; row++)
            {
                var line = Element("en-tabs", root);
                for (int col = 0; col < 3; col++)
                {
                    int cell = row * 3 + col;
                    string label = "Estrella " + board.Target(cell) + (board.Value(cell) == 1 ? " · unida" : "");
                    Button(label, () => apply(new WorkshopMove(cell)), line);
                }
            }
        }

        private void DrawBells(BellBoard board, VisualElement root, Action<WorkshopMove> apply)
        {
            var line = Element("en-tabs", root);
            for (int note = 0; note < BellBoard.Notes; note++)
            {
                int selected = note;
                Button(workshopNotes[note], () => apply(new WorkshopMove(selected)), line);
            }
            Text("Las notas distintas de la tarjeta se pueden escuchar sin borrar tu progreso.", "en-note", root);
        }

        private void DrawTides(WorkshopBoard board, VisualElement root, Action<WorkshopMove> apply, Action redraw)
        {
            Text(selectedReservoir < 0 ? "Elige primero el depósito de origen." : "Origen " + (selectedReservoir + 1) + ": elige el destino.", "en-note", root);
            for (int i = 0; i < 4; i++)
            {
                int cell = i;
                var button = Button("Depósito " + (i + 1) + " · " + board.Value(i) + "/6 · meta " + board.Target(i), () =>
                {
                    if (selectedReservoir < 0) { selectedReservoir = cell; redraw(); return; }
                    int source = selectedReservoir;
                    selectedReservoir = -1;
                    apply(new WorkshopMove(source * 4 + cell));
                    redraw();
                }, root);
                button.SetEnabled(selectedReservoir < 0 ? board.Value(i) > 0 :
                    ((TideBoard)board).CanTransfer(selectedReservoir, i));
            }
            Button("Cancelar selección", () => { selectedReservoir = -1; redraw(); }, root, "en-quiet");
        }

        private void DrawBinding(WorkshopBoard board, VisualElement root, Action<WorkshopMove> apply)
        {
            for (int row = 0; row < 3; row++)
            {
                int selected = row;
                var line = Element("en-tabs", root);
                string label = "Grupo " + (row + 1) + ": ";
                for (int col = 0; col < 3; col++) label += workshopColours[board.Value(row * 3 + col)] + " ";
                Text(label, "en-note", line);
                Button("← Girar", () => apply(new WorkshopMove(selected, -1)), line);
                Button("Girar →", () => apply(new WorkshopMove(selected)), line);
            }
        }

        private void DrawNursery(WorkshopBoard board, VisualElement root, Action<WorkshopMove> apply)
        {
            for (int i = 0; i < 6; i++)
            {
                int cell = i;
                var line = Element("en-tabs", root);
                Text("Semillero " + (i + 1) + ": " + board.Value(i) + "/5 · meta " + board.Target(i), "en-note", line);
                Button("Vaciar una medida", () => apply(new WorkshopMove(cell, -1)), line, "en-quiet");
                Button("Regar una medida", () => apply(new WorkshopMove(cell)), line);
            }
        }

        private void PlayWorkshopPhrase(PoolroomsStation station, WorkshopBoard board, int lease)
        {
            if (!WorkshopAvailable(station, lease)) return;
            int revision = board.Revision;
            int playback = ++workshopPlayback;
            for (int i = 0; i < board.CellCount; i++)
            {
                int note = board.Target(i);
                content.schedule.Execute(() =>
                {
                    if (!WorkshopAvailable(station, lease) || board.Revision != revision || playback != workshopPlayback) return;
                    sound?.PlayInteractiveNote(note, station.SoundPosition);
                    station.Pulse();
                }).StartingIn(i * 550);
            }
        }
    }
}
