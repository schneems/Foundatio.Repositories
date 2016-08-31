﻿using System;
using ElasticMacros;
using Nest;

namespace Foundatio.Repositories.Elasticsearch.Queries.Builders {
    public interface ISearchQuery {
        string Filter { get; set; }
        string SearchQuery { get; set; }
        SearchOperator DefaultSearchQueryOperator { get; set; }
    }

    public enum SearchOperator {
        And,
        Or
    }

    public class SearchQueryBuilder : IElasticQueryBuilder {
        public void Build<T>(QueryBuilderContext<T> ctx) where T : class, new() {
            var searchQuery = ctx.GetSourceAs<ISearchQuery>();
            if (searchQuery == null)
                return;

            if (!String.IsNullOrEmpty(searchQuery.Filter))
                ctx.Filter &= new QueryFilter {
                    Query = new QueryStringQuery {
                        Query = searchQuery.Filter,
                        DefaultOperator = Operator.And,
                        AnalyzeWildcard = false
                    }.ToContainer()
                };

            if (!String.IsNullOrEmpty(searchQuery.SearchQuery))
                ctx.Query &= new QueryStringQuery {
                    Query = searchQuery.SearchQuery,
                    DefaultOperator = searchQuery.DefaultSearchQueryOperator == SearchOperator.Or ? Operator.Or : Operator.And,
                    AnalyzeWildcard = true
                };
        }
    }

    public class ElasticMacroSearchQueryBuilder : IElasticQueryBuilder {
        private readonly ElasticMacroProcessor _processor;

        public ElasticMacroSearchQueryBuilder(ElasticMacroProcessor processor = null) {
            _processor = processor ?? new ElasticMacroProcessor();
        }

        public void Build<T>(QueryBuilderContext<T> ctx) where T : class, new() {
            var searchQuery = ctx.GetSourceAs<ISearchQuery>();
            if (searchQuery == null)
                return;

            // TODO: Use default search operator and wildcards
            if (!String.IsNullOrEmpty(searchQuery.SearchQuery))
                ctx.Query &= _processor.BuildQuery(searchQuery.SearchQuery);

            if (!String.IsNullOrEmpty(searchQuery.Filter))
                ctx.Filter &= _processor.BuildFilter(searchQuery.Filter);
        }
    }

    public static class SearchQueryExtensions {
        public static T WithFilter<T>(this T query, string filter) where T : ISearchQuery {
            query.Filter = filter;
            return query;
        }

        public static T WithSearchQuery<T>(this T query, string queryString, bool useAndAsDefaultOperator = true) where T : ISearchQuery {
            query.SearchQuery = queryString;
            query.DefaultSearchQueryOperator = useAndAsDefaultOperator ? SearchOperator.And : SearchOperator.Or;
            return query;
        }
    }
}