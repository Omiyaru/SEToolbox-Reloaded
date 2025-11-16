using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SEToolbox.Controls
{
    // Actually it's a component, but meh.

    class MyWebClient : HttpClient
    {
        public Uri RequestUri { get; private set; }
        
        public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Task<HttpResponseMessage> response = base.SendAsync(request, cancellationToken);
         try
         {
            response.Wait();
         }
         catch (Exception ex)
         {
           
            throw new WebException( ex.Message);
         }  
             RequestUri = response?.Result.RequestMessage.RequestUri;
            return response;
        }
    }
}
