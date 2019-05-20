using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FeedbackTracker.Common.Models
{
    public class SubscriptionModel
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "clientState", NullValueHandling = NullValueHandling.Ignore)]
        public string ClientState { get; set; }

        [JsonProperty(PropertyName = "expirationDateTime")]
        public DateTime ExpirationDateTime { get; set; }

        [JsonProperty(PropertyName = "notificationUrl")]
        public string NotificationUrl { get; set; }

        [JsonProperty(PropertyName = "resource", NullValueHandling = NullValueHandling.Ignore)]
        public string Resource { get; set; }
    }

    /// <summary>
    /// generic class used to hold a collection of objects
    /// </summary>
    /// <typeparam name="T">Type of object</typeparam>
    public class ResponseModel<T>
    {
        [JsonProperty(PropertyName = "value")]
        public List<T> Value { get; set; }
    }

    /// <summary>
    /// Web hook notification model: this message is received when SharePoint "fires" a web hook 
    /// </summary>
    public class NotificationModel
    {
        [JsonProperty(PropertyName = "subscriptionId")]
        public string SubscriptionId { get; set; }

        [JsonProperty(PropertyName = "clientState")]
        public string ClientState { get; set; }

        [JsonProperty(PropertyName = "expirationDateTime")]
        public DateTime ExpirationDateTime { get; set; }

        [JsonProperty(PropertyName = "resource")]
        public string Resource { get; set; }

        [JsonProperty(PropertyName = "tenantId")]
        public string TenantId { get; set; }

        [JsonProperty(PropertyName = "siteUrl")]
        public string SiteUrl { get; set; }

        [JsonProperty(PropertyName = "webId")]
        public string WebId { get; set; }
    }

    public class SharePointList
    {
        public string Title { get; set; }
        public Guid Id { get; set; }
    }

    public class SharePointSiteModel
    {
        public Guid SelectedSharePointList { get; set; }
        public IEnumerable<SharePointList> Lists { get; set; }
        public IEnumerable<SubscriptionModel> WebHooks { get; set; }
    }
}