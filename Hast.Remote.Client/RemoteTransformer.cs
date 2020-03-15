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
using System.Text;
using System.Threading.Tasks;

namespace Hast.Remote.Client
{
    public class RemoteTransformer : ITransformer
    {
        public async Task<IHardwareDescription> Transform(IEnumerable<string> assemblyPaths, IHardwareGenerationConfiguration configuration)
        {
            var apiClient = ApiClientFactory.CreateApiClient(configuration.RemoteClientConfiguration());

            var assemblyContainers = assemblyPaths
                .Select(path => new AssemblyContainer
                {
                    Name = Path.GetFileNameWithoutExtension(path),
                    FileContent = File.ReadAllBytes(path)
                });

            var apiConfiguration = new Bridge.Models.HardwareGenerationConfiguration
            {
                CustomConfiguration = configuration.CustomConfiguration,
                DeviceName = configuration.DeviceName,
                HardwareEntryPointMemberFullNames = configuration.HardwareEntryPointMemberFullNames,
                HardwareEntryPointMemberNamePrefixes = configuration.HardwareEntryPointMemberNamePrefixes
            };

            try
            {
                var transformationTicket = await apiClient
                    .RequestTransformation(new TransformationRequest
                    {
                        Assemblies = assemblyContainers,
                        Configuration = apiConfiguration
                    });


                TransformationResult transformationResult = null;
                const int maxResultAttemptCount = 100;
                var waitAttemptIndex = 0;
                var waitMilliseconds = 333;
                while (transformationResult == null && waitAttemptIndex < maxResultAttemptCount)
                {
                    await Task.Delay(waitMilliseconds);
                    var transformationResultResponse = await apiClient.GetTransformationResult(transformationTicket.Token);

                    if (transformationResultResponse.ResponseMessage.IsSuccessStatusCode)
                    {
                        transformationResult = transformationResultResponse.GetContent();
                    }
                    else if (transformationResultResponse.ResponseMessage.StatusCode != System.Net.HttpStatusCode.NotFound)
                    {
                        transformationResultResponse.ResponseMessage.EnsureSuccessStatusCode();
                    }

                    if (++waitAttemptIndex % 10 == 0) waitMilliseconds *= 2;
                }

                var exceptionMessageBase =
                    "Transforming the following assemblies failed: " + string.Join(", ", assemblyPaths) + ". ";
                if (transformationResult == null)
                {
                    throw new Exception(
                        exceptionMessageBase + "The remote Hastlayer service didn't produce a result in a timely manner. " +
                        "This could indicate a problem with the service or that the assemblies contained exceptionally complex code.");
                }

                if (transformationResult.Errors?.Any() == true)
                {
                    throw new Exception(
                         exceptionMessageBase + "The following error(s) happened: " + Environment.NewLine +
                         string.Join(Environment.NewLine, transformationResult.Errors));
                }

                var hardwareDescription = transformationResult.HardwareDescription;

                if (hardwareDescription.Language != VhdlHardwareDescription.LanguageName)
                {
                    throw new NotSupportedException("Only hardware descriptions in the VHDL language are supported.");
                }

                using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(hardwareDescription.SerializedHardwareDescription)))
                {
                    return await VhdlHardwareDescription.Deserialize(memoryStream);
                }
            }
            catch (ApiException ex)
            {
                var message = "Remote transformation failed: ";

                if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    message += "Authorizing with Hastlayer Remote Services failed. Maybe you mistyped your credentials?";
                }
                else
                {
                    message += $"The response code was {ex.StatusCode}. Most possibly there is some issue with Hastlayer Remote Services. If this error persists please get in touch with us under https://hastlayer.com/contact.";
                }

                throw new RemoteTransformationException(message, ex);
            }
            catch (JsonReaderException ex)
            {
                // This will happen also when authorization fails and Azure AD authentication takes over from the API,
                // and redirects the client to the Microsoft login screen (there doesn't seem to be a way to prevent
                // this).
                throw new RemoteTransformationException("Remote transformation failed because Hastlayer Remote Services returned an unexpected response. This might be because authorization failed (check if you mistyped your credentials) or because there is some issue with the service. If this error persists please get in touch with us under https://hastlayer.com/contact.", ex);
            }
        }


        private class RemoteHardwareDescription : IHardwareDescription
        {
            public IReadOnlyDictionary<string, int> HardwareEntryPointNamesToMemberIdMappings { get; set; }
            public string Language { get; set; }
            public string Source { get; set; }
            public IEnumerable<ITransformationWarning> Warnings { get; set; }

            public Task Serialize(Stream stream)
            {
                throw new NotImplementedException();
            }

            public Task WriteSource(Stream stream)
            {
                using (var streamWriter = new StreamWriter(stream))
                {
                    // WriteAsync would throw a "The stream is currently in use by a previous operation on the stream." for
                    // FileStreams, even though supposedly there's no operation on the stream.
                    streamWriter.Write(Source);
                }

                return Task.CompletedTask;
            }
        }
    }
}
