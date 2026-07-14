using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace RemoteWake.Infrastructure.Windows.Security;

[SupportedOSPlatform("windows")]
internal static partial class WindowsDpapi
{
    private const uint UiForbidden = 0x1;
    private const int MaximumInputBytes = 65_536;

    public static byte[] Protect(ReadOnlySpan<byte> plaintext) =>
        Transform(plaintext, protect: true);

    public static byte[] Unprotect(ReadOnlySpan<byte> ciphertext) =>
        Transform(ciphertext, protect: false);

    private static byte[] Transform(ReadOnlySpan<byte> input, bool protect)
    {
        if (input.IsEmpty || input.Length > MaximumInputBytes)
        {
            throw new ArgumentException("DPAPI input must contain at most 65536 bytes.", nameof(input));
        }

        var inputBytes = input.ToArray();
        var inputBlob = new DataBlob
        {
            Size = inputBytes.Length,
            Data = Marshal.AllocHGlobal(inputBytes.Length),
        };
        var outputBlob = default(DataBlob);

        try
        {
            Marshal.Copy(inputBytes, 0, inputBlob.Data, inputBytes.Length);
            var succeeded = protect
                ? CryptProtectData(
                    ref inputBlob,
                    "RemoteWake private material",
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    UiForbidden,
                    out outputBlob)
                : CryptUnprotectData(
                    ref inputBlob,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    UiForbidden,
                    out outputBlob);

            if (!succeeded)
            {
                throw new Win32Exception(Marshal.GetLastPInvokeError(), "DPAPI operation failed.");
            }

            var output = new byte[outputBlob.Size];
            Marshal.Copy(outputBlob.Data, output, 0, output.Length);
            return output;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(inputBytes);
            if (inputBlob.Data != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(inputBlob.Data);
            }

            if (outputBlob.Data != IntPtr.Zero)
            {
                LocalFree(outputBlob.Data);
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Size;
        public IntPtr Data;
    }

    [LibraryImport("Crypt32.dll", EntryPoint = "CryptProtectData", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CryptProtectData(
        ref DataBlob dataIn,
        string? description,
        IntPtr optionalEntropy,
        IntPtr reserved,
        IntPtr promptStruct,
        uint flags,
        out DataBlob dataOut);

    [LibraryImport("Crypt32.dll", EntryPoint = "CryptUnprotectData", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CryptUnprotectData(
        ref DataBlob dataIn,
        IntPtr description,
        IntPtr optionalEntropy,
        IntPtr reserved,
        IntPtr promptStruct,
        uint flags,
        out DataBlob dataOut);

    [LibraryImport("Kernel32.dll")]
    private static partial IntPtr LocalFree(IntPtr memory);
}
