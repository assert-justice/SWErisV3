// using Eris;
// using ErisMath;

// namespace SpoonWitch.Game.Map.MapData;

// public class SwSectorData
// {
//     public readonly ErRect2I RectTiles;
//     public readonly ErVec2I SectorCoord;
//     public readonly int[][] Layers;
//     public SwSectorData(ErVec2I sectorCoord, ErVec2I sectorSizeTiles, int numTileLayers)
//     {
//         SectorCoord = sectorCoord;
//         RectTiles = new(SectorCoord * sectorSizeTiles, sectorSizeTiles);
//         Layers = new int[numTileLayers][];
//     }
//     private int GetTileIdx(ErVec2I tileCoord)
//     {
//         tileCoord -= RectTiles.Position;
//         return tileCoord.Y * RectTiles.Size.X + tileCoord.X;
//     }
//     public void SetTile(int layerIdx, ErVec2I tileCoord, int tileIdx)
//     {
//         if (!RectTiles.Contains(tileCoord))
//         {
//             ErEngine.LogWarning("attempted to set tile ", tileCoord, " of sector ", SectorCoord, ". coord out of bounds");
//             return;
//         }
//         if(Layers[layerIdx] is null)
//         {
//             Layers[layerIdx] = new int[RectTiles.Size.X * RectTiles.Size.Y];
//             Array.Fill(Layers[layerIdx], -1);
//         }
//         Layers[layerIdx][GetTileIdx(tileCoord)] = tileIdx;
//     }
//     public int GetTile(int layerIdx, ErVec2I tileCoord)
//     {
//         if (!RectTiles.Contains(tileCoord))
//         {
//             ErEngine.LogWarning("attempted to get tile ", tileCoord, " of sector ", SectorCoord, ". coord out of bounds");
//             return - 1;
//         }
//         if(Layers[layerIdx] is null) return -1;
//         return Layers[layerIdx][GetTileIdx(tileCoord)];
//     }
//     public int GetTopTile(ErVec2I tileCoord)
//     {
//         if (!RectTiles.Contains(tileCoord))
//         {
//             ErEngine.LogWarning("attempted to get top tile ", tileCoord, " of sector ", SectorCoord, ". coord out of bounds");
//             return - 1;
//         }
//         int tileIdx = GetTileIdx(tileCoord);
//         foreach (var layer in Layers)
//         {
//             if(layer is null) continue;
//             if(layer[tileIdx] >= 0) return layer[tileIdx];
//         }
//         return -1;
//     }
//     // public IEnumerable<(int layerIdx, ErVec2I tileCoord)> GetNonDefaultTiles()
//     // {
//     //     for (int layerIdx = 0; layerIdx < Layers.Length; layerIdx++)
//     //     {
//     //         if(Layers[layerIdx] is null) continue;
//     //         int width = RectTiles.Size.X;
//     //         for (int tileIdx = 0; tileIdx < Layers[layerIdx].Length; tileIdx++)
//     //         {
//     //             int tileId = Layers[layerIdx][tileIdx];
//     //             if(tileId < 0) continue;
//     //             int xi = tileIdx % width;
//     //             int yi = tileIdx / width;
//     //             yield return (tileId, new(xi,yi));
//     //         }
//     //     }
//     // }
// }
