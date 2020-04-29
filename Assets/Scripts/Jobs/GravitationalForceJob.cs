using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

public struct GravitationalForceJob
{
    [ReadOnly]
    public NativeArray<float> mass;
    [ReadOnly]
    public NativeArray<Vector3> position;
    [ReadOnly]
    public NativeArray<Vector3> gravitationalforces;

    public NativeArray<Vector3> gradiant;


    public void Execute()
    {
        float gravityPotentialEnergy = 0.0f;
        for (int index = 0; index < gravitationalforces.Length; index++)
        {
            Vector3 gravitationalforce = gravitationalforces[index];
            int i = (int)gravitationalforce.x;
            int j = (int)gravitationalforce.y;
            float G = gravitationalforce.z;

            float l = Vector3.Distance(position[i], position[j]);
            Vector3 n = (position[i] - position[j]).normalized;
            float m1 = mass[i];
            float m2 = mass[j];

            float forceVal = (m1 * m2 * G) / (l * l);
            gravityPotentialEnergy += -(m1 * m2 * G) / l;

            gradiant[i] += (forceVal * n);
            gradiant[j] += (-forceVal * n);
        }
    }
}