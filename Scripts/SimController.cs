using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using System.Linq;

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
    
    public GameObject cubesParent;
    public QLearningAgent qla;

    // Method to initialize the prismatic structure
    public void InitPrismaticStructure(int a, int b, int c)
    {
        if (cubePrefab == null || emptyPrefab == null)
        {
            Debug.LogError("Prefabs are not assigned!");
            return;
        }

        // Create an empty GameObject to hold all cubes
        cubesParent = new GameObject("BoxContainer");

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
        if (qla == null && GameObject.FindGameObjectWithTag("la") != null)
            qla = GameObject.FindGameObjectWithTag("la").GetComponent<QLearningAgent>();

        print(1);
        if (!positionToCubeMap.ContainsKey(cubePosition))
        {
            Debug.LogError("No cube found at the specified position.");
            print(cubePosition.ToString());
            return false;
        }
        print(2);


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
        print(3);

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
        print(4);

        return true;
    }
    public int movect = 0;
    //public Vector3Int pickAndMove = Vector3Int.zero;
    public bool PickAndMoveCube(Vector3Int cubePosition)
    {
        // List of directions
        string[] directions = { "left", "right", "front", "back" };

        // Randomly select a direction
        string direction = directions[Random.Range(0, directions.Length)];

        // Initialize targetPosition
        Vector3Int targetPosition = cubePosition;

        // Modify targetPosition based on the selected direction
        switch (direction.ToLower())
        {
            case "left": targetPosition.x -= 1; break;
            case "right": targetPosition.x += 1; break;
            case "front": targetPosition.z += 1; break;
            case "back": targetPosition.z -= 1; break;
            default: Debug.LogError("Invalid direction specified."); return false;
        }

        // Check if the target position is within the structure boundaries
        if (targetPosition.x < 0 || targetPosition.x >= structureSize.x ||
            targetPosition.z < 0 || targetPosition.z >= structureSize.z)
        {
            // If the target position is out of bounds, choose a new random direction and try again
            return PickAndMoveCube(cubePosition);
        }

        print("from:" + cubePosition.ToString());
        print("target:" + targetPosition.ToString());
        print("\n");

        if (qla == null && GameObject.FindGameObjectWithTag("la") != null)
            qla = GameObject.FindGameObjectWithTag("la").GetComponent<QLearningAgent>();

        qla.ClearUpperBoxes(targetPosition);

        // Call the MoveCube function
        if (!MoveCube(cubePosition, direction) && movect < 6)
        {
            print("pick and move");
            // If MoveCube failed, try recursively moving the cube in the specified direction
            movect++;
            return PickAndMoveCube(targetPosition);
        }

        qla.stop = false;
        print("return");
        return true;
    }

    //public int localStop = 0;
    Dictionary<Vector3Int, string> indexToOldBlockDirection = new Dictionary<Vector3Int, string>();
    List<List<QTreeNode>> chainList = new List<List<QTreeNode>>();

    public bool PickAndMoveCubeAnim(Vector3Int cubePosition)
    {
        if (qla == null && GameObject.FindGameObjectWithTag("la") != null)
            qla = GameObject.FindGameObjectWithTag("la").GetComponent<QLearningAgent>();

        // List of directions
        string[] directions = { "left", "right", "front", "back" };
        float epsilon = 0.1f;
        // Find the node with cubePosition in any tree
        QTreeNode node = null;
        QTree qTree = null;
        foreach (var tree in qla.qTreeList)
        {
            node = FindNode(tree.GetRoot(), cubePosition);
            if (node != null)
            {
                qTree = tree;
                break;
            }
        }
        if (node == null) print("nulll"+cubePosition.ToString());

        bool hasChildren = node.Children.Count > 0;

        // Initialize targetPosition
        Vector3Int targetPosition = Vector3Int.zero;
        string direction = "";
        if (Random.Range(0f, 0.4f) < epsilon || !hasChildren)
        {
            print("yyyrandom");
            // Check if the target position is within the structure boundaries
            do
            {
                // Randomly select a direction

                // Check if cubePosition exists in indexToOldBlockDirection
                if (indexToOldBlockDirection.ContainsKey(cubePosition))
                {
                    foreach (var kvp in indexToOldBlockDirection)
                    {
                        Debug.Log("Key: " + kvp.Key + ", Value: " + kvp.Value);
                    }

                    // Retrieve the direction associated with cubePosition
                    direction = indexToOldBlockDirection[cubePosition];

                    // Remove the element from the dictionary
                    indexToOldBlockDirection.Remove(cubePosition);

                    // Now you can use the direction variable as needed
                }
                else
                    direction = directions[Random.Range(0, directions.Length)];

                // Initialize targetPosition with cubePosition for each iteration
                targetPosition = cubePosition;

                // Modify targetPosition based on the selected direction
                switch (direction.ToLower())
                {
                    case "left": targetPosition.x -= 1; break;
                    case "right": targetPosition.x += 1; break;
                    case "front": targetPosition.z += 1; break;
                    case "back": targetPosition.z -= 1; break;
                }
            }
            while (targetPosition.x < 0 || targetPosition.x >= structureSize.x ||
                     targetPosition.z < 0 || targetPosition.z >= structureSize.z ||
                     upperBoxPoses.Contains(targetPosition));

            if (RegisterUpperBoxesInner(targetPosition) || RegisterUpperBoxesInner(cubePosition))
            {
                //print("error!");

                print("**"+cubePosition.ToString()+targetPosition.ToString());

                AddChildToNode(cubePosition, tempUpperY);
                print("**3" + cubePosition.ToString() + tempUpperY.ToString());
                tempUpperY = Vector3Int.zero;

                indexToOldBlockDirection.Add(cubePosition, direction);
                AddChildToNode(cubePosition, targetPosition);

                return false;
            }
        }

        else
        {
            print("yyyExploit");
            // Find the child with the highest QValue
            QTreeNode childWithHighestQValue = node.Children.OrderByDescending(child => child.QValue).First();
            targetPosition = childWithHighestQValue.State;
            //print("**********t:" + targetPosition.ToString());
            direction = FindDirection(cubePosition, targetPosition);

        }

        print("fromm:" + cubePosition.ToString());
        print("targett:" + targetPosition.ToString());
        print("\n");



        //ClearUpperBox(targetPosition);
        QTreeNode childNode = AddChildToNode(cubePosition, targetPosition);

        // Assuming childNode is a QTreeNode instance
        List<QTreeNode> chain = childNode.GetParentsUntilRoot();
        chain.Add(childNode);

        if (!IsChainInList(chain))
        {
            chainList.Add(chain);
            childNode.ApplyLearnedQValueToAllNodes(-0.01f,1.5f);

        }

        // Call the MoveCube function
        if (!MoveCube(cubePosition, direction))
        {
            print("pick and move");
            // If MoveCube failed, try recursively moving the cube in the specified direction
            //movect++;
            upperBoxPoses.Add(targetPosition);
            indexToOldBlockDirection.Add(cubePosition, direction);
            return false;
            //pickAndMove = targetPosition;
        }

        else
        {
            epsilon = qTree.epsilon;
            qTree.DecayEpsilon();
            print("yyy" + qTree.epsilon);
            //localStop++;
            //pickAndMove = Vector3Int.zero;
            downwardQueue.Add(targetPosition);
            print("returns true");
            return true;
        }

        //print("return");
    }

    public bool IsChainInList(List<QTreeNode> targetChain)
    {
        foreach (var chain in chainList)
        {
            if (chain.SequenceEqual(targetChain))
            {
                return true;
            }
        }
        return false;
    }

    // AddChild() TODO add tree related methods to tree classes and change their names
    public QTreeNode AddChildToNode(Vector3Int cubePosition, Vector3Int targetPosition)
    {
        if (qla == null && GameObject.FindGameObjectWithTag("la") != null)
            qla = GameObject.FindGameObjectWithTag("la").GetComponent<QLearningAgent>();

        // Find the node with cubePosition in any tree
        QTreeNode node = null;
        foreach (var tree in qla.qTreeList)
        {
            node = FindNode(tree.GetRoot(), cubePosition);
            if (node != null)
            {
                break;
            }
        }

        if (node != null)
        {
            QTreeNode childNode;
            // Check if targetPosition is already a child of cubePosition
            if ((childNode = IsChildOfNode(node, targetPosition)) != null)
            {
                Debug.LogWarning("targetPosition is already a child of cubePosition.");
                return childNode;
            }
            // Create child if it does not exist

            // Create a new node for targetPosition
            childNode = new QTreeNode(targetPosition);

            // Add the new node as a child of the found node
            node.AddChild(childNode);
            childNode.Parent = node;
            return childNode;
        }
        else
        {
            Debug.LogError("Node with cubePosition not found in any tree.");
            return null;
        }
    }

    // Helper method to check if a node with state exists in the children of a given node
    private QTreeNode IsChildOfNode(QTreeNode currentNode, Vector3Int state)
    {
        foreach (var childNode in currentNode.Children)
        {
            if (childNode.State == state)
            {
                return childNode;
            }
        }
        return null;
    }


    // Helper method to recursively find a node with a specific state in the tree
    private QTreeNode FindNode(QTreeNode currentNode, Vector3Int state)
    {
        if (currentNode.State == state)
        {
            return currentNode;
        }

        foreach (QTreeNode childNode in currentNode.Children)
        {
            QTreeNode foundNode = FindNode(childNode, state);
            if (foundNode != null)
            {
                return foundNode;
            }
        }

        return null;
    }

    // Finds the direction in x-z 2D plane
    string FindDirection(Vector3Int cubePosition, Vector3Int targetPosition)
    {
        // Calculate the direction based on the difference between cubePosition and targetPosition
        int deltaX = targetPosition.x - cubePosition.x;
        int deltaZ = targetPosition.z - cubePosition.z;

        // Check which axis has the greater difference to determine the direction
        if (Mathf.Abs(deltaX) > Mathf.Abs(deltaZ))
        {
            // Move along the x-axis
            if (deltaX > 0)
            {
                return "right";
            }
            else
            {
                return "left";
            }
        }
        else
        {
            // Move along the z-axis
            if (deltaZ > 0)
            {
                return "front";
            }
            else
            {
                return "back";
            }
        }
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
            if (cube != null && empty != null) {
                cube.transform.position = Vector3.Lerp(startPositionCube, startPositionEmpty, t);
                empty.transform.position = Vector3.Lerp(startPositionEmpty, startPositionCube, t);
            }

            yield return null;
        }

        if (cube != null && empty != null)
        {
            // Ensure objects are exactly in their final positions
            cube.transform.position = startPositionEmpty;
            empty.transform.position = startPositionCube;
        }
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

    public void resetDestReachedText()
    {
        destinationReachedCount = 0;
        destinationReachedText.text = "Destination reached: " + destinationReachedCount;

    }

    /*public void GoToDestination(Vector3Int initialPosition, Vector3Int destination)
    {
        SwitchCubeMaterialAt(initialPosition);
    }*/

    // Start is called before the first frame update
    public void Start()
    {
        //upperBoxIndex = -1;
        //totalUpperBoxes = 0;
        upperBoxPoses.Clear();
        downwardQueue.Clear();
        chainList.Clear();
        indexToOldBlockDirection.Clear();
        positionToCubeMap.Clear();
        positionToEmptyMap.Clear();
        InitPrismaticStructure(structureSize.x, structureSize.y, structureSize.z);
    }

    public void UpdateDuration(float newDuration)
    {
        animationDuration = 1-newDuration;
    }

    public List<Vector3Int> upperBoxPoses = new List<Vector3Int>();
    public List<Vector3Int> downwardQueue = new List<Vector3Int>();

    public void RegisterUpperBoxes(Vector3Int cubePosition)
    {
        print("inside ClearUpperBoxes");
        if (qla == null && GameObject.FindGameObjectWithTag("la") != null)
            qla = GameObject.FindGameObjectWithTag("la").GetComponent<QLearningAgent>();
        for (int y = cubePosition.y + 1; y < structureSize.y ; y++)
        {
            var upperYPos = new Vector3Int(cubePosition.x, y, cubePosition.z);
            if (positionToCubeMap.ContainsKey(upperYPos))
            {
                upperBoxPoses.Add(upperYPos);

                // Check if any item in qla.qTreeList has the same root position as upperYPos
                if (!qla.qTreeList.Any(item => item.GetRoot().State.Equals(upperYPos)))
                {
                    // If no item with the same root position is found, add a new QTree object with the specified upperYPos to the list
                    qla.qTreeList.Add(new QTree(upperYPos));
                }

            }
        }
        foreach (QTree tree in qla.qTreeList)
        {
            Debug.Log("Root position of tree: " + tree.GetRoot().State);
        }
        if (upperBoxPoses.Count > 0)
        {
            qla.stop = true;
            print("stop qlearning!");
            qla.exit = true;
        }

        print("total upper boxes:" + upperBoxPoses.Count);
        //upperBoxIndex = upperBoxPoses.Count - 1;
    }
    public Vector3Int tempUpperY;
    public bool RegisterUpperBoxesInner(Vector3Int cubePosition)
    {
        print("inside ClearUpperBoxes");

        for (int y = cubePosition.y + 1; y < structureSize.y; y++)
        {
            var upperYPos = new Vector3Int(cubePosition.x, y, cubePosition.z);
            if (positionToCubeMap.ContainsKey(upperYPos))
            {
                upperBoxPoses.Add(upperYPos);
                tempUpperY = upperYPos;
                return true;
            }
        }

        return false;

        //upperBoxIndex = upperBoxPoses.Count - 1;
    }
    //private int upperBoxIndex = -1;
    /*    public void ClearNextUpperBox()
        {
            //print("any:" + upperBoxIndex);
            int index = upperBoxPoses.Count - 1;
            var upperYPos = upperBoxPoses[index];
    *//*        if (positionToCubeMap.ContainsKey(upperYPos))
            {*//*

            print("trig:" + upperYPos);
            if (qla.waitForAnim) pickAndMove = upperYPos;
            else PickAndMoveCube(upperYPos);
            //}
        }*/
    [SerializeField]
    private float timer1 = 0f;

    // Update is called once per frame
    void Update()
    {
        timer1 += Time.deltaTime;

        if (timer1 >= animationDuration)
        {
            //print("pickandmoveAnim");
            if (downwardQueue.Count > 0)
            {
                int index = downwardQueue.Count - 1;
                bool retv = MoveCube(downwardQueue[index], "down");
                if (!retv)
                {
                    downwardQueue.RemoveAt(index);
                }
                else
                {
                    // Retrieve the current Vector3Int at the specified index
                    Vector3Int currentValue = downwardQueue[index];

                    // Decrement the z value by 1
                    Vector3Int newValue = new Vector3Int(currentValue.x, currentValue.y - 1, currentValue.z);

                    // Assign the new Vector3Int to the specified index in the list
                    downwardQueue[index] = newValue;
                }
            }
            else if (upperBoxPoses.Count > 0)
            {
                int index = upperBoxPoses.Count - 1;
                print("merhaba");
                bool retv = PickAndMoveCubeAnim(upperBoxPoses[index]);

                if (retv)
                {
                    upperBoxPoses.RemoveAt(index);
                }

/*                else
                {
                    timer1 = 1500;
                    return;
                }*/
                    
            }

            else if (qla != null)
                qla.stop = false;

            if (qla != null)
            {
                QTreePrinter qTreePrinter = new QTreePrinter();
                qTreePrinter.PrintAllTreesToJsonFile(qla.qTreeList, Application.dataPath + "/QTables/trees/all_trees.json");
                //print("error!1");
            }
            //print("timer reset");
            timer1 = 0f; // Reset the timer

        }


        /*        if (pickAndMove != Vector3Int.zero)
                {
                    timer1 += Time.deltaTime;

                    if (timer1 >= animationDuration)
                    {
                        print("pickandmoveAnim");
                        PickAndMoveCubeAnim(pickAndMove);
                        timer1 = 0f; // Reset the timer
                    }

                }
                if(upperBoxIndex >= 0)
                {
                    print("upperBoxIndex:" + upperBoxIndex);

                    ClearUpperBox();
                    upperBoxIndex--;
                    if (upperBoxIndex == -1) upperBoxPoses.Clear();

                }

                if (localStop == totalUpperBoxes)
                {
                    timer2 += Time.deltaTime;

                    if (timer2 >= animationDuration)
                    {

                        print("localstop");
                        qla.stop = false;
                        localStop = totalUpperBoxes = 0;
                        timer2 = 0f; // Reset the timer
                    }

                }*/

    }


}
