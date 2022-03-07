namespace Hkmp.Ui.Component {
    public delegate void OnToggle(bool newValue);

    public interface ICheckboxComponent : IComponent {
        void SetOnToggle(OnToggle onToggle);

        /**
         * Returns whether the checkbox is currently toggled.
         * True if it is checked, false otherwise.
         */
        bool IsToggled { get; }

        /**
         * Set whether this checkbox is toggled
         */
        void SetToggled(bool newValue);

        void SetInteractable(bool interactable);
    }
}