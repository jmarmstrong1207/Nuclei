using System;

namespace Nuclei.CritzOS;

public class CritzOSGlobals
{
    internal static string ServerName = Environment.GetEnvironmentVariable("serverName")!;
    internal static string connectionString = Environment.GetEnvironmentVariable("connectionString")!;
}