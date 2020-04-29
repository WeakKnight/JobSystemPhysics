using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

public struct SymplecticEulerJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<bool> fixes;
    [ReadOnly]
    public NativeArray<float> mass;
    [ReadOnly]
    public NativeArray<Vector3> gradiant;

    public NativeArray<Vector3> position;
    public NativeArray<Vector3> velocity;

    public float deltaTime;

    public void Execute(int index)
    {
        if (fixes[index])
        {
            return;
        }

        Vector3 force = -gradiant[index];
        velocity[index] = velocity[index] + (deltaTime * force / mass[index]);
        position[index] = position[index] + (velocity[index] * deltaTime);
    }
}