namespace ViGo.Models.QueryString.Pagination
{
    public class PaginationParameter
    {
        //public static PaginationParameter Default => new PaginationParameter();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public PaginationParameter()
        {
            PageNumber = 1;
            PageSize = 10;
        }

        public PaginationParameter(int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}
