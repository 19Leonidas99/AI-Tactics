﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

static class GM
{
    public static int maxMoves = 5;
    public static int maxHP = 25;
    public static string lvlExt = ".map";
    public static float battleSpd = 4;
    public static Transform tilesParent;
    public static float randomProbability = .0f;
    public static int turnSyncer;
    public static int mapSize;
    public static int maxStr = 20;
    public static string platform = "Steam";
    public static KeyCode[][] kc = new KeyCode[][] { new KeyCode[] { KeyCode.BackQuote, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3 }, new KeyCode[] { KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0, KeyCode.Minus } };
    public static int time;
    public static int maxRunAway = 5;
    public static string[] hbName = new string[2];
    public static AI_Config[] intelli = new AI_Config[2];
    public static int nodePerConstant = 4;
    public static float attendence = .88f;
    public static float lR = .01f;
    public static int[] win = new int[2];
    public static int totalRounds = 3;
    public static List<int>[] battleAvgThisMatch = new List<int>[] { new List<int>(), new List<int>()};
    public static int[][] battleAvg = new int[2][];
    public static int currentRound => win[0] + win[1];
    public static float[] explSetter = new float[2];
    public static bool[] nnIsLearning = new bool[] { true, true };

    public static void Init()
    {
        turnSyncer = 0;
        battleAvgThisMatch = new List<int>[] { new List<int>(), new List<int>() };
    }

    public static void FullReset()
    {
        win = new int[2];

        for (int i = 0; i < battleAvg.Length; i++)
        {
            battleAvg[i] = new int[totalRounds];
        }

        Init();
    }

    public static float XYtoDeg(float x, float y)
    {
        return Mathf.Atan2(y, x) * Mathf.Rad2Deg;
    }
}
