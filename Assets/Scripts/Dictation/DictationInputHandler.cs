using Meta.Voice.Samples.Dictation;
using UnityEngine;
using UnityEngine.InputSystem;

public class DictationInputHandler : MonoBehaviour
{
    [SerializeField] private InputActionReference dictationToggleAction;
    [SerializeField] private DictationActivation dictationScript;

    private void OnEnable()
    {
        dictationToggleAction.action.performed += OnTogglePerformed;
        dictationToggleAction.action.Enable();
    }

    private void OnDisable()
    {
        dictationToggleAction.action.performed -= OnTogglePerformed;
        dictationToggleAction.action.Disable();
    }

    private void OnTogglePerformed(InputAction.CallbackContext context)
    {
        dictationScript.ToggleActivation();
    }
}
