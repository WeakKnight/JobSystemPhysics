using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Collections;
using System.Xml;

public class PhysicalScene : MonoBehaviour
{
    public TextAsset sceneFile;
    public GameObject particlePrefab;
    public GameObject linePrefab;

    [System.NonSerialized]
    public string integratorType = "";
    [System.NonSerialized]
    public int objectCount = 0;
    [System.NonSerialized]
    public float dt = 0;

    public NativeArray<Vector3> m_Velocities;
    public NativeArray<Vector3> m_Positions;
    public NativeArray<bool> m_fixes;
    public NativeArray<float> m_masses;
    public NativeArray<Vector3> m_gradiants;
    public TransformAccessArray m_TransformsAccessArray;
    // i, j, G
    public NativeArray<Vector3> gravitationalforces;
    // edge, k, l0. b
    public NativeArray<Vector4> springForces;

    [System.NonSerialized]
    public Vector3 m_Gravity;
    [System.NonSerialized]
    public float m_DragDamping = 0.0f;
    [System.NonSerialized]
    public List<Transform> particleList = new List<Transform>();
    [System.NonSerialized]
    public List<LineRenderer> edgeList = new List<LineRenderer>();
    [System.NonSerialized]
    public List<Vector2Int> edgeIndexList = new List<Vector2Int>();

    public void Load()
    {
        Load(sceneFile);
    }

    // Start is called before the first frame update
    public void Load(TextAsset scene)
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

            float radius = 0.5f;
            XmlAttribute radiusAttr = particle.Attributes["radius"];
            if (radiusAttr != null)
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
        if (gravityNodeList.Count > 0)
        {
            XmlNode gravityNode = gravityNodeList[0];
            float gx = float.Parse(gravityNode.Attributes["fx"]?.InnerText);
            float gy = float.Parse(gravityNode.Attributes["fy"]?.InnerText);

            m_Gravity = new Vector3(gx, gy, 0.0f);
        }

        m_TransformsAccessArray = new TransformAccessArray(particleList.ToArray());

        XmlNodeList particleColors = xmlDoc.GetElementsByTagName("particlecolor");
        foreach (XmlNode particleColor in particleColors)
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
        if (backgroundColorNodes.Count > 0)
        {
            XmlNode backgroundColorNode = backgroundColorNodes[0];
            float r = float.Parse(backgroundColorNode.Attributes["r"]?.InnerText);
            float g = float.Parse(backgroundColorNode.Attributes["g"]?.InnerText);
            float b = float.Parse(backgroundColorNode.Attributes["b"]?.InnerText);

            Camera.main.backgroundColor = new Color(r, g, b);
        }

        XmlNodeList edges = xmlDoc.GetElementsByTagName("edge");
        foreach (XmlNode edge in edges)
        {
            int i = int.Parse(edge.Attributes["i"]?.InnerText);
            int j = int.Parse(edge.Attributes["j"]?.InnerText);

            float radius = 0.2f;
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

        // Gravitational Forces
        XmlNodeList gravitationalNodes = xmlDoc.GetElementsByTagName("gravitationalforce");

        List<Vector3> tempG = new List<Vector3>();
        foreach(XmlNode gravitationalNode in gravitationalNodes)
        {
            float G = float.Parse(gravitationalNode.Attributes["G"]?.InnerText);
            int i = int.Parse(gravitationalNode.Attributes["i"]?.InnerText);
            int j = int.Parse(gravitationalNode.Attributes["j"]?.InnerText);

            tempG.Add(new Vector3(i, j, G));
        }

        gravitationalforces = new NativeArray<Vector3>(tempG.ToArray(), Allocator.Persistent);

        // Spring Forces
        XmlNodeList springforceNodes = xmlDoc.GetElementsByTagName("springforce");

        List<Vector4> tempS = new List<Vector4>();
        foreach(XmlNode springforceNode in springforceNodes)
        {
            int edgeIndex = int.Parse(springforceNode.Attributes["edge"]?.InnerText);
            float k = float.Parse(springforceNode.Attributes["k"]?.InnerText);
            float l0 = float.Parse(springforceNode.Attributes["l0"]?.InnerText);
            float b = 0.0f;
            XmlAttribute bAttr = springforceNode.Attributes["b"];
            if(bAttr != null)
            {
                b = float.Parse(bAttr.InnerText);
            }

            tempS.Add(new Vector4(edgeIndex, k, l0, b));
        }
        springForces = new NativeArray<Vector4>(tempS.ToArray(), Allocator.Persistent);

        // Drag Damping
        XmlNodeList dragDampingNodes = xmlDoc.GetElementsByTagName("dragdamping");
        if(dragDampingNodes.Count > 0)
        {
            m_DragDamping = float.Parse(dragDampingNodes[0].Attributes["b"].InnerText);
        }

        // Description
        XmlNodeList descriptions = xmlDoc.GetElementsByTagName("description");
        foreach(XmlNode description in descriptions)
        {
            Debug.Log(description.Attributes["text"]?.InnerText);
        }

        // Frame Rate
        XmlNodeList integrators = xmlDoc.GetElementsByTagName("integrator");
        if(integrators.Count > 0)
        {
            XmlNode integrator = integrators[0];
            dt = float.Parse(integrator.Attributes["dt"]?.InnerText);
            integratorType = integrator.Attributes["type"]?.InnerText;

            Debug.Log("Target Delta Time Is " + dt.ToString());
            Debug.Log("Integrator Type Is " + integratorType);
            Application.targetFrameRate = (int)(1.0f / dt);
        }
    }

    public void Dispose()
    {
        m_Velocities.Dispose();
        m_Positions.Dispose();
        m_TransformsAccessArray.Dispose();
        m_fixes.Dispose();
        m_masses.Dispose();
        m_gradiants.Dispose();
        gravitationalforces.Dispose();
        springForces.Dispose();
    }

    public void Frame()
    {
        for (int i = 0; i < edgeList.Count; i++)
        {
            LineRenderer lineRenderer = edgeList[i];
            Vector2Int vertexIndex = edgeIndexList[i];

            lineRenderer.SetPosition(0, particleList[vertexIndex.x].position);
            lineRenderer.SetPosition(1, particleList[vertexIndex.y].position);
        }
    }
}
