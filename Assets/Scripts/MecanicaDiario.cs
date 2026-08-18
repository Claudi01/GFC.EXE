using System.Collections;
using UnityEngine;

public class MecanicaDiario : MonoBehaviour
{
    [Header("Interface UI")]
    public GameObject telaDiario;
    public GameObject telaPreta;
    public GameObject textoUIInteracao; // NOVO: A marreta da UI
    private FaderScript fader;

    [Header("Referências do Player")]
    public MonoBehaviour scriptMovimento; 
    public MonoBehaviour scriptOlharCamera; // NOVO: A marreta do Mouse
    public InteracaoPlayer scriptInteracao;

    [Header("A Mágica do Bote & Itens")]
    public GameObject boteInteiro;
    public GameObject boteDestruido;
    public GameObject ganchoEscalada;
    public AudioSource somImpacto;

    private bool lendo = false;
    private bool jaCaiu = false;
    private bool emTransicao = false;

    void Start()
    {
        if (telaPreta != null) fader = telaPreta.GetComponent<FaderScript>();
        
        if (scriptMovimento == null)
        {
            scriptMovimento = Object.FindFirstObjectByType<FirstPersonController>();
        }

        if (scriptInteracao == null)
        {
            scriptInteracao = Object.FindFirstObjectByType<InteracaoPlayer>();
        }
    }

    void Update()
    {
        if (lendo && !emTransicao && Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(FecharProcesso());
        }
    }

    public void Interagir()
    {
        if (fader == null || emTransicao) return;
        
        if (!lendo) StartCoroutine(AbrirProcesso());
    }

    void AlternarControlePlayer(bool estado)
    {
        // 1. Desliga o movimento do corpo
        if (scriptMovimento != null) scriptMovimento.enabled = estado;

        // 2. Desliga o giro da câmera na força bruta
        if (scriptOlharCamera != null) scriptOlharCamera.enabled = estado;

        // 3. Desliga o sistema de interação invisível
        if (scriptInteracao != null) scriptInteracao.enabled = estado; 

        // 4. Apaga o texto "Ler" direto na raiz
        if (textoUIInteracao != null) textoUIInteracao.SetActive(estado);

        // 5. Solta o mouse para clicar na tela se precisar (ou só trava)
        if (estado)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    IEnumerator AbrirProcesso()
    {
        emTransicao = true;
        lendo = true;
        AlternarControlePlayer(false); 
        
        telaPreta.SetActive(true);
        yield return StartCoroutine(fader.FazerFade(true));
        telaDiario.SetActive(true);
        yield return StartCoroutine(fader.FazerFade(false));
        
        emTransicao = false;
    }

    IEnumerator FecharProcesso()
    {
        emTransicao = true;
        
        yield return StartCoroutine(fader.FazerFade(true));
        telaDiario.SetActive(false);

        if (!jaCaiu)
        {
            if (boteInteiro != null) boteInteiro.SetActive(false);
            if (boteDestruido != null) boteDestruido.SetActive(true);
            
            if (ganchoEscalada != null) ganchoEscalada.SetActive(true);
            
            if (somImpacto != null) somImpacto.Play();
            jaCaiu = true;
        }

        yield return StartCoroutine(fader.FazerFade(false));
        telaPreta.SetActive(false);
        
        AlternarControlePlayer(true); 
        lendo = false;
        emTransicao = false;
    }
}