using System;
using System.Collections.Generic;
using System.Globalization;
using System.ServiceModel;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Vendr.Core;
using Vendr.Core.Models;
using Vendr.Core.Web.Api;
using Vendr.Core.Web.PaymentProviders;

namespace Vendr.PaymentProviders.EPayBambora
{
    [PaymentProvider("epay-bambora", "Epay Bambora", "EPay/Bambora (using legacy payments) payment provider for one time payments")]
    public class EPayBamboraPaymentProvider : EPayBamboraPaymentProviderBase<EPayBamboraSettings>
    {
        public EPayBamboraPaymentProvider(VendrContext vendr) : base(vendr)
        {

        }


        public override bool CanCancelPayments => true;
        public override bool CanCapturePayments => true;
        public override bool CanRefundPayments => true;
        public override bool CanFetchPaymentStatus => true;

        // We'll finalize via webhook callback
        public override bool FinalizeAtContinueUrl => true;


        public override PaymentFormResult GenerateForm(OrderReadOnly order, string continueUrl, string cancelUrl, string callbackUrl, EPayBamboraSettings settings)
        {
            var currency = Vendr.Services.CurrencyService.GetCurrency(order.CurrencyId);
            var currencyCode = currency.Code.ToUpperInvariant();

            var orderAmount = (int)AmountToMinorUnits(order.TotalPrice.Value.WithTax);


            // Ensure currency has valid ISO 4217 code
            if (!Iso4217.CurrencyCodes.ContainsKey(currencyCode))
            {
                throw new Exception("Currency must be a valid ISO 4217 currency code: " + currency.Name);
            }


            PaymentForm htmlForm = new PaymentForm("https://ssl.ditonlinebetalingssystem.dk/integration/ewindow/Default.aspx", FormMethod.Get);

            if (settings.TestMode)
            {
                htmlForm.Inputs.Add("merchantnumber", settings.TestMerchantNumber);
            }
            else
            {
                htmlForm.Inputs.Add("merchantnumber", settings.LiveMerchantNumber);
            }

            //orderid
            htmlForm.Inputs.Add("orderid", order.CartNumber);

            //currency
            htmlForm.Inputs.Add("currency", currencyCode);
            //amount
            htmlForm.Inputs.Add("amount", (orderAmount).ToString("0", CultureInfo.InvariantCulture));
            htmlForm.Inputs.Add("accepturl", continueUrl);
            htmlForm.Inputs.Add("cancelurl", cancelUrl);
            htmlForm.Inputs.Add("callbackurl", callbackUrl);
            //instantcallback
            htmlForm.Inputs.Add("instantcallback", "1");

            //instantcapture
            if (htmlForm.Inputs.ContainsKey("instantcapture") && string.IsNullOrEmpty(htmlForm.Inputs["instantcapture"]))
                htmlForm.Inputs.Remove("instantcapture");
            //cardtype
            if (htmlForm.Inputs.ContainsKey("paymenttype") && string.IsNullOrEmpty(htmlForm.Inputs["paymenttype"]))
                htmlForm.Inputs.Remove("paymenttype");
            //windowstate
            if (htmlForm.Inputs.ContainsKey("windowstate") && string.IsNullOrEmpty(htmlForm.Inputs["windowstate"]))
                htmlForm.Inputs.Remove("windowstate");
            htmlForm.Inputs.Add("ownreceipt", "1");

            //ePay dont support to show order line information to the shopper
            //md5securitykey
            if (!string.IsNullOrEmpty(settings.Md5Key))
            {
                htmlForm.Inputs.Add("hash", GenerateMD5Hash(string.Join("", htmlForm.Inputs.Values) + settings.Md5Key));
            }

            htmlForm.Js = SubmitJavascriptFunction(htmlForm.Inputs, settings);

            return new PaymentFormResult()
            {
                Form = htmlForm,
            };

        }


        private string SubmitJavascriptFunction(IDictionary<string, string> inputFields, EPayBamboraSettings settings)
        {
            inputFields.MustNotBeNull("inputFields");
            settings.MustNotBeNull("settings");
            string rtnString = string.Empty;
            //If its state 3 (fullscreen) we return empty string because it's not supported by the JavaScript
            if (!inputFields.ContainsKey("windowstate") || inputFields["windowstate"] != "3")
            {
                //Check if its iFrame mode (2) and check if an html element is specified - else fallback to overlay (1)
                //if (inputFields.ContainsKey("windowstate") && inputFields["windowstate"] == "2" && !settings.ContainsKey("iframeelement"))
                {
                    inputFields["windowstate"] = "1";
                }
                rtnString += "var paymentwindow = new PaymentWindow({";
                foreach (var kvp in inputFields)
                {
                    rtnString += "'" + kvp.Key + "': '" + kvp.Value + "',";
                }
                rtnString = rtnString.Remove(rtnString.Length - 1, 1);
                rtnString += "});";
                //Check if it's iFrame mode
                //if (inputFields.ContainsKey("windowstate") && inputFields["windowstate"] == "2")
                //{
                //    rtnString += "paymentwindow.append('" + settings["iframeelement"] + "');";
                //}
                rtnString += "paymentwindow.open();";
            }
            return rtnString;
        }

        protected string GenerateMD5Hash(string input)
        {
            return ToHex(new System.Security.Cryptography.MD5CryptoServiceProvider().ComputeHash(System.Text.Encoding.UTF8.GetBytes(input)));
        }

        public string ToHex(byte[] bytes, bool upperCase = false)
        {
            StringBuilder stringBuilder = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                stringBuilder.Append(b.ToString("X2"));
            }
            return stringBuilder.ToString();
        }


        protected Vendr.PaymentProviders.EPayBambora.ePayService.PaymentSoapClient GetEPayServiceClient()
        {
            return new Vendr.PaymentProviders.EPayBambora.ePayService.PaymentSoapClient(new BasicHttpBinding(BasicHttpSecurityMode.Transport), new EndpointAddress("https://ssl.ditonlinebetalingssystem.dk/remote/payment.asmx"));
        }

 

        public override CallbackResult ProcessCallback(OrderReadOnly order, HttpRequestBase request, EPayBamboraSettings settings)
        {

            string transactionId = request.QueryString["txnid"];
            string strAmount = request.QueryString["amount"];
            string hash = request.QueryString["hash"];

            string md5CheckValue = string.Empty;

            foreach (string k in request.QueryString.Keys)
            {
                if (k != "hash")
                {
                    md5CheckValue += request.QueryString[k];
                }
            }
            if (!string.IsNullOrEmpty(settings.Md5Key))
            {
                md5CheckValue += settings.Md5Key;
            }


            if (order.CartNumber == request.QueryString["orderid"] && GenerateMD5Hash(md5CheckValue) == hash.ToUpper())
            {
                string fee = request.QueryString["txnfee"];
                string cardid = request.QueryString["paymenttype"];
                string cardnopostfix = request.QueryString["cardno"];

                decimal totalAmount = (decimal.Parse(strAmount, CultureInfo.InvariantCulture) + decimal.Parse(fee, CultureInfo.InvariantCulture));

                //bool autoCaptured = settings.ContainsKey("instantcapture") && settings["instantcapture"] == "1";

                //callbackInfo = new CallbackInfo(totalAmount / 100M, transaction, !autoCaptured ? PaymentState.Authorized : PaymentState.Captured, cardid, cardnopostfix);

                return CallbackResult.Ok(new TransactionInfo
                {
                    TransactionId = transactionId,
                    AmountAuthorized = AmountFromMinorUnits(Convert.ToInt64(totalAmount) + Convert.ToInt64(fee)),
                    TransactionFee = AmountFromMinorUnits(Convert.ToInt64(fee)),
                    PaymentStatus = PaymentStatus.Authorized
                });


            }
            else
            {
                return CallbackResult.BadRequest();
            }
        }

        public override ApiResult CapturePayment(OrderReadOnly order, EPayBamboraSettings settings)
        {

            if (settings != null)
            {
                if (order != null)
                {
                    string merchantNumber = "";
                    if (settings.TestMode)
                    {
                        merchantNumber = settings.TestMerchantNumber;
                    }
                    else
                    {
                        merchantNumber = settings.LiveMerchantNumber;
                    }


                    if (!string.IsNullOrEmpty(merchantNumber))
                    {


                        ApiResult apiInfo = null;
                        try
                        {

                            int pbsResponse = 0;
                            int ePayResponse = 0;
                            if (GetEPayServiceClient().capture(
                                merchantnumber: int.Parse(merchantNumber),
                                transactionid: long.Parse(order.TransactionInfo.TransactionId),
                                amount: (int)(order.TransactionInfo.AmountAuthorized.Value * 100M),
                                group: string.Empty,
                                pwd: settings.WebservicePassword != null ? settings.WebservicePassword : string.Empty,
                                invoice: string.Empty,
                                orderid: order.CartNumber,
                                pbsResponse: ref pbsResponse,
                                epayresponse: ref ePayResponse))
                            {
                                apiInfo = new ApiResult()
                                {
                                    TransactionInfo = new TransactionInfoUpdate()
                                    {
                                        TransactionId = order.TransactionInfo.TransactionId,
                                        PaymentStatus = PaymentStatus.Captured
                                    }
                                };
                            }
                            else
                            {
                                Vendr.Log.Warn<EPayBamboraPaymentProvider>($"Bambora EPay({order.OrderNumber}) - Error making API request - error code: '{ePayResponse}', pbs response: '{pbsResponse}'");
                            }
                        }
                        catch (Exception ex)
                        {
                            Vendr.Log.Error<EPayBamboraPaymentProvider>(ex, "Bambora - CapturePayment");
                        }
                        return apiInfo;
                    }
                    else
                    {
                        throw new System.Exception($"Merchantnumber not specified. (Testmode: { settings.TestMode.ToString() }) ");
                    }
                }
                else
                {
                    throw new System.Exception("Order not found. Verify if switched between test/live.");
                }
            }
            else
            {
                throw new System.Exception("Settings are not created or found?!");
            }

            //return ApiResult.Empty;
        }

        public override ApiResult RefundPayment(OrderReadOnly order, EPayBamboraSettings settings)
        {
            if (settings != null)
            {
                if (order != null)
                {
                    string merchantNumber = "";
                    if (settings.TestMode)
                    {
                        merchantNumber = settings.TestMerchantNumber;
                    }
                    else
                    {
                        merchantNumber = settings.LiveMerchantNumber;
                    }
                    if (!string.IsNullOrEmpty(merchantNumber))
                    {
                        ApiResult apiInfo = null;
                        try
                        {
                            int pbsResponse = 0;
                            int ePayResponse = 0;
                            if (GetEPayServiceClient().credit(
                                merchantnumber: int.Parse(merchantNumber),
                                transactionid: long.Parse(order.TransactionInfo.TransactionId),
                                amount: (int)(order.TransactionInfo.AmountAuthorized.Value * 100M),
                                group: string.Empty,
                                invoice: string.Empty,
                                pwd: settings.WebservicePassword != null ? settings.WebservicePassword : string.Empty,
                                orderid: order.CartNumber,
                                pbsresponse: ref pbsResponse,
                                epayresponse: ref ePayResponse))
                            {
                                apiInfo = new ApiResult()
                                {
                                    TransactionInfo = new TransactionInfoUpdate()
                                    {
                                        TransactionId = order.TransactionInfo.TransactionId,
                                        PaymentStatus = PaymentStatus.Captured
                                    }
                                };
                            }
                            else
                            {
                                Vendr.Log.Warn<EPayBamboraPaymentProvider>($"Bambora EPay({order.OrderNumber}) - Error making API request - error code: '{ePayResponse}', pbs response: '{pbsResponse}'");
                            }
                        }
                        catch (Exception ex)
                        {
                            Vendr.Log.Error<EPayBamboraPaymentProvider>(ex, "Bambora - RefundPayment");
                        }
                        return apiInfo;
                    }
                    else
                    {
                        throw new System.Exception($"Merchantnumber not specified. (Testmode: { settings.TestMode.ToString() }) ");
                    }
                }
                else
                {
                    throw new System.Exception("Order not found. Verify if switched between test/live.");
                }
            }
            else
            {
                throw new System.Exception("Settings are not created or found?!");
            }
        }   
        
        public override ApiResult CancelPayment(OrderReadOnly order, EPayBamboraSettings settings)
        {
            if (settings != null)
            {
                if (order != null)
                {
                    string merchantNumber = "";
                    if (settings.TestMode)
                    {
                        merchantNumber = settings.TestMerchantNumber;
                    }
                    else
                    {
                        merchantNumber = settings.LiveMerchantNumber;
                    }
                    if (!string.IsNullOrEmpty(merchantNumber))
                    {
                        ApiResult apiInfo = null;
                        try
                        {
                            int pbsResponse = 0;
                            int ePayResponse = 0;
                            if (GetEPayServiceClient().delete(
                                merchantnumber: int.Parse(merchantNumber),
                                transactionid: long.Parse(order.TransactionInfo.TransactionId),
                                group: string.Empty,
                                pwd: settings.WebservicePassword != null ? settings.WebservicePassword : string.Empty,
                                epayresponse: ref ePayResponse))
                            {
                                apiInfo = new ApiResult()
                                {
                                    TransactionInfo = new TransactionInfoUpdate()
                                    {
                                        TransactionId = order.TransactionInfo.TransactionId,
                                        PaymentStatus = PaymentStatus.Captured
                                    }
                                };
                            }
                            else
                            {
                                Vendr.Log.Warn<EPayBamboraPaymentProvider>($"Bambora EPay({order.OrderNumber}) - Error making API request - error code: '{ePayResponse}', pbs response: '{pbsResponse}'");
                            }
                        }
                        catch (Exception ex)
                        {
                            Vendr.Log.Error<EPayBamboraPaymentProvider>(ex, "Bambora - CancelPayment");
                        }
                        return apiInfo;
                    }
                    else
                    {
                        throw new System.Exception($"Merchantnumber not specified. (Testmode: { settings.TestMode.ToString() }) ");
                    }
                }
                else
                {
                    throw new System.Exception("Order not found. Verify if switched between test/live.");
                }
            }
            else
            {
                throw new System.Exception("Settings are not created or found?!");
            }
        }


        public override ApiResult FetchPaymentStatus(OrderReadOnly order, EPayBamboraSettings settings)
        {

            if (settings != null)
            {
                if (order != null)
                {
                    string merchantNumber = "";
                    if (settings.TestMode)
                    {
                        merchantNumber = settings.TestMerchantNumber;
                    }
                    else
                    {
                        merchantNumber = settings.LiveMerchantNumber;
                    }
                    if (!string.IsNullOrEmpty(merchantNumber))
                    {
                        ApiResult apiInfo = null;
                        try
                        {

                            
                            int ePayResponse = 0;
                            ePayService.TransactionInformationType transactionInformationType = new ePayService.TransactionInformationType();

                            if (GetEPayServiceClient().gettransaction(
                                merchantnumber: int.Parse(merchantNumber),
                                transactionid: long.Parse(order.TransactionInfo.TransactionId),
                                pwd: settings.WebservicePassword != null ? settings.WebservicePassword : string.Empty,
                                transactionInformation: ref transactionInformationType,
                                epayresponse: ref ePayResponse))
                            {

                                PaymentStatus status = PaymentStatus.PendingExternalSystem;
                                switch(transactionInformationType.status)
                                {
                                    case ePayService.TransactionStatus.PAYMENT_NEW:
                                        status = PaymentStatus.Authorized;
                                        break;

                                    case ePayService.TransactionStatus.PAYMENT_CAPTURED:

                                        if (transactionInformationType.capturedamount > 0 && transactionInformationType.creditedamount > 0 && transactionInformationType.creditedamount == transactionInformationType.capturedamount)
                                        {
                                            status = PaymentStatus.Refunded;
                                        }
                                        else
                                        {
                                            status = PaymentStatus.Captured;
                                        }

                                        break;

                                    case ePayService.TransactionStatus.PAYMENT_EUROLINE_WAIT_CAPTURE:
                                    case ePayService.TransactionStatus.PAYMENT_EUROLINE_WAIT_CREDIT:
                                        status = PaymentStatus.PendingExternalSystem;
                                        break;

                                    case ePayService.TransactionStatus.PAYMENT_DELETED:
                                        status = PaymentStatus.Cancelled;
                                        break;
                                    default:
                                        status = PaymentStatus.Error;
                                        break;

                                }



                                apiInfo = new ApiResult()
                                {
                                    TransactionInfo = new TransactionInfoUpdate()
                                    {
                                        TransactionId = order.TransactionInfo.TransactionId,
                                        PaymentStatus = status
                                    }
                                };
                            }
                            else
                            {
                                Vendr.Log.Warn<EPayBamboraPaymentProvider>($"Bambora EPay({order.OrderNumber}) - Error making API request - error code: '{ePayResponse}'");
                            }
                        }
                        catch (Exception ex)
                        {
                            Vendr.Log.Error<EPayBamboraPaymentProvider>(ex, "Bambora - CancelPayment");
                        }
                        return apiInfo;
                    }
                    else
                    {
                        throw new System.Exception($"Merchantnumber not specified. (Testmode: { settings.TestMode.ToString() }) ");
                    }
                }
                else
                {
                    throw new System.Exception("Order not found. Verify if switched between test/live.");
                }
            }
            else
            {
                throw new System.Exception("Settings are not created or found?!");
            }

        }

    }
}
