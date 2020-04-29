using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

public struct PotentialEnergyJob
{
    //input
    public NativeArray<float> mass;
    public NativeArray<Vector3> position;
    public Vector3 gravity;
    public NativeArray<Vector3> velocity;

    //output
    public float Execute(float totalSpringEnergy, float totalGravityEnergy)
    {
        float result = totalSpringEnergy + totalGravityEnergy;

        for(int i = 0; i < position.Length; i++)
        {
            // simple gravity potential energy
            result += (-mass[i] * Vector3.Dot(gravity, position[i]));
            // kinetic energy
            result += 0.5f * mass[i] * Vector3.Dot(velocity[i], velocity[i]);
        }

        return result;
    }
}
