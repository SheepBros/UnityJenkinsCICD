using System;
using System.IO;
using UnityEditor;

namespace Build
{
    public static partial class BuildCommand
    {
        private class BuildArguments
        {
            public BuildTarget Target = BuildTarget.NoTarget;
            public BuildEnvironment Environment = BuildEnvironment.None;
            public BuildConfiguration Configuration = BuildConfiguration.None;
            public string Version;
            public int BuildNumber = -1;
            public BuildAndroidPackageFormat PackageFormat = BuildAndroidPackageFormat.apk;
            public string CommitSHA;

            public BuildArguments(string[] args)
            {
                for (var index = 0; index < args.Length; index++)
                {
                    if (index + 1 >= args.Length)
                    {
                        break;
                    }
                
                    var arg = args[index];
                    if (arg == "-target" && Enum.TryParse(args[++index], out BuildTarget target))
                    {
                        Target = target;
                    }
                    else if (arg == "-environment" && Enum.TryParse(args[++index], out BuildEnvironment environment))
                    {
                        Environment = environment;
                    }
                    else if (arg == "-configuration")
                    {
                        string configuration = args[++index];
                        if (configuration == "Development")
                        {
                            Configuration = BuildConfiguration.Development;
                        }
                        else if (configuration == "Release")
                        {
                            Configuration = BuildConfiguration.Release;
                        }
                    }
                    else if (arg == "-buildNumber" && int.TryParse(args[++index], out int buildNumber))
                    {
                        BuildNumber = buildNumber;
                    }
                    else if (arg == "-packageFormat" && Enum.TryParse(args[++index], out BuildAndroidPackageFormat packageFormat))
                    {
                        PackageFormat = packageFormat;
                    }
                    else if (arg == "-buildVersion")
                    {
                        Version = args[++index];
                    }
                    else if (arg == "-commitSHA")
                    {
                        CommitSHA = args[++index];
                    }
                }

                ValidateBuildArguments();
            }

            public BuildOptions GetBuildOptions()
            {
                if (Configuration == BuildConfiguration.Development)
                {
                    return BuildOptions.Development | BuildOptions.AllowDebugging;
                }
                
                return BuildOptions.None;
            }

            public string GetOutputPath()
            {
                string buildPath = GetBuildPath();
                string version = string.IsNullOrWhiteSpace(Version)
                    ? PlayerSettings.bundleVersion
                    : Version;

                string commitSha = string.IsNullOrWhiteSpace(CommitSHA)
                    ? "local"
                    : CommitSHA;

                
                Directory.CreateDirectory(buildPath);
                
                switch (Target)
                {
                    case BuildTarget.Android:
                    {
                        return $"{buildPath}/UnityJenkinsCICD_{GetEnvironmentName()}_{GetConfigurationName()}_{version}_{BuildNumber}_{commitSha}.{PackageFormat.ToString()}";
                    }
                    case BuildTarget.iOS:
                    {
                        return $"{buildPath}/UnityJenkinsCICD_{GetEnvironmentName()}_{GetConfigurationName()}_{version}_{BuildNumber}_{commitSha}";
                    }
                    default:
                    {
                        throw new Exception($"Build target {Target} not supported.");
                    }
                }
            }

            public string GetBuildPath()
            {
                switch (Target)
                {
                    case BuildTarget.Android:
                    {
                        return "Builds/Android";
                    }
                    case BuildTarget.iOS:
                    {
                        return "Builds/iOS";
                    }
                    default:
                    {
                        throw new Exception($"Build target {Target} not supported.");
                    }
                }
            }

            public string GetConfigurationName()
            {
                switch (Configuration)
                {
                    case BuildConfiguration.Development:
                    {
                        return "development";
                    }
                    case BuildConfiguration.Release:
                    {
                        return "release";
                    }
                    default:
                    {
                        throw new Exception($"Build configuration {Configuration} not supported.");
                    }
                }
            }

            public string GetEnvironmentName()
            {
                switch (Environment)
                {
                    case BuildEnvironment.Dev:
                    {
                        return "dev";
                    }
                    case BuildEnvironment.Prod:
                    {
                        return "prod";
                    }
                    default:
                    {
                        throw new Exception($"Build environment {Environment} not supported.");
                    }
                }
            }

            private void ValidateBuildArguments()
            {
                if (Target == BuildTarget.NoTarget)
                {
                    throw new Exception($"Build target is not set.");
                }
            
                if (Environment == BuildEnvironment.None)
                {
                    throw new Exception($"Build environment is not set.");
                }
            
                if (Configuration == BuildConfiguration.None)
                {
                    throw new Exception($"Build configuration is not set.");
                }

                if (BuildNumber <= 0)
                {
                    throw new Exception("Build number must be greater than 0.");
                }
            }
        }
    }
}