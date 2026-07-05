using System;
using System.IO;
using UnityEngine;

public class GameDataCollector : MonoBehaviour
{
    private FireSafetyData playerData;
    private float gameStartTime;
    private bool gameStarted = false;
    private bool gameEnded = false;

    private int currentActionStep = 0;
    private bool sequenceError = false;

    private bool extinguisherAlreadyPicked = false;
    private bool hasSentData = false;

    [SerializeField] private FireSafetyAPI fireSafetyAPI;

    void Start()
    {
        fireSafetyAPI = FindFirstObjectByType<FireSafetyAPI>();

        if (fireSafetyAPI == null)
            Debug.LogError("FireSafetyAPI not found!");
    }

    public void StartGame()
    {
        playerData = new FireSafetyData();

        gameStartTime = Time.time;
        gameStarted = true;
        gameEnded = false;

        currentActionStep = 0;
        sequenceError = false;
        extinguisherAlreadyPicked = false;
        hasSentData = false;

        Debug.Log("Game Started");
    }

    private void CheckSequence(int expectedStep)
    {
        if (!gameStarted || gameEnded) return;

        if (currentActionStep != expectedStep)
        {
            sequenceError = true;
            Debug.LogWarning($"Sequence Error: expected {expectedStep} but got {currentActionStep}");
        }
    }

    public void OnCheckFire()
    {
        CheckSequence(0);

        currentActionStep = 1;
        playerData.Is_Checked_Fire = 1;

        Debug.Log("Checked Fire");
    }

    public void OnAlarm()
    {
        CheckSequence(1);

        currentActionStep = 2;
        playerData.Is_Alarm_On = 1;

        Debug.Log("Alarm Activated");
    }

    public void OnPowerCut()
    {
        CheckSequence(2);

        currentActionStep = 3;
        playerData.Is_Power_Off = 1;

        Debug.Log("Power Cut");
    }

    public void OnPickExtinguisher(bool isCorrect)
    {
        if (!gameStarted || gameEnded) return;

        
        if (!extinguisherAlreadyPicked)
        {
            CheckSequence(3);
            currentActionStep = 4;
            extinguisherAlreadyPicked = true;
        }

        
        if (isCorrect)
        {
            playerData.Used_Correct_Capsule = 1;
            Debug.Log("Correct Extinguisher Picked (Recorded)");
        }
        else
        {
            playerData.Used_Wrong_Capsule = 1;
            Debug.Log("Wrong Extinguisher Picked (Recorded)");
        }
    }

    public void OnRemovePin()
    {
        if (!gameStarted || gameEnded) return;

        CheckSequence(4);

        currentActionStep = 5;

        Debug.Log("Pin Removed");
    }

    public void OnFireOut(bool success)
    {
        if (!gameStarted || gameEnded) return;

        CheckSequence(5);

        currentActionStep = 6;

        if (success)
        {
            playerData.Is_Fire_Off = 1;
            Debug.Log("Fire Extinguished");
        }
        else
        {
            playerData.Is_Fire_Bigger = 1;
            Debug.Log("Fire Became Bigger");
        }
    }

    public void OnOpenDoor()
    {
        if (!gameStarted || gameEnded) return;

        CheckSequence(6);

        currentActionStep = 7;
        playerData.Is_Door_Open = 1;

        Debug.Log("Door Opened");
    }

    public void OnExit(bool reachedSafeZone)
    {
        if (!gameStarted || gameEnded) return;

        CheckSequence(7);

        currentActionStep = 8;
        gameEnded = true;

        if (reachedSafeZone)
        {
            playerData.Is_Finish_Level = 1;
            Debug.Log("Player Reached Safe Zone");
        }
        else
        {
            playerData.Is_Finish_Level = 0;
            Debug.Log("Player Failed to Reach Safe Zone");
        }

        playerData.Total_Time = Time.time - gameStartTime;

        if (sequenceError)
            playerData.Is_Correct_Sequence = 0;
        else
            playerData.Is_Correct_Sequence = 1;

        SendDataToAlgorithm();
    }

    public void SendDataToAlgorithm()
    {
        if (hasSentData) return;

        hasSentData = true;

        fireSafetyAPI.GetScore(playerData,
            (score) =>
            {
                Debug.Log($"Score: {score}");

                
                playerData.Final_Score_me = (float)score;

                string level = FireSafetyAPI.LastSafetyLevel;
                string[] feedback = FireSafetyAPI.LastFeedback;

                Debug.Log($"Safety Level: {level}");

                foreach (string tip in feedback)
                    Debug.Log($"Tip: {tip}");

                FindAnyObjectByType<FireSafetyReportUI>().ShowReport(score, level, feedback, playerData.Total_Time);


                
                SaveDataToCSV(playerData);


            },
            (error) =>
            {
                Debug.LogError($"API Error: {error}");
            });
    }

    public FireSafetyData GetFinalPlayerData()
    {
        return playerData;
    }

    public bool DidGameEnd()
    {
        return gameEnded;
    }

    public void SaveDataToCSV(FireSafetyData data)
    {
        string path = Application.dataPath + "/My_fire_dataset.csv";

        bool fileExists = File.Exists(path);

        using (StreamWriter sw = new StreamWriter(path, append: true))
        {
            if (!fileExists)
            {
                sw.WriteLine("Is_Checked_Fire,Is_Alarm_On,Is_Power_Off,Used_Correct_Capsule,Used_Wrong_Capsule,Is_Fire_Bigger,Is_Fire_Off,Is_Door_Open,Is_Correct_Sequence,Is_Finish_Level,Total_Time,Final_Score");
            }

            string row =
                data.Is_Checked_Fire + "," +
                data.Is_Alarm_On + "," +
                data.Is_Power_Off + "," +
                data.Used_Correct_Capsule + "," +
                data.Used_Wrong_Capsule + "," +
                data.Is_Fire_Bigger + "," +
                data.Is_Fire_Off + "," +
                data.Is_Door_Open + "," +
                data.Is_Correct_Sequence + "," +
                data.Is_Finish_Level + "," +
                data.Total_Time + "," +
                data.Final_Score_me;

            sw.WriteLine(row);
        }

        Debug.Log("CSV Updated: " + path);
    }
}