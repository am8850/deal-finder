using DF.Services.State.Models;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DF.Services.State
{
    public class StateService
    {
        private const string PartitionKey = "DealState";
        private const string TableName = "DealsStateInfo";

        public string ConnectionString { get; set; }
        private CloudTable Table { get; set; }
        private CloudStorageAccount StorageAccount { get; set; }

        private StateService(string connStr)
        {
            ConnectionString = connStr;
        }

        public static async Task<StateService> CreateAsync(string connStr)
        {
            var service = new StateService(connStr);
            await service.CreateTableAsync(connStr, TableName);
            return service;
        }

        private async Task CreateTableAsync(string connStr, string tableName)
        {
            try
            {
                StorageAccount = CloudStorageAccount.Parse(connStr);

                // Create a table client for interacting with the table service
                CloudTableClient tableClient = StorageAccount.CreateCloudTableClient(new TableClientConfiguration());

                Debug.WriteLine("Create a Table for the demo");

                // Create a table client for interacting with the table service 
                Table = tableClient.GetTableReference(tableName);

                if (await Table.CreateIfNotExistsAsync())
                {
                    Debug.WriteLine("Created Table named: {0}", tableName);
                }
                else
                {
                    Debug.WriteLine("Table {0} already exists", tableName);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Unable to create connection or table: {e.Message}");
                throw;
            }
        }

        public async Task<bool> FindAsync(string rowKey)
        {
            try
            {
                TableOperation retrieveOperation = TableOperation.Retrieve<StateInfo>(PartitionKey, rowKey);
                TableResult result = await Table.ExecuteAsync(retrieveOperation);
                StateInfo customer = result.Result as StateInfo;
                if (customer is null)
                {
                    return false;
                }
                else return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error checking table: {e.Message}");
                throw;
            }
        }

        public async Task SaveAsync(string rowKey)
        {
            var entity = new StateInfo(PartitionKey, rowKey);
            try
            {
                // Create the InsertOrReplace table operation
                TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(entity);

                // Execute the operation.
                TableResult result = await Table.ExecuteAsync(insertOrMergeOperation);
            }
            catch (StorageException e)
            {
                Debug.WriteLine($"Error adding to state: {e.Message}");
                throw;
            }
        }

        public async Task<int> CleanupAsync()
        {
            var dt = DateTime.UtcNow.AddDays(-3);

            TableQuery<StateInfo> query = new TableQuery<StateInfo>()
                   .Where(TableQuery.GenerateFilterConditionForDate("TimeStamp", QueryComparisons.LessThan, dt));

            var counter = 0;
            
            foreach (var item in Table.ExecuteQuery(query))
            {
                var oper = TableOperation.Delete(item);
                await Table.ExecuteAsync(oper);
                counter++;
            }

            return counter;
        }
    }
}
