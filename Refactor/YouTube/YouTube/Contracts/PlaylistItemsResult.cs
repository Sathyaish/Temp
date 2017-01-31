using System.Collections.Generic;

namespace YouTube.Contracts
{
    public class PlaylistItemsResult
    {
        public string Id { get; set; }
        public string NextPageToken { get; set; }
        public string PreviousPageToken { get; set; }
        public PageInfo PageInfo { get; set; }

        public IEnumerable<Item> Items { get; set; }
    }
}
