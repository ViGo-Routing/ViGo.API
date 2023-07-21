using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ViGo.Models.QueryString.Pagination;

namespace ViGo.Models.QueryString
{
    public abstract class QueryStringParameters
    {
        //public PaginationParameter Pagination { get; set; } = PaginationParameter.Default;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string OrderBy { get; set; }
    }

    public static class QueryStringUtilities
    {
        public static string ToSortingCriteria
            (params SortingCriteria[] criterias)
        {
            IEnumerable<string> strCriterias = new List<string>();

            foreach (SortingCriteria criteria in criterias)
            {
                strCriterias = strCriterias.Append(criteria.PropertyName + " " +
                    criteria.SortingType.ToString().ToLower());
            }

            return string.Join(",", strCriterias);
        }
    }

    public class SortingCriteria
    {
        public string PropertyName { get; set; }
        public SortingType SortingType { get; set; }

        public SortingCriteria(string propertyName)
        {
            PropertyName = propertyName;
            SortingType = SortingType.ASC;
        }

        public SortingCriteria(string propertyName, SortingType sortingType)
        {
            PropertyName = propertyName;
            SortingType = sortingType;
        }
    }

    public enum SortingType
    {
        ASC = 1,
        DESC = 2
    }
}
