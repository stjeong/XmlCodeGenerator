using BclExtension;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Xsl;

namespace XmlSrcGenerator
{
    [Generator]
    public class SourceCodeGenerator : ISourceGenerator
    {
        static Dictionary<string, XslCompiledTransform> xsltDict = new Dictionary<string, XslCompiledTransform>();
        internal const string DefaultXslFileName = "default.xslt";

        public void Execute(SourceGeneratorContext context)
        {
            string fileNamespace = "ConsoleApp1"; // context.Compilation.GlobalNamespace.GetNamespaceMembers().First().ToDisplayString();

            foreach (AdditionalText item in context.AdditionalFiles)
            {
                if (item.Path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) == false)
                {
                    continue;
                }

                string baseFolder = Path.GetDirectoryName(item.Path);

                string txt = GenerateCode(item.Path, fileNamespace, baseFolder);
                string fileName = Path.GetFileNameWithoutExtension(item.Path) + ".partial.xml";

                context.AddSource(fileName, SourceText.From(txt, Encoding.UTF8));
            }
        }

        public void Initialize(InitializationContext context)
        {
        }

        public string GenerateCode(string inputFileName, string fileNamespace, string baseFolder)
        {
            return Encoding.UTF8.GetString(GenerateCode(inputFileName, fileNamespace, baseFolder, string.Empty));
        }

        protected byte[] GenerateCode(string inputFileName, string fileNamespace, string baseFolder, string inputFileContent)
        {
            string xmlFilePath = inputFileName;
            string xslFilePath = ResolveXSLPath(xmlFilePath, baseFolder);
            if (string.IsNullOrEmpty(xslFilePath) == true)
            {
                string msg = string.Format("// XSL File not found ({0})", xslFilePath);
                return System.Text.Encoding.UTF8.GetBytes(msg);
            }

            Debug.WriteLine("Original XmlFilePath: " + xmlFilePath);
            xmlFilePath = ResolveXmlPath(xmlFilePath);
            Debug.WriteLine("Resolved XmlFilePath: " + xmlFilePath);
            Debug.WriteLine("Resolved XslFilePath: " + xslFilePath);

#if DEBUG
            string txt2 = File.ReadAllText(xslFilePath);
#endif

            XslCompiledTransform xslt;
            XsltSettings xst = XsltSettings.Default;
            xst.EnableScript = true;

            try
            {
                string xsltText = File.ReadAllText(xslFilePath);

                // 재활용한다.
                if (xsltDict.TryGetValue(xsltText, out xslt) == false)
                {
                    xslt = new XslCompiledTransform();

                    StringReader sr = new StringReader(xsltText);
                    using (XmlReader xr = XmlReader.Create(sr))
                    {
                        xslt.Load(xr, xst, null);
                    }

                    xsltDict.Add(xsltText, xslt);
                }
            }
            catch (Exception ex)
            {
                string output = string.Format("{0}{1}{2}", ex.Message, Environment.NewLine, ex.ToString());
                return System.Text.Encoding.UTF8.GetBytes(output);
            }

            StringBuilder sb = new StringBuilder();
            using (StringWriter sw = new StringWriter(sb, CultureInfo.CurrentCulture))
            {
                XmlWriterSettings xws = new XmlWriterSettings();
                xws.ConformanceLevel = ConformanceLevel.Auto;
                xws.Encoding = Encoding.UTF8;

                XmlReaderSettings xrs = new XmlReaderSettings();

                XmlWriter writer = XmlTextWriter.Create(sw, xws);
                using (XmlReader reader = XmlReader.Create(xmlFilePath, xrs))
                {
                    XsltArgumentList xal = new XsltArgumentList();
                    // LoadXsltExtensionMethod(xal);
                    xal.AddParam("XCG_CurrentTime", string.Empty, DateTime.Now.ToString(CultureInfo.InvariantCulture));
                    xal.AddParam("XCG_Namespace", string.Empty, fileNamespace);
                    xal.AddParam("XCG_BaseFolder", string.Empty, baseFolder);
                    xal.AddParam("XCG_Version", string.Empty, "1.0");
                    xslt.Transform(reader, xal, writer);
                }

#if DEBUG
                string txt = sb.ToString();
#endif
            }

            return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string ResolveXmlPath(string xmlFilePath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlFilePath);

            string baseFolder = Path.GetDirectoryName(xmlFilePath);

            XmlNode sourceNode = xmlDoc.SelectSingleNode("//ExternalSource");
            if (sourceNode == null)
            {
                return xmlFilePath;
            }

            return Path.Combine(baseFolder, sourceNode.InnerText);
        }

        private string ResolveXSLPath(string xmlFilePath, string baseFolder)
        {
            // 0: 만약 XML 파일안에 xcg-xsltKey Preprocessor 가 있다면 그를 따른다.
            XmlDocument document = new XmlDocument();
            document.Load(xmlFilePath);
            XmlProcessingInstruction xcgPI =
                document.SelectSingleNode("/processing-instruction(\"xcg-xsltKey\")")
                as XmlProcessingInstruction;

            if (xcgPI != null)
            {
                return Path.Combine(baseFolder, xcgPI.Value) + ".xslt";
            }

            // 0.1: 만약 XML 파일안에 xcg-xsltFileName Preprocessor 가 있다면 그를 따른다.
            xcgPI = document.SelectSingleNode("/processing-instruction(\"xcg-xsltFileName\")") as XmlProcessingInstruction;

            if (xcgPI != null)
            {
                string xmlFolder = Path.GetDirectoryName(xmlFilePath);
                return Path.Combine(xmlFolder, xcgPI.Value) + ".xslt";
            }

            // 1: XML 파일에서 "xslt" 확장자만 교체해서 검색
            string xslFilePath = Path.ChangeExtension(xmlFilePath, "xslt");
            if (File.Exists(xslFilePath) == true)
            {
                return xslFilePath;
            }

            // 2: 같은 폴더에서 파일명만 default.xslt
            string defaultXslFilePath = PathExtension.ChangeFileName(xslFilePath, DefaultXslFileName);
            if (File.Exists(defaultXslFilePath) == true)
            {
                return defaultXslFilePath;
            }

            // 3: 상위 폴더에서 루트 드라이브까지 파일명이 XML 파일에서 xslt 확장자만 교체한 것과 일치한 것을 검색
            string found = PathExtension.SearchInParents(xslFilePath);
            if (string.IsNullOrEmpty(found) == false)
            {
                return found;
            }

            // 4: 상위 폴더에서 루트 드라이브까지 파일명이 default.xslt 파일이 있는지 검사.
            return PathExtension.SearchInParents(defaultXslFilePath);
        }

    }
}
