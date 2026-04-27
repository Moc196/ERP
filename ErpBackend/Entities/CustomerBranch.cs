using System.Text.Json.Serialization;

namespace ErpBackend.Entities
{
    public class CustomerBranch
    {
        public int CustomerId { get; set; }
        [JsonIgnore]
        public virtual Customer? Customer { get; set; }

        public int BranchId { get; set; }
        public virtual Branch? Branch { get; set; }
    }
}
