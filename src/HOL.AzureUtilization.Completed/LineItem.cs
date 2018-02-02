// -----------------------------------------------------------------------
// <copyright file="LineItem..cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace HOL.AzureUtilization.Completed
{
    using System;

    public class LineItem
    {
        /// <summary>
        /// Gets or sets the category of the consumed Azure resource.
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the Azure resource that was consumed. 
        /// Also, known as the resourceID or resourceGUID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the friendly name of the Azure resource being consumed.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Computed price of the Azure resource being consumed.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the quantity consumed of the Azure resource.
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// Gets or sets the region of the consumed Azure resource.
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets the fully qualified Azure resource ID, which includes the resource
        /// groups and the instance name.
        /// </summary>
        public Uri ResourceUri { get; set; }

        /// <summary>
        /// Gets or sets the sub-category of the consumed Azure resource.
        /// </summary>
        public string Subcategory { get; set; }

        /// <summary>
        /// Gets or sets the end of the usage aggregation time range. The response is grouped
        /// by the time of consumption (when the resource was actually used VS. when was
        /// it reported to the billing system).
        /// </summary>
        public DateTimeOffset UsageEndTime { get; set; }

        /// <summary>
        /// Gets or sets the start of the usage aggregation time range. The response is grouped
        /// by the time of consumption (when the resource was actually used VS. when was
        /// it reported to the billing system).
        /// </summary>
        public DateTimeOffset UsageStartTime { get; set; }
    }
}