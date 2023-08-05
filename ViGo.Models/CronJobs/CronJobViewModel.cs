using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Models.CronJobs
{
    public class CronJobViewModel
    {
        public string JobGroup { get; set; }
        public ICollection<JobViewModel> Jobs { get; set; }
    }

    public class JobViewModel
    {
        public string JobKey { get; set; }
        public string JobDescription { get; set; }
        public ICollection<TriggerViewModel> Triggers { get; set; }
    }

    public class TriggerViewModel
    {
        public string TriggerKey { get; set; }
        //public string TriggerName { get; set; }
        public string TriggerState { get; set; }
        public DateTimeOffset? NextFireTimeUtc { get; set; }
        public DateTimeOffset? PreviousFireTimeUtc { get; set; }
    }
}
