using System.Collections.Generic;
using UnityEngine;

public class DungeonObject
{
    GameObject gameObject;
    Dictionary<string, string> info;
} 

public class DungeonCell {
    public bool empty;
    DungeonObject cell, floor, lu, ru, item, air, ld, rd;
    public DungeonCell()
    {
        empty = true;
        cell = null;
        floor = null;
        lu = null;
        ru = null;
        item = null;
        air = null;
        ld = null;
        rd = null;
    }
}

public class GenerateRange
{
    public int start, end;
    public GenerateRange(int start, int end)
    {
        this.start = start;
        this.end = end;
    }

    public int Generate()
    {
        return Random.Range(start, end);
    }

    public int Len() 
    {
        return end - start + 1;
    }
}

public class GenerateVector
{
    public int x, y;

    public GenerateVector(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

public class ItemPool 
{

}

public class RoomGenerationParameters
{
    public GenerateRange w, h, space, depth, corridorLenth;
    public ItemPool pool;
    public int generateTry = 0;
}

public class DungeonGenerateParameters
{
    public List<RoomGenerationParameters> roomsData;
    public int sizeW, sizeH, maxCountOfGenerateTry, maxDepth;
}

public class RoomOutput
{
    public GenerateVector direction;
    public GenerateRange x, y;
}

public class DungeonCellMap
{
    public List<List<DungeonCell>> cells;
    DungeonGenerateParameters parameters;
    public DungeonCellMap()
    {
        cells = null;
    }

    public bool GenerateCellMap(DungeonGenerateParameters generationData) {
        parameters = generationData;
        InitEmptyCellMap(generationData.sizeW, generationData.sizeH);
        Queue<int> generationQueue = new Queue<int>();
        for (int i = 0; i < generationData.roomsData.Count; i++)
        {
            generationQueue.Enqueue(i);
        }

        List<List<RoomOutput>> outputsByDepth = new List<List<RoomOutput>>();
        for (int i  = 0; i < generationData.maxDepth; i++)
        {
            List<RoomOutput> empty = new List<RoomOutput>();
            outputsByDepth.Add(empty);
        }

        GenerateStartRoom(ref outputsByDepth, ref generationData);


        while (generationQueue.Count != 0)
        {
            int roomId = generationQueue.Dequeue();
            generationData.roomsData[roomId].generateTry++;
            if (generationData.roomsData[roomId].generateTry > generationData.maxCountOfGenerateTry)
            {
                return false;
            }
            RoomGenerationParameters roomData = generationData.roomsData[roomId];
            bool isOk = GenerateNewRoom(ref outputsByDepth, ref roomData);
            if (!isOk)
            {
                generationData.roomsData[roomId].generateTry++;
                generationQueue.Enqueue(roomId);
            }
        }
        return true;
    }

    private void InitEmptyCellMap(int sizeW, int sizeH)
    {
        cells = new List<List<DungeonCell>>();
        for (int i = 0; i < sizeW; i++)
        {
            List<DungeonCell> empty = new List<DungeonCell>();
            for (int j = 0; j < sizeH; j++)
            {
                empty.Add(new DungeonCell());
            }
            cells.Add(empty);
        }
    }

    public void GenerateStartRoom(ref List<List<RoomOutput>> outputsByDepth, ref DungeonGenerateParameters generationData)
    {
        int startX = generationData.sizeW / 2 - 3;
        int startY = generationData.sizeH / 2 - 3;
        int endX = generationData.sizeW / 2 + 3;
        int endY = generationData.sizeH / 2 + 3;
        for (int i = startX; i < endX; i++)
        {
            for (int j = startY; j < endY; j++)
            {
                cells[i][j].empty = false;
            }
        }

        GenerateNewOutputs(ref outputsByDepth, 0, new GenerateRange(startX, startX), new GenerateRange(startY, endY), new GenerateVector(-1, 0));
        GenerateNewOutputs(ref outputsByDepth, 0, new GenerateRange(startX, endX), new GenerateRange(startY, startY), new GenerateVector(0, -1));
        GenerateNewOutputs(ref outputsByDepth, 0, new GenerateRange(endX, endX), new GenerateRange(startY, endY), new GenerateVector(1, 0));
        GenerateNewOutputs(ref outputsByDepth, 0, new GenerateRange(startX, endX), new GenerateRange(endY, endY), new GenerateVector(0, 1));
}

    private bool GenerateNewRoom(ref List<List<RoomOutput>> outputsByDepth, ref RoomGenerationParameters data)
    {
        int space = data.space.Generate();
        int endW = Mathf.Min(space / data.h.start, data.w.end);
        GenerateRange rangeW = new GenerateRange(data.w.start, endW);
        int w = rangeW.Generate();
        int h = data.h.Generate();
        int corridorLenth = data.corridorLenth.Generate();
        List<int> outputsCount = new List<int>();
        int fullOutputsCount = 0;
        for (int i = data.depth.start; i <= data.depth.end; i++)
        {
            fullOutputsCount+= outputsByDepth[i].Count;
            outputsCount.Add(fullOutputsCount);
        }
        int outputId = Random.Range(0, fullOutputsCount - 1);
        int nowPosition = 0;
        int resultDepth = 0;
        for (int i = data.depth.start; i <= data.depth.end; i++)
        {
            nowPosition += outputsByDepth[i].Count;
            if (nowPosition > outputId)
            {
                resultDepth = i;
                break;
            }
        }
        for (int i = resultDepth; i <= data.depth.end; i++)
        {
            int count = outputsByDepth[i].Count;
            for (int j = 0; j < count; j++)
            {
                RoomOutput output = outputsByDepth[i][j];
                int startX = output.x.Generate();
                int startY = output.y.Generate();
                if (output.direction.x == -1) { startX -= 1; }
                if (output.direction.y == -1) { startY -= 1; }
                int deltaX = Random.Range(0, w - 3) + 2;
                int deltaY = Random.Range(0, h - 3) + 2;
                bool isOk = IsRoomPushable(ref output, new GenerateVector(startX, startY), new GenerateVector(deltaX, deltaY), w, h, corridorLenth);
                if (isOk)
                {
                    outputsByDepth[i].RemoveAt(j);
                    PushRoom(ref outputsByDepth, ref output, new GenerateVector(startX, startY), new GenerateVector(deltaX, deltaY), w, h, corridorLenth, i);
                    return true;
                }
            }
        }
        return false;
    }

    private bool IsRoomPushable(ref RoomOutput output, GenerateVector start, GenerateVector delta, int w, int h, int corridorLenth) {
        int toUpDirectionX = 0;
        if (output.direction.y != 0) { toUpDirectionX = 1; }
        int toUpDirectionY = 0;
        if (output.direction.x != 0) { toUpDirectionY = 1; }
        int pointX = start.x + (output.direction.x * (corridorLenth - 1)) + toUpDirectionX;
        int pointY = start.y + (output.direction.y * (corridorLenth - 1)) + toUpDirectionY;
        GenerateRange CorrX = new GenerateRange(Mathf.Min(start.x, pointX), Mathf.Max(start.x, pointX));
        GenerateRange CorrY = new GenerateRange(Mathf.Min(start.y, pointY), Mathf.Max(start.y, pointY));
        for (int i = CorrX.start; i <= CorrX.end; i++)
        {
            for (int j = CorrY.start; j <= CorrY.end; j++)
            {
                if (!cells[i][j].empty) { return false; }
            }
        }
        if (toUpDirectionX == 0) { pointX++; }
        if (toUpDirectionY == 0) { pointY++; }
        int firstAngleX = pointX + delta.x * toUpDirectionX;
        int firstAngleY = pointY + delta.y * toUpDirectionY;
        int secondAngleX = firstAngleX + w * output.direction.x;
        int secondAngleY = firstAngleY + h * output.direction.y;
        if (output.direction.x == 0) { secondAngleX -= w; }
        if (output.direction.y == 0) { secondAngleY -= h; }
        GenerateVector roomStart = new GenerateVector(Mathf.Min(firstAngleX, secondAngleX), Mathf.Min(firstAngleY, secondAngleY));
        GenerateVector roomEnd = new GenerateVector(Mathf.Max(firstAngleX, secondAngleX), Mathf.Max(firstAngleY, secondAngleY));
        for (int i = roomStart.x - 1; i < roomEnd.x + 1; i++)
        {
            for (int j = roomStart.y - 1; j < roomEnd.y + 1; j++)
            {
                if (!cells[i][j].empty) { return false; }
            }
        }
        return true;
    }

    private void PushRoom(ref List<List<RoomOutput>> outputsByDepth, ref RoomOutput output, GenerateVector start, GenerateVector delta, int w, int h, int corridorLenth, int depth)
    {
        int toUpDirectionX = 0;
        if (output.direction.y != 0) { toUpDirectionX = 1; }
        int toUpDirectionY = 0;
        if (output.direction.x != 0) { toUpDirectionY = 1; }
        int pointX = start.x + (output.direction.x * (corridorLenth - 1)) + toUpDirectionX;
        int pointY = start.y + (output.direction.y * (corridorLenth - 1)) + toUpDirectionY;
        GenerateRange CorrX = new GenerateRange(Mathf.Min(start.x, pointX), Mathf.Max(start.x, pointX));
        GenerateRange CorrY = new GenerateRange(Mathf.Min(start.y, pointY), Mathf.Max(start.y, pointY));
        for (int i = CorrX.start; i <= CorrX.end; i++)
        {
            for (int j = CorrY.start; j <= CorrY.end; j++)
            {
                cells[i][j].empty = false;
            }
        }
        if (toUpDirectionX == 0) { pointX ++; }
        if (toUpDirectionY == 0) { pointY ++; }
        int firstAngleX = pointX + delta.x * toUpDirectionX;
        int firstAngleY = pointY + delta.y * toUpDirectionY;
        int secondAngleX = firstAngleX + w * output.direction.x;
        int secondAngleY = firstAngleY + h * output.direction.y;
        if (output.direction.x == 0) { secondAngleX -= w; }
        if (output.direction.y == 0) { secondAngleY -= h; }
        GenerateVector roomStart = new GenerateVector(Mathf.Min(firstAngleX, secondAngleX), Mathf.Min(firstAngleY, secondAngleY));
        GenerateVector roomEnd = new GenerateVector(Mathf.Max(firstAngleX, secondAngleX), Mathf.Max(firstAngleY, secondAngleY));
        for (int i = roomStart.x; i < roomEnd.x; i++)
        {
            for (int j = roomStart.y; j < roomEnd.y; j++)
            {
                cells[i][j].empty = false;
            }
        }

        if (!(output.direction.x == 1 && output.direction.y == 0))
        {
            GenerateNewOutputs(ref outputsByDepth, depth, new GenerateRange(roomStart.x, roomStart.x), new GenerateRange(roomStart.y, roomEnd.y), new GenerateVector(-1, 0));
        }
        if (!(output.direction.x == 0 && output.direction.y == 1))
        {
            GenerateNewOutputs(ref outputsByDepth, depth, new GenerateRange(roomStart.x, roomEnd.x), new GenerateRange(roomStart.y, roomStart.y), new GenerateVector(0, -1));
        }
        if (!(output.direction.x == -1 && output.direction.y == 0))
        {
            GenerateNewOutputs(ref outputsByDepth, depth, new GenerateRange(roomEnd.x, roomEnd.x), new GenerateRange(roomStart.y, roomEnd.y), new GenerateVector(1, 0));
        }
        if (!(output.direction.x == 0 && output.direction.y == -1))
        {
            GenerateNewOutputs(ref outputsByDepth, depth, new GenerateRange(roomStart.x, roomEnd.x), new GenerateRange(roomEnd.y, roomEnd.y), new GenerateVector(0, 1));
        }
        // добавить обратные.
    }

    private void GenerateNewOutputs(ref List<List<RoomOutput>> outputsByDepth, int depth, GenerateRange x, GenerateRange y, GenerateVector direction)
    {
        int lenX = x.Len();
        int lenY = y.Len();
        if (lenY == 1)
        {
            int position = 0;
            int count = lenX / 6;
            int delta = lenX - count * 6;
            for (int i = 0; i < count; i++)
            {
                int size;
                if (i == count - 1)
                {
                    size = 6 + delta;
                }
                else
                {
                    int add = Random.Range(0, delta);
                    size = 6 + add;
                    delta -= add;
                }
                RoomOutput newOutput = new()
                {
                    direction = direction,
                    x = new GenerateRange(x.start + position + 1, x.start + position + size - 3),
                    y = new GenerateRange(y.start, y.start)
                };
                int outputsCount = outputsByDepth[depth + 1].Count;
                outputsByDepth[depth + 1].Insert(Random.Range(0, outputsCount), newOutput);
                position += size;
            }
        }
        else
        {
            int position = 0;
            int count = lenY / 6;
            int delta = lenY - count * 6;
            for (int i = 0; i < count; i++)
            {
                int size;
                if (i == count - 1)
                {
                    size = 6 + delta;
                }
                else
                {
                    int add = Random.Range(0, delta);
                    size = 6 + add;
                    delta -= add;
                }
                RoomOutput newOutput = new()
                {
                    direction = direction,
                    x = new GenerateRange(x.start, x.start),
                    y = new GenerateRange(y.start + position + 1, y.start + position + size - 3)
                };
                int outputsCount = outputsByDepth[depth + 1].Count;
                outputsByDepth[depth + 1].Insert(Random.Range(0, outputsCount), newOutput);
                position += size;
            }
        }
    }
}

public class DungeonNavMap
{
    public DungeonNavMap() 
    { 

    }
}

public class DungeonData 
{
    public int sizeW, sizeH;
    public Dictionary<string, string> data;
    public DungeonCellMap cellMap;
    public DungeonNavMap navMap;
    public DungeonData(int sizeW, int sizeH) 
    {
        this.sizeW = sizeW;
        this.sizeH = sizeH;
        data = new Dictionary<string, string>();
        cellMap = new DungeonCellMap();
        navMap = new DungeonNavMap();
    }

    public DungeonData(DungeonGenerateParameters parameters)
    {
        sizeW = parameters.sizeW;
        sizeH = parameters.sizeH;
        data = new Dictionary<string, string>();
        cellMap = new DungeonCellMap();
        cellMap.GenerateCellMap(parameters);
        navMap = new DungeonNavMap();
    }
}

public class Dungeon : MonoBehaviour
{
    [SerializeField] GameObject cellGameObject;
    List<GameObject> objects;
    DungeonData dungeon;
    void Start() {
        Random.InitState(124121352);
        objects = new List<GameObject>();
        CreateDungeon();
        DrawDungeon();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ClearDungeon();
            CreateDungeon();
            DrawDungeon();
        }
    }


    private void CreateDungeon() {
        List<RoomGenerationParameters> roomData = new List<RoomGenerationParameters>();
        roomData.Add(new RoomGenerationParameters()
        {
            w = new GenerateRange(10, 15),
            h = new GenerateRange(6, 8),
            corridorLenth = new GenerateRange(3, 4),
            depth = new GenerateRange(0, 10),
            pool = new ItemPool(),
            space = new GenerateRange(60, 120)
        });
        roomData.Add(new RoomGenerationParameters()
        {
            w = new GenerateRange(6, 10),
            h = new GenerateRange(6, 10),
            corridorLenth = new GenerateRange(3, 4),
            depth = new GenerateRange(1, 3),
            pool = new ItemPool(),
            space = new GenerateRange(36, 200)
        });
        roomData.Add(new RoomGenerationParameters()
        {
            w = new GenerateRange(6, 10),
            h = new GenerateRange(6, 10),
            corridorLenth = new GenerateRange(3, 4),
            depth = new GenerateRange(1, 3),
            pool = new ItemPool(),
            space = new GenerateRange(36, 200)
        });
        roomData.Add(new RoomGenerationParameters()
        {
            w = new GenerateRange(6, 10),
            h = new GenerateRange(6, 10),
            corridorLenth = new GenerateRange(3, 4),
            depth = new GenerateRange(1, 3),
            pool = new ItemPool(),
            space = new GenerateRange(36, 200)
        });
        roomData.Add(new RoomGenerationParameters()
        {
            w = new GenerateRange(6, 10),
            h = new GenerateRange(6, 10),
            corridorLenth = new GenerateRange(3, 4),
            depth = new GenerateRange(1, 3),
            pool = new ItemPool(),
            space = new GenerateRange(36, 200)
        });
        roomData.Add(new RoomGenerationParameters()
        {
            w = new GenerateRange(6, 10),
            h = new GenerateRange(6, 10),
            corridorLenth = new GenerateRange(3, 4),
            depth = new GenerateRange(1, 3),
            pool = new ItemPool(),
            space = new GenerateRange(36, 200)
        });
        roomData.Add(new RoomGenerationParameters()
        {
            w = new GenerateRange(6, 10),
            h = new GenerateRange(6, 10),
            corridorLenth = new GenerateRange(3, 4),
            depth = new GenerateRange(1, 3),
            pool = new ItemPool(),
            space = new GenerateRange(36, 200)
        });
        DungeonGenerateParameters parameters = new DungeonGenerateParameters()
        {
            sizeW = 1000,
            sizeH = 1000,
            maxDepth = 20,
            maxCountOfGenerateTry = 3,
            roomsData = roomData,
        };

        DungeonData dungeon = new DungeonData(parameters);
        this.dungeon = dungeon;
    }
    private void DrawDungeon() {
        for (int i = 0; i < dungeon.sizeW; i++)
        {
            for (int j = 0; j < dungeon.sizeH; j++)
            {
                if (!(dungeon.cellMap.cells[i][j].empty))
                {
                    float newX = 0.5f * (i - j);
                    float newY = 0.25f * (i + j);
                    GameObject newObj = Instantiate(cellGameObject, new Vector3(newX, newY, +newY), new Quaternion());
                    objects.Add(newObj);
                }
            }
        }
    }

    private void ClearDungeon() { 
        for (int i = 0; i < objects.Count; i++)
        {
            Destroy(objects[i]);
        }
        objects.Clear();
    }
}
