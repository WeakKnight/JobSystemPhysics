using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;

public class ExplicitEulerIntegrator: IIntegrator
{
    // By Order
    SimpleGravityJob m_GravityGradiantJob;
    ExplicitEulerJob m_ExplicitEulerJob;
    UpdateJob m_UpdateJob;

    JobHandle m_GravityGradiantJobHandle;
    JobHandle m_ExplicitEulerJobHandle;
    JobHandle m_UpdateJobHandle;

    public void StepOneFrame(PhysicalScene scene)
    {
        m_GravityGradiantJob = new SimpleGravityJob
        {
            gravity = scene.m_Gravity,
            gradiant = scene.m_gradiants,
            mass = scene.m_masses,
            fixes = scene.m_fixes,
        };

        m_ExplicitEulerJob = new ExplicitEulerJob
        {
            deltaTime = Time.deltaTime,
            velocity = scene.m_Velocities,
            fixes = scene.m_fixes,
            position = scene.m_Positions,
            gradiant = scene.m_gradiants,
            mass = scene.m_masses,
        };

        m_UpdateJob = new UpdateJob
        {
            position = scene.m_Positions,
            gradiant = scene.m_gradiants,
        };

        m_GravityGradiantJobHandle = m_GravityGradiantJob.Schedule(scene.objectCount, 64);

        m_ExplicitEulerJobHandle = m_ExplicitEulerJob.Schedule(scene.objectCount, 64, m_GravityGradiantJobHandle);

        m_UpdateJobHandle = m_UpdateJob.Schedule(scene.m_TransformsAccessArray, m_ExplicitEulerJobHandle);

        m_UpdateJobHandle.Complete();
    }
}
