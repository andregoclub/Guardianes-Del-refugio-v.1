using UnityEngine;

public class KobuAttack : MonoBehaviour
{
    private Animator anim;

    public bool estoyAtacando;

    private bool siguienteGolpeDerecho = false;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // NUEVO: no atacar mientras el juego está en pausa
        if (PauseMenuController.EstaPausado) return;

        if (Input.GetKeyDown(KeyCode.Mouse0) && !estoyAtacando)
        {
            if (siguienteGolpeDerecho)
            {
                anim.SetTrigger("Rightpunch");
            }
            else
            {
                anim.SetTrigger("golpe");
            }

            siguienteGolpeDerecho = !siguienteGolpeDerecho;
            estoyAtacando = true;
        }
    }

    // Animation Event: se llama al final de la animación
    public void DejaDeGolpear()
    {
        estoyAtacando = false;
    }
}