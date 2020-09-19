using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vendr.PaymentProviders.EPayBambora.Api.Models
{
    public class EPayBamboraTransaction
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("orderid")]
        public string OrderId { get; set; }
        [JsonProperty("merchantnumber")]
        public string MerchantNumber { get; set; }
        [JsonProperty("reference")]
        public string Reference { get; set; }
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("total")]
        public EPayBamboraTotals Total { get; set; }
    }
}
