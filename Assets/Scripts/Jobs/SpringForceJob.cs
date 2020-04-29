using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System.Collections.Generic;

public struct SpringForceJob
{
    [ReadOnly]
    public NativeArray<Vector3> position;
    [ReadOnly]
    public NativeArray<Vector3> velocity;
    [ReadOnly]
    public NativeArray<Vector4> springForces;
    [ReadOnly]
    public List<Vector2Int> edges;

    public NativeArray<Vector3> gradiant;

    public void Execute()
    {
        float springPotentialEnergy = 0.0f;
        for (int index = 0; index < springForces.Length; index++)
        {
            Vector4 springforceInfo = springForces[index];
            int edgeIndex = (int)springforceInfo.x;
            int i = edges[edgeIndex].x;
            int j = edges[edgeIndex].y;
            float k = springforceInfo.y;
            float l0 = springforceInfo.z;
            float b = springforceInfo.w;

            Vector3 xi = position[i];
            Vector3 xj = position[j];

            float l = Vector3.Distance(position[i], position[j]);
            Vector3 n = (xi - xj).normalized;

            springPotentialEnergy += 0.5f * k * (l - l0) * (l - l0);
            gradiant[i] += (k * (l - l0) * n);
            gradiant[j] += (-k * (l - l0) * n);

            Vector3 vi = velocity[i];
            Vector3 vj = velocity[j];
           
            gradiant[i] += (-Vector3.Dot(-b * n, vi - vj) * n);
            gradiant[j] += (-Vector3.Dot(b * n, vi - vj) * n);
        }
    }
}