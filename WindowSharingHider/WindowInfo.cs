using System;

namespace WindowSharingHider
{
    public class WindowInfo
    {
        public string Title { get; set; }
        public IntPtr Handle { get; set; }
        public int ProcessId { get; set; }
        public bool StillExists { get; set; }

        public override string ToString()
        {
            return $"{Title} - PID: {ProcessId}";
        }
    }
}
