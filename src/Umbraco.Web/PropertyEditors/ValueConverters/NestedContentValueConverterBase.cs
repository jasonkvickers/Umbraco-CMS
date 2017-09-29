﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Umbraco.Core;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;
using Umbraco.Web.PublishedCache;

namespace Umbraco.Web.PropertyEditors.ValueConverters
{
    public abstract class NestedContentValueConverterBase : PropertyValueConverterBase
    {
        private readonly IFacadeAccessor _facadeAccessor;

        protected NestedContentValueConverterBase(IFacadeAccessor facadeAccessor, IPublishedModelFactory publishedModelFactory)
        {
            _facadeAccessor = facadeAccessor;
            PublishedModelFactory = publishedModelFactory;
        }

        protected IPublishedModelFactory PublishedModelFactory { get; }

        public static bool IsNested(PublishedPropertyType publishedProperty)
        {
            return publishedProperty.PropertyEditorAlias.InvariantEquals(Constants.PropertyEditors.NestedContentAlias);
        }

        public static bool IsNestedSingle(PublishedPropertyType publishedProperty)
        {
            if (!IsNested(publishedProperty))
                return false;

            // fixme - the facade should provide this
            var preValueCollection = NestedContentHelper.GetPreValuesCollectionByDataTypeId(publishedProperty.DataTypeId);
            var preValueDictionary = preValueCollection.PreValuesAsDictionary;

            return preValueDictionary.TryGetValue("minItems", out var minItems)
                   && preValueDictionary.TryGetValue("maxItems", out var maxItems)
                   && int.TryParse(minItems.Value, out var minItemsValue) && minItemsValue == 1
                   && int.TryParse(maxItems.Value, out var maxItemsValue) && maxItemsValue == 1;
        }

        public static bool IsNestedMany(PublishedPropertyType publishedProperty)
        {
            return IsNested(publishedProperty) && !IsNestedSingle(publishedProperty);
        }

        protected IPublishedElement ConvertToElement(JObject sourceObject, PropertyCacheLevel referenceCacheLevel, bool preview)
        {
            var elementTypeAlias = sourceObject[NestedContentPropertyEditor.ContentTypeAliasPropertyKey]?.ToObject<string>();
            if (string.IsNullOrEmpty(elementTypeAlias))
                return null;

            var publishedContentType = _facadeAccessor.Facade.ContentCache.GetContentType(elementTypeAlias);
            if (publishedContentType == null)
                return null;

            var propertyValues = sourceObject.ToObject<Dictionary<string, object>>();

            if (!propertyValues.TryGetValue("key", out var keyo)
                || !Guid.TryParse(keyo.ToString(), out var key))
                key = Guid.Empty;

            IPublishedElement element = new PublishedElement(publishedContentType, key, propertyValues, preview, referenceCacheLevel, _facadeAccessor);
            element = PublishedModelFactory.CreateModel(element);
            return element;
        }
    }
}