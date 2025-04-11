using System.Collections;
using UnityEngine;

public class IceMelter : MonoBehaviour
{
    public float graceTime = 10f;
    public float meltTime = 360f;
    private bool isMelting = false;

    


    private void OncCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("VR Hand"))
        {
            if (!isMelting)
            {
                isMelting = true;
                // Start the melting process after a grace period
                Invoke("MeltIce", graceTime);
            }
        }
    }

    IEnumerator MeltIce()
    {
        Vector3 originalScale = transform.localScale;

        // Start melting the ice
        float elapsedTime = 0f;
        while (elapsedTime < meltTime)
        {
            
            transform.localScale = Vector3.Lerp(originalScale, originalScale / 100, elapsedTime / meltTime);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Destroy the ice object after melting
        Destroy(gameObject);
    }
}
