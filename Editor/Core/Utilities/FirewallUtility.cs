using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

using Debug = UnityEngine.Debug;

namespace Unity.LiveCapture.Editor
{
    /// <summary>
    /// A class that can checks for and add firewall rules.
    /// </summary>
    static class FirewallUtility
    {
        /// <summary>
        /// Can the firewall be modified on the editor platform.
        /// </summary>
        public static bool IsSupported
        {
            get
            {
#if UNITY_EDITOR_WIN
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// An event invoked when the firewall has been configured.
        /// </summary>
        /// <remarks>
        /// The Boolean indicates if all the firewall rules already existed or were successfully added.
        /// </remarks>
        public static event Action<bool> FirewallConfigured;

        /// <summary>
        /// Checks if the firewall rules needed to permit Unity to freely communicate exist.
        /// </summary>
        /// <returns>True if all the rules exist.</returns>
        public static bool IsConfigured()
        {
            if (!IsSupported)
            {
                Debug.LogWarning("Automatic firewall configuration is not supported on this editor platform.");
                return false;
            }

#if UNITY_EDITOR_WIN
            var ruleName = $"Unity {Application.unityVersion} Live Capture";

            var info = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"advfirewall firewall show rule name=\"{ruleName}\"",
                // don't use a command line so we can read the standard output
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // hide windows opened by the process
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
            };

            try
            {
                using (var process = Process.Start(info))
                {
                    var hasExited = process.WaitForExit(2000);

                    if (!hasExited)
                    {
                        process.Kill();
                        Debug.LogWarning($"Failed to get firewall rules, netsh did not exit successfully.");
                        return false;
                    }

                    var output = process.StandardOutput.ReadToEnd();

                    switch (process.ExitCode)
                    {
                        case 0: // success
                            return CheckForRule(output, "In") && CheckForRule(output, "Out");
                        case 1: // no matching rules found. Not in the docs, found this out experimentally
                            return false;
                        default: // assume there is an error otherwise
                            Debug.LogError($"Failed to get firewall rules with exit code {process.ExitCode:X}: ({output})!");
                            return false;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get firewall rule: {e}");
                return false;
            }
#else
            return false;
#endif
        }

        /// <summary>
        /// Adds firewall rules that permit Unity to freely communicate over the network.
        /// </summary>
        /// <remarks>
        /// This will invoke the <see cref="FirewallConfigured"/> event once completed, as the configuration may
        /// take place asynchronously.
        /// </remarks>
        public static void ConfigureFirewall()
        {
            if (!IsSupported)
            {
                Debug.LogWarning("Automatic firewall configuration is not supported on this editor platform.");
                FirewallConfigured?.Invoke(false);
                return;
            }
            if (IsConfigured())
            {
                FirewallConfigured?.Invoke(true);
                return;
            }

#if UNITY_EDITOR_WIN
            var programPath = Path.GetFullPath(EditorApplication.applicationPath);
            var ruleName = $"Unity {Application.unityVersion} Live Capture";

            // We need to supply all the commands in a single line.
            // Commands separated by '&' are executed even if a previous command fails.
            var args = new StringBuilder();
            args.Append($"netsh advfirewall firewall add rule name=\"{ruleName}\" program=\"{programPath}\" dir=in profile=private,domain action=allow enable=yes");
            args.Append("&");
            args.Append($"netsh advfirewall firewall add rule name=\"{ruleName}\" program=\"{programPath}\" dir=out profile=private,domain action=allow enable=yes");
            args.Append("&");
            args.Append("exit");

            // run the commands from a command line
            var info = new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = $"/k {args}",
                UseShellExecute = true,
                // Attempts to run with elevated permissions. This is mandatory when modifying firewall rules.
                // If User Account Control notifications are enabled, a pop-up will appear asking for permission,
                // otherwise the process can execute without user input.
                Verb = "runas",
            };

            try
            {
                var process = new Process
                {
                    StartInfo = info,
                    EnableRaisingEvents = true,
                };

                process.Exited += (sender, eventArgs) =>
                {
                    switch (process.ExitCode)
                    {
                        case 0: // success
                            FirewallConfigured?.Invoke(true);
                            break;
                        default: // assume there is an error otherwise
                            Debug.LogError($"Failed to add firewall rules!");
                            FirewallConfigured?.Invoke(false);
                            break;
                    }
                    process.Dispose();
                };

                process.Start();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to configure firewall: {e}");
                FirewallConfigured?.Invoke(false);
            }
#else
            FirewallConfigured?.Invoke(false);
#endif
        }

#if UNITY_EDITOR_WIN
        static bool CheckForRule(string output, string direction)
        {
            // Sample output rule is below.

            // Rule Name:                            Unity Live Capture
            // ----------------------------------------------------------------------
            // Enabled:                              Yes
            // Direction:                            In
            // Profiles:                             Domain,Private,Public
            // Grouping:
            // LocalIP:                              Any
            // RemoteIP:                             Any
            // Protocol:                             Any
            // Edge traversal:                       No
            // Action:                               Allow

            using var stream = new StringReader(output);
            var ruleMatches = false;

            while (true)
            {
                var line = stream.ReadLine();

                if (line == null)
                    break;

                var keyValue = line.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

                // ignore keys with no specified value, we can assume default values
                if (keyValue.Length != 2)
                {
                    continue;
                }

                var key = keyValue[0].Trim();
                var value = keyValue[1].Trim();

                // keep track of the start of a new rule
                if (key == "Rule Name")
                {
                    // the previous rule matches what we are looking for, we are done looking
                    if (ruleMatches)
                    {
                        break;
                    }

                    ruleMatches = true;
                    continue;
                }

                // check if the value matches what we are looking for since the start of the last rule
                if (!ruleMatches)
                {
                    continue;
                }

                switch (key)
                {
                    case "Enabled":
                        ruleMatches = value == "Yes";
                        break;
                    case "Direction":
                        ruleMatches = value == direction;
                        break;
                    case "Action":
                        ruleMatches = value == "Allow";
                        break;
                }
            }

            return ruleMatches;
        }

#endif
    }
}
