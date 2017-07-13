using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hast.Common.Helpers;
using Hast.Layer;
using Hast.Remote.Bridge.Models;
using Hast.Transformer.Abstractions;

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
                    Id = Sha2456Helper.ComputeHash(path),
                    FileContent = File.ReadAllBytes(path)
                });

            var apiConfiguration = new Bridge.Models.HardwareGenerationConfiguration
            {
                CustomConfiguration = configuration.CustomConfiguration,
                DeviceName = configuration.DeviceName,
                HardwareEntryPointMemberFullNames = configuration.HardwareEntryPointMemberFullNames,
                HardwareEntryPointMemberNamePrefixes = configuration.HardwareEntryPointMemberNamePrefixes
            };

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
                     exceptionMessageBase + "The following error(s) happened: " + 
                     string.Join(Environment.NewLine, transformationResult.Errors));
            }

            return new RemoteHardwareDescription
            {
                HardwareEntryPointNamesToMemberIdMappings = transformationResult.HardwareDescription.HardwareEntryPointNamesToMemberIdMappings,
                Language = transformationResult.HardwareDescription.Language,
                Source = transformationResult.HardwareDescription.Source
            };
        }


        private class RemoteHardwareDescription : IHardwareDescription
        {
            public IReadOnlyDictionary<string, int> HardwareEntryPointNamesToMemberIdMappings { get; set; }
            public string Language { get; set; }
            public string Source { get; set; }


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
