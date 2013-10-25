// Guids.cs
// MUST match guids.h
using System;

namespace wwwsysnetpekr.XmlCodeGenerator
{
    static class GuidList
    {
        public const string guidXmlCodeGeneratorPkgString = "b9a2f630-ec32-49a5-96a9-612bcb1d25e4";
        public const string guidXmlCodeGeneratorCmdSetString = "d24cbf97-3f83-4ced-b342-8ae04b59f46e";

        public static readonly Guid guidXmlCodeGeneratorCmdSet = new Guid(guidXmlCodeGeneratorCmdSetString);
    };
}