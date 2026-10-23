using System.Numerics;
using Poolrooms.Core;
using UnityEngine;

namespace Poolrooms
{
    [DisallowMultipleComponent]
    public sealed class PoolroomsForest39Activity : MonoBehaviour
    {
        Forest39Activity kind;
        Forest39ActivityState state;
        string key, note;
        GameObject visual;
        PoolroomsChunk owner;
        bool restored;

        public string Label
        {
            get
            {
                if (kind == Forest39Activity.FallenLeaf || kind == Forest39Activity.Acorn) return "recoger recuerdo";
                if (kind == Forest39Activity.Listening) return "escuchar el bosque";
                if (kind == Forest39Activity.FieldNotebook) return "leer el cuaderno";
                return state != null && state.Complete ? "recoger ficha del estudio" : "ajustar estudio del bosque";
            }
        }

        public void Configure(Forest39Activity activity, string address, uint seed,
            GameObject rendered, string story, PoolroomsChunk resources)
        {
            kind = activity;
            key = address;
            state = new Forest39ActivityState(activity, seed);
            visual = rendered;
            note = story;
            owner = resources;
        }

        public void Restore(PoolroomsInventory inventory)
        {
            if (restored || state == null || inventory == null) return;
            restored = true;
            int position = 0;
            bool stored = inventory.State(key + ":saved");
            for (int bit = 0; bit < 3; bit++)
                if (inventory.State(key + ":p" + bit)) position |= 1 << bit;
            if (stored && position < state.Limit)
                state.Restore(position, inventory.State(key + ":complete"), inventory.WasCollected(key + ":reward"));
            RefreshGeometry();
        }

        public bool Use(PoolroomsExplorer actor)
        {
            if (actor == null || actor.IsPaused || actor.InputCaptured || !actor.IsReady || state == null) return false;
            var inventory = PoolroomsInventory.For(actor);
            if (inventory == null) return false;
            Restore(inventory);
            var sound = actor.GetComponent<PoolroomsSoundscape>();
            if (kind == Forest39Activity.FieldNotebook)
            {
                inventory.Journal.Read(key);
                var room = actor.world.RoomAtViewer();
                var local = actor.transform.position - actor.world.RoomWorldPosition(room);
                inventory.ShowText("Cuaderno del Nivel 39", note + "\n\n" +
                    Forest39RouteMap.Describe(actor.world.CurrentLayout, room, local.x, local.z));
                inventory.Animate(HumanoidGesture.Read, 2.5f);
                sound?.PlayStation(StationSound.Page, transform.position);
                return true;
            }
            if (kind == Forest39Activity.Listening)
            {
                inventory.Journal.Record(RoomActivity.Rest, key);
                if(actor.Seating!=null)actor.Seating.Sit(GetComponent<PoolroomsInteractable>());
                inventory.Toast("El viento pasa entre las hojas. Puedes quedarte aquí sin completar ninguna tarea.", 6);
                return true;
            }
            if (kind == Forest39Activity.FallenLeaf || kind == Forest39Activity.Acorn)
            {
                if (inventory.WasCollected(key)) return false;
                if (!inventory.Items.Add(Forest39Activities.ItemKey(kind), BigInteger.One))
                {
                    inventory.Toast("El inventario está lleno. El recuerdo seguirá aquí.");
                    return true;
                }
                inventory.MarkCollected(key);
                inventory.Animate(HumanoidGesture.Reach);
                sound?.PlayStation(StationSound.Leaves, transform.position);
                inventory.Toast("Guardado: " + Forest39Activities.Name(kind));
                gameObject.SetActive(false);
                return true;
            }
            if (state.Complete) return Reward(inventory);
            state.Advance();
            Save(inventory);
            RefreshGeometry();
            inventory.Animate(HumanoidGesture.Adjust);
            sound?.PlayStation(Forest39Activities.Sound(kind), transform.position);
            inventory.Toast(state.Feedback(), 6);
            if (state.Complete) Reward(inventory);
            return true;
        }

        bool Reward(PoolroomsInventory inventory)
        {
            if (inventory.WasCollected(key + ":reward"))
            {
                inventory.Toast("El estudio está terminado; puedes seguir el sendero.");
                return true;
            }
            if (!inventory.Items.Add("forest39:2", BigInteger.One))
            {
                inventory.Toast("El estudio queda guardado. Libera un espacio para recoger su ficha.");
                return true;
            }
            state.Claim();
            inventory.MarkCollected(key + ":reward");
            inventory.Journal.Record(RoomActivity.Game, key);
            inventory.Toast("Ficha del herbario guardada. Gracias por dejar el rincón preparado.", 6);
            return true;
        }

        void Save(PoolroomsInventory inventory)
        {
            inventory.SetState(key + ":saved", true);
            for (int bit = 0; bit < 3; bit++)
                inventory.SetState(key + ":p" + bit, (state.Position & (1 << bit)) != 0);
            inventory.SetState(key + ":complete", state.Complete);
        }

        void RefreshGeometry()
        {
            if (visual == null || owner == null) return;
            MeshFactory.UpdateSurface(visual, Forest39ActivityGeometry.Body(kind, state.Position, state.Limit));
        }
    }
}
