using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class QLearningAgent : MonoBehaviour
{
    private string[] actions = {"moveLeft", "moveRight", "moveFront", "moveBack", "ascend", "descend"};
    private Dictionary<Vector3Int, Dictionary<string, float>> qTable;
    public SimController simController; // Reference to the SimController script
    public float epsilon = 0.1f;
    private float initialEpsilon;
    public float alpha = 0.5f;
    public float gamma = 0.9f;
    public int maxMovesPerEpisode = 50;
    public Vector3Int gridSize;
    public Vector3Int initialPosition;
    public Vector3Int destination;
    public bool loadTable = false;
    bool destinationReached = false;
    int distanceCovered = 0;

    // For each episode
    float totalReward = 0;
    InvalidMoves invalidMoves;
    int exploration;
    int exploitation;

    public bool SetVariables(List<Vector3Int> varStack)
    {
        // Check if varStack has enough elements
        if (varStack.Count >= 3)
        {
            gridSize = varStack[0];
            initialPosition = varStack[1];
            destination = varStack[2];
            return true; // Return true if the assignment is successful
        }
        else
        {
            Debug.Log("Variables could not be assigned!");
            return false; // Return false if varStack does not have enough elements
        }
    }

    public Vector3Int currentState;
    private int currentMoves;
    void Start()
    {
        simController = GameObject.FindGameObjectWithTag("gc").GetComponent<SimController>();
        ClearEpisodeDataFile();

        initialEpsilon = epsilon;

        if (loadTable)
            LoadQTable();
        else
            InitializeQTable();

        currentState = initialPosition;
        currentMoves = 0;
        invalidMoves = new InvalidMoves();
        invalidMoves.Reset();
        exploration = 0;
        exploitation = 0;
    }

    void InitializeQTable()
    {
        qTable = new Dictionary<Vector3Int, Dictionary<string, float>>();
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                for (int z = 0; z < gridSize.z; z++)
                {
                    Vector3Int state = new Vector3Int(x, y, z);
                    qTable[state] = new Dictionary<string, float>();
                    foreach (string action in actions)
                    {
                        qTable[state][action] = 0.0f; // Initialize Q-values to 0
                    }
                }
            }
        }
    }

    string ChooseAction(Vector3Int state)
    {
        if (Random.Range(0f, 1f) < epsilon)
        {
            // Explore
            exploration++;
            return actions[Random.Range(0, actions.Length)];
        }
        else
        {
            // Exploit best known action
            exploitation++;
            string bestAction = null;
            float maxQValue = float.MinValue;
            foreach (var action in actions)
            {
                float qValue = qTable[state][action];
                if (qValue > maxQValue)
                {
                    maxQValue = qValue;
                    bestAction = action;
                }
            }
            return bestAction;
        }
    }


    void Learn(Vector3Int oldState, string action, float reward, Vector3Int newState)
    {
        float oldQValue = qTable[oldState][action];
        float maxNewQValue = float.MinValue;
        foreach (var newAction in actions)
        {
            maxNewQValue = Mathf.Max(maxNewQValue, qTable[newState][newAction]);
        }
        float newQValue = (1 - alpha) * oldQValue + alpha * (reward + gamma * maxNewQValue);
        qTable[oldState][action] = newQValue;
    }

    // Mapping agent actions to SimController commands
    string ConvertActionToDirection(string action)
    {
        switch (action)
        {
            case "moveLeft": return "left";
            case "moveRight": return "right";
            case "moveFront": return "front";
            case "moveBack": return "back";
            case "ascend": return "up";
            case "descend": return "down";
            default: return ""; // Return an empty string or handle invalid action
        }
    }

    Vector3Int CalculateNewState(Vector3Int currentState, string direction)
    {
        Vector3Int newState = new Vector3Int(currentState.x, currentState.y, currentState.z);

        switch (direction)
        {
            case "up":
                newState.y += 1;
                break;
            case "down":
                newState.y -= 1;
                break;
            case "left":
                newState.x -= 1;
                break;
            case "right":
                newState.x += 1;
                break;
            case "front":
                newState.z += 1;
                break;
            case "back":
                newState.z -= 1;
                break;
        }

        return newState;
    }


    void HandleFailedMove(Vector3Int currentState, string direction)
    {
        // Logic to handle a failed move
        // This could involve leaving the state unchanged, applying a penalty, etc.
        // ...
    }

    bool destReachedFlag = false;
    public bool exit = false;

    public bool TrainStep()
    {
        print("before TrainStep");

        distanceCovered++;
        string action = ChooseAction(currentState);
        string direction = ConvertActionToDirection(action);
        print("before ClearUpperBoxes");
        ClearUpperBoxes(currentState);
        print("after ClearUpperBoxes");

        if (exit)
        {
            print("exit");
            exit = false;
            //currentEpisode -= 1;
            return false;
        }
        
        //stop = false;
        // Attempt to move the cube
        bool moveSuccessful = simController.MoveCube(currentState, direction);

        Vector3Int newState = currentState;
        if (moveSuccessful)
        {
            // If move is successful, calculate the new state
            // Assuming the new state is based on the action taken
            newState = CalculateNewState(currentState, direction);
        }
        else
        {
            // If move is not successful, handle accordingly
            // Placeholder for custom logic in case of a failed move
            HandleFailedMove(currentState, direction);
        }

        //print(currentState.ToString() + newState.ToString());

        float reward = GetReward(newState);
        Learn(currentState, action, reward, newState);
        currentState = newState;
        currentMoves++;

        if (currentState == destination)
        {
            Debug.Log("Destination reached");
            destReachedFlag = destinationReached = true;
            EndOfEpisode();
            distanceCovered = 0;
            return true; // Indicate that the destination was reached
        }
        else if (currentMoves >= maxMovesPerEpisode)
        {
            Debug.Log("Lost");
            destinationReached = false;
            EndOfEpisode();
            distanceCovered = 0;
            return true; // Indicate that the agent is lost
        }
        return false; // Indicate that the training is ongoing
    }
    public bool waitForAnim = true;
    public void ClearUpperBoxes(Vector3Int cubePosition)
    {
        simController.RegisterUpperBoxes(cubePosition);
        print("registered");
    }


    private Queue<Vector3Int> stateHistory = new Queue<Vector3Int>();
    private int stateHistoryLength = 8; // Length of the history buffer
    float GetReward(Vector3Int state)
    {
        //gamma = initialGamma;
        float reward = 0;

        // Penalize in each step
        reward += -0.22f;

        // Reward for reaching the destination
        if (state == destination)
        {
            reward += 0.2f;
        }

        // Penalize for not moving due to obstructions
        if (state == currentState)
        {
            reward += -0.5f;
            invalidMoves.impossibleMoves += 1;
        }

        // Reward for ascending towards the highest plane while aligning with the initial state's x and z coordinates
        if (state.y > currentState.y && state.x == initialPosition.x && state.z == initialPosition.z)
        {
            //print("reward"+ currentState.ToString() + state.ToString());
            reward += 1.75f;
        }

        else if (state.x == initialPosition.x && state.z == initialPosition.z)
        {
            invalidMoves.unwantedMoves += 1;
        }
        // **********************************************************************************


        // Penalty for descending downwards without reaching the destination's x and z coordinates
        if (state.y < currentState.y && !(state.x == destination.x && state.z == destination.z))
        {
            //print("reward"+ currentState.ToString() + state.ToString());
            reward += -2.25f;
            invalidMoves.unwantedMoves += 1;
        }


        // Penalize if the state is in the history buffer
        if (stateHistory.Contains(state))
        {
            reward += -2.0f; // Penalty for revisiting a recent state
        }

        // Update state history
        stateHistory.Enqueue(state);
        if (stateHistory.Count > stateHistoryLength)
        {
            stateHistory.Dequeue();
        }

        // Penalize downward movement when the agent reaches the target column
        if (currentState.x == initialPosition.x && currentState.z == initialPosition.z
           && (state.x != initialPosition.x || state.z != initialPosition.z))
        {
            //print("reward"+ currentState.ToString() + state.ToString());
            reward += -0.75f;
            invalidMoves.unwantedMoves += 1;
        }



       totalReward += reward;
        return reward;

    }

    public void ResetStateHistory()
    {
        stateHistory.Clear();
    }

    public int totalEpisodes = 900;
    private int currentEpisode = 0;

    private float timer = 0f;

    public float minEpsilon = 0.05f;
    public float epsilonDecayRate = 0.995f;

    public bool stop = false;

    // Update is called once per frame
    private void Update()
    {
        timer += Time.deltaTime;

        if (!stop && timer >= simController.animationDuration)
        {
            if (!resetFlag)
                GoToDestination();

            timer = 0f; // Reset the timer
        }
    }
    void GoToDestination()
    {
        if (currentEpisode < totalEpisodes)
        {
            Debug.Log("Episode: " + currentEpisode);
            simController.movect = 0;

            bool episodeCompleted = TrainStep();
            if (episodeCompleted)
            {
                //Debug.Log("Episode: " + currentEpisode);
                StartNewEpisode();

                if (currentEpisode >= totalEpisodes)
                {
                    Debug.Log("Training completed.");
                    // Perform any finalization needed after training
                }

                StartCoroutine(ResetAgent(simController.animationDuration));

            }

            // Update the Unity scene here (e.g., move game objects, update UI, etc.)
        }
    }

    void StartNewEpisode()
    {
        // Reset the state history at the beginning of the episode
        ResetStateHistory();
        invalidMoves.Reset();
        totalReward = 0;
        exploration = 0;
        exploitation = 0;
        currentEpisode++;
        epsilon = Mathf.Max(minEpsilon, epsilon * epsilonDecayRate);
    }


    [SerializeField]
    bool resetFlag = false;
    IEnumerator ResetAgent(float time)
    {
        ToggleFlag(ref resetFlag);
        yield return new WaitForSeconds(time);

        if (destReachedFlag)
        {
            simController.UpdateDestReachedText();
            simController.SwitchCubeMaterialAt(currentState, "reached");
        }

        yield return new WaitForSeconds(time);

        simController.ResetPosition(); // Reset the agent's position for the next episode

        currentMoves = 0; // Reset the move counter
        currentState = initialPosition;
        ToggleFlag(ref resetFlag);

        if (destReachedFlag)
        {
            simController.SwitchCubeMaterialAt(currentState, "selected");
            destReachedFlag = false;
        }
        //currentState = initialPosition; // Reset the state
    }

    private void ToggleFlag(ref bool flag)
    {
        flag = !flag;
    }

    readonly string qTablePath = Application.dataPath + "/QTables/table.json";

    public void SaveQTable()
    {
        QTableUtils.SaveQTable(qTable, qTablePath);

        SaveSessionData(Application.dataPath + "/QTables/sessionData.json");
    }

    public void LoadQTable()
    {
        //qTable = new Dictionary<Vector3Int, Dictionary<string, float>>();

        qTable = QTableUtils.LoadQTable(qTablePath);
    }

    public void SaveSessionData(string filePath)
    {
        SessionData data = new SessionData
        {
            initialEpsilon = this.initialEpsilon,
            alpha = this.alpha,
            gamma = this.gamma,
            maxMovesPerEpisode = this.maxMovesPerEpisode,
            gridSize = this.gridSize,
            initialPosition = this.initialPosition,
            destination = this.destination,
            loadTable = this.loadTable,
            minEpsilon = this.minEpsilon,
            epsilonDecayRate = this.epsilonDecayRate
        };

        string json = JsonUtility.ToJson(data);
        // Serialize the data with indentation
        File.WriteAllText(filePath, json);
    }

    // Method to save episode data
    public void SaveEpisodeData(string filePath, EpisodeData data)
    {
        string json = JsonUtility.ToJson(data);

        // Append data to the file
        using (StreamWriter sw = File.AppendText(filePath))
        {
            sw.WriteLine(json);
        }
    }

    string epFilePath = Application.dataPath + "/QTables/episodeData.json";

    void EndOfEpisode()
    {
        EpisodeData data = new EpisodeData
        {
            episodeNumber = currentEpisode/* current episode number */,
            destinationReached = this.destinationReached/* true or false */,
            distanceCovered = this.distanceCovered /* calculate distance covered */,
            //stepsTaken = this.stepsTaken/* steps taken in the episode */,
            epsilon = this.epsilon,
            totalReward = this.totalReward,
            impossibleMoves = this.invalidMoves.impossibleMoves,
            unwantedMoves = this.invalidMoves.unwantedMoves,
            exploration = this.exploration,
            exploitation = this.exploitation

        };
        
        SaveEpisodeData(epFilePath, data);
    }

    void ClearEpisodeDataFile()
    {

        // Check if the file exists before trying to clear its contents
        if (File.Exists(epFilePath))
        {
            ClearFileContents(epFilePath);
            Debug.Log("File contents cleared: " + epFilePath);
        }
        else
        {
            Debug.LogWarning("File not found, cannot clear contents: " + epFilePath);
        }
    }

    public void ClearFileContents(string filePath)
    {
        // Overwrite the file with an empty string
        File.WriteAllText(filePath, string.Empty);
    }
}

    [System.Serializable]
    public class SessionData
    {
        public float initialEpsilon;
        public float alpha;
        public float gamma;
        public int maxMovesPerEpisode;
        public Vector3Int gridSize;
        public Vector3Int initialPosition;
        public Vector3Int destination;
        public bool loadTable;
        public float minEpsilon;
        public float epsilonDecayRate;
    }

    

    [System.Serializable]
    public class EpisodeData
    {
        public int episodeNumber;
        public bool destinationReached;
        public int distanceCovered;
        //public int stepsTaken;
        public float epsilon;
        public float totalReward;
        public float impossibleMoves;
        public float unwantedMoves;
        public int exploration;
        public int exploitation;
}

    public class InvalidMoves
    {
        // Attribute for moves that are physically impossible or invalid
        public float impossibleMoves;

        // Attribute for moves that are possible but not desired according to the strategy
        public float unwantedMoves;

        // Method to reset all attributes to zero
        public void Reset()
        {
            impossibleMoves = 0.0f;
            unwantedMoves = 0.0f;
        }
}











