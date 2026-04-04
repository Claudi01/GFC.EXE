using System.Collections;
using UnityEngine;

public class MecanicaDiario : MonoBehaviour
{
    [Header("Interface UI")]
    public GameObject telaDiario;
    public GameObject telaPreta;
    private FaderScript fader;

    [Header("Referências do Player")]
    public MonoBehaviour scriptMovimento; // Aqui você vai arrastar o FirstPersonController

    [Header("A Mágica do Bote")]
    public GameObject boteInteiro;
    public GameObject boteDestruido; 
    public AudioSource somImpacto; 

    private bool lendo = false;
    private bool jaCaiu = false;

    void Start()
    {
        if (telaPreta != null) fader = telaPreta.GetComponent<FaderScript>();
    }

    public void Interagir()
    {
        if (fader == null) return;
        if (!lendo) StartCoroutine(AbrirProcesso());
        else StartCoroutine(FecharProcesso());
    }

    // Função para ligar/desligar o controle do jogador e o mouse
    void AlternarControlePlayer(bool estado)
    {
        if (scriptMovimento != null) scriptMovimento.enabled = estado;

        // Libera ou trava o cursor do mouse
        if (estado) // Se o jogador voltou a andar
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else // Se o jogador está lendo
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    IEnumerator AbrirProcesso()
    {
        lendo = true;
        AlternarControlePlayer(false); // TRAVA O PLAYER AQUI
        
        telaPreta.SetActive(true);
        yield return StartCoroutine(fader.FazerFade(true)); 
        
        telaDiario.SetActive(true);
        yield return StartCoroutine(fader.FazerFade(false));
    }

    IEnumerator FecharProcesso()
    {
        yield return StartCoroutine(fader.FazerFade(true));
        
        telaDiario.SetActive(false);
        
        if (!jaCaiu)
        {
            if (boteInteiro != null) boteInteiro.SetActive(false); 
            if (boteDestruido != null) boteDestruido.SetActive(true);
            if (somImpacto != null) somImpacto.Play(); 
            jaCaiu = true;
        }

        yield return StartCoroutine(fader.FazerFade(false));
        telaPreta.SetActive(false);
        
        AlternarControlePlayer(true); // LIBERA O PLAYER AQUI
        lendo = false;
    }
}