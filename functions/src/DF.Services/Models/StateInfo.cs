using Microsoft.Azure.Cosmos.Table;

namespace DF.Services.State.Models
{
    public class StateInfo : TableEntity
    {
        public StateInfo()
        {

        }
        public StateInfo(string partitionKey, string rowKey)
        {
            this.PartitionKey = partitionKey;
            this.RowKey = rowKey;
        }
    }
}
