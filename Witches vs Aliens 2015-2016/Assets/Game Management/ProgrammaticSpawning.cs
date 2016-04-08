﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ProgrammaticSpawning : MonoBehaviour, IObserver<Message> {
    [SerializeField]
    protected GameObject MainMusicPrefab;
    GameObject spawnedMainMusicPrefab;

    [SerializeField]
    protected GameObject IntroMusicPrefab;

    [SerializeField]
    protected GameObject IntroCountdownPrefab;

    [SerializeField]
    protected float warmupDuration;

    [SerializeField]
    protected AudioClip overtimeClip;

    [SerializeField]
    protected GameObject PuckPrefab;

    [SerializeField]
    protected GameObject CountdownPrefab;

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
    protected float introDuration;

    [SerializeField]
    protected float introCountdownVoiceDelay;

    [SerializeField]
    protected float introPlayerSpawnDelay;

    [SerializeField]
    protected int firstIntroCountdownNumber;

    [SerializeField]
    protected float goalResetDuration;

    [SerializeField]
    protected float goalToPlayerResetTime;

    public SetupData data;
    Vector2[] leftPoints;
    Vector2[] rightPoints;
    Transform[] players;
    public Transform[] Players { get { return players; } }
    List<Transform> leftPlayers = new List<Transform>();
    public List<Transform> LeftPlayers { get { return leftPlayers; } }
    List<Transform> rightPlayers = new List<Transform>();
    public List<Transform> RightPlayers { get { return rightPlayers; } }
    PuckFX puck;
    public Transform Puck { get { return puck.transform; } }
    Transform leftGoal;
    public Transform LeftGoal { get { return LeftGoal; } }
    Transform rightGoal;
    public Transform RightGoal { get { return RightGoal; } }

    // Use this for initialization
    void Awake () {

        if (SetupData.self != null) //workaround for unity level-loading method order
            data = SetupData.self;

        Observers.Subscribe(this, GoalScoredMessage.classMessageType, GameEndMessage.classMessageType, OvertimeMessage.classMessageType);

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
            GameObject spawnedPlayer = (GameObject)Instantiate(data.playerComponentPrefabs[i].character.basePlayer, new Vector2((i+1) * 200, 0), Quaternion.identity); //the positions are temporary
            Stats spawnedStats = spawnedPlayer.AddComponent<Stats>();
            spawnedStats.side = data.playerComponentPrefabs[i].character.side;
            spawnedStats.playerID = data.playerComponentPrefabs[i].playerID;
            spawnedStats.networkMode = data.playerComponentPrefabs[i].bindings.networkMode;
            
            switch (data.playerComponentPrefabs[i].bindings.inputMode)
            {
                case InputConfiguration.PlayerInputType.MOUSE:
                    switch (data.playerComponentPrefabs[i].bindings.networkMode)
                    {
                        case NetworkMode.LOCALCLIENT:
                        case NetworkMode.LOCALSERVER:
                        case NetworkMode.UNKNOWN:
                            spawnedPlayer.AddComponent<MousePlayerInput>().bindings = data.playerComponentPrefabs[i].bindings;
                            break;
                    }
                    break;
                case InputConfiguration.PlayerInputType.JOYSTICK:
                    switch (data.playerComponentPrefabs[i].bindings.networkMode)
                    {
                        case NetworkMode.LOCALCLIENT:
                        case NetworkMode.LOCALSERVER:
                        case NetworkMode.UNKNOWN:
                            spawnedPlayer.AddComponent<JoystickCustomDeadZoneInput>().bindings = data.playerComponentPrefabs[i].bindings;
                            break;
                    }
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
            GameObject visuals = GameObject.Instantiate(data.playerComponentPrefabs[i].character.visuals);
            visuals.transform.SetParent(spawnedPlayer.transform, false);
            IHueShiftableVisuals huedVisuals = visuals.GetComponent<IHueShiftableVisuals>();
            if (huedVisuals != null)
                huedVisuals.shift = data.playerComponentPrefabs[i].characterVisualsVector;
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
	}

    void Start()
    {
        if (data.warmupActive)
        {
            Callback.FireForUpdate(() => resetPositions(1, false), this);
            Callback.FireAndForget(() => StartCoroutine(IntroCountdown()), warmupDuration, this);
        }
        else
        {
            StartCoroutine(IntroCountdown());
        }
    }

    IEnumerator IntroCountdown()
    {
        Queue<float> countdownTimes = new Queue<float>(firstIntroCountdownNumber);
        for (int i = firstIntroCountdownNumber; i > 0; i--)
        {
            countdownTimes.Enqueue(i);
        }
        countdownTimes.Enqueue(-1); //ensure there is always something to Peek();
        yield return null;

        Instantiate(IntroMusicPrefab);

        Callback.FireAndForget(() => Instantiate(IntroCountdownPrefab), introCountdownVoiceDelay, this);

        float timeRemaining = introDuration;

        Callback.FireAndForget(() => resetPositions(introPlayerSpawnDelay), timeRemaining - introPlayerSpawnDelay, this);

        while (timeRemaining > 0)
        {
            yield return null;
            timeRemaining -= Time.deltaTime;
            if(timeRemaining < countdownTimes.Peek())
                SimplePool.Spawn(CountdownPrefab, Vector3.zero).GetComponent<TimerCountdown>().count = countdownTimes.Dequeue().ToString();
        }

        spawnedMainMusicPrefab = Instantiate(MainMusicPrefab);
    }

    public void resetPositions(float duration = -1, bool resetTime = true)
    {
        duration = (duration == -1) ? goalResetDuration : duration; //use goalResetDuration as the default
        leftPoints.Shuffle<Vector2>();
        rightPoints.Shuffle<Vector2>();
        int leftPointsIndex = 0;
        int rightPointsIndex = 0;
        foreach (Transform t in players)
        {
            switch (t.GetComponent<Stats>().side)
            {
                case Side.LEFT:
                    t.GetComponent<ResetScripting>().Reset(leftPoints[leftPointsIndex], duration);
                    leftPointsIndex++;
                    break;
                case Side.RIGHT:
                    t.GetComponent<ResetScripting>().Reset(rightPoints[rightPointsIndex], duration);
                    rightPointsIndex++;
                    break;
            }
        }
        puck.Respawn(puckRespawnPoint.position);
        if(resetTime)
        {
            Callback.FireAndForget(() => timer.running = true, duration, this);
        }
    }

    public void Notify(Message m)
    {
        switch (m.messageType)
        {
            case GoalScoredMessage.classMessageType:
                timer.running = false;
                Callback.FireAndForget(() => resetPositions(), goalToPlayerResetTime, this);
                break;

            case GameEndMessage.classMessageType:
                //add player stats data to the endData, once we figure out what stats to use
                data.Destruct();
                break;
            case OvertimeMessage.classMessageType:
                AudioSource mainMusic = spawnedMainMusicPrefab.GetComponent<AudioSource>();
                mainMusic.clip = overtimeClip;
                mainMusic.Play();
                resetPositions(resetTime : false);
                break;
        }
    }
}
