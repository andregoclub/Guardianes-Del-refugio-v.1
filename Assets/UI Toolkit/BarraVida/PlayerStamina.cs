using UnityEngine;

public class PlayerStamina : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private StaminaHUD hud;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float drainPerSecond = 20f;
    [SerializeField] private float regenPerSecond = 15f;
    [SerializeField] private float regenDelay = 1f;           // pausa normal tras gastar
    [SerializeField] private float exhaustedRegenDelay = 2f;  // pausa si se quedó en 0
    [SerializeField] private float minToActAgain = 20f;       // mínimo para volver a correr/dash

    private LogicaKobu movimiento;
    private float current;
    private float lastUseTime;
    private bool exhausted;

    void Awake()
    {
        movimiento = GetComponent<LogicaKobu>();
    }

    void Start()
    {
        current = maxStamina;
        hud.SetStamina(current, maxStamina);
    }

    void Update()
    {
        // Gasta stamina solo si Kobu está corriendo de verdad
        bool running = movimiento != null && movimiento.estoyCorriendo && !exhausted && current > 0f;

        if (running)
        {
            current -= drainPerSecond * Time.deltaTime;
            lastUseTime = Time.time;
        }

        current = Mathf.Clamp(current, 0f, maxStamina);

        // Al llegar a 0 queda agotado
        if (current <= 0f && !exhausted)
        {
            exhausted = true;
            lastUseTime = Time.time;
        }

        // Recarga tras la pausa
        float delay = exhausted ? exhaustedRegenDelay : regenDelay;
        if (!running && Time.time - lastUseTime > delay)
        {
            current += regenPerSecond * Time.deltaTime;
            current = Mathf.Clamp(current, 0f, maxStamina);
        }

        // Sale del agotamiento al llegar al mínimo
        if (exhausted && current >= minToActAgain)
            exhausted = false;

        hud.SetStamina(current, maxStamina);
    }

    public bool CanSprint() => !exhausted && current > 0f;

    /// <summary>
    /// Intenta gastar stamina para una acción puntual (dash, etc.).
    /// Devuelve true si había suficiente y se descontó.
    /// </summary>
    public bool TryConsume(float cost)
    {
        if (exhausted || current < cost) return false;

        current -= cost;
        lastUseTime = Time.time;
        return true;
    }
}