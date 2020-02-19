﻿using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

class FightCTRL : MonoBehaviour
{
    public enum Phase { Start, Battle, End };
    public static Phase phase;

    public string levelName;
    public int mMoves = 5;
    public bool randomOutputs;
    public float btSpd;
    [Range(0, 1)]
    public float dist = 1;
    [Range(-1, 1)]
    public float angleX = 0, angleY = 0;
    public GameObject one, two;

    int _turn;
    public int turn => _turn % 2;
    public int time => _turn;

    int angleSelect;
    Map map;
    List<Tile> selLoc = new List<Tile>();
    List<Tile> loc = new List<Tile>();
    TilePath p;
    Fighter[] fighter = new Fighter[2];

    void Start()
    {
        // 
        map = new Map(levelName);

        // 
        fighter[0] = new Fighter(one, 0, map.mapSize);
        fighter[1] = new Fighter(two, 1, map.mapSize);

        // 
        fighter[0].SetOpponent(fighter[1]);
        fighter[1].SetOpponent(fighter[0]);

        // 
        map.SetFighters(fighter);

        // 
        GameObject blocksParent = new GameObject("Blocks");

        // 
        foreach (Tile ve in map.loc)
        {
            if (!ve.free)
            {
                Color c = ve.type == 'w' ? new Color(.75f, .25f, 0) : Color.black;
                GameObject g = null;

                // 
                if (ve.type == 'p')
                {
                    g = Instantiate(Resources.Load<GameObject>("Objs/Pillar"));
                    g.transform.position = new Vector3(ve.x - map.halfMapSize, .5f, ve.y - map.halfMapSize);
                }
                else if (ve.type == 'w')
                {
                    g = Instantiate(Resources.Load<GameObject>("Objs/Wall"));
                    g.transform.localScale = new Vector3(1, 2, 1);
                    g.transform.position = new Vector3(ve.x - map.halfMapSize, 1.5f, ve.y - map.halfMapSize);
                }

                // 
                g.transform.SetParent(blocksParent.transform);
            }
        }
    }

    void Update()
    {
        GM.battleSpd = btSpd;

        GM.maxMoves = mMoves;

        if (phase == Phase.Battle)
        {
            // 
            if (_turn == GM.turnSyncer)
            {
                TakeTurn();
            }

            // 
            for (int i = 0; i < loc.Count; i++)
            {
                loc[i].ToggleRender(true, (Color.blue + Color.red) / 2);
            }

            // 
            for (int i = 0; i < selLoc.Count; i++)
            {
                selLoc[i].ToggleRender(true, i == angleSelect ? Color.yellow : Color.cyan);
            }
        }

        // 
        ListenForControls();
    }

    void ListenForControls()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            GM.turnSyncer = 0;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            phase = Phase.Battle;
        }
    }

    void TakeTurn()
    {
        // At the start of the turn, do a checkup on the fighters
        for (int i = 0; i < fighter.Length; i++)
        {
            fighter[i].stateData.Update(turn);
            fighter[i].CheckUp(time);
        }

        // We must reset the tile rendering
        for (int i = 0; i < map.loc.GetLength(0); i++)
        {
            for (int j = 0; j < map.loc.GetLength(0); j++)
            {
                map.loc[i, j].ToggleRender(false, Color.clear);
            }
        }

        //// 
        //OutputRest r = OutputRest.Calculate();

        //// Calculate if Resting
        //if (r.rest)
        //{
        //    fighter[turn].Rest(time);
        //GM.turnSyncer++;
        //    _turn++;
        //    return;
        //}


        // Calculate the move output
        OutputMove m = OutputMove.CalculateOutput(fighter[turn].stateData);

        // Get the movement data
        (List<Tile> loc, List<Tile> selLoc, TilePath path, int angleSelect) outp = Map.OutputLocation(map, fighter[turn].expression, fighter[turn == 0 ? 1 : 0].expression, randomOutputs ? m.distance : dist, m.angleX, m.angleY);

        // set all of the movement data
        p = outp.path;
        loc = outp.loc;
        selLoc = outp.selLoc;
        angleSelect = outp.angleSelect;

        // start our Coroutine of moving our fighter
        StartCoroutine(map.MoveFighter(time, turn, GM.battleSpd, p));

        // incriment the turn
        _turn++;
    }

    void OnDrawGizmos2()
    {
        if (Application.isPlaying)
        {
            if (p != null)
            {
                for (int i = 0; i < p.path.Count; i++)
                {
                    Gizmos.color = i == p.path.Count - 1 ? Color.yellow : Color.white;
                    Gizmos.DrawWireCube(new Vector3(p.path[i].eX, 1, p.path[i].eY), Vector3.one);
                }
            }
        }
    }
}