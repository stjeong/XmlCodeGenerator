using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasicSample
{
    class Program
    {
        static void Main(string[] args)
        {
            macroTest.Test test = new macroTest.Test();
            test.DoTest(5);

            //XmlCodeGenerator gen = new XmlCodeGenerator();

            //string xmlFile = Path.Combine(Environment.CurrentDirectory, "Test.xml");
            //string text = gen.GenerateCode(xmlFile);

            //Console.WriteLine(text);

        }
    }
}
