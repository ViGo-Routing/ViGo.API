using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ViGo.Models.QueryString.Pagination
{
    /// <summary>
    /// Represents a list of entities that is paged
    /// </summary>
    /// <typeparam name="T">Type of the entities</typeparam>
    public class PagedEnumerable<T> : IPagedEnumerable<T>
    {
        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source">Entities Source</param>
        /// <param name="pageNumber">Page Number</param>
        /// <param name="pageSize">Page Size</param>
        /// <param name="totalRecords">Original Total records</param>
        /// <param name="baseUri">Base URI of the retrieving action</param>
        /// <param name="route">Route of the retrieving action</param>
        /// <param name="isOriginalSource">True if the source has not been paged. 
        /// The paging process with take place in this constructor</param>
        public PagedEnumerable(IEnumerable<T> source,
            int pageNumber, int pageSize, int totalRecords,
            string baseUri, string route, bool isOriginalSource = false)
        {
            // Minimum allowed page number is 1
            pageNumber = pageNumber < 1 ? 1 : pageNumber;

            // Minimum allowed page size is 1, -1 to get all
            pageSize = pageSize == -1 ? totalRecords :
                pageSize < 1 ? 1 : pageSize;

            TotalCount = totalRecords;
            TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            PageSize = pageSize;
            PageNumber = pageNumber;
            //var test = source.Skip((pageIndex - 1) * pageSize).Take(pageSize);
            Data = isOriginalSource ? source.Skip((pageNumber - 1) * pageSize).Take(pageSize) : source;

            NextPage =
                pageNumber >= 1 && pageNumber < TotalPages
                ? GeneratePageUri(pageSize, pageNumber, baseUri, route)
                : null;
            PreviousPage =
                pageNumber - 1 >= 1 && pageNumber <= TotalPages
                ? GeneratePageUri(pageSize, pageNumber, baseUri, route)
                : null;
            FirstPage = GeneratePageUri(pageSize, 1, baseUri, route);
            LastPage = GeneratePageUri(pageSize, TotalPages, baseUri, route);

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source">Entities Source</param>
        /// <param name="pageNumber">Page Number</param>
        /// <param name="pageSize">Page Size</param>
        public PagedEnumerable(IEnumerable<T> source,
            int pageNumber, int pageSize)
        {
            // Minimum allowed page number is 1
            pageNumber = (pageNumber < 1 || pageSize == -1) ? 1 : pageNumber;

            // Minimum allowed page size is 1, -1 to get all
            pageSize = pageSize == -1 ? source.Count() :
                pageSize < 1 ? 1 : pageSize;

            TotalCount = source.Count();
            TotalPages = (int)Math.Ceiling((double)TotalCount / pageSize);

            PageSize = pageSize;
            PageNumber = pageNumber;
            //var test = source.Skip((pageIndex - 1) * pageSize).Take(pageSize);
            Data = source.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }
        #endregion

        #region Private member
        #endregion
        private Uri GeneratePageUri(int pageSize, int pageNumber, string baseUri, string route)
        {
            Uri endpoint = new Uri(string.Concat(baseUri, route));
            var uriBuilder = new UriBuilder(endpoint);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["pageNumber"] = pageNumber.ToString();
            query["pageSize"] = pageSize.ToString();

            uriBuilder.Query = query.ToString();
            return new Uri(uriBuilder.ToString());
        }
        #region Properties
        /// <summary>
        /// List of values
        /// </summary>
        public IEnumerable<T> Data { get; }
        /// <summary>
        /// Page Number
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// Page Size
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// First Page
        /// </summary>
        public Uri? FirstPage { get; }

        /// <summary>
        /// Last Page
        /// </summary>
        public Uri? LastPage { get; }

        /// <summary>
        /// Total Count of Entities
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Total Pages
        /// </summary>
        public int TotalPages { get; }

        /// <summary>
        /// Has Previous Page
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// Previous Page
        /// </summary>
        public Uri? PreviousPage { get; }

        /// <summary>
        /// Has Next Page
        /// </summary>
        public bool HasNextPage => PageNumber + 1 <= TotalPages;

        /// <summary>
        /// Next Page
        /// </summary>
        public Uri? NextPage { get; }
        #endregion
    }
}
