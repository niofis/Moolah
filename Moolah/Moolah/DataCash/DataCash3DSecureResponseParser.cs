﻿using System;
using System.Xml.Linq;

namespace Moolah.DataCash
{
    public interface IDataCash3DSecureResponseParser
    {
        I3DSecureResponse Parse(string dataCashResponse);
    }

    public class DataCash3DSecureResponseParser : IDataCash3DSecureResponseParser
    {
        public I3DSecureResponse Parse(string dataCashResponse)
        {
            var document = XDocument.Parse(dataCashResponse);

            var response = new DataCash3DSecurePaymentResponse(document);

            string transactionReference;
            if (document.TryGetXPathValue("Response/datacash_reference", out transactionReference))
                response.TransactionReference = transactionReference;

            string avsCv2Result;
            if (document.TryGetXPathValue("Response/CardTxn/Cv2Avs/cv2avs_status", out avsCv2Result))
                response.AvsCv2Result = avsCv2Result;

            var dataCashStatus = int.Parse(document.XPathValue("Response/status"));
            switch (dataCashStatus)
            {
                case DataCashStatus.Success:
                    response.Status = PaymentStatus.Successful;
                    break;
                case DataCashStatus.RequiresThreeDSecureAuthentication:
                    response.Status = PaymentStatus.Pending;
                    response.Requires3DSecurePayerVerification = true;
                    response.PAReq = document.XPathValue("Response/CardTxn/ThreeDSecure/pareq_message");
                    response.ACSUrl = document.XPathValue("Response/CardTxn/ThreeDSecure/acs_url");
                    break;
                default:
                    if (DataCashStatus.CanImmediatelyAuthorise(dataCashStatus))
                        response.Status = PaymentStatus.Pending;
                    else

                    {
                        response.Status = PaymentStatus.Failed;
                        response.IsSystemFailure = DataCashStatus.IsSystemFailure(dataCashStatus);
                        var failureReason = DataCashStatus.FailureReason(dataCashStatus);
                        response.FailureMessage = failureReason.Message;
                        response.FailureType = failureReason.Type;
                    }
                    break;
            }

            return response;
        }
    }
}
