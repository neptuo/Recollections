using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Neptuo.Recollections.Entries;

namespace Neptuo.Recollections.Components;

public interface IMapService
{
    Task<List<MapSearchModel>> GetGeoLocateListAsync(string query);
    Task<Stream> GetTileAsync(string type, int x, int y, int z);
}