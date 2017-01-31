using System.Collections.Generic;

namespace KYC.Contracts
{
    public class Subscription
    {
        public string Id { get; set; }
        public string ChannelId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string DefaultThumbnailUrl { get; set; }

        public int UploadCount { get; set; }
        public int RecentUploadCount { get; set; }

        public IEnumerable<Video> Uploads { get; set; }
    }
}