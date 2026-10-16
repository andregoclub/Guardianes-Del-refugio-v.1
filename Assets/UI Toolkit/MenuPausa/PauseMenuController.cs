using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor; // evita ambigüedad con UnityEngine.UIElements.Cursor

[RequireComponent(typeof(UIDocument))]
public class PauseMenuController : MonoBehaviour
{
    [Header("Tecla")]
    [SerializeField] private KeyCode teclaPausa = KeyCode.Escape;

    [Header("Nombres de los elementos en el UI Builder")]
    [SerializeField] private string nombreContenedor = "Contenedor";
    [SerializeField] private string nombreBotonResume = "Resume";
    [SerializeField] private string nombreBotonRestart = "Restart";
    [SerializeField] private string nombreBotonMenu = "MenuPrincipal";

    [Header("Escenas (deben estar en Build Settings)")]
    [SerializeField] private string escenaJuego = "GameScene";
    [SerializeField] private string escenaMenu = "Menu Principal";

    // ---------- Estado de pausa ----------

    private static bool pausado;
    private static int frameReanudado = -1;

    /// <summary>
    /// Otros scripts (ataque, dash, salto) lo consultan para ignorar el input mientras está pausado.
    /// Sigue en true el frame en que se reanuda, para que el clic en "Resume" no cuente como un golpe.
    /// </summary>
    public static bool EstaPausado => pausado || Time.frameCount <= frameReanudado;

    private VisualElement panel;          // el contenedor que se muestra u oculta
    private CursorLockMode cursorAnterior;
    private bool cursorVisibleAnterior;

    // ---------- Inicio ----------

    void Start()
    {
        pausado = false;
        frameReanudado = -1;

        var raiz = GetComponent<UIDocument>().rootVisualElement;

        panel = raiz.Q<VisualElement>(nombreContenedor);
        if (panel == null)
        {
            Debug.LogError($"No encuentro el elemento '{nombreContenedor}' en el UXML del menú de pausa.");
            enabled = false;
            return;
        }

        ConectarBoton(raiz, nombreBotonResume, Reanudar);
        ConectarBoton(raiz, nombreBotonRestart, () => CargarEscena(escenaJuego));
        ConectarBoton(raiz, nombreBotonMenu, () => CargarEscena(escenaMenu));

        panel.style.display = DisplayStyle.None; // arranca oculto
    }

    void ConectarBoton(VisualElement raiz, string nombre, System.Action accion)
    {
        var boton = raiz.Q<Button>(nombre);
        if (boton == null)
        {
            Debug.LogWarning($"No encuentro el botón '{nombre}' en el UXML del menú de pausa.");
            return;
        }
        boton.clicked += accion;
    }

    // ---------- Pausar y reanudar ----------

    void Update()
    {
        if (!Input.GetKeyDown(teclaPausa)) return;

        if (pausado) Reanudar();
        else Pausar();
    }

    void Pausar()
    {
        pausado = true;
        Time.timeScale = 0f;

        // Guarda el estado del cursor y lo libera para poder hacer clic en los botones
        cursorAnterior = Cursor.lockState;
        cursorVisibleAnterior = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        panel.style.display = DisplayStyle.Flex;
    }

    void Reanudar()
    {
        panel.style.display = DisplayStyle.None;

        Time.timeScale = 1f;
        pausado = false;
        frameReanudado = Time.frameCount;

        Cursor.lockState = cursorAnterior;
        Cursor.visible = cursorVisibleAnterior;
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
        pausado = false;
        SceneManager.LoadScene(nombreEscena);
    }

    void OnDestroy()
    {
        // Seguridad: nunca dejar el juego congelado
        if (pausado)
        {
            Time.timeScale = 1f;
            pausado = false;
        }
    }
}