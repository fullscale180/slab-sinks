using System.Collections.Generic;

namespace FullScale180.SemanticLogging.Sinks.Tests.TestObjects
{
    public class MockHttpListenerResponse
    {
        public MockHttpListenerResponse()
        {
            this.Headers = new List<string>();
        }

        public int ResponseCode { get; set; }

        public string Content { get; set; }

        public string ContentType { get; set; }

        public List<string> Headers { get; set; }
    }
}
