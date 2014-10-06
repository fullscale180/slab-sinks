// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FullScale180.SemanticLogging.Sinks.Tests.TestSupport
{
    [TestClass]
    public abstract class ContextBase
    {
        [TestInitialize]
        public void Initialize()
        {
            this.Given();
            this.When();
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.OnCleanup();
        }

        protected virtual void Given()
        {
        }

        protected virtual void When()
        {
        }

        protected virtual void OnCleanup()
        {
        }
    }
}
