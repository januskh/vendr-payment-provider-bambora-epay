using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vendr.PaymentProviders.EPayBambora.Api.Models
{
    public class EPayBamboraClientConfig
    {

        public string WebservicePassword { get; set; }
        public string MerchantNumber { get; set; }
        public string SecretKey { get; set; }
        public string MD5Key { get; set; }
        public string Authorization { get; set; }

    }
}
