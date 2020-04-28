using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
using System.Xml;

[RequireComponent(typeof(PhysicalScene))]
public class EulerRunner : MonoBehaviour
{
    PhysicalScene scene;
    ExplicitEulerIntegrator explicitEuler = new ExplicitEulerIntegrator();

    void Awake()
    {
        scene = GetComponent<PhysicalScene>();
        scene.Load();
    }

    void OnDestroy()
    {
        scene.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        explicitEuler.StepOneFrame(scene);
        scene.Frame();
    }

}
