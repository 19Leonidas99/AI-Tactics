﻿using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Linq;

public class FightCTRL : MonoBehaviour
{
    public enum Mode { Start, Battle, End };
    public static Mode mode;

    public enum Phase { Wait, Movement, Fight };
    public Phase phase;

    public string levelName;
    public int mMoves = 5;
    public float btSpd;
    [Range(0, 1)]
    public float dist = 1;
    [Range(-1, 1)]
    public float angleX = 0, angleY = 0;
    public GameObject one, two, roundsUI;
    public AudioClip sfx_Walk, sfx_Draw, sfx_StepBack, sfx_Hit, sfx_Crit, sfx_Def, sfx_PowerUp, sfx_PowerDown, sfx_End, sfx_Wrong, sfx_Eat;
    public bool areInBattle;
    public Button start;
    public GameObject[] graphObj, tGraphObj;
    public TextMeshProUGUI[] rounds, txtAiName, txtGraphMax, txtTGraphMax;

    int _turn;
    public int turn => _turn % 2;
    public int gmTurn => GM.turnSyncer % 2;
    public int time => _turn;
    public bool aHumanIsInvolved => humansInvolved[0] || humansInvolved[1];
    public bool[] humansInvolved => _humansInvolved;

    Map map;
    Fighter[] fighter = new Fighter[2];
    AudioSource aS;
    Loc[] outp = new Loc[2];

    float p_D = -1, p_aX = -1, p_aY = -1;
    bool[] _humansInvolved = new bool[2];
    int nextWaffleSpawn;
    int randNextFood => Random.Range(20, 40);
    bool canSpawnWaffle => map.waffleCount < 3;
    Graph[] graph = new Graph[2], totalGraph = new Graph[2];

    public static Material[] txts;

    void Start()
    {
        GenerateAudience(GM.attendence);

        for (int i = 0; i < graph.Length; i++)
        {
            graph[i] = new Graph(graphObj[i], accumulative: true);
            totalGraph[i] = new Graph(tGraphObj[i], accumulative: false);
        }

        string o = "";

        HXB.LoadHaxbot(one, GM.hbName[0]);
        HXB.LoadHaxbot(two, GM.hbName[1]);

        // 
        AddToNextWaffleSpawn();

        // 
        txts = new Material[] { Resources.Load<Material>("Mats/attack"), Resources.Load<Material>("Mats/defend"), Resources.Load<Material>("Mats/taunt") };

        // 
        map = new Map(this, levelName);

        // We dont want to see this pesky button in the heat of battle
        if (GM.currentRound > 0)
        {
            start.gameObject.SetActive(false);
        }
        else
        {
            for (int i = 0; i < totalGraph.Length; i++)
            {
                totalGraph[i] = new Graph(tGraphObj[i], accumulative: false);
            }

            GM.FullReset();

            map.SetCamTo(Map.CamMode.Field);
        }

        // 
        for (int i = 0; i < 2; i++)
        {
            GM.intelli[i].SetShowoff(GM.explSetter[i]);
            txtAiName[i].text = $"{GM.intelli[i].aiName}";
        }

        // 
        fighter[0] = new Fighter(one, 0, map.mapSize, GM.intelli[0]);
        fighter[1] = new Fighter(two, 1, map.mapSize, GM.intelli[1]);

        // 
        for (int i = 0; i < fighter.Length; i++)
        {
            if (fighter[i].config == null)
            {
                humansInvolved[i] = true;
            }
        }

        // 
        fighter[0].SetOpponent(fighter[1]);
        fighter[1].SetOpponent(fighter[0]);

        // 
        fighter[0].LookAtOpponent();
        fighter[1].LookAtOpponent();

        // 
        map.SetFighters(fighter);

        // 
        aS = gameObject.AddComponent<AudioSource>();

        // 
        for (int i = 0; i < 2; i++)
        {
            outp[i] = Map.OutputLocation(map, fighter[i].expression, fighter[i == 0 ? 1 : 0].expression, dist, angleX, angleY);
        }

        // 
        foreach (Tile ve in map.loc)
        {
            if (!ve.free)
            {
                Color c = ve.type == 'w' ? new Color(.75f, .25f, 0) : Color.black;
                GameObject g = null;
            }
        }
    }

    void GenerateAudience(float attendence)
    {
        int seats = 30, rows = 7;

        for (int j = 0; j < rows; j++)
        {
            for (int i = 0; i < seats; i++)
            {
                float dice = Random.value;

                if (dice < attendence)
                {
                    GameObject ob = Instantiate(Resources.Load<GameObject>("Objs/aud"));
                    float where = (Mathf.PI * 2 * i) / seats;
                    float r = 16.5f + (2 * j);
                    ob.transform.position = new Vector3(Mathf.Sin(where) * r, 2.9f + (.6f * j), Mathf.Cos(where) * r);
                    ob.transform.LookAt(Vector3.zero);
                }
            }
        }
    }

    void AddToNextWaffleSpawn()
    {
        nextWaffleSpawn += randNextFood;
    }

    void Update()
    {
        GM.battleSpd = btSpd;

        GM.maxMoves = mMoves;

        GM.time = time;

        // 
        if (_turn == nextWaffleSpawn)
        {
            SpawnWaffle();
        }

        // 
        if (mode == Mode.Battle)
        {
            // 
            if (_turn == GM.turnSyncer)
            {
                ProcessTurn();
            }
        }

        // 
        ListenForControls();
    }

    void SpawnWaffle()
    {
        if (canSpawnWaffle)
        {
            // 
            GameObject food = Instantiate(Resources.Load<GameObject>("Objs/Waffle"));
            // 
            int tileID = Random.Range(0, map.freeTiles.Count);
            // 
            Tile tile = map.freeTiles[tileID];
            // 
            tile.SetDressing(food);
            // 
            Vector2Int loc = tile.expression;
            // 
            map.waffleTiles.Add(tile);

            // 
            map.freeTiles.RemoveAt(tileID);
            // 
            food.transform.position = new Vector3(loc.x, 1, loc.y);
            // 
        }

        AddToNextWaffleSpawn();
    }

    void ProcessTurn()
    {
        phase = Phase.Movement;

        TakeTurn();

        // 
        if (outp[gmTurn].path != null)
        {
            List<Tile> tempP = new List<Tile>(outp[gmTurn].path.path);

            // 
            if (!areInBattle)
            {
                // 
                for (int i = 0; i < outp[gmTurn].loc.Count; i++)
                {
                    outp[gmTurn].loc[i].ToggleRender(true, (Color.blue + Color.red) / 2);
                }

                // 
                for (int i = 0; i < outp[gmTurn].selLoc.Count; i++)
                {
                    outp[gmTurn].selLoc[i].ToggleRender(true, i == outp[gmTurn].angleSelect ? Color.yellow : Color.cyan);

                    // 
                    if (i == outp[gmTurn].angleSelect)
                    {
                        tempP.Remove(outp[gmTurn].selLoc[i]);
                    }
                }

                // 
                for (int i = 0; i < tempP.Count; i++)
                {
                    tempP[i].ToggleRender(true, Color.white);
                }
            }
            else
            {
                map.ResetAllTiles();
            }
        }
    }

    public void UpdateGraph(int who, int r)
    {
        graph[who].AddValueToGraph(r);
        graph[who].UpdateGraph();

        txtGraphMax[who].text = $"{graph[who].performance}";
    }

    public void PlaySFX(string c)
    {
        AudioClip ac = null;

        if (c == "walk")
        {
            ac = sfx_Walk;
        }
        else if (c == "draw")
        {
            ac = sfx_Draw;
        }
        else if (c == "stepback")
        {
            ac = sfx_StepBack;
        }
        else if (c == "hit")
        {
            ac = sfx_Hit;
        }
        else if (c == "def")
        {
            ac = sfx_Def;
        }
        else if (c == "crit")
        {
            ac = sfx_Crit;
        }
        else if (c == "powerup")
        {
            ac = sfx_PowerUp;
        }
        else if (c == "powerdown")
        {
            ac = sfx_PowerDown;
        }
        else if (c == "end")
        {
            ac = sfx_End;
        }
        else if (c == "wrong")
        {
            ac = sfx_Wrong;
        }
        else if (c == "eat")
        {
            ac = sfx_Eat;
        }

        aS.clip = ac;
        aS.Play();
    }

    void ListenForControls()
    {
        // 
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            Restart();
        }

        // 
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            mode = Mode.Start;
            GM.FullReset();
            SceneManager.LoadScene("mainMenu");
        }

        // 
        if (Input.GetKeyDown(KeyCode.Period))
        {
            fighter[0].DMGTEMP(25);
        }

        // 
        if (Input.GetKeyDown(KeyCode.Space) && mode != Mode.End)
        {
            mode = Mode.Battle;
        }

        // 
        if (Input.GetKeyDown(KeyCode.F))
        {
            map.SetCamTo(Map.CamMode.Field);
        }

        // 
        if (Input.GetKeyDown(KeyCode.T))
        {
            map.SetCamTo(Map.CamMode.Topdown);
        }

        // 
        if (Input.GetKeyDown(KeyCode.I))
        {
            map.SetCamTo(Map.CamMode.Isometric);
        }

        // 
        if (Input.GetKeyDown(KeyCode.A))
        {
            map.SetCamTo(Map.CamMode.Action);
        }

        // 
        if (Input.GetKeyDown(KeyCode.H))
        {
            map.SetCamTo(Map.CamMode.IsoAction);
        }
    }

    public void SetCam(int c)
    {
        map.SetCamTo((Map.CamMode)c);
    }

    public void Restart()
    {
        mode = Mode.Start;
        GM.Init();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void TakeTurn()
    {
        //print(fighter[turn].stateData.PrintState());

        // At the start of the turn, do a checkup on the fighters
        for (int i = 0; i < fighter.Length; i++)
        {
            fighter[i].stateData.Update(turn);
            fighter[i].CheckUp(map);
        }

        // We must reset the tile renderings
        map.ResetAllTiles();

        // 
        if (humansInvolved[turn])
        {
            if (p_D != dist || p_aX != angleX || p_aY != angleY)
            {
                // Get the movement data
                outp[turn] = Map.OutputLocation(map, fighter[turn].expression, fighter[turn == 0 ? 1 : 0].expression, dist, angleX, angleY);
                p_D = dist;
                p_aX = angleX;
                p_aY = angleY;
            }

            // Set camera to turnee
            map.SetCamTo(Map.camMode, turn);

            // 
            if (fighter[turn].isStunned)
            {
                StartCoroutine(map.FIGHT(time, map, turn));

                // incriment the turn
                _turn++;

                p_D = -1;
                p_aY = -1;
                p_aX = -1;
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    // start our Coroutine of moving our fighter
                    StartCoroutine(map.MoveFighter(time, outp[turn].loc.Count > 0, map, turn, GM.battleSpd, outp[turn].path));

                    // incriment the turn
                    _turn++;

                    p_D = -1;
                    p_aY = -1;
                    p_aX = -1;
                }
                else if (Input.GetKeyDown(KeyCode.Backspace))
                {
                    StartCoroutine(map.FIGHT(time, map, turn));

                    // incriment the turn
                    _turn++;

                    p_D = -1;
                    p_aY = -1;
                    p_aX = -1;
                }
            }
        }
        else
        {
            // 
            OutputMakeMove s = OutputMakeMove.CalculateNormal(fighter[turn]);

            // Calculate if moving
            if (s.stay || fighter[turn].isStunned)
            {
                // calculate if AI even wants to battle
                OutputToBattle oB = OutputToBattle.CalculateOutput(fighter[turn]);

                // if the result of oB is to battle,
                if (oB.toBattle)
                {
                    // start the coroutine to start the fight
                    StartCoroutine(map.FIGHT(time, map, turn));
                }
                else // otherwise we'll just entirely skip its turn
                {
                    GM.turnSyncer++;
                }

                // incriment the turn
                _turn++;
            }
            else
            {
                // Calculate the move output
                OutputMove m = OutputMove.CalculateOutput(fighter[turn]);

                // 
                if (m.distance >= .5f)
                {
                    fighter[turn].RanAway();
                }

                // Get the movement data
                outp[turn] = Map.OutputLocation(map, fighter[turn].expression, fighter[turn == 0 ? 1 : 0].expression, m.distance, m.angleX, m.angleY);

                // start our Coroutine of moving our fighter
                StartCoroutine(map.MoveFighter(time, outp[turn].loc.Count > 0, map, turn, GM.battleSpd, outp[turn].path));

                // incriment the turn
                _turn++;
            }
        }
    }

    public void EnableRoundsUI()
    {
        for (int i = 0; i < fighter.Length; i++)
        {
            rounds[i].text = $"{GM.win[i]} / {GM.totalRounds} - {Mathf.Round(100 * ((float)GM.win[i] / GM.totalRounds))}%";
            GM.battleAvg[i][GM.currentRound-1] = (int)GM.battleAvgThisMatch[i].Sum();

            // 
            for (int j = 0; j < GM.currentRound; j++)
            {
                totalGraph[i].AddValueToGraph(GM.battleAvg[i][j]);
            }

            totalGraph[i].UpdateGraph();
            txtTGraphMax[i].text = $"{(Mathf.Round(totalGraph[i].performance*1000))/1000}";
        }

        roundsUI.gameObject.SetActive(true);

        StartCoroutine(NextRound());
    }

    IEnumerator NextRound()
    {
        yield return new WaitForSeconds(5);

        bool stillBattling = (GM.win[0] + GM.win[1]) < GM.totalRounds;

        // 
        if (stillBattling)
        {
            Restart();
            StartBattle();
        }
    }

    public void StartBattle()
    {
        mode = Mode.Battle;
        start.gameObject.SetActive(false);
    }

    public void StartABattle(int who)
    {
        if (humansInvolved[who])
        {
            StartCoroutine(map.FIGHT(time, map, who));
        }
        else
        {
            // calculate if AI even wants to battle
            OutputToBattle oB = OutputToBattle.CalculateOutput(fighter[who]);

            // if the result of oB is to battle,
            if (oB.toBattle)
            {
                StartCoroutine(map.FIGHT(time, map, who));
            }
            else // otherwise we'll just entirely skip its turn
            {
                GM.turnSyncer++;

                fighter[who].CheckIfRanTooMuch();
            }
        }
    }
}

public class Loc
{
    public List<Tile> loc;
    public List<Tile> selLoc;
    public TilePath path;
    public int angleSelect;

    public Loc(List<Tile> loc, List<Tile> selLoc, TilePath path, int angleSelect)
    {
        this.loc = loc;
        this.selLoc = selLoc;
        this.path = path;
        this.angleSelect = angleSelect;
    }
}