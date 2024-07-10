using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Pages.Core;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Extensions;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace VirtoCommerce.Pages.Data.Search
{
    public class PagesSearchRequestBuilder(ISearchPhraseParser searchPhraseParser) : ISearchRequestBuilder
    {
        public virtual string DocumentType => ModuleConstants.PageDocumentType;

        public virtual Task<SearchRequest> BuildRequestAsync(SearchCriteriaBase criteria)
        {
            SearchRequest result = null;

            if (criteria is PageDocumentSearchCriteria searchCriteria)
            {
                // GetFilters() modifies Keyword
                searchCriteria = searchCriteria.CloneTyped();
                var filters = GetFilters(searchCriteria);

                result = new SearchRequest
                {
                    SearchKeywords = searchCriteria.Keyword,
                    SearchFields = new[] { IndexDocumentExtensions.ContentFieldName },
                    Filter = filters.And(),
                    Sorting = GetSorting(searchCriteria),
                    Skip = criteria.Skip,
                    Take = criteria.Take,
                };
            }

            return Task.FromResult(result);
        }

        protected virtual IList<IFilter> GetFilters(PageDocumentSearchCriteria criteria)
        {
            var result = new List<IFilter>();

            if (!string.IsNullOrEmpty(criteria.Keyword))
            {
                var parseResult = searchPhraseParser.Parse(criteria.Keyword);
                criteria.Keyword = parseResult.Keyword;
                result.AddRange(parseResult.Filters);
            }

            if (criteria.ObjectIds?.Any() == true)
            {
                result.Add(new IdsFilter { Values = criteria.ObjectIds });
            }

            if (!string.IsNullOrEmpty(criteria.StoreId))
            {
                result.Add(CreateTermFilter(nameof(PageDocument.StoreId), criteria.StoreId));
            }

            if (!string.IsNullOrEmpty(criteria.Permalink))
            {
                result.Add(CreateTermFilter(nameof(PageDocument.Permalink), criteria.Permalink));
            }

            if (criteria.Visibility.HasValue)
            {
                result.Add(CreateTermFilter(nameof(PageDocument.Visibility), criteria.Visibility.Value.ToString()));
            }

            if (criteria.Status.HasValue)
            {
                result.Add(CreateTermFilter(nameof(PageDocument.Status), criteria.Status.Value.ToString()));
            }

            result.Add(AddLanguageFilter(criteria));

            //if (!string.IsNullOrEmpty(criteria.FolderUrl))
            //{
            //    result.Add(CreateTermFilter("FolderUrl", criteria.FolderUrl));
            //}
            //if (!string.IsNullOrEmpty(criteria.ContentType))
            //{
            //    result.Add(CreateTermFilter("ContentType", criteria.ContentType));
            //}
            return result;
        }

        private IFilter AddLanguageFilter(PageDocumentSearchCriteria criteria)
        {
            var cultureFilter = CreateTermFilter("CultureName", "__any");

            if (!criteria.LanguageCode.IsNullOrEmpty())
            {
                cultureFilter = cultureFilter.Or(CreateTermFilter("CultureName", criteria.LanguageCode));
            }

            return cultureFilter;
        }

        protected virtual IList<SortingField> GetSorting(PageDocumentSearchCriteria criteria)
        {
            var result = new List<SortingField>();

            foreach (var sortInfo in criteria.SortInfos)
            {
                var fieldName = sortInfo.SortColumn.ToLowerInvariant();
                var isDescending = sortInfo.SortDirection == SortDirection.Descending;
                result.Add(new SortingField(fieldName, isDescending));
            }

            return result;
        }

        protected static IFilter CreateTermFilter(string fieldName, string value)
        {
            return new TermFilter
            {
                FieldName = fieldName,
                Values = new[] { value },
            };
        }
    }

}
