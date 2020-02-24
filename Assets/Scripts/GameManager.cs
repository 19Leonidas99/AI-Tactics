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
    public static FightCTRL.InputType[] inpType = new FightCTRL.InputType[2]; 

    public static float XYtoDeg(float x, float y)
    {
        return Mathf.Atan2(y, x) * Mathf.Rad2Deg;
    }
}
