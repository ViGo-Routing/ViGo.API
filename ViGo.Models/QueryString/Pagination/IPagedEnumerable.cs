using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViGo.Models.QueryString.Pagination
{
    /// <summary>
    /// Represents a list of entities that is paged
    /// </summary>
    /// <typeparam name="T">Type of the entities</typeparam>
    public interface IPagedEnumerable<T>
    {
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
        public bool HasPreviousPage { get; }

        /// <summary>
        /// Previous Page
        /// </summary>
        public Uri? PreviousPage { get; }

        /// <summary>
        /// Has Next Page
        /// </summary>
        public bool HasNextPage { get; }

        /// <summary>
        /// Next Page
        /// </summary>
        public Uri? NextPage { get; }
    }
}
