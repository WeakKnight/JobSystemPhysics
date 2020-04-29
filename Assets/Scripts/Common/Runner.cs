using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
using System.Xml;

[RequireComponent(typeof(PhysicalScene))]
public class Runner : MonoBehaviour
{
    PhysicalScene scene;
    IIntegrator integrator;

    void Awake()
    {
        scene = GetComponent<PhysicalScene>();
        scene.Load();

        if (scene.integratorType == "explicit-euler")
        {
            integrator = new ExplicitEulerIntegrator();
        }
        else if (scene.integratorType == "symplectic-euler")
        {
            integrator = new SymplecticEulerIntegrator();
        }
        else
        {
            Debug.Assert(false, "Can Not Find Any Suitable Integrator.");
        }
    }

    void OnDestroy()
    {
        scene.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        integrator.StepOneFrame(scene);
        scene.Frame();
    }

}
