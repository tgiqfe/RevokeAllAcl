using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace RevokeAllAcl
{
    /*  [書式]
     *  RevokeAllAcl <対象ファイル>
     *  RevokeAllAcl <対象フォルダー> [サブフォルダー true | false]
     *  ※デフォルト：true
     */
    class Program
    {
        static readonly string[] PARAMS_ENABLE = new string[] {
            "Enable", "E", "Enabled", "1", "true", "yes", "on" };
        static readonly string[] PARAMS_DISABLE = new string[] {
            "Disable", "D", "Disabled", "0", "false", "no", "off" };
        static int processCount = 0;
        static int totalCount = 0;

        static void Main(string[] args)
        {
            if (args.Length >= 1)
            {
                string targetFile = ResolvEnv(args[0]);

                if (File.Exists(targetFile))
                {
                    RevokeAcl(targetFile, true);
                }
                else
                {
                    RevokeAcl(targetFile, false);

                    //  引数の数が2未満 ⇒ サブフォルダーも処理
                    //  引数が2以上 & Disableパラメータに一致 ⇒ サブフォルダーは無視
                    //  引数が2以上 & Disableパラメータに不一致 ⇒サブフォルダーも処理
                    if (args.Length < 2 ||
                        !PARAMS_DISABLE.Any(x => x.Equals(args[1], StringComparison.OrdinalIgnoreCase)))
                    {
                        foreach (string fileName in Directory.GetFiles(targetFile, "*", SearchOption.AllDirectories))
                        {
                            RevokeAcl(fileName, true);
                        }
                        foreach (string dirName in Directory.GetDirectories(targetFile, "*", SearchOption.AllDirectories))
                        {
                            RevokeAcl(dirName, false);
                        }
                    }
                }
            }
            Console.WriteLine("合計 {0} オブジェクト中、{1} オブジェクトを処理", totalCount, processCount);
        }

        //  変数名を解決
        const int MAX_TRY_RESOLVENV = 5;
        static readonly Regex Pattern_ENV = new Regex("%.+%", RegexOptions.Compiled);
        static string ResolvEnv(string sourceText)
        {
            for (int i = 0; i < MAX_TRY_RESOLVENV && sourceText != null && Pattern_ENV.IsMatch(sourceText); i++)
            {
                sourceText = Environment.ExpandEnvironmentVariables(sourceText);
            }
            return sourceText;
        }

        //  アクセス権剥奪
        static void RevokeAcl(string targetFile, bool isFile)
        {
            totalCount++;
            bool isChange = false;
            if (isFile)
            {
                FileSecurity fileSecurity = File.GetAccessControl(targetFile);
                
                foreach (FileSystemAccessRule rule in 
                    fileSecurity.GetAccessRules(true, false, typeof(NTAccount)))
                {
                    fileSecurity.RemoveAccessRule(rule);
                    isChange = true;
                }
                if (isChange)
                {
                    processCount++;
                    Console.WriteLine("File：" + targetFile);
                    File.SetAccessControl(targetFile, fileSecurity);
                }
            }
            else
            {
                DirectorySecurity directorySecurity = Directory.GetAccessControl(targetFile);
                foreach (FileSystemAccessRule rule in 
                    directorySecurity.GetAccessRules(true, false, typeof(NTAccount)))
                {
                    directorySecurity.RemoveAccessRule(rule);
                    isChange = true;
                }
                if (isChange)
                {
                    processCount++;
                    Console.WriteLine("Directory：" + targetFile);
                    Directory.SetAccessControl(targetFile, directorySecurity);
                }
            }
        }
    }
}
