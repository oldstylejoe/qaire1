using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qaire1
{
    public static class HighResolutionDateTime
    {
        public static bool IsAvailable { get; private set; }

        [StructLayout(LayoutKind.Sequential)]
        public struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void GetSystemTimePreciseAsFileTime(out long filetime);
        [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
        private static extern void GetSystemTimePreciseAsFileTime(out FILETIME filetime);

        public static ulong now
        {
            get
            {
                if (!IsAvailable)
                {
                    throw new InvalidOperationException("High resolution clock isn't available.");
                }

                FILETIME filetime;
                GetSystemTimePreciseAsFileTime(out filetime);

                //ulong h = Convert.ToUInt64( filetime.dwHighDateTime.ToString() + filetime.dwLowDateTime.ToString() );
                ulong h = filetime.dwHighDateTime;
                h <<= 32;
                h |= filetime.dwLowDateTime;

                return h;
            }
        }

        public static DateTime UtcNow
        {
            get
            {
                if (!IsAvailable)
                {
                    throw new InvalidOperationException("High resolution clock isn't available.");
                }

                long filetime;
                GetSystemTimePreciseAsFileTime(out filetime);

                return DateTime.FromFileTimeUtc(filetime);
            }
        }

        static HighResolutionDateTime()
        {
            try
            {
                long filetime;
                GetSystemTimePreciseAsFileTime(out filetime);
                IsAvailable = true;
            }
            catch (InvalidOperationException)
            {
                // Not running Windows 8 or higher.
                IsAvailable = false;
            }
        }
    }
}
