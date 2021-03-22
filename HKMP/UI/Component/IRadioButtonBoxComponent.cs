using System;

namespace HKMP.UI.Component {
    public delegate void OnValueChange(int newIndex);
    
    public interface IRadioButtonBoxComponent {
        
        /**
         * Set a callback method for when the active button changes
         */
        void SetOnChange(OnValueChange onValueChange);

        /**
         * Get the index of the currently active radio button
         */
        int GetActiveIndex();

        /**
         * Set the index of the radio button that should be active
         */
        void SetActiveIndex(int index, bool invokeCallback);
    }
}