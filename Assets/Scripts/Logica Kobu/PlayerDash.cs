using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerStamina))]
public class PlayerDash : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator animator;

    [Header("Dash")]
    [SerializeField] private KeyCode dashKey = KeyCode.E;
    [SerializeField] private float dashCost = 25f;

    [Header("Desplazamiento")]
    [SerializeField] private float dashDistance = 2.5f;   // metros que avanza
    [SerializeField] private float dashDuration = 0.25f;  // segundos (ajustalo a la duración de tu animación)
    [SerializeField] private bool dashHaciaAtrasConS = true; // si mantenés S, esquiva hacia atrás
    [SerializeField] private bool bloquearDuranteAtaque = true;

    /// <summary>True mientras dura el dash. LogicaKobu lo usa para no pisar el movimiento.</summary>
    public bool EstaDasheando { get; private set; }

    /// <summary>Para el sistema de daño: ignorar golpes mientras se esquiva.</summary>
    public bool EsInvulnerable => EstaDasheando;

    private PlayerStamina stamina;
    private KobuAttack ataque;
    private Rigidbody rb;

    void Awake()
    {
        stamina = GetComponent<PlayerStamina>();
        ataque = GetComponent<KobuAttack>();
        rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!Input.GetKeyDown(dashKey)) return;
        if (EstaDasheando) return;
        if (bloquearDuranteAtaque && ataque != null && ataque.estoyAtacando) return;
        if (!stamina.TryConsume(dashCost)) return;

        if (animator != null) animator.SetTrigger("Dash");
        StartCoroutine(DashRoutine());
    }

    IEnumerator DashRoutine()
    {
        EstaDasheando = true;

        // Dirección fijada al inicio: adelante, o atrás si mantenés S
        float dir = (dashHaciaAtrasConS && Input.GetAxisRaw("Vertical") < -0.1f) ? -1f : 1f;
        Vector3 direccion = transform.forward * dir;

        float elapsed = 0f;
        float progresoPrevio = 0f;

        while (elapsed < dashDuration)
        {
            yield return new WaitForFixedUpdate();

            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / dashDuration);

            // Ease-out: arranca rápido y frena suave
            float progreso = 1f - (1f - t) * (1f - t);
            Vector3 paso = direccion * (dashDistance * (progreso - progresoPrevio));
            progresoPrevio = progreso;

            if (rb != null) rb.MovePosition(rb.position + paso);
            else transform.position += paso;
        }

        EstaDasheando = false;
    }
}