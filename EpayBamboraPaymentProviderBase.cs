using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Vendr.Core;
using Vendr.Core.Models;
using Vendr.Core.Web.Api;
using Vendr.Core.Web.PaymentProviders;
using Vendr.PaymentProviders.EPayBambora.Api.Models;

namespace Vendr.PaymentProviders.EPayBambora
{
    public abstract class EPayBamboraPaymentProviderBase<TSettings> : PaymentProviderBase<TSettings>
     where TSettings : EPayBamboraSettingsBase, new()
    {
        public EPayBamboraPaymentProviderBase(VendrContext vendr)
            : base(vendr)
        {
            System.Diagnostics.Debug.WriteLine("Base!");
        }

        public override string GetCancelUrl(OrderReadOnly order, TSettings settings)
        {
            settings.MustNotBeNull("settings");
            settings.CancelUrl.MustNotBeNull("settings.CancelUrl");
            return settings.CancelUrl;
        }
        public override string GetContinueUrl(OrderReadOnly order, TSettings settings)
        {
            settings.MustNotBeNull("settings");
            settings.AcceptUrl.MustNotBeNull("settings.AcceptUrl");
            return settings.AcceptUrl;
        }
        public override string GetErrorUrl(OrderReadOnly order, TSettings settings)
        {
            settings.MustNotBeNull("settings");
            settings.ErrorUrl.MustNotBeNull("settings.ErrorUrl");
            return settings.ErrorUrl;
        }
        protected EPayBamboraClientConfig GetBamboraClientConfig(EPayBamboraSettingsBase settings)
        {
            EPayBamboraClientConfig config;
            if (settings.TestMode)
            {
                config = new EPayBamboraClientConfig
                {
                    WebservicePassword = settings.WebservicePassword,
                    MerchantNumber = settings.TestMerchantNumber,
                    MD5Key = settings.Md5Key
                };
            }
            else
            {
                config = new EPayBamboraClientConfig
                {
                    WebservicePassword = settings.WebservicePassword,
                    MerchantNumber = settings.LiveMerchantNumber,
                    MD5Key = settings.Md5Key
                };
            }
            var apiKey = GenerateApiKey(config.WebservicePassword, config.MerchantNumber, config.SecretKey);
            config.Authorization = "Basic " + apiKey;
            return config;
        }
        protected PaymentStatus GetPaymentStatus(EPayBamboraTransaction transaction)
        {
            if (transaction.Total.Credited > 0)
                return PaymentStatus.Refunded;
            if (transaction.Total.Declined > 0)
                return PaymentStatus.Cancelled;
            if (transaction.Total.Captured > 0)
                return PaymentStatus.Captured;
            if (transaction.Total.Authorized > 0)
                return PaymentStatus.Authorized;
            return PaymentStatus.Initialized;
        }
        protected string BamboraSafeOrderId(string orderId)
        {
            return Regex.Replace(orderId, "[^a-zA-Z0-9]", "");
        }
        private string GenerateApiKey(string accessToken, string merchantNumber, string secretToken)
        {
            var unencodedApiKey = $"{accessToken}@{merchantNumber}:{secretToken}";
            var unencodedApiKeyAsBytes = Encoding.UTF8.GetBytes(unencodedApiKey);
            return Convert.ToBase64String(unencodedApiKeyAsBytes);
        }
    }
}
