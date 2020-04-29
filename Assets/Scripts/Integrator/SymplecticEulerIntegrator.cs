using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;

public class SymplecticEulerIntegrator : IIntegrator
{
    // By Order
    SimpleGravityJob m_GravityGradiantJob;
    SymplecticEulerJob m_SymplecticEulerJob;
    UpdateJob m_UpdateJob;
    GravitationalForceJob m_GravitationalForceJob;

    JobHandle m_GravityGradiantJobHandle;
    JobHandle m_SymplecticEulerJobHandle;
    JobHandle m_UpdateJobHandle;

    public void StepOneFrame(PhysicalScene scene)
    {
        m_GravitationalForceJob = new GravitationalForceJob
        {
            mass = scene.m_masses,
            position = scene.m_Positions,
            gradiant = scene.m_gradiants,
            gravitationalforces = scene.gravitationalforces
        };

        m_GravityGradiantJob = new SimpleGravityJob
        {
            gravity = scene.m_Gravity,
            gradiant = scene.m_gradiants,
            mass = scene.m_masses,
            fixes = scene.m_fixes,
        };

        m_SymplecticEulerJob = new SymplecticEulerJob
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

        m_GravitationalForceJob.Execute();

        m_GravityGradiantJobHandle = m_GravityGradiantJob.Schedule(scene.objectCount, 64);

        m_SymplecticEulerJobHandle = m_SymplecticEulerJob.Schedule(scene.objectCount, 64, m_GravityGradiantJobHandle);

        m_UpdateJobHandle = m_UpdateJob.Schedule(scene.m_TransformsAccessArray, m_SymplecticEulerJobHandle);

        m_UpdateJobHandle.Complete();
    }
}
