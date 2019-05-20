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
    public class Program
    {
        static void Main(string[] args)
        {
            JobHostConfiguration config = new JobHostConfiguration();
            config.Queues.BatchSize = 1;
            config.Queues.MaxDequeueCount = 1;
            var host = new JobHost(config);

            // The following code ensures that the WebJob will be running continuously
            host.RunAndBlock();
        }


    }
}
