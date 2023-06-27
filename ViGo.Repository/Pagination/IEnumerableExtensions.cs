using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViGo.Repository.Pagination;
using ViGo.Utilities;

namespace ViGo.Repository.Pagination
{
    public static class IEnumerableExtensions
    {
        public static IPagedEnumerable<T> ToPagedEnumerable<T>(
            this IEnumerable<T> source, int pageNumber, int pageSize)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            pageSize = pageSize < 1 ? 1 : pageSize;
            return new PagedEnumerable<T>(
                source,
                pageNumber, pageSize);
        }

        public static IPagedEnumerable<T> ToPagedEnumerable<T>(
            this IEnumerable<T> source, int pageNumber, int pageSize, 
            int totalRecords,
            HttpContext context, bool isOriginalSource = true)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            pageSize = pageSize < 1 ? 1 : pageSize;
            return new PagedEnumerable<T>(
                source, 
                pageNumber, pageSize, totalRecords,
                context.GetBaseUri(), context.Request.Path.Value, isOriginalSource);
        }
    }
}
