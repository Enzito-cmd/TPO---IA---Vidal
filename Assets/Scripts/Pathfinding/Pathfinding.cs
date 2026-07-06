using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    private GridManager grid;

    [Header("Settings")]
    public bool useThetaStar = false; 
    public LayerMask obstacleMask; 

    private void Awake()
    {
        grid = GetComponent<GridManager>();
    }

    public List<Node> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(targetPos);

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                List<Node> path = RetracePath(startNode, targetNode);

                if (useThetaStar)
                {
                    return SmoothPathThetaStar(path);
                }
                return path; 
            }

            foreach (Node neighbor in grid.GetNeighbors(currentNode))
            {
                if (!neighbor.isWalkable || closedSet.Contains(neighbor)) continue;

                int newCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                }
            }
        }
        return null;
    }

    private List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;
        int safetyCounter = 0; 

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;

            safetyCounter++;
            if (safetyCounter > 1000)
            {
                break;
            }
        }
        path.Reverse();
        return path;
    }

    private List<Node> SmoothPathThetaStar(List<Node> path)
    {
        if (path == null || path.Count < 3) return path;

        List<Node> smoothedPath = new List<Node>();
        smoothedPath.Add(path[0]);

        int currentIdx = 0;
        while (currentIdx < path.Count - 2)
        {
            if (HasLineOfSight(smoothedPath[smoothedPath.Count - 1], path[currentIdx + 2]))
            {
                currentIdx++; 
            }
            else
            {
                smoothedPath.Add(path[currentIdx + 1]);
                currentIdx++;
            }
        }

        smoothedPath.Add(path[path.Count - 1]);
        return smoothedPath;
    }

    private bool HasLineOfSight(Node fromNode, Node toNode)
    {
        Vector3 origin = fromNode.worldPosition + Vector3.up * 0.5f;
        Vector3 target = toNode.worldPosition + Vector3.up * 0.5f;

        return !Physics.Linecast(origin, target, obstacleMask);
    }

    private int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
        return (dstX > dstY) ? 14 * dstY + 10 * (dstX - dstY) : 14 * dstX + 10 * (dstY - dstX);
    }
}