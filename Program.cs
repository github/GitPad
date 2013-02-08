using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Gitpad
{
    public enum LineEndingType
    {
        Windows, /*CR+LF*/
        Posix, /*LF*/
        MacOS9, /*CR*/
        Unsure,
    }

    public class Program
    {
        public static int Main(string[] args)
        {
            if (IsProcessElevated())
            {
                MessageBox.Show("Run this application as a normal user (not as Elevated Administrator)",
                                "App is Elevated", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }

            int ret = 0;
            if (args.Length == 0)
            {
                if (MessageBox.Show("Do you want to use your default text editor as your commit editor?", 
                    "Installing GitPad", MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    return -1;
                }

                var target = new DirectoryInfo(Environment.ExpandEnvironmentVariables(@"%AppData%\GitPad"));
                if (!target.Exists)
                {
                    target.Create();
                }

                var dest = new FileInfo(Environment.ExpandEnvironmentVariables(@"%AppData%\GitPad\GitPad.exe"));
                File.Copy(Assembly.GetExecutingAssembly().Location, dest.FullName, true);

                Environment.SetEnvironmentVariable("EDITOR", "~/AppData/Roaming/GitPad/GitPad.exe", EnvironmentVariableTarget.User);
                return 0;
            }

            string fileData = null;
            string path = null;
            try
            {
                fileData = File.ReadAllText(args[0], Encoding.UTF8);
                path = Path.GetRandomFileName() + ".txt";
                WriteStringToFile(path, fileData, LineEndingType.Windows, true);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                ret = -1;
                goto bail;
            }

            var psi = new ProcessStartInfo(path)
            {
                WindowStyle = ProcessWindowStyle.Normal,
                UseShellExecute = true,
            };

            var proc = Process.Start(psi);

            // See http://stackoverflow.com/questions/3456383/process-start-returns-null
            // In case of editor reuse (think VS) we can't block on the process so we only have two options. Either try 
            // to be clever and monitor the file for changes but it's quite possible that users save their file before 
            // being done with them so we'll go with the semi-sucky method of showing a message on the console
            if (proc == null)
            {
                Console.WriteLine("Press enter when you're done editing your commit message, or CTRL+C to abort");
                Console.ReadLine();
            }
            else
            {
                proc.WaitForExit();
                if (proc.ExitCode != 0)
                {
                    ret = proc.ExitCode;
                    goto bail;
                }
            }

            try
            {
                fileData = File.ReadAllText(path, Encoding.UTF8);
                WriteStringToFile(args[0], fileData, LineEndingType.Posix, false);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                ret = -1;
                goto bail;
            }

        bail:
            if (File.Exists(path))
                File.Delete(path);
            return ret;
        }

        static void WriteStringToFile(string path, string fileData, LineEndingType lineType, bool emitUTF8Preamble)
        {
            using(var of = File.Open(path, FileMode.Create))
            {
                var buf = Encoding.UTF8.GetBytes(ForceLineEndings(fileData, lineType));
                if (emitUTF8Preamble)
                    of.Write(Encoding.UTF8.GetPreamble(), 0, Encoding.UTF8.GetPreamble().Length);
                of.Write(buf, 0, buf.Length);
            }
        }

        public static string ForceLineEndings(string fileData, LineEndingType type)
        {
            var ret = new StringBuilder(fileData.Length);

            string ending;
            switch(type)
            {
                case LineEndingType.Windows:
                    ending = "\r\n";
                    break;
                case LineEndingType.Posix:
                    ending = "\n";
                    break;
                case LineEndingType.MacOS9:
                    ending = "\r";
                    break;
                default:
                    throw new Exception("Specify an explicit line ending type");
            }

            foreach (var line in fileData.Split('\n'))
            {
                var fixedLine = line.Replace("\r", "");
                ret.Append(fixedLine);
                ret.Append(ending);
            }

            return ret.ToString();
        }

        public static unsafe bool IsProcessElevated()
        {
            if (Environment.OSVersion.Version < new Version(6,0,0,0)) 
            {
                // Elevation is not a thing.
                return false;
            }

            IntPtr tokenHandle;
            if (!NativeMethods.OpenProcessToken(NativeMethods.GetCurrentProcess(), NativeMethods.TOKEN_QUERY, out tokenHandle))
            {
                throw new Exception("OpenProcessToken failed", new Win32Exception());
            }

            try
            {
                TOKEN_ELEVATION_TYPE elevationType;
                uint dontcare;
                if (!NativeMethods.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType, out elevationType, (uint)sizeof(TOKEN_ELEVATION_TYPE), out dontcare))
                {
                    throw new Exception("GetTokenInformation failed", new Win32Exception());
                }

                return (elevationType == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull);
            }
            finally
            {
                NativeMethods.CloseHandle(tokenHandle);
            }
        }
    }

    public enum TOKEN_INFORMATION_CLASS
    {
        /// <summary>
        /// The buffer receives a TOKEN_USER structure that contains the user account of the token.
        /// </summary>
        TokenUser = 1,

        /// <summary>
        /// The buffer receives a TOKEN_GROUPS structure that contains the group accounts associated with the token.
        /// </summary>
        TokenGroups,

        /// <summary>
        /// The buffer receives a TOKEN_PRIVILEGES structure that contains the privileges of the token.
        /// </summary>
        TokenPrivileges,

        /// <summary>
        /// The buffer receives a TOKEN_OWNER structure that contains the default owner security identifier (SID) for newly created objects.
        /// </summary>
        TokenOwner,

        /// <summary>
        /// The buffer receives a TOKEN_PRIMARY_GROUP structure that contains the default primary group SID for newly created objects.
        /// </summary>
        TokenPrimaryGroup,

        /// <summary>
        /// The buffer receives a TOKEN_DEFAULT_DACL structure that contains the default DACL for newly created objects.
        /// </summary>
        TokenDefaultDacl,

        /// <summary>
        /// The buffer receives a TOKEN_SOURCE structure that contains the source of the token. TOKEN_QUERY_SOURCE access is needed to retrieve this information.
        /// </summary>
        TokenSource,

        /// <summary>
        /// The buffer receives a TOKEN_TYPE value that indicates whether the token is a primary or impersonation token.
        /// </summary>
        TokenType,

        /// <summary>
        /// The buffer receives a SECURITY_IMPERSONATION_LEVEL value that indicates the impersonation level of the token. If the access token is not an impersonation token, the function fails.
        /// </summary>
        TokenImpersonationLevel,

        /// <summary>
        /// The buffer receives a TOKEN_STATISTICS structure that contains various token statistics.
        /// </summary>
        TokenStatistics,

        /// <summary>
        /// The buffer receives a TOKEN_GROUPS structure that contains the list of restricting SIDs in a restricted token.
        /// </summary>
        TokenRestrictedSids,

        /// <summary>
        /// The buffer receives a DWORD value that indicates the Terminal Services session identifier that is associated with the token. 
        /// </summary>
        TokenSessionId,

        /// <summary>
        /// The buffer receives a TOKEN_GROUPS_AND_PRIVILEGES structure that contains the user SID, the group accounts, the restricted SIDs, and the authentication ID associated with the token.
        /// </summary>
        TokenGroupsAndPrivileges,

        /// <summary>
        /// Reserved.
        /// </summary>
        TokenSessionReference,

        /// <summary>
        /// The buffer receives a DWORD value that is nonzero if the token includes the SANDBOX_INERT flag.
        /// </summary>
        TokenSandBoxInert,

        /// <summary>
        /// Reserved.
        /// </summary>
        TokenAuditPolicy,

        /// <summary>
        /// The buffer receives a TOKEN_ORIGIN value. 
        /// </summary>
        TokenOrigin,

        /// <summary>
        /// The buffer receives a TOKEN_ELEVATION_TYPE value that specifies the elevation level of the token.
        /// </summary>
        TokenElevationType,

        /// <summary>
        /// The buffer receives a TOKEN_LINKED_TOKEN structure that contains a handle to another token that is linked to this token.
        /// </summary>
        TokenLinkedToken,

        /// <summary>
        /// The buffer receives a TOKEN_ELEVATION structure that specifies whether the token is elevated.
        /// </summary>
        TokenElevation,

        /// <summary>
        /// The buffer receives a DWORD value that is nonzero if the token has ever been filtered.
        /// </summary>
        TokenHasRestrictions,

        /// <summary>
        /// The buffer receives a TOKEN_ACCESS_INFORMATION structure that specifies security information contained in the token.
        /// </summary>
        TokenAccessInformation,

        /// <summary>
        /// The buffer receives a DWORD value that is nonzero if virtualization is allowed for the token.
        /// </summary>
        TokenVirtualizationAllowed,

        /// <summary>
        /// The buffer receives a DWORD value that is nonzero if virtualization is enabled for the token.
        /// </summary>
        TokenVirtualizationEnabled,

        /// <summary>
        /// The buffer receives a TOKEN_MANDATORY_LABEL structure that specifies the token's integrity level. 
        /// </summary>
        TokenIntegrityLevel,

        /// <summary>
        /// The buffer receives a DWORD value that is nonzero if the token has the UIAccess flag set.
        /// </summary>
        TokenUIAccess,

        /// <summary>
        /// The buffer receives a TOKEN_MANDATORY_POLICY structure that specifies the token's mandatory integrity policy.
        /// </summary>
        TokenMandatoryPolicy,

        /// <summary>
        /// The buffer receives the token's logon security identifier (SID).
        /// </summary>
        TokenLogonSid,

        /// <summary>
        /// The maximum value for this enumeration
        /// </summary>
        MaxTokenInfoClass
    }

    public enum TOKEN_ELEVATION_TYPE
    {
        TokenElevationTypeDefault = 1,
        TokenElevationTypeFull,
        TokenElevationTypeLimited
    }

    public static class NativeMethods
    {
        public const UInt32 STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        public const UInt32 STANDARD_RIGHTS_READ = 0x00020000;
        public const UInt32 TOKEN_ASSIGN_PRIMARY = 0x0001;
        public const UInt32 TOKEN_DUPLICATE = 0x0002;
        public const UInt32 TOKEN_IMPERSONATE = 0x0004;
        public const UInt32 TOKEN_QUERY = 0x0008;
        public const UInt32 TOKEN_QUERY_SOURCE = 0x0010;
        public const UInt32 TOKEN_ADJUST_PRIVILEGES = 0x0020;
        public const UInt32 TOKEN_ADJUST_GROUPS = 0x0040;
        public const UInt32 TOKEN_ADJUST_DEFAULT = 0x0080;
        public const UInt32 TOKEN_ADJUST_SESSIONID = 0x0100;
        public const UInt32 TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
        public const UInt32 TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY |
            TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE |
            TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT |
            TOKEN_ADJUST_SESSIONID);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool GetTokenInformation(
            IntPtr TokenHandle,
            TOKEN_INFORMATION_CLASS TokenInformationClass,
            out TOKEN_ELEVATION_TYPE TokenInformation,
            uint TokenInformationLength,
            out uint ReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);
    }
}
