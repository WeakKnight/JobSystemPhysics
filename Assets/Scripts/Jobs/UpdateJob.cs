using UnityEngine;
using UnityEngine.Jobs;
using Unity.Collections;

public struct UpdateJob : IJobParallelForTransform
{
    [ReadOnly]
    public NativeArray<Vector3> position;
    public NativeArray<Vector3> gradiant;

    public void Execute(int index, TransformAccess transform)
    {
        transform.position = position[index];
        // Refresh Gradiant To Zero
        gradiant[index] = new Vector3(0.0f, 0.0f, 0.0f);
    }
}