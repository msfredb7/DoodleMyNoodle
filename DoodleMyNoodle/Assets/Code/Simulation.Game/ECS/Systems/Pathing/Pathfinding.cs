﻿using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using static fixMath;
using static Unity.Mathematics.math;


public static partial class CommonReads
{
    public static bool FindNavigablePath(ISimWorldReadAccessor accessor, in int2 from, in int2 to, fix maxCost, Allocator allocator, 
        out NativeList<int2> resultList)
    {
        resultList = new NativeList<int2>(allocator);
        return Pathfinding.FindNavigablePath(accessor, from, to, maxCost, resultList);
    }

    public static bool FindNavigablePath(ISimWorldReadAccessor accessor, int2 start, int2 goal, fix maxCost,
        NativeList<int2> outResult)
    {
        return Pathfinding.FindNavigablePath(accessor, start, goal, maxCost, outResult);
    }
}

public static class Pathfinding
{
    public const int MAX_PATH_COST = 20;

    public static bool FindNavigablePath(ISimWorldReadAccessor accessor, int2 start, int2 goal, fix maxCost, NativeList<int2> result)
    {
        result.Clear();

        if(maxCost > MAX_PATH_COST)
        {
            Debug.LogWarning($"Path Finding Max Cost cannot exceed {MAX_PATH_COST}");
            maxCost = MAX_PATH_COST;
        }

        if (start.Equals(goal))
        {
            result.Add(goal);
            return true;
        }

        // Destination cannot be reached
        if(!IsTileValid(goal, accessor))
        {
            return false;
        }

        NativeList<int2> resultInvertedPath = new NativeList<int2>(Allocator.Temp);
        NativeList<int2> neighbors = new NativeList<int2>(Allocator.Temp);

        // The set of discovered nodes that may need to be (re-)expanded.
        // Initially, only the start node is known.
        // This is usually implemented as a min-heap or priority queue rather than a hash-set.
        NativeList<int2> openSet = new NativeList<int2>(Allocator.Temp)
        {
            start
        };

        // List of nodes already discovered and explored. 
        // Starts off empty
        // Once a node has been 'current' it then goes here
        NativeList<int2> closeSet = new NativeList<int2>(Allocator.Temp);

        // For node n, cameFrom[n] is the node immediately preceding it on the cheapest path from start
        // to n currently known.
        NativeHashMap<int2, int2> cameFrom = new NativeHashMap<int2, int2>(100, Allocator.Temp);

        // For node n, gScore[n] is the cost of the cheapest path from start to n currently known.
        NativeHashMap<int2, fix> gScore = new NativeHashMap<int2, fix>(100, Allocator.Temp);
        // default value is Infinity
        gScore[start] = 0;

        // For node n, fScore[n] := gScore[n] + h(n). fScore[n] represents our current best guess as to
        // how short a path from start to finish can be if it goes through n.
        NativeHashMap<int2, fix> fScore = new NativeHashMap<int2, fix>(100, Allocator.Temp);
        // default value is Infinity
        fScore[start] = h(start, goal);


        int2 node_in_openSet_having_the_lowest_fScore_value()
        {
            int2 lowest = default;
            fix lowestValue = fix.MaxValue;
            for (int i = 0; i < openSet.Length; i++)
            {
                if (fScore.TryGetValue(openSet[i], out fix value) && value < lowestValue)
                {
                    lowest = openSet[i];
                    lowestValue = value;
                }
            }
            return lowest;
        }

        bool pathFound = false;
        while (openSet.Length != 0)
        {
            // This operation could occur in O(1) time if openSet was a min-heap or a priority queue
            int2 current = node_in_openSet_having_the_lowest_fScore_value();
            if (current.Equals(goal))
            {
                reconstruct_path(cameFrom, current, resultInvertedPath);
                pathFound = true;
                break;
            }

            // Current node goes into the closed set
            closeSet.Add(current);

            openSet.RemoveAtSwapBack(openSet.IndexOf(current));

            get_neighbors(current, neighbors, accessor);
            for (int i = 0; i < neighbors.Length; i++)
            {
                // d(current,neighbor) is the weight of the edge from current to neighbor
                // tentative_gScore is the distance from start to the neighbor through current
                fix tentative_gScore = gScore[current] + d(current, neighbors[i]);
                if (tentative_gScore <= maxCost && (!gScore.TryGetValue(neighbors[i], out fix s) || tentative_gScore < s))
                {
                    // This path to neighbor is better than any previous one. Record it!

                    cameFrom.SetOrAdd(neighbors[i], current);
                    gScore.SetOrAdd(neighbors[i], tentative_gScore);
                    fScore.SetOrAdd(neighbors[i], tentative_gScore + h(neighbors[i], goal));
                    if (!closeSet.Contains(neighbors[i]))
                        openSet.Add(neighbors[i]);
                }
            }
        }

        construct_result(resultInvertedPath, result);

        resultInvertedPath.Dispose();
        neighbors.Dispose();
        openSet.Dispose();
        closeSet.Dispose();
        cameFrom.Dispose();
        gScore.Dispose();
        fScore.Dispose();

        return pathFound;
    }


    private static void construct_result(NativeList<int2> invertedPath, NativeList<int2> result)
    {
        if (result.Capacity < invertedPath.Length)
            result.Capacity = invertedPath.Capacity;

        for (int i = invertedPath.Length - 1; i >= 0; i--)
        {
            result.Add(invertedPath[i]);
        }
    }

    private static void reconstruct_path(NativeHashMap<int2, int2> cameFrom, int2 current, NativeList<int2> result)
    {
        result.Add(current);
        while (cameFrom.TryGetValue(current, out int2 cameFromValue))
        {
            current = cameFromValue;
            result.Add(current);
        }
    }

    // d(current,neighbor) is the weight of the edge from current to neighbor
    private static fix d(in int2 current, in int2 neighbor)
    {
        return 1; // in a 2D grid, neighbor weight is always 1
    }

    private static void get_neighbors(in int2 tile, NativeList<int2> neighbors, ISimWorldReadAccessor accessor)
    {
        // temporary hard code
        const int TILE_MIN = -99;
        const int TILE_MAX = 99;

        neighbors.Clear();
        if (tile.x > TILE_MIN)
        {
            int2 neighborTilePos = tile + int2(-1, 0);
            if (IsTileValid(neighborTilePos, accessor))
            {
                neighbors.Add(neighborTilePos); // neighbor left
            }
        }

        if (tile.x < TILE_MAX)
        {
            int2 neighborTilePos = tile + int2(1, 0);
            if (IsTileValid(neighborTilePos, accessor))
            {
                neighbors.Add(neighborTilePos); // neighbor right
            }
        }

        if (tile.y > TILE_MIN)
        {
            int2 neighborTilePos = tile + int2(0, -1);
            if (IsTileValid(neighborTilePos, accessor))
            {
                neighbors.Add(neighborTilePos); // neighbor down
            }
        }

        if (tile.y < TILE_MAX)
        {
            int2 neighborTilePos = tile + int2(0, 1);
            if (IsTileValid(neighborTilePos, accessor))
            {
                neighbors.Add(neighborTilePos); // neighbor up
            }
        }
    }

    private static bool IsTileValid(int2 tilePos, ISimWorldReadAccessor accessor)
    {
        Entity tile = CommonReads.GetTile(accessor, tilePos);
        return CommonReads.DoesTileRespectFilters(accessor, tile, TileFilterFlags.Navigable | TileFilterFlags.Inoccupied);
    }

    // h is the heuristic function. h(n) estimates the cost to reach goal from node n.
    private static fix h(in int2 tile, in int2 goal)
    {
        int2 delta = abs(goal - tile);
        //delta *= delta; // making tiles in diagonal more appealing than tiles in a straight line far away
        return delta.x + delta.y;
    }
}
