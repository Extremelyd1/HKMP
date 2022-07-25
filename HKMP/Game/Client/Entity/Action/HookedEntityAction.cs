using HutongGames.PlayMaker;

namespace Hkmp.Game.Client.Entity.Action; 

internal class HookedEntityAction {
    public FsmStateAction Action { get; set; }
    
    public int FsmIndex { get; set; }
    
    public int StateIndex { get; set; }

    public int ActionIndex { get; set; }
}