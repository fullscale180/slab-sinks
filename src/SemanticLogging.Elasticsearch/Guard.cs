using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace FullScale180.SemanticLogging
{
    internal static class Guard
    {
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

        public static void ArgumentGreaterOrEqualThan<T>(T lowerValue, T argumentValue, string argumentName) where T : struct, IComparable
        {
            if (argumentValue.CompareTo(lowerValue) < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentValue, string.Format(CultureInfo.CurrentCulture, "REPLACE", new object[] { argumentName, lowerValue }));
            }
        }

        public static void ArgumentIsValidTimeout(TimeSpan? argumentValue, string argumentName)
        {
            if (argumentValue.HasValue)
            {
                long totalMilliseconds = (long)argumentValue.Value.TotalMilliseconds;
                if ((totalMilliseconds < -1L) || (totalMilliseconds > 0x7fffffffL))
                {
                    throw new ArgumentOutOfRangeException(string.Format(CultureInfo.CurrentCulture, "REPLACE", new object[] { argumentName }));
                }
            }
        }

        public static void ArgumentLowerOrEqualThan<T>(T higherValue, T argumentValue, string argumentName) where T : struct, IComparable
        {
            if (argumentValue.CompareTo(higherValue) > 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentValue, string.Format(CultureInfo.CurrentCulture, "REPLACE", new object[] { argumentName, higherValue }));
            }
        }

        public static void ArgumentNotNull(object argumentValue, string argumentName)
        {
            if (argumentValue == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        public static void ArgumentNotNullOrEmpty(string argumentValue, string argumentName)
        {
            if (argumentValue == null)
            {
                throw new ArgumentNullException(argumentName);
            }
            if (argumentValue.Length == 0)
            {
                throw new ArgumentException("REPLACE", argumentName);
            }
        }

        public static void ValidateTimestampPattern(string timestampPattern, string argumentName)
        {
            ArgumentNotNullOrEmpty(timestampPattern, argumentName);
            foreach (char ch in timestampPattern.ToCharArray())
            {
                if (InvalidFileNameChars.Contains<char>(ch))
                {
                    throw new ArgumentException("Timestamp contains invalid characters", argumentName);
                }
            }
        }

        public static void ValidDateTimeFormat(string format, string argumentName)
        {
            if (format != null)
            {
                try
                {
                    DateTime.Now.ToString(format, CultureInfo.InvariantCulture);
                }
                catch (FormatException exception)
                {
                    throw new ArgumentException(argumentName, "Invalid DateTime Format", exception);
                }
            }
        }
    }
}
