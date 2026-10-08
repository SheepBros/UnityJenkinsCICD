using System;
using UnityEditor;

namespace Build
{
    public static class AndroidSigning
    {
        private const string KeystorePathVariable = "ANDROID_KEYSTORE_PATH";
        private const string KeystorePasswordVariable = "ANDROID_KEYSTORE_PASSWORD";
        private const string KeyAliasVariable = "ANDROID_KEY_ALIAS";
        private const string KeyPasswordVariable = "ANDROID_KEY_PASSWORD";

        public static void Apply()
        {
            string keystorePath = GetEnvironmentVariable(KeystorePathVariable);
            string keystorePassword = GetEnvironmentVariable(KeystorePasswordVariable);
            string keyAlias = GetEnvironmentVariable(KeyAliasVariable);
            string keyPassword = GetEnvironmentVariable(KeyPasswordVariable);

            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystorePath;
            PlayerSettings.Android.keystorePass = keystorePassword;
            PlayerSettings.Android.keyaliasName = keyAlias;
            PlayerSettings.Android.keyaliasPass = keyPassword;
        }

        private static string GetEnvironmentVariable(string name)
        {
            string value = Environment.GetEnvironmentVariable(name);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Environment variable is missing: {name}");
            }

            return value;
        }
    }
}