using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerStamina))]
public class PlayerDash : MonoBehaviour
{
    private enum DireccionDash { Adelante, Izquierda, Derecha }

    [Header("Referencias")]
    [SerializeField] private Animator animator;

    [Header("Dash")]
    [SerializeField] private KeyCode dashKey = KeyCode.E;
    [SerializeField] private float dashCost = 25f;

    [Header("Desplazamiento")]
    [SerializeField] private float dashDistance = 2.5f;   // metros que avanza
    [SerializeField] private float dashDuration = 0.25f;  // segundos (ajustalo a la duración de tu animación)
    [SerializeField] private bool bloquearDuranteAtaque = true;

    [Header("Distancia según la dirección")]
    [Tooltip("1 = misma distancia que hacia adelante. Menos de 1 = dash de lado más corto (la animación no cambia).")]
    [Range(0.2f, 1.5f)]
    [SerializeField] private float multiplicadorLateral = 0.65f;

    [Header("Triggers del Animator")]
    [SerializeField] private string triggerAdelante = "Dash";
    [SerializeField] private string triggerIzquierda = "DashIzquierda";
    [SerializeField] private string triggerDerecha = "DashDerecha";

    [Header("Diagnóstico")]
    [Tooltip("Escribe en la consola qué dirección y qué trigger se usó en cada dash.")]
    [SerializeField] private bool mostrarDebug = false;

    /// <summary>True mientras dura el dash. LogicaKobu lo usa para no pisar el movimiento.</summary>
    public bool EstaDasheando { get; private set; }

    /// <summary>Para el sistema de daño: ignorar golpes mientras se esquiva.</summary>
    public bool EsInvulnerable => EstaDasheando;

    private PlayerStamina stamina;
    private KobuAttack ataque;
    private TargetLock targetLock;
    private Rigidbody rb;

    void Awake()
    {
        stamina = GetComponent<PlayerStamina>();
        ataque = GetComponent<KobuAttack>();
        targetLock = GetComponent<TargetLock>();
        rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        ValidarTriggers();
    }

    // Avisa al iniciar si algún trigger no existe en el Animator (error de nombre o mayúsculas)
    void ValidarTriggers()
    {
        if (animator == null) return;

        foreach (string nombre in new[] { triggerAdelante, triggerIzquierda, triggerDerecha })
        {
            if (string.IsNullOrEmpty(nombre)) continue;

            bool existe = false;
            foreach (AnimatorControllerParameter parametro in animator.parameters)
            {
                if (parametro.name == nombre && parametro.type == AnimatorControllerParameterType.Trigger)
                {
                    existe = true;
                    break;
                }
            }

            if (!existe)
                Debug.LogWarning($"PlayerDash: el Animator no tiene un trigger llamado '{nombre}'.");
        }
    }

    void Update()
    {
        if (PauseMenuController.EstaPausado) return;
        if (!Input.GetKeyDown(dashKey)) return;
        if (EstaDasheando) return;
        if (bloquearDuranteAtaque && ataque != null && ataque.estoyAtacando) return;
        if (!stamina.TryConsume(dashCost)) return;

        DireccionDash direccion = ElegirDireccion();
        string trigger = NombreTrigger(direccion);

        if (mostrarDebug)
            Debug.Log($"PlayerDash: dirección {direccion}, trigger '{trigger}'");

        if (animator != null)
            animator.SetTrigger(trigger);

        StartCoroutine(DashRoutine(direccion));
    }

    // Con objetivo fijado: A/D esquivan de lado. En cualquier otro caso, hacia adelante.
    DireccionDash ElegirDireccion()
    {
        float x = Input.GetAxisRaw("Horizontal");
        bool fijado = targetLock != null && targetLock.EstaFijado;

        if (fijado && x < -0.1f) return DireccionDash.Izquierda;
        if (fijado && x > 0.1f) return DireccionDash.Derecha;

        return DireccionDash.Adelante;
    }

    string NombreTrigger(DireccionDash direccion)
    {
        string nombre = direccion switch
        {
            DireccionDash.Izquierda => triggerIzquierda,
            DireccionDash.Derecha => triggerDerecha,
            _ => triggerAdelante
        };

        // Si un trigger se deja vacío, se usa el de adelante
        return string.IsNullOrEmpty(nombre) ? triggerAdelante : nombre;
    }

    float DistanciaPara(DireccionDash direccion)
    {
        return direccion == DireccionDash.Adelante ? dashDistance : dashDistance * multiplicadorLateral;
    }

    Vector3 VectorDireccion(DireccionDash direccion)
    {
        switch (direccion)
        {
            case DireccionDash.Izquierda: return -transform.right;
            case DireccionDash.Derecha: return transform.right;
            default: return transform.forward;
        }
    }

    IEnumerator DashRoutine(DireccionDash direccion)
    {
        EstaDasheando = true;

        // La dirección se fija al inicio del dash
        Vector3 vector = VectorDireccion(direccion);
        float distancia = DistanciaPara(direccion);

        float elapsed = 0f;
        float progresoPrevio = 0f;

        while (elapsed < dashDuration)
        {
            yield return new WaitForFixedUpdate();

            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / dashDuration);

            // Ease-out: arranca rápido y frena suave
            float progreso = 1f - (1f - t) * (1f - t);
            Vector3 paso = vector * (distancia * (progreso - progresoPrevio));
            progresoPrevio = progreso;

            if (rb != null) rb.MovePosition(rb.position + paso);
            else transform.position += paso;
        }

        EstaDasheando = false;
    }
}