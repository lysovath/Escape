using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GoalPoint : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created


    private Collider[] colliders;
    void Start()
    {
        colliders = GetComponents<Collider>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerExit(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            Vector3 playerPos = other.transform.position;
            Vector3 goalPos = transform.position;

            Vector3 goalScale = transform.lossyScale;

            Debug.Log(goalScale);

            double x = Math.Abs(goalScale.x * 0.49);
            double z = Math.Abs(goalScale.z * 0.49);

            if (playerPos.x > (goalPos.x - x) && playerPos.x < (goalPos.x + x)
            && playerPos.z > (goalPos.z - z) && playerPos.z < (goalPos.z + z))
            {

                foreach(Collider collider in colliders)
                {
                    collider.isTrigger = false;
                }
                
                // foreach (Collider wallCollider in wallColliders)
                // {
                //     wallCollider.isTrigger = false;
                //     Debug.Log("IsTrigger");
                // }
            }

        }
    }
}
