using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextCopy;

namespace AwsEcrLogin
{
    internal class LoginService
    {
        internal static async Task Execute()
        {
            Console.WriteLine("Checking AWS SSO session status...");

            // 1. 检查SSO会话是否有效
            bool isSsoValid = await CheckSsoSessionValid();
            if (!isSsoValid)
            {
                Console.WriteLine("AWS SSO session expired or invalid. Starting login...");
                await RunAwsSsoLogin();
            }
            else
            {
                Console.WriteLine("AWS SSO session is still valid.");
            }

            // 2. 获取ECR登录密码
            Console.WriteLine("Retrieving ECR login password...");
            string ecrPassword = await GetEcrPassword();
            if (string.IsNullOrWhiteSpace(ecrPassword))
            {
                Console.Error.WriteLine("Failed to retrieve ECR password");
                return;
            }

            // 3. 构建Docker登录命令并复制到剪贴板
            string dockerLoginCommand = $"echo {ecrPassword.Trim()} | docker login --username AWS --password-stdin 975049908461.dkr.ecr.ap-east-1.amazonaws.com";

            Console.WriteLine("Docker login command generated. Copying to clipboard...");
            await ClipboardService.SetTextAsync(dockerLoginCommand);

            Console.WriteLine("Command copied to clipboard successfully!");
            Console.WriteLine("You can now paste it into your terminal to login to ECR.");
            Console.WriteLine($"Command: {dockerLoginCommand}");
        }

        private static async Task<bool> CheckSsoSessionValid()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "aws",
                    Arguments = "sts get-caller-identity",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();
                await process.WaitForExitAsync();

                // 如果命令返回0且输出包含有效信息，则认为会话有效
                return process.ExitCode == 0 &&
                       !string.IsNullOrWhiteSpace(await process.StandardOutput.ReadToEndAsync());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking SSO session: {ex.Message}");
                return false;
            }
        }

        private static async Task RunAwsSsoLogin()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "aws",
                Arguments = "sso login",
                UseShellExecute = false,
                CreateNoWindow = false
            };

            using var process = Process.Start(startInfo);
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception($"AWS SSO login failed with exit code {process.ExitCode}");
            }
        }


        private static async Task<string> GetEcrPassword()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "aws",
                Arguments = "ecr get-login-password --region ap-east-1",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                string error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"ECR password retrieval failed: {error}");
            }

            return output;
        }
    }
}
