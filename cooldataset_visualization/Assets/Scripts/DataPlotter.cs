using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Import the UI namespace
using TMPro;


public class DataPlotter : MonoBehaviour
{
    public TextAsset csvFile; // CSV file
    public GameObject spherePrefab; // Sphere prefab
    public GameObject nameTagPrefab; // Prefab for the name tag
    public Canvas mainCanvas; // Reference to the main Canvas
    public float graphWidth = 3f;  // Width of the graph
    public float graphHeight = 3f; // Height of the graph
    public float sphereSpacing = 0.5f; // Spacing between spheres

    public GameObject axisPrefab; // Prefab para el eje
    public float axisLength = 5f;  // Longitud de los ejes


    private Dictionary<string, List<DataPoint>> countryData = new Dictionary<string, List<DataPoint>>();
    private Dictionary<string, GameObject> countrySpheres = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> countryNameTags = new Dictionary<string, GameObject>(); // Dictionary to hold name tags
    private int currentPeriod = 0;
    private bool isTransitioning = false;

    private float minGDP, maxGDP, minCO2, maxCO2;

    void Start()
    {
        LoadDataFromCSV();
        NormalizeData();
        CreateSpheresForFirstPeriod();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isTransitioning)
        {
            currentPeriod++;
            if (currentPeriod < 3)  // Assuming there are 3 periods in total
            {
                StartCoroutine(TransitionToNextPeriod());
            }
        }
    }

    void LoadDataFromCSV()
    {
        string[] data = csvFile.text.Split(new char[] { '\n' });
        for (int i = 1; i < data.Length; i++) // Start at 1 to skip the header line
        {
            if (string.IsNullOrEmpty(data[i])) continue;

            string[] row = data[i].Split(',');

            if (row.Length < 5)  // Ensure there are enough columns
            {
                Debug.LogWarning($"Row {i} is improperly formatted: {data[i]}");
                continue;
            }

            string country = row[0];
            float gdp = float.Parse(row[1]);
            float co2 = float.Parse(row[2]);
            int startYear = int.Parse(row[3]);
            int endYear = int.Parse(row[4]);

            DataPoint dp = new DataPoint(gdp, co2, GetColorForCountry(country), startYear, endYear);

            if (!countryData.ContainsKey(country))
            {
                countryData[country] = new List<DataPoint>();
            }
            countryData[country].Add(dp);
        }

        // Debug log to check if countries were loaded
        Debug.Log($"Loaded countries: {string.Join(", ", countryData.Keys)}");
    }

    void NormalizeData()
    {
        Debug.Log("Normalize data...");
        minGDP = float.MaxValue; maxGDP = float.MinValue;
        minCO2 = float.MaxValue; maxCO2 = float.MinValue;

        foreach (var country in countryData.Keys)
        {
            foreach (var dp in countryData[country])
            {
                if (dp.gdp < minGDP) minGDP = dp.gdp;
                if (dp.gdp > maxGDP) maxGDP = dp.gdp;
                if (dp.co2 < minCO2) minCO2 = dp.co2;
                if (dp.co2 > maxCO2) maxCO2 = dp.co2;
            }
        }
    }

    float Normalize(float value, float min, float max)
    {
        return (value - min) / (max - min);
    }

void CreateSpheresForFirstPeriod()
{
    Vector3 plotCenter = this.transform.position;
    Quaternion plotRotation = this.transform.rotation;

    foreach (var country in countryData.Keys)
    {
        DataPoint dp = countryData[country][0];  // First period

        // Normalized positions
        float normalizedGDP = Normalize(dp.gdp, minGDP, maxGDP);
        float normalizedCO2 = Normalize(dp.co2, minCO2, maxCO2);

        // Create the local position based on normalized values
        Vector3 localPosition = new Vector3(normalizedGDP * graphWidth, normalizedCO2 * graphHeight, 0);
        Vector3 worldPosition = plotCenter + this.transform.TransformDirection(localPosition);

        // Instantiate the sphere and store it
        GameObject sphere = Instantiate(spherePrefab, worldPosition, plotRotation);
        sphere.GetComponent<Renderer>().material.color = dp.pointColor;
        sphere.transform.localScale = Vector3.one * 0.5f;
        countrySpheres[country] = sphere;

        // Create the name tag
        GameObject nameTag = Instantiate(nameTagPrefab);
        nameTag.transform.SetParent(mainCanvas.transform, false); // Set it as a child of the Canvas

        // Set the text of the name tag
        TMP_Text textComponent = nameTag.GetComponent<TMP_Text>();
        if (textComponent != null)
        {
            textComponent.text = country; // Set the country name
        }

        // Calculate name tag position based on the sphere's position
        Vector3 nameTagOffset = new Vector3(0.6f, 0.6f, 0); // Adjust the offset as necessary
        Vector3 nameTagPosition = worldPosition + nameTagOffset;

        // Set the name tag position
        nameTag.transform.position = nameTagPosition;

        // Store the name tag in the dictionary
        countryNameTags[country] = nameTag;
    }
}




IEnumerator TransitionToNextPeriod()
{
    isTransitioning = true;

    float elapsedTime = 0;
    float transitionDuration = 2f;  // Duración de la transición

    // Guardar las posiciones iniciales de las esferas
    Dictionary<string, Vector3> initialPositions = new Dictionary<string, Vector3>();

    foreach (var country in countryData.Keys)
    {
        initialPositions[country] = countrySpheres[country].transform.position;
    }

    while (elapsedTime < transitionDuration)
    {
        elapsedTime += Time.deltaTime;
        float t = elapsedTime / transitionDuration;

        foreach (var country in countryData.Keys)
        {
            DataPoint dp = countryData[country][currentPeriod];

            // Calcular posiciones normalizadas basadas en los datos del período actual
            float normalizedGDP = Normalize(dp.gdp, minGDP, maxGDP);
            float normalizedCO2 = Normalize(dp.co2, minCO2, maxCO2);

            // Crear posición objetivo en función de los valores normalizados
            Vector3 plotCenter = this.transform.position;
            Vector3 localPosition = new Vector3(normalizedGDP * graphWidth, normalizedCO2 * graphHeight, 0);
            Vector3 targetPosition = plotCenter + this.transform.TransformDirection(localPosition);

            // Interpolar la posición de la esfera y el nombre simultáneamente
            Vector3 newSpherePosition = Vector3.Lerp(initialPositions[country], targetPosition, t);
            countrySpheres[country].transform.position = newSpherePosition;

            // Asegúrate de que el texto se mueva a la misma posición que la esfera
            countryNameTags[country].transform.position = newSpherePosition + new Vector3(0, 0.5f, 0); // Ajusta la altura del texto
        }

        yield return null;
    }

    // Asegurarse que al final de la transición, las posiciones sean correctas
    foreach (var country in countryData.Keys)
    {
        DataPoint dp = countryData[country][currentPeriod];
        float normalizedGDP = Normalize(dp.gdp, minGDP, maxGDP);
        float normalizedCO2 = Normalize(dp.co2, minCO2, maxCO2);

        Vector3 plotCenter = this.transform.position;
        Vector3 localPosition = new Vector3(normalizedGDP * graphWidth, normalizedCO2 * graphHeight, 0);
        Vector3 finalTargetPosition = plotCenter + this.transform.TransformDirection(localPosition);

        countrySpheres[country].transform.position = finalTargetPosition;
        countryNameTags[country].transform.position = finalTargetPosition + new Vector3(0, 0.5f, 0);
    }

    isTransitioning = false;
}




    Color GetColorForCountry(string country)
    {
        Dictionary<string, Color> countryColors = new Dictionary<string, Color>()
        {
            { "Germany", Color.red },
            { "France", Color.green },
            { "Japan", Color.blue },
            { "USA", Color.yellow },
            { "Canada", Color.cyan },
            { "Russia", Color.magenta },
            { "China", new Color(1f, 0.5f, 0f) }, // Orange
            { "India", new Color(0.5f, 0f, 0.5f) }, // Purple
            { "Ukraine", new Color(0.5f, 0.5f, 0f) }, // Olive
            { "United Kingdom", new Color(0.5f, 0.5f, 1f) }  // Light Blue
        };

        return countryColors.ContainsKey(country) ? countryColors[country] : Color.white;
    }

    public class DataPoint
    {
        public float gdp;
        public float co2;
        public Color pointColor;
        public int startYear;
        public int endYear;

        public DataPoint(float gdp, float co2, Color pointColor, int startYear, int endYear)
        {
            this.gdp = gdp;
            this.co2 = co2;
            this.pointColor = pointColor;
            this.startYear = startYear;
            this.endYear = endYear;
        }
    }
}
