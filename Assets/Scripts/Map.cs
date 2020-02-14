﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Map
{
    public int mapSize => loc.GetLength(0);
    public int halfMapSize => mapSize/2;
    public Tile a => current;
    Tile tileFrom => _loc[fX, fY];
    Tile tileTo => _loc[tX, tY];
    public Tile[,] loc => _loc;
    int fX, fY, tX, tY;
    public TilePath thee;

    Tile current;
    Tile[,] _loc;
    public List<Tile> openTiles, closedTiles;

    public Map(int size)
    {
        _loc = new Tile[size, size];

        InitLocations();
    }

    public Map(string path)
    {
        LoadLeveData(path);
    }

    public void BlockTiles(List<Vector2Int> b)
    {
        for (int i = 0; i < b.Count; i++)
        {
            _loc[b[i].x, b[i].y].free = false;
        }
    }

    void InitLocations()
    {
        // first we init
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                _loc[j, i] = new Tile(j, i, halfMapSize: halfMapSize);
            }
        }
    }

    public List<Tile> CalcAvailbleMoves(Vector2Int source)
    {
        // get a reference to what we're going to return
        List<Tile> ret = new List<Tile>();

        // loop through the entire map size
        for (int i = 0; i < mapSize; i++)
        {
            for (int j = 0; j < mapSize; j++)
            {
                // if the tile is in the right distance from the source & that tile is not the tile the source is on,
                if ((ManhattanDistance(source, loc[j, i].v2Int) <= GM.maxMoves) && source != loc[j, i].v2Int)
                {
                    // also IF that space is reachable in 5 or less moves
                    if (FindPath(source.x, source.y, loc[j, i].x, loc[j, i].y)?.dist <= 5)
                    {
                        // add it to the return reference
                        ret.Add(loc[j, i]);
                    }
                }
            }
        }

        return ret;
    }

    public static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        // calculate the X + Y distance between a & b
        return (Mathf.Abs(a.x - b.x) + (Mathf.Abs(a.y - b.y)));
    }

    public static int SelectAnAngle(List<Tile> toCheck, float iX, float iY, Vector2Int whoLoc)
    {
        int ret = -1;

        // 
        float[] temp = new float[toCheck.Count];

        // Get all availible select positions & give them an ID
        for (int i = 0; i < toCheck.Count; i++)
        {
            temp[i] = GM.XYtoDeg(toCheck[i].x - whoLoc.x, toCheck[i].y - whoLoc.y);
        }

        float lowestDist = Mathf.Infinity;

        // Get the distance from desired point to all availible selected positions
        for (int i = 0; i < temp.Length; i++)
        {
            float desiredPoint = GM.XYtoDeg(iX, iY);

            float dist = Mathf.Abs(desiredPoint - temp[i]);

            // 
            if (dist < lowestDist)
            {
                lowestDist = dist;
                ret = i;
            }
        }

        // Return the availible selected position with the lowest distance
        return ret;
    }

    public TilePath FindPath(int _fX, int _fY, int _tX, int _tY)
    {
        return PathFinder(_fX, _fY, _tX, _tY);
    }

    public TilePath FindLimitedPath(int _fX, int _fY, int _tX, int _tY)
    {
        // if the tile we are trying to reach is out of range,
        if (ManhattanDistance(new Vector2Int(_fX, _fY), new Vector2Int(_tX, _tY)) > GM.maxMoves)
        {
            // appologize. right now.
            Debug.Log($"Sorry out of range!");

            return null;
        }

        return PathFinder(_fX, _fY, _tX, _tY);
    }

    public void LoadLeveData(string levelName)
    {
        using (StreamReader sR = new StreamReader($"{Path.Combine(Application.dataPath, $"lvl_{levelName}{GM.lvlExt}")}"))
        {
            // make an algo that will convert text data of x = block o = free into a list of Vector2Ints
            string[] raw = sR.ReadToEnd().Split('\n');

            int mapSize = raw.Length;

            // 
            _loc = new Tile[mapSize, mapSize];
            
            // 
            InitLocations();

            // make a reference to the data mapped as chars
            char[,] mapped = new char[mapSize, mapSize];

            // 
            List<Vector2Int> block = new List<Vector2Int>();

            // loop through the mapped chars
            for (int i = 0; i < raw.Length; i++)
            {
                for (int j = 0; j < raw.Length; j++)
                {
                    // fill in the mapped chars
                    mapped[j, i] = raw[i][j];

                    // assign type
                    loc[j, i].AssignType(raw[i][j]);

                    // ADD ALL THINGS, w, p etc
                    if (raw[i][j] == 'x' || raw[i][j] == 'p' || raw[i][j] == 'w')
                    {
                        block.Add(new Vector2Int(j, i));
                    }
                }
            }

            // pass that to map.Block()
            BlockTiles(block);

            // profit 
        }
    }

    public static (List<Tile> loc, List<Tile> selLoc, TilePath path, int angleSelect) OutputLocation(Map map, Vector2Int self, Vector2Int opponent, float dist, float angleX, float angleY)
    {
        // reset our selPos list every frame, this allows us to make changes to it
        List<Tile> selLoc = new List<Tile>();

        // 
        int intervals = 4;

        // now we calculate the selected range of tiles based on distance from the opponent
        int calc = Mathf.RoundToInt((dist * intervals) + (Map.ManhattanDistance(self, opponent) - GM.maxMoves));

        // we need a reference to a temp all selected positions list
        List<Vector2Int> tempAllSelPos = new List<Vector2Int>();

        List<Tile> loc = map.CalcAvailbleMoves(self);

        // next we loop through all of the positions & see who is viable
        for (int i = 0; i < loc.Count; i++)
        {
            //Debug.Log($"ManDist: {Map.ManhattanDistance(loc[i].v2Int, opponent)} == {Mathf.Clamp(calc, 1, calc)}");

            if (Map.ManhattanDistance(loc[i].v2Int, opponent) == Mathf.Clamp(calc, 1, calc))
            {
                // IF THIS IS EMPTY IT THROWS EXCEPTIONS
                // NEED TO MAKE SURE IT EITHER STAYS ON LAST IN RANGE, OR FORCE THE CALC TO STAY IN RANGE OF WHATS POSSIBLE.
                // I THINK THE ABOVE CAN BE ACHIEVED USING THE CLAMP, FIGURING OUT A SMART WAY OF GETTING THE MAX POSSIBLE HSCORE
                // ^^^ CAN SIMPLY CHECK IN A LOOP & STORE THE MAX, BUT I THINK IF I CAN INTELLIGENTLY REMAP IT TO 0-1, THAT'D BE BEST
                // IT APPEARS THE INTERVALS VARIABLE IS WHERE ITS AT, IT APPEARS TO = HOW MANY MANHAT SPACES THE FURTHEST SPACE IS,
                // WHICH IS QUITE COMPLICATED, BUT THANKFULLY AFTER THINKING ABOUT IT I THINK THAT KEEPING IT A CONSTANT INTERVAL OF 10,
                // & CLAMPING THE CALC IS BETTER BECAUSE THEN PREDICTING A 1 DIST WILL ALWAYS MEAN THE SAME THING, REGARDLESS IF GRANTED OR NOT
                selLoc.Add(loc[i]);
            }
        }

        int angleSelect = Map.SelectAnAngle(selLoc, angleX, angleY, opponent);

        TilePath p = map.FindLimitedPath(self.x, self.y, selLoc[angleSelect].x, selLoc[angleSelect].y);

        return (loc, selLoc, p, angleSelect);
    }

    TilePath PathFinder(int _fX, int _fY, int _tX, int _tY)
    {
        float start = Time.time;
        thee = new TilePath(new List<Tile>());

        fX = _fX;
        fY = _fY;
        tX = _tX;
        tY = _tY;

        // set list of tiles to be evaluated
        openTiles = new List<Tile>();
        // set list of tiles already evaluated
        closedTiles = new List<Tile>();

        // add current node to the open list
        AddTileToOpen(tileFrom);

        // While we still have tiles in the open tiles list
        while (openTiles.Count > 0)
        {
            // current tile = tile with the lowest f cost in the open set (start off with only 1 tile in the open tiles list so that is the lowest)
            current = GetLowestFCost();

            // if current == target, then we're done
            if (current == tileTo)
            {
                //Debug.Log($"AT END! Elapsed Time: {Time.time - start}");

                // 
                thee.path.Add(current);

                // While the current tile's parent does not = our starting tile,
                while (current.parent != tileFrom)
                {
                    // add it to the path
                    thee.path.Add(current.parent);
                    // update what the current node is
                    current = current.parent;
                }

                // Because we trace from the end to the start, we need to reverse the path
                thee.path.Reverse();

                return thee;
            }

            // if not then well Process this tile for exploration
            ProcessTile(current);
        }

        //Debug.Log($"CAN'T FIND! Elapsed Time: {Time.time - start}");

        return null;
    }

    void AddTileToOpen(Tile t)
    {
        if (!openTiles.Contains(t))
        {
            openTiles.Add(t);
        }
    }

    void ProcessTile(Tile c)
    {
        // Get & set all of the neighbors for that tile, which will also add it to the open tiles list
        current.SetNeighbors(GetNeighbors(c, c.parent, tileTo));

        // remove current from open since it's ben processed
        openTiles.Remove(c);

        // add current to closed since its been processed
        closedTiles.Add(c);
    }

    float GetDistance(Vector2 a, Vector2 b)
    {
        return (a - b).sqrMagnitude;
    }

    List<Tile> GetNeighbors(Tile current, Tile _from, Tile to)
    {
        _from = current;

        List<Tile> ret = new List<Tile>();

        // Set our current tile's fCost
        float gc = GetDistance(new Vector2(current.x, current.y), new Vector2(_from.x, _from.y));
        float hc = GetDistance(new Vector2(current.x, current.y), new Vector2(to.x, to.y));

        current.SetGnH(gc, hc);

        Tile n = _loc[current.x - 1, current.y];
        // set our neighbor's fcost
        if (current.x - 1 > 0 && !openTiles.Contains(n) && !closedTiles.Contains(n) && _loc[current.x - 1, current.y].free)
        {
            gc = GetDistance(new Vector2(n.x, n.y), new Vector2(_from.x, _from.y));
            hc = GetDistance(new Vector2(n.x, n.y), new Vector2(to.x, to.y));

            n.SetGnH(gc, hc);
            n.SetParent(current);
            AddTileToOpen(n);
            ret.Add(n);
        }

        n = _loc[current.x, current.y - 1];
        // 
        if (current.y - 1 > 0 && !openTiles.Contains(n) && !closedTiles.Contains(n) && _loc[current.x, current.y - 1].free)
        {
            gc = GetDistance(new Vector2(n.x, n.y), new Vector2(_from.x, _from.y));
            hc = GetDistance(new Vector2(n.x, n.y), new Vector2(to.x, to.y));

            n.SetGnH(gc, hc);
            n.SetParent(current);
            AddTileToOpen(n);
            ret.Add(n);
        }

        n = _loc[current.x + 1, current.y];
        // 
        if (current.x + 1 < mapSize - 1 && !openTiles.Contains(n) && !closedTiles.Contains(n) && _loc[current.x + 1, current.y].free)
        {
            gc = GetDistance(new Vector2(n.x, n.y), new Vector2(_from.x, _from.y));
            hc = GetDistance(new Vector2(n.x, n.y), new Vector2(to.x, to.y));

            n.SetGnH(gc, hc);
            n.SetParent(current);
            AddTileToOpen(n);
            ret.Add(n);
        }

        n = _loc[current.x, current.y + 1];
        // 
        if (current.y + 1 < mapSize - 1 && !openTiles.Contains(n) && !closedTiles.Contains(n) && _loc[current.x, current.y + 1].free)
        {
            gc = GetDistance(new Vector2(n.x, n.y), new Vector2(_from.x, _from.y));
            hc = GetDistance(new Vector2(n.x, n.y), new Vector2(to.x, to.y));

            n.SetGnH(gc, hc);
            n.SetParent(current);
            AddTileToOpen(n);
            ret.Add(n);
        }

        return ret;
    }

    Tile GetLowestFCost()
    {
        // 
        Tile lowest = new Tile(0, 0);
        lowest.SetGnH(0, Mathf.Infinity);

        // 
        for (int i = 0; i < openTiles.Count; i++)
        {
            if (openTiles[i].fCost < lowest.fCost)
            {
                lowest = openTiles[i];
            }
        }

        // 
        return lowest;
    }
}
