class HallwayState
{
    public (int, int) start;
    public (int, int) end;
    public bool entranceDoor;
    public bool exitDoor;

    public HallwayState((int, int) start, (int, int) end, bool entranceDoor, bool exitDoor)
    {
        this.start = start;
        this.end = end;
        this.entranceDoor = entranceDoor;
        this.exitDoor = exitDoor;
    }
}

private Tuple<int, int>[] GetNeighbors(int row, int column, int size = 1)
{
    Tuple<int, int>[] cells = new Tuple<int, int>[8]
    {
        new Tuple<int, int>(row - size, column),    // top
        new Tuple<int, int>(row + size, column),    // bot
        new Tuple<int, int>(row, column - size),    // left
        new Tuple<int, int>(row, column + size),    // right
        new Tuple<int, int>(row - size, column - size),// topLeft
        new Tuple<int, int>(row - size, column + size),// topRight
        new Tuple<int, int>(row + size, column - size),// bottomLeft
        new Tuple<int, int>(row + size, column + size) // bottomRight
    };
    return cells;
}

private int ManhattanDistance(int x, int y, int endX, int endY)
{
    return Math.Abs(x - endX) + Math.Abs(y - endY);
}

// roomCenters are the coordinats of, well, centers of rooms that have been generated on a 2D array.
private (int, int) FindNearestRoom((int, int) start, List<(int, int)> roomCenters)
{
    int previousDistance = int.MaxValue;
    (int, int) closestRoom = (0, 0);
    foreach (var (row, col) in roomCenters) {
        int distance = ManhattanDistance(start.Item1, start.Item2, row, col);
        if (distance < previousDistance) {
            closestRoom = (row, col);
            previousDistance = distance;
        }
    }
    roomCenters.Remove((closestRoom.Item1, closestRoom.Item2));
    return closestRoom;
}


private void GenerateHallways(List<(int, int)> originalRoomCenters, bool coridors = false)
{
    Stack<HallwayState> hallwayStack = new Stack<HallwayState>();
    int currentDepth = 0;
    List<(int, int)> roomCenters = new List<(int, int)>(originalRoomCenters);
    (int, int) startRoom = roomCenters.First();
    roomCenters.RemoveAt(0);
    int roomCentersCount = roomCenters.Count();
    while(currentDepth < roomCentersCount) {
        bool entranceDoor = true;
        bool exitDoor = true;
        (int, int) endRoom = FindNearestRoom(startRoom, roomCenters);
        HallwayState initHallwayState = new HallwayState(startRoom, endRoom, entranceDoor, exitDoor);
        hallwayStack.Push(initHallwayState);
        while(!GenerateNextHallway(hallwayStack, coridors)){}
        if (hallwayStack.Count > 0) hallwayStack.Pop();
        startRoom = endRoom;
        currentDepth++;
    }
    Debug.Log("Finished all hallways, exiting...");
}

private bool GenerateNextHallway(Stack<HallwayState> hallwayStack, bool coridors)
{
    HallwayState hallwayData = hallwayStack.Peek();
    (int, int) start = hallwayData.start;
    (int, int) end = hallwayData.end;
    bool entranceDoor = hallwayData.entranceDoor;
    bool exitDoor = hallwayData.exitDoor;
    hallwayStack.Pop();
    List<(int x, int y)> possibleRoutes = new List<(int x, int y)>();
    Tuple<int, int>[] directNeighbors = GetNeighbors(start.Item1, start.Item2).Take(4).ToArray();

    int possibleRoutesCount;
    int distance;
    int previousNodeDistance;

    int nextRow;
    int nextColumn;

    // direct neighbors and check out of Range.
    foreach (var (row, col) in directNeighbors) {
        try {
            int rangeCheck = dungeon[row, col];
            possibleRoutes.Add((row, col));
        } catch (IndexOutOfRangeException) {
            continue;
        }
        // Connected hallway, continue with next hallway.
        if ((row, col) == end) {
            if(dungeon[row, col] > 2 && dungeon[start.Item1, start.Item2] < 2) {
                dungeon[start.Item1, start.Item2] = hallwayMarker + 1;
            }
            return true;
        }
        if (dungeon[row, col] == 1 && (row, col) != start && coridors == true) {
            return true;
        }
    }
    possibleRoutesCount = possibleRoutes.Count();
    if (possibleRoutesCount == 0) return true;
    (int, int) closestElement = start;
    previousNodeDistance = ManhattanDistance(start.Item1, start.Item2, end.Item1, end.Item2);
    for (int j = 0; j < possibleRoutesCount; j++) {
        int row0 = possibleRoutes[j].Item1;
        int col0 = possibleRoutes[j].Item2;
        distance = ManhattanDistance(row0, col0, end.Item1, end.Item2);
        if (distance < previousNodeDistance) {
            closestElement = possibleRoutes[j];
            previousNodeDistance = ManhattanDistance(closestElement.Item1, closestElement.Item2, end.Item1, end.Item2);
        }
    }
    nextRow = closestElement.Item1;
    nextColumn = closestElement.Item2;
    // Door mechanic
    if(dungeon[nextRow, nextColumn] < 2) {
        if (entranceDoor) {
            dungeon[nextRow, nextColumn] = hallwayMarker + 1;
            entranceDoor = false;
            exitDoor = true;
        }
        else {
            dungeon[nextRow, nextColumn] = hallwayMarker;
        }
    }
    if ((dungeon[nextRow, nextColumn] > 2) && !entranceDoor && exitDoor) {
        dungeon[start.Item1, start.Item2] = hallwayMarker + 1;
        exitDoor = false;
        entranceDoor = true;
    }
    HallwayState nextState = new HallwayState((nextRow, nextColumn), end, entranceDoor, exitDoor);
    hallwayStack.Push(nextState);
    // Just in case infinity loop should occur.
    if(nextRow == start.Item1 && nextColumn == start.Item2) {
        infinityFailsafe++;
    }
    if (infinityFailsafe > 3) {
        Debug.Log("Infinite loop, breaking");
        infinityFailsafe = 0;
        return true;
    }
    return false;
}