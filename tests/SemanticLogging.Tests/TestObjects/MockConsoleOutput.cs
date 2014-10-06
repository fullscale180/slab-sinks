// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.IO;

namespace FullScale180.SemanticLogging.Sinks.Tests.TestObjects
{
    public class MockConsoleOutput : IDisposable
    {
        private TextWriter writer;  
        private TextWriter originalOutput;

        public MockConsoleOutput() 
        {
            writer = new StringWriter();
            originalOutput = Console.Out; 
            Console.SetOut(writer); 
        }

        public string Ouput
        {
            get { return writer.ToString(); }
        }

        public void Dispose() 
        { 
            Console.SetOut(originalOutput); 
            writer.Dispose();
        }
    }
}
