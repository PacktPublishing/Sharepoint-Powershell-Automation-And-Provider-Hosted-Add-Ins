using System;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DemoFeedbackTracker_2._0Web.SQL
{
    public partial class SharePointWebHooks : DbContext
    {
        public SharePointWebHooks()
            : base("name=pnpwebhooksdemoEntities")
        {
        }


        public SharePointWebHooks(string name)
            : base("name=" + name)
        {
        }

        public virtual DbSet<ListWebHook> ListWebHooks { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
        }
    }
}