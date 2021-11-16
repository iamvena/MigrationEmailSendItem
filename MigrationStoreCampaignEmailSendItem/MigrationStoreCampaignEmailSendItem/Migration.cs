using MacHaik.Campaigns.DataStore.Models;
using MacHaik.Campaigns.DataStore.Services;
using MacHaik.Data;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationStoreCampaignEmailSendItem
{
    public class Migration
    {
        public Migration()
        {

        }


        public void UpdateStore()
        {
            Console.WriteLine("Retrieving active stores...");

            var stores = GetAllActiveStores();

            foreach (var store in stores)
            {
                if (store.StoreId != 60) continue;

                var lastCampaignId = 0;

                var hasEmailCampaignHeaders = true;

                var totalEmailCampaignHeaders = 0;

                Console.WriteLine($"Retrieve campaign headers by store... " +
                    $"StoreId - {store.StoreId} , " +
                    $"StoreName - {store.StoreName}");

                var headerIds = new List<int>();

                while (hasEmailCampaignHeaders)
                {
                    var emailCampaignHeaders = GetActiveCampaignByStore(store.StoreId, lastCampaignId);

                    if (!emailCampaignHeaders.Any()) break;

                    totalEmailCampaignHeaders += emailCampaignHeaders.Count();

                    lastCampaignId = emailCampaignHeaders.OrderByDescending(d => d).FirstOrDefault();

                    UpdateCampaignSendItemStore(emailCampaignHeaders, store.StoreId);

                    using (var context = new MacHaikCrmEntities())
                    {
                        hasEmailCampaignHeaders = context.Email_Campaign_Header
                             .OrderBy(r => r.Id)
                             .Any(r => r.Id > lastCampaignId);
                    }

                    Console.WriteLine($"" +
                        $"Total updated records : {totalEmailCampaignHeaders} , " +
                        $"StoreId : {store.StoreId} ");
                }
            }
        }

        public void UpdateCampaignSendItemStore(List<int> emailCampaignHeaderIds, int storeId)
        {
            foreach (var headerId in emailCampaignHeaderIds)
            {
                var hasEmailCampaignSendItems = true;

                var totalEmailSendItemByHeaderId = 0;

                while (hasEmailCampaignSendItems)
                {
                    var emailCampaignSendItems = Get100MongoDbEmailCampaignSendItem(headerId);

                    if (!emailCampaignSendItems.Any()) break;

                    totalEmailSendItemByHeaderId += emailCampaignSendItems.Count();

                    foreach (var item in emailCampaignSendItems)
                    {
                        var filter = Builders<EmailCampaignSendItem>.Filter.Eq(d => d.Uid, item.Uid);

                        var update = Builders<EmailCampaignSendItem>.Update.Set(d => d.StoreId, storeId);

                        DefaultConnectionProvider.GetInstance()
                           .GetCollection<EmailCampaignSendItem>(nameof(EmailCampaignSendItem))
                           .UpdateOne(filter, update);

                        Console.WriteLine($"EmailCampaignSendItem Updated . " +
                            $"Uid - {item.Uid}");
                    }

                    hasEmailCampaignSendItems = Get100MongoDbEmailCampaignSendItem(headerId).Any();
                }

                Console.WriteLine($"Total updated record in email campaign send item by headerId" +
                    $"Total : {totalEmailSendItemByHeaderId} ," +
                    $"CampaignHeaderId - {headerId}");
            }
        }

        public List<EmailCampaignSendItem> Get100MongoDbEmailCampaignSendItem(int campaignId)
        {
            var sendItems = new List<EmailCampaignSendItem>();

            sendItems = DefaultConnectionProvider.GetInstance()
                    .GetCollection<EmailCampaignSendItem>(nameof(EmailCampaignSendItem))
                    .Find(c => c.CampaignId == campaignId &&
                          !c.StoreId.HasValue)
                    .Limit(100)
                    .ToList();

            return sendItems;

        }

        public List<EmailCampaignSendItem> GetMongoDbSendEmailItemCampaign(int campaignId)
        {
            var collection = DefaultConnectionProvider.GetInstance()
                    .GetCollection<EmailCampaignSendItem>(nameof(EmailCampaignSendItem));

            int rows = 100;

            return collection.Find(c => c.CampaignId == campaignId)
                .Limit(rows)
                .ToList();
        }

        public List<int> GetActiveCampaignByStore(int storeId, int lastCampaignId)
        {
            var campaignHeaders = new List<int>();

            using (var context = new MacHaikCrmEntities())
            {
                if (lastCampaignId == 0)
                {
                    campaignHeaders = context.Email_Campaign_Header
                        .Where(c => c.StoreId == storeId &&
                               c.IsActive == true)
                        .OrderBy(r => r.Id)
                        .Take(100)
                        .Select(c => c.Id)
                        .ToList();
                }
                else
                {
                    campaignHeaders = context.Email_Campaign_Header
                        .Where(c => c.StoreId == storeId &&
                               c.IsActive == true)
                        .OrderBy(r => r.Id)
                        .Where(r => r.Id > lastCampaignId)
                        .Take(100)
                        .Select(r => r.Id)
                        .ToList();
                }
            }

            return campaignHeaders;
        }

        public List<StoreModel> GetAllActiveStores()
        {
            using (var context = new MacHaikCrmEntities())
            {
                var stores = context.StorePreferences
                        .Where(c => c.Name == "Store.Settings.Active" &&
                               c.Value == "1")
                        .Select(c => new StoreModel
                        {
                            StoreId = c.StoreId.Value,
                            StoreName = c.Store.Name
                        }).ToList();

                return stores;
            }
        }

        public class StoreModel
        {
            public int StoreId { set; get; }
            public string StoreName { set; get; }
        }

        public class EmailCampaignHeaderModel
        {
            public int CampaignHeaderId { set; get; }
        }
    }
}
