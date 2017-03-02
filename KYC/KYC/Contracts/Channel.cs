using System;

namespace KYC.Contracts
{
    public class Channel
    {
        public string ETag { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime MemberSince { get; set; }
        public string HighResolutionThumbnailUrl { get; set; }
        public string UploadsPlaylistId { get; set; }
        public long ViewCount { get; set; }
        public long SubscriberCount { get; set; }
        public int VideoCount { get; set; }
        public int SubscriptionCount { get; set; }
    }
}