using HKMP.Networking.Client;
using ModCommon.Util;
using UnityEngine;

namespace HKMP.Game.Client.Entity {
    public class FalseKnight : Entity {
        public FalseKnight(
            NetClient netClient,
            byte entityId, 
            GameObject gameObject
        ) : base(
            netClient, 
            EntityType.FalseKnight, 
            entityId,
            gameObject
        ) {
            Fsm = gameObject.LocateMyFSM("FalseyControl");

            // Jump X
            ControlledVariables[0] = new ControlledVariable(VariableType.Float);
            
            AddControlledStates();
            CreateDefaultControlledStates();
            
            CreateStateEventSending();
            CreateDefaultStateEventSending();
            
            CreateVariableEvents();
        }

        private void AddControlledStates() {
            ControlledStates.Add("Jump Antic");
            ControlledStates.Add("JA Antic");
            ControlledStates.Add("Walls Check 2");
            ControlledStates.Add("Run");
        }

        private void CreateStateEventSending() {
            CreateStateEventSendMethod("Jump", false);
            CreateStateEventSendMethod("JA Jump", false);
            CreateStateEventSendMethod("S Jump", false);
            CreateStateEventSendMethod("Voice?", false);
        }
        
        private void CreateVariableEvents() {
            Fsm.InsertMethod("Jump Antic", 2, () => {
                if (IsControlled) {
                    Logger.Info(this, "Setting Jump X variable for jump");
                    ControlledVariables[0].SetFsmVariable(Fsm, "Jump X");
                } else {
                    Logger.Info(this, "Sending Jump X variable for jump");
                    SendVariableUpdate(0, Fsm.FsmVariables.GetFsmFloat("Jump X").Value);
                }
            });
            Fsm.InsertMethod("JA Antic", 5, () => {
                if (IsControlled) {
                    Logger.Info(this, "Setting Jump X variable for jump attack");
                    ControlledVariables[0].SetFsmVariable(Fsm, "Jump X");
                } else {
                    Logger.Info(this, "Sending Jump X variable for jump attack");
                    SendVariableUpdate(0, Fsm.FsmVariables.GetFsmFloat("Jump X").Value);
                }
            });
            Fsm.InsertMethod("Walls Check 2", 2, () => {
                if (IsControlled) {
                    Logger.Info(this, "Setting Jump X variable for S jump");
                    ControlledVariables[0].SetFsmVariable(Fsm, "Jump X");
                } else {
                    Logger.Info(this, "Sending Jump X variable for S jump");
                    SendVariableUpdate(0, Fsm.FsmVariables.GetFsmFloat("Jump X").Value);
                }
            });
        }
    }
}