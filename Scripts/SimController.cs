using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SimController : MonoBehaviour
{
    public GameObject cubePrefab;
    public GameObject emptyPrefab;

    public float tolerance = 0.1f; // The tolerance between cubes
    public Vector3Int structureSize;
    public int emptyProbability; // Maximum number of empty cubes in a column

    public Dictionary<Vector3Int, GameObject> positionToCubeMap = new Dictionary<Vector3Int, GameObject>();
    public Dictionary<Vector3Int, GameObject> positionToEmptyMap = new Dictionary<Vector3Int, GameObject>();

    // Snapshots of the initial states
    private Dictionary<Vector3Int, GameObject> initialPositionToCubeMap;
    private Dictionary<Vector3Int, GameObject> initialPositionToEmptyMap;

    // Method to initialize the prismatic structure
    public void InitPrismaticStructure(int a, int b, int c)
    {
        if (cubePrefab == null || emptyPrefab == null)
        {
            Debug.LogError("Prefabs are not assigned!");
            return;
        }

        // Create an empty GameObject to hold all cubes
        GameObject cubesParent = new GameObject("BoxContainer");

        // Calculate the size of the cube including tolerance
        Renderer cubeRenderer = cubePrefab.GetComponent<Renderer>();
        if (cubeRenderer == null)
        {
            Debug.LogError("Cube prefab does not have a Renderer component!");
            return;
        }

        Vector3 cubeSize = cubeRenderer.bounds.size;
        Vector3 interval = cubeSize + new Vector3(tolerance, tolerance, tolerance);

        // Instantiate the cubes and set them as children of cubesParent
        for (int x = 0; x < a; x++)
        {
            for (int z = 0; z < c; z++)
            {
                // Determine the number of filled cubes in this column
                int filledCubesCount = b - Random.Range(0, emptyProbability + 1);

                for (int y = 0; y < b; y++)
                {
                    GameObject newCube;
                    Vector3 position = new Vector3(x * interval.x, y * interval.y, z * interval.z);
                    Vector3Int positionKey = new Vector3Int(x, y, z);

                    // If we haven't reached the limit of filled cubes, instantiate an empty prefab
                    if (y >= filledCubesCount)
                    {
                        newCube = Instantiate(emptyPrefab, position, Quaternion.identity);
                        positionToEmptyMap.Add(positionKey, newCube);
                    }
                    else
                    {
                        newCube = Instantiate(cubePrefab, position, Quaternion.identity);
                        positionToCubeMap.Add(positionKey, newCube);
                    }

                    newCube.transform.parent = cubesParent.transform; // Set the parent of the new cube
                }
            }
        }

        // Take snapshots after initialization
        initialPositionToCubeMap = new Dictionary<Vector3Int, GameObject>(positionToCubeMap);
        initialPositionToEmptyMap = new Dictionary<Vector3Int, GameObject>(positionToEmptyMap);
    }

    // Reset method
    public void ResetPosition()
    {
        // Reset the position maps to their initial states
        positionToCubeMap = new Dictionary<Vector3Int, GameObject>(initialPositionToCubeMap);
        positionToEmptyMap = new Dictionary<Vector3Int, GameObject>(initialPositionToEmptyMap);

        // Reset the physical positions of cubes and empty units
        foreach (var pair in positionToCubeMap)
        {
            pair.Value.transform.position = ConvertKeyToPosition(pair.Key);
        }
        foreach (var pair in positionToEmptyMap)
        {
            pair.Value.transform.position = ConvertKeyToPosition(pair.Key);
        }
    }

    private Vector3 ConvertKeyToPosition(Vector3Int key)
    {
        // Assuming you have a reference to the cube prefab to get its size
        Renderer cubeRenderer = cubePrefab.GetComponent<Renderer>();
        if (cubeRenderer == null)
        {
            Debug.LogError("Cube prefab does not have a Renderer component!");
            return Vector3.zero;
        }

        Vector3 cubeSize = cubeRenderer.bounds.size;
        Vector3 interval = cubeSize + new Vector3(tolerance, tolerance, tolerance);

        // Calculate world position based on grid key and interval
        float xPosition = key.x * interval.x;
        float yPosition = key.y * interval.y;
        float zPosition = key.z * interval.z;

        return new Vector3(xPosition, yPosition, zPosition);
    }


    // Method to move a cube in a specified direction
    public bool MoveCube(Vector3Int cubePosition, string direction)
    {
        if (!positionToCubeMap.ContainsKey(cubePosition))
        {
            Debug.LogError("No cube found at the specified position.");
            print(cubePosition.ToString());
            return false;
        }

        Vector3Int targetPosition = cubePosition;
        switch (direction.ToLower())
        {
            case "up": targetPosition.y += 1; break;
            case "down": targetPosition.y -= 1; break;
            case "left": targetPosition.x -= 1; break;
            case "right": targetPosition.x += 1; break;
            case "front": targetPosition.z += 1; break;
            case "back": targetPosition.z -= 1; break;

            default: Debug.LogError("Invalid direction specified."); return false;
        }

        if (positionToEmptyMap.ContainsKey(targetPosition))
        {
            GameObject cube = positionToCubeMap[cubePosition];
            GameObject empty = positionToEmptyMap[targetPosition];

            StartCoroutine(SlideObjects(cube, empty, cubePosition, targetPosition));
        }
        else
        {
            Debug.LogError("The move is not possible. The target position is not empty or out of bounds."); return false;
        }
        return true;
    }

    public float animationDuration = 1.0f; // Duration of the slide in seconds

    // Coroutine for sliding animation
    private IEnumerator SlideObjects(GameObject cube, GameObject empty, Vector3Int cubePosition, Vector3Int targetPosition)
    {
        // Update the dictionaries
        positionToCubeMap.Remove(cubePosition);
        positionToEmptyMap.Remove(targetPosition);

        positionToCubeMap.Add(targetPosition, cube);
        positionToEmptyMap.Add(cubePosition, empty);

        float elapsed = 0;

        Vector3 startPositionCube = cube.transform.position;
        Vector3 startPositionEmpty = empty.transform.position;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;

            cube.transform.position = Vector3.Lerp(startPositionCube, startPositionEmpty, t);
            empty.transform.position = Vector3.Lerp(startPositionEmpty, startPositionCube, t);

            yield return null;
        }

        // Ensure objects are exactly in their final positions
        cube.transform.position = startPositionEmpty;
        empty.transform.position = startPositionCube;
    }

    public Material material1; 
    public Material material2; 
    public Material material3;

    // Function to switch the material of a cube based on its coordinates
    public void SwitchCubeMaterialAt(Vector3Int cubeCoordinates, string materialType)
    {
        if (!positionToCubeMap.TryGetValue(cubeCoordinates, out GameObject cube))
        {
            Debug.LogError("No cube found at the specified coordinates.");
            return;
        }

        Renderer cubeRenderer = cube.GetComponent<Renderer>();
        if (cubeRenderer == null)
        {
            Debug.LogError("The cube at the specified coordinates does not have a Renderer component.");
            return;
        }

        // Switch material based on the materialType argument
        switch (materialType)
        {
            case "normal":
                cubeRenderer.material = material1;
                break;
            case "selected":
                cubeRenderer.material = material2;
                break;
            case "reached":
                cubeRenderer.material = material3;
                break;
            default:
                Debug.LogError("Invalid material type specified.");
                break;
        }
    }

    public TMP_Text destinationReachedText;
    private int destinationReachedCount;
    public void UpdateDestReachedText()
    {
        destinationReachedCount++; // Increment the count
        destinationReachedText.text = "Destination reached: " + destinationReachedCount;
    }

    /*public void GoToDestination(Vector3Int initialPosition, Vector3Int destination)
    {
        SwitchCubeMaterialAt(initialPosition);
    }*/

    // Start is called before the first frame update
    void Start()
    {
        InitPrismaticStructure(structureSize.x, structureSize.y, structureSize.z);
    }

    public void UpdateDuration(float newDuration)
    {
        animationDuration = 1-newDuration;
    }

    // Update is called once per frame
    void Update()
    {

    }

  

}
