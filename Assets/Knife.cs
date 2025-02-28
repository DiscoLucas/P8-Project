using UnityEngine;
using EzySlice;
using Unity.VisualScripting;

public class Knife : MonoBehaviour
{
        public Transform knife;
        public GameObject target;
        public Material inside;
        float cutForce = 1000;
public void slice(GameObject target){
    SlicedHull hull = target.Slice(knife.position, -knife.transform.TransformDirection(Vector3.forward));

    if(hull != null){
        GameObject upperHull = hull.CreateUpperHull(target, inside);
        SetupSlicedComponent(upperHull);

        GameObject lowerHull = hull.CreateLowerHull(target, inside);
        SetupSlicedComponent(lowerHull);

        Destroy(target);
    }
}

public void SetupSlicedComponent(GameObject slicedObject){
    Rigidbody rb = slicedObject.AddComponent<Rigidbody>();
    MeshCollider collider = slicedObject.AddComponent<MeshCollider>();
    collider.convex = true;
    rb.AddExplosionForce(cutForce, slicedObject.transform.position, 1);
}
    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject == target){
            slice(target);
        }
    }
}
