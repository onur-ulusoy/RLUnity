using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class QTableUtils : MonoBehaviour
{
    public static void SaveQTable(Dictionary<Vector3Int, Dictionary<string, float>> qTable, string filePath)
    {
        SerializableQTable serializableQTable = new SerializableQTable();
        foreach (var state in qTable.Keys)
        {
            StateActionPair pair = new StateActionPair();
            pair.state = state;
            pair.actions = new SerializableActionDictionary();

            foreach (var action in qTable[state])
            {
                pair.actions.actionValues.Add(new ActionValue { action = action.Key, value = action.Value });
            }

            serializableQTable.pairs.Add(pair);
        }

        string json = JsonUtility.ToJson(serializableQTable);
        File.WriteAllText(filePath, json);
    }

    public static Dictionary<Vector3Int, Dictionary<string, float>> LoadQTable(string filePath)
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            SerializableQTable serializableQTable = JsonUtility.FromJson<SerializableQTable>(json);

            var qTable = new Dictionary<Vector3Int, Dictionary<string, float>>();
            foreach (var pair in serializableQTable.pairs)
            {
                var actionDict = new Dictionary<string, float>();
                foreach (var actionValue in pair.actions.actionValues)
                {
                    actionDict[actionValue.action] = actionValue.value;
                }
                qTable[pair.state] = actionDict;
            }

            return qTable;
        }
        else
        {
            Debug.LogError("File not found");
            return null;
        }
    }
}

[System.Serializable]
public class SerializableQTable
{
    public List<StateActionPair> pairs = new List<StateActionPair>();
}

[System.Serializable]
public class StateActionPair
{
    public Vector3Int state;
    public SerializableActionDictionary actions;
}

[System.Serializable]
public class SerializableActionDictionary
{
    public List<ActionValue> actionValues = new List<ActionValue>();
}

[System.Serializable]
public class ActionValue
{
    public string action;
    public float value;
}

