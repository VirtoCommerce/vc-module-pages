using System;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Platform.Core.Events;

namespace VirtoCommerce.Pages.Core.Events
{
    public class PagesDomainEvent : IEvent
    {
        public string Id { get; set; }
        public int Version { get; set; }
        public DateTimeOffset TimeStamp { get; set; }
        public PageDocument Page { get; set; }
        public PageOperation Operation { get; set; }
    }
}
