using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
using System.Xml;
using System.Xml.Serialization;

public struct GravityGradiantJob : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float> mass;
    [ReadOnly]
    public NativeArray<bool> fixes;

    public NativeArray<Vector3> gradiant;
    public Vector3 gravity;

    public void Execute(int index)
    {
        if(fixes[index])
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

public class ExplicitEuler : MonoBehaviour
{
    public TextAsset scene;
    public GameObject particlePrefab;
    public GameObject linePrefab;

    int objectCount = 0;

    NativeArray<Vector3> m_Velocities;
    NativeArray<Vector3> m_Positions;
    NativeArray<bool> m_fixes;
    NativeArray<float> m_masses;
    NativeArray<Vector3> m_gradiants;
    TransformAccessArray m_TransformsAccessArray;

    Vector3 m_Gravity;

    // By Order
    GravityGradiantJob m_GravityGradiantJob;
    PositionJob m_PositionJob;
    VelocityJob m_VelocityJob;
    UpdateJob m_UpdateJob;

    JobHandle m_GravityGradiantJobHandle;
    JobHandle m_PositionJobHandle;
    JobHandle m_VelocityJobHandle;
    JobHandle m_UpdateJobHandle;

    List<Transform> particleList = new List<Transform>();
    List<LineRenderer> edgeList = new List<LineRenderer>();
    List<Vector2Int> edgeIndexList = new List<Vector2Int>();

    void Awake()
    {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(scene.text);
        XmlNodeList particles = xmlDoc.GetElementsByTagName("particle");
        objectCount = particles.Count;

        m_Velocities = new NativeArray<Vector3>(objectCount, Allocator.Persistent);
        m_Positions = new NativeArray<Vector3>(objectCount, Allocator.Persistent);
        m_fixes = new NativeArray<bool>(objectCount, Allocator.Persistent);
        m_masses = new NativeArray<float>(objectCount, Allocator.Persistent);
        m_gradiants = new NativeArray<Vector3>(objectCount, Allocator.Persistent);

        for (int i = 0; i < particles.Count; i++)
        {
            XmlNode particle = particles[i];
            float px = float.Parse(particle.Attributes["px"]?.InnerText);
            float py = float.Parse(particle.Attributes["py"]?.InnerText);
            float vx = float.Parse(particle.Attributes["vx"]?.InnerText);
            float vy = float.Parse(particle.Attributes["vy"]?.InnerText);

            float mass = float.Parse(particle.Attributes["m"]?.InnerText);

            float radius = 1.0f;
            XmlAttribute radiusAttr = particle.Attributes["radius"];
            if(radiusAttr != null)
            {
                radius = float.Parse(radiusAttr.InnerText);
            }

            bool isFixed = int.Parse(particle.Attributes["fixed"]?.InnerText) == 1;
            
            m_Positions[i] = new Vector3(px, py, 0.0f);
            m_Velocities[i] = new Vector3(vx, vy, 0.0f);
            m_fixes[i] = isFixed;
            m_masses[i] = mass;
            m_gradiants[i] = new Vector3(0.0f, 0.0f, 0.0f);

            GameObject instance = GameObject.Instantiate(particlePrefab, new Vector3(px, py, 0.0f), Quaternion.identity);
            instance.transform.localScale = new Vector3(radius, radius, radius);

            particleList.Add(instance.transform);
        }

        XmlNodeList gravityNodeList = xmlDoc.GetElementsByTagName("simplegravity");
        if(gravityNodeList.Count > 0)
        {
            XmlNode gravityNode = gravityNodeList[0];
            float gx = float.Parse(gravityNode.Attributes["fx"]?.InnerText);
            float gy = float.Parse(gravityNode.Attributes["fy"]?.InnerText);

            m_Gravity = new Vector3(gx, gy, 0.0f);
        }
      
        m_TransformsAccessArray = new TransformAccessArray(particleList.ToArray());

        XmlNodeList particleColors = xmlDoc.GetElementsByTagName("particlecolor");
        foreach(XmlNode particleColor in particleColors)
        {
            float r = float.Parse(particleColor.Attributes["r"]?.InnerText);
            float g = float.Parse(particleColor.Attributes["g"]?.InnerText);
            float b = float.Parse(particleColor.Attributes["b"]?.InnerText);
            int index = int.Parse(particleColor.Attributes["i"]?.InnerText);
            particleList[index].GetComponent<Renderer>().material.color = new Color(r, g, b);
        }

        XmlNodeList particlePaths = xmlDoc.GetElementsByTagName("particlepath");
        foreach (XmlNode particlePath in particlePaths)
        {
            float r = float.Parse(particlePath.Attributes["r"]?.InnerText);
            float g = float.Parse(particlePath.Attributes["g"]?.InnerText);
            float b = float.Parse(particlePath.Attributes["b"]?.InnerText);
            float duration = float.Parse(particlePath.Attributes["duration"]?.InnerText);
            int index = int.Parse(particlePath.Attributes["i"]?.InnerText);

            TrailRenderer trailRenderer = particleList[index].GetComponentInChildren<TrailRenderer>();
            Gradient newGradient = new Gradient();
            GradientAlphaKey gradientAlphaKey0 = new GradientAlphaKey(1.0f, 0.0f);
            GradientColorKey gradientColorKey0 = new GradientColorKey(new Color(r, g, b), 0.0f);
            GradientAlphaKey gradientAlphaKey1 = new GradientAlphaKey(1.0f, 1.0f);
            GradientColorKey gradientColorKey1 = new GradientColorKey(new Color(r, g, b), 1.0f);
            newGradient.alphaKeys = new GradientAlphaKey[2] { gradientAlphaKey0, gradientAlphaKey1 };
            newGradient.colorKeys = new GradientColorKey[2] { gradientColorKey0, gradientColorKey1 };
            trailRenderer.colorGradient = newGradient;
            trailRenderer.time = duration;
        }

        XmlNodeList backgroundColorNodes = xmlDoc.GetElementsByTagName("backgroundcolor");
        if(backgroundColorNodes.Count > 0)
        {
            XmlNode backgroundColorNode = backgroundColorNodes[0];
            float r = float.Parse(backgroundColorNode.Attributes["r"]?.InnerText);
            float g = float.Parse(backgroundColorNode.Attributes["g"]?.InnerText);
            float b = float.Parse(backgroundColorNode.Attributes["b"]?.InnerText);

            Camera.main.backgroundColor = new Color(r, g, b);
        }

        XmlNodeList edges = xmlDoc.GetElementsByTagName("edge");
        foreach(XmlNode edge in edges)
        {
            int i = int.Parse(edge.Attributes["i"]?.InnerText);
            int j = int.Parse(edge.Attributes["j"]?.InnerText);
            
            float radius = 1.0f;
            XmlAttribute radiusAttr = edge.Attributes["radius"];
            if (radiusAttr != null)
            {
                radius = float.Parse(radiusAttr.InnerText);
            }

            GameObject edgeObj = GameObject.Instantiate(linePrefab);
            LineRenderer lineRenderer = edgeObj.GetComponent<LineRenderer>();
            lineRenderer.SetPosition(0, particleList[i].position);
            lineRenderer.SetPosition(1, particleList[j].position);
            lineRenderer.startWidth = radius;
            lineRenderer.endWidth = radius;

            edgeList.Add(lineRenderer);
            edgeIndexList.Add(new Vector2Int(i, j));
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }


    // Update is called once per frame
    void Update()
    {
        m_GravityGradiantJob = new GravityGradiantJob
        {
            gravity = m_Gravity,
            gradiant = m_gradiants,
            mass = m_masses,
            fixes = m_fixes,
        };

        m_PositionJob = new PositionJob
        {
            deltaTime = Time.deltaTime,
            velocity = m_Velocities,
            fixes = m_fixes,
            position = m_Positions,
        };

        m_VelocityJob = new VelocityJob
        {
            gradiant = m_gradiants,
            mass = m_masses,
            fixes = m_fixes,
            velocity = m_Velocities,
            deltaTime = Time.deltaTime,
        };

        m_UpdateJob = new UpdateJob
        {
            position = m_Positions,
            gradiant = m_gradiants,
        };

        m_GravityGradiantJobHandle = m_GravityGradiantJob.Schedule(objectCount, 64);
        
        m_PositionJobHandle = m_PositionJob.Schedule(objectCount, 64, m_GravityGradiantJobHandle);
        
        m_VelocityJobHandle = m_VelocityJob.Schedule(objectCount, 64, m_PositionJobHandle);

        m_UpdateJobHandle = m_UpdateJob.Schedule(m_TransformsAccessArray, m_VelocityJobHandle);

        m_UpdateJobHandle.Complete();

        for(int i = 0; i < edgeList.Count; i++)
        {
            LineRenderer lineRenderer = edgeList[i];
            Vector2Int vertexIndex = edgeIndexList[i];

            lineRenderer.SetPosition(0, particleList[vertexIndex.x].position);
            lineRenderer.SetPosition(1, particleList[vertexIndex.y].position);
        }
    }

    void LateUpdate()
    {
    }

    void OnDestroy()
    {
        m_Velocities.Dispose();
        m_Positions.Dispose();
        m_TransformsAccessArray.Dispose();
        m_fixes.Dispose();
        m_masses.Dispose();
        m_gradiants.Dispose();
    }
}
