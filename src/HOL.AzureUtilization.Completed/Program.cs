// -----------------------------------------------------------------------
// <copyright file="Program..cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace HOL.AzureUtilization.Completed
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using Microsoft.Store.PartnerCenter;
    using Microsoft.Store.PartnerCenter.Enumerators;
    using Microsoft.Store.PartnerCenter.Extensions;
    using Microsoft.Store.PartnerCenter.Models;
    using Microsoft.Store.PartnerCenter.Models.RateCards;
    using Microsoft.Store.PartnerCenter.Models.Utilizations;
    using Microsoft.Store.PartnerCenter.RequestContext;

    internal class Program
    {
        /// <summary>
        /// Name of the application communicating with Partner Center.
        /// </summary>
        private const string ApplicationName = "Partner-Center-Labs Azure Utilization HOL";

        /// <summary>
        /// Entry point for the application.
        /// </summary>
        /// <param name="args">Argument specified when starting the application</param>
        internal static void Main(string[] args)
        {
            AzureRateCard rateCard;
            Guid correlationId;
            List<AzureUtilizationRecord> records;
            string customerId;
            string subscriptionId;

            try
            {
                correlationId = Guid.NewGuid();

                // Obtain the rate card details. 
                rateCard = GetAzureRateCard(correlationId);

                // Prompt for the customer identifier. This is the identifier for the customer that owns the Azure subscription. 
                customerId = ConsoleHelper.Instance.ObtainCustomerId();
                // Prompt for the subscription identifier. This should be an identifier for an Azure subscription. 
                subscriptionId = ConsoleHelper.Instance.ObtainSubscriptionId();

                // Obtain usage records for the specified subscription.
                records = GetAzureUtilization(customerId, subscriptionId, correlationId);

                // Combine the rate card and utilization detials, and then print the result to the console.
                PrintLineItems(rateCard, records);

                ConsoleHelper.Instance.Pause("Press enter to exit...");
            }
            finally
            {
                rateCard = null;
                records = null;
            }
        }

        /// <summary>
        /// Gets the Azure rate card details.
        /// </summary>
        /// <param name="correlationId">Correlation identifier used when communicating with Partner Center</param>
        /// <returns>
        /// Rate card details that provide real time pricing for Azure resources.
        /// </returns>
        private static AzureRateCard GetAzureRateCard(Guid correlationId = default(Guid))
        {
            IPartner partner;

            try
            {
                if (correlationId == default(Guid))
                {
                    correlationId = Guid.NewGuid();
                }

                partner = GetPartnerService(correlationId);

                return partner.RateCards.Azure.Get();
            }
            finally
            {
                partner = null;
            }
        }

        /// <summary>
        /// Gets Azure utilization records for the speciied subscription. 
        /// </summary>
        /// <param name="customerId">Identifier of the customer</param>
        /// <param name="subscriptionId">Identifier of the subscription</param>
        /// <param name="correlationId">Correlation identifier used when communicating with Partner Center</param>
        /// <returns>
        /// A list of Azure utilization records for the specified subscription.
        /// </returns>
        /// <remarks>
        /// The query invoked by this function will return all available Azure utilization 
        /// records for the past seven days. Also, it will limit each request to Partner Center 
        /// to only return ten records for each call. 
        /// </remarks>
        private static List<AzureUtilizationRecord> GetAzureUtilization(string customerId, string subscriptionId, Guid correlationId = default(Guid))
        {
            IPartner partner;
            IResourceCollectionEnumerator<ResourceCollection<AzureUtilizationRecord>> usageEnumerator;
            List<AzureUtilizationRecord> usageRecords;
            ResourceCollection<AzureUtilizationRecord> records;

            try
            {
                ConsoleHelper.Instance.StartProgress("Querying Azure utilization records");

                if (correlationId == default(Guid))
                {
                    correlationId = Guid.NewGuid();
                }

                partner = GetPartnerService(correlationId);

                records = partner.Customers.ById(customerId).Subscriptions.ById(subscriptionId).Utilization.Azure.Query(
                        DateTimeOffset.Now.AddDays(-7), DateTimeOffset.Now, AzureUtilizationGranularity.Daily, true, 10);

                usageEnumerator = partner.Enumerators.Utilization.Azure.Create(records);
                usageRecords = new List<AzureUtilizationRecord>();

                while (usageEnumerator.HasValue)
                {
                    usageRecords.AddRange(usageEnumerator.Current.Items);
                    usageEnumerator.Next();
                }

                ConsoleHelper.Instance.StopProgress();

                return usageRecords;
            }
            finally
            {
                partner = null;
                records = null;
                usageEnumerator = null;
            }
        }

        /// <summary>
        /// Gets an aptly configured instance of the partner service. 
        /// </summary>
        /// <param name="correlationId">Correlation identifier used when communicating with Partner Center</param>
        /// <returns>An aptly populated instance of the partner service.</returns>
        /// <remarks>
        /// This function will request the necessary access token to communicate with Partner Center and initialize 
        /// an instance of the partner service. The application name and correlation identifier are optional values, however, 
        /// they have been included here because it is considered best practice. Including the application name makes it where
        /// Microsoft can quickly identify what application is communicating with Partner Center. Specifying the correlation 
        /// identifier should be done to easily correlate a series of calls to Partner Center. Both of these properties will 
        /// help Microsoft with identifying issues and supporting you. 
        /// </remarks>
        private static IPartner GetPartnerService(Guid correlationId)
        {
            IPartnerCredentials credentials = PartnerCredentials.Instance.GenerateByApplicationCredentials(
                ConfigurationManager.AppSettings["PartnerCenter.ApplicationId"],
                ConfigurationManager.AppSettings["PartnerCenter.ApplicationSecret"],
                ConfigurationManager.AppSettings["PartnerCenter.AccountId"]);

            IAggregatePartner partner = PartnerService.Instance.CreatePartnerOperations(credentials);

            PartnerService.Instance.ApplicationName = ApplicationName;

            return partner.With(RequestContextFactory.Instance.Create(correlationId));
        }

        /// <summary>
        /// Prints line items that represents the combination of rate card and utilization records.
        /// </summary>
        /// <param name="rateCard">Rate card details for Azure resources.</param>
        /// <param name="records">A list of Azure utilization records.</param>
        /// <remarks>
        /// The calculations performed by this method do not included quantities and do not take tiered pricing into 
        /// consideration. Both were omittied for the sake of simiplicity. If you want to ensure the calculations 
        /// match the reconciliation file then you will need to the appropriate logic. Please note that the included 
        /// quantity value specifies the included quantity for the billing cycle. The billing cycle is when Microsoft 
        /// sends the partner an invoice for all services consumed through the Cloud Solution Provider program.
        /// </remarks>
        private static void PrintLineItems(AzureRateCard rateCard, List<AzureUtilizationRecord> records)
        {
            List<LineItem> items;

            try
            {
                ConsoleHelper.Instance.StartProgress("Combining utilization records with rate card details");
                items = new List<LineItem>();

                items = records.Select(r => new LineItem
                {
                    Category = r.Resource.Category,
                    Id = r.Resource.Id,
                    Name = r.Resource.Name,
                    Price = rateCard.Meters.Single(m => m.Id == r.Resource.Id).Rates[0] * r.Quantity,
                    Quantity = r.Quantity,
                    Region = r.Resource.Region,
                    ResourceUri = r.InstanceData.ResourceUri,
                    Subcategory = r.Resource.Category,
                    UsageEndTime = r.UsageEndTime,
                    UsageStartTime = r.UsageStartTime
                }).ToList();

                ConsoleHelper.Instance.StopProgress();
                ConsoleHelper.Instance.WriteObject(items);
            }
            finally
            {
                items = null;
            }
        }
    }
}