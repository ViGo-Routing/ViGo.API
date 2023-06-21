using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Repository.Pagination
{
    public class PaginationParameter
    {
        public static PaginationParameter Default => new PaginationParameter();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public PaginationParameter()
        {
            PageNumber = 1;
            PageSize = 10;
        }

        public PaginationParameter(int pageNumber, int pageSize)
        {
            PageNumber = pageNumber < 1 ? 1 : pageNumber;
            PageSize = pageSize < 1 ? 1 : pageSize;
        }
    }
}
