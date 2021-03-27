using System.Collections.Generic;
using HKMP.Networking.Client;
using ModCommon.Util;
using UnityEngine;

namespace HKMP.Game.Client.Entity {
    public class FalseKnight : Entity {
        public FalseKnight(
            NetClient netClient,
            byte entityId, 
            GameObject gameObject
        ) : base(netClient, EntityType.FalseKnight, entityId) {
            Fsm = gameObject.LocateMyFSM("FalseyControl");
            
            VariableIndices = new Dictionary<byte, string> {
                {0, "Jump X"}
            };
            
            CreateStateEventSending();
            CreateVariableEventSending();
        }
        
        private void CreateVariableEventSending() {
            Fsm.InsertMethod("Jump Antic", 2, () => {
                Logger.Info(this, "Sending Jump X variable for jump");
                SendVariableUpdate(0, Fsm.FsmVariables.GetFsmFloat("Jump X").Value);
            });
            Fsm.InsertMethod("JA Antic", 5, () => {
                Logger.Info(this, "Sending Jump X variable for jump attack");
                SendVariableUpdate(0, Fsm.FsmVariables.GetFsmFloat("Jump X").Value);
            });
            Fsm.InsertMethod("Walls Check 2", 2, () => {
                Logger.Info(this, "Sending Jump X variable for S jump");
                SendVariableUpdate(0, Fsm.FsmVariables.GetFsmFloat("Jump X").Value);
            });
        }
    }
}