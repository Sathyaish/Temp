using KYC.Contracts;
using System.Collections.Generic;

namespace KYC.ViewModels
{
    public class MeIndexViewModel : ViewModelBase
    {
        public ViewModes ViewMode { get; set; }

        public Channel Channel { get; set; }

        public IEnumerable<Subscription> Subscriptions { get; set; }

        public Subscription AddSubscription(Subscription subscription)
        {
            if (Subscriptions == null)
                Subscriptions = new List<Subscription>();

            var list = Subscriptions as List<Subscription>;

            list.Add(subscription);

            return subscription;
        }
    }
}