using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
using System.Xml;

public struct SimpleGravityGradiantJob : IJobParallelFor
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

public struct PositionJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<Vector3> velocity;
    [ReadOnly]
    public NativeArray<bool> fixes;

    public NativeArray<Vector3> position;

    public float deltaTime;

    public void Execute(int index)
    {
        if (fixes[index])
        {
            return;
        }

        position[index] = position[index] + velocity[index] * deltaTime;
    }
}

public struct VelocityJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<Vector3> gradiant;
    [ReadOnly]
    public NativeArray<float> mass;
    [ReadOnly]
    public NativeArray<bool> fixes;

    public NativeArray<Vector3> velocity;
    public float deltaTime;

    public void Execute(int index)
    {
        if (fixes[index])
        {
            return;
        }

        Vector3 force = -gradiant[index];
        velocity[index] = velocity[index] + deltaTime * force / mass[index];
    }
}

public struct UpdateJob : IJobParallelForTransform
{
    [ReadOnly]
    public NativeArray<Vector3> position;
    public NativeArray<Vector3> gradiant;
    public void Execute(int index, TransformAccess transform)
    {
        transform.position = position[index];
        gradiant[index] = new Vector3(0.0f, 0.0f, 0.0f);
    }
}

public class ExplicitEulerIntegrator
{
    // By Order
    SimpleGravityGradiantJob m_GravityGradiantJob;
    PositionJob m_PositionJob;
    VelocityJob m_VelocityJob;
    UpdateJob m_UpdateJob;

    JobHandle m_GravityGradiantJobHandle;
    JobHandle m_PositionJobHandle;
    JobHandle m_VelocityJobHandle;
    JobHandle m_UpdateJobHandle;

    public void StepOneFrame(PhysicalScene scene)
    {
        m_GravityGradiantJob = new SimpleGravityGradiantJob
        {
            gravity = scene.m_Gravity,
            gradiant = scene.m_gradiants,
            mass = scene.m_masses,
            fixes = scene.m_fixes,
        };

        m_PositionJob = new PositionJob
        {
            deltaTime = Time.deltaTime,
            velocity = scene.m_Velocities,
            fixes = scene.m_fixes,
            position = scene.m_Positions,
        };

        m_VelocityJob = new VelocityJob
        {
            gradiant = scene.m_gradiants,
            mass = scene.m_masses,
            fixes = scene.m_fixes,
            velocity = scene.m_Velocities,
            deltaTime = Time.deltaTime,
        };

        m_UpdateJob = new UpdateJob
        {
            position = scene.m_Positions,
            gradiant = scene.m_gradiants,
        };

        m_GravityGradiantJobHandle = m_GravityGradiantJob.Schedule(scene.objectCount, 64);

        m_PositionJobHandle = m_PositionJob.Schedule(scene.objectCount, 64, m_GravityGradiantJobHandle);

        m_VelocityJobHandle = m_VelocityJob.Schedule(scene.objectCount, 64, m_PositionJobHandle);

        m_UpdateJobHandle = m_UpdateJob.Schedule(scene.m_TransformsAccessArray, m_VelocityJobHandle);

        m_UpdateJobHandle.Complete();
    }
}
