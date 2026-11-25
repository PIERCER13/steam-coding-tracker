using Steam4NET;
using System;
using System.Diagnostics;
using System.Threading;

namespace PiercerSteamIdleWorker
{
    class IdleWorker
    {
        static void Main(string[] args)
        {
            if (args.Length < 1) return;
            uint appId;
            if (!uint.TryParse(args[0], out appId)) return;

            int parentPid = -1;
            if (args.Length >= 2) int.TryParse(args[1], out parentPid);

            Environment.SetEnvironmentVariable("SteamAppId", appId.ToString());

            if (!Steamworks.Load(true))
            {
                Environment.Exit(2);
                return;
            }

            ISteamClient012 client = Steamworks.CreateInterface<ISteamClient012>();
            if (client == null)
            {
                Environment.Exit(3);
                return;
            }

            int pipe = client.CreateSteamPipe();
            if (pipe == 0)
            {
                Environment.Exit(4);
                return;
            }

            int user = client.ConnectToGlobalUser(pipe);
            if (user == 0)
            {
                Environment.Exit(5);
                return;
            }

            while (true)
            {
                if (parentPid > 0)
                {
                    try
                    {
                        Process p = Process.GetProcessById(parentPid);
                        if (p.HasExited) break;
                    }
                    catch
                    {
                        break;
                    }
                }
                Thread.Sleep(2000);
            }

            try
            {
                client.ReleaseUser(pipe, user);
                client.BReleaseSteamPipe(pipe);
            }
            catch { }
        }
    }
}
