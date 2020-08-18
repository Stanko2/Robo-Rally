using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Mirror;
using UnityEngine.Serialization;

public static class Saver{
    private static string MapSavePath{
        get{
            return $"{Application.persistentDataPath}/Maps";
        }
    }
    public static string TemporaryPath{ get {return Path.GetTempPath(); } }
    public static void Save(TileCollection data, string name){
        XmlSerializer xml = new XmlSerializer(typeof(TileCollection), new System.Type[]{typeof(SerializedTile)});
        if(!Directory.Exists(MapSavePath)) Directory.CreateDirectory(MapSavePath);
        if(File.Exists($"{MapSavePath}/{name}.rbl")) File.Delete($"{MapSavePath}/{name}.rbl");
        // File.WriteAllText($"{MapSavePath}/{Name}.rbl", JsonUtility.ToJson(data));
        using(FileStream stream = File.Create($"{MapSavePath}/{name}.rbl")){
            xml.Serialize(stream, data);
        }
    }
    public static void Save(byte[] data, string name){
        if(!Directory.Exists(MapSavePath)) Directory.CreateDirectory(MapSavePath);
        if(File.Exists($"{MapSavePath}/{name}.rbl")) File.Delete($"{MapSavePath}/{name}.rbl");
        Debug.Log(data);
        File.WriteAllBytes($"{MapSavePath}/{name}.rbl", data);
    }
    public static void Delete(string name){
        if(File.Exists($"{MapSavePath}/{name}.rbl")) File.Delete($"{MapSavePath}/{name}.rbl");
    }

    public static TileCollection Load(string name){
        XmlSerializer xml = new XmlSerializer(typeof(TileCollection));
        if(!File.Exists($"{MapSavePath}/{name}.rbl")) return null;
        using(FileStream stream = File.Open($"{MapSavePath}/{name}.rbl",FileMode.Open)){
            TileCollection tiles = (TileCollection)xml.Deserialize(stream);
            tiles.Loaded = true;
            //Debug.Log(tiles.tiles[58].buildIndex);
            return tiles;
        }
    }

    public static void DeleteTemporary(){
        string[] maps = ListSavedMaps();
        foreach (var map in maps)
        {
            if(Regex.Match(map, @"temp_[0-9]*").Length > 0){
                Delete(map);
            }   
        }
    }

    public static byte[] ReadFully(this Stream input)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            input.CopyTo(ms);
            return ms.ToArray();
        }
    }

    public static byte[] GetMapBytes(string mapName){
        if(!File.Exists($"{MapSavePath}/{mapName}.rbl")) return null;
        using(FileStream stream = new FileStream($"{MapSavePath}/{mapName}.rbl", FileMode.Open)){
            byte[] mapData = stream.ReadFully();
            stream.Close();
            return mapData;
        }
    }

    public static string[] ListSavedMaps(){
        if(!Directory.Exists(MapSavePath)) return null;
        string[] maps = Directory.GetFiles(MapSavePath,"*.rbl");
        for (int i = 0; i < maps.Length; i++)
        {
            maps[i] = Path.GetFileNameWithoutExtension(maps[i]);
        }
        return maps;
    }
}
[System.Serializable]
public class TileCollection{
    public int width, height;
    public SerializedTile[] tiles;
    [FormerlySerializedAs("StartX")] public int startX;
    [FormerlySerializedAs("StartY")] public int startY;
    public Vector2 StartLocation => new Vector2(startX, startY);
    public string name;
    [System.NonSerialized]
    public bool Loaded;
    public static TileCollection Create(Map.Map map){
        var c = new TileCollection
        {
            width = map.width,
            height = map.height,
            name = map.name,
            startX = (int) map.startLocation.x,
            startY = (int) map.startLocation.y
        };
        c.tiles = new SerializedTile[c.width * c.height];
        for (var i = 0; i < c.width; i++)
        {
            for (var j = 0; j < c.height; j++)
            {
                c.tiles[i + c.width * j] = new SerializedTile(map.tiles[i,j].walls, map.tiles[i,j].buildIndex, map.tiles[i,j].Direction);                
            }
        }
        return c;
    }
    public TileCollection() {}
    // public TileCollection(){
    //     width = 10;
    //     height = 10;
    //     StartX = 0;
    //     StartY = 0;
    //     tiles = new SerializedTile[width * height];
    //     for (int i = 0; i < width; i++)
    //     {
    //         for (int j = 0; j < height; j++)
    //         {
    //             tiles[i + width * j] = new SerializedTile();                
    //         }
    //     }
    // }
}

[System.Serializable]
public class SerializedTile{
    [FormerlySerializedAs("Walls")] public bool[] walls;
    public int buildIndex;
    [FormerlySerializedAs("Direction")] public int direction;
    public SerializedTile(bool[] walls, int buildIndex, int dir){
        direction = dir;
        this.walls = walls;
        this.buildIndex = buildIndex;
    }
    // public SerializedTile(){
    //     Direction = 0;
    //     Walls = new bool[]{false,false, false, false};
    //     buildIndex = 0;
    // }
    public SerializedTile(){}
}

public class SerializedConveyorBelt : SerializedTile{
    public SerializedConveyorBelt(bool[] walls, int buildindex, int dir) : base(walls, buildindex, dir){
        
    }
}