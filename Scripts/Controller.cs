using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Controller : MonoBehaviour
{
    public SimController simController;
    private Vector3Int selectedCubePosition = Vector3Int.zero;
    Dictionary<GameObject, Agent> agentLookup = new Dictionary<GameObject, Agent>();

    void Start()
    {

        foreach (var agent in simController.agents)
        {
            agentLookup[agent.visualization] = agent;
        }

    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                GameObject hitObject = hit.collider.gameObject;
                if (hitObject.CompareTag("Box"))
                {
                    Vector3Int position = FindPositionInMap(simController.positionToCubeMap, hitObject);
                    if (position != Vector3Int.zero)
                    {
                        selectedCubePosition = position;
                        Debug.Log("Selected Cube Position: " + selectedCubePosition);
                    }
                }

                else if (hit.collider.gameObject.CompareTag("EmptyUnit"))
                {
                    Vector3Int emptyPosition = FindPositionInMap(simController.positionToEmptyMap, hit.collider.gameObject);
                    if (emptyPosition != Vector3Int.zero && selectedCubePosition != Vector3Int.zero)
                    {

                        string direction = GetDirection(selectedCubePosition, emptyPosition);
                        Vector3Int targetPos = simController.FindTarget(selectedCubePosition, direction);
                        StartCoroutine(DelayedMoveAgentAboveBox(targetPos));
                        print("selected:"+selectedCubePosition+"target:"+ targetPos+ direction);
                        simController.MoveCube(selectedCubePosition, direction);
                        UpdatePosition(direction);
                        SetDistancesToBox(selectedCubePosition);

                    }
                }

                else if (hit.collider.gameObject.CompareTag("agentController"))
                {
                    GameObject agentVis = hit.collider.gameObject.transform.parent.gameObject;
                    if (agentLookup.TryGetValue(agentVis, out Agent agent))
                    {
                        agent.available2 = !agent.available2;
                        Debug.Log($"Agent available status toggled to {agent.available2}");
                    }
                }

            }
        }
        if (selectedCubePosition != Vector3Int.zero)
        {
            TryMoveCube("up", KeyCode.X);
            TryMoveCube("down", KeyCode.Z);
            TryMoveCube("left", KeyCode.A);
            TryMoveCube("right", KeyCode.D);
            TryMoveCube("front", KeyCode.W);
            TryMoveCube("back", KeyCode.S);

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                simController.SwitchCubeMaterialAt(selectedCubePosition, "normal");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                simController.SwitchCubeMaterialAt(selectedCubePosition, "selected");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                simController.SwitchCubeMaterialAt(selectedCubePosition, "reached");
            }

            HandleAgentAssignment(selectedCubePosition);

            if (Input.GetKeyDown(KeyCode.Q))
            {
                selectedCubePosition = Vector3Int.zero;
                foreach (var agent in simController.agents)
                {
                    SetAgentOpacity(agent, 0.3f);
                    agent.available = true;
                    //agent.available2 = true;

                }
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            simController.ResetAllMaterials(); // It can be later changed to only reset the boxes that are not normal
            //simController.SwitchCubeMaterialAt(selectedCubePosition, "normal");
            //selectedCubePosition = Vector3Int.zero;
            simController.ResetPosition();
            simController.ResetAgentPositions();

            foreach (var agent in simController.agents)
            {
                selectedCubePosition = Vector3Int.zero;
                StopAgentMovement(agent);
                SetAgentOpacity(agent, 0.3f);
                agent.available = true;
                //agent.available2 = true;



            }
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            foreach (var agent in simController.agents)
            {
                selectedCubePosition = Vector3Int.zero;
                StopAgentMovement(agent);
                SetAgentOpacity(agent, 0.3f);
                agent.available = true;
                //agent.available2 = true;

            }
        }

    }

    private string GetDirection(Vector3Int from, Vector3Int to)
    {
        Vector3Int directionVector = to - from;

        // Simplify the direction to a unit vector
        directionVector.Clamp(new Vector3Int(-1, -1, -1), new Vector3Int(1, 1, 1));

        if (directionVector == Vector3Int.up)
            return "up";
        else if (directionVector == Vector3Int.down)
            return "down";
        else if (directionVector == new Vector3Int(-1, 0, 0))
            return "left";
        else if (directionVector == new Vector3Int(1, 0, 0))
            return "right";
        else if (directionVector == new Vector3Int(0, 0, 1))
            return "front";
        else if (directionVector == new Vector3Int(0, 0, -1))
            return "back";

        return null; // Return null if no valid direction is determined
    }

    public bool agentLock = false;
    public float maxClosestAgents = 2;

    void HandleAgentAssignment(Vector3Int cubePosition)
    {
        Vector3 cubeWorldPosition = simController.positionToCubeMap[cubePosition].transform.position;
        List<Agent> closestAgents = new List<Agent>();
        int agentsAbove = 0;
        int unavailableAgents = 0;

        // Iterate over agents sorted by distance to the box
        foreach (var agent in simController.agents.OrderBy(agent => agent.distanceToBox))
        {
            Vector3 agentPosition = agent.visualization.transform.position;
            if (IsAgentAbove(cubeWorldPosition, agentPosition, agent.visualization.transform.localScale.x))
            {
                agentsAbove++;
                if (simController.animationEnabled && agent.available && !agentLock && agent.available2)
                {
                    SetAgentOpacity(agent, 0.8f);
                    agent.available = false;
                    agentLock = true;
                }
                else if (!simController.animationEnabled)
                {
                    agent.available = true;
                    //MoveClosestAgentAboveBox(selectedCubePosition);

                    SetAgentOpacity(agent, 0.3f);
                    agentLock = false;
                }
            }
            else if (!simController.animationEnabled)
            {
                float distance = Vector3.Distance(agentPosition, cubeWorldPosition);
                // Add agents to the list with their distance
                closestAgents.Add(agent);
                agent.available = true;
                //MoveClosestAgentAboveBox(selectedCubePosition);

                SetAgentOpacity(agent, 0.3f);
            }

            if (!agent.available2)
            {
                unavailableAgents++;
            }
        }

        // If there are agents above, adjust maxClosestAgents accordingly
        int agentsToMove = (int)Mathf.Max(0, maxClosestAgents - (agentsAbove-unavailableAgents));
        //print(maxClosestAgents+ " " + unavailableAgents);
        //print(agentsToMove);

        if (agentsToMove > 0)
        {
            // Sort agents by distance and take the required number of closest agents
            closestAgents.Sort((a, b) => Vector3.Distance(a.visualization.transform.position, cubeWorldPosition).CompareTo(Vector3.Distance(b.visualization.transform.position, cubeWorldPosition)));
            for (int i = 0; i < System.Math.Min(agentsToMove, closestAgents.Count); i++)
            {
                Agent agentToMove = closestAgents[i];

                if (!agentToMove.available2)
                    continue;

                Vector3 targetPosition = ConstrainPositionWithinBounds(cubeWorldPosition, simController.structureSize, agentToMove.visualization.transform.localScale.x);
                agentToMove.currentMovement = true;
                //simController.SwitchCubeMaterialAt(selectedCubePosition, "selected");

                selectedCubePosition = Vector3Int.zero;
                print("..:");

                StartCoroutine(MoveAgentToPosition(agentToMove, targetPosition, cubePosition));
            }
        }

    }

    bool IsAgentAbove(Vector3 cubeCenter, Vector3 agentPosition, float agentSize)
    {
        float halfAgentSize = agentSize / 2;
        return Mathf.Abs(agentPosition.x - cubeCenter.x) <= halfAgentSize &&
               Mathf.Abs(agentPosition.z - cubeCenter.z) <= halfAgentSize;
    }

    Vector3 ConstrainPositionWithinBounds(Vector3 targetPosition, Vector3Int structureSize, float agentSize)
    {
        // Calculate the effective maximum bounds, incorporating agent size and grid interval
        float maxX = (structureSize.x - 1) * simController.intervalFloat;
        float maxZ = (structureSize.z - 1) * simController.intervalFloat;

        // Determine the half-width of the agent to adjust the clamping range
        float halfAgentSize = agentSize / 2;
        float halfInterval = simController.intervalFloat / 2;

        // Calculate the discrete range steps for randomization
        float rangeSteps = 1;  // This means -2, -1, 0, 1, 2 steps are available
        int randomStepX = Random.Range((int)-rangeSteps, (int)rangeSteps + 1); // +1 because upper bound is exclusive
        int randomStepZ = Random.Range((int)-rangeSteps, (int)rangeSteps + 1);

        // Apply randomization to targetPosition x and z using discrete steps
        targetPosition.x += randomStepX * simController.intervalFloat;
        targetPosition.z += randomStepZ * simController.intervalFloat;

        print(randomStepX * simController.intervalFloat);

        // Clamp positions to ensure agent's edges do not exceed the structure bounds
        float clampedX = Mathf.Clamp(targetPosition.x, halfAgentSize - halfInterval, maxX - halfAgentSize + halfInterval);
        float clampedZ = Mathf.Clamp(targetPosition.z, halfAgentSize - halfInterval, maxZ - halfAgentSize + halfInterval);

        // Return the new position with the original y-coordinate (unchanged vertical position)
        return new Vector3(clampedX + randomStepX * simController.intervalFloat, targetPosition.y, clampedZ);
    }


    public void SetAgentOpacity(Agent agent, float opacity)
    {
        Renderer renderer = agent.visualization.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = renderer.material;

            // Ensure the material copies do not pile up in memory
            if (!mat.name.EndsWith(" (Instance)"))
            {
                renderer.material = new Material(mat) { name = $"{mat.name} (Instance)" };
                mat = renderer.material;
            }

            // Check and set the material to transparent mode to properly display transparency
            if (mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 2); // Sets the material to Transparent mode
            }

            // Clone the existing color but replace the alpha value
            Color color = mat.color;
            color.a = opacity;
            mat.color = color;
        }
    }

    public float agentSpeed = 1f; // Units per second

    IEnumerator MoveAgentToPosition(Agent agent, Vector3 targetPosition, Vector3Int cubePosition)
    {
        Vector3 startingPosition = agent.visualization.transform.position;
        float startY = startingPosition.y; // Preserve the original Y position

        float distanceToTarget = Vector3.Distance(new Vector3(startingPosition.x, startY, startingPosition.z), new Vector3(targetPosition.x, startY, targetPosition.z));
        float timeToMove = distanceToTarget / agentSpeed;

        float elapsedTime = 0;

        while (elapsedTime < timeToMove && agent.currentMovement)
        {
            Vector3 newPosition = Vector3.Lerp(startingPosition, new Vector3(targetPosition.x, startY, targetPosition.z), elapsedTime / timeToMove);
            agent.visualization.transform.position = newPosition;
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (agent.currentMovement)
        // Ensure the agent stops exactly at the grid position, correcting any floating-point imprecision from Lerp
            agent.visualization.transform.position = new Vector3(targetPosition.x, startY, targetPosition.z);
        agent.currentMovement = false;
        selectedCubePosition = cubePosition;

    }

    void StopAgentMovement(Agent agent)
    {
        if (agent.currentMovement != false)
        {
            print("stop agent");
            agent.currentMovement = false;
        }
    }


    Vector3Int FindPositionInMap(Dictionary<Vector3Int, GameObject> map, GameObject targetObject)
    {
        foreach (KeyValuePair<Vector3Int, GameObject> entry in map)
        {
            if (entry.Value == targetObject)
            {
                return entry.Key;
            }
        }
        return Vector3Int.zero;
    }

    IEnumerator DelayedMoveAgentAboveBox(Vector3Int boxPosition)
    {
        // Wait for the specified animation duration before moving the agent
        yield return new WaitForSeconds(0.01f);

        // Now move the closest agent above the box
        MoveClosestAgentAboveBox(boxPosition);
    }


    void TryMoveCube(string direction, KeyCode key)
    {
        if (Input.GetKeyDown(key))
        {
            Vector3Int previousPosition = selectedCubePosition;

            Vector3Int targetPos = simController.FindTarget(selectedCubePosition, direction);
            StartCoroutine(DelayedMoveAgentAboveBox(targetPos));

            if (simController.MoveCube(selectedCubePosition, direction))
            {
                UpdatePosition(direction);

                print("*0." + selectedCubePosition);
                //StartCoroutine(DelayedMoveAgentAboveBox(selectedCubePosition));

                Debug.Log("Moved " + direction + ". New position: " + selectedCubePosition);
            }
            else
            {
                selectedCubePosition = previousPosition; // Revert if not moved
                Debug.LogError("Failed to move " + direction);
            }
        }
    }
    void SetDistancesToBox(Vector3Int boxPosition)
    {
        Vector3 boxWorldPosition = simController.positionToCubeMap[boxPosition].transform.position;

        foreach (var agent in simController.agents)
        {
            // Calculate the distance from the agent to the box
            agent.distanceToBox = Vector3.Distance(agent.visualization.transform.position, boxWorldPosition);
            //print(agent.distanceToBox);
        }
    }

// Method to find the closest available agent to a given box position
    Agent FindClosestAgent(Vector3Int boxPosition)
    {
        // First, update all distances to ensure they are current
        SetDistancesToBox(boxPosition);

        // Use LINQ to find the closest agent whose available2 flag is true
        Agent closestAgent = simController.agents

            // Comment out below to not filter availability

            .Where(agent => agent.available2) // Filter to include only agents where available2 is true 
            .OrderBy(agent => agent.distanceToBox) // Order by distance
            .FirstOrDefault(); // Get the first agent that meets the criteria or null if none do

        return closestAgent;
    }

    void MoveClosestAgentAboveBox(Vector3Int boxPosition)
    {

        Agent closestAgent = FindClosestAgent(boxPosition);

        if (closestAgent != null)
        {
            // Calculate the target position directly above the box
            Vector3 boxWorldPosition = simController.positionToAllMap[boxPosition].transform.position;
            print("*:" + boxPosition + " world"+ boxWorldPosition);
            Vector3 targetPosition = new Vector3(boxWorldPosition.x, closestAgent.visualization.transform.position.y, boxWorldPosition.z); // Adjust Y offset as needed

            selectedCubePosition = Vector3Int.zero;
            //print("*.*a"+closestAgent+" "+ targetPosition+" "+ boxPosition);

            // Start the coroutine to move the agent
            closestAgent.currentMovement = true;
            print("*a." + targetPosition);
            StartCoroutine(MoveAgentToPosition(closestAgent, targetPosition, boxPosition));
        }
    }


    void UpdatePosition(string direction)
    {
        switch (direction.ToLower())
        {
            case "up": selectedCubePosition.y += 1; break;
            case "down": selectedCubePosition.y -= 1; break;
            case "left": selectedCubePosition.x -= 1; break;
            case "right": selectedCubePosition.x += 1; break;
            case "front": selectedCubePosition.z += 1; break;
            case "back": selectedCubePosition.z -= 1; break;
        }
    }
}
