using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Domain.Enumerations;

namespace ViGo.Models.Reports
{
    public class ReportUpdateModel
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public ReportType? Type { get; set; }

    }
}
