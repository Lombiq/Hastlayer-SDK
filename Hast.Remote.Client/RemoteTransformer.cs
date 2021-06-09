using Hast.Common.Models;
using Hast.Layer;
using Hast.Remote.Bridge.Models;
using Hast.Transformer.Abstractions;
using Newtonsoft.Json;
using RestEase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using HardwareGenerationConfiguration = Hast.Remote.Bridge.Models.HardwareGenerationConfiguration;

namespace Hast.Remote.Client
{
    public class RemoteTransformer : ITransformer
    {
        public async Task<IHardwareDescription> TransformAsync(IList<string> assemblyPaths, IHardwareGenerationConfiguration configuration)
        {
            var apiClient = ApiClientFactory.CreateApiClient(configuration.RemoteClientConfiguration());

            var assemblyContainers = assemblyPaths.Select(GetAssemblyContainers);

            var apiConfiguration = new HardwareGenerationConfiguration(
                configuration.DeviceName,
                configuration.CustomConfiguration,
                configuration.HardwareEntryPointMemberFullNames,
                configuration.HardwareEntryPointMemberNamePrefixes);

            try
            {
                var transformationTicket = await apiClient
                    .RequestTransformationAsync(new TransformationRequest
                    {
                        Assemblies = assemblyContainers,
                        Configuration = apiConfiguration,
                    });

                TransformationResult transformationResult = null;
                const int maxResultAttemptCount = 100;
                var waitAttemptIndex = 0;
                var waitMilliseconds = 333;
                while (transformationResult == null && waitAttemptIndex < maxResultAttemptCount)
                {
                    await Task.Delay(waitMilliseconds);
                    var transformationResultResponse = await apiClient.GetTransformationResultAsync(transformationTicket.Token);

                    if (transformationResultResponse.ResponseMessage.IsSuccessStatusCode)
                    {
                        transformationResult = transformationResultResponse.GetContent();
                    }
                    else if (transformationResultResponse.ResponseMessage.StatusCode != HttpStatusCode.NotFound)
                    {
                        transformationResultResponse.ResponseMessage.EnsureSuccessStatusCode();
                    }

                    waitAttemptIndex++;
                    if (waitAttemptIndex % 10 == 0) waitMilliseconds *= 2;
                }

                var exceptionMessageBase =
                    "Transforming the following assemblies failed: " + string.Join(", ", assemblyPaths) + ". ";
                if (transformationResult == null)
                {
                    throw new TimeoutException(
                        exceptionMessageBase + "The remote Hastlayer service didn't produce a result in a timely manner. " +
                        "This could indicate a problem with the service or that the assemblies contained exceptionally complex code.");
                }

                var localVersion = GetType().Assembly.GetName().Version.ToString();
                if (transformationResult.RemoteHastlayerVersion != localVersion)
                {
                    throw new InvalidOperationException(
                        "The local version of Hastlayer is out of date compared to the remote one " +
                        $"(remote version: {transformationResult.RemoteHastlayerVersion}, local version: {localVersion}). " +
                        "Please update Hastlayer otherwise incompatibilities may occur.");
                }

                if (transformationResult.Errors?.Any() == true)
                {
                    throw new InvalidOperationException(
                         exceptionMessageBase + "The following error(s) happened: " + Environment.NewLine +
                         string.Join(Environment.NewLine, transformationResult.Errors));
                }

                var hardwareDescription = transformationResult.HardwareDescription;

                if (hardwareDescription.Language != VhdlHardwareDescription.LanguageName)
                {
                    throw new NotSupportedException("Only hardware descriptions in the VHDL language are supported.");
                }

                using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(hardwareDescription.SerializedHardwareDescription));
                return await VhdlHardwareDescription.DeserializeAsync(memoryStream);
            }
            catch (ApiException ex)
            {
                var message = "Remote transformation failed: ";

                message += ex.StatusCode == HttpStatusCode.Unauthorized
                    ? "Authorizing with Hastlayer Remote Services failed. Maybe you mistyped your credentials?"
                    : $"The response code was {ex.StatusCode}. Most possibly there is some issue with Hastlayer " +
                      $"Remote Services. If this error persists please get in touch with us under " +
                      $"https://hastlayer.com/contact.";

                throw new RemoteTransformationException(message, ex);
            }
            catch (JsonReaderException ex)
            {
                // This will happen also when authorization fails and Azure AD authentication takes over from the API,
                // and redirects the client to the Microsoft login screen (there doesn't seem to be a way to prevent
                // this).
                throw new RemoteTransformationException(
                    "Remote transformation failed because Hastlayer Remote Services returned an unexpected " +
                    "response. This might be because authorization failed (check if you mistyped your credentials) " +
                    "or because there is some issue with the service. If this error persists please get in touch " +
                    "with us under https://hastlayer.com/contact.",
                    ex);
            }
        }

        private AssemblyContainer GetAssemblyContainers(string path) =>
            new(Path.GetFileNameWithoutExtension(path), File.ReadAllBytes(path));
    }
}
