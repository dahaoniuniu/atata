﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Atata
{
    public class UIComponentMetadata
    {
        private AttributeSearchSet declaredAttributeSet;

        private AttributeSearchSet parentComponentAttributeSet;

        private AttributeSearchSet assemblyAttributeSet;

        private AttributeSearchSet globalAttributeSet;

        private AttributeSearchSet componentAttributeSet;

        public UIComponentMetadata(
            string name,
            Type componentType,
            Type parentComponentType)
        {
            Name = name;
            ComponentType = componentType;
            ParentComponentType = parentComponentType;
        }

        [Flags]
        private enum AttributeTargetFilterOptions
        {
            None = 0,
            Targeted = 1 << 0,
            NonTargeted = 1 << 1,
            All = Targeted | NonTargeted
        }

        public string Name { get; private set; }

        public Type ComponentType { get; private set; }

        public Type ParentComponentType { get; private set; }

        public UIComponentDefinitionAttribute ComponentDefinitonAttribute { get; internal set; }

        internal List<Attribute> DeclaredAttributesList
        {
            get => declaredAttributeSet.Attributes;
            set => declaredAttributeSet = new AttributeSearchSet(value) { TargetFilterOptions = AttributeTargetFilterOptions.NonTargeted };
        }

        internal List<Attribute> ParentComponentAttributesList
        {
            get => parentComponentAttributeSet.Attributes;
            set => parentComponentAttributeSet = new AttributeSearchSet(value) { TargetFilterOptions = AttributeTargetFilterOptions.Targeted };
        }

        internal List<Attribute> AssemblyAttributesList
        {
            get => assemblyAttributeSet.Attributes;
            set => assemblyAttributeSet = new AttributeSearchSet(value);
        }

        internal List<Attribute> GlobalAttributesList
        {
            get => globalAttributeSet.Attributes;
            set => globalAttributeSet = new AttributeSearchSet(value);
        }

        internal List<Attribute> ComponentAttributesList
        {
            get => componentAttributeSet.Attributes;
            set => componentAttributeSet = new AttributeSearchSet(value) { TargetFilterOptions = AttributeTargetFilterOptions.NonTargeted };
        }

        public IEnumerable<Attribute> DeclaredAttributes => DeclaredAttributesList.AsEnumerable();

        public IEnumerable<Attribute> ComponentAttributes => ComponentAttributesList.AsEnumerable();

        public IEnumerable<Attribute> ParentComponentAttributes => ParentComponentAttributesList.AsEnumerable();

        public IEnumerable<Attribute> AssemblyAttributes => AssemblyAttributesList.AsEnumerable();

        public IEnumerable<Attribute> GlobalAttributes => GlobalAttributesList.AsEnumerable();

        public IEnumerable<Attribute> AllAttributes => DeclaredAttributesList.
            Concat(ParentComponentAttributesList).
            Concat(AssemblyAttributesList).
            Concat(GlobalAttributesList).
            Concat(ComponentAttributesList);

        /// <summary>
        /// Gets the first attribute of the specified type or <c>null</c> if no such attribute is found.
        /// </summary>
        /// <typeparam name="TAttribute">The type of the attribute.</typeparam>
        /// <returns>The first attribute found or <c>null</c>.</returns>
        public TAttribute Get<TAttribute>()
        {
            return Get<TAttribute>(null);
        }

        /// <summary>
        /// Gets the first attribute of the specified type or <c>null</c> if no such attribute is found.
        /// </summary>
        /// <typeparam name="TAttribute">The type of the attribute.</typeparam>
        /// <param name="filterConfiguration">The filter configuration function.</param>
        /// <returns>The first attribute found or <c>null</c>.</returns>
        public TAttribute Get<TAttribute>(Func<AttributeFilter<TAttribute>, AttributeFilter<TAttribute>> filterConfiguration)
        {
            return GetAll(filterConfiguration).FirstOrDefault();
        }

        /// <summary>
        /// Gets the first attribute of the specified type or <c>null</c> if no such attribute is found.
        /// </summary>
        /// <typeparam name="TAttribute">The type of the attribute.</typeparam>
        /// <param name="levels">The attribute levels.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="filterByTarget">If set to <c>true</c>, filters by <see cref="MulticastAttribute"/> criteria if <typeparamref name="TAttribute"/> is <see cref="MulticastAttribute"/>.</param>
        /// <returns>The first attribute found or <c>null</c>.</returns>
        public TAttribute Get<TAttribute>(AttributeLevels levels, Func<TAttribute, bool> predicate = null, bool filterByTarget = true)
        {
            return GetAll(levels, predicate, filterByTarget).FirstOrDefault();
        }

        /// <summary>
        /// Gets the sequence of attributes of the specified type.
        /// </summary>
        /// <typeparam name="TAttribute">The type of the attribute.</typeparam>
        /// <returns>The sequence of attributes found.</returns>
        public IEnumerable<TAttribute> GetAll<TAttribute>()
        {
            return GetAll<TAttribute>(null);
        }

        /// <summary>
        /// Gets the sequence of attributes of the specified type.
        /// </summary>
        /// <typeparam name="TAttribute">The type of the attribute.</typeparam>
        /// <param name="filterConfiguration">The filter configuration function.</param>
        /// <returns>The sequence of attributes found.</returns>
        public IEnumerable<TAttribute> GetAll<TAttribute>(Func<AttributeFilter<TAttribute>, AttributeFilter<TAttribute>> filterConfiguration)
        {
            AttributeFilter<TAttribute> defaultFilter = new AttributeFilter<TAttribute>();

            AttributeFilter<TAttribute> filter = filterConfiguration?.Invoke(defaultFilter) ?? defaultFilter;

            var attributeSets = GetAllAttributeSets(filter.Levels);
            return FilterAttributeSets(attributeSets, filter, true);
        }

        /// <summary>
        /// Gets the sequence of attributes of the specified type.
        /// </summary>
        /// <typeparam name="TAttribute">The type of the attribute.</typeparam>
        /// <param name="levels">The attribute levels.</param>
        /// <param name="predicate">The predicate.</param>
        /// <param name="filterByTarget">If set to <c>true</c>, filters by <see cref="MulticastAttribute"/> criteria if <typeparamref name="TAttribute"/> is <see cref="MulticastAttribute"/>.</param>
        /// <returns>The sequence of attributes found.</returns>
        public IEnumerable<TAttribute> GetAll<TAttribute>(AttributeLevels levels, Func<TAttribute, bool> predicate = null, bool filterByTarget = true)
        {
            AttributeFilter<TAttribute> filter = new AttributeFilter<TAttribute>().At(levels).Where(predicate);

            var attributeSets = GetAllAttributeSets(filter.Levels);
            return FilterAttributeSets(attributeSets, filter, filterByTarget);
        }

        private IEnumerable<AttributeSearchSet> GetAllAttributeSets(AttributeLevels level)
        {
            if (level.HasFlag(AttributeLevels.Declared))
                yield return declaredAttributeSet;

            if (level.HasFlag(AttributeLevels.ParentComponent))
                yield return parentComponentAttributeSet;

            if (level.HasFlag(AttributeLevels.Assembly))
                yield return assemblyAttributeSet;

            if (level.HasFlag(AttributeLevels.Global))
                yield return globalAttributeSet;

            if (level.HasFlag(AttributeLevels.Component))
                yield return componentAttributeSet;
        }

        // TODO: filterByTarget should be removed.
        private IEnumerable<TAttribute> FilterAttributeSets<TAttribute>(IEnumerable<AttributeSearchSet> attributeSets, AttributeFilter<TAttribute> filter, bool filterByTarget)
        {
            bool shouldFilterByTarget = filterByTarget && typeof(MulticastAttribute).IsAssignableFrom(typeof(TAttribute));

            foreach (AttributeSearchSet set in attributeSets)
            {
                var query = set.Attributes.OfType<TAttribute>();

                if (shouldFilterByTarget)
                    query = FilterAndOrderByTarget(query, filter, set.TargetFilterOptions);

                foreach (var predicate in filter.Predicates)
                    query = query.Where(predicate);

                foreach (TAttribute attribute in query)
                    yield return attribute;
            }
        }

        private IEnumerable<TAttribute> FilterAndOrderByTarget<TAttribute>(IEnumerable<TAttribute> attributes, AttributeFilter<TAttribute> filter, AttributeTargetFilterOptions targetFilterOptions)
        {
            if (targetFilterOptions == AttributeTargetFilterOptions.None)
                return Enumerable.Empty<TAttribute>();

            var query = attributes.OfType<MulticastAttribute>();

            if (targetFilterOptions == AttributeTargetFilterOptions.Targeted)
                query = query.Where(x => x.IsTargetSpecified);
            else if (targetFilterOptions == AttributeTargetFilterOptions.NonTargeted)
                query = query.Where(x => !x.IsTargetSpecified);

            var rankedQuery = query.
                Select(x => new { Attribute = x, TargetRank = x.CalculateTargetRank(this) }).
                ToArray().
                Where(x => x.TargetRank.HasValue);

            if (filter.TargetAttributeType != null && typeof(AttributeSettingsAttribute).IsAssignableFrom(typeof(TAttribute)))
            {
                return rankedQuery.
                    Select(x => new { x.Attribute, x.TargetRank, TargetAttributeRank = ((AttributeSettingsAttribute)x.Attribute).CalculateTargetAttributeRank(filter.TargetAttributeType) }).
                    ToArray().
                    Where(x => x.TargetAttributeRank.HasValue).
                    OrderByDescending(x => x.TargetRank.Value).
                    ThenByDescending(x => x.TargetAttributeRank.Value).
                    Select(x => x.Attribute).
                    Cast<TAttribute>();
            }
            else
            {
                return rankedQuery.
                    OrderByDescending(x => x.TargetRank.Value).
                    Select(x => x.Attribute).
                    Cast<TAttribute>();
            }
        }

        /// <summary>
        /// Gets the culture by searching the <see cref="CultureAttribute"/> at all attribute levels or current culture if not found.
        /// </summary>
        /// <returns>The <see cref="CultureInfo"/> instance.</returns>
        public CultureInfo GetCulture()
        {
            string cultureName = Get<CultureAttribute>()?.Value;

            return cultureName != null ? CultureInfo.GetCultureInfo(cultureName) : CultureInfo.CurrentCulture;
        }

        /// <summary>
        /// Gets the format by searching the <see cref="FormatAttribute"/> at all attribute levels or <c>null</c> if not found.
        /// </summary>
        /// <returns>The format or <c>null</c> if not found.</returns>
        public string GetFormat()
        {
            return Get<FormatAttribute>()?.Value;
        }

        private class AttributeSearchSet
        {
            public AttributeSearchSet(List<Attribute> attributes) =>
                Attributes = attributes;

            public List<Attribute> Attributes { get; private set; }

            public AttributeTargetFilterOptions TargetFilterOptions { get; set; } = AttributeTargetFilterOptions.All;
        }
    }
}
