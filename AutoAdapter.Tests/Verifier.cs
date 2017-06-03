using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using AutoAdapter.Fody;
using NUnit.Framework;

namespace AutoAdapter.Tests
{
    public static class Verifier
    {
        public static void Verify(string beforeAssemblyPath, string afterAssemblyPath)
        {
            var before = Validate(beforeAssemblyPath);
            var after = Validate(afterAssemblyPath);
            var message = $"Failed processing {Path.GetFileName(afterAssemblyPath)}\r\n{after}";
            Assert.AreEqual(TrimLineNumbers(before), TrimLineNumbers(after), message);
        }

        private static string Validate(string assemblyPath)
        {
            var exePath = GetPathToPeVerify();

            if (exePath.HasNoValue)
                throw new Exception("Could not find PEVerify.exe");

            var process = Process.Start(new ProcessStartInfo(exePath.GetValue(), "\"" + assemblyPath + "\"")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            process.WaitForExit(10000);

            return process.StandardOutput.ReadToEnd().Trim().Replace(assemblyPath, "");
        }

        private static Maybe<string> GetPathToPeVerify()
        {
            var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            var possiblePathds = new[]
            {
                $@"{programFilesX86}\Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.0 Tools\PEVerify.exe",
                $@"{programFilesX86}\Microsoft SDKs\Windows\v8.0A\Bin\NETFX 4.0 Tools\PEVerify.exe",
                $@"{programFilesX86}\Microsoft SDKs\Windows\v8.1A\Bin\NETFX 4.5.1 Tools\PEVerify.exe",
                $@"{programFilesX86}\Microsoft SDKs\Windows\v10.0A\Bin\NETFX 4.6 Tools\PEVerify.exe",
                $@"{programFilesX86}\Microsoft SDKs\Windows\v10.0A\Bin\NETFX 4.6.1 Tools\PEVerify.exe"
            };

            return possiblePathds.FirstOrNoValue(File.Exists);
        }

        private static string TrimLineNumbers(string foo)
        {
            return Regex.Replace(foo, @"0x.*]", "");
        }
    }
}
