using Unity.Mathematics;
using UnityEngine;

public class PourFunctionality : MonoBehaviour
{
    ParticleSystem myParticles;
    float pourspeed;
    void Start(){
        myParticles = GetComponent<ParticleSystem>();
    }
    void Update()
    {
        //if I get tilted below a threshold, I start my particle system
        float pourThreshold = 80;
        float pourspeed = (Vector3.Dot(transform.up, Vector3.down)*10); //10 is there to actually make number make a visible difference, I do not know the real world formula for this...
        if (Vector3.Dot(transform.up, Vector3.down) > Mathf.Cos(pourThreshold * Mathf.Deg2Rad)){
            myParticles.Play();
            myParticles.startSpeed = pourspeed; //I'm going to use the deprecated one because the other one is a filthy liar - if you can fix it go try...
        }else
        {
            myParticles.Stop();
        }
        //if I get below, I stop pouring
    }
}
