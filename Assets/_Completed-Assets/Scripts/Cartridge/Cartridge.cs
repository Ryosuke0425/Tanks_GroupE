using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cartridge : MonoBehaviour
{
    private float blinkInterval = 0.2f;
    private float lifetime = 10.0f;
    private Renderer cartridgeRenderer;
    private float elapsedTime = 0f;
    //private bool isVisible = true;
    // Start is called before the first frame update
    void Start()
    {
        cartridgeRenderer = GetComponent<Renderer>();
        StartCoroutine(BlinkAndDestroy());
    }

    private IEnumerator BlinkAndDestroy()
    {
        while (elapsedTime < lifetime)//lifetimeˆÈ‰º‚Å‚ ‚éŽž“_–Å‚³‚¹‚é
        {
            if (elapsedTime % blinkInterval < blinkInterval / 2)
            {
                cartridgeRenderer.enabled = true;
            }
            else
            {
                cartridgeRenderer.enabled = false;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
