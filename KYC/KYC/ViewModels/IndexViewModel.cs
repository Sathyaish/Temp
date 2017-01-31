using KYC.Contracts;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace KYC.ViewModels
{
    public class IndexViewModel
    {
        public bool IsPostback { get; set; }
        public string Url { get; set; }

        public bool Failed { get; set; }
        public string ErrorMessage { get; set; }

        public IEnumerable<Subscription> Subscriptions { get; set; }

        public Subscription AddSubscription(Subscription subscription)
        {
            if (Subscriptions == null)
                Subscriptions = new List<Subscription>();

            var list = Subscriptions as List<Subscription>;

            list.Add(subscription);

            return subscription;
        }

        public int SubscriptionCount
        {
            get
            {
                return Subscriptions.Count();
            }
        }
    }
}