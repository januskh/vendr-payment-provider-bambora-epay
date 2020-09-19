using Flurl.Http;
using System.Text;
using System.Linq;
using System.Web;
using Vendr.PaymentProviders.EPayBambora.Api.Models;
using System;
using System.Security.Cryptography;
using System.Web.Services.Description;

namespace Vendr.PaymentProviders.EPayBambora.Api
{
    public class EPayBamboraClient
    {
        private EPayBamboraClientConfig _config;

        public EPayBamboraClient(EPayBamboraClientConfig config)
        {
            _config = config;
        }

    }
}
