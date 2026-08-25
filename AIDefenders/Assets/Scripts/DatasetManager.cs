using UnityEngine;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class DatasetAnimal
{
    public Target_Base animal;          // 랜덤화에 쓸 원본(씬에 배치)
    public int classID;                 // YOLO 클래스 ID
    public string label = "unknown";    // 클래스 이름(로그/구분용)
    public string[] animationStates;    // 이 동물의 애니메이션 상태
}

public class DatasetManager : MonoBehaviour
{
    [Header("Capture Settings")]
    public Camera datasetCamera;
    public RenderTexture renderTexture;
    [Range(1, 100)]
    public int jpegQuality = 70;        // CCTV EncodeToJPG(70)과 맞춤

    [Header("Animals")]
    public DatasetAnimal[] animalPool;  // 랜덤화할 대상 리스트
    public List<Target_Base> captureTargets = new List<Target_Base>();  // 실제 촬영 대상
    public int minCaptureCount = 1;
    public int maxCaptureCount = 3;
    public float minSpacing = 2f;
    public float targetY = 0.006f;
    public Vector2 targetXZRange = new Vector2(-5f, 5f);

    [Header("Camera")]
    public Vector2 cameraXZRange = new Vector2(-8f, 8f);
    public Vector2 cameraHeightRange = new Vector2(4f, 10f);

    [Header("Lighting")]
    public Light directionalLight;

    [Header("Dataset")]
    public int sampleCount = 100;
    public bool writeDatasetYaml = true;

    readonly List<int> captureClassIds = new List<int>();
    readonly List<string[]> captureAnimationStates = new List<string[]>();
    Texture2D screenTexture;

    void Start()
    {
        if (!datasetCamera) return;
        if (!renderTexture) return;
        if (animalPool == null || animalPool.Length == 0) return;

        HidePoolAnimals();

        RenderSettings.fogMode = FogMode.Exponential;

        screenTexture = new Texture2D(
            renderTexture.width,
            renderTexture.height,
            TextureFormat.RGB24,
            false
        );

        StartCoroutine(GenerateDataset());
    }

    IEnumerator GenerateDataset()
    {
        CreateDirectories();
        WriteDatasetYaml();

        for (int i = 0; i < sampleCount; i++)
        {
            RandomizeScene();
            RandomizeCaptureAnimations();

            yield return null;
            yield return new WaitForEndOfFrame();

            CaptureAndSave(i);
        }

        ClearCaptureTargets();
        Debug.Log("Dataset generation complete!");
    }

    string GetDatasetRoot()
    {
        return Application.dataPath + "/Dataset";
    }

    void CreateDirectories()
    {
        string[] splits = { "train", "val", "test" };

        foreach (string split in splits)
        {
            string imagePath = GetDatasetRoot() + "/images/" + split;
            string labelPath = GetDatasetRoot() + "/labels/" + split;

            if (!Directory.Exists(imagePath))
                Directory.CreateDirectory(imagePath);

            if (!Directory.Exists(labelPath))
                Directory.CreateDirectory(labelPath);
        }
    }

    void WriteDatasetYaml()
    {
        if (!writeDatasetYaml)
            return;

        Dictionary<int, string> names = BuildClassNames();
        if (names.Count == 0)
        {
            Debug.LogWarning("animalPool에 유효한 classID/label이 없어 dataset.yaml을 만들지 않습니다.");
            return;
        }

        int maxId = 0;
        foreach (int classId in names.Keys)
        {
            if (classId > maxId)
                maxId = classId;
        }

        int classCount = maxId + 1;
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("path: .");
        builder.AppendLine("train: images/train");
        builder.AppendLine("val: images/val");
        builder.AppendLine("test: images/test");
        builder.AppendLine();
        builder.AppendLine("nc: " + classCount);
        builder.AppendLine("names:");

        for (int classId = 0; classId <= maxId; classId++)
        {
            string label;
            if (!names.TryGetValue(classId, out label) || string.IsNullOrWhiteSpace(label))
                label = "class_" + classId;

            builder.Append("  ");
            builder.Append(classId);
            builder.Append(": ");
            builder.AppendLine(FormatYamlScalar(label));
        }

        string yamlPath = GetDatasetRoot() + "/dataset.yaml";
        File.WriteAllText(yamlPath, builder.ToString(), new UTF8Encoding(false));
        Debug.Log("Wrote dataset.yaml (" + classCount + " classes): " + yamlPath);
    }

    Dictionary<int, string> BuildClassNames()
    {
        Dictionary<int, string> names = new Dictionary<int, string>();

        if (animalPool == null)
            return names;

        foreach (DatasetAnimal entry in animalPool)
        {
            if (entry == null)
                continue;

            if (entry.classID < 0)
                continue;

            string label = string.IsNullOrWhiteSpace(entry.label)
                ? "class_" + entry.classID
                : entry.label.Trim();

            if (!names.ContainsKey(entry.classID))
                names.Add(entry.classID, label);
        }

        return names;
    }

    string FormatYamlScalar(string value)
    {
        bool needsQuotes =
            value.IndexOfAny(new[] { ':', '#', ',', '[', ']', '{', '}', '&', '*', '!', '|', '>', '\'', '"', '%' }) >= 0
            || value.Contains(" ");

        if (!needsQuotes)
            return value;

        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    void HidePoolAnimals()
    {
        foreach (DatasetAnimal entry in animalPool)
        {
            if (entry == null || entry.animal == null)
                continue;

            entry.animal.gameObject.SetActive(false);
        }
    }

    void RandomizeScene()
    {
        SpawnCaptureTargets();
        RandomizeCamera();
        RandomizeLighting();
        RandomizeFog();
    }

    void SpawnCaptureTargets()
    {
        ClearCaptureTargets();

        List<DatasetAnimal> validPool = new List<DatasetAnimal>();
        foreach (DatasetAnimal entry in animalPool)
        {
            if (entry != null && entry.animal != null)
                validPool.Add(entry);
        }

        if (validPool.Count == 0)
            return;

        int minCount = Mathf.Max(1, minCaptureCount);
        int maxCount = Mathf.Max(minCount, maxCaptureCount);
        int spawnCount = Random.Range(minCount, maxCount + 1);

        for (int i = 0; i < spawnCount; i++)
        {
            DatasetAnimal source = validPool[Random.Range(0, validPool.Count)];
            Target_Base clone = Instantiate(source.animal);
            clone.isSample = true;
            clone.gameObject.SetActive(true);

            if (clone.rigidbody == null)
                clone.rigidbody = clone.GetComponent<Rigidbody>();

            if (clone.rigidbody != null)
            {
                clone.rigidbody.useGravity = false;
                clone.rigidbody.linearVelocity = Vector3.zero;
                clone.rigidbody.angularVelocity = Vector3.zero;
                clone.rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            }

            clone.transform.position = FindSpawnPosition();
            clone.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            captureTargets.Add(clone);
            captureClassIds.Add(source.classID);
            captureAnimationStates.Add(source.animationStates);
        }
    }

    Vector3 FindSpawnPosition()
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            Vector3 candidate = new Vector3(
                Random.Range(targetXZRange.x, targetXZRange.y),
                targetY,
                Random.Range(targetXZRange.x, targetXZRange.y)
            );

            bool farEnough = true;
            foreach (Target_Base existing in captureTargets)
            {
                if (existing == null)
                    continue;

                if (Vector3.Distance(existing.transform.position, candidate) < minSpacing)
                {
                    farEnough = false;
                    break;
                }
            }

            if (farEnough)
                return candidate;
        }

        return new Vector3(
            Random.Range(targetXZRange.x, targetXZRange.y),
            targetY,
            Random.Range(targetXZRange.x, targetXZRange.y)
        );
    }

    void RandomizeCamera()
    {
        datasetCamera.transform.position = new Vector3(
            Random.Range(cameraXZRange.x, cameraXZRange.y),
            Random.Range(cameraHeightRange.x, cameraHeightRange.y),
            Random.Range(cameraXZRange.x, cameraXZRange.y)
        );

        Vector3 lookPoint = Vector3.zero;
        int count = 0;

        foreach (Target_Base target in captureTargets)
        {
            if (target == null)
                continue;

            lookPoint += target.transform.position;
            count++;
        }

        if (count > 0)
            lookPoint /= count;

        datasetCamera.transform.LookAt(lookPoint);
    }

    void RandomizeLighting()
    {
        if (directionalLight == null)
            return;

        float sunPitch = Random.Range(15f, 80f);
        float sunYaw = Random.Range(0f, 360f);

        directionalLight.transform.rotation = Quaternion.Euler(sunPitch, sunYaw, 0f);
        directionalLight.intensity = Random.Range(1.2f, 2f);
        directionalLight.color = new Color(
            Random.Range(0.8f, 1f),
            Random.Range(0.8f, 1f),
            Random.Range(0.8f, 1f)
        );
    }

    void RandomizeFog()
    {
        RenderSettings.fog = Random.value > 0.5f;

        RenderSettings.fogColor = new Color(
            Random.Range(0.7f, 1f),
            Random.Range(0.7f, 1f),
            Random.Range(0.7f, 1f)
        );

        RenderSettings.fogDensity = Random.Range(0.002f, 0.02f);
    }

    void RandomizeCaptureAnimations()
    {
        for (int i = 0; i < captureTargets.Count; i++)
        {
            Target_Base target = captureTargets[i];
            if (target == null || target.animator == null)
                continue;

            string[] states = captureAnimationStates[i];
            if (states == null || states.Length == 0)
                continue;

            string state = states[Random.Range(0, states.Length)];
            target.animator.Play(state, 0, Random.value);
        }
    }

    void ClearCaptureTargets()
    {
        foreach (Target_Base target in captureTargets)
        {
            if (target != null)
                Destroy(target.gameObject);
        }

        captureTargets.Clear();
        captureClassIds.Clear();
        captureAnimationStates.Clear();
    }

    void CaptureAndSave(int index)
    {
        if (captureTargets.Count == 0)
            return;

        string split = GetDatasetSplit();

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;

        datasetCamera.Render();

        screenTexture.ReadPixels(
            new Rect(0, 0, renderTexture.width, renderTexture.height),
            0,
            0
        );
        screenTexture.Apply();

        RenderTexture.active = currentRT;

        byte[] bytes = screenTexture.EncodeToJPG(jpegQuality);

        string imageName = "sample_" + index;
        string imagePath =
            GetDatasetRoot() +
            "/images/" +
            split +
            "/" +
            imageName +
            ".jpg";

        File.WriteAllBytes(imagePath, bytes);
        SaveLabels(imageName, split);
    }

    string GetDatasetSplit()
    {
        float rand = Random.value;

        if (rand < 0.7f)
            return "train";

        if (rand < 0.9f)
            return "val";

        return "test";
    }

    void SaveLabels(string imageName, string split)
    {
        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < captureTargets.Count; i++)
        {
            string line = GenerateYOLOBBox(captureTargets[i], captureClassIds[i]);
            if (string.IsNullOrEmpty(line))
                continue;

            if (builder.Length > 0)
                builder.Append('\n');

            builder.Append(line);
        }

        string labelPath =
            GetDatasetRoot() +
            "/labels/" +
            split +
            "/" +
            imageName +
            ".txt";

        File.WriteAllText(labelPath, builder.ToString());
    }

    string GenerateYOLOBBox(Target_Base target, int classID)
    {
        if (target == null)
            return null;

        Renderer renderer = target.getRenderer();
        if (renderer == null)
            return null;

        Bounds bounds = renderer.bounds;

        Vector3[] corners = new Vector3[8];
        corners[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
        corners[1] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[3] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        corners[4] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        corners[6] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        corners[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        int visibleCorners = 0;

        foreach (Vector3 corner in corners)
        {
            Vector3 screenPoint = datasetCamera.WorldToScreenPoint(corner);
            if (screenPoint.z < 0)
                continue;

            visibleCorners++;
            min.x = Mathf.Min(min.x, screenPoint.x);
            min.y = Mathf.Min(min.y, screenPoint.y);
            max.x = Mathf.Max(max.x, screenPoint.x);
            max.y = Mathf.Max(max.y, screenPoint.y);
        }

        if (visibleCorners == 0)
            return null;

        float width = renderTexture.width;
        float height = renderTexture.height;

        min.x = Mathf.Clamp(min.x, 0f, width);
        min.y = Mathf.Clamp(min.y, 0f, height);
        max.x = Mathf.Clamp(max.x, 0f, width);
        max.y = Mathf.Clamp(max.y, 0f, height);

        float boxWidth = (max.x - min.x) / width;
        float boxHeight = (max.y - min.y) / height;

        if (boxWidth <= 0.001f || boxHeight <= 0.001f)
            return null;

        float xCenter = ((min.x + max.x) / 2f) / width;
        float yCenter = ((min.y + max.y) / 2f) / height;
        yCenter = 1f - yCenter;

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} {1:0.######} {2:0.######} {3:0.######} {4:0.######}",
            classID,
            xCenter,
            yCenter,
            boxWidth,
            boxHeight
        );
    }
}
