using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Social_Sentry.Services
{
    public class SelfProtectionService
    {
        public void ApplySelfProtection()
        {
            if (Debugger.IsAttached)
            {
                Debug.WriteLine("SelfProtection: Debugger attached, skipping ACL protection to prevent lockout.");
                return;
            }

            try
            {
                Process p = Process.GetCurrentProcess();
                IntPtr hProcess = p.Handle;

                // 1. Create a partial DACL
                var dacl = new DiscretionaryAcl(false, false, 1);
                
                // 2. Allow SYSTEM Full Access (Critical)
                var systemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
                dacl.AddAccess(AccessControlType.Allow, systemSid, (int)NativeMethods.PROCESS_ALL_ACCESS, InheritanceFlags.None, PropagationFlags.None);

                // 3. Deny 'Everyone' Terminate Access
                // Note: Deny ACEs generally should appear before Allow ACEs in canonical order 
                // but standard .NET DiscretionaryAcl handles insertion order to canonical form? 
                // Actually RawAcl is strict. GenericAcl usually sorts.
                // Let's rely on DiscretionaryAcl.
                var worldSid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                dacl.AddAccess(AccessControlType.Deny, worldSid, (int)NativeMethods.PROCESS_TERMINATE, InheritanceFlags.None, PropagationFlags.None);

                // 4. Serialize DACL to native memory
                byte[] binaryDacl = new byte[dacl.BinaryLength];
                dacl.GetBinaryForm(binaryDacl, 0);

                IntPtr pDacl = Marshal.AllocHGlobal(binaryDacl.Length);
                try
                {
                    Marshal.Copy(binaryDacl, 0, pDacl, binaryDacl.Length);

                    // 5. Apply Security Info
                    uint result = NativeMethods.SetSecurityInfo(
                        hProcess,
                        NativeMethods.SE_OBJECT_TYPE.SE_KERNEL_OBJECT,
                        NativeMethods.DACL_SECURITY_INFORMATION | NativeMethods.PROTECTED_DACL_SECURITY_INFORMATION, // Protected prevents inheritance overwrite
                        IntPtr.Zero,
                        IntPtr.Zero,
                        pDacl,
                        IntPtr.Zero);

                    if (result == 0)
                    {
                        Debug.WriteLine("SelfProtection: ACL applied successfully.");
                    }
                    else
                    {
                        Debug.WriteLine($"SelfProtection: SetSecurityInfo failed with error code {result}");
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pDacl);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying self protection: {ex.Message}");
            }
        }

        public void StartWatchdog()
        {
            try
            {
                // Path to Watchdog Exe 
                // Assuming it's in the same directory (after build copy)
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                string watchdogPath = System.IO.Path.Combine(currentDir, "SocialSentry.Watchdog.exe");

                if (System.IO.File.Exists(watchdogPath))
                {
                    int myPid = Process.GetCurrentProcess().Id;
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = watchdogPath,
                        Arguments = $"{myPid}", // Pass my PID to watchdog
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Hidden // Hide the watchdog console
                    };
                    Process.Start(psi);
                    Debug.WriteLine($"Watchdog started with PID target: {myPid}");
                }
                else
                {
                    Debug.WriteLine("Watchdog executable not found.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting watchdog: {ex.Message}");
            }
        }
    }
}
