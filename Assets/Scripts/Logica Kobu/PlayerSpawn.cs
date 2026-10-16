using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [Header("Punto de respawn")]

    // Punto donde aparecerá Kobu cuando muera.
    [SerializeField] private Transform playerSpawn;


    [Header("Referencias")]

    // Referencia al sistema de vida.
    [SerializeField] private KobuHealth kobuHealth;

    // Referencia al movimiento de Kobu.
    [SerializeField] private LogicaKobu logicaKobu;

    // Referencia al sistema de ataque.
    [SerializeField] private KobuAttack kobuAttack;

    // Referencia al Animator.
    [SerializeField] private Animator anim;


    private void Start()
    {
        // Buscamos automáticamente los componentes
        // si no fueron asignados en el Inspector.
        if (kobuHealth == null)
            kobuHealth = GetComponent<KobuHealth>();

        if (logicaKobu == null)
            logicaKobu = GetComponent<LogicaKobu>();

        if (kobuAttack == null)
            kobuAttack = GetComponent<KobuAttack>();

        if (anim == null)
            anim = GetComponent<Animator>();
    }


    /// Respawnea a Kobu después de morir.
    /// Este método es llamado por el Animation Event
    /// al finalizar la animación de muerte.

    public void Respawnear()
    {
        // NUEVO: si hay Game Over, el jugador no reaparece solo
        if (GameOverController.JuegoTerminado) return;

        // Colocamos a Kobu en el punto de respawn.
        transform.position = playerSpawn.position;

        // Hacemos que mire en la misma dirección que el Spawn.
        transform.rotation = playerSpawn.rotation;

        // Restauramos la vida al máximo.
        kobuHealth.RestaurarVida();

        // Volvemos al estado Blend Tree.
        anim.Play("Blend Tree");

        // Reactivamos el movimiento.
        logicaKobu.enabled = true;

        // Reactivamos el ataque.
        kobuAttack.enabled = true;
    }
}