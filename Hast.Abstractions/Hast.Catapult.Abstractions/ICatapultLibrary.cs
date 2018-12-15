using System;
using System.Threading.Tasks;

namespace Hast.Catapult.Abstractions
{
    public interface ICatapultLibrary : IDisposable
    {
        int BufferCount { get; }
        int BufferSize { get; }
        IntPtr Handle { get; }
        CatapultLogFunction LogFunction { get; }
        ICatapultNativeLibrary NativeLibrary { get; }
        bool PcieEnabled { get; }
        int ShellRegisterCount { get; }

        Task<Memory<byte>> ExecuteJob(int bufferIndex, Memory<byte> inputData);
        void VerifyResult(Constants.Status status);
    }
}