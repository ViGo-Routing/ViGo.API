using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ViGo.HttpContextUtilities;
using ViGo.Models.QueryString.Pagination;
using System.Linq.Dynamic.Core;

namespace ViGo.Models.QueryString
{
    public static class IEnumerableExtensions
    {
        #region Paginatation
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
            HttpContext context, bool isOriginalSource = false)
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
        #endregion

        #region Sorting
        public static IEnumerable<T> Sort<T>(this IEnumerable<T> source, string orderByString)
        {
            if (source.Any())
            {
                if (!string.IsNullOrWhiteSpace(orderByString))
                {
                    var orderParams = orderByString.Trim().Split(",");
                    var propertyInfos = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    var orderQueryBuilder = new StringBuilder();

                    foreach (string param in orderParams)
                    {
                        if (!string.IsNullOrWhiteSpace(param))
                        {
                            string propertyFromQueryName = param.Split(" ")[0];
                            var objectProperty = propertyInfos.FirstOrDefault(
                                pi => pi.Name.Equals(propertyFromQueryName,
                                StringComparison.InvariantCultureIgnoreCase));

                            if (objectProperty != null)
                            {
                                string sortingOrder = param.EndsWith(" desc") ?
                                    "descending" : "ascending";

                                orderQueryBuilder.Append($"{objectProperty.Name.ToString()} {sortingOrder}, ");
                            }
                        }
                    }

                    string orderQuery = orderQueryBuilder.ToString().TrimEnd(',', ' ');
                    if (!string.IsNullOrWhiteSpace(orderQuery)) 
                    {
                        source = source.AsQueryable().OrderBy(orderQuery);
                    }
                }
            }

            return source;
        }
        #endregion
    }
}
