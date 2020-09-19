using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vendr.PaymentProviders.EPayBambora.Api.Models
{
    public class EPayBamboraTotals
    {
        [JsonProperty("authorized")]
        public int Authorized { get; set; }
        [JsonProperty("balance")]
        public int Balance { get; set; }
        [JsonProperty("captured")]
        public int Captured { get; set; }
        [JsonProperty("credited")]
        public int Credited { get; set; }
        [JsonProperty("declined")]
        public int Declined { get; set; }
        [JsonProperty("feeamount")]
        public int FeeAmount { get; set; }
    }
}
