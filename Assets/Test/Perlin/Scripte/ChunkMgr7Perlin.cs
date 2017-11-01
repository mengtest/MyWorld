﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMgr7Perlin : MonoBehaviour
{
    [Label("玩家")]
    public Transform player;
    [Label("图快预制体")]
    [SerializeField]
    private GameObject _chunkPrefab;
    [SerializeField]
    [Label("点击显示的高亮预制体")]
    private GameObject _hightBlock;

    [SerializeField]
    [Label("是否预加载图块")]
    private bool _preload = true;
    /// <summary>
    /// 加载范围以玩家为中心点的正方向边长的一半
    /// </summary>
    [SerializeField]
    [Header("初始化图块范围")]
    [Range(0, 100)]
    private int _preloadRange = 60;

    [Label("最大图块高度")]
    public int _maxHigh = 8;

    /// <summary>
    /// 保存所有chunk
    /// </summary>
    [HideInInspector]
    private static List<Chunk7Perlin> _chunks = new List<Chunk7Perlin>();

    private static ChunkMgr7Perlin _instance;
    public static ChunkMgr7Perlin Instance()
    {
        return _instance;
    }

    private void Awake()
    {
        _instance = this;

        LoadPlayerAround();
    }

    private void Update()
    {
        SpawnChunk();
        DestoryChunk();
        LoadChunkAround();
        BlockContrller();
    }

    /// <summary>
    /// 加载玩家周围的图块
    /// </summary>
    private void LoadPlayerAround()
    {
        if (!_preload)
            return;

        for (float x = player.position.x - _preloadRange; x < player.position.x + _preloadRange; x += Chunk.length)
        {
            int chunkX = Mathf.FloorToInt(x / Chunk.length);
            for (float z = player.position.z - _preloadRange; z < player.position.z + _preloadRange; z += Chunk.width)
            {
                int chunkZ = Mathf.FloorToInt(z / Chunk.width);
                for (float y = player.position.y - _preloadRange; y < player.position.y + _preloadRange; y += Chunk.height)
                {
                    int chunkY = Mathf.FloorToInt(y / Chunk.height);
                    if (!HasChunkByChunkPos(new Vector3(chunkX, chunkY, chunkZ)))
                    {
                        AddChunk(chunkX, chunkY, chunkZ);
                    }
                }
            }
        }
    }

    public void AddChunk(int chunkX, int chunkY, int chunkZ)
    {
        GameObject go = Instantiate(_chunkPrefab, new Vector3(
            chunkX * Chunk.length, chunkY * Chunk.width, chunkZ * Chunk.height), Quaternion.identity);
        Chunk7Perlin chunk = go.GetComponent<Chunk7Perlin>();
        chunk.Init(chunkX, chunkY, chunkZ);
        _chunks.Add(chunk);
    }

    /// <summary>
    /// 加载图块周围的图块
    /// </summary>
    private void LoadChunkAround()
    {
        if (!_preload)
            return;

        for (int i = 0; i < _chunks.Count; i++)
        {
            Chunk7Perlin chunk = _chunks[i];
            chunk.LoadAround();
        }
    }

    /// <summary>
    /// 生产图块 创建地图
    /// </summary>
    private void SpawnChunk()
    {
        if (Chunk7Perlin.working)
            return;

        float lastDis = 99999999;
        Chunk7Perlin target = null;
        foreach (Chunk7Perlin chunk in _chunks)
        {
            float dis = Vector3.Distance(chunk.transform.position, player.position);
            if (dis < lastDis)
            {
                if (chunk.ready == false)
                {
                    lastDis = dis;
                    target = chunk;
                }
            }
        }

        if (target != null)
        {
            target.CalculateMap();
        }
    }

    /// <summary>
    /// 销毁图块
    /// </summary>
    private void DestoryChunk()
    {
        for (int i = _chunks.Count - 1; i >= 0; i--)
        {
            Chunk7Perlin chunk = _chunks[i];
            float dis = Vector3.Distance(chunk.transform.position, player.position);
            if (dis > (_preloadRange + Chunk.width * 2))
            {
                _chunks.Remove(chunk);
                Destroy(chunk.gameObject);
            }
        }
    }

    /// <summary>
    /// 图块控制
    /// </summary>
    private void BlockContrller()
    {
        RaycastHit hitInfo;
        if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hitInfo, 10f))
        {
            Vector3 pos = hitInfo.point - hitInfo.normal / 2;
            //Vector3 pos = new Vector3(hitX, hitY, hitZ);
            _hightBlock.transform.position = DataUtil.CeilToInt(pos);

            if (Input.GetMouseButton(0))
            {
                Chunk7Perlin chunk = GetChunkByWorldPos(DataUtil.CeilToInt(pos));
                chunk.SetBlock(pos, null);
            }
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                pos = hitInfo.point + hitInfo.normal / 2;
                Chunk7Perlin chunk = GetChunkByWorldPos(DataUtil.CeilToInt(pos));
                chunk.SetBlock(pos, BlockMap.GetBlock("TNT"));
            }
        }
        else
        {
            _hightBlock.transform.position = new Vector3(10000, 10000, 10000);
        }
    }

    /// <summary>
    /// 是否在预加载范围内
    /// </summary>
    /// <param name="worldPos"></param>
    /// <returns></returns>
    public bool IsInPreLoadRange(Vector3 worldPos)
    {
        return Vector3.Distance(worldPos, player.position) <= _preloadRange;
    }

    #region 获取图块
    public static Chunk7Perlin GetChunkByWorldPos(int x, int y, int z)
    {
        Vector3 pos = new Vector3(x, y, z);
        return GetChunkByWorldPos(pos);
    }

    public static Chunk7Perlin GetChunkByWorldPos(Vector3 pos)
    {
        foreach (Chunk7Perlin chunk in _chunks)
        {
            Vector3 chunkPos = chunk.transform.position;
            if (chunkPos.Equals(pos))
                return chunk;

            if (pos.x < chunkPos.x || pos.y < chunkPos.y || pos.z < chunkPos.z
                || pos.x >= chunkPos.x + Chunk.length || pos.y >= chunkPos.y + Chunk.height || pos.z >= chunkPos.z + Chunk.width)
                continue;

            return chunk;
        }

        return null;
    }

    public static Chunk7Perlin GetChunkByChunkPos(int x, int y, int z)
    {
        Vector3 pos = new Vector3(x, y, z);
        return GetChunkByChunkPos(pos);
    }

    public static Chunk7Perlin GetChunkByChunkPos(Vector3 pos)
    {
        foreach (Chunk7Perlin chunk in _chunks)
        {
            Vector3 chunkPos = chunk.GetChunkPos();
            if (chunkPos.Equals(pos))
                return chunk;
        }
        return null;
    }

    public static bool HasChunkByWorldPos(int x, int y, int z)
    {
        return HasChunkByWorldPos(new Vector3(x, y, z));
    }

    public static bool HasChunkByWorldPos(Vector3 pos)
    {
        foreach (Chunk7Perlin chunk in _chunks)
        {
            Vector3 chunkPos = chunk.transform.position;
            if (chunkPos.Equals(pos))
                return true;

            if (pos.x < chunkPos.x || pos.y < chunkPos.y || pos.z < chunkPos.z
                || pos.x >= chunkPos.x + Chunk.length || pos.y >= chunkPos.y + Chunk.height || pos.z >= chunkPos.z + Chunk.width)
                continue;

            return true;
        }
        return false;
    }

    public static bool HasChunkByChunkPos(int x, int y, int z)
    {
        Vector3 pos = new Vector3(x, y, z);
        return HasChunkByChunkPos(pos);
    }

    public static bool HasChunkByChunkPos(Vector3 pos)
    {
        foreach (Chunk7Perlin chunk in _chunks)
        {
            Vector3 chunkPos = chunk.GetChunkPos();
            if (chunkPos.Equals(pos))
                return true;
        }
        return false;
    }
    #endregion
}