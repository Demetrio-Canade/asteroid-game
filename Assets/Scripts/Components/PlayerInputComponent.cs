using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// I comandi del giocatore in partita: fuoco e cambio arma.
///
/// Non conosce il gioco: dice soltanto che un tasto è premuto. Costruisce le
/// proprie InputAction a codice, senza dipendere dall'asset InputSystem_Actions.
/// </summary>
public class PlayerInputComponent : MonoBehaviour
{
    [SerializeField] private float scrollThreshold = 0.1f;

    // ---------- OUTPUTS ----------

    public event Action OnWeaponToggleRequested;

    /// <summary>
    /// Il fuoco è uno stato, non un evento: si legge quando serve invece di
    /// essere annunciato. Così non può restare "incastrato premuto" se i comandi
    /// vengono spenti a metà raffica.
    /// </summary>
    public bool FireHeld => fireAction != null && fireAction.enabled && fireAction.IsPressed();

    private InputAction fireAction;
    private InputAction toggleAction;
    private InputAction scrollAction;

    private bool scrollArmed = true;

    private void Awake()
    {
        fireAction = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
        toggleAction = new InputAction("ToggleWeapon", InputActionType.Button, "<Keyboard>/q");
        scrollAction = new InputAction("ScrollWeapon", InputActionType.Value, "<Mouse>/scroll/y");

        toggleAction.performed += OnTogglePerformed;
    }

    private void OnEnable()
    {
        fireAction?.Enable();
        toggleAction?.Enable();
        scrollAction?.Enable();
    }

    private void OnDisable()
    {
        fireAction?.Disable();
        toggleAction?.Disable();
        scrollAction?.Disable();
    }

    private void OnDestroy()
    {
        if (toggleAction != null)
        {
            toggleAction.performed -= OnTogglePerformed;
        }

        fireAction?.Dispose();
        toggleAction?.Dispose();
        scrollAction?.Dispose();
    }

    // ---------- INPUTS ----------

    /// <summary>Accende o spegne i comandi. Da spenti non arriva più niente.</summary>
    public void SetActive(bool value)
    {
        enabled = value;
    }

    private void Update()
    {
        if (scrollAction == null)
        {
            return;
        }

        // La rotella manda un valore continuo: aspettiamo che torni a riposo
        // prima di accettare il prossimo scatto, altrimenti un colpo di rotella
        // cambierebbe arma più volte di fila.
        float scroll = scrollAction.ReadValue<float>();

        if (Mathf.Abs(scroll) < scrollThreshold)
        {
            scrollArmed = true;
            return;
        }

        if (!scrollArmed)
        {
            return;
        }

        scrollArmed = false;
        RequestToggle("rotella");
    }

    private void OnTogglePerformed(InputAction.CallbackContext context)
    {
        RequestToggle("Q");
    }

    private void RequestToggle(string source)
    {
        OnWeaponToggleRequested?.Invoke();
    }
}
