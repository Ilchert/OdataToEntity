﻿using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using Microsoft.OData.UriParser.Aggregation;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace OdataToEntity.Parsers
{
    public sealed class OeExpressionBuilder
    {
        private Type _entityType;
        private readonly IEdmModel _model;
        private Func<Type, IEdmEntitySet, Type, OeEntryFactory> _entryFactory;
        private OeQueryNodeVisitor _visitor;

        public OeExpressionBuilder(IEdmModel model, Type entityType)
        {
            _model = model;
            _entityType = entityType;

            _visitor = new OeQueryNodeVisitor(_model, Expression.Parameter(_entityType));
        }

        public Expression ApplyAggregation(Expression source, ApplyClause applyClause)
        {
            if (applyClause == null)
                return source;

            var aggTranslator = new OeAggregationTranslator(_visitor);
            Expression aggExpression = aggTranslator.Build(source, applyClause);

            _entryFactory = aggTranslator.CreateEntryFactory;

            Type aggItemType = OeExpressionHelper.GetCollectionItemType(aggExpression.Type);
            _visitor = new OeQueryNodeVisitor(_model, Expression.Parameter(aggItemType), _visitor.Constans)
            {
                TuplePropertyByAliasName = aggTranslator.GetTuplePropertyByAliasName
            };

            return aggExpression;
        }
        public Expression ApplyCount(Expression source, bool? queryCount)
        {
            if (!queryCount.GetValueOrDefault())
                return source;

            MethodInfo countMethodInfo = OeMethodInfoHelper.GetCountMethodInfo(ParameterType);
            return Expression.Call(countMethodInfo, source);
        }
        public Expression ApplyFilter(Expression source, FilterClause filterClause)
        {
            if (filterClause == null)
                return source;

            Expression e = _visitor.TranslateNode(filterClause.Expression);
            LambdaExpression lambda = Expression.Lambda(e, Parameter);

            MethodInfo whereMethodInfo = OeMethodInfoHelper.GetWhereMethodInfo(ParameterType);
            return Expression.Call(whereMethodInfo, source, lambda);
        }
        public Expression ApplyNavigation(Expression source, IEnumerable<OeParseNavigationSegment> parseNavigationSegments)
        {
            if (parseNavigationSegments == null)
                return source;

            Type sourceItemType = OeExpressionHelper.GetCollectionItemType(source.Type);
            foreach (OeParseNavigationSegment parseNavigationSegment in parseNavigationSegments)
            {
                Type selectType;
                ParameterExpression parameter;
                Expression e;
                if (parseNavigationSegment.NavigationSegment == null) //EntitySetSegment
                {
                    parameter = Parameter;
                    e = source;
                    selectType = sourceItemType;
                }
                else
                {
                    parameter = Expression.Parameter(sourceItemType);
                    PropertyInfo navigationClrProperty = sourceItemType.GetProperty(parseNavigationSegment.NavigationSegment.NavigationProperty.Name);
                    e = Expression.MakeMemberAccess(parameter, navigationClrProperty);

                    MethodInfo selectMethodInfo;
                    selectType = OeExpressionHelper.GetCollectionItemType(e.Type);
                    if (selectType == null)
                    {
                        selectType = e.Type;
                        selectMethodInfo = OeMethodInfoHelper.GetSelectMethodInfo(sourceItemType, selectType);
                    }
                    else
                        selectMethodInfo = OeMethodInfoHelper.GetSelectManyMethodInfo(sourceItemType, selectType);

                    LambdaExpression lambda = Expression.Lambda(e, parameter);
                    source = Expression.Call(selectMethodInfo, source, lambda);
                }

                if (parseNavigationSegment.Filter != null)
                {
                    var visitor = new OeQueryNodeVisitor(_visitor, Expression.Parameter(selectType));
                    e = visitor.TranslateNode(parseNavigationSegment.Filter.Expression);
                    LambdaExpression lambda = Expression.Lambda(e, visitor.Parameter);

                    MethodInfo whereMethodInfo = OeMethodInfoHelper.GetWhereMethodInfo(selectType);
                    source = Expression.Call(whereMethodInfo, source, lambda);
                }

                _entityType = selectType;
                sourceItemType = selectType;
            }

            _visitor = new OeQueryNodeVisitor(_model, Expression.Parameter(_entityType), _visitor.Constans);
            return source;
        }
        public Expression ApplyOrderBy(Expression source, OrderByClause orderByClause)
        {
            if (orderByClause == null)
                return source;

            var orderBytranslator = new OeOrderByTranslator(_visitor);
            return orderBytranslator.Build(source, orderByClause);
        }
        public Expression ApplySelect(Expression source, OeQueryContext queryContext)
        {
            if (queryContext.ODataUri.SelectAndExpand == null && (queryContext.ODataUri.OrderBy == null || queryContext.PageSize == 0))
                return source;

            var selectTranslator = new OeSelectTranslator(_visitor, queryContext.ODataUri.Path);
            Expression selectExpression = selectTranslator.Build(source, queryContext);
            if (selectExpression == null)
                return source;

            _entryFactory = selectTranslator.CreateEntryFactory;

            Type selectItemType = OeExpressionHelper.GetCollectionItemType(selectExpression.Type);
            _visitor = new OeQueryNodeVisitor(_model, Expression.Parameter(selectItemType), _visitor.Constans);

            return selectExpression;
        }
        public Expression ApplySkip(Expression source, long? skip, ODataPath path)
        {
            if (skip == null)
                return source;

            ConstantExpression skipConstant = Expression.Constant((int)skip.Value, typeof(int));
            _visitor.AddSkipConstant(skipConstant, path);

            MethodInfo skipMethodInfo = OeMethodInfoHelper.GetSkipMethodInfo(ParameterType);
            return Expression.Call(skipMethodInfo, source, skipConstant);
        }
        public Expression ApplySkipToken(Expression source, OeSkipTokenNameValue[] skipTokenNameValues, OrderByClause uniqueOrderBy, bool isDatabaseNullHighestValue)
        {
            if (skipTokenNameValues == null || skipTokenNameValues.Length == 0)
                return source;

            var skipTokenTranslator = new OeSkipTokenTranslator(_visitor, isDatabaseNullHighestValue);
            return skipTokenTranslator.Build(source, skipTokenNameValues, uniqueOrderBy);
        }
        public Expression ApplyTake(Expression source, long? top, ODataPath path)
        {
            if (top == null)
                return source;

            ConstantExpression topConstant = Expression.Constant((int)top.Value, typeof(int));
            _visitor.AddTopConstant(topConstant, path);

            MethodInfo takeMethodInfo = OeMethodInfoHelper.GetTakeMethodInfo(ParameterType);
            return Expression.Call(takeMethodInfo, source, topConstant);
        }
        public OeEntryFactory CreateEntryFactory(IEdmEntitySet entitySet)
        {
            if (_entryFactory != null)
                return _entryFactory(EntityType, entitySet, ParameterType);

            OePropertyAccessor[] accessors = OePropertyAccessor.CreateFromType(ParameterType, entitySet);
            return OeEntryFactory.CreateEntryFactory(entitySet, accessors);
        }

        public IReadOnlyDictionary<ConstantExpression, ConstantNode> Constants => _visitor.Constans;
        public Type EntityType => _entityType;
        private ParameterExpression Parameter => _visitor.Parameter;
        private Type ParameterType => _visitor.Parameter.Type;
    }
}
