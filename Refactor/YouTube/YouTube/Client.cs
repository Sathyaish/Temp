using System.Threading.Tasks;
using YouTube.Contracts;

namespace YouTube
{
    public class Client
    {
        private string _apiKey = null;

        public Client(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<PlaylistItemsResult> GetVideosInPlaylistAsync(
            string playlistId, 
            string nextPageToken = null)
        {
            await Task.FromResult(0);

            return default(PlaylistItemsResult);
        }

        public async Task<string> GetPlaylistNameAsync(string playlistId)
        {
            await Task.FromResult(0);

            return default(string);
        }

        // OAuth endpoint
        public async Task<PlaylistItemsResult> GetUserHistoryAsync(
            string accessToken)
        {
            await Task.FromResult(0);

            return default(PlaylistItemsResult);
        }
    }
}