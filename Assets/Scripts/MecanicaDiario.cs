using System.Collections;
using UnityEngine;

public class MecanicaDiario : MonoBehaviour
{
    [Header("Interface UI")]
    public GameObject telaDiario;
    public GameObject telaPreta;
    private FaderScript fader;

    [Header("Referências do Player")]
    public MonoBehaviour scriptMovimento;

    [Header("A Mágica do Bote & Itens")]
    public GameObject boteInteiro;
    public GameObject boteDestruido;
    public GameObject ganchoEscalada; // Gancho coletável que vai na areia
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

    void AlternarControlePlayer(bool estado)
    {
        if (scriptMovimento != null) scriptMovimento.enabled = estado;

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
        lendo = true;
        AlternarControlePlayer(false);
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
            
            // Liga o gancho no exato momento em que o bote quebra!
            if (ganchoEscalada != null) ganchoEscalada.SetActive(true);
            
            if (somImpacto != null) somImpacto.Play();
            jaCaiu = true;
        }

        yield return StartCoroutine(fader.FazerFade(false));
        telaPreta.SetActive(false);
        AlternarControlePlayer(true);
        lendo = false;
    }
}