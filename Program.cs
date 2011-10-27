using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

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
            int ret = 0;
            if (args.Length == 0)
            {
                return -1;
            }

            string fileData = null;
            string path = null;
            try
            {
                fileData = File.ReadAllText(args[0], Encoding.UTF8);
                path = Path.GetTempFileName();
                WriteStringToFile(path, fileData, LineEndingType.Windows);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                ret = -1;
                goto bail;
            }

            var psi = new ProcessStartInfo(Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\Notepad.exe"), path)
            {
                WindowStyle = ProcessWindowStyle.Normal,
                UseShellExecute = false,
            };

            var proc = Process.Start(psi);
            proc.WaitForExit();
            if (proc.ExitCode != 0)
            {
                ret = proc.ExitCode;
                goto bail;
            }

            try
            {
                fileData = File.ReadAllText(path, Encoding.UTF8);
                WriteStringToFile(args[0], fileData, LineEndingType.Posix);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                ret = -1;
                goto bail;
            }

        bail:
            File.Delete(path);
            return ret;
        }

        static void WriteStringToFile(string path, string fileData, LineEndingType lineType)
        {
            using(var of = File.OpenWrite(path))
            {
                var buf = Encoding.UTF8.GetBytes(ForceLineEndings(fileData, lineType));
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

        public static IEnumerable<string> InplaceSplit(string inputString, params char[] splitCharacters)
        {
            if (splitCharacters.Length == 0)
            {
                throw new ArgumentException("Specify at least one character to split on");
            }

            // This is written as for loops to be snappy, not because I like for
            // loops :)
            int start = 0;
            for(int i = 0; i < inputString.Length; i++)
            {
                for (int j = 0; j < splitCharacters.Length; j++)
                {
                    if(inputString[i] == splitCharacters[j] && i - start > 0)
                    {
                        yield return inputString.Substring(start, i - start);
                        start = i;
                        break;
                    }
                }
            }

            yield return inputString.Substring(start);
        }
    }
}
