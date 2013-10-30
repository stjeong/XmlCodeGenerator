using System;

using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Xsl;
using System.Diagnostics;

using System.Collections;
using Microsoft.Win32;
using System.Globalization;
using System.Reflection;
using System.Runtime.Remoting;
using System.Text;
using System.Xml;
using System.Collections.Generic;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using Microsoft.VisualStudio.Shell;
using VSLangProj80;
using System.CodeDom.Compiler;
using Microsoft.VisualStudio.Shell.Interop;

namespace BclExtension
{
    /// <summary>
    ///     This class exists to be cocreated a in a preprocessor build step.
    /// </summary>
    [Guid(XmlCodeGenerator.RefGuid)]
    [ClassInterface(ClassInterfaceType.None)]
    [CodeGeneratorRegistration(typeof(XmlCodeGenerator), "C# XML Code Generator", vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
    [CodeGeneratorRegistration(typeof(XmlCodeGenerator), "VB XML Code Generator", vsContextGuids.vsContextGuidVBProject, GeneratesDesignTimeSource = true)]
    [ProvideObject(typeof(XmlCodeGenerator))]
    public class XmlCodeGenerator : BaseCodeGeneratorWithSite
    {
        internal const string RefGuid = "64B3B9EF-EEF7-4523-82FD-7D68459D7DFA";
        internal const string DefaultXslFileName = "default.xslt";

        internal string BaseFolder
        {
            get
            {
                string modulePath = typeof(XmlCodeGenerator).Assembly.Location;
                return Path.GetDirectoryName(modulePath);
            }
        }

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public XmlCodeGenerator()
        {
        }

        static Dictionary<string, XslCompiledTransform> xsltDict = new Dictionary<string, XslCompiledTransform>();

        /// <summary>
        /// public method for GenerateCode
        /// </summary>
        /// <param name="inputFileName"></param>
        /// <returns></returns>
        public string GenerateCode(string inputFileName)
        {
            return Encoding.UTF8.GetString(GenerateCode(inputFileName, string.Empty));
        }

        /// <summary>
        /// demand-creates a CodeDomProvider
        /// </summary>
        protected override byte[] GenerateCode(string inputFileName, string inputFileContent)
        {
            string xmlFilePath = inputFileName;
            string xslFilePath = ResolveXSLPath(xmlFilePath);
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
                    xal.AddParam("XCG_Namespace", string.Empty, this.FileNamespace);
                    xal.AddParam("XCG_BaseFolder", string.Empty, this.BaseFolder);
                    xal.AddParam("XCG_Version", string.Empty, "1.0");
                    xslt.Transform(reader, xal, writer);
                }

#if DEBUG
            string txt = sb.ToString();
#endif
            }

            return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
        }

        /// <summary>
        /// XML 파일 경로가 XML 파일안에 있는 경우를 해석
        /// </summary>
        /// <param name="xmlFilePath">VS.NET IDE 에서 제공되는 XML 파일 경로</param>
        /// <returns></returns>
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

        private string ResolveXSLPath(string xmlFilePath)
        {
            // 0: 만약 XML 파일안에 xcg-xsltKey Preprocessor 가 있다면 그를 따른다.
            XmlDocument document = new XmlDocument();
            document.Load(xmlFilePath);
            XmlProcessingInstruction xcgPI =
                document.SelectSingleNode("/processing-instruction(\"xcg-xsltKey\")")
                as XmlProcessingInstruction;

            if (xcgPI != null)
            {
                return Path.Combine(this.BaseFolder, xcgPI.Value) + ".xslt";
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

        public override string GetDefaultExtension()
        {
            CodeDomProvider codeDom = GetCodeProvider();
            Debug.Assert(codeDom != null, "CodeDomProvider is NULL.");
            string extension = codeDom.FileExtension;
            if (extension != null && extension.Length > 0)
            {
                extension = "." + extension.TrimStart(".".ToCharArray());
            }
            return extension;
        }

        private CodeDomProvider codeDomProvider = null;
        protected virtual CodeDomProvider GetCodeProvider()
        {
            if (codeDomProvider == null)
            {
                // Query for IVSMDCodeDomProvider/SVSMDCodeDomProvider for this project type
                Microsoft.VisualStudio.Designer.Interfaces.IVSMDCodeDomProvider provider = GetService(typeof(SVSMDCodeDomProvider))
                    as Microsoft.VisualStudio.Designer.Interfaces.IVSMDCodeDomProvider;

                if (provider != null)
                {
                    codeDomProvider = provider.CodeDomProvider as CodeDomProvider;
                }
                else
                {
                    //In the case where no language specific CodeDom is available, fall back to C#
                    codeDomProvider = CodeDomProvider.CreateProvider("C#");
                }
            }
            return codeDomProvider;
        }


        //private static void LoadXsltExtensionMethod(XsltArgumentList xal)
        //{
        //    // 지정된 레지스트리에 등록된 Xslt Extension 메서드 목록을 반환.
        //    string regPath = XmlCodeGenerator.CodeGeneratorRegistryPath;

        //    using (RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(regPath))
        //    {
        //        string[] subKeyNames = baseKey.GetSubKeyNames();
        //        foreach (string subKeyName in subKeyNames)
        //        {
        //            Trace.WriteLine(subKeyName);

        //            using (RegistryKey subKey = baseKey.OpenSubKey(subKeyName))
        //            {
        //                string extensionPath = Convert.ToString(subKey.GetValue("Path"));
        //                Trace.WriteLine("==> " + extensionPath);

        //                Assembly extensionAssembly = Assembly.LoadFrom(extensionPath);
        //                foreach (Type type in extensionAssembly.GetTypes())
        //                {
        //                    Type extensionInterface = type.GetInterface(typeof(IXmlCodeGenExtension).Name);
        //                    if (extensionInterface == null)
        //                    {
        //                        continue;
        //                    }

        //                    Trace.WriteLine("Extension Type: " + type.FullName);
        //                    IXmlCodeGenExtension extension = extensionAssembly.CreateInstance(type.FullName) as IXmlCodeGenExtension;
        //                    extension.AddExtensionObject(xal);
        //                }
        //            }
        //        }
        //    }
        //}

    }
}


/*

 *       AppDomain newDomain = null;

      string filePath = string.Empty;

      try
      {
        newDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString());

        string thisAssemblyFileName = this.GetType().Assembly.Location;
        string path = Path.GetDirectoryName(thisAssemblyFileName);
        string dllPath = Path.Combine(path, "SeparatedCodeGenerator.dll");
        Debug.WriteLine(dllPath);

        ObjectHandle objHandle = newDomain.CreateInstanceFrom(dllPath, "BclExtension.XmlCodeGenerator");

        object objTarget = objHandle.Unwrap();
        if (objTarget == null)
        {
          Debug.WriteLine("objTarget == null");
        }
        else
        {
          Debug.WriteLine("objTarget != null");
        }
        Debug.WriteLine("GetHashCode == " + objTarget.GetHashCode());

        Type type = objTarget.GetType();

        Debug.WriteLine(type.FullName);
        Debug.WriteLine(type.BaseType.FullName);
        Debug.WriteLine("MemberInfo member in type.GetMembers");
        foreach (MemberInfo member in type.GetMembers())
        {
          Debug.WriteLine(member.Name);
        }

        Debug.WriteLine("Type itype in type.GetInterfaces");
        foreach (Type itype in type.GetInterfaces())
        {
          Debug.WriteLine(itype.FullName);
        }

        IXmlCodeGenerator codeGen = objTarget as IXmlCodeGenerator;
        if (codeGen == null)
        {
          Debug.WriteLine("codeGen == null");
        }
        else
        {
          Debug.WriteLine("codeGen != null");
        }
        Debug.WriteLine("TEST");
        string txt = codeGen.GenerateCode(dllPath);
        Debug.WriteLine("TEST-7");

        return System.Text.Encoding.UTF8.GetBytes(txt);
      }
      finally
      {
        if (newDomain != null)
        {
          AppDomain.Unload(newDomain);
          newDomain = null;
        }
      }



*/