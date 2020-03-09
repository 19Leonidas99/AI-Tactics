﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public struct AI
{
    public static float Sigmoid(float x)
    {
        return 1 / (1 + Mathf.Exp(-x));
    }

    public static float Sinh(float x)
    {
        return (Mathf.Exp(x) - Mathf.Exp(-x)) / 2;
    }

    public static float Cosh(float x)
    {
        return (Mathf.Exp(x) + Mathf.Exp(-x)) / 2;
    }

    public static float Tanh(float x)
    {
        return (float)Math.Tanh((double)x);
    }

    public static float Softsign(float x)
    {
        return x / (1 + Math.Abs(x));
    }

    public static float ReLu(float x)
    {
        return Mathf.Clamp(x, 0, x);
    }

    public static float Σ(params float[][] s)
    {
        float ret = 0;


        for (int i = 0; i < s.Length; i++)
        {
            for (int j = 0; j < s[0].Length; j++)
            {
                ret += s[i][j] * (i + 1 < s.Length ? s[i + 1][j] : 1);
            }
        }

        return ret;
    }
}

public struct OutputMove
{
    float _distance, _angleX, _angleY;

    public float distance => _distance;
    public float angleX => _angleX;
    public float angleY => _angleY;

    OutputMove(float d, float aX, float aY)
    {
        _distance = d;
        _angleX = aX;
        _angleY = aY;
    }

    /// <summary>
    /// this takes in state data, & spits out a prediction on the best move to make
    /// </summary>
    /// <param name="inp"></param>
    /// <returns></returns>
    public static OutputMove CalculateOutput(Fighter fighter)
    {
        float d = UnityEngine.Random.value;
        float aX = UnityEngine.Random.Range(-1f, 1f);
        float aY = UnityEngine.Random.Range(-1f, 1f);

        AI_Config conf = fighter.config;
        StateData state = fighter.stateData;

        // 
        if (conf.neuralNetwork[1])
        {
            // get a ref to how many inputs will we be using for the move output network
            int inpCount = state.fullState.Length;
            // ref to number of output nodes for move network
            int numOutputNodes = 3;

            // ref to output nodes
            float[] o = new float[numOutputNodes];
            // ref to hidden layer nodes
            float[] hL = new float[numOutputNodes * GM.nodePerConstant];
            // ref to hidden layer node weights
            float[][] wHL = new float[numOutputNodes * GM.nodePerConstant][];
            // ref to output layer weights
            float[][] wO = new float[numOutputNodes][];
            // ref to the when the hidden layer weights end
            int lastHLW = numOutputNodes * GM.nodePerConstant * inpCount;

            // init our hidden layer nodes wieghts
            for (int i = 0; i < wHL.Length; i++)
            {
                wHL[i] = new float[inpCount];
            }

            // init our output nodes wieghts
            for (int i = 0; i < wO.Length; i++)
            {
                wO[i] = new float[hL.Length];
            }

            // convert config string NN into 12 float arrays
            for (int j = 0; j < wHL.Length; j++)
            {
                for (int i = 0; i < inpCount; i++)
                {
                    wHL[j][i] = float.Parse(conf.movementNN[(inpCount * j) + (i)]);
                }
            }

            // convert the remaining config string NN into 3 float arrays
            for (int j = 0; j < wO.Length; j++)
            {
                for (int i = 0; i < wHL.Length; i++)
                {
                    wO[j][i] = float.Parse(conf.movementNN[lastHLW + (wHL.Length * j) + i]);
                }
            }

            // calculate the hidden layer nodes output
            for (int i = 0; i < hL.Length; i++)
            {
                hL[i] = AI.ReLu(AI.Σ(state.fullState, wHL[i]) + 0);
            }

            // calculate the output nodes output
            for (int i = 0; i < o.Length; i++)
            {
                o[i] = AI.Σ(hL, wO[i]);
            }

            // squash & set the the final values for returning
            d = AI.Sigmoid(o[0]);
            aX = AI.Tanh(o[1]);
            aY = AI.Tanh(o[2]);

            Debug.Log($"{fighter.name} dXY: {d},{aX},{ aY}");
        }
        else
        {
            float dice = UnityEngine.Random.value;

            d = dice < conf.movementTrad ? 0 : 1;
        }

        return new OutputMove(d, aX, aY);
    }
}

public struct OutputToBattle
{
    float _decision;
    public bool toBattle => _decision > .5f;

    OutputToBattle(float d)
    {
        _decision = d;
    }

    public static OutputToBattle CalculateOutput(Fighter fighter)
    {
        float ret = UnityEngine.Random.value;

        AI_Config conf = fighter.config;
        StateData state = fighter.stateData;

        // 
        if (conf.neuralNetwork[2])
        {
            // get a ref to how many inputs will we be using for the To Battle output network
            int inpCount = state.fullState.Length;
            // ref to number of output nodes for To Battle network
            int numOutputNodes = 1;

            // ref to output nodes
            float[] o = new float[numOutputNodes];
            // ref to hidden layer nodes
            float[] hL = new float[numOutputNodes * GM.nodePerConstant];
            // ref to hidden layer node weights
            float[][] wHL = new float[numOutputNodes * GM.nodePerConstant][];
            // ref to output layer weights
            float[][] wO = new float[numOutputNodes][];
            // ref to the when the hidden layer weights end
            int lastHLW = numOutputNodes * GM.nodePerConstant * inpCount;

            // init our hidden layer nodes wieghts
            for (int i = 0; i < wHL.Length; i++)
            {
                wHL[i] = new float[inpCount];
            }

            // init our output nodes wieghts
            for (int i = 0; i < wO.Length; i++)
            {
                wO[i] = new float[hL.Length];
            }

            // convert config string NN into 12 float arrays
            for (int j = 0; j < wHL.Length; j++)
            {
                for (int i = 0; i < inpCount; i++)
                {
                    wHL[j][i] = float.Parse(conf.battleNN[(inpCount * j) + (i)]);
                }
            }

            // convert the remaining config string NN into 3 float arrays
            for (int j = 0; j < wO.Length; j++)
            {
                for (int i = 0; i < wHL.Length; i++)
                {
                    //Debug.Log($"{lastHLW + (wHL.Length * j) + i}");
                    wO[j][i] = float.Parse(conf.battleNN[lastHLW + (wHL.Length * j) + i]);
                }
            }

            // calculate the hidden layer nodes output
            for (int i = 0; i < hL.Length; i++)
            {
                hL[i] = AI.ReLu(AI.Σ(state.fullState, wHL[i]) + 0);
            }

            // calculate the output nodes output
            for (int i = 0; i < o.Length; i++)
            {
                o[i] = AI.Σ(hL, wO[i]);
            }

            // squash & set the the final values for returning
            ret = AI.Sigmoid(o[0]);

            Debug.Log($"{fighter.name} Battle: {ret}");
        }
        else
        {
            float dice = UnityEngine.Random.value;

            ret = dice < conf.battleTrad ? 1 : 0;
        }

        return new OutputToBattle(ret);
    }
}

public struct OutputAttack
{
    float _attack, _defend, _taunt;
    public float attack => _attack;
    public float defend => _defend;
    public float taunt => _taunt;

    float[] votes => new float[] { attack, defend, taunt };
    public int decision => Decision();
    public string choice => decision == 0 ? "Attack" : decision == 1 ? "Defend" : "Taunt";

    OutputAttack(float a, float d, float t)
    {
        _attack = a;
        _defend = d;
        _taunt = t;
    }

    public void DEBUGSetVals(float a, float d, float t)
    {
        _attack = a;
        _defend = d;
        _taunt = t;
    }

    public static OutputAttack CalculateOutput(bool stunned, Fighter fighter)
    {
        float a = UnityEngine.Random.value;
        float d = UnityEngine.Random.Range(0, 1 - a);
        float t = 1 - (a + d);

        AI_Config conf = fighter.config;
        StateData state = fighter.stateData;

        // 
        if (conf.neuralNetwork[3])
        {
            // get a ref to how many inputs will we be using for the attack output network
            int inpCount = state.fullState.Length;
            // ref to number of output nodes for attack network
            int numOutputNodes = 3;

            // ref to output nodes
            float[] o = new float[numOutputNodes];
            // ref to hidden layer nodes
            float[] hL = new float[numOutputNodes * GM.nodePerConstant];
            // ref to hidden layer node weights
            float[][] wHL = new float[numOutputNodes * GM.nodePerConstant][];
            // ref to output layer weights
            float[][] wO = new float[numOutputNodes][];
            // ref to the when the hidden layer weights end
            int lastHLW = numOutputNodes * GM.nodePerConstant * inpCount;

            // init our hidden layer nodes wieghts
            for (int i = 0; i < wHL.Length; i++)
            {
                wHL[i] = new float[inpCount];
            }

            // init our output nodes wieghts
            for (int i = 0; i < wO.Length; i++)
            {
                wO[i] = new float[hL.Length];
            }

            // convert config string NN into 12 float arrays
            for (int j = 0; j < wHL.Length; j++)
            {
                for (int i = 0; i < inpCount; i++)
                {
                    wHL[j][i] = float.Parse(conf.attackNN[(inpCount * j) + (i)]);
                }
            }

            // convert the remaining config string NN into 3 float arrays
            for (int j = 0; j < wO.Length; j++)
            {
                for (int i = 0; i < wHL.Length; i++)
                {
                    wO[j][i] = float.Parse(conf.attackNN[lastHLW + (wHL.Length * j) + i]);
                }
            }

            // calculate the hidden layer nodes output
            for (int i = 0; i < hL.Length; i++)
            {
                hL[i] = AI.ReLu(AI.Σ(state.fullState, wHL[i]) + 0);
            }

            // calculate the output nodes output
            for (int i = 0; i < o.Length; i++)
            {
                o[i] = AI.Σ(hL, wO[i]);
            }

            // squash & set the the final values for returning
            a = AI.Sigmoid(o[0]);
            d = AI.Sigmoid(o[1]);
            t = AI.Sigmoid(o[2]);

            Debug.Log($"{fighter.name} adt: {a},{d},{ t}");
        }
        else
        {
            a = UnityEngine.Random.Range(0, conf.attackTrad);
            d = UnityEngine.Random.Range(0, conf.defendTrad);
            t = UnityEngine.Random.Range(0, conf.tauntTrad);
        }

        // MOVE IS STUNNED FUNCT INTO HERE INSTEAD, PASS IN BOOL
        if (stunned)
        {
            // check if defense is higher than taunt
            if (d > t)
            {
                a = 0;
                d = 1;
                t = 0;
            }
            // else that means that taunt is higher than defense, & we can set taunt
            else
            {
                a = 0;
                d = 0;
                t = 1;
            }

            return new OutputAttack(a, d, t);
        }

        // 
        return new OutputAttack(a, d, t);
    }

    public static OutputAttack ForceOutput(int w)
    {
        float[] ret = new float[3];

        ret[w] = 1;

        return new OutputAttack(ret[0], ret[1], ret[2]);
    }

    int Decision()
    {
        int ret = 0;
        float highest = 0;

        for (int i = 0; i < votes.Length; i++)
        {
            if (votes[i] > highest)
            {
                ret = i;
                highest = votes[i];
            }
        }

        return ret;
    }
}

public struct OutputMakeMove
{
    float _stay;
    public bool stay => UnityEngine.Mathf.Round(_stay) == 0;

    public OutputMakeMove(float r)
    {
        _stay = r;
    }

    public static OutputMakeMove CalculateNormal(Fighter fighter)
    {
        float ret = UnityEngine.Random.value;

        AI_Config conf = fighter.config;
        StateData state = fighter.stateData;

        // when fighter is stunned, it has to stay put
        if (fighter.isStunned)
        {
            return new OutputMakeMove(0);
        }

        // 
        if (conf.neuralNetwork[0])
        {
            // get a ref to how many inputs will we be using for the stay output network
            int inpCount = state.fullState.Length;
            // ref to number of output nodes for stay network
            int numOutputNodes = 1;

            // ref to output nodes
            float[] o = new float[numOutputNodes];
            // ref to hidden layer nodes
            float[] hL = new float[numOutputNodes * GM.nodePerConstant];
            // ref to hidden layer node weights
            float[][] wHL = new float[numOutputNodes * GM.nodePerConstant][];
            // ref to output layer weights
            float[][] wO = new float[numOutputNodes][];
            // ref to the when the hidden layer weights end
            int lastHLW = numOutputNodes * GM.nodePerConstant * inpCount;

            // init our hidden layer nodes wieghts
            for (int i = 0; i < wHL.Length; i++)
            {
                wHL[i] = new float[inpCount];
            }

            // init our output nodes wieghts
            for (int i = 0; i < wO.Length; i++)
            {
                wO[i] = new float[hL.Length];
            }

            // convert config string NN into 12 float arrays
            for (int j = 0; j < wHL.Length; j++)
            {
                for (int i = 0; i < inpCount; i++)
                {
                    wHL[j][i] = float.Parse(conf.stayNN[(inpCount * j) + (i)]);
                }
            }


            // convert the remaining config string NN into 3 float arrays
            for (int j = 0; j < wO.Length; j++)
            {
                for (int i = 0; i < wHL.Length; i++)
                {
                    //Debug.Log($"{lastHLW + (wHL.Length * j) + i}");
                    wO[j][i] = float.Parse(conf.stayNN[lastHLW + (wHL.Length * j) + i]);
                }
            }

            // calculate the hidden layer nodes output
            for (int i = 0; i < hL.Length; i++)
            {
                hL[i] = AI.ReLu(AI.Σ(state.fullState, wHL[i]) + 0);
            }

            // calculate the output nodes output
            for (int i = 0; i < o.Length; i++)
            {
                o[i] = AI.Σ(hL, wO[i]);
            }

            // squash & set the the final values for returning
            ret = AI.Sigmoid(o[0]);

            Debug.Log($"{fighter.name} Stay: {ret}");
        }
        else
        {
            float dice = UnityEngine.Random.value;

            ret = dice < conf.stayTrad ? 1 : 0;
        }

        return new OutputMakeMove(ret);
    }

    public static OutputMakeMove CalculateNN(Fighter fighter)
    { 
        float ret = -1;
        float[][][] w = new float[2][][];
        float[] x = fighter.stateData.fullState;
        int layers = 2;
        float[] hL = new float[2], o = new float[2];
        float[][] b = new float[2][];

        // weight[layer][node][weight]

        // 
        for (int k = 0; k < layers; k++)
        {
            for (int j = 0; j < w[k].Length; j++)
            {
                for (int i = 0; i < x.Length; i++)
                {
                    hL[j] = AI.ReLu((x[i] * w[k][j][i]) + b[k][j]);
                }
            }
        }

        o[0] = AI.Sigmoid(hL[0] * w[1][0][0]);

        return new OutputMakeMove(ret);
    }
}

public struct StateData
{
    public StateData(int turn, Fighter me, Fighter opp)
    {
        this.me = me;
        this.opp = opp;
        theTurn = turn;
    }

    public void Update(int turn)
    {
        theTurn = turn;
    }

    public string PrintState()
    {
        return $"distX: {distX}, distY: {distY}" +
            $"\nmyTurn: {myTurn}, myX: {myX}, myY: {myY}, myHp: {myHP}, myStr: {myStr}, isStunned: {iStunned}, myRuns: {myRuns}, myCandyX: {myCandyX}, myCandyY: {myCandyY}" +
        $"\nopTurn: {oppTurn}, opX: {oppX}, opY: {oppY}, opHp: {oppHP}, opStr: {oppStr}, opStunned: {oppStunned}, oppRuns: {oppRuns}, oppCandyX: {oppCandyX}, oppCandyY: {oppCandyY}";
    }

    public Fighter me;
    public Fighter opp;
    int theTurn;
    public float[] fullState => new float[] { distX, distY, myTurn, myX, myY, myHP, myStr, iStunned, myRuns, myCandyX, myCandyY, oppTurn, oppX, oppY, oppHP, oppStr, oppStunned, oppRuns, oppCandyX, oppCandyY };

    // 
    // // SHARED
    // 

    // 1- dist x
    public float distX => UnityEngine.Mathf.Abs(me.x - opp.x) / ((float)GM.mapSize - 1);
    // 2- dist y
    public float distY => UnityEngine.Mathf.Abs(me.y - opp.y) / ((float)GM.mapSize - 1);

    // 
    // // MINE
    //

    // 3- myTurn
    public int myTurn => theTurn == me.myTurn ? 1 : 0;
    // 4- myX
    public float myX => (float)me.eX / (float)GM.mapSize;
    // 5- myY
    public float myY => (float)me.eY / (float)GM.mapSize;
    // 6- myHp
    public float myHP => (float)me.hp / (float)GM.maxHP;
    // 7- myStr
    public float myStr => (float)me.str / (float)GM.maxStr;
    // 8- iStunned
    public int iStunned => me.isStunned ? 1 : 0;
    // 9 - myRuns
    public float myRuns => (float)me.ranAway / (float)GM.maxRunAway;
    // 10 - myCandyX
    public float myCandyX => (float)me.candyX / (float)GM.mapSize;
    // 11 - myCandyY
    public float myCandyY => (float)me.candyY / (float)GM.mapSize;

    // 
    // // OPPONENT
    // 

    // 12 - oppTurn
    public int oppTurn => myTurn == 0 ? 1 : 0;
    // 13- oppX
    public float oppX => (float)opp.eX / (float)GM.mapSize;
    // 14- oppY
    public float oppY => (float)opp.eY / (float)GM.mapSize;
    // 15- oppHp
    public float oppHP => (float)opp.hp / (float)GM.maxHP;
    // 16- oppStr
    public float oppStr => (float)opp.str / (float)GM.maxStr;
    // 17- oppStunned
    public int oppStunned => opp.isStunned ? 1 : 0;
    // 18 - oppRuns
    public float oppRuns => (float)opp.ranAway / (float)GM.maxRunAway;
    // 19 - oppCandyX
    public float oppCandyX => (float)opp.candyX / (float)GM.mapSize;
    // 20 - oppCandyY
    public float oppCandyY => (float)opp.candyY / (float)GM.mapSize;
}