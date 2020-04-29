using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

public struct SimpleGravityJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float> mass;
    [ReadOnly]
    public NativeArray<bool> fixes;

    public NativeArray<Vector3> gradiant;
    public Vector3 gravity;

    public void Execute(int index)
    {
        if (fixes[index])
        {
            return;
        }

        gradiant[index] -= (mass[index] * gravity);
    }
}