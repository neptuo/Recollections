
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Neptuo.Recollections.Accounts;
using Neptuo.Recollections.Components;

namespace Neptuo.Recollections.Entries;

internal class ApiMapService(Api api, PropertyCollection properties) : IMapService
{
    public Task<List<MapSearchModel>> GetGeoLocateListAsync(string query)
        => api.GetGeoLocateListAsync(query);

    public Task<Stream> GetTileAsync(string type, int x, int y, int z)
        => api.GetTileAsync(type, x, y, z);

    public Task<string> GetTypeAsync()
        => properties.MapTypeAsync();

    public Task SetTypeAsync(string type)
        => properties.MapTypeAsync(type);
}
