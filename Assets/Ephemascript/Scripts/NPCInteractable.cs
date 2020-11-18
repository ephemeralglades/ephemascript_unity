using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ephemascript
{
    public class NPCInteractable : MonoBehaviour
    {
        public NPC npc;

        public string state;
        public virtual void Interact()
        {
            if (!this.isActiveAndEnabled)
            {
                return;
            }
            npc.Interact(state);
        }

        public virtual void Start()
        {
            if (npc == null)
            {
                Debug.LogWarning("NPC not set");
                return;
            }
            if (npc != null && !npc.ShouldNPCInteractableAppear(state, gameObject))
            {
                Destroy(gameObject);
                return;
            }
        }
    }
}
