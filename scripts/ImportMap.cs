using JosephEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
public struct TerrainLayerData
{
    public string url;
    public float sizeX;
    public float sizeY;
}
public class ImportMap : MonoBehaviour
{
    public Dropdown dd_mapItem;
    public Terrain terrain;
    public Button btn_quit, btn_next, btn_before;
    public Text tt_current;
    public InputField if_current;
    Dictionary<long, string> crcAndUrl = new Dictionary<long, string>();
    public TerrainData terrainData;
    // Start is called before the first frame update
    void Start()
    {
        GetAllcrcAndUrl($"config/property");
        Init_UI();
    }
    void GetAllcrcAndUrl(string directoryPath)
    {
        List<string> fileList = GetAllFilesInDirectory(directoryPath);
        foreach (var fileUrl in fileList)
        {
            ArrayList arrayList = GetCRC(fileUrl);
            try
            {
                crcAndUrl.Add((long)arrayList[0], arrayList[1].ToString());
            }
            catch (Exception e)
            {
                //print(e.StackTrace);
            }
        }
    }
    void ParseAreaData(string url)
    {
        using (StreamReader sr = File.OpenText(url))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith("Start Object"))
                {
                    ParseObject(sr);
                }
            }
        }
    }
    void ParseObject(StreamReader sr)
    {
        string line;
        line = sr.ReadLine();
        string[] split = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        Vector3 position = new Vector3(float.Parse(split[0]), float.Parse(split[2]), float.Parse(split[1]));
        line = sr.ReadLine();
        long dwCRC = long.Parse(line);
        line = sr.ReadLine();
        split = line.Split(new[] { '#' }, StringSplitOptions.RemoveEmptyEntries);
        float m_fYaw = float.Parse(split[0]);
        float m_fPitch = float.Parse(split[1]);
        float m_fRoll = float.Parse(split[2]);
        line = sr.ReadLine();
        float m_fHeightBias = float.Parse(line);

        SetObjectInGame(position, dwCRC, m_fYaw, m_fPitch, m_fRoll, m_fHeightBias);
    }
    void SetObjectInGame(Vector3 position, long dwCRC, float m_fYaw, float m_fPitch, float m_fRoll, float m_fHeightBias)
    {
        string buildingfile = string.Empty;
        try
        {
            buildingfile = crcAndUrl[dwCRC];
            GameObject instance = Instantiate(Resources.Load(buildingfile, typeof(GameObject))) as GameObject;
            instance.tag = "building";
            instance.transform.position = (position + new Vector3(0f, m_fHeightBias, 0f)) / 100f + new Vector3(0f, 0f, 256f);
            instance.transform.Rotate(0f, 0f, m_fYaw);
            instance.transform.Rotate(m_fPitch, 0f, 0f);
            instance.transform.Rotate(0f, m_fRoll, 0f);
        }
        catch (Exception e)
        {
            //print(e.StackTrace);
        }
    }
    ArrayList GetCRC(string url)
    {
        ArrayList arrayList = new ArrayList();
        using (StreamReader sr = File.OpenText(url))
        {
            try
            {
                string line;
                sr.ReadLine();
                line = sr.ReadLine();
                arrayList.Add(long.Parse(line));
                line = sr.ReadLine();
                string[] split = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                arrayList.Add(split[1].Replace("work/", string.Empty).Replace('"', '\0').Replace(".gr2", "out"));
            }
            catch { }
            return arrayList;
        }
    }
    List<string> GetAllFilesInDirectory(string directoryPath)
    {
        List<string> fileList = new List<string>();

        try
        {
            string[] filePaths = Directory.GetFiles(directoryPath, "*.prb");
            fileList.AddRange(filePaths);

            string[] subDirectories = Directory.GetDirectories(directoryPath);

            foreach (var dir in subDirectories)
            {
                fileList.AddRange(GetAllFilesInDirectory(dir));
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error: " + e.Message);
        }

        return fileList;
    }
    List<string> GetAllAreaDataFiles(string rootDirectory, string fileName)
    {
        List<string> filePaths = new List<string>();

        string[] subDirectories = Directory.GetDirectories(rootDirectory);

        foreach (var dir in subDirectories)
        {
            string[] filesInSubdirectory = Directory.GetFiles(dir, fileName, SearchOption.AllDirectories);

            filePaths.AddRange(filesInSubdirectory);
        }

        return filePaths;
    }
    void Loadbuildings(string rootDirectoryPath)
    {
        List<string> filePaths = GetAllAreaDataFiles(rootDirectoryPath, "areadata.txt");
        foreach (var file in filePaths)
        {
            ParseAreaData(file);
        }
    }
    void Init_UI()
    {
        dd_mapItem.AddOptions(GetAllMaps($"config/map"));
        dd_mapItem.onValueChanged.AddListener(OnDropDown);
        btn_quit.onClick.AddListener(Quit);
        btn_next.onClick.AddListener(Next);
        btn_before.onClick.AddListener(Before);
        if_current.onEndEdit.AddListener(OnEndEdit);
    }
    void OnEndEdit(string str)
    {
        if (string.IsNullOrEmpty(str)) return;
        int index = int.Parse(str);
        dd_mapItem.value = index;
    }
    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
    void Next()
    {
        int cnt = dd_mapItem.options.Count - 1;
        if (dd_mapItem.value == cnt)
        {
            dd_mapItem.value = 0;
        }
        if_current.text = (++dd_mapItem.value).ToString();
    }
    void Before()
    {
        int cnt = dd_mapItem.options.Count - 1;
        if (dd_mapItem.value <= 1)
        {
            dd_mapItem.value = cnt;
            if_current.text = cnt.ToString();
        }
        else
            if_current.text = (--dd_mapItem.value).ToString();
    }
    List<string> GetAllMaps(string directory_url)
    {
        List<string> all_maps = new List<string>();
        if (Directory.Exists(directory_url))
        {
            string[] urls = Directory.GetDirectories(directory_url);
            foreach (string url in urls)
            {
                all_maps.Add(url);
            }
            tt_current.text = "/" + urls.Length;
        }
        return all_maps;
    }

    void OnDropDown(int index)
    {
        BroadcastMessage("SetPreviousIndex", index);
        if (index != 0)
        {
            if_current.text = dd_mapItem.value.ToString();
            GameObject[] gos_old = GameObject.FindGameObjectsWithTag("building");
            foreach (GameObject go in gos_old)
                Destroy(go);
            GameObject[] gos_terrains = GameObject.FindGameObjectsWithTag("map");
            foreach (GameObject go in gos_terrains)
                Destroy(go);
            LoadMaps(dd_mapItem.captionText.text);
            Loadbuildings(dd_mapItem.captionText.text);
        }
    }

    void LoadMaps(string directory_url)
    {
        //try
        //{
        //    Josephf.MapLoad(terrain, dd_mapItem.captionText.text);
        //}
        //catch
        //{
        //}
        int mapWidth = 0, mapHeight = 0;
        float cellScale = 0f;
        terrain.terrainData.terrainLayers = null;
        List<TerrainLayerData> terrainLayerDatas = new List<TerrainLayerData>();
        string map_url = string.Empty;
        cellScale = 0f;
        float heightScale = 0f;
        using (StreamReader sr = File.OpenText(directory_url + "/setting.txt"))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.Contains("CellScale"))
                {
                    string[] split = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    cellScale = float.Parse(split[1]);
                }
                else if (line.Contains("HeightScale"))
                {
                    string[] split = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    heightScale = float.Parse(split[1]);
                }
                else if (line.Contains("TextureSet"))
                {
                    string[] split = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    map_url = split[1];
                }
                else if (line.Contains("MapSize"))
                {
                    string[] split = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    mapWidth = int.Parse(split[1]);
                    mapHeight = int.Parse(split[2]);
                }
            }
        }
        //print(cellScale);
        cellScale = 256f;

        using (StreamReader sr = File.OpenText($"config/" + map_url))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith("Start Texture"))
                {
                    terrainLayerDatas.Add(GetLayerData(sr));
                }
            }
        }

        TerrainLayer[] newTerrainLayer = new TerrainLayer[terrainLayerDatas.Count];
        for (int i = 0; i < newTerrainLayer.Length; i++)
        {
            newTerrainLayer[i] = new TerrainLayer();
            string _url = terrainLayerDatas[i].url.Replace('"', '\0').Remove(0, 18).Replace(".dds", string.Empty);
            newTerrainLayer[i].diffuseTexture = Resources.Load<Texture2D>(_url);
            newTerrainLayer[i].tileSize = new Vector2(terrainLayerDatas[i].sizeX, terrainLayerDatas[i].sizeY);
        }

        terrain.terrainData.terrainLayers = newTerrainLayer;

        for (int tileX = 0; tileX < mapWidth; tileX++)
            for (int tileY = 0; tileY < mapHeight; tileY++)
            {
                Terrain instance = Instantiate(terrain, terrain.GetPosition() + new Vector3(tileX, 0f, -tileY) * cellScale, terrain.transform.rotation);
                instance.tag = "map";
                TerrainData instanceData = Instantiate(terrainData);
                //print(instanceData.alphamapResolution);
                float _height = 1.28f * 256f;
                instanceData.size = new Vector3(cellScale, _height, cellScale);

                int resolution = instanceData.heightmapResolution; // 129
                //float[,] data = new float[resolution, resolution];
                //instanceData.SetHeights(0, 0, data);
                string url = directory_url + $"/00{tileX}00{tileY}/height.raw";


                int size = 131;

                float[,] data = new float[size - 2, size - 2];
                instanceData.SetHeights(0, 0, data);
                using (var file = System.IO.File.OpenRead(url))
                using (var reader = new System.IO.BinaryReader(file))
                {
                    for (int y = 0; y <= size - 1; y++)
                    //for (int y = size - 1; y >= 0; y--)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float v = (float)reader.ReadUInt16() / 0xFFFF;
                            if (x >= 1 && y >= 1 && x <= size - 2 && y <= size - 2)
                            {
                                //data[y - 1, x - 1] = v;
                                data[(size - 2) - y, x - 1] = v;
                            }
                        }
                    }
                }
                instanceData.SetHeights(0, 0, data);

                int[,] __texData = new int[258, 258];
                url = directory_url + $"/00{tileX}00{tileY}/tile.raw";
                LoadTextures(url, ref __texData);

                //LoadFullTextures(texData, instanceData, 258, 258);
                int width = 256, height = 256;
                float[,,] texData = instanceData.GetAlphamaps(0, 0, width, height);
                {
                    //for (int y = 0; y < height; y++)
                    //for (int y = size - 1; y >= 0; y--)
                    for (int y = height - 1; y >= 0; y--)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            {
                                var count = 0;
                                float[] texWeights = new float[instanceData.terrainLayers.Length];
                                // BOTTOM
                                if (x - 1 >= 0 && y - 1 >= 0)
                                {
                                    count++;
                                    texWeights[__texData[x - 1, y - 1]] += 1 / 9f;
                                }

                                if (y - 1 >= 0)
                                {
                                    // here
                                    count++;
                                    try
                                    {
                                        texWeights[__texData[x, y - 1]] += 1;
                                    }
                                    catch { break; }
                                }

                                if (x + 1 < width && y - 1 >= 0)
                                {
                                    count++;
                                    try
                                    {
                                        texWeights[__texData[x + 1, y - 1]] += 1;
                                    }
                                    catch { }
                                }

                                // CENTER
                                if (x - 1 >= 0)
                                {
                                    count++;
                                    texWeights[__texData[x - 1, y]] += 1;
                                }

                                if (true)
                                {
                                    // there
                                    count++;
                                    try
                                    {
                                        texWeights[__texData[x, y]] += 1;
                                    }
                                    catch { break; }
                                }

                                if (x + 1 < width)
                                {
                                    count++;
                                    try
                                    {
                                        texWeights[__texData[x + 1, y]] += 1;
                                    }
                                    catch { }
                                }

                                // TOP
                                if (x - 1 >= 0 && y + 1 < height)
                                {
                                    count++;
                                    texWeights[__texData[x - 1, y]] += 1;
                                }

                                if (y + 1 < height)
                                {
                                    count++;
                                    texWeights[__texData[x, y]] += 1;
                                }

                                if (x + 1 < width && y + 1 < height)
                                {
                                    count++;
                                    try
                                    {
                                        texWeights[__texData[x + 1, y]] += 1;
                                    }
                                    catch { }
                                }

                                for (var iTex = 0; iTex < instanceData.terrainLayers.Length; iTex++)
                                {
                                    try
                                    {
                                        texData[y, x, iTex] = texWeights[iTex] / (float)count;
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
                instanceData.SetAlphamaps(0, 0, texData);




                instance.terrainData = instanceData;
                instance.gameObject.SetActive(true);
            }
    }
    TerrainLayerData GetLayerData(StreamReader sr)
    {
        TerrainLayerData terrainLayerData = new TerrainLayerData();
        terrainLayerData.url = sr.ReadLine();
        terrainLayerData.sizeX = float.Parse(sr.ReadLine());
        terrainLayerData.sizeY = float.Parse(sr.ReadLine());
        return terrainLayerData;
    }
    void LoadTextures(string tileFileName, ref int[,] fullTexData)
    {
        int size = 258;
        using (var texFile = System.IO.File.OpenRead(tileFileName))
        using (var texReader = new System.IO.BinaryReader(texFile))
        {
            for (int y = size - 1; y >= 0; y--)
            {
                for (int x = 0; x < size; x++)
                {
                    int tex = texReader.ReadByte();
                    try
                    {
                        fullTexData[x, y] = tex - 1;
                    }
                    catch { break; }
                }
            }
        }
    }
}
