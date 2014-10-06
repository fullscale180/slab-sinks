// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FullScale180.SemanticLogging.Sinks.Tests.TestSupport
{
    public static class AssertEx
    {
        public static TException Throws<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();                
            }
            catch (TException e)
            {
                return e;
            }

            Assert.Fail("Exception of type {0} should be thrown.", typeof(TException));
            
            return default(TException);
        }

        public static TException ThrowsInner<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                for (Exception x = e; x != null; x = x.InnerException)
                {
                    if (x.GetType() == typeof(TException)) { return (TException)e; }
                }
            }

            Assert.Fail("Exception of type {0} should be thrown.", typeof(TException));
            
            return default(TException);
        }
    }
}
