﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ProgrammaticSpawning : MonoBehaviour, IObserver<Message> {

    [SerializeField]
    protected GameObject PuckPrefab;

    [SerializeField]
    [AutoLink(parentTag = Tags.stage, parentName = "Left")]
    protected Transform leftRespawnPointsParent;
    [SerializeField]
    [AutoLink(parentTag = Tags.stage, parentName = "Right")]
    protected Transform rightRespawnPointsParent;

    [SerializeField]
    [AutoLink(parentTag = Tags.stage, parentName = "PuckRespawnPoint")]
    protected Transform puckRespawnPoint;

    [SerializeField]
    [AutoLink(parentTag = Tags.canvas, parentName = "Timer")]
    protected GameTimer timer;

    [SerializeField]
    protected float resetDuration;

    [SerializeField]
    protected float goalToResetTime;

    public SetupData data;
    Vector2[] leftPoints;
    Vector2[] rightPoints;
    Transform[] players;
    List<Transform> leftPlayers = new List<Transform>();
    List<Transform> rightPlayers = new List<Transform>();
    PuckFX puck;

    // Use this for initialization
    void Awake () {

        if (SetupData.self != null) //workaround for unity level-loading method order
            data = SetupData.self;

        Observers.Subscribe(this, new string[]{GoalScoredMessage.classMessageType});

        leftPoints = new Vector2[leftRespawnPointsParent.childCount];
        int index = 0;
        foreach(Transform child in leftRespawnPointsParent)
        {
            leftPoints[index] = child.position;
            index++;
        }

        index = 0;
        rightPoints = new Vector2[rightRespawnPointsParent.childCount];
        foreach (Transform child in rightRespawnPointsParent)
        {
            rightPoints[index] = child.position;
            index++;
        }

        puck = ((GameObject)(Instantiate(PuckPrefab, puckRespawnPoint.position, Quaternion.identity))).GetComponent<PuckFX>();

        players = new Transform[data.playerComponentPrefabs.Length];
        for (int i = 0; i < data.playerComponentPrefabs.Length; i++)
        {
            GameObject spawnedPlayer = (GameObject)Instantiate(data.playerComponentPrefabs[i].character.basePlayer, i % 2 == 0 ? leftPoints[i % 2] : rightPoints[i % 2], Quaternion.identity); //the positions are temporary
            spawnedPlayer.AddComponent<Stats>().side = data.playerComponentPrefabs[i].character.side;
            switch (data.playerComponentPrefabs[i].bindings.inputMode)
            {
                case InputConfiguration.PlayerInputType.MOUSE:
                    spawnedPlayer.AddComponent<MousePlayerInput>().bindings = data.playerComponentPrefabs[i].bindings;
                    break;
                case InputConfiguration.PlayerInputType.JOYSTICK:
                    spawnedPlayer.AddComponent<JoystickPlayerInput>().bindings = data.playerComponentPrefabs[i].bindings;
                    break;
                case InputConfiguration.PlayerInputType.CRAPAI:
                    spawnedPlayer.AddComponent<CrappyAIInput>().bindings = data.playerComponentPrefabs[i].bindings; //don't really need the bindings; it's an AI
                    break;
                case InputConfiguration.PlayerInputType.INTERPOSEAI:
                    spawnedPlayer.AddComponent<InterposeAI>().bindings = data.playerComponentPrefabs[i].bindings; //don't really need the bindings; it's an AI
                    break;
                case InputConfiguration.PlayerInputType.DEFENSIVEAI:
                    spawnedPlayer.AddComponent<DefensiveAI>().bindings = data.playerComponentPrefabs[i].bindings; //don't really need the bindings; it's an AI
                    break;
            }
            GameObject.Instantiate(data.playerComponentPrefabs[i].character.movementAbility).transform.SetParent(spawnedPlayer.transform, false);
            GameObject.Instantiate(data.playerComponentPrefabs[i].character.genericAbility).transform.SetParent(spawnedPlayer.transform, false);
            GameObject.Instantiate(data.playerComponentPrefabs[i].character.superAbility).transform.SetParent(spawnedPlayer.transform, false);
            players[i] = spawnedPlayer.transform;
            switch (data.playerComponentPrefabs[i].character.side)
            {
                case Side.LEFT:
                    leftPlayers.Add(players[i]);
                    break;
                case Side.RIGHT:
                    rightPlayers.Add(players[i]);
                    break;
            }
        }
        //set up AI/Ability data
        Goal[] goals = GameObject.FindObjectsOfType<Goal>();
        Transform leftGoal = null; //null to get the compiler to shut up about it not being assigned
        Transform rightGoal = null;
        switch(goals[0].side)
        {
            case Side.LEFT:
                leftGoal = goals[0].transform;
                rightGoal = goals[1].transform;
                break;
            case Side.RIGHT:
                rightGoal = goals[0].transform;
                leftGoal = goals[1].transform;
                break;
        }
        for (int i = 0; i < players.Length; i++)
        {
            AbstractPlayerInput input = players[i].GetComponent<AbstractPlayerInput>();
            if (input is IInterferenceAI)
            {
                switch (data.playerComponentPrefabs[i].character.side)
                {
                    case Side.LEFT:
                        (input as IInterferenceAI).myOpponents = rightPlayers;
                        break;
                    case Side.RIGHT:
                        (input as IInterferenceAI).myOpponents = leftPlayers;
                        break;
                }
            }

            if (input is IGoalAI)
            {
                switch (data.playerComponentPrefabs[i].character.side)
                {
                    case Side.LEFT:
                        (input as IGoalAI).myGoal = leftGoal;
                        (input as IGoalAI).opponentGoal = rightGoal;
                        break;
                    case Side.RIGHT:
                        (input as IGoalAI).myGoal = rightGoal;
                        (input as IGoalAI).opponentGoal = leftGoal;
                        break;
                }
            }

            //now abilities

            foreach (AbstractAbility ability in players[i].GetComponentsInChildren<AbstractAbility>())
            {
                if (ability is IAlliesAbility)
                {
                    switch (data.playerComponentPrefabs[i].character.side)
                    {
                        case Side.LEFT:
                            (ability as IAlliesAbility).allies = leftPlayers;
                            break;
                        case Side.RIGHT:
                            (ability as IAlliesAbility).allies = rightPlayers;
                            break;
                    }
                }
                if (ability is IOpponentsAbility)
                {
                    switch (data.playerComponentPrefabs[i].character.side)
                    {
                        case Side.LEFT:
                            (ability as IOpponentsAbility).opponents = rightPlayers;
                            break;
                        case Side.RIGHT:
                            (ability as IOpponentsAbility).opponents = leftPlayers;
                            break;
                    }
                }
                if (ability is IGoalAbility)
                {
                    switch (data.playerComponentPrefabs[i].character.side)
                    {
                        case Side.LEFT:
                            (ability as IGoalAbility).myGoal = leftGoal;
                            (ability as IGoalAbility).opponentGoal = rightGoal;
                            break;
                        case Side.RIGHT:
                            (ability as IGoalAbility).myGoal = rightGoal;
                            (ability as IGoalAbility).opponentGoal = leftGoal;
                            break;
                    }
                }
                if (ability is IPuckAbility)
                {
                    (ability as IPuckAbility).puck = puck.transform;
                }
            }
        }
        Callback.FireForNextFrame(() => resetPositions(), this);
	}

    void resetPositions()
    {
        leftPoints.Shuffle<Vector2>();
        rightPoints.Shuffle<Vector2>();
        int leftPointsIndex = 0;
        int rightPointsIndex = 0;
        foreach (Transform t in players)
        {
            switch (t.GetComponent<Stats>().side)
            {
                case Side.LEFT:
                    t.GetComponent<ResetScripting>().Reset(leftPoints[leftPointsIndex], resetDuration);
                    leftPointsIndex++;
                    break;
                case Side.RIGHT:
                    t.GetComponent<ResetScripting>().Reset(rightPoints[rightPointsIndex], resetDuration);
                    rightPointsIndex++;
                    break;
            }
        }
        puck.Respawn(puckRespawnPoint.position);
        Callback.FireAndForget(() => timer.running = true, resetDuration, this);
    }

    public void Notify(Message m)
    {
        switch (m.messageType)
        {
            case GoalScoredMessage.classMessageType:
                timer.running = false;
                Callback.FireAndForget(() => resetPositions(), goalToResetTime, this);
                break;
        }
    }
}
