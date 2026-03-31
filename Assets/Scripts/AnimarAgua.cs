using UnityEngine;

public class AnimarAgua : MonoBehaviour
{
    [Tooltip("Coloque aqui os 3 frames da textura da água")]
    public Texture[] frames; 
    
    [Tooltip("Velocidade da animação. PS1 costuma ser mais lento, entre 3 e 5")]
    public float fps = 4f;   
    
    private Renderer rend;

    void Start()
    {
       
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        
        if (frames.Length == 0) return; 

        int index = (int)(Time.time * fps) % frames.Length;
        
        rend.material.mainTexture = frames[index];
    }
}