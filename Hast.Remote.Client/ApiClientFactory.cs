using Hast.Remote.Bridge.Models;
using Hast.Remote.Configuration;
using RestEase;
using System;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Hast.Remote.Client;

public static class ApiClientFactory
{
    public static IHastlayerApi CreateApiClient(RemoteClientConfiguration configuration)
    {
        var api = RestClient.For<IHastlayerApi>(configuration.EndpointBaseUri);

        api.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes(configuration.AppName + ":" + configuration.AppSecret)));

        return api;
    }

    /// <summary>
    /// The API for communicating with the transformation worker server using REST.
    /// </summary>
    public interface IHastlayerApi
    {
        /// <summary>
        /// Gets or sets the authentication information needed to authorize usage.
        /// </summary>
        [Header("Authorization")]
        AuthenticationHeaderValue Authorization { get; set; }

        /// <summary>
        /// Sends the configuration and assemblies requesting transformation.
        /// </summary>
        [Post("TransformationRequests")]
        Task<TransformationTicket> RequestTransformationAsync([Body] TransformationRequest transformationRequest);

        /// <summary>
        /// Retrieves the result of the transformation when it's finished using the ticket from <see
        /// cref="RequestTransformationAsync(TransformationRequest)"/>.
        /// </summary>
        [Get("TransformationResults"), AllowAnyStatusCode]
        Task<Response<TransformationResult>> GetTransformationResultAsync([Query] string transformationToken);
    }
}
