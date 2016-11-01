using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CellManager {

    public int loadDistance = 50;
    public int cellSize = 500;

    public int curPosX = -9999999;
    public int curPosY = -9999999;

    private Dictionary<CellPos, Cell> cells;
    private Object cellLock = new Object();

    private static CellManager singleton;
    private static Object singletonLock = new Object();

    public static CellManager GetCellManager()
    {
        lock(singletonLock)
        {
            if (singleton == null)
            {
                singleton = new CellManager();
            }
        }

        return singleton;
    }

    private CellManager()
    {
        cells = new Dictionary<CellPos, Cell>();
    }

    // HERE'S WHERE THE ACTUAL WORK GETS DONE
    public void Tick()
    {
        UpdateCells();

        Vector3 position = ObjectManager.GetObjectManager().GetPlayerPosition();
        int posX = Mathf.FloorToInt(position.x/cellSize);
        int posY = Mathf.FloorToInt(position.z/cellSize);

        TerrainGenerator.Update();

        if (posX == curPosX && posY == curPosY)
        {
            // We don't need to do any work, we haven't moved cells
            return;
        }

        curPosX = posX;
        curPosY = posY;

        RemoveOldCells();
        LoadNewCells();
        UpdateLods();
    }

    public Cell GetCellAtPosition( CellPos pos )
    {
        lock (cellLock)
        {
            if (cells.ContainsKey(pos))
                return cells[pos];
            return null;
        }
    }

    private bool shouldLoad(CellPos pos)
    {
        return distanceFromPos(pos) < loadDistance;
    }

    private int distanceFromPos(CellPos pos)
    {
        int distX = pos.x - curPosX;
        int distY = pos.y - curPosY;

        if (distX < 0)
            distX *= -1;
        if (distY < 0)
            distY *= -1;

        return distX + distY;
    }

    private void RemoveOldCells()
    {
        lock(cells)
        {
            List<CellPos> removals = new List<CellPos>();
            foreach(KeyValuePair<CellPos, Cell> keyValuePair in cells)
            {
                CellPos pos = keyValuePair.Key;
                if (!shouldLoad(pos))
                    removals.Add(pos);
            }
            foreach(CellPos pos in removals)
            {
                Unload(pos);
            }
        }
    }

    private void LoadNewCells()
    {
        lock (cells)
        {
            for (int x = -loadDistance; x <= loadDistance; x++)
            {
                for (int y = -loadDistance; y <= loadDistance; y++)
                {
                    CellPos pos;
                    pos.x = x + curPosX;
                    pos.y = y + curPosY;

                    if (shouldLoad(pos))
                        Load(pos);
                }
            }
        }
    }

    private void UpdateCells()
    {
        foreach (KeyValuePair<CellPos, Cell> keyValuePair in cells)
        {
            Cell cell = keyValuePair.Value;
            cell.UpdateLoadstatus();
        }
    }

    private void UpdateLods()
    {
        foreach (KeyValuePair<CellPos, Cell> keyValuePair in cells)
        {
            Cell cell = keyValuePair.Value;
            cell.LoadLod(CalculateLod(keyValuePair.Key));
        }
    }

    // This cell exists in our system
    private bool KnowCell(CellPos pos)
    {
        lock (cells)
        {
            return cells.ContainsKey(pos);
        }
    }

    private int CalculateLod(CellPos pos)
    {
        int distance = distanceFromPos(pos);

        int counter = 0;
        int i = 1;

        while ( i <= distance )
        {
            i *= 4;
            counter++;
        }

        return counter;
    }

    private void Load(CellPos pos)
    {
        // We already know this cell,
        // Why try to request it?
        if (KnowCell(pos))
            return;

        lock (cellLock)
        {
            Cell cell = new Cell(pos, cellSize);
            cells[pos] = cell;
            cell.RequestLoad();
        }
    }

    private void Unload(CellPos pos)
    {
        if (!KnowCell(pos))
            return;

        lock (cellLock)
        {
            Cell cell = GetCellAtPosition(pos);
            cell.Unload();
            cells.Remove(pos);
        }
    }
}
