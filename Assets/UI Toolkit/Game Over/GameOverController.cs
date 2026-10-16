using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor; // evita ambigüedad con UnityEngine.UIElements.Cursor

[RequireComponent(typeof(UIDocument))]
public class GameOverController : MonoBehaviour
{
    public static GameOverController Instancia { get; private set; }

    /// <summary>True desde el momento en que Kobu muere. Lo usan el menú de pausa y el respawn para no interferir.</summary>
    public static bool JuegoTerminado { get; private set; }

    [Header("Nombres de los elementos en el UI Builder")]
    [SerializeField] private string nombreContenedor = "Contenedor";
    [SerializeField] private string nombreBotonRestart = "Restart";
    [SerializeField] private string nombreBotonMenu = "Menu";

    [Header("Escenas (deben estar en Build Settings)")]
    [SerializeField] private string escenaJuego = "GameScene";
    [SerializeField] private string escenaMenu = "Menu Principal";

    [Header("Tiempo")]
    [Tooltip("Segundos de espera tras la muerte, para que se vea la animación Death.")]
    [SerializeField] private float retardo = 2f;

    private VisualElement panel;

    // ---------- Inicio ----------

    void Awake()
    {
        Instancia = this;
        JuegoTerminado = false;
    }

    void Start()
    {
        var raiz = GetComponent<UIDocument>().rootVisualElement;

        panel = raiz.Q<VisualElement>(nombreContenedor);
        if (panel == null)
        {
            Debug.LogError($"No encuentro el elemento '{nombreContenedor}' en el UXML del Game Over.");
            return;
        }

        ConectarBoton(raiz, nombreBotonRestart, () => CargarEscena(escenaJuego));
        ConectarBoton(raiz, nombreBotonMenu, () => CargarEscena(escenaMenu));

        panel.style.display = DisplayStyle.None; // arranca oculto
    }

    void ConectarBoton(VisualElement raiz, string nombre, System.Action accion)
    {
        var boton = raiz.Q<Button>(nombre);
        if (boton == null)
        {
            Debug.LogWarning($"No encuentro el botón '{nombre}' en el UXML del Game Over.");
            return;
        }
        boton.clicked += accion;
    }

    // ---------- Mostrar Game Over ----------

    /// <summary>Lo llama KobuHealth cuando Kobu muere.</summary>
    public void Mostrar()
    {
        if (JuegoTerminado) return;
        JuegoTerminado = true;

        StartCoroutine(MostrarConRetardo());
    }

    IEnumerator MostrarConRetardo()
    {
        yield return new WaitForSeconds(retardo);

        if (panel == null) yield break;

        Time.timeScale = 0f;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        panel.style.display = DisplayStyle.Flex;
    }

    // ---------- Cambio de escena ----------

    void CargarEscena(string nombreEscena)
    {
        if (!Application.CanStreamedLevelBeLoaded(nombreEscena))
        {
            Debug.LogError($"La escena '{nombreEscena}' no existe o no está en Build Settings.");
            return;
        }

        Time.timeScale = 1f;   // si no, la escena nueva arrancaría congelada
        JuegoTerminado = false;
        SceneManager.LoadScene(nombreEscena);
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;

        if (JuegoTerminado)
        {
            Time.timeScale = 1f;
            JuegoTerminado = false;
        }
    }
}