using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Pages.Core;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Extensions;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.Pages.Data.Search
{
    public class PagesSearchRequestBuilder(
        ISearchPhraseParser searchPhraseParser,
        IStoreService storeService) : ISearchRequestBuilder
    {
        public virtual string DocumentType => ModuleConstants.PageDocumentType;

        public virtual async Task<SearchRequest> BuildRequestAsync(SearchCriteriaBase criteria)
        {
            SearchRequest result = null;

            if (criteria is PageDocumentSearchCriteria searchCriteria)
            {
                // GetFilters() modifies Keyword
                searchCriteria = searchCriteria.CloneTyped();
                var filters = await GetFilters(searchCriteria);

                result = new SearchRequest
                {
                    SearchKeywords = searchCriteria.Keyword,
                    SearchFields = [IndexDocumentExtensions.ContentFieldName],
                    Filter = filters.And(),
                    Sorting = GetSorting(searchCriteria),
                    Skip = criteria.Skip,
                    Take = criteria.Take,
                };
            }

            return result;
        }

        protected virtual async Task<IList<IFilter>> GetFilters(PageDocumentSearchCriteria criteria)
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

            if (criteria.Visibility == PageDocumentVisibility.Public)
            {
                result.Add(CreateTermFilter(nameof(PageDocument.Visibility), PageDocumentVisibility.Public.ToString()));
            }

            result.Add(CreateTermFilter(nameof(PageDocument.Status), criteria.Status.ToString()));

            await AddLanguageFilter(criteria, result);
            AddDateFilter(criteria, result);
            AddUserGroups(criteria, result);

            return result;
        }

        private static void AddUserGroups(PageDocumentSearchCriteria criteria, List<IFilter> result)
        {
            var userGroups = criteria.UserGroups ?? [];
            var filter = new TermFilter
            {
                FieldName = nameof(PageDocument.UserGroups),
                Values =
                [
                    "__any",
                    ..userGroups
                ]
            };
            result.Add(filter);
        }

        private static void AddDateFilter(PageDocumentSearchCriteria criteria, List<IFilter> result)
        {
            var date = criteria.CertainDate ?? DateTime.UtcNow;
            var dateFilter = CreateDateFilter(nameof(PageDocument.StartDate), date, true)
                .And(CreateDateFilter(nameof(PageDocument.EndDate), date, false));
            result.Add(dateFilter);
        }

        private async Task AddLanguageFilter(PageDocumentSearchCriteria criteria, List<IFilter> filter)
        {
            IFilter cultureFilter = null;

            if (!criteria.LanguageCode.IsNullOrEmpty())
            {
                cultureFilter = CreateTermFilter(nameof(PageDocument.CultureName), criteria.LanguageCode);
            }
            else
            {
                var store = await storeService.GetByIdAsync(criteria.StoreId);
                var storeLanguage = store?.DefaultLanguage;
                if (!storeLanguage.IsNullOrEmpty())
                {
                    cultureFilter = CreateTermFilter(nameof(PageDocument.CultureName), storeLanguage);
                }
            }

            if (cultureFilter != null)
            {
                filter.Add(cultureFilter);
            }
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

        protected static IFilter CreateDateFilter(string fieldName, DateTime date, bool isLess)
        {
            return new RangeFilter
            {
                FieldName = fieldName,
                Values =
                [
                    new RangeFilterValue
                    {
                        IncludeLower = true,
                        IncludeUpper = true,
                        Lower = isLess ? null : date.ToString("O"),
                        Upper = isLess ? date.ToString("O") : null
                    }
                ]
            };
        }
    }

}
