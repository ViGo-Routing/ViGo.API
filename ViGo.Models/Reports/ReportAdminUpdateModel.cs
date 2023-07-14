using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.Reports
{
    public class ReportAdminUpdateModel
    {
        public string? ReviewerNote { get; set; }
        public ReportStatus? Status { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
