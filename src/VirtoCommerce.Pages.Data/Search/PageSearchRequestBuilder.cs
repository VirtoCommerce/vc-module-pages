using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Pages.Core;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Data.Converters;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Extensions;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.StoreModule.Core.Services;

namespace VirtoCommerce.Pages.Data.Search
{
    public class PageSearchRequestBuilder(
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

                result = AbstractTypeFactory<SearchRequest>.TryCreateInstance();
                result.SearchKeywords = searchCriteria.Keyword;
                result.SearchFields = [IndexDocumentExtensions.ContentFieldName];
                result.Filter = filters.And();
                result.Sorting = GetSorting(searchCriteria);
                result.Skip = criteria.Skip;
                result.Take = criteria.Take;
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

            if (criteria.Status != null)
            {
                result.Add(CreateTermFilter(nameof(PageDocument.Status), criteria.Status.ToString()));
            }

            await AddLanguageFilter(criteria, result);
            AddDateFilter(criteria, result);
            AddUserGroups(criteria, result);
            AddOrganizationFilter(criteria, result);

            return result;
        }

        private static void AddUserGroups(PageDocumentSearchCriteria criteria, List<IFilter> result)
        {
            if (criteria.UserGroups == null)
            {
                return;
            }
            var userGroups = criteria.UserGroups;
            var filter = new TermFilter
            {
                FieldName = nameof(PageDocument.UserGroups),
                Values =
                [
                    PageDocumentConverter.Any,
                    ..userGroups
                ]
            };
            result.Add(filter);
        }

        private static void AddDateFilter(PageDocumentSearchCriteria criteria, List<IFilter> result)
        {
            if (criteria.CertainDate.HasValue)
            {
                var date = criteria.CertainDate.Value;
                var dateFilter = CreateDateFilter(nameof(PageDocument.StartDate), date, true)
                    .And(CreateDateFilter(nameof(PageDocument.EndDate), date, false));
                result.Add(dateFilter);
            }
        }

        private void AddOrganizationFilter(PageDocumentSearchCriteria criteria, List<IFilter> result)
        {
            if (criteria.OrganizationId.IsNullOrEmpty())
            {
                return;
            }
            var anyFilter = new TermFilter
            {
                FieldName = nameof(PageDocument.OrganizationId),
                Values = [PageDocumentConverter.Any, criteria.OrganizationId],
            };
            result.Add(anyFilter);
        }

        private async Task AddLanguageFilter(PageDocumentSearchCriteria criteria, List<IFilter> filter)
        {
            if (criteria.LanguageCode.IsNullOrEmpty())
            {
                return;
            }
            var store = await storeService.GetByIdAsync(criteria.StoreId);
            var storeLanguage = store?.DefaultLanguage;

            var languages = new[]
            {
                criteria.LanguageCode,
                storeLanguage.EmptyToNull(),
            }
            .Where(x => x != null)
            .Distinct()
            .ToArray();

            if (languages.Length > 0)
            {
                var termFilter = new TermFilter
                {
                    FieldName = nameof(PageDocument.CultureName),
                    Values =
                    [
                        PageDocumentConverter.Any,
                        ..languages
                    ],
                };
                filter.Add(termFilter);
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
                Values = [value],
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
