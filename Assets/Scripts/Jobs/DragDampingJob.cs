using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

public struct DragDampingJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<Vector3> velocity;

    public NativeArray<Vector3> gradiant;
    public float b;

    public void Execute(int index)
    {
        Vector3 vi = velocity[index];
        gradiant[index] += (b * vi);
    }
}