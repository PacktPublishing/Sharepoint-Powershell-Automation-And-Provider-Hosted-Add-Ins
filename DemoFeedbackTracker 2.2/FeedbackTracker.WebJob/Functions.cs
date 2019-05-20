using FeedbackTracker.Common;
using FeedbackTracker.Common.Models;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FeedbackTracker.WebJob
{
    public static class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue. This triggering is done due to the QueueTrigger attribute
        public static void ProcessQueueMessage(
            [QueueTrigger(ChangeManager.StorageQueueName)]NotificationModel notification, 
            TextWriter log)
        {
            log.WriteLine(String.Format("Processing subscription {0} for site {1}", notification.SubscriptionId, notification.SiteUrl));
            ChangeManager changeManager = new ChangeManager();
            changeManager.ProcessNotification(notification);
        }
    }
}
