using System;

namespace HKMP.Game.Client.Entity {
    public class ControlledVariable {

        private VariableType _variableType;
        
        public bool BoolValue { get; set; }
        public int IntValue { get; set; }
        public float FloatValue { get; set; }

        public ControlledVariable(VariableType variableType) {
            _variableType = variableType;
        }

        public void UpdateVariable(byte[] variableArray, ref int currentIndex) {
            var typeByte = variableArray[currentIndex++];
            var type = (VariableType) typeByte;

            Logger.Info(this, $"Updating variable of type: {type}");

            switch (type) {
                case VariableType.Bool:
                    BoolValue = BitConverter.ToBoolean(variableArray, currentIndex);
                    currentIndex += 1;
                    
                    break;
                case VariableType.Int:
                    IntValue = BitConverter.ToInt32(variableArray, currentIndex);
                    currentIndex += 4;
                    
                    break;
                case VariableType.Float:
                    FloatValue = BitConverter.ToSingle(variableArray, currentIndex);
                    currentIndex += 4;

                    Logger.Info(this, $"New float value: {FloatValue}");

                    break;
            }
        }

        public void SetFsmVariable(PlayMakerFSM fsm, string variableName) {
            switch (_variableType) {
                case VariableType.Bool:
                    fsm.FsmVariables.GetFsmBool(variableName).Value = BoolValue;
                    
                    break;
                case VariableType.Int:
                    fsm.FsmVariables.GetFsmInt(variableName).Value = IntValue;
                    
                    break;
                case VariableType.Float:
                    fsm.FsmVariables.GetFsmFloat(variableName).Value = FloatValue;

                    Logger.Info(this, $"Set float value: {FloatValue}");
                    
                    break;
            }
        }
        
    }
}